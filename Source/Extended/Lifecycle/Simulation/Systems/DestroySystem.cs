namespace Nanory.Lex.Lifecycle
{
    public sealed class DestroySystem : EcsSystemBase, IReact<DestroyRequest>
    {
        public void React(DestroyRequest _, Entity entity)
        {
            Emit<DestroyedEvent>(entity);
            World.RemoveEntity(entity);
        }
    }
}
