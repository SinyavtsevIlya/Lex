using UnityEngine;
using System.Collections.Generic;

namespace Nanory.Lex.Conversion
{
    public static class GameObjectConversionExtensions
    {
        public static void Convert(this EcsWorld world, GameObject gameObject, ConversionMode mode = ConversionMode.ConvertAndDestroy)
        {
            ref var requestEntity = ref world.Add<ConvertGameObjectRequest>(world.NewEntity());
            requestEntity.Value = gameObject;
            requestEntity.Mode = mode;
        }
    }

    public abstract class Authoring<TComponent> : MonoBehaviour, IConvertGameObjectToEntity where TComponent : struct
    {
        public void Convert(int entity, GameObjectConversionSystem converstionSystem)
        {
            if (IsPrefab)
            {
                var world = converstionSystem.World;
                world.SetAsPrefab(entity);
                world.GetPool<Prefab>().Get(entity).PoolIndeces.Values.Add(EcsComponent<TComponent>.TypeIndex);
            }

            OnConvert(entity, converstionSystem);
        }

        protected abstract void OnConvert(int entity, GameObjectConversionSystem converstionSystem);

        private bool IsPrefab => gameObject.scene.name == null;
    }

    public abstract class AutoAuthoring<TComponent> : MonoBehaviour, IConvertGameObjectToEntity where TComponent : struct
    {
        [SerializeField] protected TComponent _component;

        public void Convert(int entity, GameObjectConversionSystem converstionSystem)
        {
            var world = converstionSystem.World;
            if (IsPrefab)
            {
                world.SetAsPrefab(entity);
                world.AddToPrefab<TComponent>(entity) = _component;
            }
            else
            {
                world.Add<TComponent>(entity) = _component;
            }
        }

        private bool IsPrefab => gameObject.scene.name == null;
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
        void Convert(int entity, GameObjectConversionSystem converstionSystem);
    }

    public class GameObjectConversionSystem : IEcsRunSystem, IEcsInitSystem
    {
        Dictionary<GameObject, int> _conversionMap;
        EcsWorld _world;
        EcsPool<ConvertGameObjectRequest> _requestsPool;
        EcsFilter _requestsFilter;

        public GameObjectConversionSystem(EcsWorld world)
        {
            _conversionMap = new Dictionary<GameObject, int>();
            _world = world;
        }

        public EcsWorld World => _world;

        public int GetPrimaryEntity(GameObject gameObject)
        {
            if (_conversionMap.TryGetValue(gameObject, out var resolvedEntity))
            {
                return resolvedEntity;
            }

            var newEntity = _world.NewEntity();
            _conversionMap[gameObject] = newEntity;

            return newEntity;
        }

        public int Convert(GameObject gameObject, ConversionMode mode)
        {
#if DEBUG
            if (gameObject == null)
                throw new System.ArgumentException("Unable to convert. Passed gameObject is null");
#endif
            var entity = GetPrimaryEntity(gameObject);
            foreach (var convertable in gameObject.GetComponents<IConvertGameObjectToEntity>())
            {
                convertable.Convert(entity, this);
                Debug.Log("convert");
            }

            if (mode == ConversionMode.ConvertAndDestroy)
                Object.Destroy(gameObject);

            return entity;
        }

        public void Init(EcsSystems systems)
        {
            _requestsPool = _world.GetPool<ConvertGameObjectRequest>();
            _requestsFilter = _world.Filter<ConvertGameObjectRequest>().End();
        }

        public void Run(EcsSystems systems)
        {
            foreach (var requestEntity in _requestsFilter)
            {
                ref var request = ref _requestsPool.Get(requestEntity);
                Convert(request.Value, request.Mode);
                _world.DelEntity(requestEntity);
            }
        }
    }
}
