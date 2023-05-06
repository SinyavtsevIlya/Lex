using System;
using System.Collections.Generic;

namespace Nanory.Lex
{
    public static class UISystemTypesRegistry
    {
        public static Type[] Values = new Type[]
        {
            typeof(BeginUiBindingEcbSystem),
            typeof(BeginUiUnbindingEcbSystem),
            typeof(EndUiEcbSystemGroup),
            typeof(BeginUiEcbSystemGroup),
            typeof(EndUiBindingEcbSystem),
            typeof(EndUiUnbindingEcbSystem),
            typeof(EndPresentationEntityCommandBufferSystem),
            typeof(ScreenSystemGroup),
            typeof(UiSystemGroup)
        };
    }
}
