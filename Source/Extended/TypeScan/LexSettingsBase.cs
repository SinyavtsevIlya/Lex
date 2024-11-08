using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nanory.Lex
{
    public abstract class LexSettingsBase<TSettings> : ScriptableObject where TSettings : ScriptableObject
    {
        private static TSettings _default;
        public static TSettings Default
        {
            get
            {
                var name = typeof(TSettings).Name;
                
#if UNITY_EDITOR
                if (_default != null) 
                    return _default;
                
                var settingsGuid = AssetDatabase.FindAssets($"t: {typeof(TSettings).Name}").FirstOrDefault();

                if (settingsGuid != null)
                {
                    var settingsPath = AssetDatabase.GUIDToAssetPath(settingsGuid);
                    _default = AssetDatabase.LoadAssetAtPath<TSettings>(settingsPath);
                    return _default;
                }
                    
                _default = CreateInstance<TSettings>();

                (_default as LexSettingsBase<TSettings>).OnCreate();

                var resourcesPath = Application.dataPath + "Assets/Resources/";

                if (!System.IO.Directory.Exists(resourcesPath))
                    System.IO.Directory.CreateDirectory(resourcesPath);

                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }

                AssetDatabase.CreateAsset(_default, $"Assets/Resources/{name}.asset");
                AssetDatabase.SaveAssets();
                
                return _default;
#endif
                
                if (_default == null)
                    _default = Resources.Load<TSettings>(name);

                if (_default == null)
                    throw new Exception($"{typeof(TSettings).Name} must be created in the editor");
                
                return _default;
            }
        }

        [HideInInspector]
        [SerializeField]
        private bool _isCreated;

        public abstract void OnCreate();

        public void Awake()
        {
            if (!_isCreated)
            {
                OnCreate();
                _isCreated = true;
            }
        }
    }

    // public class LexSettingsPathAttribute : System.Attribute
    // {
    //     public string Path;
    //
    //     public LexSettingsPathAttribute(string path)
    //     {
    //         Path = path;
    //     }
    // }
}
