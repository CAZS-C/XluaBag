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
local initialized = false -- Init 防重入：避免重复订阅事件 / 重复绑定回调
local displayToUid = {}   -- 显示位置(0-based) → 物品实例 uid
local displayToCell = {}  -- 显示位置(0-based) → 格子 GameObject（用于更新选中高亮）
local currentTabType = "Equipment"
local selectedUid = 0     -- 当前选中的物品 uid（0 = 未选中）
local refreshing = false  -- 防重入标志：Sort 发事件 → Refresh 回调 → 再 Sort 会死循环

-- Tab 路径和类型映射，与预制体中的命名一致
local TAB_CONFIG = {
    { path = "Canvas/BagPanel/TabEquip", type = "Equipment" },
    { path = "Canvas/BagPanel/TabItem",  type = "Item" },
    { path = "Canvas/BagPanel/TabGem",   type = "Gem" },
}

function BagPanelCtrl.Init()
    -- 防重入：Init 只执行一次，避免重复订阅事件导致一次变化触发多次刷新
    if initialized then
        return
    end
    initialized = true

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

    -- 注入格子点击回调：点击格子 → uid 反查真实槽位 → 选中
    ItemCellCtrl.onCellClick = BagPanelCtrl.OnCellClick

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
    displayToCell = {}
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

-- 格子点击回调：用 uid 反查真实槽位并选中
function BagPanelCtrl.OnCellClick(cellGo, uid)
    if uid == nil or uid <= 0 then
        return
    end

    -- 切换选中：点同一个格子取消选中，否则选中新格子
    if selectedUid == uid then
        selectedUid = 0
    else
        selectedUid = uid
    end

    -- 通知数据层选中状态（广播 OnSelectionChanged）
    local slotIdx = inventoryMgr:GetSlotIndexByUid(uid)
    if slotIdx >= 0 then
        inventoryMgr:SelectSlot(selectedUid > 0 and slotIdx or -1)
    end

    -- 刷新所有格子的高亮
    BagPanelCtrl.RefreshSelected()
end

-- 根据 selectedUid 刷新所有格子高亮
function BagPanelCtrl.RefreshSelected()
    for pos, cellGo in pairs(displayToCell) do
        local uid = displayToUid[pos]
        ItemCellCtrl.SetSelected(cellGo, uid == selectedUid)
    end
end

function BagPanelCtrl.OnCellBind(index, cellGo)
    -- index 是 C# 传入的 0-based 显示位置
    local uid = displayToUid[index + 1]
    if not uid then
        return
    end
    displayToCell[index + 1] = cellGo

    -- 用 uid 反查真实槽位，渲染物品
    local slotIdx = inventoryMgr:GetSlotIndexByUid(uid)
    if slotIdx < 0 then
        return
    end

    local slot = inventoryMgr.Slots[slotIdx]
    local def = slot.instance.Definition

    ItemCellCtrl.Bind(cellGo, {
        itemId  = slot.instance.defId,
        uid     = slot.instance.uid,
        count   = slot.count,
        quality = def and def.qualityValue or 1,
        icon    = def and def.iconPath or nil,
        name    = def and def.name or "",
    })

    -- 恢复选中高亮（滚动重绑格子后保持选中状态）
    ItemCellCtrl.SetSelected(cellGo, uid == selectedUid)
end

return BagPanelCtrl
