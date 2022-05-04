using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.Serialization;

namespace Nanory.Lex.Conversion
{
    [CreateAssetMenu(fileName = "AuthoringEntity", menuName = "Lex/AuthoringEntity")]
    public class AuthoringEntity : ScriptableObject, IConvertToEntity, ISerializationCallbackReceiver
    {
        [SerializeReference]
        public List<AuthoringComponent> _components = new List<AuthoringComponent>();

        public bool IsPrefab { get; private set; } = true;

        public bool Has<TAuthoringComponent>() where TAuthoringComponent: AuthoringComponent
        {
            foreach (var component in _components)
            {
                if (component is TAuthoringComponent)
                {
                    return true; 
                }
            }
            return false;
        }

        public TAuthoringComponent Get<TAuthoringComponent>() where TAuthoringComponent: AuthoringComponent
        {
            foreach (var value in _components)
            {
                if (value is TAuthoringComponent c)
                {
                    return c;
                }
            }
            throw new Exception($"entity {this.name} doesn't have {typeof(TAuthoringComponent)} component");
        }

        public bool TryGet<TAuthoringComponent>(out TAuthoringComponent component) where TAuthoringComponent: AuthoringComponent
        {
            component = null;
            foreach (var value in _components)
            {
                if (value is TAuthoringComponent c)
                {
                    component = c;
                    return true;
                }
            }
            return false;
        }

        public AuthoringEntity Add<TAuthoringComponent>(TAuthoringComponent component) where TAuthoringComponent: AuthoringComponent
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
            foreach (var component in _components)
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
