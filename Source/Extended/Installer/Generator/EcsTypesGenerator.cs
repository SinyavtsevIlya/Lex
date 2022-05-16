#if UNITY_EDITOR
using Nanory.Lex.AssetsManagement;
using System;
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
                .Where(type => typeof(FeatureBase).IsAssignableFrom(type))
                .Where(type => type != typeof(FeatureBase))
                .SelectMany(featureType => (
                    new GeneratedFile[]
                    {
                        GetSystemTypesLookup(featureType)
                    }
                ))
                .ToList()
                .ForEach(file => WriteOnDisk(file.Content, file.Name));

            GeneratedFile GetSystemTypesLookup(Type featureType) =>
                new GeneratedFile()
                {
                    Name = featureType.Namespace.SolidifyNamespace(),
                    Content = GenerateSystemTypes(featureType, scanner)
                };
        }

        public class GeneratedFile
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

            var baseSystemsSeq = worldSystemTypes.Count() == 0 ? null : $"// Base Systems{Format.NewLine(2)}" + worldSystemTypes
                .Select(type => $"typeof({type.Name})")
                .Aggregate((a, b) => $"{a},{Format.NewLine(2)}{b}");

            var cleanupSystemsSeq = oneFrameSystemTypes.Count() == 0 ? null : $"// OneFrame Systems{Format.NewLine(2)}" + oneFrameSystemTypes
                .Select(type =>
                {
                    var typeName = type.IsGenericType ? type.ToGenericTypeString() : type.FullName.Replace("+", ".");
                    var systemName = "OneFrameSystem";
                    return (typeName, systemName);
                })
                .Select(cleanupArgs => $"typeof({cleanupArgs.systemName}<{cleanupArgs.typeName}>)")
                .Aggregate((a, b) => $"{a},{Format.NewLine(2)}{b}");

            var namespacesHashSet = new HashSet<string>();

            worldSystemTypes
                .Union(oneFrameSystemTypes)
                .SelectMany(t => GetNamespacesRecursive(t))
                .Where(n => n != null).ToList()
                .ForEach(ns => namespacesHashSet.Add(ns));

            var namespacesSeq = namespacesHashSet.Count == 0 ? string.Empty : namespacesHashSet.Select(t => $"using {t};").Aggregate((a, b) => $"{a}{Format.NewLine(1)}{b}");

            var featureName = featureType.Namespace.SolidifyNamespace();

            var systems = new string[] { baseSystemsSeq, cleanupSystemsSeq }
            .Where(s => s != null);

            var systemsSeq = systems.Count() == 0 ? null : systems
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

        public static string Space
        {
            get
            {
                return " ";
            }
        }

        public static string Spaces(int count)
        {
            string result = string.Empty;
            for (int i = 0; i < count; i++)
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