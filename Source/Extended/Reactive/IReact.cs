using System;

namespace Nanory.Lex
{
    public interface IReact<in TReaction> : IReact where TReaction : struct, IEmit
    {
        public void React(TReaction emission, Entity entity);

        internal bool IsMatch(Entity entity, World world)
        {
            return true;
        }
    }

    public interface IReact<in TReaction, TConstraint> : IReact<TReaction>
        where TReaction : struct, IEmit
        where TConstraint : struct, IComponent
    {
        bool IReact<TReaction>.IsMatch(Entity entity, World world)
        {
            return world.Has<TConstraint>(entity);
        }
    }
    
    public interface IReact<in TReaction, TConstraint, TConstraint2> : IReact<TReaction>
        where TReaction : struct, IEmit
        where TConstraint : struct, IComponent
        where TConstraint2 : struct, IComponent
    {
        bool IReact<TReaction>.IsMatch(Entity entity, World world)
        {
            return world.Has<TConstraint>(entity) && world.Has<TConstraint2>(entity);
        }
    }
    
    public interface IReact<in TReaction, TConstraint, TConstraint2, TConstraint3> : IReact<TReaction>
        where TReaction : struct, IEmit
        where TConstraint : struct, IComponent
        where TConstraint2 : struct, IComponent
        where TConstraint3 : struct, IComponent
    {
        bool IReact<TReaction>.IsMatch(Entity entity, World world)
        {
            return world.Has<TConstraint>(entity) && 
                   world.Has<TConstraint2>(entity) &&
                   world.Has<TConstraint3>(entity);
        }
    }
        
    
    public interface IReact { }

    public class ReactionOrderAttribute : Attribute
    {
        public Type EmissionType;
        public int Priority;

        public ReactionOrderAttribute(Type emissionType, int priority)
        {
            EmissionType = emissionType;
            Priority = priority;
        }
    }
}