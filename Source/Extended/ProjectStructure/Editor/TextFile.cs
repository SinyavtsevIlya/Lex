namespace Nanory.Lex.UnityEditorIntegration.ProjectStructure
{
    using UnityEngine;

    public class TextFile : MonoBehaviour
    {
        [Multiline(15, order = -100)]
        public string Content;
    }
}

