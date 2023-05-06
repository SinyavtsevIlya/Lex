using UnityEngine;

namespace Nanory.Lex
{
    public static class EcsWidgetManagementExtensions
    {
        public static void BindOrUnbind<TWidget>(this EcsSystemBase system, int ownerEntity, TWidget widget, bool value) where TWidget : MonoBehaviour
        {
            if (value)
                BindWidget(system, ownerEntity,widget);
            else 
                UnbindWidget(system,ownerEntity,widget);
        }
        
        public static void BindWidget<TWidget>(this EcsSystemBase system, int ownerEntity, TWidget widget) where TWidget : MonoBehaviour
        {
            var beginSyncPoint = system.GetCommandBufferFrom<BeginUiBindingEcbSystem>();
            var endSyncPoint = system.GetCommandBufferFrom<EndUiBindingEcbSystem>();

            beginSyncPoint.Add<BindEvent<TWidget>>(ownerEntity).Value = widget;
            endSyncPoint.Add<Mono<TWidget>>(ownerEntity).Value = widget;
            endSyncPoint.Del<BindEvent<TWidget>>(ownerEntity);
        }

        public static void UnbindWidget<TWidget>(this EcsSystemBase system, int ownerEntity, TWidget widget) where TWidget : MonoBehaviour
        {
            var beginSyncPoint = system.GetCommandBufferFrom<BeginUiUnbindingEcbSystem>();
            var endSyncPoint = system.GetCommandBufferFrom<EndUiUnbindingEcbSystem>();

            beginSyncPoint.Add<UnbindEvent<TWidget>>(ownerEntity).Value = widget;
            endSyncPoint.Del<Mono<TWidget>>(ownerEntity);
            endSyncPoint.Del<UnbindEvent<TWidget>>(ownerEntity);
        }
    }
}
