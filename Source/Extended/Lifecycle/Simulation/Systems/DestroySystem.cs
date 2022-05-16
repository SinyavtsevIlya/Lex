namespace Nanory.Lex.Lifecycle
{
    [UpdateInGroup(typeof(OneFrameSystemGroup), OrderFirst = true)]
    public sealed class DestroySystem : EcsSystemBase
    {
        protected override void OnUpdate()
        {
            var later = GetCommandBufferFrom<BeginSimulationECBSystem>();

            foreach (var destoyedEntity in Filter()
            .With<DestroyedEvent>()
            .End())
            {
                later.DelEntity(destoyedEntity);
            }
        }
    }
}
