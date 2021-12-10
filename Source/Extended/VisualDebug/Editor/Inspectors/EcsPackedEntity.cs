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
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.IntField("Id", packed.Id);
            EditorGUILayout.IntField("Gen", packed.Gen);
            EditorGUILayout.EndHorizontal();


            if (packed.Unpack(world, out var entity))
            {
                if (world.TryGet<EcsDebugGameobjectLink>(packed.Id, out var ecsDebugGameobjectLink))
                {
                    EditorGUILayout.ObjectField(ecsDebugGameobjectLink.Value, typeof(GameObject), true);
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