using UnityEngine;

namespace Nanory.Lex
{
    public static class EcsWidgetManagementExtensions
    {
        public static void BindWidget<TWidget>(this EcsSystemBase system, int ownerEntity, TWidget widget) where TWidget : MonoBehaviour
        {
            var beginSyncPoint = system.GetCommandBufferFrom<BeginWidgetBindingEcbSystem>();
            var endSyncPoint = system.GetCommandBufferFrom<EndWidgetBindingEcbSystem>();

            beginSyncPoint.Add<BindEvent<TWidget>>(ownerEntity).Value = widget;
            endSyncPoint.Add<Mono<TWidget>>(ownerEntity).Value = widget;
            endSyncPoint.Del<BindEvent<TWidget>>(ownerEntity);
        }

        public static void UnbindWidget<TWidget>(this EcsSystemBase system, int ownerEntity, TWidget widget) where TWidget : MonoBehaviour
        {
            var beginSyncPoint = system.GetCommandBufferFrom<BeginWidgetUnbindingEcbSystem>();
            var endSyncPoint = system.GetCommandBufferFrom<EndWidgetUnbindingEcbSystem>();

            beginSyncPoint.Add<UnbindEvent<TWidget>>(ownerEntity).Value = widget;
            endSyncPoint.Del<Mono<TWidget>>(ownerEntity);
            endSyncPoint.Del<UnbindEvent<TWidget>>(ownerEntity);
        }
    }
}
