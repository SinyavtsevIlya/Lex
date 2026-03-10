using System;
using System.Collections.Generic;

namespace Nanory.Lex
{
    public static class IdEmit<T> where T : struct, IEmit {
        public static readonly int Id;

        static IdEmit() {
            Id = EmitTypeRegistry.Register(typeof(T));
        }
    }
    
    internal static class EmitTypeRegistry {
        private static int _nextId;
        private static readonly Dictionary<Type, int> _ids = new();

        public static int Register(Type type) {
            if (_ids.TryGetValue(type, out var id))
                return id;

            id = _nextId++;
            _ids[type] = id;
            return id;
        }

        public static int GetLength() => _nextId;
    }
}