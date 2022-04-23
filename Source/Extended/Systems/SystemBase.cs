using System.Collections.Generic;
using System;

namespace Nanory.Lex
{
    [UpdateInGroup(typeof(BeginSimulationSystemGroup), OrderFirst = true)]
    public class BeginSimulationECBSystem : EntityCommandBufferSystem { }


    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public class BeginSimulationSystemGroup : EcsSystemGroup { }

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

    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public class OneFrameSystemGroup : EcsSystemGroup { }

    public class RootSystemGroup : EcsSystemGroup { }

    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public class InitializationSystemGroup : EcsSystemGroup { }

    [UpdateInGroup(typeof(RootSystemGroup))]
    [UpdateBefore(typeof(PresentationSystemGroup))]
    public class SimulationSystemGroup : EcsSystemGroup { }

    [UpdateInGroup(typeof(RootSystemGroup))]
    public class PresentationSystemGroup : EcsSystemGroup { }

    public abstract class EcsSystemGroup : IEcsRunSystem, IEcsInitSystem, IEcsDestroySystem
    {
        protected List<IEcsRunSystem> _runSystems = new List<IEcsRunSystem>();
        protected List<IEcsPreInitSystem> _preInitSystems = new List<IEcsPreInitSystem>();
        protected List<IEcsInitSystem> _initSystems = new List<IEcsInitSystem>();
        protected List<IEcsSystem> _ecsSystems = new List<IEcsSystem>();
        protected List<IEcsDestroySystem> _destroySystems = new List<IEcsDestroySystem>();

#if DEBUG
        public event Action<IEcsRunSystem> Stepped;
#endif

        public bool IsEnabled { get; set; } = true;

        public void Add(IEcsSystem system)
        {
#if DEBUG
            if (system == this)
                throw new Exception($"<b>{system.GetType().Name}</b> Trying to pass itself to systems list");
            if (_ecsSystems.Contains(system))
                throw new Exception($"Trying to add a duplicate <b>{system.GetType().Name}</b> to a <b>{GetType().Name}</b> ");
#endif

            _ecsSystems.Add(system);

            if (system is IEcsRunSystem runSystem)
                _runSystems.Add(runSystem);
            if (system is IEcsPreInitSystem preInitSystem)
                _preInitSystems.Add(preInitSystem);
            if (system is IEcsInitSystem initSystem)
                _initSystems.Add(initSystem);
            if (system is IEcsDestroySystem destroySystem)
                _destroySystems.Add(destroySystem);
        }

        public void Init(EcsSystems systems)
        {
            if (IsEnabled)
            {
                OnCreate(systems);
            }
        }

        public void Run(EcsSystems systems)
        {
            if (IsEnabled)
            {
                OnUpdate(systems);
            }
        }

        public void Destroy(EcsSystems systems)
        {
            for (int i = 0; i < _destroySystems.Count; i++)
            {
                _destroySystems[i].Destroy(systems);
            }

            _runSystems.Clear();
            _preInitSystems.Clear();
            _initSystems.Clear();
            _ecsSystems.Clear();
            _destroySystems.Clear();
        }

        public List<IEcsSystem> Systems
        {
            get => _ecsSystems;
            set
            {
                _ecsSystems.Clear();
                _preInitSystems.Clear();
                _initSystems.Clear();
                _runSystems.Clear();
                _destroySystems.Clear();

                foreach (var system in value)
                    Add(system);
            }
        }

        protected virtual void OnUpdate(EcsSystems systems)
        {
            for (int i = 0; i < _runSystems.Count; i++)
            {
                _runSystems[i].Run(systems);
#if DEBUG
                Stepped?.Invoke(_runSystems[i]);
#endif
            }
        }

        protected virtual void OnCreate(EcsSystems systems) 
        {
            for (int i = 0; i < _preInitSystems.Count; i++)
            {
                _preInitSystems[i].PreInit(systems);
            }

            for (int i = 0; i < _initSystems.Count; i++)
            {
                _initSystems[i].Init(systems);
            }
        }
    }

    public class MissingCommandBufferSystemException<TSystem> : Exception where TSystem : EntityCommandBufferSystem 
    {
        private readonly IEcsEntityCommandBufferLookup _context;

        public MissingCommandBufferSystemException(IEcsEntityCommandBufferLookup context)
        {
            _context = context;
        }

        public override string Message => $"No {nameof(TSystem)} was found in {nameof(_context)}.";
    }

    public interface IEcsEntityCommandBufferLookup
    {
        IEcsEntityCommandBufferLookup SetEntityCommandBufferSystemsLookup(List<EntityCommandBufferSystem> systems);
        EntityCommandBuffer GetCommandBufferFrom<TSystem>() where TSystem : EntityCommandBufferSystem;
    }

    public abstract class EcsSystemBase : IEcsRunSystem, IEcsPreInitSystem, IEcsInitSystem, IEcsEntityCommandBufferLookup
    {
        private readonly List<EcsLocalFilterContainer> _localFilterContainers = new List<EcsLocalFilterContainer>(8);
        protected List<EntityCommandBufferSystem> _entityCommandBufferSystems;

        public EcsWorldBase World;
        public EcsSystems EcsSystems;

        public void PreInit(EcsSystems systems)
        {
            EcsSystems = systems;
            World = systems.GetWorld() as EcsWorldBase;
        }

        public void Init(EcsSystems systems)
        {
            OnCreate();
        }

        public void Run(EcsSystems systems)
        {
            OnUpdate();
        }
        public IEcsEntityCommandBufferLookup SetEntityCommandBufferSystemsLookup(List<EntityCommandBufferSystem> systems)
        {
            _entityCommandBufferSystems = systems;
            return this;
        }

        public EntityCommandBuffer GetCommandBufferFrom<TSystem>() where TSystem : EntityCommandBufferSystem
        {
            foreach (var system in _entityCommandBufferSystems)
            {
                if (system is TSystem)
                    return system.GetBuffer();
            }

            throw new Exception($"no system {typeof(TSystem)} presented in the entityCommandBufferSystems lookup");
        }

        protected abstract void OnUpdate();

        protected virtual void OnCreate() { }

        protected int NewEntity()
        {
            return World.NewEntity();
        }

        protected ref TComponent Get<TComponent>(int entity) where TComponent : struct
        {
            return ref World.GetPool<TComponent>().Get(entity);
        }

        protected void Swap<TComponent>(int a, int b) where TComponent : struct
        {
            var hasComponentA = (TryGet<TComponent>(a, out var tComponentA));
            var hasComponentB = (TryGet<TComponent>(b, out var tComponentB));

            if (hasComponentA && hasComponentB)
            {
                Get<TComponent>(a) = tComponentB;
                Get<TComponent>(b) = tComponentA;
            }

            if (!hasComponentA && hasComponentB)
            {
                Add<TComponent>(a) = tComponentB;
                Del<TComponent>(b);
            }

            if (!hasComponentB && hasComponentA)
            {
                Add<TComponent>(b) = tComponentA;
                Del<TComponent>(a);
            }
        }

        protected void SwapTag<TComponent>(int a, int b) where TComponent : struct
        {
            var hasTagA = Has<TComponent>(a);
            var hasTagB = Has<TComponent>(b);

            if (hasTagA != hasTagB)
            {
                var aa = hasTagA ? a : b;
                var bb = hasTagA ? b : a;

                Del<TComponent>(aa);
                Add<TComponent>(bb);
            }
        }

        protected ref TComponent GetOrAdd<TComponent>(int entity) where TComponent : struct
        {
            var pool = World.GetPool<TComponent>();

            if (pool.Has(entity))
            {
                return ref pool.Get(entity);
            }
            else
            {
                return ref pool.Add(entity);
            }
        }

        protected bool TryGet<T>(int entity, out T component) where T : struct
        {
            if (World.GetPool<T>().Has(entity))
            {
                component = World.GetPool<T>().Get(entity);
                return true;
            }
            component = default;
            return false;
        }

        protected ref TComponent Add<TComponent>(int entity) where TComponent : struct
        {
            return ref World.GetPool<TComponent>().Add(entity);
        }

        protected bool Has<TComponent>(int entity) where TComponent : struct
        {
            return World.GetPool<TComponent>().Has(entity);
        }

        protected void Del<TComponent>(int entity) where TComponent : struct
        {
            World.GetPool<TComponent>().Del(entity);
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
        private EntityCommandBuffer _buffer;

        public EntityCommandBufferSystem SetDstWorld(EcsWorld dstWorld)
        {
            _buffer = new EntityCommandBuffer(dstWorld);
            return this;
        }

        public EntityCommandBuffer GetBuffer() => _buffer;

        public void Run(EcsSystems systems)
        {
            _buffer.Playback();
        }
    }
}