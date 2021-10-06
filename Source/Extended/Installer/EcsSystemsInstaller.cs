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

    public class EcsSystemOrderer<TWorld> : IDisposable where TWorld : TargetWorldAttribute
    {
        protected EcsWorld World { get; private set; }
        protected EcsSystemGroup RootSystemGroup { get; private set; }
        protected Dictionary<Type, IEcsSystem> SystemMap { get; private set; }
        protected Func<Type, IEcsSystem> Creator { get; private set; }
        protected Type[] SystemTypes { get; set; }


        public EcsSystemOrderer(EcsWorld world, Func<Type, IEcsSystem> creator = null, EcsTypesScanner ecsTypesScanner = null)
        {
            World = world;
            SystemMap = new Dictionary<Type, IEcsSystem>();
            Creator = creator;

            var scanner = ecsTypesScanner == null ? new EcsTypesScanner(EcsScanSettings.Default) : ecsTypesScanner;

            var defaultSystemGroupTypes = new Type[]
            {
                typeof(SimulationSystemGroup),
                typeof(PresentationSystemGroup),
                typeof(BeginSimulationECBSystem),
            };

            var conversionSystemTypes = new Type[]
            {
                typeof(GameObjectConversionSystem)
            };

            SystemTypes = scanner
                .ScanSystemTypes(typeof(TWorld))
                .Union(defaultSystemGroupTypes)
                .Union(conversionSystemTypes)
                .Union(UISystemTypesRegistry.Values)
                .ToArray();
        }

        public EcsSystemGroup CreateSystemsOrdered()
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
            var baseSystems = SystemMap.Values.Where(s => s is EcsSystemBase).Select(s => s as EcsSystemBase).ToList();
            var systemGroups = SystemMap.Values.Where(s => s is EcsSystemGroup).Select(s => s as EcsSystemGroup).ToList();

            commandBufferSystems.ForEach(cbs => cbs.SetDstWorld(World));
            baseSystems.ForEach(bs => bs.SetEntityCommandBufferSystemsLookup(commandBufferSystems));

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

                var orderLastSystems = new List<IEcsSystem>();

                // Check for special attributes parameters OrderFirst/Last...
                for (int idx = unsorted.Count - 1; idx >= 0; idx--)
                {
                    var currentSystem = unsorted[idx];

                    var updateInGroup = (UpdateInGroup) Attribute.GetCustomAttribute(currentSystem.GetType(), typeof(UpdateInGroup));
                    if (updateInGroup != null)
                    {
                        // Put a special "OrderFirst" systems to the very beginning 
                        if (updateInGroup.OrderFirst)
                        {
                            dependencyTable[0].Add(currentSystem);
                            unsorted.RemoveAt(idx);
                        }

                        // Remove from list special "OrderLast" systems to add them later
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
                systemGroup.Systems = dependencyTable.SelectMany(layer => layer).ToList();

                // And in the and add "OrderLast" systems
                foreach (var lastSystems in orderLastSystems)
                {
                    systemGroup.Add(lastSystems);
                }

                void SortRecursive(List<IEcsSystem> unsorted, List<List<IEcsSystem>> dependencyTable, int dependencyLevel)
                {
                    var dependencyLayer = new List<IEcsSystem>();
                    dependencyTable.Add(dependencyLayer);

                    for (int idx = unsorted.Count - 1; idx >= 0; idx--)
                    {
                        var currentSystem = unsorted[idx];

                        var updateBefore = (UpdateBefore) Attribute.GetCustomAttribute(currentSystem.GetType(), typeof(UpdateBefore));
                        if (updateBefore != null)
                        {
                            if (SystemMap.TryGetValue(updateBefore.TargetSystemType, out var beforeSystem))
                            {
                                if (dependencyTable[dependencyLevel - 1].Contains(beforeSystem))
                                {
                                    dependencyLayer.Add(currentSystem);
                                    unsorted.RemoveAt(idx);
                                }
                                else
                                {
                                    throw new Exception($"System <b>{currentSystem}</b> and <b>{updateBefore.TargetSystemType}</b> that are in different Groups. Only systems are in the same group can be ordered using {nameof(UpdateBefore)} Attribute.");
                                }
                            }
                            else
                            {
                                throw new Exception($"<b> {currentSystem}</b> has an {nameof(UpdateBefore)} <b>{updateBefore.TargetSystemType}</b> attribute. But <b>{updateBefore.TargetSystemType}</b> is not exist. Use {nameof(UpdateInGroup)} attribute");
                            }
                        }
                    }

                    if (unsorted.Count > 0)
                        SortRecursive(unsorted, dependencyTable, dependencyLevel++);
                }
            }

#if UNITY_EDITOR
            var debugger = UnityEditor.EditorWindow.GetWindow<LexSystemsDebugger>();
            LexSystemsDebugger.AddEcsSystems(rootSystemGroup);
#endif

            return rootSystemGroup;
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

        public void Dispose() 
        {
            World = null;
            RootSystemGroup = null;
            SystemMap = null;
            SystemTypes = null;

#if UNITY_EDITOR
            var debugger = UnityEditor.EditorWindow.GetWindow<LexSystemsDebugger>();
            LexSystemsDebugger.RemoveEcsSystems(RootSystemGroup);
#endif
        }
    }
}
