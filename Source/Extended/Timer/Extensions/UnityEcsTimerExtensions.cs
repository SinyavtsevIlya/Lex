using Nanory.Lex;

namespace Nanory.Lex.Timer
{
    public static class EcsTimerExtensions
    {
        /// <summary>
        /// Creates a child timer entity, and after specified duration destroys or restarts it.
        /// </summary>
        /// <typeparam name="TContext">Context Tag which can be queried. Should be struct</typeparam>
        /// <param name="ecb">Entity Command Buffer to perform operations</param>
        /// <param name="duration">Timer duration in seconds</param>
        /// <param name="ownerEntity">Owner of the timer</param>
        /// <param name="notifyOwner">If true, adds TimerCompletedEvent on the owner, otherwise - on the timer itself</param>
        /// <param name="isInfinity">If true, restarts the timer when time is out</param>
        /// <returns></returns>
        public static int AddDelayed<TContext>(this EntityCommandBuffer ecb,
                                                 float duration,
                                                 int ownerEntity,
                                                 bool isInfinity = false) where TContext : struct
        {
            var timerEntity = ecb.DstWorld.NewEntity();

            var timerContextComponentIndex = EcsComponent<TContext>.TypeIndex;
            ecb.Add<Timer>(timerEntity) = new Timer(duration, isInfinity, timerContextComponentIndex);
            ecb.Add<TimerOwnerLink>(timerEntity) = new TimerOwnerLink { Value = ecb.DstWorld.PackEntity(ownerEntity) };

            return timerEntity;
        }
    }
}
