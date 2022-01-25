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
                if (_default == null)
                    _default = Resources.Load<TSettings>(typeof(TSettings).Name);

#if UNITY_EDITOR
                if (_default == null)
                {
                    _default = CreateInstance<TSettings>();

                    (_default as LexSettingsBase<TSettings>).OnCreate();

                    var resourcesPath = Application.dataPath + "Assets/Resources/";

                    if (!System.IO.Directory.Exists(resourcesPath))
                        System.IO.Directory.CreateDirectory(resourcesPath);

                    AssetDatabase.CreateFolder("Assets", "Resources");

                    AssetDatabase.CreateAsset(_default, $"Assets/Resources/{typeof(TSettings).Name}.asset");
                    AssetDatabase.SaveAssets();
                }
#endif
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
}
