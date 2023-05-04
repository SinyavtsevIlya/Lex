using System;
using System.Collections.Generic;

namespace Nanory.Lex
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class UIPresentationSystemGroup : EcsSystemGroup { }

    public static class UISystemTypesRegistry
    {
        public static Type[] Values = new Type[]
        {
            typeof(BeginWidgetBindingEcbSystem),
            typeof(BeginWidgetUnbindingEcbSystem),
            typeof(EndWidgetEcbSystemGroup),
            typeof(BeginWidgetEcbSystemGroup),
            typeof(EndWidgetBindingEcbSystem),
            typeof(EndWidgetUnbindingEcbSystem),
            typeof(EndPresentationEntityCommandBufferSystem),
            typeof(ScreenSystemGroup),
            typeof(WidgetSystemGroup)
        };
    }
}
