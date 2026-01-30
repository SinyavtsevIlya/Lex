using System;
using System.Collections.Generic;

namespace Nanory.Lex
{

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
        
        public void Insert(int idx, IEcsSystem system)
        {
#if DEBUG
            if (system == this)
                throw new Exception($"<b>{system.GetType().Name}</b> Trying to pass itself to systems list");
            if (_ecsSystems.Contains(system))
                throw new Exception($"Trying to add a duplicate <b>{system.GetType().Name}</b> to a <b>{GetType().Name}</b> ");
#endif

            _ecsSystems.Insert(idx, system);

            if (system is IEcsRunSystem runSystem)
                _runSystems.Insert(idx, runSystem);
            if (system is IEcsPreInitSystem preInitSystem)
                _preInitSystems.Insert(idx, preInitSystem);
            if (system is IEcsInitSystem initSystem)
                _initSystems.Insert(idx, initSystem);
            if (system is IEcsDestroySystem destroySystem)
                _destroySystems.Insert(idx, destroySystem);
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
    
    public abstract class EcsRunSystemBase : EcsSystemBase
    {
        
    }

    public abstract class EcsSystemBase : IEcsRunSystem, IEcsPreInitSystem, IEcsInitSystem, IEcsDestroySystem
    {
        private readonly List<EcsLocalFilterContainer> _localFilterContainers = new List<EcsLocalFilterContainer>(8);

        #region Shortcuts
        /// <summary>
        /// Default predefined entity command buffer shortcut.
        /// </summary>
        public EcsWorldBase World;
        #endregion

        public void PreInit(EcsSystems systems)
        {
            World = systems.GetWorld() as EcsWorldBase;
        }

        public void Init(EcsSystems systems)
        {
            OnCreate();
        }

        public void Destroy(EcsSystems systems)
        {
            OnDestroy();
        }

        public void Run(EcsSystems systems)
        {
            OnUpdate();
        }

        protected virtual void OnUpdate() { }

        protected virtual void OnCreate() { }
        
        protected virtual void OnDestroy() { }

        public int NewEntity()
        {
            return World.NewEntity();
        }

        public ref TComponent Get<TComponent>(int entity) where TComponent : struct
        {
            return ref World.GetPool<TComponent>().Get(entity);
        }

        public void Swap<TComponent>(int a, int b) where TComponent : struct
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

        public ref TComponent GetOrAdd<TComponent>(int entity) where TComponent : struct
        {
            var pool = World.GetPool<TComponent>();

            if (pool.Has(entity))
                return ref pool.Get(entity);
            
            return ref pool.Add(entity);
        }

        public bool TryGet<T>(int entity, out T component) where T : struct
        {
            if (World.GetPool<T>().Has(entity))
            {
                component = World.GetPool<T>().Get(entity);
                return true;
            }
            component = default;
            return false;
        }

        public ref TComponent Add<TComponent>(int entity) where TComponent : struct
        {
            return ref World.GetPool<TComponent>().Add(entity);
        }

        public bool Has<TComponent>(int entity) where TComponent : struct
        {
            return World.GetPool<TComponent>().Has(entity);
        }

        public void Del<TComponent>(int entity) where TComponent : struct
        {
            World.GetPool<TComponent>().Del(entity);
        }

        public ref Buffer<TComponent> AddBuffer<TComponent>(int entity) where TComponent : struct
        {
            return ref World.AddBuffer<TComponent>(entity);
        }
                
        public void Emit<TEmission>(int entity, in TEmission emission = default) where TEmission : struct, IEmit
        {
            World.Emit(entity, emission);
        }

        public List<int> Q(in Type[] filterTypes)
        {
            return new List<int>();
        }

        public EcsFilter.Mask Filter<T>() where T : struct
        {
            return EcsFilter.Mask.New(World, _localFilterContainers).With<T>();
        }

        public EcsFilter.Mask Filter()
        {
            return EcsFilter.Mask.New(World, _localFilterContainers);
        }

        /// <summary>
        /// Unpacks the passed <see cref="EcsPackedEntity"/> using a user predefined World (via <see cref="EcsSystems.GetWorld(string)"/>)
        /// </summary>
        /// <param name="ecsPackedEntity"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool TryUnpack(EcsPackedEntity ecsPackedEntity, out int entity) => ecsPackedEntity.Unpack(World, out entity);
        
        public EcsPackedEntity Pack(int entity)
        {
            EcsPackedEntity packed;
            packed.Id = entity;
            packed.Gen = World.GetEntityGen(entity);
            return packed;
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