using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Nanory.Lex
{
    [CreateAssetMenu(fileName = "LexEcsScanSettings", menuName = "Lex/EcsScanSettings")]
    public class EcsScanSettings : ScriptableObject
    {
        private const string FallbackAssemblyName = "Assembly-CSharp";

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

                    _default._clientAssemblyNames = new string[] { FallbackAssemblyName };

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

        public string[] ClientAssemblyNames => _clientAssemblyNames;

        #if UNITY_EDITOR
        private void OnValidate()
        {
            if (_assemblyDefinitions?.Length > 0)
            {
                var result = new List<string>();
                for (var idx = 0; idx < _assemblyDefinitions.Length; idx++)
                {
                    var asmDefAsset = _assemblyDefinitions[idx];

                    if (asmDefAsset != null)
                    {
                        var match = Regex.Match(asmDefAsset.text, "\"name\": \"(.+)\"");
                        result.Add(match.Groups[1].Captures[0].Value);
                    }
                }
                _clientAssemblyNames = result.ToArray();
            }

            if (_assemblyDefinitions == null || !_assemblyDefinitions.Any(ad => ad != null))
            {
                _clientAssemblyNames = new string[] { FallbackAssemblyName };
            }
        } 
        #endif
        [HideInInspector]
        [SerializeField]
        private string[] _clientAssemblyNames;
#if UNITY_EDITOR
        [Tooltip("Put here asmdefs you want to be scanned. \nIf no asmdef specified " + FallbackAssemblyName + " will be selected")]
        [SerializeField]
        private UnityEditorInternal.AssemblyDefinitionAsset[] _assemblyDefinitions;
#endif

    }
}
