using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 一次性创建热更新启动场景，并加入 Build Settings。
/// 场景只放：Main Camera + EventSystem + HotUpdateLauncher；
/// 更新面板由 HotUpdateLauncher 运行时用代码创建，避免预制体依赖。
/// </summary>
public static class CreateLaunchScene
{
    private const string LaunchScenePath = "Assets/Scenes/LaunchScene.unity";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/HotUpdate/5. 创建启动场景", false, 5)]
    public static void Create()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[CreateLaunchScene] 请先退出 Play Mode");
            return;
        }

        Directory.CreateDirectory("Assets/Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        Camera camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f, 1f);
        camera.orthographic = true;

        GameObject eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();
        eventSystemGo.AddComponent<StandaloneInputModule>();

        GameObject launcherGo = new GameObject("HotUpdateLauncher");
        launcherGo.AddComponent<HotUpdateLauncher>();

        EditorSceneManager.SaveScene(scene, LaunchScenePath);

        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(LaunchScenePath, true)
        };
        if (File.Exists(SampleScenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(SampleScenePath, true));
        }
        EditorBuildSettings.scenes = scenes.ToArray();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CreateLaunchScene] 已创建 " + LaunchScenePath + " 并加入 Build Settings");
    }
}
