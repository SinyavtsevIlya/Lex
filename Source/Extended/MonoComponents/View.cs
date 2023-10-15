namespace Nanory.Lex
{
    public struct View<TView> : IEcsAutoReset<View<TView>>
    {
        public TView Value;

        public void AutoReset(ref View<TView> c)
        {
            c.Value = default;
        }
    }
}