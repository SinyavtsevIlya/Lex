using System;
using System.Collections.Generic;
using System.Linq;

namespace Nanory.Lex
{
    public class EcsTypesScanner
    {
        private readonly string[] _clientAssemblyNames;

        #region State
        private List<Type> _cachedTypes;
        private List<Type> _componentTypes;
        #endregion

        public EcsTypesScanner(EcsScanSettings settings)
        {
            _clientAssemblyNames = settings.ClientAssemblyNames;

            _cachedTypes = CacheAssemblyTypes();
            _componentTypes = GetAssignableTypes(typeof(IComponentContract)).ToList();
        }

        public EcsTypesScanner() : this(settings: EcsScanSettings.Default)
        {
        }

        public static List<Type> ScanAssembliesTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .ToList();
        }

        public IEnumerable<Type> ScanSystemTypes(params Type[] targetFeatureTypes)
        {
            return GetSystemTypesByFeature(targetFeatureTypes)
                .Union(GetOneFrameSystemTypesFeaturesGeneric(targetFeatureTypes));
        }

        public IEnumerable<Type> GetSystemTypesByFeature(IEnumerable<Type> featureTypes)
        {
            var providedSystemTypes = featureTypes
                .Where(ft => GetTypesByFeature(typeof(SystemTypesProviderBase), featureTypes).Any())
                .SelectMany(ft => GetTypesByFeature(typeof(SystemTypesProviderBase), featureTypes))
                .SelectMany(systemProviderType => (Activator.CreateInstance(systemProviderType) as SystemTypesProviderBase).GetSystemTypes(this));

            return GetTypesByFeature(typeof(IEcsSystem), featureTypes)
                .Union(providedSystemTypes);
        }

        public IEnumerable<Type> GetOneFrameSystemTypesFeaturesGeneric(IEnumerable<Type> featureTypes)
        {
            return GetOneFrameSystemTypesGenericArgumentsByFeature(featureTypes).Select(t =>
            {
                var genericSystemType = typeof(OneFrameSystem<>);
                return genericSystemType.MakeGenericType(t);
            });
        }

        public List<Type> GetOneFrameSystemTypesGenericArgumentsByFeature(IEnumerable<Type> featureTypes)
        {
            return GetAssignableTypes(typeof(IComponentContract))
                .FilterGenericTypesByAttribute<OneFrame>()
                .FilterTypesByFeature(featureTypes)
                .ToList();
        }

        private IEnumerable<Type> GetTypesByFeature(Type ecsType, IEnumerable<Type> targetFeatureTypes)
        {
            var customTypes = GetAssignableTypes(ecsType);
            return customTypes.FilterTypesByFeature(targetFeatureTypes);
        }

        private List<Type> CacheAssemblyTypes()
        {
            return AppDomain.CurrentDomain.GetAssembliesByName(_clientAssemblyNames)
                .AssertIsEmpty($"Check your _clientAssemblyNames: {_clientAssemblyNames}")
                .SelectMany(s => s.GetTypes())
                .ToList();
        }

        public List<Type> GetTypes() => _cachedTypes;

        /// <summary>
        /// Returns types that <b>possibly</b> can be components. (non primitive value types)
        /// </summary>
        public List<Type> GetComponentTypes() => _componentTypes;

        public IEnumerable<Type> GetAssignableTypes(params Type[] typesToScan)
        {
            return _cachedTypes
                .Where(type =>
                {
                    if (type.Namespace == null)
                    {
                        //UnityEngine.Debug.LogWarning($"{type} has no namespace.");
                    }

                    if (typesToScan.Any(t => t == typeof(IComponentContract)))
                    {
                        return type.IsValueType && 
                        !type.IsPrimitive && 
                        type.Namespace != null && 
                        !type.Namespace.StartsWith("System") && 
                        !type.IsEnum;
                    }

                    if (type.IsGenericTypeDefinition || type.IsInterface)
                        return false;

                    if (type.CustomAttributes.Any(a => a.AttributeType == typeof(PreserveAutoCreationAttribute)))
                        return false;

                    if (type.IsAbstract)
                        return false;

                    return typesToScan.Any(t => t.IsAssignableFrom(type));
                });
        }
    }

    [UnityEngine.Scripting.Preserve]
    // Only for internal scanning
    internal interface IComponentContract { }

    public class PreserveAutoCreationAttribute : Attribute { }
}
