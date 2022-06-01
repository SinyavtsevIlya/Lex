#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Nanory.Lex.UnityEditorIntegration.Inspectors
{
    sealed class EcsPackedEntityInspector : IEcsComponentInspector
    {
        public Type GetFieldType()
        {
            return typeof(EcsPackedEntity);
        }

        public void OnGUI(string label, object value, EcsWorld world, int entityId)
        {
            var packed = (EcsPackedEntity)value;

            if (packed.Unpack(world, out var entity))
            {
                if (world is EcsWorldBase worldBase)
                {
                    var debugView = worldBase.GetSystem<EcsWorldDebugSystem>().GetEntityDebugView(entity);
                    EditorGUILayout.ObjectField(debugView, typeof(GameObject), true);
                }
                else
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.IntField("Id", packed.Id);
                    EditorGUILayout.IntField("Gen", packed.Gen);
                    EditorGUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.LabelField("Entity reference is invalid", new GUIStyle() { normal = new GUIStyleState() { textColor = Color.red } });
            }
        }
    }
}
#endif