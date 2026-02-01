namespace Nanory.Lex
{
    public struct Prefab : IComponent
    {
    }
    
    public static class EntityCopyExtensions
    {
        public static bool SetAsPrefab(this World world, Entity entity)
        {
            if (entity.Has<Prefab>())
                return false;

            entity.Add<Prefab>();
            return true;
        }
    }
}

