using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包数据管理层 — 核心职责：物品的增删改查、堆叠、移动、筛选、排序。
///
/// 设计要点：
///   1. 不持有自己的槽位列表，而是直接操作 PlayerData.inventorySlots（单一数据源）。
///      PlayerData 是数据载体（负责存档），InventoryManager 是控制器（负责逻辑）。
///   2. 完全不依赖任何 UI 类。数据变化通过 InventoryEvent 事件广播，UI 自行订阅。
///   3. 所有方法操作成功后统一走 EmitSlotChanged 发事件，UI 只刷变化的格子。
/// </summary>
public class InventoryManager : SingletonMono<InventoryManager>
{
    /// <summary>
    /// 背包容量上限
    /// </summary>
    [SerializeField]
    private int maxSlots = 99;

    private int curSelectedIndex = -1;

    /// <summary>
    /// 引用 PlayerData 的槽位列表（单一数据源）。
    /// 首次访问时懒初始化：消除对 PlayerData.Awake 执行顺序的依赖，
    /// 无论组件 Awake 先后，第一次使用背包时保证列表已建好。
    /// </summary>
    public List<ItemSlot> Slots
    {
        get
        {
            PlayerData pd = PlayerData.GetInstance();
            if (pd == null)
            {
                Debug.LogError("InventoryManager.Slots：场景中未挂载 PlayerData");
                return new List<ItemSlot>();
            }
            if (pd.inventorySlots.Count == 0)
            {
                pd.InitInventory(maxSlots);
            }
            return pd.inventorySlots;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        // 首次访问 Slots 时懒初始化（见 Slots 属性），这里无需额外处理。
        // 时序无关：即使 PlayerData.Awake 晚于本组件，首次用背包时也会自动初始化。
    }

    /// <summary>
    /// 预检查背包是否能完整放下 count 个物品。
    /// 用途：地面拾取前先判断，避免 AddItem 部分放入后调用方又销毁地面物品，
    /// 造成“只捡到一部分，但地上物品已经消失”的数据丢失。
    /// </summary>
    public bool CanAdd(int defId, int count = 1)
    {
        if (count <= 0) return true;

        ItemDefinition def = ItemDefinition.Get(defId);
        if (def == null) return false;

        int space = 0;
        for (int i = 0; i < Slots.Count; i++)
        {
            ItemSlot slot = Slots[i];
            if (slot.IsEmpty)
            {
                space += def.stackMax;
            }
            else if (slot.instance.defId == defId && slot.count < def.stackMax)
            {
                space += def.stackMax - slot.count;
            }

            if (space >= count) return true;
        }
        return false;
    }

    /// <summary>
    /// 添加物品（自动堆叠到已有同 id 槽位，再找空槽）。
    /// 返回是否全部放入成功（false = 背包空间不足，且本次不会放入任何物品）。
    /// </summary>
    public bool AddItem(int defId, int count = 1)
    {
        ItemDefinition def = ItemDefinition.Get(defId);
        if (def == null)
        {
            Debug.LogWarning($"AddItem 失败：未注册的 defId {defId}");
            return false;
        }

        // 先做整体容量检查：失败时保持背包原状，调用方可以安全地保留地面物品
        if (!CanAdd(defId, count))
        {
            Debug.LogWarning($"背包空间不足，{def.name} ×{count} 未能放入");
            return false;
        }

        int remaining = count;

        // 1. 优先堆叠到已有的同 id 且未满的槽位
        for (int i = 0; i < Slots.Count && remaining > 0; i++)
        {
            ItemSlot slot = Slots[i];
            if (slot.IsEmpty || slot.instance.defId != defId) continue;
            if (slot.count >= def.stackMax) continue;

            int add = Mathf.Min(remaining, def.stackMax - slot.count);
            slot.count += add;
            remaining -= add;
            EmitSlotChanged(i);
        }

        // 2. 剩余数量放到空槽
        for (int i = 0; i < Slots.Count && remaining > 0; i++)
        {
            ItemSlot slot = Slots[i];
            if (!slot.IsEmpty) continue;

            slot.instance = ItemInstance.Create(defId);
            slot.count = Mathf.Min(remaining, def.stackMax);
            remaining -= slot.count;
            EmitSlotChanged(i);
        }

        if (remaining > 0)
        {
            Debug.LogWarning($"背包空间不足，{def.name} 有 {remaining} 个未能放入");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 从指定槽位移除 count 个物品；数量归零时清空槽位。
    /// </summary>
    public bool RemoveItem(int slotIndex, int count = 1)
    {
        if (slotIndex < 0 || slotIndex >= Slots.Count) return false;
        ItemSlot slot = Slots[slotIndex];
        if (slot.IsEmpty || slot.count < count) return false;

        slot.count -= count;
        if (slot.count <= 0)
        {
            slot.Clear();
        }
        EmitSlotChanged(slotIndex);
        return true;
    }

    /// <summary>
    /// 移动/交换两个槽位的物品。支持同 id 可堆叠物品合并。
    /// </summary>
    public bool MoveItem(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return false;
        if (fromIndex < 0 || fromIndex >= Slots.Count) return false;
        if (toIndex < 0 || toIndex >= Slots.Count) return false;

        ItemSlot from = Slots[fromIndex];
        ItemSlot to = Slots[toIndex];

        // 源槽为空，直接失败
        if (from.IsEmpty) return false;

        // 目标为空：直接搬过去
        if (to.IsEmpty)
        {
            to.instance = from.instance;
            to.count = from.count;
            from.Clear();
            EmitSlotChanged(fromIndex);
            EmitSlotChanged(toIndex);
            return true;
        }

        // 同 id 可堆叠 → 合并
        if (from.instance.defId == to.instance.defId)
        {
            ItemDefinition def = from.instance.Definition;
            if (def != null && def.stackMax > to.count)
            {
                int add = Mathf.Min(from.count, def.stackMax - to.count);
                to.count += add;
                from.count -= add;
                if (from.count <= 0) from.Clear();
                EmitSlotChanged(fromIndex);
                EmitSlotChanged(toIndex);
                return true;
            }
        }

        // 其余情况：交换两个槽位
        ItemInstance tmpInst = from.instance;
        int tmpCount = from.count;
        from.instance = to.instance;
        from.count = to.count;
        to.instance = tmpInst;
        to.count = tmpCount;
        EmitSlotChanged(fromIndex);
        EmitSlotChanged(toIndex);
        return true;
    }

    /// <summary>
    /// 按类型过滤出非空槽位（供 UI 按 Tab 展示）。
    /// </summary>
    public List<ItemSlot> GetFilteredSlots(ItemType type)
    {
        List<ItemSlot> result = new List<ItemSlot>();
        for (int i = 0; i < Slots.Count; i++)
        {
            ItemSlot slot = Slots[i];
            if (slot.IsEmpty) continue;
            if (slot.instance.Definition != null && slot.instance.Definition.type == type)
            {
                result.Add(slot);
            }
        }
        return result;
    }

    /// <summary>
    /// 排序（按品质降序 → id 升序）。后续可由 Lua 传入自定义排序规则。
    ///
    /// 注意：
    ///   1. 这里必须保存 ItemSlot 的"值副本"（instance 引用 + count），
    ///      而不能保存 ItemSlot 对象本身——因为后续 Slots[i].Clear() 会清空同一个对象，
    ///      导致副本里的数据也被清空（引用类型陷阱）。
    ///   2. 排序属于"布局变化"，不再逐槽位发 OnSlotChanged：
    ///      调用方（BagPanelCtrl.Refresh）在排序完成后自行重建映射并全量刷新。
    ///      否则一次排序会触发几十次事件，UI 端即使有防重入也会做几十次无效回调。
    ///   3. 配置缺失（Definition == null）时按 quality=0 排在最后，
    ///      不能直接跳过——否则清空槽位后该物品会丢失。
    /// </summary>
    public void Sort()
    {
        // 临时结构保存非空槽的实例引用与数量（值副本）
        List<ItemInstance> items = new List<ItemInstance>();
        List<int> counts = new List<int>();
        List<int> indexes = new List<int>();
        bool hasMissingDefinition = false;

        for (int i = 0; i < Slots.Count; i++)
        {
            if (!Slots[i].IsEmpty)
            {
                items.Add(Slots[i].instance);
                counts.Add(Slots[i].count);
                indexes.Add(i);
                if (Slots[i].instance.Definition == null)
                {
                    hasMissingDefinition = true;
                }
            }
        }

        // 排序索引（品质降序，再按 id 升序）
        List<int> order = new List<int>();
        for (int i = 0; i < items.Count; i++) order.Add(i);
        order.Sort((a, b) =>
        {
            ItemDefinition da = items[a].Definition;
            ItemDefinition db = items[b].Definition;
            QualityLevel qa = da != null ? da.quality : (QualityLevel)0;
            QualityLevel qb = db != null ? db.quality : (QualityLevel)0;
            if (qa != qb)
                return qb.CompareTo(qa);
            return items[a].defId.CompareTo(items[b].defId);
        });

        // 清空所有槽后按排序结果重填到原位置
        for (int i = 0; i < Slots.Count; i++)
        {
            Slots[i].Clear();
        }
        for (int i = 0; i < order.Count; i++)
        {
            int src = order[i];
            Slots[indexes[i]].instance = items[src];
            Slots[indexes[i]].count = counts[src];
        }

        if (hasMissingDefinition)
        {
            Debug.LogWarning("InventoryManager.Sort：存在未注册的 ItemDefinition，已按 quality=0 排在最后");
        }
    }

    /// <summary>
    /// 设置当前选中槽位，广播选中变化事件。
    /// </summary>
    public void SelectSlot(int index)
    {
        int prev = curSelectedIndex;
        curSelectedIndex = index;
        InventoryEvent.EmitSelectionChanged(prev, curSelectedIndex);
    }

    /// <summary>
    /// 按物品实例 uid 反查所在槽位索引。
    /// 返回 -1 表示未找到（物品可能已被移除）。
    ///
    /// 设计目的：UI 交互以 uid 为锚（物品身份稳定），
    /// 排序/移动/筛选改变槽位顺序后，仍能通过 uid 定位到物品当前所在槽位。
    /// </summary>
    public int GetSlotIndexByUid(int uid)
    {
        for (int i = 0; i < Slots.Count; i++)
        {
            if (!Slots[i].IsEmpty && Slots[i].instance.uid == uid)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// 派发单个槽位的变化事件
    /// </summary>
    private void EmitSlotChanged(int index)
    {
        ItemSlot slot = Slots[index];
        InventoryEvent.EmitSlotChanged(new SlotChangeData(
            index,
            slot.IsEmpty ? null : slot.instance,
            slot.IsEmpty ? 0 : slot.count));
    }
}
