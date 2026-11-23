using UnityEngine;

/// <summary>
/// 运行时 AB 根目录解析：
///   有更新版本 → persistentDataPath/XluaBag/Res/{current}
///   没有 → StreamingAssets/{平台}
/// ABMgr 默认使用这个结果；HotUpdateLauncher 也可以在切换版本时显式覆盖。
/// </summary>
public static class ResPathResolver
{
    public static string GetCurrentBundleRoot()
    {
        return UpdateStateStore.GetCurrentBundleRoot();
    }

    public static string PlatformFolder
    {
        get { return HotUpdateConfig.PlatformFolder; }
    }
}
