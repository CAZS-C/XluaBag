using System;

/// <summary>
/// 槽位变化数据 — 事件载荷。
/// 包含变化槽位的完整信息，UI 收到事件后只需刷新该槽位，无需全量刷新。
/// </summary>
public struct SlotChangeData
{
    public int index;           // 变化的槽位（真实索引）
    public ItemInstance item;   // 槽位中的物品实例（null = 槽位已清空）
    public int count;           // 变化后的数量（0 = 槽位已清空）

    public SlotChangeData(int index, ItemInstance item, int count)
    {
        this.index = index;
        this.item = item;
        this.count = count;
    }
}

/// <summary>
/// 背包全局事件总线（静态类，免挂载）。
///
/// 设计目的：
///   数据层（InventoryManager）只负责修改数据并发事件，
///   不直接调用任何 UI。谁关心数据变化，谁订阅对应事件。
///   这样数据层与 UI 完全解耦，符合分层架构。
/// </summary>
public static class InventoryEvent
{
    /// <summary>
    /// 槽位变化事件：新增、移除、堆叠数量变化
    /// </summary>
    public static event Action<SlotChangeData> OnSlotChanged;

    /// <summary>
    /// 选中变化事件：参数为 (上一个选中索引, 当前选中索引)，-1 表示无选中
    /// </summary>
    public static event Action<int, int> OnSelectionChanged;

    /// <summary>
    /// 派发槽位变化事件（由 InventoryManager 调用）
    /// </summary>
    public static void EmitSlotChanged(SlotChangeData data)
    {
        OnSlotChanged?.Invoke(data);
    }

    /// <summary>
    /// 派发选中变化事件（由 InventoryManager 调用）
    /// </summary>
    public static void EmitSelectionChanged(int prevIndex, int curIndex)
    {
        OnSelectionChanged?.Invoke(prevIndex, curIndex);
    }
}
