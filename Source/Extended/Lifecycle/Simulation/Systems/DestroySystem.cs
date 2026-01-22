namespace Nanory.Lex.Lifecycle
{
    [UpdateInGroup(typeof(OneFrameSystemGroup), OrderFirst = true)]
    public sealed class DestroySystem : EcsSystemBase, IReact<DestroyRequest>
    {
        public void React(DestroyRequest _, int entity)
        {
            Emit<DestroyedEvent>(entity);
            World.DelEntity(entity);
        }
    }
}
