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
        pd:InitInventory(120)
    end

    local inv = CS.InventoryManager.GetInstance()
    if inv == nil then
        print("[Main] InventoryManager 为 nil，无法添加测试数据")
        return
    end

    -- ════════════════════════════════════════
    -- 装备：全部 20 件（品质 1~4），凑 2 页
    -- ════════════════════════════════════════
    for i = 1001, 1020 do
        inv:AddItem(i, 1)
    end

    -- 再补 10 件高品质装备（堆叠重复，验证排序稳定性），凑满 3 页
    for i = 1, 10 do
        inv:AddItem(1016 + (i % 4), 1)   -- 1016~1019 品质4装备循环
    end

    -- ════════════════════════════════════════
    -- 消耗品：全部 20 组（带数量）
    -- ════════════════════════════════════════
    for i = 2001, 2020 do
        inv:AddItem(i, 5)
    end

    -- ════════════════════════════════════════
    -- 宝石：全部 15 种
    -- ════════════════════════════════════════
    for i = 3001, 3015 do
        inv:AddItem(i, 3)
    end

    -- 统计
    local filled = 0
    for i = 0, pd.inventorySlots.Count - 1 do
        if pd.inventorySlots[i].instance ~= nil then
            filled = filled + 1
        end
    end
    print("[Main] 测试数据添加完成，非空格子数: " .. filled)
end

AddTestItems()

local BagPanelCtrl = require("Bag.BagPanelCtrl")
BagPanelCtrl.Init()
