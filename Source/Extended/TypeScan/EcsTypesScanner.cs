﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nanory.Lex;

namespace Nanory.Lex
{
    public class EcsTypesScanner
    {
        private readonly string _clientAssemblyName;
        private readonly string _frameworkAssemblyName;
        private readonly string _clientNamespaceTag;
        private readonly string _frameworkNamespaceTag;

        public EcsTypesScanner(EcsScanSettings settings)
        {
            _clientAssemblyName = settings.ClientAssemblyName;
            _frameworkAssemblyName = settings.FrameworkAssemblyName;
            _clientNamespaceTag = settings.ClientNamespaceTag;
            _frameworkNamespaceTag = settings.FrameworkNamespaceTag;
        }

        public EcsTypesScanner()
        {
            var settings = EcsScanSettings.Default;

            _clientAssemblyName = settings.ClientAssemblyName;
            _frameworkAssemblyName = settings.FrameworkAssemblyName;
            _clientNamespaceTag = settings.ClientNamespaceTag;
            _frameworkNamespaceTag = settings.FrameworkNamespaceTag;
        }

        public IEnumerable<Type> GetSystemTypesByWorld(Type targetWorldAttributeType)
        {
            return GetTypesByWorld(typeof(EcsSystemBase), targetWorldAttributeType);
        }

        public List<Type> GetOneFrameSystemTypesGenericArgumentsByWorld(Type worldAttributeType)
        {
            var frameworkOneFrameSystemTypes = GetFrameworkTypes()
                .FilterGenericTypesByAttribute<OneFrame>().ToList();

            return GetClientTypes(typeof(IComponentMock))
                .FilterGenericTypesByAttribute<OneFrame>()
                .FilterTypesByWorld(worldAttributeType)
                .Union(frameworkOneFrameSystemTypes).ToList();
        }

        public IEnumerable<Type> GetOneFrameSystemTypesByWorldGeneric(Type worldAttributeType)
        {
            return GetOneFrameSystemTypesGenericArgumentsByWorld(worldAttributeType).Select(t =>
            {
                var genericSystemType = typeof(OneFrameSystem<>);
                return genericSystemType.MakeGenericType(t);
            });
        }

        public IEnumerable<string> GetNamespacesRecursive(Type type)
        {
            var result = new List<string>();

            if (type.GetGenericArguments().Count() > 0)
            {
                foreach (var arg in type.GetGenericArguments())
                {
                    result.AddRange(GetNamespacesRecursive(arg));
                }
            }
            else
            {
                result.Add(type.Namespace);
            }

            return result;
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

        private IEnumerable<Type> GetClientSystemTypes()
        {
            return GetClientTypes(typeof(EcsSystemBase));
        }

        private IEnumerable<Type> GetFrameworkTypes(params Type[] typesToScan)
        {
            return GetTypesFromTaggedNamespaces(_frameworkNamespaceTag, typesToScan);
        }

        private IEnumerable<Type> GetTypesFromTaggedNamespaces(string namespaceTag, params Type[] typesToScan)
        {
            return AppDomain.CurrentDomain.GetAssembliesByName(_clientAssemblyName, _frameworkAssemblyName)
                .SelectMany(s => s.GetTypes())
                .Where(t => t.FullName.Contains(namespaceTag))
                .Where(p => 
                {
                    if (typesToScan == null) return true;
                    else if (typesToScan.Length == 0) return true;
                    else return typesToScan.Any(t => t.IsAssignableFrom(p));
                })
                //.Where(t => !t.IsGenericType) // TODO: uncomment this, and refactor generic types filtering
                ;
        }

        public class SystemOrderHelpers
        {
            public static int GetPriorityBySystemTag(string tag)
            {
                throw new NotImplementedException();
            }
        }
    }

    public static class TypeExtensions
    {
        public static IEnumerable<Type> FilterTypesByWorld(this IEnumerable<Type> inputTypes, Type targetWorldAttributeType)
        {
            var noneWorldTypes = FilterTypesByAttribute<NoneWorldAttribute>(inputTypes);
            inputTypes = inputTypes.Except(noneWorldTypes);

            var allWorldTypes = FilterTypesByAttribute<AllWorldAttribute>(inputTypes);
            var targetWorldTypes = FilterWorldTypes(targetWorldAttributeType, inputTypes);

            return targetWorldTypes.Union(allWorldTypes);
        }

        private static IEnumerable<Type> FilterTypesByAttribute<AttributeType>(this IEnumerable<Type> types) where AttributeType : Attribute
        {
            return types
                .Where(s => s.CustomAttributes.Select(a => a.AttributeType).Contains(typeof(AttributeType)));
        }

        /// <summary>
        /// 
        /// </summary>
        private static IEnumerable<Type> FilterWorldTypes(Type worldAttribute, IEnumerable<Type> types)
        {
            var isDefaultWorld = typeof(IDefaultWorldAttribute).IsAssignableFrom(worldAttribute);

            return types
                .Where(t =>
                {
                    var worldAttributes = t.CustomAttributes.Select(a => a.AttributeType).Where(a => a.IsWorldAttribute());

                    var hasNoneWorldAttribute = worldAttributes.Contains(typeof(NoneWorldAttribute));

                    if (hasNoneWorldAttribute)
                        return false;

                    if (isDefaultWorld)
                    {
                        if (worldAttributes.Count() == 0)
                            return true;
                        if (worldAttributes.Any(a => a.IsDefaultWorldAttribute()))
                            return true;

                        return false;
                    }
                    else
                    {
                        return worldAttributes.Contains(worldAttribute);
                    }
                });
        }

        public static IEnumerable<Type> FilterGenericTypesByAttribute<TAttribute>(this IEnumerable<Type> inputTypes) where TAttribute : Attribute
        {
            inputTypes = inputTypes
                .Where(p =>
                {
                    foreach (var a in Attribute.GetCustomAttributes(p))
                        if (a is TAttribute)
                            return true;

                    return false;
                });

            var resultTypes = new List<Type>(inputTypes.Count());

            foreach (var item in inputTypes)
            {
                if (item.IsGenericType)
                {
                    //var genericChildTypes = AppDomain.CurrentDomain.GetAssemblies()
                    //    .SelectMany(s => s.GetCustomAttributes(typeof(RegisterGenericComponentTypeAttribute))
                    //    .Select(a => (a as RegisterGenericComponentTypeAttribute).ConcreteType))
                    //    .Where(a => a.GetGenericTypeDefinition() == item)
                    //    .ToArray();

                    //resultTypes.AddRange(genericChildTypes);
                }
                else
                    resultTypes.Add(item);
            }

            return resultTypes;
        }

        public static bool IsUpdateInGroupAttribute(this Type attributeType)
        {
            return typeof(UpdateInGroup).IsAssignableFrom(attributeType);
        }

        public static bool IsTargetWorldAttribute(this Type attributeType)
        {
            return typeof(TargetWorldAttribute).IsAssignableFrom(attributeType);
        }

        public static bool IsWorldAttribute(this Type attributeType)
        {
            return typeof(WorldAttribute).IsAssignableFrom(attributeType);
        }

        public static bool IsDefaultWorldAttribute(this Type attributeType)
        {
            return typeof(IDefaultWorldAttribute).IsAssignableFrom(attributeType);
        }

        public static string ToGenericTypeString(this Type t)
        {
            if (!t.IsGenericType)
                return t.Name;
            string genericTypeName = t.GetGenericTypeDefinition().Name;
            genericTypeName = genericTypeName.Substring(0,
                genericTypeName.IndexOf('`'));
            string genericArgs = string.Join(",",
                t.GetGenericArguments()
                    .Select(ta => ToGenericTypeString(ta)).ToArray());
            return genericTypeName + "<" + genericArgs + ">";
        }

        public static IEnumerable<Assembly> GetAssembliesByName(this AppDomain appDomain, params string[] names)
        {
            foreach (var name in names)
            {
                yield return AppDomain.CurrentDomain.GetAssemblies().
                 SingleOrDefault(assembly => assembly.GetName().Name == name);
            }
        }
    }

    [UnityEngine.Scripting.Preserve]
    // Only for internal scanning
    internal interface IComponentMock { }
}
