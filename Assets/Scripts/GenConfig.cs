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
    };

    [CSharpCallLua]
    public static List<Type> CSharpCallLua = new List<Type>()
    {
        typeof(Action<int, GameObject>),
        typeof(Action<int>),
        typeof(Action),
    };
}
