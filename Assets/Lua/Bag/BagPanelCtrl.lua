-- BagPanelCtrl.lua
-- 背包面板主控制器：负责 Tab 切换、数据筛选、驱动 C# 网格刷新。
--
-- 数据源（S5 改造后）：从 C# InventoryManager 读真实背包数据，
--   替代早期版本的 Lua ItemConfig 假数据。
--
-- 职责：
--   1. Init 时获取 BagGridLayout 引用，绑定 onCellBind 回调。
--   2. 为三个 Tab Toggle（Equip / Item / Gem）绑定切换事件。
--   3. OnTabChanged：调 InventoryManager.Sort 真实重排 → 按类型筛选 → 建立 uid 映射 → grid:Refresh。
--   4. OnCellBind：通过 uid 反查真实槽位，渲染该物品。
--   5. 订阅 InventoryEvent.OnSlotChanged，数据变化时刷新当前 Tab。
--
-- 关键设计：uid 锚定
--   排序后显示顺序 != 真实槽位顺序，因此 UI 层保存"显示位置 → 物品 uid"映射，
--   交互（点选/后续右键）一律用 uid 反查真实槽位，保证操作对象永远正确。
--   见 MD 文档《背包系统架构方案(简练级)》数据流章节。

local ItemCellCtrl = require("Bag/ItemCellCtrl")

local BagPanelCtrl = {}

local grid
local inventoryMgr
local displayToUid = {}   -- 显示位置(0-based) → 物品实例 uid
local currentTabType = "Equipment"
local refreshing = false  -- 防重入标志：Sort 发事件 → Refresh 回调 → 再 Sort 会死循环

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

    -- 获取 InventoryManager（数据源）
    inventoryMgr = CS.InventoryManager.GetInstance()

    -- 订阅背包数据变化事件（XLua 用 "+" 字符串订阅 C# 事件），变化时刷新当前 Tab
    CS.InventoryEvent.OnSlotChanged("+", function(data)
        BagPanelCtrl.Refresh()
    end)

    -- 绑定 Tab Toggle 事件
    for i, tab in ipairs(TAB_CONFIG) do
        BagPanelCtrl.BindTab(tab.path, tab.type)
    end

    local tabEquip = CS.UnityEngine.GameObject.Find(TAB_CONFIG[1].path)
    if tabEquip then
        local toggle = tabEquip:GetComponent(typeof(CS.UnityEngine.UI.Toggle))
        toggle.isOn = true
    end

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
    currentTabType = tabType
    BagPanelCtrl.Refresh()
end

-- 刷新：真实排序 → 按类型筛选 → 建立 uid 映射 → 驱动网格
function BagPanelCtrl.Refresh()
    if not inventoryMgr then
        return
    end
    -- 防重入：Sort 触发的 OnSlotChanged 事件回调 Refresh 时直接忽略，
    -- 避免 Refresh → Sort → 事件 → Refresh 的无限循环
    if refreshing then
        return
    end
    refreshing = true

    -- 1. 真实排序（按品质降序重排槽位，符合设计文档 Sort() 语义）
    inventoryMgr:Sort()

    -- 2. 遍历槽位，筛选当前 Tab 类型，同时记录显示位置 → uid 映射
    displayToUid = {}
    local slots = inventoryMgr.Slots
    local realSlotCount = slots.Count

    local typeName = currentTabType
    for i = 0, realSlotCount - 1 do
        local slot = slots[i]
        if slot.instance ~= nil then
            local def = slot.instance.Definition
            if def ~= nil then
                local matches = false
                if typeName == "Equipment" then
                    matches = (def.type == CS.ItemType.Equipment)
                elseif typeName == "Item" then
                    matches = (def.type == CS.ItemType.Consumable)
                elseif typeName == "Gem" then
                    matches = (def.type == CS.ItemType.Gem)
                end

                if matches then
                    table.insert(displayToUid, slot.instance.uid)
                end
            end
        end
    end

    -- 3. 驱动网格
    if grid then
        grid:Refresh(#displayToUid)
    end

    refreshing = false
end

function BagPanelCtrl.OnCellBind(index, cellGo)
    -- index 是 C# 传入的 0-based 显示位置
    local uid = displayToUid[index + 1]
    if not uid then
        return
    end

    -- 用 uid 反查真实槽位，渲染物品
    local slotIdx = inventoryMgr:GetSlotIndexByUid(uid)
    if slotIdx < 0 then
        return
    end

    local slot = inventoryMgr.Slots[slotIdx]
    local def = slot.instance.Definition

    ItemCellCtrl.Bind(cellGo, {
        itemId = slot.instance.defId,
        count  = slot.count,
        icon   = nil,
        name   = def and def.name or "",
    })
end

return BagPanelCtrl
