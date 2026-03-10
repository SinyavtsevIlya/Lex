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

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return Path.GetFullPath(path);
        }
    }
}
#endif