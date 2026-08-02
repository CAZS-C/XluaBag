using System;

/// <summary>
/// 背包槽位 — 描述"背包里的一个位置放了什么"。
///
/// 设计要点：
///   1. 一个槽位只放一种物品：instance 指向运行时实例，count 表示当前堆叠数量。
///   2. 装备等不可堆叠物品 count 恒为 1；药水等可堆叠物品 count 可大于 1。
///   3. instance 为 null 或 count 为 0 时视为空槽。
/// </summary>
[Serializable]
public class ItemSlot
{
    /// <summary>
    /// 槽位中的物品实例（null = 空槽）
    /// </summary>
    public ItemInstance instance;

    /// <summary>
    /// 当前堆叠数量
    /// </summary>
    public int count;

    /// <summary>
    /// 是否为空槽
    /// </summary>
    public bool IsEmpty
    {
        get { return instance == null || count <= 0; }
    }

    /// <summary>
    /// 清空槽位
    /// </summary>
    public void Clear()
    {
        instance = null;
        count = 0;
    }
}
