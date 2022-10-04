using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nanory.Lex.Conversion
{
    [CreateAssetMenu(fileName = "AuthoringEntity", menuName = "Lex/AuthoringEntity")]
    public class AuthoringEntity : ScriptableObject, IConvertToEntity, ISerializationCallbackReceiver
    {
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
                    GetBaseComponentsNonAlloc(_overallComponents);
                }
                return _overallComponents;
            }
        }

        public bool IsPrefab { get; private set; } = true;

        public bool Has<TAuthoringComponent>() where TAuthoringComponent : AuthoringComponent
        {
            foreach (var component in Components)
            {
                if (component is TAuthoringComponent)
                {
                    return true;
                }
            }
            return false;
        }

        public TAuthoringComponent Get<TAuthoringComponent>() where TAuthoringComponent : AuthoringComponent
        {
            foreach (var value in Components)
            {
                if (value is TAuthoringComponent c)
                {
                    return c;
                }
            }
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
            {
                if (c is TAuthoringComponent)
                    throw new System.Exception($"Component {c} is already on a {this}");
            }
            component.AuthoringEntity = this;
            _components.Add(component);
            return this;
        }

        public void Convert(int entity, ConvertToEntitySystem сonvertToEntitySystem)
        {
            foreach (var component in Components)
            {
                component.Convert(entity, сonvertToEntitySystem);
            }
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            foreach (var component in _components)
            {
                component.AuthoringEntity = this;
            }
        }

        public AuthoringEntity Instantiate()
        {
            var copy = Instantiate(this);
            copy.IsPrefab = false;
            return copy;
        }

        public void GetBaseComponentsNonAlloc(List<AuthoringComponent> result)
        {
            if (_baseAuthoringEntity != null)
            {
                _baseAuthoringEntity.GetBaseComponentsNonAlloc(result);
            }

            foreach (var overrideComponent in _components)
            {
                var hasFound = false;
                for (var idx = 0; idx < result.Count; idx++)
                {
                    var component = result[idx];
                    if (component.GetType().Equals(overrideComponent.GetType()))
                    {
                        result[idx] = overrideComponent;
                        hasFound = true;
                        break;
                    }
                }

                if (!hasFound)
                {
                    result.Add(overrideComponent);
                }
            }
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
