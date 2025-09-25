using System;

namespace Nanory.Lex
{
    public class WithAttribute : Attribute
    {
        public Type WithType;

        public WithAttribute(Type withType)
        {
            WithType = withType;
        }
    }

    public abstract class EcsReactiveSystemBase : EcsSystemBase
    {
    }
    
    public abstract class EcsReactiveSystemBase<TReaction> : EcsReactiveSystemBase
    {
        public abstract void OnUpdate(int entity, in TReaction component);
    }

    public static class EcsReactiveSystemExtensions
    {

    }
}