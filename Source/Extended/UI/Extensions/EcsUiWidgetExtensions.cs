using System;
using UnityEngine;

namespace Nanory.Lex
{
    public static class EcsUiWidgetExtensions
    {
        public static void BindOrUnbind<TWidget>(this EcsSystemBase system, Entity ownerEntity, TWidget widget, bool value) where TWidget : MonoBehaviour
        {
            if (value)
                BindWidget(system, ownerEntity,widget);
            else 
                UnbindWidget(system,ownerEntity,widget);
        }
        
        public static void BindWidget<TWidget>(this EcsSystemBase system, Entity ownerEntity, TWidget widget) where TWidget : MonoBehaviour
        {
            if (system.Has<Mono<TWidget>>(ownerEntity))
                throw new WidgetAlreadyBoundException(ownerEntity, system.Get<Mono<TWidget>>(ownerEntity).Value, widget);
            
            ownerEntity.Add<Mono<TWidget>>().Value = widget;
            system.World.Emit(ownerEntity, new BindEvent<TWidget> { Value = widget });
        }

        public static void UnbindWidget<TWidget>(this EcsSystemBase system, Entity ownerEntity, TWidget widget) where TWidget : MonoBehaviour
        {
            ref var mono = ref system.World.GetStash<Mono<TWidget>>().Get(ownerEntity, out var hasMono); 
            
            if (!hasMono)
                throw new WidgetIsNotBoundException(ownerEntity, widget);
            
            if (mono.Value != widget)
                throw new WidgetMismatchException(ownerEntity, mono.Value, widget);
            
            system.World.Emit(ownerEntity, new UnbindEvent<TWidget> { Value = widget });
            ownerEntity.RemoveComponent<Mono<TWidget>>();
        }

        public class WidgetAlreadyBoundException : Exception
        {
            public WidgetAlreadyBoundException(Entity entity, MonoBehaviour mono, MonoBehaviour newMono)
                : base($"{entity} is already bound to {mono}. Cannot bind {newMono}.")
            {
            }
        }

        public class WidgetIsNotBoundException : Exception
        {
            public WidgetIsNotBoundException(Entity entity, MonoBehaviour mono)
                : base($"{entity} is not bound to {mono}.")
            {
            }
        }

        public class WidgetMismatchException : Exception
        {
            public WidgetMismatchException(Entity entity, MonoBehaviour bound, MonoBehaviour notBound)
                : base($"{entity} is bound to {bound} not to {notBound}")
            {
            }
        }
    }
}
