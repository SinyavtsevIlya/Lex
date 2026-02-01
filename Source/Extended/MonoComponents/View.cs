using System;

namespace Nanory.Lex
{
    public struct View<TView> : IComponent, IDisposable
    {
        public TView Value;

        public void Dispose()
        {
            Value = default;
        }
    }
}