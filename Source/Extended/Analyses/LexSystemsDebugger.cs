#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Nanory.Lex;
using System.Collections.Generic;
using System;

public class LexSystemsDebugger : EditorWindow
{
    static LexSystemsDebugger()
    {
        EditorApplication.playModeStateChanged += (state) => 
        {
            if (HasOpenInstances<LexSystemsDebugger>()) 
            {
                GetWindow<LexSystemsDebugger>().Draw();
            }
        };
    }

    public static List<EcsSystemGroup> _rootSystemGroups = new List<EcsSystemGroup>();

    public static void AddEcsSystems(EcsSystemGroup rootSystemGroup)
    {
        _rootSystemGroups.Add(rootSystemGroup);
    }

    public static void RemoveEcsSystems(EcsSystemGroup rootSystemGroup)
    {
        _rootSystemGroups.Remove(rootSystemGroup);
    }

    [MenuItem("Window/UI Toolkit/LexDebugger")]
    public static void ShowExample()
    {
        LexSystemsDebugger wnd = GetWindow<LexSystemsDebugger>();
        wnd.titleContent = new GUIContent("LexDebugger");
    }

    private void OnEnable()
    {
        Draw();
    }

    private void OnDisable()
    {
        Draw();
    }

    private void Awake()
    {
        Draw();
    }

    private void Draw()
    {
        rootVisualElement.Clear();
        var root = rootVisualElement;

        var scroll = new ScrollView(ScrollViewMode.Vertical);
        root.Add(scroll);

        if (_rootSystemGroups.Count == 0)
        {
            var label = new Label("There is no Systems right now");
            root.Add(label);
        }
        else
        {
            foreach (var rootSystemGroup in _rootSystemGroups)
            {
                foreach (var system in rootSystemGroup.Systems)
                {
                    DrawRecursive(system, scroll);
                }
            }
        }

        void DrawRecursive(IEcsSystem system, VisualElement parent)
        {
            VisualElement element;
            var name = GetSystemName(system);
            if (system is EcsSystemGroup)
            {
                if (system is RootSystemGroup)
                {
                    element = new VisualElement();
                }
                else
                {
                    element = new Foldout();
                    var foldout = (element as Foldout);
                    foldout.text = name;
                }
            }
            else
            {
                element = new Label();
                var label = element as Label;
                label.text = name;
                label.style.color = new StyleColor(Color.white);
                label.style.marginLeft = 15;

                var thumbnail = new Image();
                thumbnail.image = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Plugins/Lex/Source/Extended/VisualDebug/Editor/Settings.png");
                thumbnail.style.width = new StyleLength(12);
                thumbnail.style.height = new StyleLength(12);
                thumbnail.style.marginLeft = -15;

                var thumbnailContainer = new VisualElement();
                thumbnailContainer.Add(thumbnail);
                thumbnailContainer.style.width = new StyleLength(15);
                thumbnailContainer.style.height = new StyleLength(15);
                element.Add(thumbnailContainer);
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
#endif