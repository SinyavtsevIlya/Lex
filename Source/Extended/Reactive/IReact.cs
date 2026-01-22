namespace Nanory.Lex
{
    public interface IReact<in TReaction> : IReact where TReaction : struct, IEmit
    {
        public void React(TReaction reaction, int entity);

        internal bool IsMatch(int entity, EcsWorldBase world)
        {
            return true;
        }
    }

    public interface IReact<in TReaction, TConstraint> : IReact<TReaction>
        where TReaction : struct, IEmit
        where TConstraint : struct
    {
        bool IReact<TReaction>.IsMatch(int entity, EcsWorldBase world)
        {
            return world.Has<TConstraint>(entity);
        }
    }
    
    public interface IReact<in TReaction, TConstraint, TConstraint2> : IReact<TReaction>
        where TReaction : struct, IEmit
        where TConstraint : struct
        where TConstraint2 : struct
    {
        bool IReact<TReaction>.IsMatch(int entity, EcsWorldBase world)
        {
            return world.Has<TConstraint>(entity) && world.Has<TConstraint2>(entity);
        }
    }
        
    
    public interface IReact { }
}