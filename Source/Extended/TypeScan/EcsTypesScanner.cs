using System;
using System.Collections.Generic;
using System.Linq;
using Nanory.Lex;

namespace Nanory.Lex
{
    public class EcsTypesScanner
    {
        private readonly string[] _clientAssemblyNames;
        private readonly string _clientNamespaceTag;

        public EcsTypesScanner(EcsScanSettings settings)
        {
            _clientAssemblyNames = settings.ClientAssemblyNames;
            _clientNamespaceTag = settings.ClientNamespaceTag;
        }

        public EcsTypesScanner()
        {
            var settings = EcsScanSettings.Default;

            _clientAssemblyNames = settings.ClientAssemblyNames;
            _clientNamespaceTag = settings.ClientNamespaceTag;
        }

        public IEnumerable<Type> ScanSystemTypes(Type targetWorldAttributeType)
        {
            return GetSystemTypesByWorld(targetWorldAttributeType)
                .Union(GetOneFrameSystemTypesByWorldGeneric(targetWorldAttributeType));
        }

        public IEnumerable<Type> GetSystemTypesByWorld(Type targetWorldAttributeType)
        {
            return GetTypesByWorld(typeof(IEcsSystem), targetWorldAttributeType);
        }

        public IEnumerable<Type> GetOneFrameSystemTypesByWorldGeneric(Type worldAttributeType)
        {
            return GetOneFrameSystemTypesGenericArgumentsByWorld(worldAttributeType).Select(t =>
            {
                var genericSystemType = typeof(OneFrameSystem<>);
                return genericSystemType.MakeGenericType(t);
            });
        }

        public List<Type> GetOneFrameSystemTypesGenericArgumentsByWorld(Type worldAttributeType)
        {
            return GetClientTypes(typeof(IComponentMock))
                .FilterGenericTypesByAttribute<OneFrame>()
                .FilterTypesByWorld(worldAttributeType)
                .ToList();
        }

        public IEnumerable<Type> GetClientTypes(params Type[] typesToScan)
        {
            return GetTypesFromTaggedNamespaces(_clientNamespaceTag, typesToScan);
        }

        private IEnumerable<Type> GetTypesByWorld(Type typeToScan, Type targetWorldAttributeType)
        {
            var customTypes = GetClientTypes(typeToScan);
            return customTypes.FilterTypesByWorld(targetWorldAttributeType);
        }

        private IEnumerable<Type> GetTypesFromTaggedNamespaces(string namespaceTag, params Type[] typesToScan)
        {
            return AppDomain.CurrentDomain.GetAssembliesByName(_clientAssemblyNames)
                .AssertIsEmpty($"Check your _clientAssemblyNames: {_clientAssemblyNames}")
                .SelectMany(s => s.GetTypes())
                .Where(t => 
                {
                    if (string.IsNullOrEmpty(namespaceTag)) return true;
                    return t.FullName.Contains(namespaceTag);
                })
                .Where(type => 
                {
                    if (typesToScan.Any(t => t == typeof(IComponentMock)))
                    {
                        return type.IsValueType && !type.IsPrimitive && !type.Namespace.StartsWith("System") && !type.IsEnum;
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
