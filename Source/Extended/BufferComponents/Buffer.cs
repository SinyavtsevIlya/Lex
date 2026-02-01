using System;
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
    ///         public struct SomeComponent : IComponent { public <see cref="Buffer{TElement}"/> Buffer; }
    ///     </code></item>   
    /// <item>As a component itself (Just by using Add-Component methods)    
    ///     <code>
    ///         <see cref="EcsBufferExtensions.AddBuffer{TElement}(World, int)"/>
    ///     </code></item>  
    /// <item>As a standalone helping temporary collection</item>
    /// </list>
    /// <item>If the buffer is used as a component field, then this component must implement an <see cref="IEcsAutoReset{T}"/>, and call Buffer's <see cref="AutoReset(ref Buffer{TElement}) inside it."/></item>
    /// <item>NOTE: All <see cref="Values"/> will be overwritten when the component is added to the entity.</item>
    /// </list>
    /// </summary>
    /// <typeparam name="TElement"></typeparam>
    [System.Serializable]
    public struct Buffer<TElement> : IComponent, IDisposable
    {
        public List<TElement> Values;

        public void Dispose()
        {
            if (Values == null)
            {
                Values = Pool.Pop();

#if DEBUG
                if (Values.Count > 0)
                    throw new Exception($"Buffer<{typeof(TElement).Name}> Values are not cleared. Values: {System.Environment.NewLine} {this}");
#endif
            }
            else
            {
                Values.Clear();
                Pool.Recycle(Values);
                Values = null;
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

        public static implicit operator List<TElement>(Buffer<TElement> buffer) => buffer.Values;

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
            return ref buffer;
        }
    }
}
