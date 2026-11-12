#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Prototipo3.Editor
{
    [InitializeOnLoad]
    public static class MaterialValidation
    {
        public const string Key = "Feral.MaterialValidation";
        static MaterialValidation()
        {
            if (SessionState.GetBool(Key, false)) EditorApplication.update += StartProbe;
        }

        public static void Run()
        {
            if (!Application.isBatchMode) throw new System.InvalidOperationException("Use batchmode");
            EditorSceneManager.OpenScene("Assets/Prototipo3/Prototipo3.unity");
            SessionState.SetBool(Key, true);
            EditorApplication.update -= StartProbe;
            EditorApplication.update += StartProbe;
            EditorApplication.EnterPlaymode();
        }

        static void StartProbe()
        {
            if (!EditorApplication.isPlaying) return;
            EditorApplication.update -= StartProbe;
            SessionState.SetBool(Key, false);
            new GameObject("MaterialValidationProbe").AddComponent<MaterialValidationProbe>();
        }

        public static void Build()
        {
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Prototipo3/Prototipo3.unity" },
                locationPathName = "Logs/MaterialValidationBuild/FeralMaterials.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development,
                extraScriptingDefines = new[] { "FERAL_MATERIAL_VALIDATION" }
            });
            System.IO.File.WriteAllText("Logs/material-build-result.txt", result.summary.result + " errors=" + result.summary.totalErrors);
            EditorApplication.Exit(result.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
#endif
