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
    /// BagGridLayout.Refresh 时调用，Tab 切换会触发池子重建。
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
            cell.SetActive(false);
            allCells.Add(cell);
            freeCells.Enqueue(cell);
        }
    }

    /// <summary>
    /// 从池中取一个格子，激活后返回。
    /// 由 BagGridLayout.OnScroll 遍历可见索引时调用。
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
    /// 每帧 OnScroll 触发前先回收，再重新分配可见格子。
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
