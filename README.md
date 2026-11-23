# XluaBag

基于 **Unity + XLua** 的背包系统客户端架构 Demo。

> 本项目以背包系统为载体，演示 **C# 稳定框架层 + Lua 可热更业务层** 的混合架构：C# 负责数据管理、资源加载、虚拟化渲染等稳定基础能力，Lua 负责 Tab 切换、筛选排序等高频迭代的业务逻辑。AB 资源包 + 事件系统支撑热更新与模块解耦。

---

## 进度状态：master（当前主线）

主分支已合并 `firsttime` 的开发成果，形成背包 UI 的**可运行版本**：虚拟滚动网格 + Lua 业务控制器 + XLua 桥接，已跑通 Play Mode 验证。

### 已合并进 master 的内容

| 阶段 | 内容 | 状态 |
|------|------|------|
| S1 | 预制体结构（BagPanel / BagItemCell / MainPanel） | ✅ |
| S2 | 数据层（`ItemDefinition` / `ItemInstance` / `ItemSlot` / `PlayerData` / `ConfigManager`） | ✅ 已跑通 |
| S3 | 数据管理层（`InventoryManager` / `InventoryEvent`，增删改查/堆叠/移动/筛选/排序/事件） | ✅ 已跑通 |
| S4 | 虚拟网格渲染（`BagGridLayout.cs` + `BagItemPool.cs`） | ✅ 已跑通 |
| S5 | Lua 业务层接入真实数据（`BagPanelCtrl.lua` 从 InventoryManager 读数据 + 真实排序 + uid 锚定） | ✅ 已跑通 |
| S6 | 格子交互（`ItemCellCtrl.lua` 绑定/选中高亮） | ✅ 已跑通 |
| S7 | 存档闭环（`PlayerData.Load/Save` + 退出自动存档 + uid 计数器修复） | ✅ 已跑通 |
| S7 | 性能压测（500 件物品 → 28 格，静止 0 重绑，Frame Debugger 27 events） | ✅ 已跑通 |
| Phase 0 | 刷新粒度与稳定性（单格刷新 / 事件退订 / pcall / 池复用） | ✅ 已跑通 |
| S8 | 热更新闭环（manifest 版本 + hash 差量 + 下载校验 + pending/confirm 回滚 + 发布工具） | ✅ 已跑通 |
| S9 | 性能观测与图集化（10 图标→1 图集 + PerfHUD；Item Tab 总数未降，原因：该页图标种类少） | ✅ 实现 / 对比待补 |
| — | Lua 一键打包工具（`Editor/LuaBundleTool.cs`，转换 + AB 标记 + 按平台构建） | ✅ 已跑通 |
| — | XLua 桥接（`GenConfig.cs` + 生成的 Wrap 代码） | ✅ |
| — | 基础设施（`LuaMgr` / `ABMgr` / 单例基类 / `Main.cs`） | ✅ |
| — | UML 类图文档（`MD/项目UML类图.md`） | ✅ |

### 核心亮点：虚拟滚动网格

背包 500 个物品时，**不需要 500 个 GameObject**：

- 只实例化 `列数 × (可视行数 + 2 行缓冲)` ≈ **28 个格子**
- 滚动时按可视范围回收 / 复用格子，重建量只跟可视格数相关，不随物品总数增长
- 相比 `GridLayoutGroup` 全量创建，性能提升一个量级

---

## 运行方式

**只想看背包功能**：

1. 用 Unity **2021.3.34f1c1** 打开项目
2. 打开场景 `Assets/Scenes/SampleScene.unity`
3. 进入 Play Mode
4. 屏幕中央显示背包网格，默认选中 **Equip** 标签页
5. 可切换 **Equip / Item / Gem** 三个标签页查看过滤后的物品

**看热更新流程**：

1. `cd HotUpdateServer` → `python dev_server.py --port 8000`
2. 打开 `Assets/Scenes/LaunchScene.unity` → Play
3. 启动场景会检查 `http://127.0.0.1:8000/PC/manifest.json`，有更新则下载后进入背包；没有则直接使用本地基础包

---

## 目录结构

```
Assets/
├── Scripts/                            # C# 稳定框架层
│   ├── Main.cs                         # 入口：初始化 LuaEnv 并加载 Main.lua
│   ├── GenConfig.cs                    # XLua 代码生成配置
│   ├── Data/                           # 数据模型 + 配置（ItemDefinition/ItemInstance/ItemSlot/PlayerData/ConfigManager）
│   ├── Inventory/                      # 数据管理层（InventoryManager / InventoryEvent）
│   ├── ProjectBase/                    # 基础框架（单例 / AB / Lua 管理器）
│   ├── HotUpdate/                      # 热更新（版本/差量/下载/校验/回滚/启动器）
│   ├── Perf/                           # PerfHUD（FPS/帧时间/GC/DrawCall/Batches）
│   └── UI/                             # 虚拟网格体系
│       ├── BagGridLayout.cs            # 虚拟化网格布局（挂在 Content 上）
│       └── BagItemPool.cs              # 格子对象池
│
├── Lua/                                # Lua 可热更业务层
│   ├── Main.lua                        # 入口
│   ├── Bag/
│   │   ├── BagPanelCtrl.lua            # 背包面板控制器（Tab 切换 / 数据过滤 / 排序）
│   │   └── ItemCellCtrl.lua            # 格子 UI 控制器
│   └── Config/
│       └── ItemConfig.lua              # 物品配置表（当前为假数据）
│
├── Editor/
│   ├── LuaBundleTool.cs                # Lua → AB 一键转换/打包工具（仅编辑器）
│   ├── HotUpdate/                      # 热更新发布工具 + 启动场景创建
│   └── Perf/                           # 图标图集化 / PerfHUD 场景准备
│
├── ABRes/
│   ├── Prefabs/UI/                     # 可打 AB 包的预制体
│   │   ├── BagPanel.prefab             # 背包面板
│   │   ├── BagItemCell.prefab          # 单个格子
│   │   └── MainPanel.prefab            # 主导航面板
│   ├── Lua/                            # 工具生成的 .lua.txt（AB 名 "lua"）
│   └── HotUpdate/Banner.txt            # 公告资源（AB 名 "banner"）
│
├── StreamingAssets/{PC|Android|IOS}/   # 基础包（PC / lua / banner + version.json）
├── ArtRes/UI/ItemIcons/                # 物品图标源文件（已移出 Resources，供图集引用）
├── Resources/ItemIconAtlas.spriteatlas # 运行时图集（10 张图标合并）
├── MD/                                 # 设计文档（方案 / UML / 压测 / 热更）
├── Resources/ItemIcons/               # 运行时加载的物品图标
├── Scenes/
│   ├── LaunchScene.unity               # 热更新启动场景
│   └── SampleScene.unity               # 背包场景
└── XLua/                               # XLua 框架源码 + 生成代码
```

项目根目录 `HotUpdateServer/`：本地 CDN（`manifest.json`、`versions/{n}`、`dev_server.py`）。

---

## 架构说明

### 职责划分

| 层 | 技术 | 职责 |
|----|------|------|
| UI 渲染层 | Unity uGUI | 只负责显示和状态 |
| Lua 业务层 | Lua | 高频迭代：Tab 切换、筛选、排序、交互 |
| C# 框架层 | C# | 稳定基础：数据管理、资源加载、事件系统 |
| Unity 底层 | — | 渲染、输入、资源 |

### C# ↔ Lua 调用关系

```
Lua: BagPanelCtrl.OnTabChanged("Equip")
  → 过滤 ItemConfig
  → grid:Refresh(count)                    (Lua → C#)
    → C#: BagGridLayout.Refresh
      → OnScroll → pool 回收/复用格子
      → onCellBind(index, cellGo)          (C# → Lua)
        → Lua: ItemCellCtrl.Bind → 设图标/数量
```

### 存档闭环（S7）

数据格式为 `JsonUtility` 序列化的 `PlayerData`（金币 + 120 个槽位），存放在 `PlayerPrefs` 的 `PlayerData` 键下。

```
启动：Main.cs Start
  → PlayerData.Load()
      ├─ 无存档 / 存档损坏 → 按新号处理（loadedFromSave = false）
      └─ 有存档 → 还原槽位 → 修正 uid 计数器（maxUid + 1）→ loadedFromSave = true
  → 执行 Main.lua
      └─ loadedFromSave == true ? 跳过测试物品 : 初始化 120 格并添加测试物品

退出：OnApplicationQuit → PlayerData.Save()
```

两个关键点：

- **uid 计数器修复**：`ItemInstance.uidCounter` 是静态变量，每次运行从 1 开始；读档后必须抬到「存档最大 uid + 1」，否则新物品会复用旧 uid，导致点选/反查槽位错乱。
- **测试数据只加一次**：通过 `loadedFromSave` 区分「新号」与「读档」，避免每次进游戏都在存档基础上重复叠加。

验证方式：Play → 停止（Console 出现「存档完成」）→ 再 Play（出现「读档完成」且不出现「测试数据添加完成」），物品数量与上一局一致。

### 刷新粒度与稳定性（Phase 0）

- **单格刷新**：同一槽位、同一 uid、只有数量变化时只重绑该格子；新增 / 移除 / 换 Tab 才全量刷新 + 排序。
- **池子复用**：`BagItemPool.EnsureSize` 在容量不变时只回收，不再每次刷新 28 次 `Instantiate` + 28 次 `Destroy`。
- **事件生命周期**：`BagPanelCtrl.Shutdown` 退订 `InventoryEvent`、Toggle 与格子按钮监听；`LuaMgr.Dispose` 先执行 Shutdown 再销毁 LuaEnv。
- **容错**：事件、格子绑定、点击回调均用 `pcall` 包裹，Lua 报错不会中断 C# 的 `AddItem` / `RemoveItem`。

实测数据与验证步骤见 [Phase0稳定性修正记录](Assets/MD/Phase0稳定性修正记录.md)。

### Lua 一键打包（AB）

菜单 `Tools/Lua AB/` 提供三个入口：

| 菜单 | 作用 |
|---|---|
| 一键转换并打包 | 转换 + 设置 AB 名 + 构建到 `StreamingAssets/{PC\|Android\|IOS}` |
| 仅转换并设置 AB | 只生成 `.lua.txt` 并打标签（配合 AssetBundleBrowser 手动打包） |
| 清理生成的 Lua 资源 | 删除 `Assets/ABRes/Lua` |

转换规则：

```
Assets/Lua/Bag/BagPanelCtrl.lua
  → Assets/ABRes/Lua/Bag/BagPanelCtrl.lua.txt   （UTF-8 无 BOM，assetBundleName = "lua"）
  → 运行时按 "Assets/ABRes/Lua/Bag/BagPanelCtrl.lua.txt" 精确加载
```

为什么必须转 `.lua.txt`：项目里的 `.lua` 是 `DefaultImporter`，Unity 不会把它当资源导入，直接设置 AB 名也打不进包；复制成 `.txt` 后按 TextAsset 处理，才能进 AB。

加载顺序：`LuaMgr` 先注册本地文件 Loader，再注册 AB Loader —— **编辑器开发时走本地，打包后（或本地目录被移除）自动走 AB**。

验证热更：

1. 运行 `Tools/Lua AB/一键转换并打包`
2. 把 `Assets/Lua` 临时改名 → Play：Console 出现 `[LuaMgr] 从 AB 加载 Lua: ...`，背包正常
3. 改任意 Lua → 重新打包 → 再 Play：新逻辑生效，无需重新编译 C#

### 热更新闭环（S8）

启动流程：

```
LaunchScene / HotUpdateLauncher
  → 读 state.json + StreamingAssets/PC/version.json
  → GET /PC/manifest.json
  → 按 md5 差量：只下载变化的 PC / lua / banner
  → Res/{新版}.tmp（Range 续传 + 重试 + MD5 校验）
  → 改名为 Res/{新版} → 写 state（current/pending）
  → 切换 ABMgr.BundleRoot → 加载 SampleScene
  → Main.lua 成功后 confirm；下次启动 pending != confirmed 则回滚
```

发布：

| 菜单 | 作用 |
|---|---|
| `Tools/HotUpdate/1. 生成基础包 v1` | 构建基础包到 `StreamingAssets/PC`，写 `version.json` 和 `Server/PC/versions/1` |
| `Tools/HotUpdate/2. 发布新版本` | 版本号 +1，构建到 `Server/PC/versions/{n}`，更新 `manifest.json` |
| `Tools/HotUpdate/3. 清理本地缓存` | 删除 `persistentDataPath/XluaBag`，模拟新安装 |
| `Tools/HotUpdate/5. 创建启动场景` | 创建 `LaunchScene` 并加入 Build Settings |

本地服务：`cd HotUpdateServer && python dev_server.py --port 8000`（支持 Range/206）。

实测：v1 → v2 只下载变化的 `PC` / `lua` / `banner`；v2 的 Lua 标记、`ItemConfig` 物品名和公告文本都生效；模拟启动未确认后能自动回滚到 v1，`Res/2` 被清理。详见 [S8 热更新闭环记录](Assets/MD/S8热更新闭环记录.md)。

---

## 设计文档

详细设计见 `Assets/MD/` 目录：

- [开发商业背包UI的方案(MVS)](Assets/MD/开发商业背包UI的方案(MVS).md) — 背包 UI 业务方案
- [搭背包格子方案](Assets/MD/搭背包格子方案.md) — 虚拟滚动 + 对象池方案
- [背包系统架构方案(简历级)](Assets/MD/背包系统架构方案(简历级).md) — 核心架构（S1–S7 实施步骤）
- [该项目的背包系统架构以及最终成果的初步预想](Assets/MD/该项目的背包系统架构以及最终成果的初步预想.md) — ARPG 远景规划
- [项目UML类图](Assets/MD/项目UML类图.md) — 代码结构图
- [S7 性能压测记录](Assets/MD/S7性能压测记录.md) — 500 物品 / 虚拟滚动实测数据
- [Phase0 稳定性修正记录](Assets/MD/Phase0稳定性修正记录.md) — 单格刷新 / 事件退订 / 池复用实测
- [S8 热更新闭环记录](Assets/MD/S8热更新闭环记录.md) — 版本 / 差量 / 校验 / 回滚实测
- [S9 性能观测与图集化记录](Assets/MD/S9性能观测与图集化记录.md) — SpriteAtlas / PerfHUD 实测与结论

---

## 下一步计划

| 阶段 | 内容 | 状态 |
|------|------|------|
| S6 扩展 | 右键菜单 / 拖拽 / Tooltip / 排序规则（SortLogic/FilterLogic） | ❌ 后续 |
| S9 补测 | 装备页/宝石页或独立包 + Frame Debugger 做图集前后对比；面板图集化 | ❌ 后续 |
| 优化 | 批量 AddItem 合并刷新（压测时发现的事件风暴） | ❌ 后续 |