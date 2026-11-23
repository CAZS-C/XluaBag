# Phase 0 稳定性与刷新粒度修正记录

> 背景：模拟面试复盘指出「事件按单格粒度」只是数据层设计，UI 端实际是全量刷新；
> 同时还有事件不退订、Lua 无 pcall、图标复用残留、池子每次重建等问题。
> 本轮（Phase 0）只修这些“让简历措辞站得住”的问题，不引入热更/图集等新功能。

---

## 一、改动清单

| 文件 | 改动 |
|---|---|
| `Assets/Lua/Bag/BagPanelCtrl.lua` | 单格刷新、`displayToSlot` / `slotToDisplay` 映射、滚动回收回调、具名事件回调 + `Shutdown` 退订、`pcall` 包裹三处回调、`Refresh(needSort)` 排序策略 |
| `Assets/Lua/Bag/ItemCellCtrl.lua` | 图标为空时清空 sprite；新增 `Clear`；点击监听保存为 `cache.onClick` 并支持 `Shutdown` 移除 |
| `Assets/Scripts/UI/BagGridLayout.cs` | 新增 `onCellRecycled` 回调；`pool.Init` → `pool.EnsureSize`；补充池子空位注释 |
| `Assets/Scripts/UI/BagItemPool.cs` | 新增 `EnsureSize`：容量不变只回收，不销毁/不实例化 |
| `Assets/Scripts/Inventory/InventoryManager.cs` | `Sort` 不再逐槽发事件；配置缺失按 quality=0 排在最后，避免 NRE 与物品丢失 |
| `Assets/Scripts/Data/PlayerData.cs` | 新增 `OnApplicationPause(true)` 存档，降低移动端被杀进程丢档风险 |
| `Assets/Scripts/ProjectBase/LuaMgr.cs` | `DoLuaFile` 返回 bool + try/catch；`Dispose` 前调用 Lua 侧 `Shutdown`；`Dispose` 空引用保护 |
| `Assets/Scripts/Main.cs` | 检查 `DoLuaFile` 结果；`OnApplicationQuit` 调 `LuaMgr.Dispose` |
| `Assets/XLua/Gen/BagGridLayoutWrap.cs`、`BagItemPoolWrap.cs` | 重新生成（新增字段/方法） |

---

## 二、实测数据

测试环境：Unity 2021.3.34f1c1，编辑器 Play Mode，默认 67 件存档。

| 场景 | 结果 |
|---|---|
| 消耗品数量 -1（5 → 4，堆叠未清空） | **全量 Refresh = 0，Bind = 1**（只重绑该格） |
| 把整堆消耗品移除（4 → 0，槽位清空） | 全量 Refresh = 1，Bind = 21，列表 22 → 21 |
| 新增一件当前 Tab 物品 | 全量 Refresh = 1，Bind = 22，列表 21 → 22 |
| 新增物品时 Content 子物体数 | **28 → 28**（旧实现会短暂出现 56 个待销毁对象） |
| 从底部（有图标）滚回顶部（图标为空） | 顶部 4 格 `sprite=nil`、`imgIcon.active=false`，无残留图标 |
| 选中装备 → 切宝石 → 切回装备 | `selectedUid=24` 保持，高亮 1 → 0 → 1 |
| 事件订阅者数量（Dispose 前/后） | **1 → 0** |
| `LuaMgr.Dispose` | 输出 `[LuaMgr] LuaEnv 已释放`，无 “try to dispose a LuaEnv with C# callback!” |
| 故意让 `ItemCellCtrl.Bind` 报错后移除物品 | 控制台打印 `[BagPanelCtrl] OnSlotChanged error`，`RemoveItem` 仍返回 true，数据正常 |
| 调用 `OnApplicationPause(true)` | 输出 `[PlayerData] 存档完成，非空格子 67` |
| **AB-only（把 `Assets/Lua` 改名后启动）** | **4 条 `[LuaMgr] 从 AB 加载 Lua`（Config/ItemConfig、Main、Bag/BagPanelCtrl、Bag/ItemCellCtrl）；本地 Loader 全部失败；67 件 / 装备 30、消耗品 22、宝石 15 / 28 格；停止时 `[LuaMgr] LuaEnv 已释放`** |

---

## 三、关键设计说明

### 1. 单格刷新的适用条件

只有「同一槽位、同一 uid、只有 count 变化」才走单格重绑：

```
OnSlotChanged(data)
  ├─ 该槽位在当前 Tab 显示？
  │    ├─ 是 + uid 不变 → 只 Bind 该格（若在屏幕上）
  │    ├─ 是 + uid 变 / 物品消失 → 列表成员变化 → 全量刷新 + 排序
  │    └─ 否 + 新增的是当前 Tab 物品 → 全量刷新 + 排序
  └─ 其他 → no-op
```

新增/移除/换 Tab 仍然全量刷新，这是有意保留的：顺序/列表长度变了，必须重建映射。

### 2. 排序策略

- 切 Tab：排序一次。
- 列表成员变化（新增/移除/替换）：排序一次。
- 纯数量变化：不排序、不碰网格。

`InventoryManager.Sort` 不再逐槽位发 `OnSlotChanged`，避免一次排序触发几十次无效回调。

### 3. 事件生命周期

`BagPanelCtrl` 现在保存三类别 C# 回调的引用，并在 `Shutdown` 中清理：

- `InventoryEvent.OnSlotChanged`（静态事件，用 `"-"` 退订）
- `BagGridLayout.onCellBind` / `onCellRecycled`（置 nil）
- `Toggle.onValueChanged` 与格子 `Button.onClick`（`RemoveListener`，否则 UnityEvent 会一直持有 DelegateBridge）

`LuaMgr.Dispose` 先执行 Lua 侧 `Shutdown`，再销毁 `LuaEnv`。

---

## 四、尚未解决（后续阶段）

- 热更只有“打包 + 加载”，没有版本/下载/校验/回滚 → S8。
- Android 上 `AssetBundle.LoadFromFile` 读 StreamingAssets 仍不成立 → S8 首次启动拷贝 + 真机验证。
- 图标仍走 `Resources`，没有图集/DrawCall 对比 → S9。
- 500 次批量 `AddItem` 仍会触发 500 次结构刷新 → 后续“批量接口 + 帧末合并刷新”。
- 简历里 “Canvas 重建 O(n)→O(1)” 仍偏夸大 → 改为“常驻 28 格，重建量与屏幕容量相关”。
