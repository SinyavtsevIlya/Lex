namespace Nanory.Lex
{
    public struct Prefab : IEcsAutoReset<Prefab>
    {
        public Buffer<int> PoolIndeces;

        public void AutoReset(ref Prefab c)
        {
            c.PoolIndeces.AutoReset(ref c.PoolIndeces);
        }
    }

    public static class EntityCopyExtensions
    {
        public static int Instantiate(this EcsWorld world, int prefabEntity)
        {
#if DEBUG
            if (!world.GetPool<Prefab>().Has(prefabEntity))
                throw new System.ArgumentException($"Entity-{prefabEntity} must have a {nameof(Prefab)} component to be instantiated. Use {nameof(SetAsPrefab)}()");
#endif
            var newEntity = world.NewEntity();

            ref var composition = ref world.GetPool<Prefab>().Get(prefabEntity);
            var indeces = composition.PoolIndeces.Values;
            for (int i = 0; i < indeces.Count; i++)
            {
                world.PoolsSparse[indeces[i]].CpyToDstEntity(prefabEntity, newEntity);
            }

            return newEntity;
        }

        public static bool SetAsPrefab(this EcsWorld world, int entity)
        {
            if (world.GetPool<Prefab>().Has(entity))
                return false;

            world.GetPool<Prefab>().Add(entity);
            return true;
        }

        public static ref TComponent AddToPrefab<TComponent>(this EcsWorld world, int prefabEntity) where TComponent : struct
        {
#if DEBUG
            if (!world.GetPool<Prefab>().Has(prefabEntity))
                throw new System.ArgumentException($"entity{prefabEntity} must have a {nameof(Prefab)} component. Use {nameof(SetAsPrefab)}()");
#endif

            world.GetPool<Prefab>().Get(prefabEntity).PoolIndeces.Values.Add(EcsComponent<TComponent>.TypeIndex);
            return ref world.GetPool<TComponent>().Add(prefabEntity);
        }
    }
}
