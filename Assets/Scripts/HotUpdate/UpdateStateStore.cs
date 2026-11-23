using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 本地更新状态与版本目录管理。
///
/// 目录结构：
///   persistentDataPath/XluaBag/state.json
///   persistentDataPath/XluaBag/Res/{resVersion}/PC|lua|banner|version.json
///
/// 健康检查：
///   更新完成 → 写 current/pending；
///   Main.lua 成功 → ConfirmCurrent() 把 confirmed 对齐；
///   下次启动 pending != confirmed → Rollback() 回到 previous。
/// </summary>
public static class UpdateStateStore
{
    public static UpdateState LoadState()
    {
        try
        {
            if (File.Exists(UpdatePaths.StateFile))
            {
                string json = File.ReadAllText(UpdatePaths.StateFile);
                UpdateState state = JsonUtility.FromJson<UpdateState>(json);
                if (state != null)
                {
                    return state;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[HotUpdate] 读取 state.json 失败，按新安装处理：" + e.Message);
        }

        return new UpdateState
        {
            appVersion = HotUpdateConfig.AppVersion,
            current = 0,
            previous = 0,
            pending = 0,
            confirmed = 0
        };
    }

    public static void SaveState(UpdateState state)
    {
        UpdatePaths.EnsureDir(UpdatePaths.Root);
        state.appVersion = HotUpdateConfig.AppVersion;
        File.WriteAllText(UpdatePaths.StateFile, JsonUtility.ToJson(state, true));
    }

    /// <summary>
    /// 首次启动：把基础包视为 resVersion 1。
    /// PC 上基础包直接读 StreamingAssets，不复制；
    /// Android 的复制由 BaseBundleInstaller 完成。
    /// </summary>
    public static void EnsureBaseState()
    {
        UpdateState state = LoadState();
        if (state.current <= 0)
        {
            state.current = 1;
            state.previous = 0;
            state.pending = 0;
            state.confirmed = 0; // 等 Main.lua 成功后再确认
            SaveState(state);
        }
    }

    /// <summary>读取本地版本描述：优先当前更新目录，其次 StreamingAssets 基础包。</summary>
    public static LocalVersion GetLocalVersion()
    {
        UpdateState state = LoadState();

        if (state.current > 0)
        {
            string versionFile = UpdatePaths.VersionDir(state.current) + "/version.json";
            if (File.Exists(versionFile))
            {
                return ParseLocalVersion(versionFile);
            }
        }

        if (File.Exists(UpdatePaths.BaseVersionFile))
        {
            return ParseLocalVersion(UpdatePaths.BaseVersionFile);
        }

        // 老版本包没有 version.json：按 resVersion 1、无哈希处理，首次更新会全量下载
        return new LocalVersion { resVersion = state.current > 0 ? state.current : 1 };
    }

    /// <summary>当前应该使用的 AB 根目录（包含 PC / lua / banner 的目录）。</summary>
    public static string GetCurrentBundleRoot()
    {
        UpdateState state = LoadState();
        if (state.current > 0)
        {
            string dir = UpdatePaths.VersionDir(state.current);
            if (File.Exists(dir + "/PC"))
            {
                return dir;
            }
        }

        // PC 基础包：StreamingAssets/PC
        return UpdatePaths.StreamingRoot;
    }

    /// <summary>上次启动没有确认新版本 → 需要回滚。</summary>
    public static bool NeedsRollback()
    {
        UpdateState state = LoadState();
        return state.pending > 0
               && state.pending != state.confirmed
               && state.current == state.pending;
    }

    /// <summary>回滚到 previous，并删除启动失败的版本目录。</summary>
    public static void Rollback()
    {
        UpdateState state = LoadState();
        int badVersion = state.current;

        state.current = state.previous > 0 ? state.previous : 1;
        state.pending = 0;
        state.confirmed = state.current;
        SaveState(state);

        if (badVersion > 0 && badVersion != state.current)
        {
            string badDir = UpdatePaths.VersionDir(badVersion);
            try
            {
                if (Directory.Exists(badDir))
                {
                    Directory.Delete(badDir, true);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[HotUpdate] 删除坏版本目录失败：" + e.Message);
            }
        }

        Debug.Log("[HotUpdate] 已回滚到资源版本 " + state.current);
    }

    /// <summary>Main.lua 成功后调用：确认当前版本健康。</summary>
    public static void ConfirmCurrent()
    {
        UpdateState state = LoadState();
        if (state.current <= 0)
        {
            return;
        }

        state.pending = state.current;
        state.confirmed = state.current;
        SaveState(state);
    }

    /// <summary>更新包已下载并校验完成：切换 current / pending。</summary>
    public static void ApplyManifest(VersionManifest manifest)
    {
        UpdateState state = LoadState();
        state.previous = state.current > 0 ? state.current : 1;
        state.current = manifest.resVersion;
        state.pending = manifest.resVersion;
        // confirmed 暂不更新：Main.lua 成功后才确认
        SaveState(state);
    }

    /// <summary>把新版本的包哈希写进版本目录，供下次差量使用。</summary>
    public static void WriteLocalVersion(int resVersion, List<BundleInfo> bundles)
    {
        LocalVersion local = new LocalVersion
        {
            resVersion = resVersion,
            bundles = bundles
        };
        string dir = UpdatePaths.VersionDir(resVersion);
        UpdatePaths.EnsureDir(dir);
        File.WriteAllText(dir + "/version.json", JsonUtility.ToJson(local, true));
    }

    public static void CleanCache()
    {
        if (Directory.Exists(UpdatePaths.Root))
        {
            Directory.Delete(UpdatePaths.Root, true);
        }
    }

    private static LocalVersion ParseLocalVersion(string path)
    {
        try
        {
            return JsonUtility.FromJson<LocalVersion>(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Debug.LogWarning("[HotUpdate] 解析本地版本失败：" + e.Message);
            return new LocalVersion { resVersion = 1 };
        }
    }
}
