using System.Collections.Generic;
using System;

namespace Nanory.Lecs
{
    // TODO: provide cross-runtime stable and predictable implicit system ordering
    public abstract class EcsSystemGroup : IEcsRunSystem
    {
        private List<IEcsRunSystem> _systems;

        public void Add(IEcsRunSystem system)
        {
            _systems.Add(system);
        }

        public void Run(EcsSystems systems)
        {
            for (int i = 0; i < _systems.Count; i++)
            {
                _systems[i].Run(systems);
            }
        }
    }

    public abstract class EcsSystemBase : IEcsRunSystem, IEcsInitSystem
    {
        private readonly List<EcsLocalFilterContainer> _localFilterContainers = new List<EcsLocalFilterContainer>(8);

        protected List<EntityCommandBufferSystem> _entityCommandBufferSystems;
        protected EcsWorld World;

        public void Init(EcsSystems systems)
        {
            World = systems.GetWorld();
            OnCreate();
        }

        public void Run(EcsSystems systems)
        {
            OnUpdate();
        }
        public EcsSystemBase SetEntityCommandBufferSystemsLookup(List<EntityCommandBufferSystem> systems)
        {
            _entityCommandBufferSystems = systems;
            return this;
        }

        protected abstract void OnUpdate();

        protected virtual void OnCreate() { }

        protected EcsBufferWorld GetCommandBufferFrom<TSystem>() where TSystem : EntityCommandBufferSystem
        {
            foreach (var system in _entityCommandBufferSystems)
            {
                if (system is TSystem) 
                    return system.GetBuffer(); 
            }

            throw new Exception($"no system {typeof(TSystem)} presented in the entityCommandBufferSystems lookup");
        }

        protected ref TComponent Get<TComponent>(int entity) where TComponent : struct
        {
            return ref World.GetPool<TComponent>().Get(entity);
        }

        protected ref TComponent Add<TComponent>(int entity) where TComponent : struct
        {
            return ref World.GetPool<TComponent>().Add(entity);
        }

        protected void RemoveBuffer<TComponent>(int entity) where TComponent : struct
        {
            World.RemoveBuffer<TComponent>(entity);
        }

        protected ref Buffer<TComponent> AddBuffer<TComponent>(int entity) where TComponent : struct
        {
            return ref World.AddBuffer<TComponent>(entity);
        }

        protected EcsFilter.Mask Filter<T>() where T : struct
        {
            return EcsFilter.Mask.New(World, _localFilterContainers).With<T>();
        }

        protected EcsFilter.Mask Filter()
        {
            return EcsFilter.Mask.New(World, _localFilterContainers);
        }
    }

    public class EntityCommandBufferSystem : IEcsRunSystem
    {
        private EcsBufferWorld _buffer;

        public EntityCommandBufferSystem SetDstWorld(EcsWorld dstWorld)
        {
            _buffer = new EcsBufferWorld(dstWorld);
            return this;
        }

        public EcsBufferWorld GetBuffer() => _buffer;

        public void Run(EcsSystems systems)
        {
            _buffer.Playback();
        }
    }
}