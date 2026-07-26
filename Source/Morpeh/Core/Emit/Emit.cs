using System;
using System.Collections.Generic;

namespace Nanory.Lex
{
    public static class IdEmit<T> where T : struct, IEmit
    {
        public static readonly int Id = EmitTypeRegistry.NextId();
    }

    internal static class EmitTypeRegistry
    {
        private static int _nextId;

        public static int NextId()
        {
            return _nextId++;
        }

        public static int GetLength()
        {
            return _nextId;
        }
    }
}