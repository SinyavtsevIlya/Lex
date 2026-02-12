using System;
using System.Collections.Generic;

namespace Nanory.Lex
{
    [Serializable]
    public struct Buffer<TElement> : IComponent, IDisposable
    {
        internal List<TElement> _values;

        public List<TElement> Values => this.Values();

        public void Dispose()
        {
            _values.Clear();
            Pool.Recycle(_values);
            _values = null;
        }

        public override string ToString()
        {
            if (_values == null)
                return ("Recycled buffer");

            if (_values.Count == 0)
                return ("Empty buffer");

            var result = string.Empty;

            foreach (var item in _values)
            {
                result += item;
                result += System.Environment.NewLine;
            }
            return result;
        }
        
        public Enumerator GetEnumerator()
        {
            if (_values == null)
            {
                _values = Pool.Pop();
#if DEBUG
                if (_values.Count > 0)
                    throw new Exception(
                        $"Buffer<{typeof(TElement).Name}> Values are not cleared.");
#endif
            }

            return new Enumerator(_values);
        }

        public struct Enumerator
        {
            private List<TElement>.Enumerator _enumerator;

            internal Enumerator(List<TElement> values)
            {
                _enumerator = values != null
                    ? values.GetEnumerator()
                    : default;
            }

            public TElement Current => _enumerator.Current;

            public bool MoveNext() => _enumerator.MoveNext();
        }

        public static implicit operator List<TElement>(Buffer<TElement> buffer) => buffer._values;

        public static class Pool
        {
            public static Stack<List<TElement>> Values = new Stack<List<TElement>>(64);

            public static void Recycle(List<TElement> elements)
            {
                elements.Clear();
                Values.Push(elements);
            }

            public static List<TElement> Pop()
            {
                return Values.Count > 0 ?
                    Values.Pop() : new List<TElement>();
            }
        }
    }

    public static class EcsBufferExtensions
    {
        public static ref Buffer<TElement> AddBuffer<TElement>(this World world, Entity entity) where TElement : struct
        {
            ref var buffer = ref world.GetStash<Buffer<TElement>>().Add(entity);
            buffer.InitializeBuffer();
            return ref buffer;
        }

        public static List<TElement> Values<TElement>(this Buffer<TElement> buffer)
        {
            if (buffer._values == null)
            {
                buffer.InitializeBuffer();
            }

            return buffer._values;
        }
        
        private static List<TElement> InitializeBuffer<TElement>(this Buffer<TElement> buffer)
        {
            var values = Buffer<TElement>.Pool.Pop();
            buffer._values = values;
#if DEBUG
            if (values.Count > 0)
                throw new Exception(
                    $"Buffer<{typeof(TElement).Name}> Values are not cleared. Values: {Environment.NewLine} {buffer}");
#endif
            return values;
        }
    }
}
