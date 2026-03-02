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
            var generator = new EcsSetupGenerator(_generationPath);
            generator.Generate();
        }
        
        [MenuItem("Tools/Lex/CodeGen/Clear")]
        public static void Clear()
        {
            var generator = new EcsSetupGenerator(_generationPath);
            generator.Clear();
        }
    }
}
#endif