using System.Collections.Generic;

namespace Nanory.Lex
{
    /// <summary>
    /// Buffer - is a pool-able collection type.
    /// <list type="bullet">
    /// <item>Wraps a <see cref="List{T}"/> inside it.</item>
    /// <item>Implements an automated pooling mechanism, to prevent allocations.</item>
    /// <item>Can be used:</item>
    /// <list type="number">
    /// <item>As a component field: 
    ///     <code>
    ///         public struct SomeComponent { public <see cref="Buffer{TElement}"/> Buffer; }
    ///     </code></item>   
    /// <item>As a component itself (Just by using Add-Component methods)    
    ///     <code>
    ///         <see cref="EcsBufferExtensions.AddBuffer{TElement}(EcsWorld, int)"/>
    ///     </code></item>  
    /// <item>As a standalone helping temporary collection</item>
    /// </list>
    /// <item>If the buffer is used as a component field, then this component must implement an <see cref="IEcsAutoReset{T}"/>, and call Buffer's <see cref="AutoReset(ref Buffer{TElement}) inside it."/></item>
    /// <item>NOTE: All <see cref="Values"/> will be overwritten when the component is added to the entity.</item>
    /// </list>
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    [System.Serializable]
    public struct Buffer<TElement> : IEcsAutoReset<Buffer<TElement>>
    {
        public List<TElement> Values;

        public void AutoReset(ref Buffer<TElement> c)
        {
            if (c.Values == null)
            {
                c.Values = Pool.Pop();

#if DEBUG
                if (c.Values.Count > 0)
                    throw new System.Exception($"Buffer<{typeof(TElement).Name}> Values are not cleared. Values: {System.Environment.NewLine} {c}");
#endif
            }
            else
            {
                c.Values.Clear();
                Pool.Recycle(c.Values);
                c.Values = null;
            }
        }

        public override string ToString()
        {
            if (Values == null)
                return ("Recycled buffer");

            if (Values.Count == 0)
                return ("Empty buffer");

            var result = string.Empty;

            foreach (var item in Values)
            {
                result += item;
                result += System.Environment.NewLine;
            }
            return result;
        }

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
