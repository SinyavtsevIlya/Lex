#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nanory.Lex.UnityEditorIntegration
{
    public class LexSystemsDebuggerWindow : EditorWindow
    {
        static LexSystemsDebuggerWindow()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                    TryDraw();
            };
        }

        private static void TryDraw()
        {
            if (HasOpenInstances<LexSystemsDebuggerWindow>()) 
                GetWindow<LexSystemsDebuggerWindow>(false, nameof(LexSystemsDebuggerWindow), false).Draw();
        }

        public static List<EcsSystemGroup> _rootSystemGroups = new List<EcsSystemGroup>();

        public static void AddEcsSystems(EcsSystemGroup rootSystemGroup)
        {
            _rootSystemGroups.Add(rootSystemGroup);
            TryDraw();
        }

        public static void RemoveEcsSystems(EcsSystemGroup rootSystemGroup)
        {
            _rootSystemGroups.Remove(rootSystemGroup);
            TryDraw();
        }

        [MenuItem("Window/Lex/Debugger")]
        public static void ShowWindow()
        {
            LexSystemsDebuggerWindow wnd = GetWindow<LexSystemsDebuggerWindow>();
            wnd.titleContent = new GUIContent("Lex Systems Debugger");
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
            
            var view = new LexSystemsDebuggerView(scroll);

            if (_rootSystemGroups.Count == 0)
                return;
            
            var treeItems = BuildTree(_rootSystemGroups.First(), 0);
            
            view.TreeView.columns.Add(new Column
            {
                title = "Name",
                makeCell = () =>
                {
                    var visualElement = new VisualElement();
                    var view = new SystemItemView(visualElement);
                    return visualElement;
                },
                bindCell = (element, index) =>
                {
                    var label = element.Q<Label>();
                    var system = view.TreeView.GetItemDataForIndex<IEcsSystem>(index);
                    label.text = GetSystemName(system);
                },
                resizable = true,
                stretchable = true
            });

            view.TreeView.columns.Add(new Column
            {
                title = "Type",
                makeCell = () => new Label(),
                bindCell = (element, index) =>
                {
                    var label = (Label)element;
                    var system = view.TreeView.GetItemDataForIndex<IEcsSystem>(index);
                    label.text = system.GetType().Namespace;
                },
                width = 500,
                resizable = true
            });


            view.TreeView.SetRootItems(treeItems);
            view.TreeView.Rebuild();

            string GetSystemName(IEcsSystem system)
            {
                var type = system.GetType();
                if (type.IsGenericType)
                    return type.ToGenericTypeString();

                return type.Name;
            }
        }
        
        private List<TreeViewItemData<IEcsSystem>> BuildTree(IEcsSystem system, int idStart)
        {
            var items = new List<TreeViewItemData<IEcsSystem>>();
            var idCounter = idStart;

            TreeViewItemData<IEcsSystem> BuildItem(IEcsSystem sys)
            {
                int id = idCounter++;
                var children = new List<TreeViewItemData<IEcsSystem>>();
                if (sys is EcsSystemGroup group)
                {
                    foreach (var child in group.Systems)
                    {
                        children.Add(BuildItem(child));
                    }
                }

                return new TreeViewItemData<IEcsSystem>(id, sys, children);
            }

            items.Add(BuildItem(system));
            return items;
        }
    }
}
#endif