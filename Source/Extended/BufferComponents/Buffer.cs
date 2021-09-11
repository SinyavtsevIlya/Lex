using System.Collections.Generic;

namespace Nanory.Lex
{
    public struct Buffer<TElement> : IEcsAutoReset<Buffer<TElement>>
    {
        public List<TElement> Values;

        public void AutoReset(ref Buffer<TElement> c)
        {
            if (c.Values != null)
                Pool.Push(c.Values);
        }

        public static class Pool
        {
            public static Stack<List<TElement>> Values = new Stack<List<TElement>>(64);

            public static void Push(List<TElement> elements)
            {
                Values.Push(elements);
            }

            public static List<TElement> Pop()
            {
                if (Values.Count > 0)
                {
                    return Values.Pop();
                }
                else
                {
                    return new List<TElement>();
                }
            }
        }
    }

    public static class EcsBufferExtensions
    {
        public static ref Buffer<TElement> AddBuffer<TElement>(this EcsWorld world, int entity) where TElement : struct
        {
            ref var buffer = ref world.GetPool<Buffer<TElement>>().Add(entity);
            buffer.Values = Buffer<TElement>.Pool.Pop();
            return ref buffer;
        }

        public static ref Buffer<TElement> AddBuffer<TElement>(this EcsWorld world, int entity, TElement initialElement) where TElement : struct
        {
            ref var buffer = ref AddBuffer<TElement>(world, entity);
            buffer.Values.Add(initialElement);
            return ref buffer;
        }

        public static void RemoveBuffer<TElement>(this EcsWorld world, int entity) where TElement : struct
        {
            world.GetPool<Buffer<TElement>>().Del(entity);
        }
    }
}
