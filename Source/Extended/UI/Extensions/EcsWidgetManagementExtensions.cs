using UnityEngine;

namespace Nanory.Lex
{
    public static class EcsWidgetManagementExtensions
    {
        public static void BindWidget<TWidget>(this EcsSystemBase system, int ownerEntity, TWidget widget) where TWidget : MonoBehaviour
        {
            var world = system.World;
            var beginSyncPoint = system.GetCommandBufferFrom<BeginWidgetEntityCommandBufferSystem>();
            var endSyncPoint = system.GetCommandBufferFrom<EndWidgetCreationEntityCommandBufferSystem>();

            world.Add<BindingDirtyTag>(world.NewEntity());
            
            beginSyncPoint.Add<BindEvent<TWidget>>(ownerEntity).Value = widget;

            // It's important to enforce the reversed order of add/remove operations of Mono<TWidget> components.
            // In the situation when two widgets of the same type are being swapped in the same tick
            // (e.g. health bar widget may exist on both core-screen and inventory screen)
            // We always first want to remove the old one, and then add the new one. If we try to make it 
            // in a different way, we will simply remove all widgets from the entity.
            // Thats why we schedule the add operation using widgetSyncPoint. 

            endSyncPoint.Add<Mono<TWidget>>(ownerEntity).Value = widget;
            endSyncPoint.Del<BindEvent<TWidget>>(ownerEntity);
        }

        public static void UnbindWidget<TWidget>(this EcsSystemBase system, int ownerEntity, TWidget widget) where TWidget : MonoBehaviour
        {
            var world = system.World;
            var beginSyncPoint = system.GetCommandBufferFrom<BeginWidgetEntityCommandBufferSystem>();
            var endSyncPoint = system.GetCommandBufferFrom<EndWidgetDestructionEntityCommandBufferSystem>();
            
            world.Add<BindingDirtyTag>(world.NewEntity());

            beginSyncPoint.Add<UnbindEvent<TWidget>>(ownerEntity).Value = widget;
            endSyncPoint.Del<Mono<TWidget>>(ownerEntity);

            endSyncPoint.Del<UnbindEvent<TWidget>>(ownerEntity);
        }
    }
}
