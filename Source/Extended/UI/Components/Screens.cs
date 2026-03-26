using System;

namespace Nanory.Lex
{
    public struct Screens : IComponent, IDisposable
    {
        public Replaceables Value;

        public void Dispose()
        {
            Value.Dispose();
        }
    }
}