using UnityEngine;

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
                    _default = CreateInstance<EcsScanSettings>();

                _default.ClientAssemblyName = "Assembly-CSharp";
                _default.FrameworkAssemblyName = "Assembly-CSharp-firstpass";
                _default.FrameworkAssemblyName = "Client";
                _default.FrameworkNamespaceTag = "Nanory";

                UnityEditor.AssetDatabase.CreateAsset(_default, "Assets/Resources/LexEcsScanSettings.asset");
                UnityEditor.AssetDatabase.SaveAssets();
#endif
                return _default;
            }
        }

        public string ClientAssemblyName;
        public string FrameworkAssemblyName;
        public string ClientNamespaceTag;
        public string FrameworkNamespaceTag;
    }
}
