namespace Nanory.Lex
{
    public interface IReact<in TReaction> : IReact where TReaction : struct, IEmit
    {
        public void React(TReaction reaction, Entity entity);

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
        
    
    public interface IReact { }
}