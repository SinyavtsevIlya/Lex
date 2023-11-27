#if UNITY_EDITOR
namespace Nanory.Lex.UnityEditorIntegration
{
    using UnityEngine;
    using UnityEditor;
    using Nanory.Lex.Conversion;
    using System.Collections.Generic;
    using UnityEditorInternal;

    public class BaseAuthoringEntityAttribute : PropertyAttribute { }

    [CustomPropertyDrawer(typeof(BaseAuthoringEntityAttribute))]
    public class BaseAuthoringEntityDrawer : PropertyDrawer
    {
        private bool _isBaseAuthoringEnityVisible;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            property.serializedObject.Update();
            var baseAuthoringEntity = property.objectReferenceValue as AuthoringEntity;

            if (_isBaseAuthoringEnityVisible == false && baseAuthoringEntity != null)
            {
                _isBaseAuthoringEnityVisible = true;
            }

            if (_isBaseAuthoringEnityVisible)
            {
                EditorGUILayout.PropertyField(property);
            }

            if (baseAuthoringEntity != null)
            {
                var results = new List<AuthoringComponent>();
                baseAuthoringEntity.MergeNonAlloc(results);
                var list = new ReorderableList(results, typeof(AuthoringComponent), false, true, false, false);
                ReorderableList.ElementCallbackDelegate onDrawElement = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    var authoringComponent = results[index];
                    EditorGUI.LabelField(rect, authoringComponent.GetType().Name.Replace("Authoring", ""));
                };
                ReorderableList.HeaderCallbackDelegate onDrawHeader = (Rect rect) =>
                {
                    EditorGUI.LabelField(rect, "Overall components");
                };
                list.drawHeaderCallback += onDrawHeader;
                list.drawElementCallback += onDrawElement;
                list.DoLayoutList();
                list.drawHeaderCallback -= onDrawHeader;
                list.drawElementCallback -= onDrawElement;
            }

            if (!_isBaseAuthoringEnityVisible)
            {
                var isAddRequested = GUILayout.Button("Add Base Authoring Entity");
                if (isAddRequested)
                {
                    _isBaseAuthoringEnityVisible = true;
                }
            }

            if (_isBaseAuthoringEnityVisible)
            {
                var isRemoveRequested = GUILayout.Button("Remove Base Authoring Entity");
                if (isRemoveRequested)
                {
                    property.objectReferenceValue = null;
                    _isBaseAuthoringEnityVisible = false;
                }
            }
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif