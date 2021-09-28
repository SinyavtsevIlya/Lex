using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nanory.Lex
{
    [CreateAssetMenu(fileName = "LexEcsScanSettings", menuName = "Lex/EcsScanSettings")]
    public class EcsScanSettings : ScriptableObject
    {
        private static EcsScanSettings _default;
        public static EcsScanSettings Default
        {
            get
            {
                if (_default == null)
                    _default = Resources.Load<EcsScanSettings>("LexEcsScanSettings");

#if UNITY_EDITOR
                if (_default == null)
                {
                    _default = CreateInstance<EcsScanSettings>();

                    _default.ClientAssemblyName = "Assembly-CSharp";
                    _default.ClientNamespaceTag = "Client";

                    var resourcesPath = Application.dataPath + "Assets/Resources/";

                    if (!System.IO.Directory.Exists(resourcesPath))
                        System.IO.Directory.CreateDirectory(resourcesPath);

                    AssetDatabase.CreateFolder("Assets", "Resources");

                    AssetDatabase.CreateAsset(_default, "Assets/Resources/LexEcsScanSettings.asset");
                    AssetDatabase.SaveAssets();
                }
#endif
                return _default;
            }
        }

        public string ClientAssemblyName;
        public string ClientNamespaceTag;
    }
}
