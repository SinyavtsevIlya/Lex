#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nanory.Lex.AssetsManagement
{
    public static class AssetManagementOps
    {
        public static string ToGlobalPath(this string localPath)
        {
            var basePath = Application.dataPath[..^"/Assets".Length];
            return Path.Combine(basePath, localPath);
        }
        
        public static string GetFilePathByName(string filename)
        {
            var guids = AssetDatabase.FindAssets($"{filename} t:Script");
            if (guids.Length == 0)
                throw new FileNotFoundException($"{filename}.cs not found in project!");

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (AssetDatabase.LoadAssetAtPath<Object>(path).name != filename)
                    continue;
                
                return Path.GetFullPath(path);
            }

            return null;
        }
    }
}
#endif