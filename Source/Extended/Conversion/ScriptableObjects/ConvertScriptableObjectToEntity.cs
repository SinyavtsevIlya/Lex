using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;

namespace Nanory.Lex.Conversion
{
    [Serializable]
    public abstract class AuthoringComponent : IConvertToEntity
    {
        [HideInInspector]
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
        public static void Convert(this EcsWorld world, IConvertToEntity convertToEntity)
        {
            ref var requestEntity = ref world.Add<ConvertRequest>(world.NewEntity());
            requestEntity.Value = convertToEntity;
        }
    }

    public struct ConvertRequest
    {
        public IConvertToEntity Value;
    }

    public interface IConvertToEntity
    {
        void Convert(int entity, ConvertToEntitySystem сonvertToEntitySystem);
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class ConvertToEntitySystem : IEcsRunSystem, IEcsInitSystem, IEcsEntityCommandBufferLookup
    {
        private Dictionary<AuthoringEntity, int> _conversionMap = new Dictionary<AuthoringEntity, int>();
        private EcsConversionWorldWrapper _conversionWorldWrapper;
        private EcsPool<ConvertRequest> _requestsPool;
        private EcsFilter _requestsFilter;
        protected List<EntityCommandBufferSystem> _entityCommandBufferSystems;

        private EcsSystems _ecsSystems;
        public EcsSystems EcsSystems => _ecsSystems;

        public EcsConversionWorldWrapper World => _conversionWorldWrapper;

        public int GetPrimaryEntity(AuthoringEntity conversionEntity)
        {
            return GetPrimaryEntity(conversionEntity, out var _);
        }

        public int GetPrimaryEntity(AuthoringEntity authoringEntity, out bool isNew)
        {
            if (_conversionMap.TryGetValue(authoringEntity, out var newEntity))
            {
                isNew = false;
                return newEntity;
            }

            newEntity = _conversionWorldWrapper.NewEntity();
            _conversionMap[authoringEntity] = newEntity;

            isNew = true;
            return newEntity;
        }

        public int Convert(IConvertToEntity convertToEntity)
        {
            var entity = World.NewEntity();
            convertToEntity.Convert(entity, this);
            return entity;
        }

        public int Convert(AuthoringEntity authoringEntity)
        {
#if DEBUG
            if (authoringEntity == null)
                throw new System.ArgumentException("Unable to convert. Passed conversionEntity is null");
#endif
            var entity = GetPrimaryEntity(authoringEntity, out var isNew);

            if (!isNew)
            {
                return entity;
            }

            if (authoringEntity.IsPrefab)
            {
                World.Dst.SetAsPrefab(entity);
            }

            authoringEntity.Convert(entity, this);

            return entity;
        }

        public void Init(EcsSystems systems)
        {
            _ecsSystems = systems;
            _conversionWorldWrapper = new EcsConversionWorldWrapper(systems.GetWorld());
            _requestsPool = _conversionWorldWrapper.Dst.GetPool<ConvertRequest>();
            _requestsFilter = _conversionWorldWrapper.Dst.Filter<ConvertRequest>().End();
        }

        public void Run(EcsSystems systems)
        {
            foreach (var requestEntity in _requestsFilter)
            {
                ref var request = ref _requestsPool.Get(requestEntity);

                if (request.Value is AuthoringEntity authoringEntity)
                {
                    Convert(authoringEntity);
                }
                else
                {
                    Convert(request.Value);
                }
                _conversionWorldWrapper.DelEntity(requestEntity);
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
