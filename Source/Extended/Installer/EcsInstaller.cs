using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.LowLevel;
using Nanory.Lex;
using UnityEngine;

namespace Nanory.Lex
{
    public interface IDefaultWorldAttribute { }
    public abstract class WorldAttribute : Attribute { }
    public class TargetWorldAttribute : WorldAttribute { }
    public class AllWorldAttribute : WorldAttribute { }
    public class NoneWorldAttribute : WorldAttribute { }

    public class EcsInstaller<TWorld> : IDisposable where TWorld : TargetWorldAttribute
    {
        protected EcsWorld World { get; private set; }
        protected EcsSystems Systems { get; private set; }
        protected Dictionary<Type, IEcsSystem> SystemMap { get; private set; }

        public EcsInstaller(EcsWorld world, EcsSystems systems)
        {
            World = world;
            Systems = systems;
            SystemMap = new Dictionary<Type, IEcsSystem>();

            var scanner = new EcsTypesScanner(EcsScanSettings.Default);

            SystemTypes = scanner.GetSystemTypesByWorld(typeof(TWorld))
                .Union(scanner.GetOneFrameSystemTypesByWorldGeneric(typeof(TWorld))
                .Union(new Type[] { typeof(BeginSimulationECBSystem) }))
                .ToArray();
            
            Install();
            CreateSystems();

#if UNITY_EDITOR
            LexDebugger.AddEcsSystems(systems);
#endif
        }

        protected Type[] SystemTypes { get; set; }
       
        protected void CreateSystems()
        {
            foreach (var systemType in SystemTypes)
            {
                CreateSystemRecursive(systemType);
            }

            // Add a root
            Systems.Add(GetSystemByType(typeof(RootSystemGroup)));

            var commandBufferSystems = SystemMap.Values.Where(s => s is EntityCommandBufferSystem).Select(s => s as EntityCommandBufferSystem).ToList();
            var baseSystems = SystemMap.Values.Where(s => s is EcsSystemBase).Select(s => s as EcsSystemBase).ToList();
            var systemGroups = SystemMap.Values.Where(s => s is EcsSystemGroup).Select(s => s as EcsSystemGroup).ToList();

            //SystemTypes.ToList().ForEach(x => Debug.Log(x));
            commandBufferSystems.ForEach(cbs => cbs.SetDstWorld(World));
            baseSystems.ForEach(bs => bs.SetEntityCommandBufferSystemsLookup(commandBufferSystems));

            foreach (var systemGroup in systemGroups)
            {
                SortSystemGroup(systemGroup);
            }

            //systemGroups.ForEach(x => x.Systems.ForEach(x => Debug.Log(x)));

            void CreateSystemRecursive(Type systemType)
            {
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

                if (targetGroup == typeof(RootSystemGroup))
                    return;

                if (updateInGroup != null)
                    CreateSystemRecursive(targetGroup);
            }

            void SortSystemGroup(EcsSystemGroup systemGroup)
            {
                var unsorted = new List<IEcsSystem>(systemGroup.Systems);

                var dependencyTable = new List<List<IEcsSystem>>();
                // Add a root dependency level
                dependencyTable.Add(new List<IEcsSystem>());

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
        }

        protected virtual void Install() { }

        protected IEcsSystem GetSystemByType(Type systemType)
        {
            if (SystemMap.TryGetValue(systemType, out var result))
            {
                return result;
            }

            var system = (IEcsSystem) Activator.CreateInstance(systemType);
            SystemMap[systemType] = system;
            return system;
        }

        public void Dispose() 
        {
#if UNITY_EDITOR
            LexDebugger.RemoveEcsSystems(Systems);
#endif

            World = null;
            Systems = null;
            SystemMap = null;
            SystemTypes = null;
        }
    }
}
