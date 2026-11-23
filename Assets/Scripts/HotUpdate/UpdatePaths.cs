using System.IO;
using UnityEngine;

/// <summary>
/// 热更新相关路径的唯一来源。
/// 统一用 / 拼接，方便直接拼 URL；Windows 下也兼容。
/// </summary>
public static class UpdatePaths
{
    /// <summary>persistentDataPath/XluaBag</summary>
    public static string Root
    {
        get { return Path.Combine(Application.persistentDataPath, "XluaBag").Replace('\\', '/'); }
    }

    /// <summary>更新状态文件</summary>
    public static string StateFile
    {
        get { return Root + "/state.json"; }
    }

    /// <summary>StreamingAssets/{平台}，基础包所在目录</summary>
    public static string StreamingRoot
    {
        get { return (Application.streamingAssetsPath + "/" + HotUpdateConfig.PlatformFolder).Replace('\\', '/'); }
    }

    /// <summary>基础包的版本描述文件</summary>
    public static string BaseVersionFile
    {
        get { return StreamingRoot + "/version.json"; }
    }

    /// <summary>某个资源版本的目录：persistentDataPath/XluaBag/Res/{version}</summary>
    public static string VersionDir(int version)
    {
        return Root + "/Res/" + version;
    }

    /// <summary>确保目录存在</summary>
    public static void EnsureDir(string dir)
    {
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }
}
