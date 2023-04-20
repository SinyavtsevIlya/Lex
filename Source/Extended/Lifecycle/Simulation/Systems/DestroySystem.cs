namespace Nanory.Lex.Lifecycle
{
    [UpdateInGroup(typeof(OneFrameSystemGroup), OrderFirst = true)]
    public sealed class DestroySystem : EcsSystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var destroyedEntity in Filter()
            .With<DestroyedEvent>()
            .End())
            {
                Later.DelEntity(destroyedEntity);
            }
        }
    }
}
