using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
#endif

namespace Nanory.Lex
{
    [CreateAssetMenu(fileName = "LexEcsProjectStructureSettings", menuName = "Lex/LexEcsProjectStructureSettings")]
    public class LexEcsProjectStructureSettings : LexSettingsBase<LexEcsProjectStructureSettings>
    {
        [SerializeField] GameObject _projectStructurePrefab;

        public GameObject ProjectStructurePrefab => _projectStructurePrefab;

        public override void OnCreate()
        {
#if UNITY_EDITOR
            var guid = UnityEditor.AssetDatabase.FindAssets("Feature t:prefab").First();
            var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            _projectStructurePrefab = asset;
#endif
        }
    }

    [CreateAssetMenu(fileName = "LexEcsScanSettings", menuName = "Lex/EcsScanSettings")]
    public class EcsScanSettings : LexSettingsBase<EcsScanSettings>
    {
        private const string FallbackAssemblyName = "Assembly-CSharp";
        private const string NanoryLex = "Nanory.Lex";

        public string[] ClientAssemblyNames => _clientAssemblyNames;

        public override void OnCreate()
        {
#if UNITY_EDITOR
            var guid = UnityEditor.AssetDatabase.FindAssets(typeof(EcsScanSettings).Assembly.GetName().Name).First();
            var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditorInternal.AssemblyDefinitionAsset>(assetPath);

            _assemblyDefinitions = new UnityEditorInternal.AssemblyDefinitionAsset[] { asset };
#endif
            _clientAssemblyNames = new string[] 
            { 
                FallbackAssemblyName,
                NanoryLex
            };
        }

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

            UnityEditor.EditorUtility.SetDirty(this);
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
