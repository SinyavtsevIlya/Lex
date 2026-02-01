using UnityEngine;

namespace Nanory.Lex.Timer
{
    [UpdateInGroup(typeof(TimersSystemGroup))]
    public class TimerSystem : EcsRunSystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var timerEntity in Filter()
            .With<Timer>()
            .With<TimerOwnerLink>()
            .Build())
            {
                ref var timer = ref Get<Timer>(timerEntity);
                ref var timerOwnerLink = ref Get<TimerOwnerLink>(timerEntity);

                timer.CurrentTime -= Time.deltaTime;

                if (!(timer.CurrentTime <= 0f)) 
                    continue;
                
                if (World.Has(timerOwnerLink.Value)) 
                    timer.TimerContextStash.Set(timerOwnerLink.Value);

                if (timer.IsInfinity == 0)
                    World.RemoveEntity(timerEntity);
                else
                    timer.CurrentTime = timer.Duration;
            }
        }
    }
}
