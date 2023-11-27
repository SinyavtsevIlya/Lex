using System;
using System.Collections.Generic;

namespace Nanory.Lex.Conversion
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class ConvertToEntitySystem : IEcsRunSystem, IEcsPreInitSystem, IEcsEntityCommandBufferLookup
    {
        private Dictionary<int, EcsPackedEntity> _conversionMap = new();
        private EcsConversionWorldWrapper _conversionWorldWrapper;
        private EcsPool<ConvertRequest> _requestsPool;
        private EcsFilter _requestsFilter;
        protected List<EntityCommandBufferSystem> _entityCommandBufferSystems;

        public EcsConversionWorldWrapper World => _conversionWorldWrapper;

        public int ConvertAsInstancedEntity(IConvertToEntity convertToEntity)
        {
            if (convertToEntity == null)
                throw new ArgumentNullException(nameof(convertToEntity));

            var entity = World.NewEntity();
            Convert(convertToEntity, entity);
            return entity;
        }

        public int ConvertOrGetAsPrefabEntity(IConvertToEntity convertToEntity) => ConvertOrGetPrimaryEntity(convertToEntity, true);

        public int ConvertOrGetAsUniqueEntity(IConvertToEntity convertToEntity) => ConvertOrGetPrimaryEntity(convertToEntity, false);

        public int GetPrimaryEntity(IConvertToEntity convertToEntity)
        {
            if (_conversionMap.TryGetValue(convertToEntity.GetHashCode(), out var newPackedEntity))
            {
                if (newPackedEntity.Unpack(World.Dst, out var newUnpackedEntity))
                    return newUnpackedEntity;
            }

            var newEntity = _conversionWorldWrapper.NewEntity();
            newPackedEntity = World.Dst.PackEntity(newEntity);
            _conversionMap[convertToEntity.GetHashCode()] = newPackedEntity;

            return newEntity;
        }

        public void PreInit(EcsSystems systems)
        {
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
                case ConversionMode.Unique: return ConvertOrGetPrimaryEntity(convertToEntity, false);
                case ConversionMode.Prefab: return ConvertOrGetPrimaryEntity(convertToEntity, true);
                default: throw new ArgumentOutOfRangeException(nameof(conversionMode));
            }
        }

        private int ConvertOrGetPrimaryEntity(IConvertToEntity convertToEntity, bool isPrefab)
        {
            if (convertToEntity == null)
                throw new ArgumentNullException(nameof(convertToEntity));

            var entity = GetPrimaryEntity(convertToEntity);

            if (IsEntityConverted(entity))
                return entity;

            if (isPrefab) 
                World.Dst.SetAsPrefab(entity);

            Convert(convertToEntity, entity);
            return entity;
        }
        
        private void Convert(IConvertToEntity convertToEntity, int entity)
        {
            World.Dst.Add<ConvertedTag>(entity);
            convertToEntity.Convert(entity, this);
        }

        /// <summary>
        ///  Determines whether the entity has been converted or not yet.
        /// <remarks>Note, that <see cref="GetPrimaryEntity"/> calls do not ensure that entity is converted.</remarks>
        /// </summary>
        private bool IsEntityConverted(int entity) => World.Dst.Has<ConvertedTag>(entity);

    }
}
