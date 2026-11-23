using System;
using System.Collections.Generic;

/// <summary>
/// 物品运行时实例 — 描述物品"当前的状态"。
///
/// 设计要点：
///   1. uid 全局唯一，用于标识一个具体的物品实例（例如两把同 id 的铁剑，uid 不同）。
///   2. 序列化时只存 defId（int），不存 ItemDefinition 引用：
///      一方面避免 JsonUtility 存档时把整个模板冗余写入；
///      另一方面加载存档后通过 defId 查注册表即可重建 Definition，保证全局共享一份模板。
///
///   3. level / gemIds 为装备扩展字段（强化等级、镶嵌宝石），普通消耗品默认不启用。
/// </summary>
[Serializable]
public class ItemInstance
{
    /// <summary>
    /// 实例唯一 ID，全局递增分配
    /// </summary>
    public int uid;

    /// <summary>
    /// 指向物品模板的 ID（序列化友好，加载后经 Definition 属性解析）
    /// </summary>
    public int defId;

    /// <summary>
    /// 强化等级（装备使用，普通物品为 0）
    /// </summary>
    public int level;

    /// <summary>
    /// 镶嵌宝石的 uid 列表（装备使用）
    /// </summary>
    public List<int> gemIds = new List<int>();

    // 全局自增计数器，保证 uid 唯一
    private static int uidCounter = 1;

    /// <summary>
    /// 物品模板（只读），通过 defId 从全局注册表解析
    /// </summary>
    public ItemDefinition Definition
    {
        get { return ItemDefinition.Get(defId); }
    }

    /// <summary>
    /// 通过模板 id 创建实例并自动分配唯一 uid。
    /// </summary>
    public static ItemInstance Create(int defId)
    {
        ItemInstance inst = new ItemInstance();
        inst.uid = uidCounter++;
        inst.defId = defId;
        return inst;
    }

    /// <summary>
    /// 读档后调用：把静态 uid 计数器抬到「存档中最大 uid + 1」，保证后续新建实例不会复用旧 uid。
    ///
    /// 为什么必须做：
    ///   uidCounter 是静态变量，每次运行都从 1 重新开始；读档会把上一局的 uid（例如 1~67）还原回来。
    ///   如果不抬高计数器，之后 AddItem 生成的新实例会再次拿到 1、2、3……
    ///   而 UI 交互（点选/反查槽位）以 uid 为锚，一旦重复就会选中错误的物品。
    /// </summary>
    public static void EnsureUidCounterAbove(int maxUid)
    {
        if (maxUid >= uidCounter)
        {
            uidCounter = maxUid + 1;
        }
    }
}
