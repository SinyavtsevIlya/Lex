using System;
using System.Collections.Generic;

namespace Nanory.Lex
{
    public class EcsWorldBase : EcsWorld, IEcsEntityCommandBufferLookup
    {
        private readonly string _name;

        private List<EntityCommandBufferSystem> _entityCommandBufferSystems;
        private Dictionary<Type, IEcsSystem> _systemsByTypes;
        private Dictionary<Type, List<EcsReactiveSystemBase>> _reactiveSystems;

        public EcsWorldBase(Config cfg = default, string name = default) : base(cfg)
        {
            _name = name;
        }

        public bool TryGetReactiveSystems<TComponent>(out List<EcsReactiveSystemBase> systems)
        {
            return _reactiveSystems.TryGetValue(typeof(TComponent), out systems);
        }

        public EntityCommandBuffer GetCommandBufferFrom<TSystem>() where TSystem : EntityCommandBufferSystem
        {
            foreach (var system in _entityCommandBufferSystems)
            {
                if (system is TSystem)
                    return system.GetBuffer();
            }

            throw new MissingCommandBufferSystemException<TSystem>(this);
        }

        public string Name => _name;

        public IEcsEntityCommandBufferLookup SetEntityCommandBufferSystemsLookup(List<EntityCommandBufferSystem> systems)
        {
            _entityCommandBufferSystems = systems;
            return this;
        }

        public void SetSystemsLookup(IEnumerable<IEcsSystem> systems)
        {
            _systemsByTypes = new Dictionary<Type, IEcsSystem>();

            foreach (var system in systems)
            {
                _systemsByTypes[system.GetType()] = system;
            }
        }

        public void SetSystemsLookup(Dictionary<Type, IEcsSystem> systemsByType)
        {
            _systemsByTypes = systemsByType;
        }

        public void SetReactiveSystems(Dictionary<Type, List<EcsReactiveSystemBase>> reactiveSystems)
        {
            _reactiveSystems = reactiveSystems;
        }

        public TSystem GetSystem<TSystem>() where TSystem : class, IEcsSystem
        {
            if (_systemsByTypes.TryGetValue(typeof(TSystem), out var system))
            {
                return system as TSystem;
            }

            throw new System.Exception($"{typeof(TSystem)} was not found in {this.Name}");
        }
    }
}
