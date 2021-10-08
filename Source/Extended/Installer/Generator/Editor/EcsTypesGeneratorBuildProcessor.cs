#if UNITY_EDITOR
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Nanory.Lex.Generation
{
    public class EcsTypesGeneratorBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        private EcsTypesGenerator _generator;

        public void OnPreprocessBuild(BuildReport report)
        {
            _generator = new EcsTypesGenerator(generationPath: "Assets");
            _generator.Generate();
            Debug.Log("Nanory.Lex.Generator : SystemTypes code generated.");
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            _generator.Clear();
            Debug.Log("Nanory.Lex.Generator : SystemTypes code cleared");
        }
    }
} 
#endif