#if UNITY_EDITOR
using Nanory.Lex.AssetsManagement;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nanory.Lex.UnityEditorIntegration;
using UnityEditor;
using UnityEngine;

namespace Nanory.Lex.Generation
{
    public class EcsTypesGenerator
    {
        private readonly string _generationPath;
        private const string FeatureTemplate = 
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
}";

        public EcsTypesGenerator(string generationPath)
        {
            _generationPath = Path.Combine(generationPath, "GeneratedCode/");
        }

        public void Generate()
        {
            EnsureDirectoryExists();
            var scanner = new EcsTypesScanner();

            var featureTypes = scanner.GetAssignableTypes(typeof(FeatureBase))
                .Where(type => type != typeof(FeatureBase) && type != typeof(Feature));

            foreach (var featureType in featureTypes)
            {
                var content = GenerateSystemTypes(featureType, scanner);
                var name = featureType.Namespace.SolidifyNamespace();
                WriteOnDisk(content, name);
            }
        }

        public void Clear()
        {
            var path = _generationPath.ToGlobalPath();
            var meta = path.TrimEnd('/') + ".meta";
            FileUtil.DeleteFileOrDirectory(path);
            FileUtil.DeleteFileOrDirectory(meta);
            AssetDatabase.Refresh();
        }

        private void EnsureDirectoryExists()
        {
            var path = _generationPath.ToGlobalPath();
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private void WriteOnDisk(string content, string name)
        {
            var filePath = Path.Combine(_generationPath.ToGlobalPath(), name + ".cs");
            File.WriteAllText(filePath, content);
            AssetDatabase.Refresh();
        }

        private static string GenerateSystemTypes(Type featureType, EcsTypesScanner scanner)
        {
            var worldSystemTypes = scanner.GetSystemTypesByFeature(new[] { featureType });

            var baseSystems = FormatSystemTypes("// Base Systems", worldSystemTypes);

            var allNamespaces = worldSystemTypes
                .SelectMany(GetNamespacesRecursive)
                .Where(ns => ns != null)
                .Distinct();

            var namespaceString = string.Join(Format.NewLine(), allNamespaces.Select(ns => $"using {ns};"));
            var systemTypesString = string.Join("," + Format.NewLine(2), new[] { baseSystems  }.Where(s => !string.IsNullOrEmpty(s)));
            var featureName = featureType.Namespace.SolidifyNamespace();

            return FeatureTemplate
                .Replace("{featureName}", featureName)
                .Replace("{namespaces}", namespaceString)
                .Replace("{systemTypes}", systemTypesString);
        }

        private static string FormatSystemTypes(string comment, IEnumerable<Type> types, bool isGeneric = false)
        {
            if (!types.Any()) return null;

            var formatted = types.Select(type =>
            {
                var typeName = type.IsGenericType ? type.ToGenericTypeString() : type.FullName.Replace("+", ".");
                return isGeneric ? $"typeof(OneFrameSystem<{typeName}>)" : $"typeof({typeName})";
            });

            return comment + Format.NewLine(2) + string.Join("," + Format.NewLine(2), formatted);
        }

        private static IEnumerable<string> GetNamespacesRecursive(Type type)
        {
            return type.IsGenericType
                ? type.GetGenericArguments().SelectMany(GetNamespacesRecursive)
                : new[] { type.Namespace };
        }
    }

    public static class Format
    {
        private const int TabLength = 4;

        public static string SolidifyNamespace(this string namespaceName) => namespaceName.Replace(".", "");

        public static string NewLine(int tabs = 0) => Environment.NewLine + new string(' ', TabLength * tabs);
    }
}

namespace Nanory.Lex.AssetsManagement
{
    public static class AssetManagementExtensions
    {
        public static string ToGlobalPath(this string localPath)
        {
            var basePath = Application.dataPath[..^"/Assets".Length];
            return Path.Combine(basePath, localPath);
        }
    }
}

#endif