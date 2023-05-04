namespace Nanory.Lex
{
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
    public class EndPresentationEntityCommandBufferSystem : EntityCommandBufferSystem
    {
    }
    
    [UpdateInGroup(typeof(BeginUiEcbSystemGroup), OrderFirst = true)]
    public class BeginUiUnbindingEcbSystem : EntityCommandBufferSystem
    {
    }
    
    [UpdateInGroup(typeof(BeginUiEcbSystemGroup), OrderLast = true)]
    public class BeginUiBindingEcbSystem : EntityCommandBufferSystem
    {
    }

    [UpdateInGroup(typeof(EndUiEcbSystemGroup), OrderLast = true)]
    public class EndUiBindingEcbSystem : EntityCommandBufferSystem
    {
    }
    
    [UpdateInGroup(typeof(EndUiEcbSystemGroup), OrderFirst = true)]
    public class EndUiUnbindingEcbSystem : EntityCommandBufferSystem
    {
    }
    
    [UpdateInGroup(typeof(UiSystemGroup), OrderFirst = true)]
    public class BeginUiEcbSystemGroup : EcsSystemGroup
    {
    }
    
    [UpdateInGroup(typeof(UiSystemGroup), OrderLast = true)]
    public class EndUiEcbSystemGroup : EcsSystemGroup
    {
    }

    [UpdateInGroup(typeof(UiSystemGroup))]
    public class WidgetsSystemGroup : EcsSystemGroup
    {
    }
    
    
}
