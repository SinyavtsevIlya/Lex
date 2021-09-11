using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.LowLevel;
using Nanory.Lex;

namespace Nanory.Lex
{
    public interface IDefaultWorldAttribute { }
    public abstract class WorldAttribute : Attribute { }
    public class TargetWorldAttribute : WorldAttribute { }
    public class AllWorldAttribute : WorldAttribute { }
    public class NoneWorldAttribute : WorldAttribute { }

    public abstract class EcsInstaller<TWorld> : UnityEngine.MonoBehaviour, IDisposable where TWorld : TargetWorldAttribute
    {
        protected EcsWorld _world;
        protected EcsSystems _systems;
        protected Dictionary<Type, IEcsSystem> _systemMap = new Dictionary<Type, IEcsSystem>();
        protected Type[] SystemTypes { get; set; }

        #region UnityMessages
        void Awake()
        {
            var scanner = new EcsTypesScanner(EcsScanSettings.Default);

            SystemTypes = scanner.GetSystemTypesByWorld(typeof(TWorld))
                .Union(scanner.GetOneFrameSystemTypesByWorldGeneric(typeof(TWorld))).ToArray();
            
            CreateSystems();
            Install();
        }

        void OnDestroy()
        {
            Dispose();
        }

        #endregion

        protected void CreateSystems()
        {
            _world = new EcsWorld();
            _systems = new EcsSystems(_world);
            _systemMap = new Dictionary<Type, IEcsSystem>();

            foreach (var systemType in SystemTypes)
            {
                CreateSystemRecursive(systemType);
            }

            // Add a root
            _systems.Add(_systemMap[typeof(SimulationSystemGroup)]);
   
            IEcsSystem CreateSystemRecursive(Type systemType)
            {
                var updateInGroup = (UpdateInGroup) Attribute.GetCustomAttribute(systemType, typeof(UpdateInGroup));
                var targetGroup = updateInGroup != null ? updateInGroup.TargetGroupType : typeof(SimulationSystemGroup);

                var instance = GetSystemByType(systemType);
                var parentInstance = (EcsSystemGroup)GetSystemByType(targetGroup);

                parentInstance.Add(instance);

                return CreateSystemRecursive(targetGroup);
            }
        }

        protected abstract void Install();

        protected IEcsSystem GetSystemByType(Type systemType)
        {
            if (_systemMap.TryGetValue(systemType, out var result))
            {
                return result;
            }

            var system = (IEcsSystem) Activator.CreateInstance(systemType);
            _systemMap[systemType] = system;
            return system;
        }

        public void Dispose() 
        {
            if(_world != null)
            {
                _world.Destroy();
                _world = null;
            }
        }
    }
}
