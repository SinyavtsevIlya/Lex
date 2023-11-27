using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Nanory.Lex.Conversion
{
    [CreateAssetMenu(fileName = "AuthoringEntity", menuName = "Lex/AuthoringEntity")]
    public class AuthoringEntity : ScriptableObject, IConvertToEntity
    {
        private static IConversionStrategy _fallbackConversionStrategy = DefaultConversionStrategy.Value;
        
        private IConversionStrategy _customConversionStrategy;
        
        /// <summary>
        /// List of components of <b>this</b> Authoring-Entity (components of base Authoring-entities are not included)
        /// </summary>
        [SerializeReference]
        public List<AuthoringComponent> _components = new List<AuthoringComponent>();

        /// <summary>
        /// Complete list of components of this Authoring-Entity and all its base Authoring-entities.
        /// NOTE: Use <see cref="Components"/> lazy property to get an actual list of components
        /// </summary>

        private List<AuthoringComponent> _overallComponents;

#if UNITY_EDITOR
        [Nanory.Lex.UnityEditorIntegration.BaseAuthoringEntity]
#endif

        [SerializeField] private AuthoringEntity _baseAuthoringEntity;

        public static void OverrideFallbackConversionStrategy(IConversionStrategy conversionStrategy) =>
            _fallbackConversionStrategy = conversionStrategy;

        public void OverrideCustomConversionStrategy(IConversionStrategy conversionStrategy) =>
            _customConversionStrategy = conversionStrategy;

        /// <summary>
        /// Complete list of components of this Authoring-Entity and all its base Authoring-entities.
        /// </summary>
        public List<AuthoringComponent> Components
        {
            get
            {
                if (_overallComponents == null)
                {
                    _overallComponents = new List<AuthoringComponent>();
                    MergeNonAlloc(_overallComponents);
                }
                return _overallComponents;
            }
        }

        public bool Has<TAuthoringComponent>(bool includeBase = true) where TAuthoringComponent : AuthoringComponent
        {
            var components = includeBase ? Components : _components;
            
            foreach (var component in components)
                if (component is TAuthoringComponent)
                    return true;

            return false;
        }
        
        public bool Has(Type componentType, bool includeBase = true)
        {
            var components = includeBase ? Components : _components;
            
            foreach (var component in components)
                if (component.GetType() == componentType)
                    return true;

            return false;
        }

        public TAuthoringComponent Get<TAuthoringComponent>(bool includeBase = true) where TAuthoringComponent : AuthoringComponent
        {
            var components = includeBase ? Components : _components;
            
            foreach (var value in components)
                if (value is TAuthoringComponent c)
                    return c;

            throw new Exception($"entity {this.name} doesn't have {typeof(TAuthoringComponent)} component");
        }

        public bool TryGet<TAuthoringComponent>(out TAuthoringComponent component) where TAuthoringComponent : AuthoringComponent
        {
            component = null;
            foreach (var value in Components)
            {
                if (value is TAuthoringComponent c)
                {
                    component = c;
                    return true;
                }
            }
            return false;
        }

        public AuthoringEntity Add<TAuthoringComponent>(TAuthoringComponent component) where TAuthoringComponent : AuthoringComponent
        {
            foreach (var c in _components)
                if (c is TAuthoringComponent)
                    throw new System.Exception($"Component {c} is already on a {this}");
            
            _components.Add(component);

            // ensure Components property to be rescanned, and all Merges be called.  
            _overallComponents = null;
            return this;
        }

        public void Convert(int entity, ConvertToEntitySystem convertToEntitySystem)
        {
            var strategy = _customConversionStrategy ?? _fallbackConversionStrategy;
            strategy.Convert(this, entity, convertToEntitySystem);
        }
        
        public void MergeNonAlloc(List<AuthoringComponent> result)
        {
            if (_baseAuthoringEntity != null) 
                _baseAuthoringEntity.MergeNonAlloc(result);

            result.MergeNonAllocDestructive(_components);
        }

#if UNITY_EDITOR
        public AuthoringComponent[] GetAvailableComponents() => AvailableComponents
            .Where(t => !_components.Any(v => v.GetType() == t))
            .Select(t => Activator.CreateInstance(t) as AuthoringComponent)
            .ToArray();

        public static Type[] AvailableComponents;

        static AuthoringEntity()
        {
            AvailableComponents = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(AuthoringComponent).IsAssignableFrom(t))
            .Where(t => !t.IsAbstract).ToArray();
        }
#endif
    }
}
