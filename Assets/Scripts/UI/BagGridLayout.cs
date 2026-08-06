using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 虚拟化网格布局 — 挂在 ScrollRect 的 Content 节点上。
/// 
/// 核心职责：
///   1. 根据 itemCount 计算 Content 总高度（替代 GridLayoutGroup 自动布局）。
///   2. 监听 ScrollRect 滚动事件，实时计算当前可视的行范围。
///   3. 驱动 BagItemPool 回收不可见格子、取出可见格子，并调用 onCellBind 让 Lua 绑定数据。
/// 
/// 为什么不用 GridLayoutGroup：
///   500 个格子 → 500 个 GameObject → Canvas 重建 O(n)。
///   虚拟化后只维护可视行 + 2 行缓冲（约 20 个格子）→ Canvas 重建 O(1)。
/// 
/// Lua 调用入口：BagGridLayout.Refresh(count)
/// Lua 绑定回调：BagGridLayout.onCellBind = function(index, cellGo) ... end
/// </summary>
public class BagGridLayout : MonoBehaviour
{
    [Header("网格参数")]
    public int colCount = 4;
    public Vector2 cellSize = new Vector2(176, 176);
    public Vector2 spacing = new Vector2(8, 8);
    public RectOffset padding;

    [Header("池子引用")]
    public BagItemPool pool;

    private ScrollRect scrollRect;
    private RectTransform content;
    private RectTransform viewport;

    private int itemCount;
    private int totalRows;
    private float cellStepY;

    // 缓存上一次可视索引范围，只有范围变化时才重建格子（避免滚动惯性期间每帧全量重建）
    private int lastStartIndex = -1;
    private int lastEndIndex = -1;

    // Lua 绑定回调：onCellBind(index, cellGo)，每帧可见范围变化时触发
    public System.Action<int, GameObject> onCellBind;

    private void Awake()
    {
        content = GetComponent<RectTransform>();
        scrollRect = GetComponentInParent<ScrollRect>();
        viewport = scrollRect.viewport;
        cellStepY = cellSize.y + spacing.y;
        scrollRect.onValueChanged.AddListener(OnScroll);

        // pool 兜底：未在 Inspector 指定时自动查找同节点上的 BagItemPool
        if (pool == null)
            pool = GetComponent<BagItemPool>();

        // RectOffset 不能在字段初始化器中 new，放在 Awake 中初始化
        if (padding == null)
            padding = new RectOffset(12, 12, 12, 12);
    }

    /// <summary>
    /// Lua 调用入口：传入当前标签页的物品总数，重算 Content 高度并刷新可见格子。
    /// 每次切换 Tab 或数据变化时调用。
    /// </summary>
    public void Refresh(int count)
    {
        itemCount = count;
        totalRows = Mathf.Max(1, Mathf.CeilToInt((float)itemCount / colCount));

        // 重置可视范围缓存，强制重建格子
        lastStartIndex = -1;
        lastEndIndex = -1;

        // 总高度 = 行数 * (格子高 + 间距) + 上下边距
        float contentHeight = totalRows * cellStepY + padding.top + padding.bottom;
        content.sizeDelta = new Vector2(content.sizeDelta.x, contentHeight);

        // 池子大小 = 列数 * (可视行 + 2 行缓冲)
        int visibleRows = Mathf.CeilToInt(viewport.rect.height / cellStepY);
        int poolRows = visibleRows + 2;
        pool.Init(colCount * poolRows);

        OnScroll(scrollRect.normalizedPosition);
    }

    /// <summary>
    /// ScrollRect 滚动事件回调（每帧触发）。
    /// 计算当前可视行范围，回收不可见格子，取出可见格子并定位 + 回调 Lua 绑定数据。
    /// </summary>
    private void OnScroll(Vector2 normalizedPos)
    {
        if (itemCount <= 0) return;

        // Content 锚点左上(0,1)，向下滚时 anchoredPosition.y 为负
        float offsetY = content.anchoredPosition.y;
        float absOffset = Mathf.Abs(offsetY);

        int firstRow = Mathf.FloorToInt(absOffset / cellStepY);
        int lastRow = firstRow + Mathf.CeilToInt((viewport.rect.height + cellSize.y) / cellStepY);

        // 上下各缓冲 1 行，防止滚动时闪白
        firstRow = Mathf.Max(0, firstRow - 1);
        lastRow = Mathf.Min(totalRows - 1, lastRow + 1);

        int startIndex = firstRow * colCount;
        int endIndex = Mathf.Min(lastRow * colCount + colCount - 1, itemCount - 1);

        // 可视索引范围未变化时直接跳过重建（此时格子保持原状，不动 Recycle/Get）
        if (startIndex == lastStartIndex && endIndex == lastEndIndex)
        {
            return;
        }
        lastStartIndex = startIndex;
        lastEndIndex = endIndex;

        // 范围变了：先回收旧格子，再取出新格子
        pool.RecycleAll();

        for (int i = startIndex; i <= endIndex; i++)
        {
            GameObject cell = pool.Get();
            if (cell == null) continue;
            PositionCell(cell, i);
            onCellBind?.Invoke(i, cell);
        }
    }

    /// <summary>
    /// 将格子定位到 Content 中第 index 个位置。
    /// 锚点固定在左上，anchoredPosition 计算从 padding 偏移开始。
    /// </summary>
    private void PositionCell(GameObject cell, int index)
    {
        int col = index % colCount;
        int row = index / colCount;

        float x = padding.left + col * (cellSize.x + spacing.x) + cellSize.x / 2;
        float y = -(padding.top + row * cellStepY + cellSize.y / 2);

        RectTransform rt = cell.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = cellSize;
    }
}
