# XluaBag

基于 **Unity + XLua** 的背包系统客户端架构 Demo。

> 本项目以背包系统为载体，演示 **C# 稳定框架层 + Lua 可热更业务层** 的混合架构：C# 负责数据管理、资源加载、虚拟化渲染等稳定基础能力，Lua 负责 Tab 切换、筛选排序等高频迭代的业务逻辑。AB 资源包 + 事件系统支撑热更新与模块解耦。

---

## 分支状态：firsttime

`firsttime` 分支完成了背包 UI 的**可运行版本**：虚拟滚动网格 + Lua 业务控制器 + XLua 桥接，已跑通 Play Mode 验证。

### 本分支完成内容

| 阶段 | 内容 | 状态 |
|------|------|------|
| S1 | 预制体结构（BagPanel / BagItemCell / MainPanel） | ✅ |
| S2 | 数据层（`ItemDefinition` / `ItemInstance` / `ItemSlot` / `PlayerData` / `ConfigManager`） | ✅ 已跑通 |
| S3 | 数据管理层（`InventoryManager` / `InventoryEvent`，增删改查/堆叠/移动/筛选/排序/事件） | ✅ 已跑通 |
| S4 | 虚拟网格渲染（`BagGridLayout.cs` + `BagItemPool.cs`） | ✅ 已跑通 |
| S5 | Lua 业务层接入真实数据（`BagPanelCtrl.lua` 从 InventoryManager 读数据 + 真实排序 + uid 锚定） | ✅ 已跑通 |
| S6 | 格子交互（`ItemCellCtrl.lua` 绑定/选中高亮） | ✅ 已跑通 |
| — | XLua 桥接（`GenConfig.cs` + 生成的 Wrap 代码） | ✅ |
| — | 基础设施（`LuaMgr` / `ABMgr` / 单例基类 / `Main.cs`） | ✅ |
| — | UML 类图文档（`MD/项目UML类图.md`） | ✅ |

### 核心亮点：虚拟滚动网格

背包 500 个物品时，**不需要 500 个 GameObject**：

- 只实例化 `列数 × (可视行数 + 2 行缓冲)` ≈ **28 个格子**
- 滚动时按可视范围回收 / 复用格子，Canvas 重建复杂度 O(1)
- 相比 `GridLayoutGroup` 全量创建，性能提升一个量级

---

## 运行方式

1. 用 Unity **2021.3.34f1c1** 打开项目
2. 打开场景 `Assets/Scenes/SampleScene.unity`
3. 进入 Play Mode
4. 屏幕中央显示背包网格，默认选中 **Equip** 标签页
5. 可切换 **Equip / Item / Gem** 三个标签页查看过滤后的物品

---

## 目录结构

```
Assets/
├── Scripts/                            # C# 稳定框架层
│   ├── Main.cs                         # 入口：初始化 LuaEnv 并加载 Main.lua
│   ├── GenConfig.cs                    # XLua 代码生成配置
│   ├── ProjectBase/                    # 基础框架（单例 / AB / Lua 管理器）
│   └── UI/                             # 虚拟网格体系
│       ├── BagGridLayout.cs            # 虚拟化网格布局（挂在 Content 上）
│       └── BagItemPool.cs              # 格子对象池
│
├── Lua/                                # Lua 可热更业务层
│   ├── Main.lua                        # 入口
│   ├── Bag/
│   │   ├── BagPanelCtrl.lua            # 背包面板控制器（Tab 切换 / 数据过滤）
│   │   └── ItemCellCtrl.lua            # 格子 UI 控制器
│   └── Config/
│       └── ItemConfig.lua              # 物品配置表（当前为假数据）
│
├── ABRes/Prefabs/UI/                   # 可打 AB 包的预制体
│   ├── BagPanel.prefab                 # 背包面板
│   ├── BagItemCell.prefab              # 单个格子
│   └── MainPanel.prefab                # 主面板
│
├── ArtRes/UI/                          # 美术资源（图标 / 按钮 / 背景）
├── MD/                                 # 设计文档
├── Scenes/SampleScene.unity            # 当前场景
└── XLua/                               # XLua 框架源码 + 生成代码
```

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

---

## 设计文档

详细设计见 `Assets/MD/` 目录：

- [开发商业背包UI的方案(MVS)](Assets/MD/开发商业背包UI的方案(MVS).md) — 背包 UI 业务方案
- [搭背包格子方案](Assets/MD/搭背包格子方案.md) — 虚拟滚动 + 对象池方案
- [背包系统架构方案(简练级)](Assets/MD/背包系统架构方案(简练级).md) — 核心架构（S1–S7 实施步骤）
- [该项目的背包系统架构以及最终成果的初步预想](Assets/MD/该项目的背包系统架构以及最终成果的初步预想.md) — ARPG 远景规划
- [项目UML类图](Assets/MD/项目UML类图.md) — 代码结构图

---

## 下一步计划

| 阶段 | 内容 | 状态 |
|------|------|------|
| S6 扩展 | 右键菜单 / 拖拽 / Tooltip / 排序规则（SortLogic/FilterLogic） | ❌ 后续 |
| S7 | 联调 + 存档闭环 + 截图压测 | ❌ 未开始 |
