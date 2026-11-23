using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;

/// <summary>
/// S9 性能闭环的编辑器准备工具：
///   1. 把物品图标移出 Resources，合并进 SpriteAtlas（避免图集和 Resources 重复加载）
///   2. 把 PerfHUD 挂到 SampleScene
/// </summary>
public static class PerfSetup
{
    private const string IconSourceDir = "Assets/Resources/ItemIcons";
    private const string IconTargetDir = "Assets/ArtRes/UI/ItemIcons";
    private const string AtlasPath = "Assets/Resources/ItemIconAtlas.spriteatlas";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";

    [MenuItem("Tools/Perf/1. 图标图集化", false, 1)]
    public static void CreateIconAtlas()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[PerfSetup] 请先退出 Play Mode");
            return;
        }

        // 1. 移出 Resources：Sprite 进图集后再从 Resources 单张加载会重复占内存
        if (AssetDatabase.IsValidFolder(IconSourceDir))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(IconTargetDir) ?? "Assets/ArtRes/UI");
            string error = AssetDatabase.MoveAsset(IconSourceDir, IconTargetDir);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError("[PerfSetup] 移动图标失败：" + error);
                return;
            }
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        if (!AssetDatabase.IsValidFolder(IconTargetDir))
        {
            Debug.LogError("[PerfSetup] 找不到图标目录：" + IconTargetDir);
            return;
        }

        // 2. 收集图标
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { IconTargetDir });
        List<Object> packables = new List<Object>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null)
            {
                packables.Add(texture);
            }
        }

        if (packables.Count == 0)
        {
            Debug.LogError("[PerfSetup] 图标目录下没有 Texture2D");
            return;
        }

        // 3. 重建图集
        // 注意：不要用 SpriteAtlasAsset.Save —— 它生成的主对象是编辑器包装类，
        // Resources.Load<SpriteAtlas> 会失败。直接 CreateAsset 一个运行时 SpriteAtlas。
        if (AssetDatabase.LoadMainAssetAtPath(AtlasPath) != null || File.Exists(AtlasPath))
        {
            AssetDatabase.DeleteAsset(AtlasPath);
        }

        SpriteAtlas atlas = new SpriteAtlas();
        atlas.name = Path.GetFileNameWithoutExtension(AtlasPath);
        SpriteAtlasExtensions.Add(atlas, packables.ToArray());

        Directory.CreateDirectory("Assets/Resources");
        AssetDatabase.CreateAsset(atlas, AtlasPath);
        AssetDatabase.ImportAsset(AtlasPath, ImportAssetOptions.ForceSynchronousImport);

        // 关键：CreateAsset 只是创建了图集配置，必须调用 PackAtlases 才会真正打包纹理；
        // 不打包时 atlas.spriteCount = 0，GetSprite 全部返回 null（图标会消失）。
        SpriteAtlasUtility.PackAtlases(
            new[] { atlas },
            EditorUserBuildSettings.activeBuildTarget,
            false);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        SpriteAtlas loaded = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPath);
        SpriteAtlas runtime = Resources.Load<SpriteAtlas>("ItemIconAtlas");
        Debug.Log("[PerfSetup] 图集创建完成：" + AtlasPath + "，图标 " + packables.Count +
                  " 张，编辑器加载=" + (loaded != null) + "，Resources 加载=" + (runtime != null));
    }

    [MenuItem("Tools/Perf/2. 添加 PerfHUD 到 SampleScene", false, 2)]
    public static void AddPerfHud()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[PerfSetup] 请先退出 Play Mode");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != SampleScenePath)
        {
            scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
        }

        GameObject hud = GameObject.Find("PerfHUD");
        if (hud == null)
        {
            hud = new GameObject("PerfHUD");
            hud.AddComponent<PerfHUD>();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[PerfSetup] PerfHUD 已添加到 " + SampleScenePath);
    }
}
