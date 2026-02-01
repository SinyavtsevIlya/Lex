namespace Nanory.Lex.Timer
{
    public struct Timer : IComponent {
        public float CurrentTime;
        public float Duration;
        public int IsInfinity;
        public IStash TimerContextStash;

        public Timer(float duration, bool isInfinity, IStash timerContextStash)
        {
            Duration = duration;
            CurrentTime = duration;
            TimerContextStash = timerContextStash;
            IsInfinity = isInfinity ? 1 : 0;
        }
    }

    public struct TimerOwnerLink : IComponent {
        public Entity Value;
    }
}