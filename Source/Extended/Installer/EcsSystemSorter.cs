using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nanory.Lex
{
    public class EcsSystemSorter : IDisposable
    {
        private EcsWorld _world;
        private Dictionary<Type, IEcsSystem> _systemMap;
        private Func<Type, IEcsSystem> _creator;
        
        private EcsSystemGroup _rootSystemGroup;
        private Type[] _systemTypes;

        private readonly Dictionary<(Type type, Type attrType), Attribute> _attributeCache = new();

        public EcsSystemSorter(EcsWorld world, Func<Type, IEcsSystem> creator = null)
        {
            _world = world;
            _systemMap = new Dictionary<Type, IEcsSystem>();
            _creator = creator;
        }

        public EcsSystemGroup GetSortedSystems(IEnumerable<Type> systemTypes)
        {
            InitializeSystemTypes(systemTypes);

            var handledSystems = new HashSet<Type>();
            _rootSystemGroup = (EcsSystemGroup)GetSystemByType(typeof(RootSystemGroup));
            handledSystems.Add(typeof(RootSystemGroup));

            foreach (var systemType in _systemTypes)
                CreateSystemHierarchy(systemType, handledSystems);

            SetupWorldLookups();

            return _rootSystemGroup;
        }

        private void InitializeSystemTypes(IEnumerable<Type> systemTypes)
        {
            var defaultSystemGroupTypes = new[]
            {
                typeof(InitializationSystemGroup),
                typeof(SimulationSystemGroup),
                typeof(PresentationSystemGroup),
                typeof(BeginSimulationECBSystem)
            };

            _systemTypes = systemTypes
                .Union(defaultSystemGroupTypes)
                .Union(UISystemTypesRegistry.Values)
                .ToArray();
        }

        private void CreateSystemHierarchy(Type systemType, HashSet<Type> handledSystems)
        {
            while (true)
            {
                if (!handledSystems.Add(systemType)) return;

                var updateInGroup = GetCachedAttribute<UpdateInGroup>(systemType);
                var targetGroupType = updateInGroup?.TargetGroupType ?? typeof(SimulationSystemGroup);

                var instance = GetSystemByType(systemType);
                var parentInstance = (EcsSystemGroup)GetSystemByType(targetGroupType);

#if DEBUG
                if (instance is EcsSystemGroup group && group.Systems.Contains(parentInstance)) throw new Exception($"<b>{instance}</b> and <b>{parentInstance}</b> have circular dependency.");
#endif

                parentInstance.Add(instance);

                systemType = targetGroupType;
            }
        }

        private void SetupWorldLookups()
        {
            if (_world is EcsWorldBase worldBase)
            {
                worldBase.SetSystemsLookup(_systemMap);
                worldBase.SetEntityCommandBufferSystemsLookup(_systemMap.Values.OfType<EntityCommandBufferSystem>().ToList());
                worldBase.SetReactiveSystems(
                    _systemMap.Values
                        .OfType<IReact>()
                        .SelectMany(sys =>
                            sys.GetType()
                                .GetInterfaces()
                                .Where(i => i.IsGenericType &&
                                            i.GetGenericTypeDefinition() == typeof(IReact<>))
                                .Select(i => new {
                                    Sys = sys,
                                    Arg = i.GetGenericArguments()[0]
                                }))
                        .GroupBy(x => x.Arg)
                        .ToDictionary(g => g.Key, g => g.Select(x => x.Sys).ToList())
                );

            }

            foreach (var cbs in _systemMap.Values.OfType<EntityCommandBufferSystem>())
                cbs.SetDstWorld(_world);

            foreach (var lookup in _systemMap.Values.OfType<IEcsEntityCommandBufferLookup>())
            {
                lookup.SetEntityCommandBufferSystemsLookup(_systemMap.Values.OfType<EntityCommandBufferSystem>().ToList());

                if (lookup is EcsSystemBase systemBase)
                    systemBase.Later = _systemMap.Values.OfType<BeginSimulationECBSystem>().First().GetBuffer();
            }
            
        }


        private IEcsSystem GetSystemByType(Type systemType)
        {
            if (!_systemMap.TryGetValue(systemType, out var system))
            {
                system = _creator?.Invoke(systemType) ?? (IEcsSystem)Activator.CreateInstance(systemType);
                _systemMap[systemType] = system;
            }

            return system;
        }

        private T GetCachedAttribute<T>(Type type) where T : Attribute
        {
            var key = (type, typeof(T));
            if (_attributeCache.TryGetValue(key, out var attr))
                return (T)attr;

            var attribute = type.GetCustomAttribute<T>();
            _attributeCache[key] = attribute;
            return attribute;
        }

        public void Dispose()
        {
            _world = null;
            _rootSystemGroup = null;
            _systemMap = null;
            _systemTypes = null;
            _attributeCache.Clear();
        }
    }
}
