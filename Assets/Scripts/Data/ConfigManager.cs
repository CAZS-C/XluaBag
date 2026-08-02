using UnityEngine;
using XLua;

/// <summary>
/// 配置管理器 — 负责把 Lua 中的静态配置表加载进 C# 内存（唯一数据源）。
///
/// 设计要点：
///   1. 物品的"是什么"（name/type/quality/iconPath/stackMax）只存在于 Lua 的
///      ItemConfig.lua（热更可改），C# 侧通过此处加载成 ItemDefinition 全局注册表。
///   2. 启动时由 Main.cs 调用，先于 Lua 业务逻辑执行，保证后续 AddItem 等操作能查到定义。
///   3. 纯静态工具类，无需挂载 GameObject。
/// </summary>
public static class ConfigManager
{
    /// <summary>
    /// 加载物品配置：执行 require 拿到 ItemConfig 表，逐条转成 ItemDefinition 并注册。
    /// 需在 LuaMgr.Init 之后调用。
    /// </summary>
    public static void LoadItemConfig()
    {
        // 通过 LuaMgr 执行 require，并把结果存到全局 _itemConfig 便于 C# 读取
        LuaMgr.GetInstance().DoString("_itemConfig = require('Config/ItemConfig')");
        LuaTable itemConfig = LuaMgr.GetInstance().Global.Get<LuaTable>("_itemConfig");

        int count = 0;
        foreach (var key in itemConfig.GetKeys())
        {
            LuaTable itemTable = itemConfig.Get<LuaTable>(key);
            ItemDefinition.FromLuaTable(itemTable);
            count++;
        }

        Debug.Log($"[ConfigManager] 加载物品定义 {count} 条");
    }
}
