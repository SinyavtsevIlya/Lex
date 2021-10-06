namespace Nanory.Lex
{
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public class EndPresentationEntityCommandBufferSystem : EntityCommandBufferSystem
    {
    }

    [UpdateInGroup(typeof(WidgetSystemGroup), OrderFirst = true)]
    public class WidgetEntityCommandBufferSystem : EntityCommandBufferSystem
    {
    }
}
