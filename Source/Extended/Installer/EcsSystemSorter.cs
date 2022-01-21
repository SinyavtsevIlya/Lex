using System;
using System.Collections.Generic;
using System.Linq;
using Nanory.Lex.Conversion;

namespace Nanory.Lex
{
    public interface IDefaultWorldAttribute { }
    public abstract class WorldAttribute : Attribute { }
    public class TargetWorldAttribute : WorldAttribute { }
    public class AllWorldAttribute : WorldAttribute { }
    public class NoneWorldAttribute : WorldAttribute { }

    /// <summary>
    /// Sorts systems in a hierarchical manner based on special Ordering attributes
    /// (<see cref="UpdateInGroup"/> and <see cref="UpdateBefore"/>). 
    /// </summary>
    /// <typeparam name="TWorld">Target world attribute for scan</typeparam>
    public class EcsSystemSorter<TWorld> : IDisposable where TWorld : TargetWorldAttribute
    {
        protected EcsWorld World { get; private set; }
        protected EcsSystemGroup RootSystemGroup { get; private set; }
        protected Dictionary<Type, IEcsSystem> SystemMap { get; private set; }
        protected Func<Type, IEcsSystem> Creator { get; private set; }
        protected Type[] SystemTypes { get; set; }

        public EcsSystemSorter(EcsWorld world, Func<Type, IEcsSystem> creator = null, EcsTypesScanner ecsTypesScanner = null) 
            : this (world, GetTypesByScanner(ecsTypesScanner), creator) { }

        public EcsSystemSorter(EcsWorld world, IEnumerable<Type> systemTypes, Func<Type,IEcsSystem> creator = null)
        {
            World = world;
            SystemMap = new Dictionary<Type, IEcsSystem>();
            Creator = creator;

            var defaultSystemGroupTypes = new Type[]
            {
                typeof(InitializationSystemGroup),
                typeof(SimulationSystemGroup),
                typeof(PresentationSystemGroup),
                typeof(BeginSimulationECBSystem),
            };

            var conversionSystemTypes = new Type[]
            {
                typeof(GameObjectConversionSystem)
            };

            SystemTypes = systemTypes
                .Union(defaultSystemGroupTypes)
                .Union(conversionSystemTypes)
                .Union(UISystemTypesRegistry.Values)
                .ToArray();
        }

        public EcsSystemGroup GetSortedSystems()
        {
            var handledSystems = new HashSet<Type>();
            // Add a root
            var rootSystemGroup = (EcsSystemGroup)GetSystemByType(typeof(RootSystemGroup));
            handledSystems.Add(typeof(RootSystemGroup));

            foreach (var systemType in SystemTypes)
            {
                TryCreateSystemRecursive(systemType);
            }

            var commandBufferSystems = SystemMap.Values.Where(s => s is EntityCommandBufferSystem).Select(s => s as EntityCommandBufferSystem).ToList();
            var commandBufferLookupSystems = SystemMap.Values.Where(s => s is IEcsEntityCommandBufferLookup).Select(s => s as IEcsEntityCommandBufferLookup).ToList();
            var systemGroups = SystemMap.Values.Where(s => s is EcsSystemGroup).Select(s => s as EcsSystemGroup).ToList();

            commandBufferSystems.ForEach(cbs => cbs.SetDstWorld(World));
            commandBufferLookupSystems.ForEach(bs => bs.SetEntityCommandBufferSystemsLookup(commandBufferSystems));

            foreach (var systemGroup in systemGroups)
            {
                SortSystemGroup(systemGroup);
            }

            void TryCreateSystemRecursive(Type systemType)
            {
                if (handledSystems.Contains(systemType))
                    return;

                var updateInGroup = (UpdateInGroup) Attribute.GetCustomAttribute(systemType, typeof(UpdateInGroup));
                var targetGroup = updateInGroup != null ? updateInGroup.TargetGroupType : typeof(SimulationSystemGroup);

                var instance = GetSystemByType(systemType);
                var parentInstance = (EcsSystemGroup)GetSystemByType(targetGroup);

#if DEBUG
                if (instance is EcsSystemGroup instanceSystemGroup)
                {
                    if (instanceSystemGroup.Systems.Contains(parentInstance))
                        throw new Exception($"<b>{instance}</b> and <b>{parentInstance}</b> have circular dependency. Check your {nameof(UpdateInGroup)} attributes");
                }
#endif
                parentInstance.Add(instance);
                handledSystems.Add(systemType);

                if (updateInGroup != null)
                    TryCreateSystemRecursive(targetGroup);
            }

            void SortSystemGroup(EcsSystemGroup systemGroup)
            {
                var unsorted = new List<IEcsSystem>(systemGroup.Systems);

                var dependencyTable = new List<List<IEcsSystem>>();
                // Add a root dependency level
                dependencyTable.Add(new List<IEcsSystem>());

                var orderFirstSystems = new List<IEcsSystem>();
                var orderLastSystems = new List<IEcsSystem>();

                // Check for special attributes parameters OrderFirst/Last...
                for (int idx = unsorted.Count - 1; idx >= 0; idx--)
                {
                    var currentSystem = unsorted[idx];

                    var updateInGroup = (UpdateInGroup) Attribute.GetCustomAttribute(currentSystem.GetType(), typeof(UpdateInGroup));
                    if (updateInGroup != null)
                    {
                        // exclude special "OrderFirst" systems to insert them later to the very beginning 
                        if (updateInGroup.OrderFirst)
                        {
                            orderFirstSystems.Add(currentSystem);
                            unsorted.RemoveAt(idx);
                        }

                        // exclude special "OrderLast" systems to add them later to the very end
                        if (updateInGroup.OrderLast)
                        {
                            orderLastSystems.Add(currentSystem);
                            unsorted.RemoveAt(idx);
                        }
                    }
                }

                // Order first systems without attributes...
                for (int idx = unsorted.Count - 1; idx >= 0; idx--)
                {
                    var currentSystem = unsorted[idx];

                    var updateBefore = (UpdateBefore) Attribute.GetCustomAttribute(currentSystem.GetType(), typeof(UpdateBefore));

                    if (updateBefore == null)
                    {
                        dependencyTable[0].Add(currentSystem);
                        unsorted.RemoveAt(idx);
                    }
                }

                SortRecursive(unsorted, dependencyTable, 1);

                dependencyTable.Reverse();

                // insert "OrderFirst" systems
                foreach (var firstSystem in orderFirstSystems)
                {
                    dependencyTable[0].Insert(0, firstSystem);
                }
                systemGroup.Systems = dependencyTable.SelectMany(layer => layer).ToList();

                // And in the end add "OrderLast" systems
                foreach (var lastSystems in orderLastSystems)
                {
                    systemGroup.Add(lastSystems);
                }

            }

#if UNITY_EDITOR
            LexSystemsDebugger.AddEcsSystems(rootSystemGroup);
#endif

            return rootSystemGroup;
        }

        // TODO: Add cycle dependencies check, valid cast check (to not mess UpdateBefore and Update in Group)
        private void SortRecursive(List<IEcsSystem> unsorted, List<List<IEcsSystem>> dependencyTable, int dependencyLevel)
        {
            var dependencyLayer = new List<IEcsSystem>();
            dependencyTable.Add(dependencyLayer);

            for (int idx = unsorted.Count - 1; idx >= 0; idx--)
            {
                var currentSystem = unsorted[idx];

                var updateBefore = (UpdateBefore)Attribute.GetCustomAttribute(currentSystem.GetType(), typeof(UpdateBefore));
                if (updateBefore != null)
                {
                    if (SystemMap.TryGetValue(updateBefore.TargetSystemType, out var beforeSystem))
                    {
                        if (dependencyTable[dependencyLevel - 1].Contains(beforeSystem))
                        {
                            dependencyLayer.Add(currentSystem);
                            unsorted.RemoveAt(idx);
                        }
                        //else
                        //{
                        //    var currentSystemParentGroup = ((UpdateInGroup) Attribute.GetCustomAttribute(currentSystem.GetType(), typeof(UpdateInGroup))).TargetGroupType;
                        //    var beforeSystemParentGroup = ((UpdateInGroup) Attribute.GetCustomAttribute(beforeSystem.GetType(), typeof(UpdateInGroup))).TargetGroupType;
                        //    throw new Exception($"System <b>{currentSystem}</b> is in group {currentSystemParentGroup.Name} and <b>{updateBefore.TargetSystemType}</b> is in group {beforeSystemParentGroup.Name}. Only systems are in the same group can be ordered using {nameof(UpdateBefore)} Attribute.");
                        //}
                    }
                    else
                    {
                        throw new Exception($"<b> {currentSystem}</b> has an {nameof(UpdateBefore)} <b>{updateBefore.TargetSystemType}</b> attribute. But <b>{updateBefore.TargetSystemType}</b> is not exist. Use {nameof(UpdateInGroup)} attribute");
                    }
                }
            }

            if (unsorted.Count > 0)
                SortRecursive(unsorted, dependencyTable, ++dependencyLevel);
        }

        protected IEcsSystem GetSystemByType(Type systemType)
        {
            if (SystemMap.TryGetValue(systemType, out var result))
            {
                return result;
            }

            IEcsSystem system = null;

            if (Creator != null)
            {
                system = Creator(systemType);
            }

            // Use Activator as a fall-back for the system creation. 
            // It gives user an ability to create manually only those systems 
            // that have dependencies
            if (system == null)
            {
                system = (IEcsSystem) Activator.CreateInstance(systemType);
            }

            SystemMap[systemType] = system;
            return system;
        }


        private static IEnumerable<Type> GetTypesByScanner(EcsTypesScanner ecsTypesScanner)
        {
            var scanner = ecsTypesScanner == null ? new EcsTypesScanner(EcsScanSettings.Default) : ecsTypesScanner;
            return scanner.ScanSystemTypes(typeof(TWorld));
        }

        public void Dispose() 
        {
            World = null;
            RootSystemGroup = null; 
            SystemMap = null;
            SystemTypes = null;

#if UNITY_EDITOR
            LexSystemsDebugger.RemoveEcsSystems(RootSystemGroup);
#endif
        }
    }

    public static class EcsSystemsExtensions
    {
        public static TTargetSystem FindSystem<TTargetSystem>(this List<IEcsSystem> systems) where TTargetSystem : IEcsSystem
        {
            foreach (var system in systems)
            {
                if (system is TTargetSystem targetSystem)
                {
                    return targetSystem;
                }

                if (system is EcsSystemGroup systemGroup)
                {
                    var result = FindSystem<TTargetSystem>(systemGroup.Systems);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return default;
        }

        public static void FindAllSystemsNonAlloc<TTargetSystem>(this List<IEcsSystem> inputSystems, List<TTargetSystem> outputSystems)
        {
            foreach (var system in inputSystems)
            {
                if (system is TTargetSystem targetSystem)
                {
                    outputSystems.Add(targetSystem);
                }

                if (system is EcsSystemGroup systemGroup)
                {
                    FindAllSystemsNonAlloc(systemGroup.Systems, outputSystems);
                }
            }
        }

        public static bool TryGetSourceHyperLink(this IEcsSystem system, out string result)
        {
            var results = UnityEditor.AssetDatabase.FindAssets(system.GetType().Name);
            foreach (var guid in results)
            {
                result = $"<a href=\"{UnityEditor.AssetDatabase.GUIDToAssetPath(guid)}\" line=\"7\">{UnityEditor.AssetDatabase.GUIDToAssetPath(guid)}:7</a>";
                return true;
            }
            result = string.Empty;
            return false;
        }
    }
}
