using System.Collections.Generic;
using System;

namespace Nanory.Lex
{
    public class BeginSimulationECBSystem : EntityCommandBufferSystem { }

    public class UpdateBefore : Attribute
    {
        public Type TargetSystemType;
        public UpdateBefore(Type targetSystemType) => TargetSystemType = targetSystemType;
    }

    public class UpdateInGroup : Attribute
    {
        public Type TargetGroupType;
        public UpdateInGroup(Type targetGroupType) => TargetGroupType = targetGroupType;
    }

    [PreserveAutoCreation]
    public class RootSystemGroup : EcsSystemGroup { }

    [UpdateBefore(typeof(PresentationSystemGroup))]
    [UpdateInGroup(typeof(RootSystemGroup))]
    public class SimulationSystemGroup : EcsSystemGroup { }

    [UpdateInGroup(typeof(RootSystemGroup))]
    public class PresentationSystemGroup : EcsSystemGroup { }

    public abstract class EcsSystemGroup : IEcsRunSystem, IEcsInitSystem
    {
        private List<IEcsRunSystem> _runSystems = new List<IEcsRunSystem>();
        private List<IEcsInitSystem> _initSystems = new List<IEcsInitSystem>();
        private List<IEcsSystem> _ecsSystems = new List<IEcsSystem>();

        public void Add(IEcsSystem system)
        {
            _ecsSystems.Add(system);

            if (system == this)
                throw new Exception("Trying to pass itself to systems list");

            if (system is IEcsRunSystem runSystem)
                _runSystems.Add(runSystem);
            if (system is IEcsInitSystem initSystem)
                _initSystems.Add(initSystem);
        }

        public void Init(EcsSystems systems)
        {
            for (int i = 0; i < _initSystems.Count; i++)
            {
                _initSystems[i].Init(systems);
            }
        }

        public void Run(EcsSystems systems)
        {
            for (int i = 0; i < _runSystems.Count; i++)
            {
                _runSystems[i].Run(systems);
            }
        }

        public List<IEcsSystem> Systems
        {
            get => _ecsSystems;
            set
            {
                _ecsSystems.Clear();
                _initSystems.Clear();
                _runSystems.Clear();

                foreach (var system in value)
                    Add(system);
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