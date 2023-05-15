using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nanory.Lex.Conversion
{
    /// <summary>
    /// Base class for serializable representation
    /// of any user-defined component. Implementing <see cref="IConvertToEntity.Convert"/>
    /// is necessary to apply a desired changes to a passed entity.
    /// All Authoring components are normally stored in <see cref="AuthoringEntity"/>. 
    /// </summary>
    [Serializable]
    public abstract class AuthoringComponent : IConvertToEntity
    {
#if UNITY_EDITOR
        [UnityEngine.HideInInspector]
#endif
        public abstract void Convert(int entity, ConvertToEntitySystem convertToEntitySystem);
    }

    public interface IPrimaryPreviewTexture
    {
        Texture2D GetPreviewTexture();
    }


    public interface IReplaceAuthoringComponent
    {
        Type GetAuthoringTypeToReplace();
    }

    public static class AuthoringComponentExtensions
    {
        /// <summary>
        /// Reusable static temporary pool for merging original AuthoingComponents 
        /// and overrides-components. We use it to avoid allocations.
        /// </summary>
        public readonly static List<AuthoringComponent> MergePoolNonAlloc = new();
        
        public static List<AuthoringComponent> MergeNonAlloc(this List<AuthoringComponent> components,
            List<AuthoringComponent> overrides)
        {
            MergePoolNonAlloc.Clear();

            foreach (var component in components)
            {
                MergePoolNonAlloc.Add(component);
            }

            if (overrides == null || overrides.Count == 0)
                return MergePoolNonAlloc;

            foreach (var overrideComponent in overrides)
            {
                var hasFound = false;
                for (var idx = 0; idx < MergePoolNonAlloc.Count; idx++)
                {
                    var component = MergePoolNonAlloc[idx];
                    var isSameType = component.GetType() == overrideComponent.GetType();
                    var isReplacementType =
                        overrideComponent is IReplaceAuthoringComponent replacement &&
                        replacement.GetAuthoringTypeToReplace() == component.GetType();
                    
                    if (isSameType || isReplacementType)
                    {
                        MergePoolNonAlloc[idx] = overrideComponent;
                        hasFound = true;
                        break;
                    }
                }

                if (!hasFound)
                {
                    MergePoolNonAlloc.Add(overrideComponent);
                }
            }

            return MergePoolNonAlloc;
        }

        public static string ToShortenedAuthoringName(this Type authoringType, bool addSpaces = true)
        {
            var typeName = authoringType.Name;
            if (!typeName.Contains("Authoring"))
                throw new Exception($"{authoringType} has a wrong naming. It should contain an \"Authoring\" postfix");

            typeName = typeName.Replace("Authoring", string.Empty);

            if (!addSpaces)
                return typeName;
            
            const string pattern = "(\\B[A-Z])";
            typeName = System.Text.RegularExpressions.Regex.Replace(typeName, pattern, " $1");
            
            return typeName;
        }
    }

    public static class ConversionExtensions
    {
        public static void Convert(this EcsWorld world, IConvertToEntity convertToEntity, ConversionMode conversionMode)
        {
            ref var requestEntity = ref world.Add<ConvertRequest>(world.NewEntity());
            requestEntity.Value = convertToEntity;
            requestEntity.Mode = conversionMode;
        }
    }

    public struct ConvertRequest
    {
        public ConversionMode Mode;
        public IConvertToEntity Value;
    }

    public enum ConversionMode
    {
        Instanced,
        Unique,
        Prefab
    }


    public interface IConvertToEntity
    {
        void Convert(int entity, ConvertToEntitySystem convertToEntitySystem);
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class ConvertToEntitySystem : IEcsRunSystem, IEcsPreInitSystem, IEcsEntityCommandBufferLookup
    {
        private Dictionary<int, EcsPackedEntity> _conversionMap = new();
        private EcsConversionWorldWrapper _conversionWorldWrapper;
        private EcsPool<ConvertRequest> _requestsPool;
        private EcsFilter _requestsFilter;
        protected List<EntityCommandBufferSystem> _entityCommandBufferSystems;

        private EcsSystems _ecsSystems;
        public EcsSystems EcsSystems => _ecsSystems;

        public EcsConversionWorldWrapper World => _conversionWorldWrapper;

        public int ConvertAsInstancedEntity(IConvertToEntity convertToEntity)
        {
            if (convertToEntity == null)
                throw new ArgumentNullException(nameof(convertToEntity));
#if DEBUG
            // TODO: cache an instanced entity int the debug
            // InstancedRegistry to prevent user from trying to convert 
            // instanced entity as prefab or unique entity.
#endif
            var entity = World.NewEntity();
            convertToEntity.Convert(entity, this);
            return entity;
        }

        public int ConvertOrGetAsPrefabEntity(IConvertToEntity convertToEntity) => ConvertOrGetPrimaryEntity(convertToEntity, true);

        public int ConvertOrGetAsUniqueEntity(IConvertToEntity convertToEntity) => ConvertOrGetPrimaryEntity(convertToEntity, false);

        public int GetPrimaryEntity(IConvertToEntity convertToEntity)
        {
            if (_conversionMap.TryGetValue(convertToEntity.GetHashCode(), out var newPackedEntity))
            {
                if (newPackedEntity.Unpack(World.Dst, out var newUnpackedEntity))
                {
                    return newUnpackedEntity;
                }
            }

            var newEntity = _conversionWorldWrapper.NewEntity();
            newPackedEntity = World.Dst.PackEntity(newEntity);
            _conversionMap[convertToEntity.GetHashCode()] = newPackedEntity;

            return newEntity;
        }

        public void PreInit(EcsSystems systems)
        {
            _ecsSystems = systems;
            _conversionWorldWrapper = new EcsConversionWorldWrapper(systems.GetWorld());
            _requestsPool = _conversionWorldWrapper.Dst.GetPool<ConvertRequest>();
            _requestsFilter = _conversionWorldWrapper.Dst.Filter<ConvertRequest>().End();
        }

        public void Run(EcsSystems systems)
        {
            var later = GetCommandBufferFrom<BeginSimulationECBSystem>();

            foreach (var requestEntity in _requestsFilter)
            {
                ref var request = ref _requestsPool.Get(requestEntity);
                Convert(request.Value, request.Mode);
                later.DelEntity(requestEntity);
            }
        }

        public IEcsEntityCommandBufferLookup SetEntityCommandBufferSystemsLookup(List<EntityCommandBufferSystem> systems)
        {
            _entityCommandBufferSystems = systems;
            return this;
        }

        public EntityCommandBuffer GetCommandBufferFrom<TSystem>() where TSystem : EntityCommandBufferSystem
        {
            foreach (var system in _entityCommandBufferSystems)
            {
                if (system is TSystem)
                    return system.GetBuffer();
            }

            throw new Exception($"no system {typeof(TSystem)} presented in the entityCommandBufferSystems lookup");
        }
        
        private int Convert(IConvertToEntity convertToEntity, ConversionMode conversionMode)
        {
            switch (conversionMode)
            {
                case ConversionMode.Instanced: return ConvertAsInstancedEntity(convertToEntity);
                case ConversionMode.Unique: return ConvertAsUniqueEntity(convertToEntity);
                case ConversionMode.Prefab: return ConvertAsPrefabEntity(convertToEntity);
                default: throw new ArgumentOutOfRangeException(nameof(conversionMode));
            }
        }
        
        private int ConvertAsUniqueEntity(IConvertToEntity convertToEntity)
        {
            if (convertToEntity == null)
                throw new ArgumentNullException(nameof(convertToEntity));

            var entity = GetPrimaryEntity(convertToEntity);
            convertToEntity.Convert(entity, this);
            return entity;
        }
        
        private int ConvertOrGetPrimaryEntity(IConvertToEntity convertToEntity, bool isPrefab)
        {
            if (convertToEntity == null)
                throw new ArgumentNullException(nameof(convertToEntity));

            var entity = GetPrimaryEntity(convertToEntity);

            if (World.Dst.GetComponentsCount(entity) > 0)
            {
                return entity;
            }

            if (isPrefab)
            {
                World.Dst.SetAsPrefab(entity);
            }

            convertToEntity.Convert(entity, this);

            return entity;
        }
        
        private int ConvertAsPrefabEntity(IConvertToEntity convertToEntity)
        {
            if (convertToEntity == null)
                throw new ArgumentNullException(nameof(convertToEntity));

            var entity = GetPrimaryEntity(convertToEntity);
            
            World.Dst.SetAsPrefab(entity);
            
            convertToEntity.Convert(entity, this);
            return entity;
        }
    }
}
