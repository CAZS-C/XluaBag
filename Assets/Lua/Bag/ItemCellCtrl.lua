-- ItemCellCtrl.lua
-- 格子控制器模块：管理单个格子的 UI 刷新和交互状态。
--
-- 职责：
--   1. Bind(cellGo, slotData)：缓存 Transform 引用，设置图标、数量、清选中。
--   2. SetSelected(cellGo, selected)：切换选中高亮（imgSelect 显隐）。
--
-- 被 BagPanelCtrl 调用，不直接挂载到任何 GameObject。
-- cellTransformCache 使用弱表（__mode = "k"），cellGo 销毁时自动释放缓存。
local UnityEngine = CS.UnityEngine
local UI = CS.UnityEngine.UI
local TMP = CS.TMPro

local ItemCellCtrl = {}

local cellTransformCache = setmetatable({}, { __mode = "k" })

function ItemCellCtrl.Bind(cellGo, slotData)
    local cache = cellTransformCache[cellGo]
    if not cache then
        local trans = cellGo.transform
        cache = {
            imgIcon   = trans:Find("imgIcon"):GetComponent(typeof(UI.Image)),
            imgSelect = trans:Find("imgSelect"):GetComponent(typeof(UI.Image)),
            txtCount  = trans:Find("txtCount"):GetComponent(typeof(TMP.TextMeshProUGUI)),
        }
        cellTransformCache[cellGo] = cache
    end

    if slotData.icon and cache.imgIcon then
        cache.imgIcon.sprite = slotData.icon
    end

    if cache.txtCount and slotData.count then
        cache.txtCount.text = tostring(slotData.count)
        cache.txtCount.gameObject:SetActive(slotData.count > 1)
    end

    cache.imgSelect.gameObject:SetActive(false)
end

function ItemCellCtrl.SetSelected(cellGo, selected)
    local cache = cellTransformCache[cellGo]
    if cache and cache.imgSelect then
        cache.imgSelect.gameObject:SetActive(selected)
    end
end

return ItemCellCtrl
