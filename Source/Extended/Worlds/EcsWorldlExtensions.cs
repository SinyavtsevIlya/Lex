using Nanory.Lex;

public static class EcsWorldlExtensions
{
    public static ref T Add<T>(this EcsWorld world, int entity) where T : struct
    {
        return ref world.GetPool<T>().Add(entity);
    }

    public static ref T Get<T>(this EcsWorld world, int entity) where T : struct
    {
        return ref world.GetPool<T>().Get(entity);
    }

    public static bool Has<T>(this EcsWorld world, int entity) where T : struct
    {
        return world.GetPool<T>().Has(entity);
    }

    public static bool TryGet<T>(this EcsWorld world, int entity, out T component) where T : struct
    {
        if (world.GetPool<T>().Has(entity))
        {
            component = world.GetPool<T>().Get(entity);
            return true;
        }
        component = default;
        return false;
    }

    public static void Del<T>(this EcsWorld world, int entity) where T : struct
    {
        world.GetPool<T>().Del(entity);
    }
}