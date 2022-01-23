using System;
using System.Collections.Generic;
using System.Linq;
using Nanory.Lex;
using UnityEngine.Assertions;

namespace Nanory.Lex
{
    public class EcsTypesScanner
    {
        private readonly string[] _clientAssemblyNames;

        public EcsTypesScanner(EcsScanSettings settings)
        {
            _clientAssemblyNames = settings.ClientAssemblyNames;
        }

        public EcsTypesScanner()
        {
            var settings = EcsScanSettings.Default;

            _clientAssemblyNames = settings.ClientAssemblyNames;
        }

        public IEnumerable<Type> ScanSystemTypes(params Type[] targetFeatureTypes)
        {
            return GetSystemTypesByFeature(targetFeatureTypes)
                .Union(GetOneFrameSystemTypesFeaturesGeneric(targetFeatureTypes));
        }

        public IEnumerable<Type> GetSystemTypesByFeature(IEnumerable<Type> featureTypes)
        {
            return GetTypesByFeature(typeof(IEcsSystem), featureTypes);
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
            return GetAssignableTypes(typeof(IComponentMock))
                .FilterGenericTypesByAttribute<OneFrame>()
                .FilterTypesByFeature(featureTypes)
                .ToList();
        }

        private IEnumerable<Type> GetTypesByFeature(Type ecsType, IEnumerable<Type> targetFeatureTypes)
        {
            var customTypes = GetAssignableTypes(ecsType);
            return customTypes.FilterTypesByFeature(targetFeatureTypes);
        }

        public IEnumerable<Type> GetAssignableTypes(params Type[] typesToScan)
        {
            return AppDomain.CurrentDomain.GetAssembliesByName(_clientAssemblyNames)
                .AssertIsEmpty($"Check your _clientAssemblyNames: {_clientAssemblyNames}")
                .SelectMany(s => s.GetTypes())
                .Where(type => 
                {
                    if (type.Namespace == null)
                    {
                        UnityEngine.Debug.LogWarning($"{type} has no namespace.");
                    }

                    if (typesToScan.Any(t => t == typeof(IComponentMock)))
                    {
                        return type.IsValueType && !type.IsPrimitive && type.Namespace != null && !type.Namespace.StartsWith("System") && !type.IsEnum;
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
    internal interface IComponentMock { }

    public class PreserveAutoCreationAttribute : Attribute { }
}
