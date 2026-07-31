-- BagPanelCtrl.lua
-- 背包面板主控制器：负责 Tab 切换、数据过滤、驱动 C# 网格刷新。
--
-- 职责：
--   1. Init 时获取场景中 BagGridLayout 的引用，绑定 onCellBind 回调。
--   2. 为三个 Tab Toggle（Equip / Item / Gem）绑定切换事件。
--   3. Tab 切换时按 type 过滤 ItemConfig 中的数据，调用 grid:Refresh(count)。
--   4. OnCellBind 中从过滤结果取数据，调用 ItemCellCtrl.Bind 刷新格子 UI。
--
-- 数据流向：
--   Tab 切换 → OnTabChanged(type) → 过滤 ItemConfig → grid:Refresh(#filtered)
--   → C# BagGridLayout.OnScroll → onCellBind(index, cellGo)
--   → BagPanelCtrl.OnCellBind → ItemCellCtrl.Bind(cellGo, data)

local ItemConfig   = require("Config/ItemConfig")
local ItemCellCtrl = require("Bag/ItemCellCtrl")

local BagPanelCtrl = {}

local grid
local filteredItems = {}

-- Tab 路径和类型映射，与预制体中的命名一致
local TAB_CONFIG = {
    { path = "Canvas/BagPanel/TabEquip", type = "Equipment" },
    { path = "Canvas/BagPanel/TabItem",  type = "Item" },
    { path = "Canvas/BagPanel/TabGem",   type = "Gem" },
}

function BagPanelCtrl.Init()
    local content = CS.UnityEngine.GameObject.Find("Canvas/BagPanel/svBag/Viewport/Content")
    if content == nil then
        return
    end
    grid = content:GetComponent(typeof(CS.BagGridLayout))
    if grid == nil then
        return
    end
    grid.onCellBind = BagPanelCtrl.OnCellBind

    for i, tab in ipairs(TAB_CONFIG) do
        BagPanelCtrl.BindTab(tab.path, tab.type)
    end

    local tabEquip = CS.UnityEngine.GameObject.Find(TAB_CONFIG[1].path)
    if tabEquip then
        local toggle = tabEquip:GetComponent(typeof(CS.UnityEngine.UI.Toggle))
        toggle.isOn = true
    end

    -- 初始显示 Equipment Tab
    BagPanelCtrl.OnTabChanged("Equipment")
end

function BagPanelCtrl.BindTab(path, tabType)
    local toggleGO = CS.UnityEngine.GameObject.Find(path)
    if not toggleGO then
        return
    end
    local toggle = toggleGO:GetComponent(typeof(CS.UnityEngine.UI.Toggle))
    toggle.onValueChanged:AddListener(function(isOn)
        if isOn then
            BagPanelCtrl.OnTabChanged(tabType)
        end
    end)
end

function BagPanelCtrl.OnTabChanged(tabType)
    filteredItems = {}

    if tabType == "Equipment" then
        for _, data in pairs(ItemConfig) do
            if data.type == "Equipment" then
                table.insert(filteredItems, data)
            end
        end

    elseif tabType == "Item" then
        for _, data in pairs(ItemConfig) do
            if data.type == "Consumable" then
                table.insert(filteredItems, data)
            end
        end

    elseif tabType == "Gem" then
        for _, data in pairs(ItemConfig) do
            if data.type == "Gem" then
                table.insert(filteredItems, data)
            end
        end
    end

    if grid then
        grid:Refresh(#filteredItems)
    end
end

function BagPanelCtrl.OnCellBind(index, cellGo)
    -- index 是 C# 传入的 0-based 索引，Lua table 是 1-based
    local data = filteredItems[index + 1]
    if not data then
        return
    end

    ItemCellCtrl.Bind(cellGo, {
        itemId = data.id,
        count  = data.count,
        icon   = nil,
    })
end

return BagPanelCtrl
