#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Nanory.Lex.AssetsManagement;
using UnityEditor;
using UnityEngine;

namespace Nanory.Lex.Generation
{
    public sealed class EcsSetupGenerator
    {
        private readonly string _generationPath;

        private const string Template =
@"using System;
using System.Collections.Generic;
using Nanory.Lex;
using Nanory.Lex.Collections;
{namespaces}

public sealed class {setupName} : IEcsSetup
{
    public void SetupWorld(World world)
    {
        var root = world.CreateSystemsGroup();

{systems}

{initializers}

        var reactionsMap = new Dictionary<int, FastList<IReact>>();

{reactions}

        world.SetupReactions(reactionsMap);

{disposable}

        world.AddSystemsGroup(0, root);
    }

    [UnityEngine.RuntimeInitializeOnLoadMethod]
    static void Register()
    {
        GeneratedEcsSetupLookup.Values[typeof({collectionType})] =
            new {setupName}();
    }
}";

        public EcsSetupGenerator(string generationPath)
        {
            _generationPath = Path.Combine(generationPath, "GeneratedCode/EcsSetups/");
        }
        
        public void Generate()
        {
            EnsureDirectoryExists();

            var scanner = new EcsTypesScanner();

            var collections = GetAllFeatureCollections();

            foreach (var collectionType in collections)
            {
                var content = GenerateSetup(collectionType, scanner);
                WriteOnDisk(content, collectionType.Name + "EcsSetup");
            }

            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Lex/Generate Code")]
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
                Directory.CreateDirectory(path);
        }

        private void WriteOnDisk(string content, string name)
        {
            var filePath = Path.Combine(_generationPath.ToGlobalPath(), name + ".cs");
            File.WriteAllText(filePath, content);
        }

        // =======================================================
        // Core generation logic
        // =======================================================

        private static IEnumerable<Type> GetAllFeatureCollections()
        {
            return AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    !t.IsAbstract &&
                    !t.IsInterface &&
                    typeof(IFeatureCollection).IsAssignableFrom(t));
        }

        private static string GenerateSetup(Type collectionType, EcsTypesScanner scanner)
        {
            var collectionInstance =
                (IFeatureCollection)Activator.CreateInstance(collectionType);

            var systemTypes = scanner
                .ScanSystemTypes(collectionInstance.FeatureTypes)
                .Distinct()
                .ToList();

            var setupName = collectionType.Name + "EcsSetup";

            var systems = GenerateSystems(systemTypes);
            var initializers = GenerateInitializers(systemTypes);
            var reactions = GenerateReactions(systemTypes);
            var disposable = GenerateDisposable();
            var namespaces = CollectNamespaces(systemTypes);

            return Template
                .Replace("{setupName}", setupName)
                .Replace("{collectionType}", FormatType(collectionType))
                .Replace("{systems}", systems)
                .Replace("{initializers}", initializers)
                .Replace("{reactions}", reactions)
                .Replace("{disposable}", disposable)
                .Replace("{namespaces}", namespaces);
        }

        private static string GenerateSystems(List<Type> types)
        {
            var systemTypes = types
                .Where(t => typeof(ISystem).IsAssignableFrom(t))
                .ToList();

            return string.Join(Environment.NewLine,
                systemTypes.Select(t =>
@$"        var {GetVariableName(t)} = new {FormatType(t)}();
root.AddSystem({GetVariableName(t)});"));
        }

        private static string GenerateInitializers(List<Type> types)
        {
            var systemTypes = types
                .Where(t => typeof(ISystem).IsAssignableFrom(t))
                .ToHashSet();

            var initializerTypes = types
                .Where(t =>
                    !systemTypes.Contains(t) &&
                    typeof(IInitializer).IsAssignableFrom(t));

            return string.Join(Environment.NewLine,
                initializerTypes.Select(t =>
@$"        var {GetVariableName(t)} = new {FormatType(t)}();
root.AddInitializer({GetVariableName(t)});"));
        }

        private static string GenerateReactions(List<Type> types)
        {
            var grouped = new Dictionary<Type, List<(Type system, int order)>>();

            foreach (var type in types)
            {
                var attr = type.GetCustomAttribute<ReactionOrderAttribute>();

                var interfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                                i.GetGenericTypeDefinition() == typeof(IReact<>));

                foreach (var iface in interfaces)
                {
                    var arg = iface.GetGenericArguments()[0];

                    var order = 0;

                    if (attr != null && attr.EmissionType == arg)
                        order = attr.Order;

                    if (!grouped.TryGetValue(arg, out var list))
                    {
                        list = new List<(Type, int)>();
                        grouped[arg] = list;
                    }

                    list.Add((type, order));
                }
            }

            var lines = new List<string>();

            foreach (var pair in grouped)
            {
                var arg = pair.Key;

                var sorted = pair.Value
                    .OrderBy(x => x.order)
                    .ToList();

                lines.Add(
                    $@"        {{
            var id = IdEmit<{FormatType(arg)}>.Id;
            var list = new FastList<IReact>();");

                foreach (var (system, _) in sorted)
                {
                    lines.Add(
                        $@"            list.Add({GetVariableName(system)});");
                }

                lines.Add(
                    @"            reactionsMap[id] = list;
        }");
            }

            return string.Join(Environment.NewLine, lines);
        }
        
        private static string GenerateDisposable()
        {
            var componentTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    t.IsValueType &&
                    !t.IsAbstract &&
                    !t.ContainsGenericParameters &&
                    typeof(IComponent).IsAssignableFrom(t) &&
                    typeof(IDisposable).IsAssignableFrom(t));

            return string.Join(Environment.NewLine,
                componentTypes.Select(t =>
                    $"        world.GetStash<{FormatType(t)}>().AsDisposable();"));
        }

        private static string CollectNamespaces(IEnumerable<Type> types)
        {
            var namespaces = types
                .SelectMany(GetNamespacesRecursive)
                .Where(ns => !string.IsNullOrEmpty(ns))
                .Distinct()
                .Select(ns => $"using {ns};");

            return string.Join(Environment.NewLine, namespaces);
        }

        private static IEnumerable<string> GetNamespacesRecursive(Type type)
        {
            if (type == null)
                yield break;

            if (!string.IsNullOrEmpty(type.Namespace))
                yield return type.Namespace;

            if (!type.IsGenericType)
                yield break;

            foreach (var arg in type.GetGenericArguments())
            {
                foreach (var ns in GetNamespacesRecursive(arg))
                    yield return ns;
            }
        }

        private static string FormatType(Type type, bool includeNamespace = true)
        {
            if (!type.IsGenericType)
                return GetName(type, includeNamespace).Replace("+", ".");

            var genericDef = GetName(type.GetGenericTypeDefinition(), includeNamespace);
            genericDef = genericDef[..genericDef.IndexOf('`')];

            var args = string.Join(", ",
                type.GetGenericArguments().Select(tt => FormatType(tt, includeNamespace)));

            return genericDef.Replace("+", ".") + "<" + args + ">";

            string GetName(Type t, bool includeNamspace)
            {
                return includeNamespace ? t.FullName : t.Name;
            }
        }

        private static string GetVariableName(Type type)
        {
            var formatType = FormatType(type, false);
            var s = KeepLetters(formatType);
            return s.ToLower();
        }
        
        public static string KeepLetters(string input) =>
            string.IsNullOrEmpty(input)
                ? input
                : new string(input.Where(char.IsLetter).ToArray());
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