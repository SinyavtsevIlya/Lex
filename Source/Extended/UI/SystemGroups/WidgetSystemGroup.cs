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
    [UpdateBefore(typeof(TertiaryWidgetSystemGroup))]
    public class SecondaryWidgetSystemGroup : EcsSystemGroup
    {
    }

    [UpdateInGroup(typeof(WidgetSystemGroup))]
    public class TertiaryWidgetSystemGroup : EcsSystemGroup
    {
    }
}
