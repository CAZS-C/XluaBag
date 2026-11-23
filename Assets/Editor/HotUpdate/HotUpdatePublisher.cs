using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 热更新发布工具：
///   1. 生成基础包 v1（StreamingAssets + Server/PC/versions/1 + version.json）
///   2. 发布新版本（版本号 +1，构建到 Server/PC/versions/{n}，更新 manifest.json）
///   3. 清理本地缓存（persistentDataPath/XluaBag）
///
/// 发布前会调用 LuaBundleTool 把 Lua 转成 .lua.txt 并设 AB 名；
/// 同时确保 Banner.txt 被打进 banner 包，用来验证“非 Lua 资源热更”。
/// </summary>
public static class HotUpdatePublisher
{
    private const string BannerPath = "Assets/ABRes/HotUpdate/Banner.txt";
    private const string BannerBundle = "banner";
    private const string ServerRoot = "HotUpdateServer";
    private const string VersionFile = "HotUpdateServer/PC/version.txt";

    [MenuItem("Tools/HotUpdate/1. 生成基础包 v1", false, 1)]
    public static void BuildBase()
    {
        if (!EnsureNotPlaying()) return;

        EnsureBanner();
        if (!LuaBundleTool.ConvertAndMark()) return;

        string platform = GetPlatformFolder(EditorUserBuildSettings.activeBuildTarget);
        string output = "Assets/StreamingAssets/" + platform;
        if (Directory.Exists(output)) Directory.Delete(output, true);
        if (!LuaBundleTool.BuildBundlesTo(output)) return;

        List<BundleInfo> bundles = CollectBundles(output);
        WriteLocalVersion(output + "/version.json", 1, bundles);

        // 同步一份到 Server/PC/versions/1，方便对照
        CopyBundlesToServer(bundles, output, 1);

        VersionManifest manifest = new VersionManifest
        {
            appVersion = PlayerSettings.bundleVersion,
            resVersion = 1,
            minAppVersion = PlayerSettings.bundleVersion,
            forceUpdate = false,
            notice = "基础版本 v1",
            bundles = bundles
        };
        WriteManifest(manifest);
        WriteVersionNumber(1);

        AssetDatabase.Refresh();
        Debug.Log("[HotUpdatePublisher] 基础包 v1 完成：" + output);
    }

    [MenuItem("Tools/HotUpdate/2. 发布新版本", false, 2)]
    public static void PublishNewVersion()
    {
        if (!EnsureNotPlaying()) return;

        EnsureBanner();
        if (!LuaBundleTool.ConvertAndMark()) return;

        int current = ReadVersionNumber();
        int next = current + 1;

        // 关键：BuildPipeline 会用“输出目录名”命名主 manifest bundle。
        // 客户端按 MainABName = "PC" 加载主包，所以输出目录最后一级必须是 PC。
        string buildRoot = "Temp/HotUpdateBuild/" + next;
        string buildDir = buildRoot + "/PC";
        if (Directory.Exists(buildRoot)) Directory.Delete(buildRoot, true);
        if (!LuaBundleTool.BuildBundlesTo(buildDir)) return;

        List<BundleInfo> bundles = CollectBundles(buildDir);
        CopyBundlesToServer(bundles, buildDir, next);

        VersionManifest manifest = new VersionManifest
        {
            appVersion = PlayerSettings.bundleVersion,
            resVersion = next,
            minAppVersion = PlayerSettings.bundleVersion,
            forceUpdate = false,
            notice = "热更 v" + next + "：更新 Lua 逻辑与公告资源",
            bundles = bundles
        };
        WriteManifest(manifest);
        WriteVersionNumber(next);

        Debug.Log("[HotUpdatePublisher] 已发布资源版本 v" + next +
                  "，清单：" + ServerRoot + "/PC/manifest.json");
    }

    [MenuItem("Tools/HotUpdate/3. 清理本地缓存", false, 3)]
    public static void CleanLocalCache()
    {
        try
        {
            UpdateStateStore.CleanCache();
            Debug.Log("[HotUpdatePublisher] 已清理本地更新缓存");
        }
        catch (Exception e)
        {
            Debug.LogError("[HotUpdatePublisher] 清理失败：" + e.Message);
        }
    }

    [MenuItem("Tools/HotUpdate/4. 查看本地状态", false, 4)]
    public static void ShowState()
    {
        UpdateState state = UpdateStateStore.LoadState();
        Debug.Log("[HotUpdatePublisher] state：current=" + state.current +
                  " previous=" + state.previous +
                  " pending=" + state.pending +
                  " confirmed=" + state.confirmed +
                  " app=" + state.appVersion);
    }

    // ────────────────────────────────────────────────────────────────
    // 工具方法
    // ────────────────────────────────────────────────────────────────

    private static void EnsureBanner()
    {
        string dir = Path.GetDirectoryName(BannerPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (!File.Exists(BannerPath))
        {
            File.WriteAllText(BannerPath, "XluaBag 公告资源 v1");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        AssetImporter importer = AssetImporter.GetAtPath(BannerPath);
        if (importer == null)
        {
            Debug.LogError("[HotUpdatePublisher] 找不到 Banner 导入器");
            return;
        }

        importer.assetBundleName = BannerBundle;
        importer.assetBundleVariant = "";
        importer.SaveAndReimport();
    }

    private static List<BundleInfo> CollectBundles(string buildDir)
    {
        List<BundleInfo> bundles = new List<BundleInfo>();
        string[] files = Directory.GetFiles(buildDir);
        foreach (string file in files)
        {
            string name = Path.GetFileName(file);
            if (name.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase)) continue;
            if (name.Equals("version.json", StringComparison.OrdinalIgnoreCase)) continue;
            if (name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;

            bundles.Add(new BundleInfo
            {
                name = name,
                file = name,
                md5 = HashUtil.Md5File(file),
                size = new FileInfo(file).Length
            });
        }

        bundles.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return bundles;
    }

    private static void CopyBundlesToServer(List<BundleInfo> bundles, string sourceDir, int version)
    {
        string targetDir = ServerRoot + "/PC/versions/" + version;
        if (Directory.Exists(targetDir)) Directory.Delete(targetDir, true);
        Directory.CreateDirectory(targetDir);

        foreach (BundleInfo bundle in bundles)
        {
            File.Copy(sourceDir + "/" + bundle.file, targetDir + "/" + bundle.file, true);
        }
    }

    private static void WriteLocalVersion(string path, int resVersion, List<BundleInfo> bundles)
    {
        LocalVersion local = new LocalVersion
        {
            resVersion = resVersion,
            bundles = bundles
        };
        File.WriteAllText(path, JsonUtility.ToJson(local, true));
    }

    private static void WriteManifest(VersionManifest manifest)
    {
        string dir = ServerRoot + "/PC";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(dir + "/manifest.json", JsonUtility.ToJson(manifest, true));
    }

    private static int ReadVersionNumber()
    {
        if (File.Exists(VersionFile))
        {
            int value;
            if (int.TryParse(File.ReadAllText(VersionFile).Trim(), out value))
            {
                return value;
            }
        }
        return 1;
    }

    private static void WriteVersionNumber(int version)
    {
        string dir = Path.GetDirectoryName(VersionFile);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllText(VersionFile, version.ToString());
    }

    private static bool EnsureNotPlaying()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[HotUpdatePublisher] 请先退出 Play Mode");
            return false;
        }
        return true;
    }

    private static string GetPlatformFolder(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.Android: return "Android";
            case BuildTarget.iOS: return "IOS";
            default: return "PC";
        }
    }
}
