-- ItemCellCtrl.lua
-- 格子控制器模块：管理单个格子的 UI 刷新和交互状态。
--
-- 职责：
--   1. Bind(cellGo, slotData)：缓存 Transform 引用，设置图标、数量、品质色、清选中。
--   2. 首次绑定点击事件：点击格子 → 通知 BagPanelCtrl（onCellClick 回调）。
--   3. SetSelected(cellGo, selected)：切换选中高亮（imgSelect 显隐）。
--
-- 品质色与选中分离（code review #2）：
--   imgQuality — 品质底色，按 quality 常显（半透明）
--   imgSelect  — 选中高亮，颜色固定为亮黄，仅 SetActive 控制显隐
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

-- 图标缓存（code review #1）：同一图标只从 Resources 加载一次，滚动重绑时复用
local iconCache = {}

-- 品质 → 底色（普通/稀有/史诗/传说）
local QUALITY_COLORS = {
    [1] = { r = 1.0, g = 1.0, b = 1.0, a = 0.25 },   -- 白
    [2] = { r = 0.27, g = 0.53, b = 1.0, a = 0.25 }, -- 蓝
    [3] = { r = 0.67, g = 0.27, b = 1.0, a = 0.25 }, -- 紫
    [4] = { r = 1.0, g = 0.53, b = 0.0, a = 0.25 },  -- 橙
}

-- 选中高亮色（亮黄，与品质色分离，后续可换贴图）
local SELECT_COLOR = { r = 1.0, g = 0.92, b = 0.3, a = 0.8 }

-- 加载图标（带缓存）
-- 优先从 SpriteAtlas 取图（同屏图标合批）；图集未就绪时退回 Resources 单图加载
local function LoadIcon(path)
    if not path or path == "" then
        return nil
    end
    if iconCache[path] then
        return iconCache[path]
    end

    local sprite = CS.IconAtlasLoader.Get(path)
    if sprite == nil then
        sprite = UnityEngine.Resources.Load(path, typeof(UnityEngine.Sprite))
    end

    iconCache[path] = sprite
    return sprite
end

function ItemCellCtrl.Bind(cellGo, slotData)
    local cache = cellTransformCache[cellGo]
    if not cache then
        local trans = cellGo.transform
        cache = {
            imgQuality = trans:Find("imgQuality"):GetComponent(typeof(UI.Image)),
            imgIcon    = trans:Find("imgIcon"):GetComponent(typeof(UI.Image)),
            imgSelect  = trans:Find("imgSelect"):GetComponent(typeof(UI.Image)),
            txtCount   = trans:Find("txtCount"):GetComponent(typeof(TMP.TextMeshProUGUI)),
            btn        = cellGo:GetComponent(typeof(UI.Button)),
            uid        = 0,
            onClick    = nil,
        }
        cellTransformCache[cellGo] = cache

        -- 首次绑定点击事件（只绑定一次，之后复用闭包捕获的 cellGo）
        -- 保存成 cache.onClick：Shutdown 时需要用同一个函数引用 RemoveListener，
        -- 否则 LuaEnv.Dispose 会报 "try to dispose a LuaEnv with C# callback!"
        if cache.btn then
            cache.onClick = function()
                if ItemCellCtrl.onCellClick then
                    ItemCellCtrl.onCellClick(cellGo, cache.uid)
                end
            end
            cache.btn.onClick:AddListener(cache.onClick)
        end
    end

    cache.uid = slotData.uid or 0

    -- 图标：从 Resources 按 iconPath 加载（带缓存）
    -- 注意：icon 为空时也必须清空 sprite，否则格子复用时会把上一个物品的图标留下来。
    if cache.imgIcon then
        local sprite = nil
        if slotData.icon and slotData.icon ~= "" then
            sprite = LoadIcon(slotData.icon)
        end
        cache.imgIcon.sprite = sprite
        cache.imgIcon.gameObject:SetActive(sprite ~= nil)
    end

    -- 品质底色：作用于 imgQuality，常显（半透明品质色）
    if cache.imgQuality then
        local q = slotData.quality or 1
        local c = QUALITY_COLORS[q] or QUALITY_COLORS[1]
        cache.imgQuality.color = UnityEngine.Color(c.r, c.g, c.b, c.a)
        cache.imgQuality.gameObject:SetActive(true)
    end

    -- 数量文字
    if cache.txtCount and slotData.count then
        cache.txtCount.text = tostring(slotData.count)
        cache.txtCount.gameObject:SetActive(slotData.count > 1)
    end

    -- 选中高亮：颜色固定，显隐由 BagPanelCtrl 统一管理
    if cache.imgSelect then
        cache.imgSelect.color = UnityEngine.Color(SELECT_COLOR.r, SELECT_COLOR.g, SELECT_COLOR.b, SELECT_COLOR.a)
        cache.imgSelect.gameObject:SetActive(false)
    end
end
--打开 / 关闭选中高亮显示（imgSelect 显隐）
function ItemCellCtrl.SetSelected(cellGo, selected)
    local cache = cellTransformCache[cellGo]
    if cache and cache.imgSelect then
        cache.imgSelect.gameObject:SetActive(selected)
    end
end

-- 清空格子：槽位不存在 / 物品已被移除时调用，避免格子复用后残留上一件物品的显示
function ItemCellCtrl.Clear(cellGo)
    local cache = cellTransformCache[cellGo]
    if not cache then
        return
    end

    cache.uid = 0
    if cache.imgIcon then
        cache.imgIcon.sprite = nil
        cache.imgIcon.gameObject:SetActive(false)
    end
    if cache.imgQuality then
        cache.imgQuality.gameObject:SetActive(false)
    end
    if cache.txtCount then
        cache.txtCount.gameObject:SetActive(false)
    end
    if cache.imgSelect then
        cache.imgSelect.gameObject:SetActive(false)
    end
end

-- 移除所有格子的点击监听：LuaEnv.Dispose 前必须清掉，
-- 否则 xLua 会检测到仍有 C# 回调并拒绝 Dispose。
function ItemCellCtrl.Shutdown()
    for _, cache in pairs(cellTransformCache) do
        if cache.btn ~= nil and cache.onClick ~= nil then
            pcall(function()
                cache.btn.onClick:RemoveListener(cache.onClick)
            end)
            cache.onClick = nil
        end
    end
end

return ItemCellCtrl
