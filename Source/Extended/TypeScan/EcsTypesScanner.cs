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
            throw new InvalidOperationException($"Check your _clientAssemblyNames: {string.Join(", ", _clientAssemblyNames)}");

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
            if (baseTypes.Contains(typeof(IComponentContract)))
            {
                if (type.IsValueType &&
                    !type.IsPrimitive &&
                    type.Namespace != null &&
                    !type.Namespace.StartsWith("System") &&
                    !type.IsEnum)
                    yield return type;

                continue;
            }

            if (type.IsGenericTypeDefinition || type.IsInterface || type.IsAbstract)
                continue;

            if (type.CustomAttributes.Any(a => a.AttributeType == typeof(PreserveAutoCreationAttribute)))
                continue;

            if (baseTypes.Any(baseType => baseType.IsAssignableFrom(type)))
                yield return type;
        }
    }

    public IEnumerable<Type> ScanSystemTypes(params Type[] targetFeatureTypes)
    {
        var invalidTypes = targetFeatureTypes
            .Where(ft => !typeof(FeatureBase).IsAssignableFrom(ft))
            .ToList();

        foreach (var invalidType in invalidTypes)
            Debug.LogError($"{invalidType.FullName} must be inherited from {nameof(FeatureBase)}");

        if (invalidTypes.Any())
            throw new ArgumentException("Invalid feature types passed to ScanSystemTypes.");

        return GetSystemTypesByFeature(targetFeatureTypes)
            .Union(GetOneFrameSystemTypesFeaturesGeneric(targetFeatureTypes));
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

        var ecsSystemTypes = GetTypesByFeature(typeof(IEcsSystem), featureTypes);

        return ecsSystemTypes.Union(providedSystemTypes);
    }

    public IEnumerable<Type> GetOneFrameSystemTypesFeaturesGeneric(IEnumerable<Type> featureTypes)
    {
        var genericArgs = GetOneFrameSystemTypesGenericArgumentsByFeature(featureTypes);

        foreach (var arg in genericArgs)
        {
            yield return typeof(OneFrameSystem<>).MakeGenericType(arg);
        }
    }

    public List<Type> GetOneFrameSystemTypesGenericArgumentsByFeature(IEnumerable<Type> featureTypes)
    {
        return GetAssignableTypes(typeof(IComponentContract))
            .FilterGenericTypesByAttribute<OneFrame>()
            .FilterTypesByFeature(featureTypes)
            .ToList();
    }

    private IEnumerable<Type> GetTypesByFeature(Type baseType, IEnumerable<Type> featureTypes)
    {
        var candidates = GetAssignableTypes(baseType);
        return candidates.FilterTypesByFeature(featureTypes);
    }
    
    [UnityEngine.Scripting.Preserve]
    // Only for internal scanning
    internal interface IComponentContract { }
    
}
}
