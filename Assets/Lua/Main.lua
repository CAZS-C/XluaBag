-- Main.lua
-- 游戏 Lua 入口，由 C# Main.cs 的 Start 方法通过 DoLuaFile("Main") 加载。
-- 职责：准备测试数据 → 初始化各模块
--
-- 注意：ConfigManager 已在 C# Main.cs 中先于本脚本加载好物品定义注册表，
-- 因此这里可以直接用 defId 添加物品。

local function AddTestItems()
    -- 确保背包已初始化（场景中 PlayerData 组件提供）
    local pd = CS.PlayerData.GetInstance()
    if pd == nil then
        return
    end
    if pd.inventorySlots.Count == 0 then
        pd:InitInventory(60)
    end

    local inv = CS.InventoryManager.GetInstance()
    if inv == nil then
        print("[Main] InventoryManager 为 nil，无法添加测试数据")
        return
    end

    -- 装备（品质 1~4 混搭，验证按品质降序排列）
    local ok1 = inv:AddItem(1001, 1)   -- 铁剑       品质1
    local ok2 = inv:AddItem(1005, 1)   -- 钢盾       品质2
    local ok3 = inv:AddItem(1009, 1)   -- 火焰之剑   品质3
    local ok4 = inv:AddItem(1016, 1)   -- 龙骨战锤   品质4
    local ok5 = inv:AddItem(1013, 1)   -- 霜噬之刃   品质3

    -- 消耗品
    local ok6 = inv:AddItem(2001, 20)  -- 生命药水
    local ok7 = inv:AddItem(2007, 10)  -- 大回复药水
    local ok8 = inv:AddItem(2016, 2)   -- 狂暴药剂   品质4

    -- 宝石
    local ok9 = inv:AddItem(3001, 12)  -- 红宝石     品质2
    local ok10 = inv:AddItem(3006, 4)  -- 攻击红宝石 品质3
    local ok11 = inv:AddItem(3011, 1)  -- 龙炎红宝石 品质4

    print("[Main] AddItem 结果: " .. tostring(ok1) .. " " .. tostring(ok2) .. " " .. tostring(ok3)
        .. " " .. tostring(ok4) .. " " .. tostring(ok5) .. " " .. tostring(ok6) .. " " .. tostring(ok7)
        .. " " .. tostring(ok8) .. " " .. tostring(ok9) .. " " .. tostring(ok10) .. " " .. tostring(ok11))
end

AddTestItems()

local BagPanelCtrl = require("Bag.BagPanelCtrl")
BagPanelCtrl.Init()
