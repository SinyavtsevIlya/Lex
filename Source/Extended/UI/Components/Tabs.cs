using System;

namespace Nanory.Lex
{
    public struct Tabs : IComponent, IDisposable
    {
        public Replaceables Value;

        public void Dispose()
        {
            Value.Dispose();
        }
    }
}