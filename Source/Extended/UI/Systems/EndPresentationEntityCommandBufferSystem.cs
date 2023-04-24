namespace Nanory.Lex
{
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public class EndPresentationEntityCommandBufferSystem : EntityCommandBufferSystem
    {
    }
    
    [UpdateInGroup(typeof(WidgetSystemGroup), OrderFirst = true)]
    public class BeginWidgetEntityCommandBufferSystem : EntityCommandBufferSystem
    {
    }

    [UpdateInGroup(typeof(EndWidgetEntityCommandBuffersSystemGroup), OrderLast = true)]
    public class EndWidgetCreationEntityCommandBufferSystem : EntityCommandBufferSystem
    {
    }
    
    [UpdateInGroup(typeof(EndWidgetEntityCommandBuffersSystemGroup), OrderFirst = true)]
    public class EndWidgetDestructionEntityCommandBufferSystem : EntityCommandBufferSystem
    {
    }
    
    [UpdateInGroup(typeof(WidgetSystemGroup), OrderLast = true)]
    public class EndWidgetEntityCommandBuffersSystemGroup : EcsSystemGroup
    {
    }
}
