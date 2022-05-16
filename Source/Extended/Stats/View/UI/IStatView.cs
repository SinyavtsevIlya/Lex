namespace Nanory.Lex.Stats
{
    public interface IStatView
    {
        IStatView SetMaxValue(int maxValue);
        IStatView SetValue(int value);
        void Dispose();
    }
}
