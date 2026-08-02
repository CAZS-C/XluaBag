using System;
using System.Collections.Generic;
using UnityEngine;
using XLua;

/// <summary>
/// 物品类型枚举
/// 与 Lua ItemConfig 中的 type 字符串对应
/// </summary>
public enum ItemType
{
    Equipment = 0,      // 装备
    Consumable = 1,     // 消耗品
    Gem = 2,            // 宝石
    Material = 3,       // 材料
}

/// <summary>
/// 品质等级枚举
/// 与 ItemConfig 中的 quality 数值对应
/// </summary>
public enum QualityLevel
{
    Common = 1,     // 普通（白）
    Rare = 2,       // 稀有（蓝）
    Epic = 3,       // 史诗（紫）
    Legendary = 4,  // 传说（橙）
}

/// <summary>
/// 物品静态模板 — 描述物品"是什么"。
///
/// 设计要点：
///   1. 全项目同 id 的物品共享同一个 ItemDefinition，避免重复存储，节省内存。
///   2. 数据唯一来源是 Lua 的 ItemConfig.lua（热更可改），C# 侧通过 FromLuaTable 加载后只读。
///   3. 用静态注册表（Dictionary）按 id 登记，运行时实例通过 defId 查表即可拿到模板。
/// </summary>
[Serializable]
public class ItemDefinition
{
    public int id;                  // 物品 ID，唯一
    public string name;             // 物品名称
    public ItemType type;           // 物品类型
    public QualityLevel quality;    // 品质等级
    public string iconPath;         // AB 内图标路径（如 "icon/sword_01"）
    public int stackMax;            // 最大堆叠数量（装备=1，药水可堆叠）

    // 全局静态注册表：id → Definition
    private static Dictionary<int, ItemDefinition> allDefinitions = new Dictionary<int, ItemDefinition>();

    /// <summary>
    /// 登记一个定义，供全局按 id 查询。已存在同 id 时覆盖。
    /// </summary>
    public static void Register(ItemDefinition def)
    {
        if (def == null) return;
        allDefinitions[def.id] = def;
    }

    /// <summary>
    /// 按 id 查询定义；不存在时返回 null。
    /// </summary>
    public static ItemDefinition Get(int id)
    {
        ItemDefinition def;
        allDefinitions.TryGetValue(id, out def);
        return def;
    }

    /// <summary>
    /// 清空注册表（用于重载配置）。
    /// </summary>
    public static void ClearAll()
    {
        allDefinitions.Clear();
    }

    /// <summary>
    /// 从 Lua 配置表创建一个 ItemDefinition 并自动登记到注册表。
    ///
    /// Lua 侧数据格式（见 ItemConfig.lua）：
    ///   [1001] = { id=1001, name="铁剑", type="Equipment", quality=2, icon="xxx", stackMax=1 }
    /// </summary>
    public static ItemDefinition FromLuaTable(LuaTable data)
    {
        ItemDefinition def = new ItemDefinition();
        def.id = data.Get<int>("id");
        def.name = data.Get<string>("name");
        def.type = ParseItemType(data.Get<string>("type"));
        def.quality = (QualityLevel)data.Get<int>("quality");
        def.iconPath = data.Get<string>("icon") ?? "";
        def.stackMax = data.Get<int>("stackMax");

        Register(def);
        return def;
    }

    /// <summary>
    /// 将 Lua 中的类型字符串转为枚举，未知类型回退为 Material。
    /// </summary>
    private static ItemType ParseItemType(string typeStr)
    {
        switch (typeStr)
        {
            case "Equipment": return ItemType.Equipment;
            case "Consumable": return ItemType.Consumable;
            case "Gem": return ItemType.Gem;
            default: return ItemType.Material;
        }
    }
}
