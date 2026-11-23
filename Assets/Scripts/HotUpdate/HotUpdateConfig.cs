using UnityEngine;

/// <summary>
/// 热更新配置。
/// 目前用本地 HTTP 服务模拟 CDN；打包后可以把 BaseUrl 换成真实地址。
/// </summary>
public static class HotUpdateConfig
{
    /// <summary>资源清单地址（不含 /PC/... 部分）</summary>
    public const string BaseUrl = "http://127.0.0.1:8000";

    /// <summary>单个请求超时（秒）</summary>
    public const int TimeoutSeconds = 30;

    /// <summary>下载失败重试次数</summary>
    public const int MaxRetry = 3;

    /// <summary>当前平台目录名，与 ABMgr.MainABName / LuaBundleTool 保持一致</summary>
    public static string PlatformFolder
    {
        get
        {
#if UNITY_IOS
            return "IOS";
#elif UNITY_ANDROID
            return "Android";
#else
            return "PC";
#endif
        }
    }

    /// <summary>最新版本清单的完整 URL</summary>
    public static string ManifestUrl
    {
        get { return BaseUrl + "/" + PlatformFolder + "/manifest.json"; }
    }

    /// <summary>当前客户端版本（ProjectSettings 里的 Version）</summary>
    public static string AppVersion
    {
        get { return Application.version; }
    }
}
