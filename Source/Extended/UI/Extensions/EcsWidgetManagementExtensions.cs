using UnityEngine;

namespace Nanory.Lex
{
    public static class EcsWidgetManagementExtensions
    {
        public static void BindWidget<TWidget>(this EcsSystemBase system, int ownerEntity, TWidget widget) where TWidget : MonoBehaviour
        {
            var world = system.World;
            var later = system.GetCommandBufferFrom<EndPresentationEntityCommandBufferSystem>();

            world.Add<BindEvent<TWidget>>(ownerEntity).Value = widget;

            // It's important to enforce the reversed order of add/remove operations of Mono<TWidget> components.
            // In the situation when two widgets of the same type are being swapped in the same tick
            // (e.g. health bar widget may exist on both core-screen and inventory screen)
            // We always first want to remove the old one, and then add the new one. If we try to make it 
            // in a different way, we will simply remove all widgets from the entity.
            // Thats why we schedule the add operation using widgetSyncPoint. 

            var widgetSyncPoint = system.GetCommandBufferFrom<WidgetEntityCommandBufferSystem>();
            widgetSyncPoint.Add<Mono<TWidget>>(ownerEntity).Value = widget;

            later.Del<BindEvent<TWidget>>(ownerEntity);
        }

        public static void UnbindWidget<TWidget>(this EcsSystemBase system, int ownerEntity, TWidget widget) where TWidget : MonoBehaviour
        {
            var world = system.World;
            var later = system.GetCommandBufferFrom<EndPresentationEntityCommandBufferSystem>();

            world.Add<UnbindEvent<TWidget>>(ownerEntity).Value = widget;
            world.Del<Mono<TWidget>>(ownerEntity);

            later.Del<UnbindEvent<TWidget>>(ownerEntity);
        }
    }
}
