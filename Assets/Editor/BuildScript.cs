using UnityEditor;
using UnityEditor.Build.Reporting;  // <-- ESSENCIAL
using System.IO;

public class BuildScript
{
    static string buildPath = "build/ios";

    [MenuItem("Build/Build iOS Project (Codemagic)")]
    public static void BuildiOS()
    {
        // Cria a pasta se não existir
        if (!Directory.Exists(buildPath))
        {
            Directory.CreateDirectory(buildPath);
        }

        // Define opções de build
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = GetEnabledScenes(),
            locationPathName = buildPath,
            target = BuildTarget.iOS,
            options = BuildOptions.None
        };

        // Executa o build
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            UnityEngine.Debug.Log("✅ Build iOS criado com sucesso!");
            UnityEngine.Debug.Log("Local: " + summary.outputPath);
        }
        else
        {
            UnityEngine.Debug.LogError("❌ Erro no build iOS: " + summary.result);
        }
    }

    private static string[] GetEnabledScenes()
    {
        var scenes = new System.Collections.Generic.List<string>();
        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
                scenes.Add(scene.path);
        }
        return scenes.ToArray();
    }
}
