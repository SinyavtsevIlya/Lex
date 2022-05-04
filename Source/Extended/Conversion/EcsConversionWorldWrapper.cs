namespace Nanory.Lex.Conversion.ScriptableObjects
{
    public class EcsConversionWorldWrapper
    {
        private readonly EcsWorld _world;

        public EcsConversionWorldWrapper(EcsWorld world)
        {
            _world = world;
        }

        public EcsWorld Dst => _world;

        public void Destroy() => _world.Destroy();
        public int NewEntity() => _world.NewEntity();
        public ref TComponent Add<TComponent>(int entity) where TComponent : struct
        {
            if (_world.GetPool<Prefab>().Has(entity))
            {
                _world.GetPool<Prefab>().Get(entity).PoolIndeces.Values.Add(EcsComponent<TComponent>.TypeIndex);
            }

            return ref _world.GetPool<TComponent>().Add(entity);
        }

        public ref TComponent Get<TComponent>(int entity) where TComponent : struct
        {
            return ref _world.GetPool<TComponent>().Get(entity);
        }

        public bool Has<TComponent>(int entity) where TComponent : struct
        {
            return _world.GetPool<TComponent>().Has(entity);
        }

        public void Del<TComponent>(int entity) where TComponent : struct
        {
            if (_world.GetPool<Prefab>().Has(entity))
            {
                _world.GetPool<Prefab>().Get(entity).PoolIndeces.Values.Remove(EcsComponent<TComponent>.TypeIndex);
            }

            _world.Del<TComponent>(entity);
        }

        public void DelEntity(int entity) => _world.DelEntity(entity);
    }
}
