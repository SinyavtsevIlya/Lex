namespace Nanory.Lex.Lifecycle
{
    [UpdateInGroup(typeof(OneFrameSystemGroup), OrderFirst = true)]
    public sealed class DestroySystem : EcsRunSystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var destroyedEntity in Filter()
                        .With<DestroyedEvent>()
                        .End())
            {
                Later.DelEntity(destroyedEntity);
            }
            
            foreach (var requestEntity in Filter()
                         .With<DestroyRequest>()
                         .End())
            {
                Later.Add<DestroyedEvent>(requestEntity);
                Del<DestroyRequest>(requestEntity);
            }
        }
    }
}
