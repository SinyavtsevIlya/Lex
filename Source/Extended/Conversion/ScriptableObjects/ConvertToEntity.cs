using System;
using System.Collections.Generic;

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

    public interface IReplaceAuthoringComponent
    {
        public Type GetAuthoringTypeToReplace();
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
        private Dictionary<int, int> _conversionMap = new Dictionary<int, int>();
        private EcsConversionWorldWrapper _conversionWorldWrapper;
        private EcsPool<ConvertRequest> _requestsPool;
        private EcsFilter _requestsFilter;
        protected List<EntityCommandBufferSystem> _entityCommandBufferSystems;

        private EcsSystems _ecsSystems;
        public EcsSystems EcsSystems => _ecsSystems;

        public EcsConversionWorldWrapper World => _conversionWorldWrapper;

        public int Convert(IConvertToEntity convertToEntity, ConversionMode conversionMode)
        {
            switch (conversionMode)
            {
                case ConversionMode.Instanced: return ConvertAsInstansedEntity(convertToEntity);
                case ConversionMode.Unique: return ConvertAsUniqueEntity(convertToEntity);
                case ConversionMode.Prefab: return ConvertAsPrefabEntity(convertToEntity);
                default: throw new ArgumentOutOfRangeException(nameof(conversionMode));
            }
        }

        public int ConvertAsInstansedEntity(IConvertToEntity convertToEntity)
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

        public int ConvertAsUniqueEntity(IConvertToEntity convertToEntity)
        {
            if (convertToEntity == null)
                throw new ArgumentNullException(nameof(convertToEntity));

            var entity = GetPrimaryEntity(convertToEntity, out _);
            convertToEntity.Convert(entity, this);
            return entity;
        }
        
        public int ConvertAsPrefabEntity(IConvertToEntity convertToEntity)
        {
            if (convertToEntity == null)
                throw new ArgumentNullException(nameof(convertToEntity));

            var entity = GetPrimaryEntity(convertToEntity, out _);
            
            World.Dst.SetAsPrefab(entity);
            
            convertToEntity.Convert(entity, this);
            return entity;
        }

        public int ConvertOrGetAsPrefabEntity(IConvertToEntity convertToEntity) => ConvertOrGetPrimaryEntity(convertToEntity, true);

        public int ConvertOrGetAsUniqueEntity(IConvertToEntity convertToEntity) => ConvertOrGetPrimaryEntity(convertToEntity, false);

        public int ConvertOrGetPrimaryEntity(IConvertToEntity convertToEntity, bool isPrefab)
        {
            if (convertToEntity == null)
                throw new ArgumentNullException(nameof(convertToEntity));

            var entity = GetPrimaryEntity(convertToEntity, out var isNew);

            if (!isNew)
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

        public int GetPrimaryEntity(IConvertToEntity convertToEntity, out bool isNew)
        {
            if (_conversionMap.TryGetValue(convertToEntity.GetHashCode(), out var newEntity))
            {
                isNew = false;
                return newEntity;
            }

            newEntity = _conversionWorldWrapper.NewEntity();
            _conversionMap[convertToEntity.GetHashCode()] = newEntity;

            isNew = true;
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
    }
}
