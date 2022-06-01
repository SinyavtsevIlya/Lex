#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Nanory.Lex.UnityEditorIntegration
{
    [CustomEditor(typeof(EcsEntityDebugView))]
    sealed class EcsEntityDebugViewInspector : Editor
    {
        const int MaxFieldToStringLength = 128;

        static object[] _componentsCache = new object[32];

        public override void OnInspectorGUI()
        {
            var observer = (EcsEntityDebugView)target;
            if (observer.World != null)
            {
                var guiEnabled = GUI.enabled;
                GUI.enabled = true;
                DrawComponents(observer);
                GUI.enabled = guiEnabled;
                EditorUtility.SetDirty(target);
            }
        }

        void DrawComponents(EcsEntityDebugView debugView)
        {
            if (debugView.gameObject.activeSelf)
            {
                var count = debugView.World.GetComponents(debugView.Entity, ref _componentsCache);
                for (var i = 0; i < count; i++)
                {
                    var component = _componentsCache[i];
                    _componentsCache[i] = null;
                    var type = component.GetType();
                    GUILayout.BeginVertical(GUI.skin.box);
                    var typeName = EditorExtensions.GetCleanGenericTypeName(type);
                    if (!EcsComponentInspectors.Render(typeName, type, component, debugView))
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(typeName, EditorStyles.boldLabel);
                        EditorGUILayout.EndHorizontal();
                        var indent = EditorGUI.indentLevel;
                        EditorGUI.indentLevel++;
                        foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public))
                        {
                            DrawTypeField(component, field, debugView);
                        }
                        EditorGUI.indentLevel = indent;
                    }
                    GUILayout.EndVertical();
                    EditorGUILayout.Space();
                }
            }
        }

        void DrawTypeField(object instance, FieldInfo field, EcsEntityDebugView entity)
        {
            var fieldValue = field.GetValue(instance);
            var fieldType = field.FieldType;
            if (!EcsComponentInspectors.Render(field.Name, fieldType, fieldValue, entity))
            {
                RenderFallbackType(fieldType, fieldValue, field.Name, entity);
            }
        }

        void RenderFallbackType(Type fieldType, object fieldValue, string fieldName, EcsEntityDebugView entity)
        {
            if (fieldType.IsGenericType)
            {
                if (fieldType.GetGenericTypeDefinition() == typeof(Buffer<>))
                {
                    var fieldInfo = fieldType.GetField("Values");
                    var list = fieldInfo.GetValue(fieldValue) as IEnumerable;

                    foreach (var item in list)
                    {
                        if (!EcsComponentInspectors.Render("", item.GetType(), item, entity))
                        {
                            RenderFallbackType(item.GetType(), item, "", entity);
                        }
                        //EditorGUILayout.SelectableLabel(item.ToString(), (GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight)));
                    }
                    return;
                }
            }

            if (fieldType == typeof(UnityEngine.Object) || fieldType.IsSubclassOf(typeof(UnityEngine.Object)))
            {
                GUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(fieldName, GUILayout.MaxWidth(EditorGUIUtility.labelWidth - 16));
                var guiEnabled = GUI.enabled;
                GUI.enabled = false;
                EditorGUILayout.ObjectField(fieldValue as UnityEngine.Object, fieldType, false);
                GUI.enabled = guiEnabled;
                GUILayout.EndHorizontal();
                return;
            }
            var strVal = fieldValue != null ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}", fieldValue) : "null";
            if (strVal.Length > MaxFieldToStringLength)
            {
                strVal = strVal.Substring(0, MaxFieldToStringLength);
            }
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(fieldName, GUILayout.MaxWidth(EditorGUIUtility.labelWidth - 16));
            EditorGUILayout.SelectableLabel(strVal, GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
            GUILayout.EndHorizontal();
        }
    }

    static class EcsComponentInspectors
    {
        static readonly Dictionary<Type, IEcsComponentInspector> Inspectors = new Dictionary<Type, IEcsComponentInspector>();

        static EcsComponentInspectors()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(IEcsComponentInspector).IsAssignableFrom(type) && !type.IsInterface)
                    {
                        if (Activator.CreateInstance(type) is IEcsComponentInspector inspector)
                        {
                            var componentType = inspector.GetFieldType();
                            if (Inspectors.ContainsKey(componentType))
                            {
                                Debug.LogWarningFormat("Inspector for \"{0}\" already exists, new inspector will be used instead.", componentType.Name);
                            }
                            Inspectors[componentType] = inspector;
                        }
                    }
                }
            }
        }

        public static bool Render(string label, Type type, object value, EcsEntityDebugView debugView)
        {
            if (Inspectors.TryGetValue(type, out var inspector))
            {
                inspector.OnGUI(label, value, debugView.World, debugView.Entity);
                return true;
            }
            return false;
        }
    }

    public interface IEcsComponentInspector
    {
        Type GetFieldType();
        void OnGUI(string label, object value, EcsWorld world, int entityId);
    }
}
#endif