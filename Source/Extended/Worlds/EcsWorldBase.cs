using System;
using System.Collections.Generic;

namespace Nanory.Lex
{
    public class EcsWorldBase : EcsWorld
    {
        private readonly string _name;

        private Dictionary<Type, IEcsSystem> _systemsByTypes;
        private Dictionary<Type, List<IReact>> _reactiveSystems;

        public EcsWorldBase(Config cfg = default, string name = default) : base(cfg)
        {
            _name = name;
        }

        public void Emit<TEmission>(int entity, in TEmission emission = default) where TEmission : struct, IEmit
        {
            if (!TryGetReactiveSystems<TEmission>(out var reactions))
                return;

            foreach (var reaction in reactions)
            {
                var reactiveSystem = (IReact<TEmission>)reaction;
                if (reactiveSystem.IsMatch(entity, this))
                {
                    reactiveSystem.React(emission, entity);
                }
            }
        }

        public bool TryGetReactiveSystems<TComponent>(out List<IReact> reactions)
        {
            return _reactiveSystems.TryGetValue(typeof(TComponent), out reactions);
        }


        public string Name => _name;

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

        public void SetReactiveSystems(Dictionary<Type, List<IReact>> reactiveSystems)
        {
            _reactiveSystems = reactiveSystems;
        }

        public TSystem GetSystem<TSystem>() where TSystem : class, IEcsSystem
        {
            if (_systemsByTypes.TryGetValue(typeof(TSystem), out var system))
            {
                return system as TSystem;
            }

            throw new Exception($"{typeof(TSystem)} was not found in {this.Name}");
        }
    }
}
