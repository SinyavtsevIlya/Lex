using System;

namespace Nanory.Lex
{
    public class WithAttribute : Attribute
    {
        public Type[] WithType;

        public WithAttribute(params Type[] withType)
        {
            WithType = withType;
        }
    }
    
    public class WithoutAttribute : Attribute
    {
        public Type[] WithoutType;

        public WithoutAttribute(params Type[] withoutType)
        {
            WithoutType = withoutType;
        }
    }

    public abstract class EcsReactiveSystemBase : EcsSystemBase
    {
    }
    
    public abstract class EcsReactiveSystemBase<TReaction> : EcsReactiveSystemBase
    {
        public virtual void React(Entity entity, in TReaction component)
        {
            OnReact(entity, in component);
        }
        
        protected abstract void OnReact(Entity entity, in TReaction component);
    }
}