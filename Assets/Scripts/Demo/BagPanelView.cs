using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包面板壳层：只负责“显示/隐藏”和入口统一。
///
/// 放在 Main Camera 这种常驻对象上，而不是 BagPanel 自己身上：
/// BagPanel 一旦 SetActive(false)，挂在它身上的组件也会停止 Update，
/// 那样 B 键就永远打不开背包了。
///
/// 开放给 Lua 的回调：
///   onShown  — 打开后让 BagPanelCtrl.Refresh(true)
///   onHidden — 关闭后清掉右键菜单和拾取提示
/// </summary>
public class BagPanelView : MonoBehaviour
{
    public static BagPanelView Instance { get; private set; }

    [SerializeField] private string panelPath = "Canvas/BagPanel";
    [SerializeField] private string closeButtonPath = "Canvas/BagPanel/btnClose";
    [SerializeField] private string bagButtonPath = "Canvas/MainPanel/btnBag";

    public Action onShown;
    public Action onHidden;

    private GameObject panel;
    private Button closeButton;
    private Button bagButton;

    public bool IsOpen
    {
        get { return panel != null && panel.activeSelf; }
    }

    private void Awake()
    {
        Instance = this;

        panel = GameObject.Find(panelPath);
        if (panel == null)
        {
            Debug.LogError("[BagPanelView] 找不到背包面板：" + panelPath);
            return;
        }

        closeButton = FindButton(closeButtonPath);
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }

        // P3 新增的背包按钮；P0 阶段可能还不存在，允许为空
        bagButton = FindButton(bagButtonPath);
        if (bagButton != null)
        {
            bagButton.onClick.AddListener(Toggle);
            // 背包打开时按钮会被面板遮住，直接隐藏，避免底部露出半截
            bagButton.gameObject.SetActive(!IsOpen);
        }
    }

    private Button FindButton(string path)
    {
        GameObject go = GameObject.Find(path);
        return go != null ? go.GetComponent<Button>() : null;
    }

    private void Update()
    {
        if (panel == null) return;

        if (Input.GetKeyDown(KeyCode.B))
        {
            Toggle();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // ESC 优先关闭右键菜单；菜单已处理时不要再把整个背包关掉
            if (ItemContextMenuView.Instance != null && ItemContextMenuView.Instance.IsOpen)
            {
                ItemContextMenuView.Instance.Hide();
                return;
            }

            if (IsOpen) Hide();
        }
    }

    public void Toggle()
    {
        if (IsOpen) Hide();
        else Show();
    }

    public void Show()
    {
        if (panel == null || panel.activeSelf) return;
        panel.SetActive(true);
        if (bagButton != null) bagButton.gameObject.SetActive(false);
        if (onShown != null) onShown();
    }

    public void Hide()
    {
        if (panel == null || !panel.activeSelf) return;
        panel.SetActive(false);
        if (bagButton != null) bagButton.gameObject.SetActive(true);
        if (onHidden != null) onHidden();
    }

    private void OnDestroy()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Hide);
        if (bagButton != null) bagButton.onClick.RemoveListener(Toggle);
        if (Instance == this) Instance = null;
    }
}
