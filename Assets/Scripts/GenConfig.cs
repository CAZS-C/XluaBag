using System;
using System.Collections.Generic;
using UnityEngine;
using XLua;

/// <summary>
/// XLua 代码生成配置，用于注册需要跨语言调用的类型。
/// CSharpCallLua 将 Lua 函数作为 C# 委托传递给 C# 时需要注册。
/// LuaCallCSharp 让 Lua 能够访问 C# 类型时使用，开启优化。
/// </summary>
public static class GenConfig
{
    [LuaCallCSharp]
    public static List<Type> LuaCallCSharp = new List<Type>()
    {
        typeof(UnityEngine.UI.ScrollRect),
        typeof(UnityEngine.UI.Toggle),
        typeof(UnityEngine.UI.ToggleGroup),
        typeof(UnityEngine.UI.Image),
        typeof(UnityEngine.UI.Button),
        typeof(TMPro.TextMeshProUGUI),
        typeof(BagGridLayout),
        typeof(BagItemPool),
        typeof(InventoryManager),
        typeof(PlayerData),
        typeof(ItemDefinition),
        typeof(ItemInstance),
        typeof(ItemSlot),
        typeof(InventoryEvent),
        typeof(IconAtlasLoader),
        // Demo 闭环：Lua 需要访问背包壳层、右键菜单、世界掉落和 HUD
        typeof(BagPanelView),
        typeof(ItemContextMenuView),
        typeof(ItemCellRightClick),
        typeof(DemoHUD),
        typeof(DemoPlayerStats),
        typeof(DemoWorld),
    };

    [CSharpCallLua]
    public static List<Type> CSharpCallLua = new List<Type>()
    {
        typeof(Action<int, GameObject>),
        typeof(Action<int>),
        typeof(Action),
        typeof(Action<SlotChangeData>),
        typeof(Action<int, int>),
    };

    /// <summary>
    /// 代码生成黑名单：这些成员只存在于编辑器版 UnityEngine 程序集，
    /// 生成进 Wrap 后编辑器能编译，但打包/构建 AB 时触发的 Player 编译会报 CS1061。
    /// 本项目的 Lua 业务用不到 Light，因此直接排除，保证工程可打包。
    /// </summary>
    [BlackList]
    public static List<List<string>> BlackList = new List<List<string>>()
    {
        new List<string>() { "UnityEngine.Light", "SetLightDirty" },
        new List<string>() { "UnityEngine.Light", "shadowRadius" },
        new List<string>() { "UnityEngine.Light", "shadowAngle" },
    };
}
