#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nanory.Lex.UnityEditorIntegration
{
    public class LexSystemsDebugger : EditorWindow
    {
        [SerializeField]
        private Texture2D Texture;

        static LexSystemsDebugger()
        {
            EditorApplication.playModeStateChanged += (state) => TryDraw();
        }

        private static void TryDraw()
        {
            if (HasOpenInstances<LexSystemsDebugger>())
            {
                GetWindow<LexSystemsDebugger>().Draw();
            }
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
        public static void ShowExample()
        {
            LexSystemsDebugger wnd = GetWindow<LexSystemsDebugger>();
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

            if (_rootSystemGroups.Count == 0)
            {
                var label = new Label("There is no Systems right now");
                root.Add(label);
            }
            else
            {
                foreach (var rootSystemGroup in _rootSystemGroups)
                {
                    DrawRecursive(rootSystemGroup, scroll);
                }
            }

            void DrawRecursive(IEcsSystem system, VisualElement parent)
            {
                VisualElement element;
                var name = GetSystemName(system);
                if (system is EcsSystemGroup)
                {
                    element = new Foldout();
                    var foldout = (element as Foldout);
                    foldout.style.unityFontStyleAndWeight = FontStyle.Bold;
                    foldout.transform.position = new Vector3(10, 0);

                    if (system is RootSystemGroup rootSystemGroup)
                    {
                        EcsSystemBase ecsSystemBase = rootSystemGroup.Systems.FindSystem<EcsSystemBase>();
                        var primaryWorld = ecsSystemBase.World;
                        foldout.text = primaryWorld.Name;
                    }
                    else
                    {
                        foldout.text = name;
                    }
                }
                else
                {
                    element = new Label();
                    var label = element as Label;
                    label.text = name;
                    element.style.color = new StyleColor(Color.white);
                    element.style.marginLeft = 17;

                    var thumbnail = new Image();
                    var thumbnailName = system is EntityCommandBufferSystem ? "CommandBufferThumbnail" : "SystemThumbnail";
                    var systemsThumbnail = GetTexture(thumbnailName);
                    thumbnail.image = systemsThumbnail;
                    thumbnail.style.width = new StyleLength(16);
                    thumbnail.style.height = new StyleLength(16);
                    thumbnail.style.marginLeft = -17;

                    var thumbnailContainer = new VisualElement();
                    thumbnailContainer.Add(thumbnail);
                    thumbnailContainer.style.width = new StyleLength(16);
                    thumbnailContainer.style.height = new StyleLength(16);
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

        private Texture2D GetTexture(string name, string format = ".png")
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"Packages/com.nanory.lex/Source/Extended/VisualDebug/Editor/{name}{format}");
            if (texture == null)
            {
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Plugins/Lex/Source/Extended/Analyses/{name}{format}");
            }
            if (texture == null)
            {
                var filePath = $"D:/Frameworks/LexUnity/Assets/Plugins/Lex/Source/Extended/Analyses/{name}{format}";
                byte[] fileData;

                if (System.IO.File.Exists(filePath))
                {
                    fileData = System.IO.File.ReadAllBytes(filePath);
                    texture = new Texture2D(2, 2);
                    texture.LoadImage(fileData);
                }
            }
            return texture;
        }
    }
}
#endif