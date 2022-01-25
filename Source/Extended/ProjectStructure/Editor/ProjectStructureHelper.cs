#if UNITY_EDITOR
namespace Nanory.Lex.UnityEditorIntegration.ProjectStructure
{
    using UnityEngine;
    using UnityEditor;
    using System;
    using System.IO;
    using System.Reflection;
    using System.Collections.Generic;
    using Nanory.Lex.AssetsManagement;

    [CustomEditor(typeof(ProjectStructureHelper))]
    public class ProjectStructureHelperEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var helper = (target as ProjectStructureHelper);
            if (helper.ReplaceNamespace)
            {
                var namespaceProp = serializedObject.FindProperty("_namespace");
                namespaceProp.stringValue = GUILayout.TextField(namespaceProp.stringValue);
                serializedObject.ApplyModifiedProperties();

                if (string.IsNullOrEmpty(namespaceProp.stringValue))
                { 
                    var rect = EditorGUILayout.GetControlRect();
                    rect.height = rect.height + 5;
                    EditorGUI.HelpBox(rect, "Specify namespace to create project structure", MessageType.Info);
                    return;
                }
            }
            var clicked = GUILayout.Button("Create project structure");

            if (clicked)
            {
                helper.FinalizeProjectStructure();
            }
        }
    }

    [ExecuteInEditMode]
    public class ProjectStructureHelper : MonoBehaviour
    {
        private static string _lastEditedScene;
        private static string _selectedProjectRootFolder;

        const string FolderIcon = "d_Folder Icon";
        const string FolderEmptyIcon = "FolderEmpty Icon";
        const string TextAssetIcon = "cs Script Icon";

        public bool ReplaceNamespace = true;
        [SerializeField]
        [HideInInspector]
        private string _namespace;

        private void OnEnable()
        {
            EditorApplication.hierarchyWindowItemOnGUI = HierarchyItemCBNew;
        }
        private void OnDisable()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= HierarchyItemCBNew;
        }

        [ContextMenu("FinalizeProjectStructure")]
        public void FinalizeProjectStructure()
        {
            foreach (Transform child in GetComponentsInChildren<Transform>())
            {
                var current = _selectedProjectRootFolder + "/" + GetLocalGameObjectPath(string.Empty, child);

                if (child.TryGetComponent<TextFile>(out var textFile))
                {
                    string content;

                    if (ReplaceNamespace)
                    {
                        content = textFile.Content.Replace("{namespace}", _namespace);
                    }
                    else
                    {
                        content = textFile.Content;
                    }

                    File.WriteAllText(current.ToGlobalPath(), content);
                } else
                {
                    Directory.CreateDirectory(current);
                }
            }
            var path = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage()?.assetPath;
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(_lastEditedScene);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.Refresh();
        }

        static void HierarchyItemCBNew(int instanceID, Rect selectionRect)
        {
            var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (go == null)
                return;

            var size = 16;
            var backgroundColor = new Color32(56, 56, 56, 255);
            Texture2D _backroundTexture;
            _backroundTexture = new Texture2D(size, size);
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    _backroundTexture.SetPixel(i, j, backgroundColor);

            _backroundTexture.Apply();

            var offset = go.transform.childCount > 0 ? 30 : 18;
            var iconName = go.TryGetComponent<TextFile>(out var _) ? TextAssetIcon : go.transform.childCount == 0 ? FolderEmptyIcon : FolderIcon;
            GUI.DrawTexture(new Rect(selectionRect.xMin - 2, selectionRect.yMin, 16, 16), _backroundTexture);
            GUI.DrawTexture(new Rect(selectionRect.xMin, selectionRect.yMin, 16, 16), EditorGUIUtility.FindTexture(iconName));
        }

        public static void AssignLabel(GameObject g, Texture2D label)
        {
            Type editorGUIUtilityType = typeof(EditorGUIUtility);
            BindingFlags bindingFlags = BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.NonPublic;
            object[] args = new object[] { g, label };
            editorGUIUtilityType.InvokeMember("SetIconForObject", bindingFlags, null, null, args);
        }

        [MenuItem("Assets/Create/Lex/NewProjectStructure", true)]
        private static bool CreateProjectStructure_IsValid()
        {
            return AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject));
        }

        [MenuItem("Assets/Create/Lex/NewProjectStructure")]
        private static void CreateProjectStructure()
        {
            _selectedProjectRootFolder = AssetDatabase.GetAssetPath(Selection.activeObject);
            var path = _selectedProjectRootFolder + "/instance.prefab";
            var instance = UnityEngine.Object.Instantiate<GameObject>(LexEcsProjectStructureSettings.Default.ProjectStructurePrefab);
            PrefabUtility.SaveAsPrefabAsset(instance, path);
            UnityEngine.Object.DestroyImmediate(instance);

            _lastEditedScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            SceneView.FrameLastActiveSceneView();
        }

        private static string GetLocalGameObjectPath(string path, Transform transform)
        {
            if (transform.parent == null)
                return path;

            var separator = string.IsNullOrEmpty(path) ? "" : "/";

            path = transform.name + separator + path;
            return GetLocalGameObjectPath(path, transform.parent);
        }
    }
}
#endif
