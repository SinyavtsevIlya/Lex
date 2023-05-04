namespace Nanory.Lex
{
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public class EndPresentationEntityCommandBufferSystem : EntityCommandBufferSystem
    {
    }
    
    [UpdateInGroup(typeof(BeginWidgetEcbSystemGroup), OrderFirst = true)]
    public class BeginWidgetUnbindingEcbSystem : EntityCommandBufferSystem
    {
    }
    
    [UpdateInGroup(typeof(BeginWidgetEcbSystemGroup), OrderLast = true)]
    public class BeginWidgetBindingEcbSystem : EntityCommandBufferSystem
    {
    }

    [UpdateInGroup(typeof(EndWidgetEcbSystemGroup), OrderLast = true)]
    public class EndWidgetBindingEcbSystem : EntityCommandBufferSystem
    {
    }
    
    [UpdateInGroup(typeof(EndWidgetEcbSystemGroup), OrderFirst = true)]
    public class EndWidgetUnbindingEcbSystem : EntityCommandBufferSystem
    {
    }
    
    [UpdateInGroup(typeof(WidgetSystemGroup), OrderFirst = true)]
    public class BeginWidgetEcbSystemGroup : EcsSystemGroup
    {
    }
    
    [UpdateInGroup(typeof(WidgetSystemGroup), OrderLast = true)]
    public class EndWidgetEcbSystemGroup : EcsSystemGroup
    {
    }

    [UpdateInGroup(typeof(WidgetSystemGroup))]
    public class WidgetsSystemGroup : EcsSystemGroup
    {
    }
    
    
}
