using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 演示场景 HUD：帮助、HP、当前装备、拾取提示、Toast、重置按钮。
///
/// 全部程序化创建，不新增 UI 预制体；这样 DemoScene 可以直接复用
/// 现有 BagPanel/MainPanel，不会为了一个演示再复制一整套 UI 资源。
/// </summary>
public class DemoHUD : MonoBehaviour
{
    public static DemoHUD Instance { get; private set; }

    private Canvas canvas;

    private GameObject helpRoot;
    private Text helpText;
    private GameObject promptRoot;
    private Text promptText;
    private GameObject toastRoot;
    private Text toastText;
    private CanvasGroup toastGroup;
    private Text hpText;
    private Image hpFill;
    private RectTransform hpFillRect;
    private Text equippedText;

    private const float HpBarWidth = 300f;
    private float resetArmedUntil = -1f;
    private string lastPrompt = "";
    private bool toastRunning;
    private readonly Queue<string> toastQueue = new Queue<string>();

    private void Awake()
    {
        Instance = this;

        canvas = GameObject.Find("Canvas") != null
            ? GameObject.Find("Canvas").GetComponent<Canvas>()
            : null;

        if (canvas == null)
        {
            Debug.LogError("[DemoHUD] 找不到 Canvas，演示 HUD 无法创建");
            return;
        }

        BuildUI();
        CreateContextMenu();
        InstallButtonFeedback();
    }

    /// <summary>由 DemoBootstrap 在 Lua/数据初始化完成后调用一次</summary>
    public void Initialize()
    {
        SetEquipped(null);
        InstallButtonFeedback();
    }

    private void BuildUI()
    {
        // ── 帮助面板（左上）──
        helpRoot = CreatePanel("DemoHelp", canvas.transform,
            new Vector2(24f, -24f), new Vector2(360f, 190f),
            new Color(0f, 0f, 0f, 0.58f), TextAnchor.UpperLeft);
        RectTransform helpRect = helpRoot.GetComponent<RectTransform>();
        helpRect.anchorMin = new Vector2(0f, 1f);
        helpRect.anchorMax = new Vector2(0f, 1f);
        helpRect.pivot = new Vector2(0f, 1f);
        helpRect.anchoredPosition = new Vector2(24f, -24f);
        helpRect.sizeDelta = new Vector2(360f, 190f);

        helpText = CreateText(helpRoot.transform, "HelpText",
            "WASD 移动    F 拾取\nB 背包 / ESC 关闭\n右键格子：使用 / 丢弃\n拾取、使用、丢弃后自动存档",
            16, Color.white, TextAnchor.UpperLeft);
        RectTransform helpTextRect = helpText.rectTransform;
        helpTextRect.anchorMin = Vector2.zero;
        helpTextRect.anchorMax = Vector2.one;
        helpTextRect.offsetMin = new Vector2(14f, 48f);
        helpTextRect.offsetMax = new Vector2(-14f, -14f);

        CreateButton(helpRoot.transform, "ResetDemo", "重置演示",
            new Vector2(14f, 12f), new Vector2(128f, 30f),
            new Color(0.42f, 0.25f, 0.14f, 1f), OnResetClicked);

        // ── HP 条（帮助面板下方）──
        GameObject hpBg = CreatePanel("DemoHP", canvas.transform,
            new Vector2(24f, -224f), new Vector2(HpBarWidth, 24f),
            new Color(0.08f, 0.08f, 0.10f, 0.9f), TextAnchor.MiddleLeft);
        RectTransform hpBgRect = hpBg.GetComponent<RectTransform>();
        hpBgRect.anchorMin = new Vector2(0f, 1f);
        hpBgRect.anchorMax = new Vector2(0f, 1f);
        hpBgRect.pivot = new Vector2(0f, 1f);
        hpBgRect.anchoredPosition = new Vector2(24f, -224f);
        hpBgRect.sizeDelta = new Vector2(HpBarWidth, 24f);

        GameObject hpFillGo = CreateImageObject("Fill", hpBg.transform,
            new Color(0.25f, 0.72f, 0.36f, 1f));
        hpFillRect = hpFillGo.GetComponent<RectTransform>();
        hpFillRect.anchorMin = new Vector2(0f, 0f);
        hpFillRect.anchorMax = new Vector2(0f, 1f);
        hpFillRect.pivot = new Vector2(0f, 0.5f);
        hpFillRect.anchoredPosition = Vector2.zero;
        hpFillRect.sizeDelta = new Vector2(HpBarWidth, 0f);
        hpFill = hpFillGo.GetComponent<Image>();

        hpText = CreateText(hpBg.transform, "HPText", "HP 60/100", 15,
            Color.white, TextAnchor.MiddleCenter);
        RectTransform hpTextRect = hpText.rectTransform;
        hpTextRect.anchorMin = Vector2.zero;
        hpTextRect.anchorMax = Vector2.one;
        hpTextRect.offsetMin = Vector2.zero;
        hpTextRect.offsetMax = Vector2.zero;

        equippedText = CreateText(canvas.transform, "DemoEquipped", "武器：无", 16,
            new Color(0.95f, 0.88f, 0.65f, 1f), TextAnchor.UpperLeft);
        RectTransform equippedRect = equippedText.rectTransform;
        equippedRect.anchorMin = new Vector2(0f, 1f);
        equippedRect.anchorMax = new Vector2(0f, 1f);
        equippedRect.pivot = new Vector2(0f, 1f);
        equippedRect.anchoredPosition = new Vector2(24f, -258f);
        equippedRect.sizeDelta = new Vector2(420f, 28f);

        // ── 拾取提示（底部中间）──
        promptRoot = CreatePanel("DemoPrompt", canvas.transform,
            new Vector2(0f, 96f), new Vector2(640f, 46f),
            new Color(0f, 0f, 0f, 0.62f), TextAnchor.MiddleCenter);
        RectTransform promptRect = promptRoot.GetComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0f);
        promptRect.anchorMax = new Vector2(0.5f, 0f);
        promptRect.pivot = new Vector2(0.5f, 0f);
        promptRect.anchoredPosition = new Vector2(0f, 96f);
        promptRect.sizeDelta = new Vector2(640f, 46f);

        promptText = CreateText(promptRoot.transform, "PromptText", "", 18,
            Color.white, TextAnchor.MiddleCenter);
        RectTransform promptTextRect = promptText.rectTransform;
        promptTextRect.anchorMin = Vector2.zero;
        promptTextRect.anchorMax = Vector2.one;
        promptTextRect.offsetMin = Vector2.zero;
        promptTextRect.offsetMax = Vector2.zero;
        promptRoot.SetActive(false);

        // ── Toast（顶部中间）──
        toastRoot = CreatePanel("DemoToast", canvas.transform,
            new Vector2(0f, -24f), new Vector2(720f, 48f),
            new Color(0f, 0f, 0f, 0.72f), TextAnchor.MiddleCenter);
        RectTransform toastRect = toastRoot.GetComponent<RectTransform>();
        toastRect.anchorMin = new Vector2(0.5f, 1f);
        toastRect.anchorMax = new Vector2(0.5f, 1f);
        toastRect.pivot = new Vector2(0.5f, 1f);
        toastRect.anchoredPosition = new Vector2(0f, -24f);
        toastRect.sizeDelta = new Vector2(720f, 48f);
        toastGroup = toastRoot.AddComponent<CanvasGroup>();
        toastGroup.alpha = 0f;

        toastText = CreateText(toastRoot.transform, "ToastText", "", 18,
            Color.white, TextAnchor.MiddleCenter);
        RectTransform toastTextRect = toastText.rectTransform;
        toastTextRect.anchorMin = Vector2.zero;
        toastTextRect.anchorMax = Vector2.one;
        toastTextRect.offsetMin = Vector2.zero;
        toastTextRect.offsetMax = Vector2.zero;
        toastRoot.SetActive(false);
    }

    private void CreateContextMenu()
    {
        if (ItemContextMenuView.Instance != null) return;

        GameObject menu = new GameObject("ItemContextMenu", typeof(RectTransform));
        menu.transform.SetParent(canvas.transform, false);
        menu.AddComponent<ItemContextMenuView>();
    }

    /// <summary>给现有背包按钮、Tab、右键菜单按钮统一加按下缩放反馈</summary>
    public void InstallButtonFeedback()
    {
        if (canvas == null) return;

        Button[] buttons = canvas.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].GetComponent<UIButtonFeedback>() == null)
            {
                buttons[i].gameObject.AddComponent<UIButtonFeedback>();
            }
        }

        Toggle[] toggles = canvas.GetComponentsInChildren<Toggle>(true);
        for (int i = 0; i < toggles.Length; i++)
        {
            if (toggles[i].GetComponent<UIButtonFeedback>() == null)
            {
                toggles[i].gameObject.AddComponent<UIButtonFeedback>();
            }
        }
    }

    // ────────────────────────────────────────────────────────────────
    // 运行时更新
    // ────────────────────────────────────────────────────────────────

    public void SetHP(int current, int max)
    {
        if (hpText != null) hpText.text = "HP " + current + "/" + max;
        if (hpFillRect != null)
        {
            float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            hpFillRect.sizeDelta = new Vector2(HpBarWidth * ratio, 0f);
        }
        if (hpFill != null)
        {
            hpFill.color = current <= max * 0.3f
                ? new Color(0.85f, 0.28f, 0.22f, 1f)
                : new Color(0.25f, 0.72f, 0.36f, 1f);
        }
    }

    public void SetEquipped(string displayName)
    {
        if (equippedText != null)
        {
            equippedText.text = string.IsNullOrEmpty(displayName)
                ? "武器：无"
                : "武器：" + displayName;
        }
    }

    public void SetPrompt(string text)
    {
        if (promptText == null || promptRoot == null) return;
        if (text == lastPrompt) return;

        lastPrompt = text;
        promptText.text = text;
        promptRoot.SetActive(!string.IsNullOrEmpty(text));
    }

    public void ShowToast(string message)
    {
        if (string.IsNullOrEmpty(message)) return;
        if (toastRoot == null)
        {
            Debug.Log("[DemoHUD] " + message);
            return;
        }

        toastQueue.Enqueue(message);
        if (!toastRunning) StartCoroutine(ToastRoutine());
    }

    private IEnumerator ToastRoutine()
    {
        toastRunning = true;
        while (toastQueue.Count > 0)
        {
            toastText.text = toastQueue.Dequeue();
            toastRoot.SetActive(true);
            toastGroup.alpha = 1f;

            yield return new WaitForSecondsRealtime(1.6f);

            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.unscaledDeltaTime;
                toastGroup.alpha = 1f - Mathf.Clamp01(t / 0.25f);
                yield return null;
            }
        }

        toastRoot.SetActive(false);
        toastRunning = false;
    }

    private void OnResetClicked()
    {
        // 二次确认，避免演示时误触清档
        if (Time.unscaledTime < resetArmedUntil)
        {
            if (DemoBootstrap.Instance != null) DemoBootstrap.Instance.ResetDemo();
            return;
        }

        resetArmedUntil = Time.unscaledTime + 3f;
        ShowToast("再次点击“重置演示”确认清空存档");
    }

    // ────────────────────────────────────────────────────────────────
    // UI 创建辅助
    // ────────────────────────────────────────────────────────────────

    private static GameObject CreatePanel(string name, Transform parent, Vector2 position,
        Vector2 size, Color color, TextAnchor alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return go;
    }

    private static GameObject CreateImageObject(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return go;
    }

    private static Text CreateText(Transform parent, string name, string content, int size,
        Color color, TextAnchor alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);

        Text text = go.GetComponent<Text>();
        text.text = content;
        text.font = DemoArt.GetUIFont();
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.lineSpacing = 1.1f;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateButton(Transform parent, string name, string label,
        Vector2 position, Vector2 size, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        if (onClick != null) button.onClick.AddListener(onClick);
        go.AddComponent<UIButtonFeedback>();

        Text text = CreateText(go.transform, "Label", label, 16, Color.white, TextAnchor.MiddleCenter);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
