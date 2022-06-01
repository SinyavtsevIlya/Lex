using System.Collections.Generic;

namespace Nanory.Lex.Stats
{
    [UpdateInGroup(typeof(StatSystemGroup))]
    public class ClampStatSystem<TStatComponent, TStatMaxComponent> : EcsSystemBase
        where TStatComponent : struct, IStat
        where TStatMaxComponent : struct, IStat
    {
        protected override void OnUpdate()
        {
            foreach (var statReceiverEntity in Filter()
            .With<TStatComponent>()
            .With<TStatMaxComponent>()
            .End())
            {
                ref var stat = ref Get<TStatComponent>(statReceiverEntity);
                ref var statmax = ref Get<TStatMaxComponent>(statReceiverEntity);

                if (stat.StatValue > statmax.StatValue)
                {
                    stat.StatValue = statmax.StatValue;
                }

                if (stat.StatValue < 0)
                {
                    stat.StatValue = 0;
                }
            }
        }
    }
}
