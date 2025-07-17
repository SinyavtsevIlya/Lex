#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nanory.Lex.UnityEditorIntegration
{
    public class LexSystemsBrowserWindow : EditorWindow
    {
        static LexSystemsBrowserWindow()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                TryDraw();
            };
        }

        private static void TryDraw()
        {
            if (HasOpenInstances<LexSystemsBrowserWindow>()) 
                GetWindow<LexSystemsBrowserWindow>(false, nameof(LexSystemsBrowserWindow), false).Draw();
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

        [MenuItem("Lex/Systems Browser")]
        public static void ShowWindow()
        {
            LexSystemsBrowserWindow wnd = GetWindow<LexSystemsBrowserWindow>();
            wnd.titleContent = new GUIContent("Lex: Systems Browser");
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
            
            var view = new LexSystemsBrowserView(root);
            
            view.EditModeStub.style.display = Application.isPlaying ? DisplayStyle.None : DisplayStyle.Flex;
            
            view.TreeView.setupDragAndDrop += _ => new StartDragArgs("rejected", DragVisualMode.Rejected);
            
            view.SearchField.RegisterValueChangedCallback(e =>
            {
                DisplayTree();
            });

            view.TreeView.columns.Add(new Column
            {
                title = "Systems",
                makeCell = () => new SystemItemView(),
                bindCell = (element, index) =>
                {
                    var itemView = (SystemItemView)element;
                    var system = view.TreeView.GetItemDataForIndex<IEcsSystem>(index);
                    var systemClass = GetSystemClassName(system);
                    
                    itemView.ClearClassList();
                    itemView.AddToClassList(systemClass);
                    itemView.Label.text = ToSpacesCase(GetSystemName(system));

                    var contextualMenuManipulator = new ContextualMenuManipulator(e =>
                    {
                        e.menu.AppendAction("Edit script", a =>
                        {
                            var systemType = system.GetType();
                            
                            var targetType = IsOneFrameSystem(system) ? systemType.GetGenericArguments().First() : systemType;
                            
                            if (!TryGetSourceAsset(targetType, out var scriptPath))
                            {
                                ShowNotification(new GUIContent($"Can't find the related script file with name {targetType.Name}. {System.Environment.NewLine} Target type is likely different from filename."));
                                return;
                            }

                            var scriptAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(scriptPath);
                            AssetDatabase.OpenAsset(scriptAsset);
                        });
                    });
                    itemView.userData = contextualMenuManipulator;
                    itemView.AddManipulator(contextualMenuManipulator);
                },
                unbindCell = (element, index) =>
                {
                    var manipulator = (ContextualMenuManipulator)element.userData;
                    manipulator.target = null;
                },
                resizable = true,
                stretchable = true,
                width = 300,
            });

            view.TreeView.columns.Add(new Column
            {
                title = "Namespace",
                makeCell = () => new Label(),
                bindCell = (element, index) =>
                {
                    var label = (Label)element;
                    var system = view.TreeView.GetItemDataForIndex<IEcsSystem>(index);
                    label.text = system.GetType().Namespace;
                },
                width = 100,
                resizable = true
            });

            DisplayTree();

            void DisplayTree()
            {
                if (_rootSystemGroups.Count == 0)
                    return;

                var treeItems = BuildTree(_rootSystemGroups.First(), 0);
                view.TreeView.SetRootItems(treeItems);
                view.TreeView.Rebuild();
                view.TreeView.ExpandAll();
            }
            
            List<TreeViewItemData<IEcsSystem>> BuildTree(IEcsSystem rootSystem, int idStart)
            {
                var items = new List<TreeViewItemData<IEcsSystem>>();
                var idCounter = idStart;
                var filter = view.SearchField.value?.ToLowerInvariant();

                bool TryBuildItem(IEcsSystem sys, out TreeViewItemData<IEcsSystem> item)
                {
                    var id = idCounter++;
                    var children = new List<TreeViewItemData<IEcsSystem>>();
                    var hasMatchingChildren = false;

                    if (sys is EcsSystemGroup group)
                    {
                        foreach (var child in group.Systems)
                        {
                            if (TryBuildItem(child, out var childItem))
                            {
                                hasMatchingChildren = true;
                                children.Add(childItem);
                            }
                        }
                    }

                    var matchesFilter = string.IsNullOrEmpty(filter) ||
                                         GetSystemName(sys).ToLowerInvariant().Contains(filter);

                    if (matchesFilter || hasMatchingChildren)
                    {
                        item = new TreeViewItemData<IEcsSystem>(id, sys, children);
                        return true;
                    }

                    item = default;
                    return false;
                }

                if (TryBuildItem(rootSystem, out var rootItem))
                {
                    items.Add(rootItem);
                }

                return items;
            }
        }
        
        private string GetSystemName(IEcsSystem system)
        {
            var type = system.GetType();
            if (type.IsGenericType)
                return type.ToGenericTypeString();

            return type.Name;
        }

        private string GetSystemClassName(IEcsSystem system)
        {
            if (system is EcsSystemGroup) 
                return "system-group";

            if (IsOneFrameSystem(system))
                return "system-one-frame";
            
            return "system-default";
        }

        private static bool IsOneFrameSystem(IEcsSystem system) =>
            system.GetType().IsGenericType &&
            system.GetType().GetGenericTypeDefinition() == typeof(OneFrameSystem<>);

        private static bool TryGetSourceAsset(System.Type type, out string result)
        {
            var typeName = type.IsGenericType ? type.GetGenericTypeDefinition().Name.Split('`')[0]: type.Name;
            var results = AssetDatabase.FindAssets(typeName);
            foreach (var guid in results)
            {
                result = AssetDatabase.GUIDToAssetPath(guid);
                return true;
            }

            result = null;
            return false;
        }
        
        private static string ToSpacesCase(string str) =>
            string.Concat(
                str.Select((x, i) =>
                    i > 0 && char.IsUpper(x) && (char.IsLower(str[i - 1]) || i < str.Length - 1 && char.IsLower(str[i + 1]))
                        ? " " + x
                        : x.ToString()));
    }
}
#endif