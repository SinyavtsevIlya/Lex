using System;
using System.Collections.Generic;

namespace Nanory.Lex
{
    public class UpdateBefore : Attribute
    {
        public Type TargetSystemType;
        public UpdateBefore(Type targetSystemType) => TargetSystemType = targetSystemType;
    }

    public class UpdateInGroup : Attribute
    {
        public Type TargetGroupType;
        public bool OrderLast;
        public bool OrderFirst;
        public UpdateInGroup(Type targetGroupType) => TargetGroupType = targetGroupType;
    }

    public class RootSystemGroup : SystemsGroupPosition { }

    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public class InitializationSystemGroup : SystemsGroupPosition { }

    [UpdateInGroup(typeof(RootSystemGroup))]
    [UpdateBefore(typeof(PresentationSystemGroup))]
    public class SimulationSystemGroup : SystemsGroupPosition { }

    [UpdateInGroup(typeof(RootSystemGroup))]
    public class PresentationSystemGroup : SystemsGroupPosition { }

    public abstract class SystemsGroupPosition { }
    
    public abstract class EcsRunSystemBase : EcsSystemBase, ISystem
    {
        public void OnUpdate(float deltaTime)
        {
            OnUpdate();
        }

        protected abstract void OnUpdate();
    }
    
    public abstract class EcsSystemBase : IInitializer
    {
        public World World { get; set; }

        public void OnAwake()
        {
            OnCreate();
        }
        public void Dispose()
        {
            OnDispose();
        }

        protected virtual void OnCreate()
        {
        }
        
        protected virtual void OnDispose()
        {
        }

        public Entity CreateEntity()
        {
            return World.CreateEntity();
        }

        public bool IsDisposed(in Entity entity)
        {
            return World.IsDisposed(entity);
        }
        
        public bool Has(in Entity entity)
        {
            return !World.IsDisposed(entity);
        }

        public ref TComponent Get<TComponent>(Entity entity) where TComponent : struct, IComponent
        {
            return ref World.GetStash<TComponent>().Get(entity);
        }
        
        public ref TComponent Get<TComponent>(Entity entity, out bool has) where TComponent : struct, IComponent
        {
            return ref World.GetStash<TComponent>().Get(entity, out has);
        }

        public ref TComponent Set<TComponent>(Entity entity) where TComponent : struct, IComponent
        {
            var stash = World.GetStash<TComponent>();
            
            if (stash.Has(entity))
            {
                return ref stash.Get(entity);
            }
            
            return ref stash.Add(entity);
        }

        public bool TryGet<T>(Entity entity, out T component) where T : struct, IComponent
        {
            if (World.GetStash<T>().Has(entity))
            {
                component = World.GetStash<T>().Get(entity);
                return true;
            }
            component = default;
            return false;
        }

        public ref TComponent Add<TComponent>(Entity entity) where TComponent : struct, IComponent
        {
            return ref World.GetStash<TComponent>().Add(entity);
        }

        public bool Has<TComponent>(Entity entity) where TComponent : struct, IComponent
        {
            return World.GetStash<TComponent>().Has(entity);
        }

        public bool Del<TComponent>(Entity entity) where TComponent : struct, IComponent
        {
            return World.GetStash<TComponent>().Remove(entity);
        }

        public ref Buffer<TComponent> AddBuffer<TComponent>(Entity entity) where TComponent : struct, IComponent
        {
            return ref Add<Buffer<TComponent>>(entity);
        }
                
        public void Emit<TEmission>(Entity entity, in TEmission emission = default) where TEmission : struct, IEmit
        {
#if DEBUG
            if (World.IsDisposed(entity))
                throw new Exception($"Disposed entity {entity} while emiting {emission}");
#endif
            
            World.Emit(entity, emission);
        }

        public List<int> Q(in Type[] filterTypes)
        {
            return new List<int>();
        }

        public FilterBuilder Filter()
        {
            return World.Filter;
        }

        public bool TryUnpack(Entity ecsPackedEntity, out Entity entity)
        {
            entity = default;
            if (World.IsDisposed(ecsPackedEntity))
            {
                return false;
            }

            entity = ecsPackedEntity;
            return true;
        }
    }

    
    public struct With<T1, T2, T3>
    {
        public static Type[] End = { typeof(T1), typeof(T2), typeof(T3) };

        public static int Idx;

        static With()
        {
            Idx = EcsComponent<With<T1, T2, T3>>.TypeIndex;
        }

        public class Without<T4, T5>
        {
            public static Type[] End = { typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5) }; 
        }
    }
}