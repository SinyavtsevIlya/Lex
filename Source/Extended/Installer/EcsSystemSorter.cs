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

            SortAndInsertOneFrameSystems();

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

        private void SortAndInsertOneFrameSystems()
        {
            var systemGroups = _systemMap.Values.OfType<EcsSystemGroup>().ToList();

            foreach (var group in systemGroups)
            {
                SortSystemGroup(group);
                InsertOneFrameSystems(group);
            }
        }

        private void InsertOneFrameSystems(EcsSystemGroup systemGroup)
        {
            for (var i = 0; i < systemGroup.Systems.Count; i++)
            {
                var system = systemGroup.Systems[i];
                var shift = 0;

                foreach (var attr in system.GetType().GetCustomAttributes())
                {
                    if (attr is EventSystemAttribute eAttr)
                    {
                        var type = typeof(OneFrameSystem<>).MakeGenericType(eAttr.EventComponentType);
                        systemGroup.Insert(i++, GetSystemByType(type));
                    }
                    else if (attr is RequestSystemAttribute rAttr)
                    {
                        var type = typeof(OneFrameSystem<>).MakeGenericType(rAttr.RequestComponentType);
                        systemGroup.Insert(i + 1, GetSystemByType(type));
                        shift++;
                    }
                }

                i += shift;
            }
        }

        private void SortSystemGroup(EcsSystemGroup group)
        {
            var unsorted = new List<IEcsSystem>(group.Systems);
            var executionLayers = new List<List<IEcsSystem>> { new() };
            var orderFirst = new List<IEcsSystem>();
            var orderLast = new List<IEcsSystem>();

            for (var i = unsorted.Count - 1; i >= 0; i--)
            {
                var sys = unsorted[i];
                var attr = GetCachedAttribute<UpdateInGroup>(sys.GetType());

                if (attr?.OrderFirst == true) { orderFirst.Add(sys); unsorted.RemoveAt(i); }
                else if (attr?.OrderLast == true) { orderLast.Add(sys); unsorted.RemoveAt(i); }
            }

            for (var i = unsorted.Count - 1; i >= 0; i--)
            {
                if (GetCachedAttribute<UpdateBefore>(unsorted[i].GetType()) == null)
                {
                    executionLayers[0].Add(unsorted[i]);
                    unsorted.RemoveAt(i);
                }
            }

            SortRecursive(unsorted, executionLayers, 1);
            executionLayers.Reverse();

            foreach (var sys in orderFirst)
                executionLayers[0].Insert(0, sys);

            group.Systems = executionLayers.SelectMany(l => l).ToList();

            foreach (var sys in orderLast)
                group.Add(sys);
        }

        private void SortRecursive(List<IEcsSystem> unsorted, List<List<IEcsSystem>> table, int level)
        {
            var layer = new List<IEcsSystem>();
            table.Add(layer);

            for (var i = unsorted.Count - 1; i >= 0; i--)
            {
                var sys = unsorted[i];
                var beforeAttr = GetCachedAttribute<UpdateBefore>(sys.GetType());

                if (beforeAttr != null)
                {
                    if (!_systemMap.TryGetValue(beforeAttr.TargetSystemType, out var target))
                    {
                        layer.Add(sys);
                        unsorted.RemoveAt(i);
                        continue;
                    }

                    if (table[level - 1].Contains(target))
                    {
                        layer.Add(sys);
                        unsorted.RemoveAt(i);
                    }
                }
            }

            if (unsorted.Count > 0)
                SortRecursive(unsorted, table, level + 1);
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
