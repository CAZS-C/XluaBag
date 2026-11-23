using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 格子对象池 — 挂在 Content 上，由 BagGridLayout 驱动。
/// 
/// 核心职责：
///   预实例化固定数量的 BagItemCell，供 BagGridLayout 滚动时取/放。
///   数量 = colCount × (可视行数 + 2)，约 20 个左右。
///   所有格子共用同一个 cellPrefab，按需 SetActive。
/// 
/// 为什么做对象池：
///   500 个物品如果每帧 Instantiate/Destroy，GC 压力巨大。
///   池化后只有约 20 个常驻 GameObject，性能可预测。
/// </summary>
public class BagItemPool : MonoBehaviour
{
    public GameObject cellPrefab;

    // allCells 持有全部格子引用，freeCells 是当前可用的格子队列
    private List<GameObject> allCells = new List<GameObject>();
    private Queue<GameObject> freeCells = new Queue<GameObject>();

    /// <summary>
    /// 初始化池子：销毁旧格子，创建指定数量的新格子，初始为未激活状态。
    /// 仅在池子容量真的变化时使用（例如窗口尺寸变化导致可视行数变化）。
    /// 数据刷新/切 Tab 应优先调用 EnsureSize，避免无意义的销毁重建。
    /// </summary>
    public void Init(int size)
    {
        RecycleAll();
        for (int i = allCells.Count - 1; i >= 0; i--)
            Destroy(allCells[i]);
        allCells.Clear();
        freeCells.Clear();

        for (int i = 0; i < size; i++)
        {
            GameObject cell = Instantiate(cellPrefab, transform);

            // 右键转发组件运行时补上，避免改 BagItemCell 预制体和 PC AB；
            // 预制体以后若自带该组件，这里不会重复添加。
            if (cell.GetComponent<ItemCellRightClick>() == null)
            {
                cell.AddComponent<ItemCellRightClick>();
            }

            cell.SetActive(false);
            allCells.Add(cell);
            freeCells.Enqueue(cell);
        }
    }

    /// <summary>
    /// 确保池子容量为 size：
    ///   容量相同 → 只回收当前激活的格子，不销毁/不实例化；
    ///   容量不同 → 才走 Init 重建。
    ///
    /// 这是"一次数据变化不再产生 28 次 Instantiate + 28 次 Destroy"的关键。
    /// </summary>
    public void EnsureSize(int size)
    {
        if (allCells.Count == size)
        {
            RecycleAll();
            return;
        }
        Init(size);
    }

    /// <summary>
    /// 从池中取一个格子，激活后返回。
    /// 由 BagGridLayout.OnScroll 遍历可见索引时调用。
    ///
    /// 返回 null 的情况：可视范围计算包含上下缓冲行，请求数可能比池子多几格；
    /// 多出来的位置都在屏幕外，调用方 continue 跳过不会造成视觉缺失。
    /// </summary>
    public GameObject Get()
    {
        if (freeCells.Count == 0) return null;
        GameObject cell = freeCells.Dequeue();
        cell.SetActive(true);
        return cell;
    }

    /// <summary>
    /// 回收所有激活的格子，放回空闲队列。
    /// 只在可视范围真的变化（或池子容量变化）时调用，不是每帧调用。
    /// </summary>
    public void RecycleAll()
    {
        for (int i = 0; i < allCells.Count; i++)
        {
            if (allCells[i].activeSelf)
            {
                allCells[i].SetActive(false);
                freeCells.Enqueue(allCells[i]);
            }
        }
    }
}
