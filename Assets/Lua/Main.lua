-- Main.lua
-- 游戏 Lua 入口，由 C# Main.cs 的 Start 方法通过 DoLuaFile("Main") 加载。
-- 职责：初始化各模块

local BagPanelCtrl = require("Bag.BagPanelCtrl")
BagPanelCtrl.Init()
