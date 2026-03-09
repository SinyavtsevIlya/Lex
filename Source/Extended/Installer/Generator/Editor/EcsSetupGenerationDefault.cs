#if UNITY_EDITOR
using UnityEditor;

namespace Nanory.Lex.Generation
{
    public static class EcsSetupGenerationDefault
    {
        private static readonly string _generationPath = "Assets/";

        [MenuItem("Tools/Lex/CodeGen/Generate")]

        public static void Generate()
        {
            var setupGenerator = new EcsSetupGenerator(_generationPath);
            setupGenerator.Generate();
        }
        
        [MenuItem("Tools/Lex/CodeGen/Clear")]
        public static void Clear()
        {
            var setupGenerator = new EcsSetupGenerator(_generationPath);
            setupGenerator.Clear();
        }
    }
}
#endif