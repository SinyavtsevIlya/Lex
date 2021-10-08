#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Nanory.Lex.UnityEditorIntegration.Inspectors
{
    sealed class Vector3Inspector : IEcsComponentInspector
    {
        public Type GetFieldType()
        {
            return typeof(Vector3);
        }

        public void OnGUI(string label, object value, EcsWorld world, int entityId)
        {
            EditorGUILayout.Vector3Field(label, (Vector3) value);
        }
    }
} 
#endif