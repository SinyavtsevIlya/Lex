using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nanory.Lex
{
    public class EcsWorldBase : EcsWorld, IEcsEntityCommandBufferLookup
    {
        private List<EntityCommandBufferSystem> _entityCommandBufferSystems;
        private Dictionary<Type, IEcsSystem> _systemsByTypes;

        public EntityCommandBuffer GetCommandBufferFrom<TSystem>() where TSystem : EntityCommandBufferSystem
        {
            foreach (var system in _entityCommandBufferSystems)
            {
                if (system is TSystem)
                    return system.GetBuffer();
            }

            throw new MissingCommandBufferSystemException<TSystem>(this);
        }

        public IEcsEntityCommandBufferLookup SetEntityCommandBufferSystemsLookup(List<EntityCommandBufferSystem> systems)
        {
            _entityCommandBufferSystems = systems;
            return this;
        }

        public void SetSystemsLookup(IEnumerable<IEcsSystem> systems)
        {
            foreach (var system in systems)
            {
                _systemsByTypes[system.GetType()] = system;
            }
        }

        public TSystem GetSystem<TSystem>() where TSystem : class, IEcsSystem
        {
            if (_systemsByTypes.TryGetValue(typeof(TSystem), out var system))
            {
                return system as TSystem;
            }

            throw new System.Exception($"{nameof(TSystem)} was not found in {this}");
        }
    }
}
