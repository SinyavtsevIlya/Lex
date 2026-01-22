using UnityEngine;

namespace Nanory.Lex.Timer
{
    [UpdateInGroup(typeof(TimersSystemGroup))]
    public class TimerSystem : EcsSystemBase
    {
        protected override void OnUpdate()
        {
            foreach (var timerEntity in Filter()
            .With<Timer>()
            .With<TimerOwnerLink>()
            .End())
            {
                ref var timer = ref Get<Timer>(timerEntity);
                ref var timerOwnerLink = ref Get<TimerOwnerLink>(timerEntity);

                timer.CurrentTime -= Time.deltaTime;

                if (timer.CurrentTime <= 0f)
                {
                    if (timerOwnerLink.Value.Unpack(World, out var ownerEntity))
                    {
                        World.PoolsSparse[timer.TimerContextComponentIndex].Activate(ownerEntity);
                    }

                    if (timer.IsInfinity == 0)
                    {
                        World.DelEntity(timerEntity);
                    }
                    else
                    {
                        timer.CurrentTime = timer.Duration;
                    }
                }
            }
        }
    }
}
