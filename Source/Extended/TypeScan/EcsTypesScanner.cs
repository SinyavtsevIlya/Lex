using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nanory.Lex
{
    public class EcsTypesScanner
    {
        private readonly string[] _clientAssemblyNames;

        #region State

        private readonly List<Type> _cachedTypes;
        private readonly List<Type> _componentTypes;

        #endregion

        public EcsTypesScanner(EcsScanSettings settings)
        {
            _clientAssemblyNames = settings.ClientAssemblyNames;

            _cachedTypes = CacheAssemblyTypes();
            _componentTypes = GetComponentTypesInternal().ToList();
        }

        public EcsTypesScanner() : this(EcsScanSettings.Default)
        {
        }

        public static List<Type> ScanAssembliesTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .ToList();
        }

        private List<Type> CacheAssemblyTypes()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssembliesByName(_clientAssemblyNames).ToList();

            if (!assemblies.Any())
                throw new InvalidOperationException(
                    $"Check your _clientAssemblyNames: {string.Join(", ", _clientAssemblyNames)}");

            return assemblies.SelectMany(a => a.GetTypes()).ToList();
        }

        [UnityEngine.Scripting.Preserve]
        public IReadOnlyList<Type> GetTypes() => _cachedTypes;

        public IReadOnlyList<Type> GetComponentTypes() => _componentTypes;

        private IEnumerable<Type> GetComponentTypesInternal()
        {
            return _cachedTypes.Where(type =>
                type.IsValueType &&
                !type.IsPrimitive &&
                type.Namespace != null &&
                !type.Namespace.StartsWith("System") &&
                !type.IsEnum);
        }

        public IEnumerable<Type> GetAssignableTypes(params Type[] baseTypes)
        {
            foreach (var type in _cachedTypes)
            {
                if (type.IsGenericTypeDefinition || type.IsInterface || type.IsAbstract)
                    continue;

                if (type.CustomAttributes.Any(a => a.AttributeType == typeof(PreserveAutoCreationAttribute)))
                    continue;

                if (baseTypes.Any(baseType => baseType.IsAssignableFrom(type)))
                    yield return type;
            }
        }

        public IEnumerable<Type> ScanSystemTypes(IEnumerable<Type> targetFeatureTypes)
        {
            var featureTypes = targetFeatureTypes.ToList();
            
            var invalidTypes = featureTypes
                .Where(ft => !typeof(FeatureBase).IsAssignableFrom(ft))
                .ToList();

            foreach (var invalidType in invalidTypes)
                Debug.LogError($"{invalidType.FullName} must be inherited from {nameof(FeatureBase)}");

            if (invalidTypes.Any())
                throw new ArgumentException("Invalid feature types passed to ScanSystemTypes.");

            return GetSystemTypesByFeature(featureTypes);
        }

        public IEnumerable<Type> GetSystemTypesByFeature(IEnumerable<Type> featureTypes)
        {
            var systemProviderTypes = GetTypesByFeature(typeof(SystemTypesProviderBase), featureTypes).ToList();

            var providedSystemTypes = systemProviderTypes.SelectMany(spType =>
            {
                if (Activator.CreateInstance(spType) is SystemTypesProviderBase provider)
                    return provider.GetSystemTypes(this);

                Debug.LogError($"Failed to create instance of {spType.FullName}");
                return Enumerable.Empty<Type>();
            });

            var ecsSystemTypes = GetTypesByFeature(typeof(EcsSystemBase), featureTypes);

            return ecsSystemTypes.Union(providedSystemTypes);
        }

        private IEnumerable<Type> GetTypesByFeature(Type baseType, IEnumerable<Type> featureTypes)
        {
            var candidates = GetAssignableTypes(baseType);
            return candidates.FilterTypesByFeature(featureTypes);
        }
    }
}