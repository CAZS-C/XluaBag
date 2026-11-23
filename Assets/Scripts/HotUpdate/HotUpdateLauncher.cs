using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 启动场景的热更新入口：
///   - 程序化创建更新 UI（不依赖预制体，方便演示）
///   - 首次启动安装基础包（Android）
///   - 检查 / 下载 / 校验 / 回滚
///   - 完成后切换 AB 根目录并加载 SampleScene
/// </summary>
public class HotUpdateLauncher : MonoBehaviour
{
    [Header("更新完成后进入的场景")]
    [SerializeField] private string nextSceneName = "DemoScene";

    private Text statusText;
    private Text noticeText;
    private Text bannerText;
    private Text versionText;
    private Image progressFill;
    private Button retryButton;
    private bool busy;

    private void Awake()
    {
        BuildUI();
    }

    private void Start()
    {
        StartCoroutine(Boot());
    }

    private IEnumerator Boot()
    {
        if (busy)
        {
            yield break;
        }
        busy = true;
        retryButton.gameObject.SetActive(false);
        SetProgress(0f);
        SetStatus("检查基础资源...");

        // 首次启动：Android 需要把 StreamingAssets 拷到 persistentDataPath
        yield return BaseBundleInstaller.EnsureBaseInstalled(SetStatus);

        // 上次更新没有确认成功 → 回滚
        if (UpdateStateStore.NeedsRollback())
        {
            UpdateStateStore.Rollback();
            SetStatus("上次更新未完成，已回滚到上一版本");
        }

        UpdateState state = UpdateStateStore.LoadState();
        versionText.text = "App " + HotUpdateConfig.AppVersion + "  |  资源 v" + state.current;

        // 先按当前版本加载公告；如果更新成功，再重新加载一次
        ABMgr.GetInstance().BundleRoot = UpdateStateStore.GetCurrentBundleRoot();
        LoadBanner();

        HotUpdateService.Result result = null;
        yield return HotUpdateService.CheckAndUpdate(
            (message, progress) =>
            {
                SetStatus(message);
                SetProgress(progress);
            },
            r => result = r);

        if (result == null)
        {
            result = new HotUpdateService.Result
            {
                canStart = false,
                message = "更新流程异常结束"
            };
        }

        if (!string.IsNullOrEmpty(result.manifest != null ? result.manifest.notice : null))
        {
            noticeText.text = result.manifest.notice;
        }

        if (!result.canStart)
        {
            SetStatus(result.message);
            retryButton.gameObject.SetActive(true);
            busy = false;
            yield break;
        }

        // 更新成功后切到新版本根目录，并重新加载公告证明资源也热更了
        ABMgr.GetInstance().BundleRoot = UpdateStateStore.GetCurrentBundleRoot();
        LoadBanner();

        UpdateState latest = UpdateStateStore.LoadState();
        versionText.text = "App " + HotUpdateConfig.AppVersion + "  |  资源 v" + latest.current;
        SetStatus(result.message + "，正在进入游戏...");
        SetProgress(1f);

        yield return new WaitForSeconds(0.6f);

        busy = false;
        string targetScene = string.IsNullOrEmpty(nextSceneName) ? "SampleScene" : nextSceneName;
        SceneManager.LoadScene(targetScene);
    }

    private void LoadBanner()
    {
        try
        {
            TextAsset banner = ABMgr.GetInstance().LoadRes<TextAsset>(
                "banner", "Assets/ABRes/HotUpdate/Banner.txt");
            if (banner != null)
            {
                bannerText.text = banner.text;
            }
        }
        catch (Exception e)
        {
            bannerText.text = "公告资源未就绪：" + e.Message;
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
        Debug.Log("[HotUpdate] " + message);
    }

    private void SetProgress(float value)
    {
        if (progressFill != null) progressFill.fillAmount = Mathf.Clamp01(value);
    }

    private void OnRetry()
    {
        if (busy) return;
        StartCoroutine(Boot());
    }

    // ────────────────────────────────────────────────────────────────
    // 程序化 UI
    // ────────────────────────────────────────────────────────────────

    private void BuildUI()
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null)
        {
            font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Roboto" }, 18);
        }

        GameObject canvasGo = new GameObject("HotUpdateCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);

        Image panel = CreateImage(canvasGo.transform, "Panel", new Color(0.10f, 0.11f, 0.14f, 0.96f));
        RectTransform panelRect = panel.rectTransform;
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(780, 480);
        panelRect.anchoredPosition = Vector2.zero;

        CreateText(panel.transform, "Title", "XluaBag 热更新", 30, new Vector2(0, -34), new Vector2(700, 50),
            TextAnchor.MiddleCenter, font, new Color(0.95f, 0.95f, 0.95f));

        statusText = CreateText(panel.transform, "Status", "准备中...", 20, new Vector2(0, -100), new Vector2(700, 40),
            TextAnchor.MiddleCenter, font, new Color(0.75f, 0.85f, 1f));

        // 进度条背景
        Image barBg = CreateImage(panel.transform, "ProgressBg", new Color(0.2f, 0.2f, 0.24f, 1f));
        RectTransform barRect = barBg.rectTransform;
        barRect.anchorMin = new Vector2(0.5f, 0.5f);
        barRect.anchorMax = new Vector2(0.5f, 0.5f);
        barRect.sizeDelta = new Vector2(620, 18);
        barRect.anchoredPosition = new Vector2(0, -10);

        progressFill = CreateImage(barRect, "Fill", new Color(0.25f, 0.7f, 1f, 1f));
        RectTransform fillRect = progressFill.rectTransform;
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(1, 1);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        progressFill.type = Image.Type.Filled;
        progressFill.fillMethod = Image.FillMethod.Horizontal;
        progressFill.fillOrigin = 0;
        progressFill.fillAmount = 0f;

        noticeText = CreateText(panel.transform, "Notice", "更新公告会显示在这里", 18, new Vector2(0, -190),
            new Vector2(680, 110), TextAnchor.UpperLeft, font, new Color(0.78f, 0.78f, 0.82f));

        bannerText = CreateText(panel.transform, "Banner", "公告资源加载中...", 18, new Vector2(0, -315),
            new Vector2(680, 80), TextAnchor.UpperLeft, font, new Color(0.55f, 0.95f, 0.7f));

        versionText = CreateText(panel.transform, "Version", "App - | 资源 -", 16, new Vector2(0, -440),
            new Vector2(680, 30), TextAnchor.MiddleLeft, font, new Color(0.6f, 0.6f, 0.65f));

        retryButton = CreateButton(panel.transform, "Retry", "重试", font);
        RectTransform retryRect = retryButton.GetComponent<RectTransform>();
        retryRect.anchorMin = new Vector2(0.5f, 0f);
        retryRect.anchorMax = new Vector2(0.5f, 0f);
        retryRect.sizeDelta = new Vector2(180, 46);
        retryRect.anchoredPosition = new Vector2(0, 60);
        retryButton.onClick.AddListener(OnRetry);
        retryButton.gameObject.SetActive(false);
    }

    private static Text CreateText(Transform parent, string name, string content, int size,
        Vector2 position, Vector2 sizeDelta, TextAnchor anchor, Font font, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.text = content;
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = sizeDelta;
        rect.anchoredPosition = position;
        return text;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static Button CreateButton(Transform parent, string name, string label, Font font)
    {
        Image background = CreateImage(parent, name, new Color(0.22f, 0.5f, 0.85f, 1f));
        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        Text text = CreateText(background.transform, "Label", label, 20, Vector2.zero,
            new Vector2(180, 46), TextAnchor.MiddleCenter, font, Color.white);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }
}
