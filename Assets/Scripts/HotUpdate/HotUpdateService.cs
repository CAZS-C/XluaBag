using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 热更新检查 / 差量下载 / 校验 / 落盘。
///
/// 流程：
///   1. 拉远端 manifest
///   2. 比对本地版本与每个包的 md5，得到需要下载的包
///   3. 下载到 Res/{新版}.tmp，支持 Range 续传 + 重试 + MD5 校验
///   4. 校验全部通过后改名为 Res/{新版}，写 state（current/pending）
///   5. 调用方切换 AB 根目录并进入游戏；Main.lua 成功后再 confirm
/// </summary>
public static class HotUpdateService
{
    public class Result
    {
        public bool canStart;
        public bool updated;
        public string message;
        public VersionManifest manifest;
    }

    public static IEnumerator CheckAndUpdate(Action<string, float> onProgress, Action<Result> onDone)
    {
        if (onProgress != null) onProgress("检查更新...", 0f);

        // ── 1. 拉 manifest ───────────────────────────────────────────
        VersionManifest manifest = null;
        string networkError = null;
        string url = HotUpdateConfig.ManifestUrl + "?t=" + DateTime.UtcNow.Ticks;

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = HotUpdateConfig.TimeoutSeconds;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                networkError = request.error + " (HTTP " + request.responseCode + ")";
            }
            else
            {
                try
                {
                    manifest = JsonUtility.FromJson<VersionManifest>(request.downloadHandler.text);
                }
                catch (Exception e)
                {
                    networkError = "manifest 解析失败：" + e.Message;
                }
            }
        }

        if (manifest == null)
        {
            // 离线：能读到本地基础包就继续启动
            string root = UpdateStateStore.GetCurrentBundleRoot();
            bool hasBase = File.Exists(root + "/PC");
            SafeDone(onDone, new Result
            {
                canStart = hasBase,
                updated = false,
                message = hasBase ? "网络不可用，使用本地版本" : "网络不可用，且本地没有基础资源",
                manifest = null
            });
            Debug.LogWarning("[HotUpdate] 检查更新失败：" + networkError);
            yield break;
        }

        // ── 2. 客户端最低版本 ────────────────────────────────────────
        if (!string.IsNullOrEmpty(manifest.minAppVersion)
            && IsVersionOlder(HotUpdateConfig.AppVersion, manifest.minAppVersion))
        {
            SafeDone(onDone, new Result
            {
                canStart = false,
                updated = false,
                message = "需要先更新客户端到 " + manifest.minAppVersion,
                manifest = manifest
            });
            yield break;
        }

        // ── 3. 差量 ─────────────────────────────────────────────────
        LocalVersion local = UpdateStateStore.GetLocalVersion();
        if (manifest.resVersion <= local.resVersion)
        {
            SafeDone(onDone, new Result
            {
                canStart = true,
                updated = false,
                message = "已是最新版本 v" + local.resVersion,
                manifest = manifest
            });
            yield break;
        }

        List<BundleInfo> toDownload = new List<BundleInfo>();
        foreach (BundleInfo bundle in manifest.bundles)
        {
            BundleInfo localBundle = local.bundles != null
                ? local.bundles.Find(x => x.name == bundle.name)
                : null;

            if (localBundle == null
                || !string.Equals(localBundle.md5, bundle.md5, StringComparison.OrdinalIgnoreCase))
            {
                toDownload.Add(bundle);
            }
        }

        if (toDownload.Count == 0)
        {
            // 版本号变了但包内容没变：不切换目录，避免得到空版本
            SafeDone(onDone, new Result
            {
                canStart = true,
                updated = false,
                message = "资源内容未变化，继续使用当前版本",
                manifest = manifest
            });
            yield break;
        }

        // ── 4. 下载到临时目录 ───────────────────────────────────────
        string finalDir = UpdatePaths.VersionDir(manifest.resVersion);
        string tempDir = finalDir + ".tmp";
        PrepareDir(tempDir);

        long totalBytes = 0;
        for (int i = 0; i < toDownload.Count; i++) totalBytes += toDownload[i].size;

        long completedBytes = 0;
        foreach (BundleInfo bundle in toDownload)
        {
            bool ok = false;
            yield return DownloadBundle(manifest, bundle, tempDir, completedBytes, totalBytes, onProgress,
                result => ok = result);

            if (!ok)
            {
                SafeDeleteDir(tempDir);
                SafeDone(onDone, new Result
                {
                    canStart = false,
                    updated = false,
                    message = "下载失败：" + bundle.name,
                    manifest = manifest
                });
                yield break;
            }
            completedBytes += bundle.size;
        }

        // ── 5. 校验完成，写本地版本并切换目录 ───────────────────────
        try
        {
            LocalVersion localVersion = new LocalVersion
            {
                resVersion = manifest.resVersion,
                bundles = manifest.bundles
            };
            File.WriteAllText(tempDir + "/version.json", JsonUtility.ToJson(localVersion, true));

            if (Directory.Exists(finalDir)) Directory.Delete(finalDir, true);
            Directory.Move(tempDir, finalDir);

            UpdateStateStore.ApplyManifest(manifest);
        }
        catch (Exception e)
        {
            SafeDeleteDir(tempDir);
            SafeDone(onDone, new Result
            {
                canStart = false,
                updated = false,
                message = "应用更新失败：" + e.Message,
                manifest = manifest
            });
            yield break;
        }

        SafeDone(onDone, new Result
        {
            canStart = true,
            updated = true,
            message = "更新完成：v" + manifest.resVersion,
            manifest = manifest
        });
    }

    private static IEnumerator DownloadBundle(
        VersionManifest manifest,
        BundleInfo bundle,
        string tempDir,
        long completedBytes,
        long totalBytes,
        Action<string, float> onProgress,
        Action<bool> onResult)
    {
        string url = HotUpdateConfig.BaseUrl + "/" + HotUpdateConfig.PlatformFolder
                     + "/versions/" + manifest.resVersion + "/" + bundle.file;
        string finalPath = tempDir + "/" + bundle.file;
        string partPath = finalPath + ".part";

        for (int attempt = 0; attempt < HotUpdateConfig.MaxRetry; attempt++)
        {
            long existing = File.Exists(partPath) ? new FileInfo(partPath).Length : 0;
            if (existing > bundle.size) existing = 0;

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = HotUpdateConfig.TimeoutSeconds;
                request.downloadHandler = new DownloadHandlerFile(partPath, existing > 0);
                if (existing > 0)
                {
                    request.SetRequestHeader("Range", "bytes=" + existing + "-");
                }

                UnityWebRequestAsyncOperation op = request.SendWebRequest();
                while (!op.isDone)
                {
                    float progress = totalBytes > 0
                        ? Mathf.Clamp01((float)(completedBytes + existing + (long)request.downloadedBytes) / totalBytes)
                        : 0f;
                    if (onProgress != null)
                    {
                        onProgress("下载 " + bundle.name + " ...", progress);
                    }
                    yield return null;
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("[HotUpdate] " + bundle.name + " 下载失败（第 " + (attempt + 1) + " 次）："
                                     + request.error + " HTTP " + request.responseCode);
                    continue;
                }

                // 服务器忽略了 Range，返回 200 且又追加写：删掉重来
                if (existing > 0 && request.responseCode == 200)
                {
                    SafeDelete(partPath);
                    Debug.LogWarning("[HotUpdate] 服务器不支持 Range，重试全量下载：" + bundle.name);
                    continue;
                }
            }

            // 校验
            string md5 = HashUtil.Md5File(partPath);
            if (!string.Equals(md5, bundle.md5, StringComparison.OrdinalIgnoreCase))
            {
                SafeDelete(partPath);
                Debug.LogWarning("[HotUpdate] " + bundle.name + " MD5 不匹配（第 " + (attempt + 1) + " 次），重试");
                continue;
            }

            try
            {
                if (File.Exists(finalPath)) File.Delete(finalPath);
                File.Move(partPath, finalPath);
                onResult(true);
            }
            catch (Exception e)
            {
                Debug.LogError("[HotUpdate] 保存 " + bundle.name + " 失败：" + e.Message);
                onResult(false);
            }
            yield break;
        }

        onResult(false);
    }

    private static void PrepareDir(string dir)
    {
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
        UpdatePaths.EnsureDir(dir);
    }

    private static void SafeDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[HotUpdate] 删除失败：" + path + " " + e.Message);
        }
    }

    private static void SafeDeleteDir(string dir)
    {
        try
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[HotUpdate] 删除目录失败：" + dir + " " + e.Message);
        }
    }

    private static void SafeDone(Action<Result> onDone, Result result)
    {
        if (onDone != null) onDone(result);
    }

    private static bool IsVersionOlder(string local, string required)
    {
        Version a, b;
        if (Version.TryParse(local, out a) && Version.TryParse(required, out b))
        {
            return a < b;
        }
        return string.CompareOrdinal(local, required) < 0;
    }
}
