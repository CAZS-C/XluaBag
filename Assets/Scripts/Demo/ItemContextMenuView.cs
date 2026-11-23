using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 物品右键菜单：使用 / 丢弃 / 取消。
///
/// 整个菜单是运行时创建的，不新增 UI 预制体，也不改现有 BagPanel 结构。
/// 根节点常驻（保证 Lua Init 时能 Find 到），真正显示时只激活
/// 全屏 blocker + 小菜单面板；点击 blocker 或 ESC 关闭。
/// </summary>
public class ItemContextMenuView : MonoBehaviour
{
    public static ItemContextMenuView Instance { get; private set; }

    public Action onUse;
    public Action onDrop;
    public Action onCancel;

    public bool IsOpen { get; private set; }

    private Canvas canvas;
    private RectTransform canvasRect;
    private GameObject blocker;
    private GameObject panel;
    private RectTransform panelRect;
    private Button useButton;
    private Text useLabel;

    private const float MenuWidth = 156f;
    private const float MenuHeight = 196f;

    private void Awake()
    {
        Instance = this;

        canvas = GetComponentInParent<Canvas>();
        if (canvas == null) canvas = GameObject.Find("Canvas") != null
            ? GameObject.Find("Canvas").GetComponent<Canvas>()
            : null;

        if (canvas == null)
        {
            Debug.LogError("[ItemContextMenuView] 找不到 Canvas");
            return;
        }
        canvasRect = canvas.GetComponent<RectTransform>();

        BuildUI();
        Hide();
    }

    private void BuildUI()
    {
        RectTransform rootRect = GetComponent<RectTransform>();
        if (rootRect == null) rootRect = gameObject.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // 全屏透明 blocker：菜单打开时点击菜单外区域关闭
        blocker = new GameObject("Blocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        blocker.transform.SetParent(transform, false);
        RectTransform blockerRect = blocker.GetComponent<RectTransform>();
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;
        Image blockerImage = blocker.GetComponent<Image>();
        blockerImage.color = new Color(0f, 0f, 0f, 0f);
        blockerImage.raycastTarget = true;
        Button blockerButton = blocker.GetComponent<Button>();
        blockerButton.targetGraphic = blockerImage;
        blockerButton.transition = Selectable.Transition.None;
        blockerButton.onClick.AddListener(Hide);

        // 菜单面板
        panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(transform, false);
        panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(0f, 0f);
        panelRect.pivot = new Vector2(0f, 0f);
        panelRect.sizeDelta = new Vector2(MenuWidth, MenuHeight);
        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.16f, 0.11f, 0.07f, 0.98f);
        panelImage.raycastTarget = true;

        useButton = CreateMenuButton(panel.transform, "Use", "使用", 12f, out useLabel);
        CreateMenuButton(panel.transform, "Drop", "丢弃", 62f, out _).onClick.AddListener(OnDropClicked);
        CreateMenuButton(panel.transform, "Cancel", "取消", 112f, out _).onClick.AddListener(OnCancelClicked);
        useButton.onClick.AddListener(OnUseClicked);
    }

    private Button CreateMenuButton(Transform parent, string name, string label, float y, out Text labelText)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(-16f, 40f);
        rect.anchoredPosition = new Vector2(0f, y);

        Image image = go.GetComponent<Image>();
        image.color = new Color(0.42f, 0.27f, 0.15f, 1f);
        image.raycastTarget = true;

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        go.AddComponent<UIButtonFeedback>();

        GameObject textGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGo.transform.SetParent(go.transform, false);
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        labelText = textGo.GetComponent<Text>();
        labelText.text = label;
        labelText.font = DemoArt.GetUIFont();
        labelText.fontSize = 18;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.raycastTarget = false;

        return button;
    }

    public void Show(Vector2 screenPosition, bool canUse, string useText)
    {
        if (canvas == null) return;

        blocker.SetActive(true);
        panel.SetActive(true);
        IsOpen = true;

        useButton.interactable = canUse;
        if (useLabel != null) useLabel.text = string.IsNullOrEmpty(useText) ? "使用" : useText;

        // 屏幕坐标 → Canvas 左下角坐标，再夹到屏幕内，避免菜单跑出画面
        Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 local;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, camera, out local))
        {
            float canvasWidth = canvasRect.rect.width;
            float canvasHeight = canvasRect.rect.height;
            float x = local.x + canvasWidth * 0.5f;
            float y = local.y + canvasHeight * 0.5f;
            x = Mathf.Clamp(x, 8f, Mathf.Max(8f, canvasWidth - MenuWidth - 8f));
            y = Mathf.Clamp(y, 8f, Mathf.Max(8f, canvasHeight - MenuHeight - 8f));
            panelRect.anchoredPosition = new Vector2(x, y);
        }
    }

    public void Hide()
    {
        if (blocker != null) blocker.SetActive(false);
        if (panel != null) panel.SetActive(false);
        IsOpen = false;
    }

    private void OnUseClicked()
    {
        Hide();
        if (onUse != null) onUse();
    }

    private void OnDropClicked()
    {
        Hide();
        if (onDrop != null) onDrop();
    }

    private void OnCancelClicked()
    {
        Hide();
        if (onCancel != null) onCancel();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
