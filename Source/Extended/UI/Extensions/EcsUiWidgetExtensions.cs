using UnityEngine;

namespace Nanory.Lex
{
    public static class EcsUiWidgetExtensions
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
            system.World.Add<Mono<TWidget>>(ownerEntity).Value = widget;
            system.World.Emit(ownerEntity, new BindEvent<TWidget> { Value = widget });
        }

        public static void UnbindWidget<TWidget>(this EcsSystemBase system, int ownerEntity, TWidget widget) where TWidget : MonoBehaviour
        {
            system.World.Emit(ownerEntity, new UnbindEvent<TWidget> { Value = widget });
            system.World.Del<Mono<TWidget>>(ownerEntity);
        }
    }
}
