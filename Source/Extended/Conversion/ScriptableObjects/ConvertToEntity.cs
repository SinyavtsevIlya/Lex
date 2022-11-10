using System;
using System.Collections.Generic;
using System.Linq;

namespace Nanory.Lex.Conversion
{
    [Serializable]
    public abstract class AuthoringComponent : IConvertToEntity
    {
#if UNITY_EDITOR
        [UnityEngine.HideInInspector]
#endif
        [NonSerialized]
        public AuthoringEntity AuthoringEntity;

        public TAuthoringComponent Get<TAuthoringComponent>()
            where TAuthoringComponent : AuthoringComponent =>
            AuthoringEntity.Get<TAuthoringComponent>();

        public TConversionComponent[] GetAll<TConversionComponent>()
            where TConversionComponent : AuthoringComponent =>
            AuthoringEntity._components.OfType<TConversionComponent>()
            .ToArray();

        public AuthoringEntity Add<TAuthoringComponent>(TAuthoringComponent c)
            where TAuthoringComponent : AuthoringComponent =>
            AuthoringEntity.Add(c);

        public abstract void Convert(int entity, ConvertToEntitySystem сonvertToEntitySystem);
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
        void Convert(int entity, ConvertToEntitySystem сonvertToEntitySystem);
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
                case ConversionMode.Prefab: return ConvertOrGetAsPrefabEntity(convertToEntity);
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
