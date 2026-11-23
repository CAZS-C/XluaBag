using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// 首次启动时把 StreamingAssets 里的基础包安装到持久化目录。
///
/// PC 上基础包可以直接用 AssetBundle.LoadFromFile 读 StreamingAssets，不需要复制；
/// Android/iOS 上 StreamingAssets 在 APK 内，AssetBundle.LoadFromFile 读不了，
/// 必须用 UnityWebRequest 拷到 persistentDataPath 后再加载。
/// </summary>
public static class BaseBundleInstaller
{
    public static IEnumerator EnsureBaseInstalled(Action<string> status)
    {
        UpdateStateStore.EnsureBaseState();

#if UNITY_ANDROID && !UNITY_EDITOR
        string targetDir = UpdatePaths.VersionDir(1);
        if (File.Exists(targetDir + "/PC"))
        {
            yield break;
        }

        if (status != null) status("正在安装基础资源...");
        UpdatePaths.EnsureDir(targetDir);

        // 先取基础包版本描述，按其中的 bundles 列表拷贝，避免硬编码文件名
        LocalVersion local = null;
        string versionJson = null;
        using (UnityWebRequest req = UnityWebRequest.Get(UpdatePaths.StreamingRoot + "/version.json"))
        {
            req.timeout = HotUpdateConfig.TimeoutSeconds;
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                versionJson = req.downloadHandler.text;
                try { local = JsonUtility.FromJson<LocalVersion>(versionJson); }
                catch (Exception e) { Debug.LogWarning("[HotUpdate] 解析基础版本失败：" + e.Message); }
            }
        }

        if (local == null || local.bundles == null || local.bundles.Count == 0)
        {
            Debug.LogError("[HotUpdate] 基础包 version.json 缺失或为空，Android 无法安装基础资源");
            yield break;
        }

        foreach (BundleInfo bundle in local.bundles)
        {
            string url = UpdatePaths.StreamingRoot + "/" + bundle.file;
            string target = targetDir + "/" + bundle.file;
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                req.timeout = HotUpdateConfig.TimeoutSeconds;
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError("[HotUpdate] 拷贝基础包失败：" + bundle.file + " " + req.error);
                    yield break;
                }
                File.WriteAllBytes(target, req.downloadHandler.data);
            }
        }

        // 版本描述也复制过去，后续差量要用
        if (!string.IsNullOrEmpty(versionJson))
        {
            File.WriteAllText(targetDir + "/version.json", versionJson);
        }
        Debug.Log("[HotUpdate] 基础资源已安装到 " + targetDir);
#endif
        yield break;
    }
}
