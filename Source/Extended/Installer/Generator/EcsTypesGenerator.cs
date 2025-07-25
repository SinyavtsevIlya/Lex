#if UNITY_EDITOR
using Nanory.Lex.AssetsManagement;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Nanory.Lex.Generation
{
    public class EcsTypesGenerator
    {
        private readonly string _generationPath;
        private static string _fatureTemplate =
@"using System;
using Nanory.Lex;
{namespaces}    

public static class {featureName}SystemTypesLookup
{
    private static Type[] _types = 
    {
        {systemTypes}
    };

    public static Type[] GetTypes() => _types;
}
";
        public EcsTypesGenerator(string generationPath)
        {
            _generationPath = Path.Combine(generationPath, "GeneratedCode/");
        }

        public void Generate()
        {
            if (!Directory.Exists(_generationPath.ToGlobalPath()))
                Directory.CreateDirectory(_generationPath.ToGlobalPath());

            var scanner = new EcsTypesScanner();

            scanner.GetAssignableTypes(typeof(FeatureBase))
                .Where(type => type != typeof(UnityEditorIntegration.Feature))
                .Where(type => typeof(FeatureBase).IsAssignableFrom(type))
                .Where(type => type != typeof(FeatureBase))
                .SelectMany(featureType => new[]
                {
                    GetSystemTypesLookup(featureType)
                })
                .ToList()
                .ForEach(file => WriteOnDisk(file.Content, file.Name));

            GeneratedFile GetSystemTypesLookup(Type featureType) =>
                new()
                {
                    Name = featureType.Namespace.SolidifyNamespace(),
                    Content = GenerateSystemTypes(featureType, scanner)
                };
        }

        private class GeneratedFile
        {
            public string Name;
            public string Content;
        }

        public void Clear()
        {
            var path = _generationPath.ToGlobalPath();
            var meta = path.Substring(0, path.Length - 1) + ".meta";
            FileUtil.DeleteFileOrDirectory(path);
            FileUtil.DeleteFileOrDirectory(meta);
            AssetDatabase.Refresh();
        }

        private void WriteOnDisk(string content, string name)
        {
            using (StreamWriter streamWriter = new StreamWriter(_generationPath.ToGlobalPath() + name + ".cs"))
            {
                streamWriter.WriteLine(content);
            }
            AssetDatabase.Refresh();
        }

        private static string GenerateSystemTypes(Type featureType, EcsTypesScanner scanner)
        {
            var worldSystemTypes = scanner.GetSystemTypesByFeature(new Type[] { featureType });
            var oneFrameSystemTypes = scanner.GetOneFrameSystemTypesGenericArgumentsByFeature(new Type[] { featureType });
            var systemTypes = worldSystemTypes.ToList();
            var eventSystemTypes = systemTypes
                .SelectMany(type =>
                {
                    var resultTypes = new List<Type>();
                    
                    foreach (var attribute in type.GetCustomAttributes())
                    {
                        if (attribute is EventSystemAttribute eventSystemAttribute)
                        {
                            var systemType = typeof(OneFrameSystem<>).MakeGenericType(eventSystemAttribute.EventComponentType);
                            resultTypes.Add(systemType);
                        }

                        if (attribute is RequestSystemAttribute requestSystemAttribute)
                        {
                            var systemType = typeof(OneFrameSystem<>).MakeGenericType(requestSystemAttribute.RequestComponentType);
                            resultTypes.Add(systemType);
                        }
                    }

                    return resultTypes;
                }).ToList();

            var baseSystemsSeq = !systemTypes.Any() ? null : $"// Base Systems{Format.NewLine(2)}" + systemTypes
                .Select(type => $"typeof({type.ToGenericTypeString()})")
                .Aggregate((a, b) => $"{a},{Format.NewLine(2)}{b}");

            var cleanupSystemsSeq = !oneFrameSystemTypes.Any() ? null : $"// OneFrame Systems{Format.NewLine(2)}" + oneFrameSystemTypes
                .Select(type =>
                {
                    var typeName = type.IsGenericType ? type.ToGenericTypeString() : type.FullName.Replace("+", ".");
                    var systemName = "OneFrameSystem";
                    return (typeName, systemName);
                })
                .Select(cleanupArgs => $"typeof({cleanupArgs.systemName}<{cleanupArgs.typeName}>)")
                .Aggregate((a, b) => $"{a},{Format.NewLine(2)}{b}");

            var eventSystemsSeq = eventSystemTypes
                .Select(type => $"typeof({type.ToGenericTypeString()})")
                .Aggregate($"// Event/Request Systems", (a, b) => $"{a},{Format.NewLine(2)}{b}");
            
            var namespacesHashSet = new HashSet<string>();
            
            systemTypes
                .Union(oneFrameSystemTypes)
                .Union(eventSystemTypes)
                .SelectMany(GetNamespacesRecursive)
                .Where(n => n != null).ToList()
                .ForEach(ns => namespacesHashSet.Add(ns));

            var namespacesSeq = namespacesHashSet.Count == 0 ? string.Empty : namespacesHashSet.Select(t => $"using {t};").Aggregate((a, b) => $"{a}{Format.NewLine(1)}{b}");

            var featureName = featureType.Namespace.SolidifyNamespace();

            var systems = new[] { baseSystemsSeq, cleanupSystemsSeq, eventSystemsSeq }
            .Where(s => s != null);

            var systemsSeq = !systems.Any() ? null : systems
            .Aggregate((a, b) => $"{a},{Format.NewLine(2)}{b}");

            var result = _fatureTemplate
                .Replace("{featureName}", featureName)
                .Replace("{namespaces}", namespacesSeq)
                .Replace("{systemTypes}", systemsSeq);

            return result;
        }

        private static IEnumerable<string> GetNamespacesRecursive(Type type)
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
    }

    public static class Format
    {
        private const int TabLength = 4;

        public static string SolidifyNamespace(this string namespaceName) => namespaceName.Replace(".", "");

        public static string NewLine(int tabs = 0)
        {
            return System.Environment.NewLine + Spaces(TabLength * tabs);
        }

        private static string Space => " ";

        private static string Spaces(int count)
        {
            var result = string.Empty;
            for (var idx = 0; idx < count; idx++)
            {
                result += Space;
            }
            return result;
        }
    }
}

namespace Nanory.Lex.AssetsManagement
{
    public static class AssetManagementExtensions
    {
        public static string ToGlobalPath(this string localPath)
        {
            var global = Application.dataPath.Substring(0, Application.dataPath.Length - "/Assets".Length);
            return global + "/" + localPath;
        }
    }
}

#endif