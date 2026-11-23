using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Lua 资源一键打包工具 —— 把 Assets/Lua 下的脚本转换成可进 AssetBundle 的 TextAsset，
/// 批量设置 AB 归属，并按平台输出到 StreamingAssets。
///
/// 为什么需要它：
///   1. 项目里的 .lua 是 DefaultImporter（Unity 不认识成资源），不能直接打进 AB；
///      必须先复制成 .lua.txt（Unity 会按 TextAsset 导入）。
///   2. 手工在 Inspector 里逐个设置 assetBundleName 容易漏、容易忘；
///      脚本化后只需点一次菜单，且源文件删除后不会残留旧副本。
///   3. 工具与运行时共用同一套路径约定，避免"包里明明有、运行时加载不到"。
///
/// 路径约定（与 LuaMgr.MyCustomLoaderFormAB 保持一致）：
///   Assets/Lua/Bag/BagPanelCtrl.lua
///     → Assets/ABRes/Lua/Bag/BagPanelCtrl.lua.txt   （AB 名 "lua"）
///     → 运行时按 "Assets/ABRes/Lua/Bag/BagPanelCtrl.lua.txt" 精确加载
///
/// 输出目录（与 ABMgr.MainABName 的 #if 分支一致）：
///   StreamingAssets/PC | StreamingAssets/Android | StreamingAssets/IOS
/// </summary>
public static class LuaBundleTool
{
    /// <summary>Lua 源目录（工程相对路径）</summary>
    private const string LuaSourceDir = "Assets/Lua";

    /// <summary>转换产物目录（工程相对路径）</summary>
    private const string LuaOutputDir = "Assets/ABRes/Lua";

    /// <summary>Lua 使用的 AssetBundle 名（运行时 LuaMgr 按这个名字取包）</summary>
    private const string LuaBundleName = "lua";

    // ────────────────────────────────────────────────────────────────
    // 菜单入口
    // ────────────────────────────────────────────────────────────────

    [MenuItem("Tools/Lua AB/一键转换并打包", false, 1)]
    public static void ConvertAndBuild()
    {
        if (!ConvertAndMark())
        {
            return;
        }
        BuildBundles();
    }

    [MenuItem("Tools/Lua AB/仅转换并设置 AB", false, 2)]
    public static void ConvertOnly()
    {
        ConvertAndMark();
    }

    [MenuItem("Tools/Lua AB/清理生成的 Lua 资源", false, 3)]
    public static void CleanGenerated()
    {
        if (!EnsureNotPlaying())
        {
            return;
        }

        if (!DeleteGeneratedFolder())
        {
            Debug.Log("[LuaBundleTool] 没有需要清理的生成目录");
            return;
        }

        AssetDatabase.Refresh();
        Debug.Log($"[LuaBundleTool] 已清理生成目录：{LuaOutputDir}");
    }

    // ────────────────────────────────────────────────────────────────
    // 核心步骤
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 扫描 Assets/Lua 下所有 .lua，复制为 Assets/ABRes/Lua/**/*.lua.txt，
    /// 然后批量设置 assetBundleName = "lua"。
    /// 返回是否成功生成了至少一个文件。
    /// 公开给 HotUpdatePublisher 复用（发布热更版本前先转换）。
    /// </summary>
    public static bool ConvertAndMark()
    {
        if (!EnsureNotPlaying())
        {
            return false;
        }

        if (!Directory.Exists(LuaSourceDir))
        {
            Debug.LogError($"[LuaBundleTool] 源目录不存在：{LuaSourceDir}");
            return false;
        }

        // 1. 先删旧产物：避免源文件删掉后，旧的 .lua.txt 继续留在 AB 里（脏数据）
        DeleteGeneratedFolder();
        Directory.CreateDirectory(LuaOutputDir);

        // 2. 递归扫描并逐个转换
        //    注意：Directory.GetFiles 返回的是系统路径（Windows 下带反斜杠），
        //    所以先统一成 "/" 再算相对路径，避免字符串截取错位。
        string rootFull = Path.GetFullPath(LuaSourceDir).Replace('\\', '/');
        string[] sourceFiles = Directory.GetFiles(LuaSourceDir, "*.lua", SearchOption.AllDirectories);

        UTF8Encoding utf8NoBom = new UTF8Encoding(false);
        List<string> generatedPaths = new List<string>(sourceFiles.Length);
        long totalBytes = 0;

        foreach (string sourceFile in sourceFiles)
        {
            string sourceFull = Path.GetFullPath(sourceFile).Replace('\\', '/');
            // 例："Bag/BagPanelCtrl.lua"
            string relativePath = sourceFull.Substring(rootFull.Length + 1);
            // 例："Bag/BagPanelCtrl.lua.txt"
            string targetRelativePath = relativePath + ".txt";
            string targetFull = Path.Combine(LuaOutputDir, targetRelativePath).Replace('\\', '/');

            string targetDir = Path.GetDirectoryName(targetFull);
            if (!string.IsNullOrEmpty(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // 读取时兼容带 BOM 的源文件；写出一律用 UTF-8 无 BOM（Lua 解析器对 BOM 不友好）
            string content = File.ReadAllText(sourceFile, Encoding.UTF8);
            File.WriteAllText(targetFull, content, utf8NoBom);

            totalBytes += utf8NoBom.GetByteCount(content);
            generatedPaths.Add(targetFull);
        }

        if (generatedPaths.Count == 0)
        {
            Debug.LogWarning($"[LuaBundleTool] {LuaSourceDir} 下没有找到 .lua 文件");
            return false;
        }

        // 3. 让 Unity 导入新文件（同步导入，保证下一步能拿到 AssetImporter）
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        // 4. 批量设置 AB 归属
        int markedCount = 0;
        foreach (string assetPath in generatedPaths)
        {
            AssetImporter importer = AssetImporter.GetAtPath(assetPath);
            if (importer == null)
            {
                Debug.LogWarning($"[LuaBundleTool] 未找到导入器，已跳过：{assetPath}");
                continue;
            }

            importer.assetBundleName = LuaBundleName;
            importer.assetBundleVariant = "";
            importer.SaveAndReimport();
            markedCount++;
        }

        AssetDatabase.SaveAssets();

        Debug.Log($"[LuaBundleTool] 转换完成：{generatedPaths.Count} 个 .lua → .lua.txt，" +
                  $"AB 名 \"{LuaBundleName}\"，共 {totalBytes / 1024f:F1} KB（标记成功 {markedCount} 个）");
        return markedCount > 0;
    }

    /// <summary>
    /// 构建当前工程中所有设置了 AB 名的资源，输出到 StreamingAssets/{平台}/。
    /// 目前工程里只有 Lua 设置了 AB 名，因此实际产物就是 lua 包 + 平台主包。
    /// </summary>
    private static void BuildBundles()
    {
        string platformFolder = GetPlatformFolder(EditorUserBuildSettings.activeBuildTarget);
        string outputDir = Path.Combine(Application.streamingAssetsPath, platformFolder).Replace('\\', '/');
        BuildBundlesTo(outputDir);
    }

    /// <summary>
    /// 构建所有 AB 到指定目录（公开给 HotUpdatePublisher 输出到 Server/版本目录）。
    /// 返回是否成功。
    /// </summary>
    public static bool BuildBundlesTo(string outputDir)
    {
        // 防御：没有 lua 包名说明转换没成功，直接构建会得到一个空包
        string[] bundleNames = AssetDatabase.GetAllAssetBundleNames();
        if (Array.IndexOf(bundleNames, LuaBundleName) < 0)
        {
            Debug.LogError($"[LuaBundleTool] 工程中不存在 AB 名 \"{LuaBundleName}\"，已取消构建");
            return false;
        }

        BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
        Directory.CreateDirectory(outputDir);

        // ChunkBasedCompression（LZ4）：压缩率与加载速度折中，适合频繁热更的小包
        AssetBundleManifest manifest = BuildPipeline.BuildAssetBundles(
            outputDir,
            BuildAssetBundleOptions.ChunkBasedCompression,
            target);

        if (manifest == null)
        {
            Debug.LogError("[LuaBundleTool] 构建失败，请查看 Console 中的 BuildPipeline 错误");
            return false;
        }

        string[] builtBundles = manifest.GetAllAssetBundles();
        long totalBytes = 0;
        foreach (string bundleName in builtBundles)
        {
            string bundlePath = Path.Combine(outputDir, bundleName);
            long size = File.Exists(bundlePath) ? new FileInfo(bundlePath).Length : 0;
            totalBytes += size;
            Debug.Log($"[LuaBundleTool] 产物 {bundleName}: {size / 1024f:F1} KB");
        }

        if (Array.IndexOf(builtBundles, LuaBundleName) < 0)
        {
            Debug.LogError($"[LuaBundleTool] 构建结果中缺少 \"{LuaBundleName}\" 包，请检查 AB 标记");
            return false;
        }

        Debug.Log($"[LuaBundleTool] 打包完成：平台 {target} → {outputDir}，" +
                  $"共 {builtBundles.Length} 个包 / {totalBytes / 1024f:F1} KB");
        return true;
    }

    // ────────────────────────────────────────────────────────────────
    // 工具方法
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// 删除生成的 Lua 产物目录（连同 .meta）。
    /// 返回是否真的删除了内容。
    /// </summary>
    private static bool DeleteGeneratedFolder()
    {
        if (AssetDatabase.IsValidFolder(LuaOutputDir))
        {
            return AssetDatabase.DeleteAsset(LuaOutputDir);
        }

        // 目录可能只存在于磁盘（例如手动拷贝进来的），AssetDatabase 不认识时用 IO 删除兜底
        if (Directory.Exists(LuaOutputDir))
        {
            Directory.Delete(LuaOutputDir, true);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 与 ABMgr.MainABName 的 #if 分支保持完全一致：
    /// iOS → "IOS"，Android → "Android"，其余 → "PC"。
    /// 目录名同时也是 BuildPipeline 生成的主包（manifest bundle）名字。
    /// </summary>
    private static string GetPlatformFolder(BuildTarget target)
    {
        switch (target)
        {
            case BuildTarget.Android:
                return "Android";
            case BuildTarget.iOS:
                return "IOS";
            default:
                return "PC";
        }
    }

    /// <summary>
    /// Play Mode 下修改资源/打包会污染运行中的 AB 缓存，直接拦截。
    /// </summary>
    private static bool EnsureNotPlaying()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("[LuaBundleTool] 请先退出 Play Mode 再执行（运行中改资源容易读到旧包）");
            return false;
        }
        return true;
    }
}
