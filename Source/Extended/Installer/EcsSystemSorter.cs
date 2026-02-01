using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nanory.Lex.Collections;

namespace Nanory.Lex
{
    public class EcsSystemSorter : IDisposable
    {
        private World _world;
        private Dictionary<Type, ISystem> _systemMap;
        private Func<Type, ISystem> _creator;
        
        private SystemsGroup _rootSystemGroup;
        private Type[] _systemTypes;

        private readonly Dictionary<(Type type, Type attrType), Attribute> _attributeCache = new();

        public EcsSystemSorter(World world, Func<Type, ISystem> creator = null)
        {
            _world = world;
            _systemMap = new Dictionary<Type, ISystem>();
            _creator = creator;
        }

        public SystemsGroup GetSortedSystems(IEnumerable<Type> systemTypes)
        {
            InitializeSystemTypes(systemTypes);

            var handledSystems = new HashSet<Type>();
            _rootSystemGroup = (SystemsGroup) GetSystemByType(typeof(RootSystemGroup));
            handledSystems.Add(typeof(RootSystemGroup));

            foreach (var systemType in _systemTypes)
                CreateSystemHierarchy(systemType, handledSystems);

            SetupWorldReactives();

            return _rootSystemGroup;
        }

        private void InitializeSystemTypes(IEnumerable<Type> systemTypes)
        {
            var defaultSystemGroupTypes = new[]
            {
                typeof(InitializationSystemGroup),
                typeof(SimulationSystemGroup),
                typeof(PresentationSystemGroup),
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
                var parentInstance = (SystemsGroup)GetSystemByType(targetGroupType);

#if DEBUG
                if (instance is SystemsGroup group && group.systems.data.Contains(parentInstance))
                    throw new Exception($"<b>{instance}</b> and <b>{parentInstance}</b> have circular dependency.");
#endif

                parentInstance.AddSystem(instance);

                systemType = targetGroupType;
            }
        }

        private void SetupWorldReactives()
        {
            _world.SetReactiveSystems(
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

        private ISystem GetSystemByType(Type systemType)
        {
            if (!_systemMap.TryGetValue(systemType, out var system))
            {
                system = systemType == typeof(SystemsGroupPosition) ? 
                    _world.CreateSystemsGroup() :
                    (ISystem) Activator.CreateInstance(systemType);
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
