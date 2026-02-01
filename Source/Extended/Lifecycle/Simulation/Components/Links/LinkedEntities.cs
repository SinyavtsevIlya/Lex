using System;

namespace Nanory.Lex.Lifecycle
{
    public struct LinkedEntities : IComponent, IDisposable
 {
        public Buffer<Entity> Buffer;
        public void Dispose()
        {
            Buffer.Dispose();
        }
    }
}