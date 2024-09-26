using System;
using System.Collections.Generic;

namespace Nanory.Lex.Conversion
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class ConvertToEntitySystem : IEcsRunSystem, IEcsEntityCommandBufferLookup
    {
        private Dictionary<int, EcsPackedEntity> _conversionMap = new();
        private EcsConversionWorldWrapper _conversionWorldWrapper;
        private EcsFilter _requestsFilter;
        protected List<EntityCommandBufferSystem> _entityCommandBufferSystems;

        public EcsConversionWorldWrapper World => _conversionWorldWrapper;
        
        public void Run(EcsSystems systems)
        {
        }

        public int ConvertAsInstancedEntity(AuthoringEntity authoringEntity)
        {
            if (authoringEntity == null)
                throw new ArgumentNullException(nameof(authoringEntity));

            var entity = World.NewEntity();
            Convert(authoringEntity, entity);
            return entity;
        }

        public int ConvertOrGetAsPrefabEntity(AuthoringEntity authoringEntity) => ConvertOrGetPrimaryEntity(authoringEntity, true);

        public int ConvertOrGetAsUniqueEntity(AuthoringEntity authoringEntity) => ConvertOrGetPrimaryEntity(authoringEntity, false);

        public int GetPrimaryEntity(AuthoringEntity authoringEntity)
        {
            if (_conversionMap.TryGetValue(authoringEntity.GetHashCode(), out var newPackedEntity))
            {
                if (newPackedEntity.Unpack(World.Dst, out var newUnpackedEntity))
                    return newUnpackedEntity;
            }

            var newEntity = _conversionWorldWrapper.NewEntity();
            newPackedEntity = World.Dst.PackEntity(newEntity);
            _conversionMap[authoringEntity.GetHashCode()] = newPackedEntity;

            return newEntity;
        }

        public void PreInit(EcsSystems systems)
        {
            _conversionWorldWrapper = new EcsConversionWorldWrapper(systems.GetWorld());
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

        private int ConvertOrGetPrimaryEntity(AuthoringEntity authoringEntity, bool isPrefab)
        {
            if (authoringEntity == null)
                throw new ArgumentNullException(nameof(authoringEntity));

            var entity = GetPrimaryEntity(authoringEntity);

            if (IsEntityConverted(entity))
                return entity;

            if (isPrefab) 
                World.Dst.SetAsPrefab(entity);

            Convert(authoringEntity, entity);
            return entity;
        }
        
        private void Convert(AuthoringEntity authoringEntity, int entity)
        {
            World.Dst.Add<ConvertedTag>(entity);
            authoringEntity.Convert(entity, this);
        }

        /// <summary>
        ///  Determines whether the entity has been converted or not yet.
        /// <remarks>Note, that <see cref="GetPrimaryEntity"/> calls do not ensure that entity is converted.</remarks>
        /// </summary>
        private bool IsEntityConverted(int entity) => World.Dst.Has<ConvertedTag>(entity);
    }
}
