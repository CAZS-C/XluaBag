-- ItemCellCtrl.lua
-- 格子控制器模块：管理单个格子的 UI 刷新和交互状态。
--
-- 职责：
--   1. Bind(cellGo, slotData)：缓存 Transform 引用，设置图标、数量、品质色、清选中。
--   2. 首次绑定点击事件：点击格子 → 通知 BagPanelCtrl（onCellClick 回调）。
--   3. SetSelected(cellGo, selected)：切换选中高亮（imgSelect 显隐）。
--
-- 被 BagPanelCtrl 调用，不直接挂载到任何 GameObject。
-- cellTransformCache 使用弱表（__mode = "k"），cellGo 销毁时自动释放缓存。
--
-- 品质色映射（与设计文档一致）：
--   1=普通(白) 2=稀有(蓝) 3=史诗(紫) 4=传说(橙)
local UnityEngine = CS.UnityEngine
local UI = CS.UnityEngine.UI
local TMP = CS.TMPro

local ItemCellCtrl = {}

-- 点击回调，由 BagPanelCtrl.Init 注入：ItemCellCtrl.onCellClick = function(cellGo, uid) ... end
ItemCellCtrl.onCellClick = nil

local cellTransformCache = setmetatable({}, { __mode = "k" })

-- 品质 → 边框/选中颜色（普通/稀有/史诗/传说）
local QUALITY_COLORS = {
    [1] = { r = 1.0, g = 1.0, b = 1.0, a = 1.0 },    -- 白
    [2] = { r = 0.27, g = 0.53, b = 1.0, a = 1.0 },  -- 蓝
    [3] = { r = 0.67, g = 0.27, b = 1.0, a = 1.0 },  -- 紫
    [4] = { r = 1.0, g = 0.53, b = 0.0, a = 1.0 },   -- 橙
}

function ItemCellCtrl.Bind(cellGo, slotData)
    local cache = cellTransformCache[cellGo]
    if not cache then
        local trans = cellGo.transform
        cache = {
            imgIcon   = trans:Find("imgIcon"):GetComponent(typeof(UI.Image)),
            imgSelect = trans:Find("imgSelect"):GetComponent(typeof(UI.Image)),
            txtCount  = trans:Find("txtCount"):GetComponent(typeof(TMP.TextMeshProUGUI)),
            btn       = cellGo:GetComponent(typeof(UI.Button)),
            uid       = 0,
        }
        cellTransformCache[cellGo] = cache

        -- 首次绑定点击事件（只绑定一次，之后复用闭包捕获的 cellGo）
        if cache.btn then
            cache.btn.onClick:AddListener(function()
                if ItemCellCtrl.onCellClick then
                    ItemCellCtrl.onCellClick(cellGo, cache.uid)
                end
            end)
        end
    end

    cache.uid = slotData.uid or 0

    -- 图标：从 Resources 按 iconPath 加载 Sprite
    -- 注意：XLua 中 Resources.Load 的参数顺序是 (path, type)
    if slotData.icon and cache.imgIcon then
        local sprite = UnityEngine.Resources.Load(slotData.icon, typeof(UnityEngine.Sprite))
        cache.imgIcon.sprite = sprite
        cache.imgIcon.gameObject:SetActive(sprite ~= nil)
    end

    -- 品质边框颜色：作用于格子的 imgSelect 作为品质底色（非选中时半透明）
    if cache.imgSelect then
        local q = slotData.quality or 1
        local c = QUALITY_COLORS[q] or QUALITY_COLORS[1]
        cache.imgSelect.color = UnityEngine.Color(c.r, c.g, c.b, 0.25)
    end

    -- 数量文字
    if cache.txtCount and slotData.count then
        cache.txtCount.text = tostring(slotData.count)
        cache.txtCount.gameObject:SetActive(slotData.count > 1)
    end

    -- 默认不选中（选中状态由 BagPanelCtrl 统一管理）
    cache.imgSelect.gameObject:SetActive(false)
end

function ItemCellCtrl.SetSelected(cellGo, selected)
    local cache = cellTransformCache[cellGo]
    if cache and cache.imgSelect then
        cache.imgSelect.gameObject:SetActive(selected)
    end
end

return ItemCellCtrl
