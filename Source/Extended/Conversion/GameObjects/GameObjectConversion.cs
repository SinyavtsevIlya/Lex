using UnityEngine;
using System.Collections.Generic;
using System;

namespace Nanory.Lex.Conversion.GameObjects
{
    public static class GameObjectConversionExtensions
    {
        public static void Convert(this EcsWorld world, GameObject gameObject, ConversionMode mode = ConversionMode.Convert)
        {
            ref var requestEntity = ref world.Add<ConvertGameObjectRequest>(world.NewEntity());
            requestEntity.Value = gameObject;
            requestEntity.Mode = mode;
        }
    }

    public abstract class Authoring<TComponent> : ConversionComponent where TComponent : struct
    {
        [SerializeField] protected TComponent _component;

        public override void Convert(int entity, ConvertToEntitySystem сonvertToEntitySystem)
        {
            сonvertToEntitySystem.World.Add<TComponent>(entity) = _component;
        }
    }

    public enum ConversionMode
    {
        ConvertAndDestroy = 0,
        Convert = 1
    }

    public struct ConvertGameObjectRequest
    {
        public GameObject Value;
        public ConversionMode Mode;
    }

    public interface IConvertGameObjectToEntity
    {
        void Convert(int entity, GameObjectConversionSystem сonvertToEntitySystem);
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class GameObjectConversionSystem : IEcsRunSystem, IEcsInitSystem, IEcsEntityCommandBufferLookup
    {
        private Dictionary<GameObject, int> _conversionMap = new Dictionary<GameObject, int>();
        private EcsConversionWorldWrapper _conversionWorldWrapper;
        private EcsPool<ConvertGameObjectRequest> _requestsPool;
        private EcsFilter _requestsFilter;
        protected List<EntityCommandBufferSystem> _entityCommandBufferSystems;

        private EcsSystems _ecsSystems;
        public EcsSystems EcsSystems => _ecsSystems;

        public EcsConversionWorldWrapper World => _conversionWorldWrapper;

        public int GetPrimaryEntity(GameObject gameObject)
        {
            return GetPrimaryEntity(gameObject, out var _);
        }

        public int GetPrimaryEntity(GameObject gameObject, out bool isNew)
        {
            if (_conversionMap.TryGetValue(gameObject, out var newEntity))
            {
                isNew = false;
                return newEntity;
            }

            newEntity = _conversionWorldWrapper.NewEntity();
            _conversionMap[gameObject] = newEntity;

            isNew = true;
            return newEntity;
        }

        public int Convert(GameObject gameObject, ConversionMode mode = ConversionMode.Convert)
        {
#if DEBUG
            if (gameObject == null)
                throw new System.ArgumentException("Unable to convert. Passed gameObject is null");
#endif
            var entity = GetPrimaryEntity(gameObject, out var isNew);

            var isPrefab = gameObject.scene.name == null;

            if (isPrefab)
            {
                if (!isNew)
                {
                    return entity;
                }

                World.Dst.SetAsPrefab(entity);
            }

            foreach (var convertable in gameObject.GetComponents<IConvertGameObjectToEntity>())
            {
                convertable.Convert(entity, this);
            }

            if (mode == ConversionMode.ConvertAndDestroy)
                GameObject.Destroy(gameObject);

            return entity;
        }

        public void Init(EcsSystems systems)
        {
            _ecsSystems = systems;
            _conversionWorldWrapper = new EcsConversionWorldWrapper(systems.GetWorld());
            _requestsPool = _conversionWorldWrapper.Dst.GetPool<ConvertGameObjectRequest>();
            _requestsFilter = _conversionWorldWrapper.Dst.Filter<ConvertGameObjectRequest>().End();
        }

        public void Run(EcsSystems systems)
        {
            foreach (var requestEntity in _requestsFilter)
            {
                ref var request = ref _requestsPool.Get(requestEntity);
                Convert(request.Value, request.Mode);
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
