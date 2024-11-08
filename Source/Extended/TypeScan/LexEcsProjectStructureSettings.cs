using System.Linq;
using UnityEngine;

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
}