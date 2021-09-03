namespace Nanory.Lex
{
    public struct EntityComposition : IEcsAutoReset<EntityComposition>
    {
        public Buffer<int> PoolIndeces;

        public void AutoReset(ref EntityComposition c)
        {
            PoolIndeces.AutoReset(ref c.PoolIndeces);
        }
    }

    public static class EntityCopyExtensions
    {
        public static int CopyEntity(this EcsWorld world, int entity)
        {
#if DEBUG
            if (!world.GetPool<EntityComposition>().Has(entity))
                throw new System.ArgumentException($"Entity-{entity} must have an {nameof(EntityComposition)} component to be copied. Use {nameof(PrepareForCopying)}()");
#endif
            var newEntity = world.NewEntity();

            ref var composition = ref world.GetPool<EntityComposition>().Get(entity);
            var indeces = composition.PoolIndeces.Values;
            for (int i = 0; i < indeces.Count; i++)
            {
                world.PoolsSparse[indeces[i]].CpyToDstEntity(entity, newEntity);
            }

            return newEntity;
        }

        public static void PrepareForCopying(this EcsWorld world, int entity)
        {
#if DEBUG
            if (world.GetPool<EntityComposition>().Has(entity))
                throw new System.ArgumentException($"Entity-{entity} already has a {nameof(EntityComposition)} component and prepared for copying");
#endif
            world.GetPool<EntityComposition>().Add(entity).PoolIndeces.Values = Buffer<int>.Pool.Pop();
        }

        public static void AddToCopyable<TComponent>(this EcsWorld world, int entity) where TComponent : struct
        {
#if DEBUG
            if (!world.GetPool<EntityComposition>().Has(entity))
                throw new System.ArgumentException($"Copyable entity must have a {nameof(EntityComposition)} component. Use {nameof(PrepareForCopying)}()");
#endif

            world.GetPool<TComponent>().Add(entity);
            world.GetPool<EntityComposition>().Get(entity).PoolIndeces.Values.Add(EcsComponent<TComponent>.TypeIndex);
        }
    }
}
