using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nanory.Lex
{
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
                var result = AppDomain.CurrentDomain.GetAssemblies().
                 SingleOrDefault(assembly => assembly.GetName().Name == name);

                if (result != default) 
                    yield return result;
            }
        }

        public static IEnumerable<T> AssertIsEmpty<T>(this IEnumerable<T> collection, string assertion)
        {
            if (collection == null) LogAssertion(assertion);
            if (collection.Count() == 0) LogAssertion(assertion);
            return collection;

            void LogAssertion(string assertion)
            {
                throw new Exception(assertion);
            }
        }

        public static IEnumerable<T> Log<T>(this IEnumerable<T> collection, string message = null)
        {
            UnityEngine.Debug.Log($"{collection}: items are:");
            var idx = 0;
            foreach (var item in collection)
            {
                UnityEngine.Debug.Log($"{++idx}) {item}");
            }
            return collection;
        }
    }
}
