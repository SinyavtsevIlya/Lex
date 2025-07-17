#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Nanory.Lex.UnityEditorIntegration
{
    public static class LexSystemsDebuggerConstants
    {
        public const string AssetsRootPath = "Packages/com.nanory.lex/Source/Extended/Analyses/";
    }
    
    public class LexSystemsBrowserView : VisualElement
    {
        public ToolbarSearchField SearchField;
        public MultiColumnTreeView TreeView;
        public VisualElement EditModeStub;

        public LexSystemsBrowserView(VisualElement parent)
        {
            parent.Add(this);
            
            this.style.flexGrow = 1f;

            var assetPath = LexSystemsDebuggerConstants.AssetsRootPath + "LexSystemsDebuggerView.uxml";
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
            
            asset.CloneTree(this);
            
            SearchField = this.Q<ToolbarSearchField>();
            TreeView = this.Q<MultiColumnTreeView>();
            EditModeStub = this.Q("EditModeStub");

            TreeView.reorderable = false;
        }
    }

    public class SystemItemView : VisualElement
    {
        public Label Label;
        public VisualElement Thumbnail;

        public SystemItemView()
        {
            this.style.flexGrow = 1f;
            
            var assetPath = LexSystemsDebuggerConstants.AssetsRootPath + "SystemsItemView.uxml";
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(assetPath);
            
            asset.CloneTree(this);

            Label = this.Q<Label>();
            Thumbnail = this.Q("Thumbnail");
        }
    }
}
#endif