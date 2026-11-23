-- BagPanelCtrl.lua
-- 背包面板主控制器：负责 Tab 切换、数据筛选、驱动 C# 网格刷新。
--
-- 数据源（S5 改造后）：从 C# InventoryManager 读真实背包数据，
--   替代早期版本的 Lua ItemConfig 假数据。
--
-- 职责：
--   1. Init 时获取 BagGridLayout 引用，绑定 onCellBind / onCellRecycled 回调。
--   2. 为三个 Tab Toggle（Equip / Item / Gem）绑定切换事件。
--   3. OnTabChanged：调 InventoryManager.Sort 真实重排 → 按类型筛选 → 建立映射 → grid:Refresh。
--   4. OnCellBind：通过显示位置直接取真实槽位，渲染该物品。
--   5. 订阅 InventoryEvent.OnSlotChanged，按变化类型决定"只刷一格"还是"全量刷新"。
--
-- 关键设计：
--   A. uid 锚定：UI 以 uid 识别物品，排序/筛选后点选不错位。
--   B. 单格刷新：只有"同一槽位、同一 uid、只有数量变化"时才只重绑一格；
--      新增 / 移除 / 换 Tab 属于列表成员变化，才全量刷新 + 排序。
--   C. 事件退订：Shutdown 时用同一个函数引用退订，避免 LuaEnv 释放后
--      C# 静态事件仍持有 DelegateBridge。

local ItemCellCtrl = require("Bag/ItemCellCtrl")

local BagPanelCtrl = {}

local grid
local inventoryMgr
local initialized = false
local currentTabType = "Equipment"
local selectedUid = 0
local refreshing = false -- 防重入：全量刷新期间忽略事件

-- 显示位置(1-based) → uid / 真实槽位索引(0-based)
local displayToUid = {}
local displayToSlot = {}
-- 真实槽位索引(0-based) → 显示位置(1-based)
local slotToDisplay = {}
-- 显示位置(1-based) → 当前真正绑定在该位置的格子（滚动回收时清空）
local displayToCell = {}

-- 事件回调必须保存成具名引用：退订时要用同一个函数
local slotChangedHandler = nil

-- Tab Toggle 的 Lua 监听：LuaEnv.Dispose 前必须 RemoveListener，否则 UnityEvent 会一直持有 DelegateBridge
local tabListeners = {}

-- 背包壳层与右键菜单（C# 提供显示/隐藏和 UI，业务规则留在 Lua）
local bagView
local contextMenu
local contextUid = 0

-- Tab 路径和类型映射，与预制体中的命名一致
local TAB_CONFIG = {
    { path = "Canvas/BagPanel/TabEquip", type = "Equipment" },
    { path = "Canvas/BagPanel/TabItem",  type = "Item" },
    { path = "Canvas/BagPanel/TabGem",   type = "Gem" },
}

-- 当前 Tab 是否匹配某个物品模板
local function MatchesTab(def)
    if def == nil then
        return false
    end
    if currentTabType == "Equipment" then
        return def.type == CS.ItemType.Equipment
    elseif currentTabType == "Item" then
        return def.type == CS.ItemType.Consumable
    elseif currentTabType == "Gem" then
        return def.type == CS.ItemType.Gem
    end
    return false
end

-- 用真实槽位数据刷新单个格子（图标 / 数量 / 品质 / 选中）
local function BindCell(cellGo, slot)
    if cellGo == nil then
        return
    end
    if slot == nil or slot.IsEmpty then
        ItemCellCtrl.Clear(cellGo)
        return
    end

    local def = slot.instance.Definition
    ItemCellCtrl.Bind(cellGo, {
        itemId  = slot.instance.defId,
        uid     = slot.instance.uid,
        count   = slot.count,
        quality = def and def.qualityValue or 1,
        icon    = def and def.iconPath or nil,
        name    = def and def.name or "",
    })
    ItemCellCtrl.SetSelected(cellGo, slot.instance.uid == selectedUid)
end

-- 重建"显示位置 ↔ 真实槽位"映射
local function RebuildDisplay()
    displayToUid = {}
    displayToSlot = {}
    slotToDisplay = {}

    local slots = inventoryMgr.Slots
    for i = 0, slots.Count - 1 do
        local slot = slots[i]
        if not slot.IsEmpty and MatchesTab(slot.instance.Definition) then
            table.insert(displayToUid, slot.instance.uid)
            table.insert(displayToSlot, i)
            slotToDisplay[i] = #displayToUid
        end
    end
end

-- 单格事件处理：只在"同一槽位、同一 uid、数量变化"时走快路径
local function HandleSlotChanged(data)
    if not initialized or refreshing then
        return
    end

    local pos = slotToDisplay[data.index]
    local uid = data.item and data.item.uid or 0
    local itemMatches = data.item ~= nil and MatchesTab(data.item.Definition)

    if pos ~= nil then
        -- 该槽位当前显示在当前 Tab：只有数量变化可以原地重绑
        if itemMatches and uid == displayToUid[pos] then
            local cellGo = displayToCell[pos]
            if cellGo ~= nil then
                BindCell(cellGo, inventoryMgr.Slots[data.index])
            end
            return
        end

        -- 物品消失 / 被替换 / 不再属于当前 Tab：列表成员变了，全量刷新
        BagPanelCtrl.Refresh(true)
        return
    end

    -- 槽位原本不在当前 Tab：只有"新增了一个属于当前 Tab 的物品"才需要刷新
    if itemMatches then
        BagPanelCtrl.Refresh(true)
    end
end

-- 安全包装：Lua 回调报错不能冒泡到 C# 的事件派发 / AddItem 循环
local function SafeSlotChanged(data)
    local ok, err = pcall(HandleSlotChanged, data)
    if not ok then
        print("[BagPanelCtrl] OnSlotChanged error: " .. tostring(err))
    end
end

local function SafeOnCellBind(index, cellGo)
    local ok, err = pcall(BagPanelCtrl.OnCellBind, index, cellGo)
    if not ok then
        print("[BagPanelCtrl] OnCellBind error: " .. tostring(err))
    end
end

local function SafeOnCellClick(cellGo, uid)
    local ok, err = pcall(BagPanelCtrl.OnCellClick, cellGo, uid)
    if not ok then
        print("[BagPanelCtrl] OnCellClick error: " .. tostring(err))
    end
end

local function SafeOnCellRightClick(cellGo, uid)
    local ok, err = pcall(BagPanelCtrl.OnCellRightClick, cellGo, uid)
    if not ok then
        print("[BagPanelCtrl] OnCellRightClick error: " .. tostring(err))
    end
end

local function SafeOnBagShown()
    local ok, err = pcall(function()
        BagPanelCtrl.HideContextMenu()
        BagPanelCtrl.Refresh(true)
    end)
    if not ok then
        print("[BagPanelCtrl] OnBagShown error: " .. tostring(err))
    end
end

local function SafeOnBagHidden()
    local ok, err = pcall(BagPanelCtrl.HideContextMenu)
    if not ok then
        print("[BagPanelCtrl] OnBagHidden error: " .. tostring(err))
    end
end

local function SafeUseSelected()
    local ok, err = pcall(BagPanelCtrl.UseSelected)
    if not ok then
        print("[BagPanelCtrl] UseSelected error: " .. tostring(err))
    end
end

local function SafeDropSelected()
    local ok, err = pcall(BagPanelCtrl.DropSelected)
    if not ok then
        print("[BagPanelCtrl] DropSelected error: " .. tostring(err))
    end
end

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
    grid.onCellBind = SafeOnCellBind
    grid.onCellRecycled = BagPanelCtrl.OnCellRecycled

    -- 获取 InventoryManager（数据源）
    inventoryMgr = CS.InventoryManager.GetInstance()

    -- 注入格子点击回调：点击格子 → uid 反查真实槽位 → 选中
    ItemCellCtrl.onCellClick = SafeOnCellClick
    ItemCellCtrl.onCellRightClick = SafeOnCellRightClick

    -- 背包壳层：打开时刷新、关闭时清掉右键菜单
    bagView = CS.BagPanelView.Instance
    if bagView ~= nil then
        bagView.onShown = SafeOnBagShown
        bagView.onHidden = SafeOnBagHidden
    end

    -- 右键菜单：使用/丢弃/取消的业务规则在 Lua，C# 只负责显示与定位
    contextMenu = CS.ItemContextMenuView.Instance
    if contextMenu ~= nil then
        contextMenu.onUse = SafeUseSelected
        contextMenu.onDrop = SafeDropSelected
        contextMenu.onCancel = function()
            contextUid = 0
        end
    end

    -- 订阅背包数据变化事件：回调保存成具名函数，Shutdown 时才能退订
    slotChangedHandler = SafeSlotChanged
    CS.InventoryEvent.OnSlotChanged("+", slotChangedHandler)

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

-- 退订 + 清理引用：LuaMgr.Dispose 前调用，避免 DelegateBridge 指向已释放的 LuaEnv
function BagPanelCtrl.Shutdown()
    if slotChangedHandler ~= nil then
        CS.InventoryEvent.OnSlotChanged("-", slotChangedHandler)
        slotChangedHandler = nil
    end

    -- 移除 Tab Toggle 的 Lua 监听
    for path, entry in pairs(tabListeners) do
        if entry.toggle ~= nil and entry.listener ~= nil then
            pcall(function()
                entry.toggle.onValueChanged:RemoveListener(entry.listener)
            end)
        end
    end
    tabListeners = {}

    -- 移除格子的点击监听
    ItemCellCtrl.Shutdown()

    if grid ~= nil then
        grid.onCellBind = nil
        grid.onCellRecycled = nil
    end
    ItemCellCtrl.onCellClick = nil
    ItemCellCtrl.onCellRightClick = nil

    if bagView ~= nil then
        bagView.onShown = nil
        bagView.onHidden = nil
        bagView = nil
    end

    if contextMenu ~= nil then
        contextMenu.onUse = nil
        contextMenu.onDrop = nil
        contextMenu.onCancel = nil
        contextMenu:Hide()
        contextMenu = nil
    end
    contextUid = 0

    displayToUid = {}
    displayToSlot = {}
    slotToDisplay = {}
    displayToCell = {}
    selectedUid = 0
    initialized = false
end

function BagPanelCtrl.BindTab(path, tabType)
    local toggleGO = CS.UnityEngine.GameObject.Find(path)
    if not toggleGO then
        return
    end
    local toggle = toggleGO:GetComponent(typeof(CS.UnityEngine.UI.Toggle))
    local listener = function(isOn)
        if isOn then
            BagPanelCtrl.OnTabChanged(tabType)
        end
    end
    toggle.onValueChanged:AddListener(listener)
    tabListeners[path] = { toggle = toggle, listener = listener }
end

function BagPanelCtrl.OnTabChanged(tabType)
    currentTabType = tabType
    -- 切 Tab 属于列表结构变化：排序 + 全量重建映射
    BagPanelCtrl.Refresh(true)
end

-- 刷新：按需排序 → 筛选 → 建立映射 → 驱动网格
-- needSort：只有"切 Tab / 列表成员变化"才传 true；
--           纯数量变化走 HandleSlotChanged 的单格路径，不进入本函数。
function BagPanelCtrl.Refresh(needSort)
    if not inventoryMgr then
        return
    end
    if refreshing then
        return
    end

    -- 列表结构变化后，原来菜单指向的 uid 可能已经换位/消失
    BagPanelCtrl.HideContextMenu()
    refreshing = true

    local ok, err = pcall(function()
        -- 1. 需要时真实排序（按品质降序重排槽位）
        if needSort then
            inventoryMgr:Sort()
        end

        -- 2. 选中项有效性校验：物品被彻底移除时清空过期选中。
        -- 注意：不能用 displayToUid 判断——切 Tab 只是显示过滤，物品仍在背包，选中必须保留。
        if selectedUid > 0 and inventoryMgr:GetSlotIndexByUid(selectedUid) < 0 then
            selectedUid = 0
            inventoryMgr:SelectSlot(-1) -- 同步数据层选中状态
        end

        -- 3. 重建"显示位置 ↔ 真实槽位"映射
        RebuildDisplay()

        -- 4. 驱动网格
        if grid then
            grid:Refresh(#displayToUid)
        end
    end)

    refreshing = false
    if not ok then
        print("[BagPanelCtrl] Refresh error: " .. tostring(err))
    end
end

-- 关闭右键菜单并清掉临时 uid
function BagPanelCtrl.HideContextMenu()
    if contextMenu ~= nil then
        contextMenu:Hide()
    end
    contextUid = 0
end

function BagPanelCtrl.Notify(message)
    if CS.DemoHUD.Instance ~= nil then
        CS.DemoHUD.Instance:ShowToast(message)
    else
        print("[BagPanelCtrl] " .. tostring(message))
    end
end

function BagPanelCtrl.Save()
    local pd = CS.PlayerData.GetInstance()
    if pd ~= nil then
        pd:Save()
    end
end

-- 右键：选中格子并在鼠标位置弹出“使用/丢弃/取消”
function BagPanelCtrl.OnCellRightClick(cellGo, uid)
    if uid == nil or uid <= 0 or inventoryMgr == nil then
        return
    end

    local slotIdx = inventoryMgr:GetSlotIndexByUid(uid)
    if slotIdx < 0 then
        return
    end

    -- 右键不做“再次点击取消”，而是稳定选中，方便下一步使用/丢弃
    selectedUid = uid
    inventoryMgr:SelectSlot(slotIdx)
    BagPanelCtrl.RefreshSelected()

    local slot = inventoryMgr.Slots[slotIdx]
    if slot == nil or slot.IsEmpty then
        return
    end
    local def = slot.instance.Definition
    if def == nil then
        return
    end

    local canUse = def.type == CS.ItemType.Consumable or def.type == CS.ItemType.Equipment
    local useLabel = def.type == CS.ItemType.Equipment and "装备" or "使用"

    contextUid = uid
    if contextMenu ~= nil then
        local mouse = CS.UnityEngine.Input.mousePosition
        contextMenu:Show(CS.UnityEngine.Vector2(mouse.x, mouse.y), canUse, useLabel)
    end
end

-- 右键菜单“使用/装备”
function BagPanelCtrl.UseSelected()
    local uid = contextUid
    BagPanelCtrl.HideContextMenu()
    if uid <= 0 or inventoryMgr == nil then
        return
    end

    local slotIdx = inventoryMgr:GetSlotIndexByUid(uid)
    if slotIdx < 0 then
        return
    end
    local slot = inventoryMgr.Slots[slotIdx]
    if slot == nil or slot.IsEmpty then
        return
    end
    local def = slot.instance.Definition
    if def == nil then
        return
    end

    if def.type == CS.ItemType.Consumable then
        if def.useValue <= 0 then
            BagPanelCtrl.Notify("该物品暂无使用效果")
            return
        end
        local stats = CS.DemoPlayerStats.Instance
        if stats == nil then
            return
        end
        if stats:Heal(def.useValue) then
            inventoryMgr:RemoveItem(slotIdx, 1)
            BagPanelCtrl.Notify("使用 " .. def.name .. "，回复 " .. tostring(def.useValue) .. " HP")
            BagPanelCtrl.Save()
        else
            BagPanelCtrl.Notify("生命值已满，暂时无法使用 " .. def.name)
        end
    elseif def.type == CS.ItemType.Equipment then
        local stats = CS.DemoPlayerStats.Instance
        if stats == nil then
            return
        end
        stats:Equip(uid, def.name)
        BagPanelCtrl.Notify("已装备 " .. def.name)
        BagPanelCtrl.Save()
    else
        BagPanelCtrl.Notify("该物品不能直接使用")
    end
end

-- 右键菜单“丢弃”：扣 1 个，落到玩家脚下，可再按 F 捡回
function BagPanelCtrl.DropSelected()
    local uid = contextUid
    BagPanelCtrl.HideContextMenu()
    if uid <= 0 or inventoryMgr == nil then
        return
    end

    local slotIdx = inventoryMgr:GetSlotIndexByUid(uid)
    if slotIdx < 0 then
        return
    end
    local slot = inventoryMgr.Slots[slotIdx]
    if slot == nil or slot.IsEmpty then
        return
    end
    local def = slot.instance.Definition
    if def == nil then
        return
    end

    if inventoryMgr:RemoveItem(slotIdx, 1) then
        local world = CS.DemoWorld.Instance
        if world ~= nil then
            world:DropFromPlayer(def.id, 1)
        end

        -- 丢掉的是当前装备时，HUD 不能继续显示它
        local stats = CS.DemoPlayerStats.Instance
        if stats ~= nil then
            stats:ValidateEquipped()
        end

        BagPanelCtrl.Notify("丢弃 " .. def.name)
        BagPanelCtrl.Save()
    end
end

-- 格子点击回调：用 uid 反查真实槽位并选中
function BagPanelCtrl.OnCellClick(cellGo, uid)
    if uid == nil or uid <= 0 then
        return
    end

    -- 左键点其他格子时，先收起右键菜单
    BagPanelCtrl.HideContextMenu()

    -- 切换选中：点同一个格子取消选中，否则选中新格子
    if selectedUid == uid then
        selectedUid = 0
    else
        selectedUid = uid
    end

    -- 通知数据层选中状态（只在这里做一次 uid 反查，点击不是高频路径）
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

-- 滚动回收 / 池子重建前由 C# 调用：清空"位置 → 格子"映射
function BagPanelCtrl.OnCellRecycled()
    BagPanelCtrl.HideContextMenu()
    displayToCell = {}
end

function BagPanelCtrl.OnCellBind(index, cellGo)
    -- index 是 C# 传入的 0-based 显示位置
    local pos = index + 1
    local slotIdx = displayToSlot[pos]
    if slotIdx == nil then
        ItemCellCtrl.Clear(cellGo)
        return
    end

    displayToCell[pos] = cellGo

    local slot = inventoryMgr.Slots[slotIdx]
    if slot == nil or slot.IsEmpty then
        ItemCellCtrl.Clear(cellGo)
        return
    end

    BindCell(cellGo, slot)
end

return BagPanelCtrl
