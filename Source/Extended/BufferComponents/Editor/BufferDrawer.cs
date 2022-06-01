namespace Nanory.Lex
{
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(Buffer<>))]
    public class BufferDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Debug.Log("Heey");
            base.OnGUI(position, property, label);
        }

    }
}