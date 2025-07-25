using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nanory.Lex
{
    public abstract class SystemTypesProviderBase
    {
        public abstract IEnumerable<Type> GetSystemTypes(EcsTypesScanner scanner);
    }

    public abstract class FeatureBase { }

    public class EcsSystemSorter : IDisposable
    {
        protected EcsWorld World { get; private set; }
        protected EcsSystemGroup RootSystemGroup { get; private set; }
        protected Dictionary<Type, IEcsSystem> SystemMap { get; private set; }
        protected Func<Type, IEcsSystem> Creator { get; private set; }
        protected Type[] SystemTypes { get; set; }

        public EcsSystemSorter(EcsWorld world, Func<Type, IEcsSystem> creator = null)
        {
            World = world;
            SystemMap = new Dictionary<Type, IEcsSystem>();
            Creator = creator;
        }

        public EcsSystemGroup GetSortedSystems(IEnumerable<Type> systemTypes)
        {
            InitializeSystemTypes(systemTypes);

            var handledSystems = new HashSet<Type>();
            RootSystemGroup = (EcsSystemGroup)GetSystemByType(typeof(RootSystemGroup));
            handledSystems.Add(typeof(RootSystemGroup));

            foreach (var systemType in SystemTypes)
                TryCreateSystemRecursive(systemType);

            SetupWorldLookups();
            SortAllSystemGroups();

            return RootSystemGroup;

            void TryCreateSystemRecursive(Type systemType)
            {
                if (!handledSystems.Add(systemType))
                    return;

                var updateInGroup = systemType.GetCustomAttribute<UpdateInGroup>();
                var targetGroupType = updateInGroup?.TargetGroupType ?? typeof(SimulationSystemGroup);

                var instance = GetSystemByType(systemType);
                var parentInstance = (EcsSystemGroup)GetSystemByType(targetGroupType);

#if DEBUG
                if (instance is EcsSystemGroup group && group.Systems.Contains(parentInstance))
                    throw new Exception($"<b>{instance}</b> and <b>{parentInstance}</b> have circular dependency.");
#endif
                parentInstance.Add(instance);

                TryCreateSystemRecursive(targetGroupType);
            }
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

            SystemTypes = systemTypes
                .Union(defaultSystemGroupTypes)
                .Union(UISystemTypesRegistry.Values)
                .ToArray();
        }

        private void SetupWorldLookups()
        {
            if (World is EcsWorldBase worldBase)
            {
                worldBase.SetSystemsLookup(SystemMap);
                worldBase.SetEntityCommandBufferSystemsLookup(SystemMap.Values.OfType<EntityCommandBufferSystem>().ToList());
            }

            foreach (var cbs in SystemMap.Values.OfType<EntityCommandBufferSystem>())
                cbs.SetDstWorld(World);

            foreach (var lookup in SystemMap.Values.OfType<IEcsEntityCommandBufferLookup>())
            {
                lookup.SetEntityCommandBufferSystemsLookup(SystemMap.Values.OfType<EntityCommandBufferSystem>().ToList());

                if (lookup is EcsSystemBase systemBase)
                    systemBase.Later = SystemMap.Values.OfType<BeginSimulationECBSystem>().First().GetBuffer();
            }
        }

        private void SortAllSystemGroups()
        {
            foreach (var group in SystemMap.Values.OfType<EcsSystemGroup>().ToList())
            {
                SortSystemGroup(group);
                InsertOneFrameSystems(group);
            }
        }

        private void InsertOneFrameSystems(EcsSystemGroup systemGroup)
        {
            for (var index = 0; index < systemGroup.Systems.Count; index++)
            {
                var system = systemGroup.Systems[index];

                var shift = 0;

                foreach (var attribute in system.GetType().GetCustomAttributes())
                {
                    if (attribute is EventSystemAttribute eventSystemAttribute)
                    {
                        var systemType = typeof(OneFrameSystem<>).MakeGenericType(eventSystemAttribute.EventComponentType);
                        systemGroup.Insert(index++, GetSystemByType(systemType));
                    }
                    else if (attribute is RequestSystemAttribute requestSystemAttribute)
                    {
                        var systemType = typeof(OneFrameSystem<>).MakeGenericType(requestSystemAttribute.RequestComponentType);
                        systemGroup.Insert(index + 1, GetSystemByType(systemType));
                        shift++;
                    }
                }
                    
                index += shift;
            }

        }

        private void SortSystemGroup(EcsSystemGroup group)
        {
            var unsorted = new List<IEcsSystem>(group.Systems);
            var dependencyTable = new List<List<IEcsSystem>> { new() };
            var orderFirst = new List<IEcsSystem>();
            var orderLast = new List<IEcsSystem>();

            for (int i = unsorted.Count - 1; i >= 0; i--)
            {
                var sys = unsorted[i];
                var attr = sys.GetType().GetCustomAttribute<UpdateInGroup>();

                if (attr?.OrderFirst == true) { orderFirst.Add(sys); unsorted.RemoveAt(i); }
                else if (attr?.OrderLast == true) { orderLast.Add(sys); unsorted.RemoveAt(i); }
            }

            for (int i = unsorted.Count - 1; i >= 0; i--)
            {
                if (unsorted[i].GetType().GetCustomAttribute<UpdateBefore>() == null)
                {
                    dependencyTable[0].Add(unsorted[i]);
                    unsorted.RemoveAt(i);
                }
            }

            SortRecursive(unsorted, dependencyTable, 1);
            dependencyTable.Reverse();

            foreach (var sys in orderFirst)
                dependencyTable[0].Insert(0, sys);

            group.Systems = dependencyTable.SelectMany(l => l).ToList();

            foreach (var sys in orderLast)
                group.Add(sys);
        }

        private void SortRecursive(List<IEcsSystem> unsorted, List<List<IEcsSystem>> table, int level)
        {
            var layer = new List<IEcsSystem>();
            table.Add(layer);

            for (int i = unsorted.Count - 1; i >= 0; i--)
            {
                var sys = unsorted[i];
                var beforeAttr = sys.GetType().GetCustomAttribute<UpdateBefore>();

                if (beforeAttr != null)
                {
                    if (!SystemMap.TryGetValue(beforeAttr.TargetSystemType, out var target))
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

        protected IEcsSystem GetSystemByType(Type systemType)
        {
            if (!SystemMap.TryGetValue(systemType, out var system))
            {
                system = Creator?.Invoke(systemType) ?? (IEcsSystem)Activator.CreateInstance(systemType);
                SystemMap[systemType] = system;
            }

            return system;
        }

        public void Dispose()
        {
            World = null;
            RootSystemGroup = null;
            SystemMap = null;
            SystemTypes = null;
        }
    }

    public static class EcsSystemsExtensions
    {
        public static TTargetSystem FindSystem<TTargetSystem>(this List<IEcsSystem> systems) where TTargetSystem : IEcsSystem
        {
            foreach (var system in systems)
            {
                if (system is TTargetSystem match)
                    return match;

                if (system is EcsSystemGroup group)
                {
                    var result = FindSystem<TTargetSystem>(group.Systems);
                    if (result != null)
                        return result;
                }
            }

            return default;
        }

        public static void FindAllSystemsNonAlloc<TTargetSystem>(this List<IEcsSystem> input, List<TTargetSystem> output)
        {
            foreach (var system in input)
            {
                if (system is TTargetSystem match)
                    output.Add(match);

                if (system is EcsSystemGroup group)
                    FindAllSystemsNonAlloc(group.Systems, output);
            }
        }
    }
}
