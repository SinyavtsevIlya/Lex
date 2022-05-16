namespace Nanory.Lex.Timer
{
    public struct Timer
    {
        public float CurrentTime;
        public float Duration;
        public int IsInfinity;
        public int TimerContextComponentIndex;

        public Timer(float duration, bool isInfinity, int timerContextComponentIndex)
        {
            Duration = duration;
            CurrentTime = duration;
            TimerContextComponentIndex = timerContextComponentIndex;
            IsInfinity = isInfinity ? 1 : 0;
        }
    }

    public struct TimerOwnerLink
    {
        public EcsPackedEntity Value;
    }
}