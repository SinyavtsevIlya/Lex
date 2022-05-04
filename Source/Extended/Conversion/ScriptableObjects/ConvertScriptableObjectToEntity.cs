using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;

namespace Nanory.Lex.Conversion.ScriptableObjects
{
    [Serializable]
    public abstract class ConversionComponent : IConvertScriptableObjectToEntity
    {
        [HideInInspector]
        [NonSerialized]
        public ConversionEntity ConversionEntity;

        public abstract void Convert(int entity, ScriptableObjectConversionSystem conversionSystem);
    }

    [CreateAssetMenu(fileName = "ScriptableEntity", menuName = "Lex/ScriptableEntity")]
    public class ConversionEntity : ScriptableObject, IConvertScriptableObjectToEntity
    {
        [SerializeReference]
        public List<ConversionComponent> _components = new List<ConversionComponent>();

        public bool Has<TConversionComponent>() where TConversionComponent : ConversionComponent
        {
            foreach (var component in _components)
            {
                if (component is TConversionComponent)
                {
                    return true;
                }
            }
            return false;
        }

        public TConversionComponent Get<TConversionComponent>() where TConversionComponent : ConversionComponent
        {
            foreach (var value in _components)
            {
                if (value is TConversionComponent c)
                {
                    return c;
                }
            }
            throw new Exception($"entity {this.name} doesn't have {typeof(TConversionComponent)} component");
        }

        public bool TryGet<TBluepintComponent>(out TBluepintComponent component) where TBluepintComponent : ConversionComponent
        {
            component = null;
            foreach (var value in _components)
            {
                if (value is TBluepintComponent c)
                {
                    component = c;
                    return true;
                }
            }
            return false;
        }

        public ConversionEntity Add<TBluepintComponent>(TBluepintComponent component) where TBluepintComponent : ConversionComponent
        {
            foreach (var c in _components)
            {
                if (c is TBluepintComponent)
                    throw new System.Exception("Component is already on a Conversion");
            }
            _components.Add(component);
            return this;
        }

        public void Convert(int entity, ScriptableObjectConversionSystem conversionSystem)
        {
            foreach (var component in _components)
            {
                component.Convert(entity, conversionSystem);  
            }
        }

#if UNITY_EDITOR
        public ConversionComponent[] GetAvailableComponents => AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(ConversionComponent).IsAssignableFrom(t))
            .Where(t => !_components.Any(v => v.GetType() == t))
            .Where(t => !t.IsAbstract)
            .Select(t => Activator.CreateInstance(t) as ConversionComponent)
            .ToArray(); 
#endif
    }

    public static class ScriptableObjectConversionExtensions
    {
        public static void Convert(this EcsWorld world, ConversionEntity conversionEntity)
        {
            ref var requestEntity = ref world.Add<ConvertScriptableObjectRequest>(world.NewEntity());
            requestEntity.Value = conversionEntity;
        }
    }

    public struct ConvertScriptableObjectRequest
    {
        public ConversionEntity Value;
    }

    public interface IConvertScriptableObjectToEntity
    {
        void Convert(int entity, ScriptableObjectConversionSystem conversionSystem);
    }

    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class ScriptableObjectConversionSystem : IEcsRunSystem, IEcsInitSystem, IEcsEntityCommandBufferLookup
    {
        private Dictionary<ScriptableObject, int> _conversionMap = new Dictionary<ScriptableObject, int>();
        private EcsConversionWorldWrapper _conversionWorldWrapper;
        private EcsPool<ConvertScriptableObjectRequest> _requestsPool;
        private EcsFilter _requestsFilter;
        protected List<EntityCommandBufferSystem> _entityCommandBufferSystems;

        private EcsSystems _ecsSystems;
        public EcsSystems EcsSystems => _ecsSystems;

        public EcsConversionWorldWrapper World => _conversionWorldWrapper;

        public int GetPrimaryEntity(ScriptableObject scriptableObject)
        {
            return GetPrimaryEntity(scriptableObject, out var _);
        }

        public int GetPrimaryEntity(ScriptableObject scriptableObject, out bool isNew)
        {
            if (_conversionMap.TryGetValue(scriptableObject, out var newEntity))
            {
                isNew = false;
                return newEntity;
            }

            newEntity = _conversionWorldWrapper.NewEntity();
            _conversionMap[scriptableObject] = newEntity;

            isNew = true;
            return newEntity;
        }

        public int Convert(ConversionEntity conversionEntity)
        {
#if DEBUG
            if (conversionEntity == null)
                throw new System.ArgumentException("Unable to convert. Passed conversionEntity is null");
#endif
            var entity = GetPrimaryEntity(conversionEntity, out var isNew);

            if (!isNew)
            {
                return entity;
            }

            World.Dst.SetAsPrefab(entity);

            conversionEntity.Convert(entity, this);

            return entity;
        }

        public void Init(EcsSystems systems)
        {
            _ecsSystems = systems;
            _conversionWorldWrapper = new EcsConversionWorldWrapper(systems.GetWorld());
            _requestsPool = _conversionWorldWrapper.Dst.GetPool<ConvertScriptableObjectRequest>();
            _requestsFilter = _conversionWorldWrapper.Dst.Filter<ConvertScriptableObjectRequest>().End();
        }

        public void Run(EcsSystems systems)
        {
            foreach (var requestEntity in _requestsFilter)
            {
                ref var request = ref _requestsPool.Get(requestEntity);
                Convert(request.Value);
                _conversionWorldWrapper.DelEntity(requestEntity);
            }
        }

        public IEcsEntityCommandBufferLookup SetEntityCommandBufferSystemsLookup(List<EntityCommandBufferSystem> systems)
        {
            _entityCommandBufferSystems = systems;
            return this;
        }

        public EntityCommandBuffer GetCommandBufferFrom<TSystem>() where TSystem : EntityCommandBufferSystem
        {
            foreach (var system in _entityCommandBufferSystems)
            {
                if (system is TSystem)
                    return system.GetBuffer();
            }

            throw new Exception($"no system {typeof(TSystem)} presented in the entityCommandBufferSystems lookup");
        }
    }
}
