namespace Nanory.Lex
{
    [UpdateInGroup(typeof(UIPresentationSystemGroup))]
    public class WidgetSystemGroup : EcsSystemGroup 
    {
    }

    [UpdateInGroup(typeof(WidgetSystemGroup))]
    [UpdateBefore(typeof(SecondaryWidgetSystemGroup))]
    public class PrimaryWidgetSystemGroup : EcsSystemGroup
    {
    }

    [UpdateInGroup(typeof(WidgetSystemGroup))]
    public class SecondaryWidgetSystemGroup : EcsSystemGroup
    {
    }
}
