#if UNITY_EDITOR && ENABLE_IL2CPP
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Nanory.Lex.Generation
{
    public class EcsTypesGenerator
    {
        private readonly string _generationPath;
        private static string _worldTemplate =
@"using System;
using Nanory.Lex;
{namespaces}    

public static class {worldName}SystemTypesLookup
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
            _generationPath = generationPath;
        }

        public void Generate()
        {
            if (!Directory.Exists(_generationPath.ToGlobalPath()))
                Directory.CreateDirectory(_generationPath.ToGlobalPath());

            var scanner = new EcsTypesScanner();

            scanner.GetClientTypes(typeof(TargetWorldAttribute))
                .Where(type => typeof(TargetWorldAttribute).IsAssignableFrom(type))
                .Where(type => type != typeof(TargetWorldAttribute))
                .SelectMany(worldAttribute => (
                    new GeneratedFile[]
                    {
                        GetSystemTypesLookup(worldAttribute)
                    }
                ))
                .ToList()
                .ForEach(file => WriteOnDisk(file.Content, file.Name));

            GeneratedFile GetSystemTypesLookup(Type worldAttribute) =>
                new GeneratedFile()
                {
                    Name = worldAttribute.Name.Replace("Attribute", "SystemTypesLookup"),
                    Content = GenerateSystemTypes(worldAttribute, scanner)
                };
        }

        public class GeneratedFile
        {
            public string Name;
            public string Content;
        }

        public void Clear()
        {
            Directory.Delete(_generationPath.ToGlobalPath(), true);
        }

        private void WriteOnDisk(string content, string name)
        {
            using (StreamWriter streamWriter = new StreamWriter(_generationPath.ToGlobalPath() + name + ".cs"))
            {
                streamWriter.WriteLine(content);
            }
            AssetDatabase.Refresh();
        }

        private static string GenerateSystemTypes(Type worldAttributeType, EcsTypesScanner scanner)
        {
            var worldSystemTypes = scanner.GetSystemTypesByWorld(worldAttributeType);
            var oneFrameSystemTypes = scanner.GetOneFrameSystemTypesGenericArgumentsByWorld(worldAttributeType);

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
                .SelectMany(t => scanner.GetNamespacesRecursive(t))
                .Where(n => n != null).ToList()
                .ForEach(ns => namespacesHashSet.Add(ns));

            var namespacesSeq = namespacesHashSet.Count == 0 ? string.Empty : namespacesHashSet.Select(t => $"using {t};").Aggregate((a, b) => $"{a}{Format.NewLine(1)}{b}");

            var worldName = worldAttributeType.Name.Replace("WorldAttribute", "");

            var systems = new string[] { baseSystemsSeq, cleanupSystemsSeq }
            .Where(s => s != null);

            var systemsSeq = systems.Count() == 0 ? null : systems
            .Aggregate((a, b) => $"{a},{Format.NewLine(2)}{b}");

            var result = _worldTemplate
                .Replace("{worldName}", worldName)
                .Replace("{namespaces}", namespacesSeq)
                .Replace("{systemTypes}", systemsSeq);

            return result;
        }
    }

    public static class AssetManagementExtensions
    {
        public static string ToGlobalPath(this string localPath)
        {
            var global = Application.dataPath.Substring(0, Application.dataPath.Length - "/Assets".Length);
            return global + "/" + localPath;
        }
    }

    public static class Format
    {
        private const int TabLength = 4;

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
#endif