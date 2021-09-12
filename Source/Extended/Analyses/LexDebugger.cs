using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Nanory.Lex;
using System.Collections.Generic;
using System;

public class LexDebugger : EditorWindow
{
    public static List<EcsSystems> _systems = new List<EcsSystems>();
    public static System.Action SystemsAddedOrRemoved;

    public static void AddEcsSystems(EcsSystems systems)
    {
        _systems.Add(systems);
        SystemsAddedOrRemoved?.Invoke();
    }

    public static void RemoveEcsSystems(EcsSystems systems)
    {
        if (_systems.Contains(systems))
            _systems.Remove(systems);

        SystemsAddedOrRemoved?.Invoke();
    }

    [MenuItem("Window/UI Toolkit/LexDebugger")]
    public static void ShowExample()
    {
        LexDebugger wnd = GetWindow<LexDebugger>();
        wnd.titleContent = new GUIContent("LexDebugger");
    }

    private void OnEnable()
    {
        Draw();
    }

    private void Draw()
    {
        rootVisualElement.Clear();
        var root = rootVisualElement;

        var scroll = new ScrollView(ScrollViewMode.Vertical);
        root.Add(scroll);

        foreach (var systems in _systems)
        {
            foreach (var system in systems.AllSystems)
            {
                DrawRecursive(system, scroll);
            }
        }

        void DrawRecursive(IEcsSystem system, VisualElement parent)
        {
            VisualElement element;
            var name = GetSystemName(system);
            if (system is EcsSystemGroup)
            {
                element = new Foldout();
                (element as Foldout).text = name;
            }
            else
            {
                element = new Label();
                (element as Label).text = name;
            }
                
            
            parent.Add(element);

            if (system is EcsSystemGroup systemGroup)
            {
                foreach (var child in systemGroup.Systems)
                {
                    DrawRecursive(child, element);
                }
            }
        }

        string GetSystemName(IEcsSystem system)
        {
            var type = system.GetType();
            if (type.IsGenericType)
                return type.ToGenericTypeString();

            return type.Name;
        }
    }
}