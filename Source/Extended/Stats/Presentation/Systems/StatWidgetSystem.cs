using UnityEngine;

namespace Nanory.Lex.Stats
{
    [UpdateInGroup(typeof(WidgetSystemGroup))]
    public class StatWidgetSystem<TStat, TStatChanged, TStatMax, TStatMaxChanged, TStatWidget> : WidgetSystemBase
        where TStat : struct, IStat
        where TStatChanged : struct
        where TStatMax : struct, IStat
        where TStatMaxChanged : struct
        where TStatWidget : MonoBehaviour, IStatView
    {
        protected override void OnUpdate()
        {
            foreach (var ownerEntity in Filter()
                         .With<TStat>()
                         .With<TStatMax>()
                         .With<TStatMaxChanged>()
                         .With<Mono<TStatWidget>>()
                         .End())
            {
                var widget = Get<Mono<TStatWidget>>(ownerEntity).Value;
                var maxValue = Get<TStatMax>(ownerEntity).StatValue;

                widget
                    .SetMaxValue(maxValue);
            }
            
            foreach (var ownerEntity in Filter()
                         .With<TStat>()
                         .With<TStatMax>()
                         .With<TStatChanged>()
                         .With<Mono<TStatWidget>>()
                         .End())
            {
                var widget = Get<Mono<TStatWidget>>(ownerEntity).Value;
                var statValue = Get<TStat>(ownerEntity).StatValue;
                
                widget
                    .SetValue(statValue);
            }
        }

        protected override void OnBind()
        {
            foreach (var ownerEntity in Filter()
                         .With<BindEvent<TStatWidget>>()
                         .End())
            {
                var widget = Get<BindEvent<TStatWidget>>(ownerEntity).Value;

                var statValue = Get<TStat>(ownerEntity).StatValue;
                var maxValue = Get<TStatMax>(ownerEntity).StatValue;

                widget
                    .SetMaxValue(maxValue)
                    .SetValue(statValue);
            }
        }

        protected override void OnUnbind()
        {
            foreach (var ownerEntity in Filter()
                         .With<UnbindEvent<TStatWidget>>()
                         .End())
            {
                var widget = Get<UnbindEvent<TStatWidget>>(ownerEntity).Value;
                widget.Dispose();
            }
        }
    }
}
