using System;
using System.Collections.Generic;

/// <summary>
/// 单个 AB 包的版本信息。
/// </summary>
[Serializable]
public class BundleInfo
{
    public string name;   // AB 名，例如 lua / banner / PC
    public string file;   // 服务器上的文件名（通常等于 name）
    public string md5;    // 内容哈希，用于差量
    public long size;     // 字节数
}

/// <summary>
/// 远端最新版本清单（manifest.json）。
/// </summary>
[Serializable]
public class VersionManifest
{
    public string appVersion;      // 发布时的客户端版本
    public int resVersion;         // 资源版本号（int 递增）
    public string minAppVersion;   // 低于这个客户端版本必须更新 APK
    public bool forceUpdate;       // 是否强制更新
    public string notice;          // 更新公告
    public List<BundleInfo> bundles = new List<BundleInfo>();
}

/// <summary>
/// 本地版本描述（StreamingAssets/PC/version.json，或更新目录里的 version.json）。
/// 只记录版本号和包哈希，用于和远端做差量。
/// </summary>
[Serializable]
public class LocalVersion
{
    public int resVersion;
    public List<BundleInfo> bundles = new List<BundleInfo>();
}

/// <summary>
/// 本地更新状态（persistentDataPath/XluaBag/state.json）。
///
/// current / previous：当前版本和上一版本目录；
/// pending / confirmed：健康检查用——启动前写 pending，Main.lua 成功后写 confirmed，
/// 下次启动发现 pending != confirmed 就自动回滚。
/// </summary>
[Serializable]
public class UpdateState
{
    public string appVersion;
    public int current;
    public int previous;
    public int pending;
    public int confirmed;
}
