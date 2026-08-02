# 项目 UML 类图

> 本文档描述 XluaBag 项目的代码结构和模块关系，涵盖 C# 类图、Lua 模块依赖、场景挂载和数据流。

---

## 一、包结构总览

```
Assets/
├── Scripts/                            # C# 脚本
│   ├── Main.cs                         # 入口：Start 中初始化 LuaEnv
│   ├── GenConfig.cs                    # XLua 代码生成配置
│   ├── ProjectBase/
│   │   ├── ABMgr.cs                    # AssetBundle 管理器
│   │   ├── LuaMgr.cs                   # Lua 解析器管理器（加载器 + 执行）
│   │   └── Base/
│   │       ├── BaseManager.cs          # 纯 C# 单例基类
│   │       ├── SingletonMono.cs        # Mono 单例基类（手动挂载）
│   │       └── SingletonAutoMono.cs    # Mono 单例基类（自动创建）
│   └── UI/
│       ├── BagGridLayout.cs            # 虚拟化网格布局
│       └── BagItemPool.cs              # 格子对象池
│
├── Lua/                                # Lua 业务逻辑
│   ├── Main.lua                        # 入口：初始化各模块
│   ├── Object.lua                      # 对象工具
│   ├── JsonUtility.lua                 # JSON 工具
│   ├── SplitTools.lua                  # 分割工具
│   ├── Bag/
│   │   ├── BagPanelCtrl.lua            # 背包面板控制器
│   │   └── ItemCellCtrl.lua            # 格子控制器
│   └── Config/
│       └── ItemConfig.lua              # 物品配置表（假数据）
│
├── ABRes/                              # AB 资源包
│   └── Prefabs/UI/
│       ├── BagPanel.prefab             # 背包面板
│       ├── BagItemCell.prefab          # 单个格子
│       └── MainPanel.prefab            # 主面板
│
├── ArtRes/                             # 美术资源
│   └── UI/                             # UI 图标、背景等
│
├── MD/                                 # 设计文档
│   ├── 开发商业背包UI的方案(MVS).md
│   ├── 搭背包格子方案.md
│   ├── 背包系统架构方案(简练级).md
│   └── 该项目的背包系统架构以及最终成果的初步预想.md
│
├── Scenes/
│   └── SampleScene.unity               # 当前场景
│
└── XLua/                               # XLua 框架
    ├── Src/                            # XLua 源码
    ├── Gen/                            # XLua 生成代码
    └── Examples/                       # XLua 示例
```

---

## 二、C# 类图

### 2.1 单例基类体系

```
┌─────────────────────────────────────┐
│         BaseManager<T>              │  ← 纯 C# 单例，不需要挂载
│  └ static T instance                │
│  └ static T GetInstance()           │
└──────────┬──────────────────────────┘
           │ 继承
┌──────────┴──────────────────────────┐
│           LuaMgr                    │  ← Lua 解析器管理器
│  └ LuaEnv luaEnv                    │
│  └ Init()                           │
│  └ DoLuaFile(fileName)             │
│  └ DoString(luaScript)             │
│  └ MyCustomLoader(ref string)      │  ← 从 Assets/Lua/ 加载 .lua
│  └ MyCustomLoaderFormAB(ref string)│  ← 从 AB 包加载 .lua
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│      SingletonAutoMono<T>           │  ← Mono 单例，自动创建 GameObject
│  └ static T GetInstance()           │
│  └ DontDestroyOnLoad                │
└──────────┬──────────────────────────┘
           │ 继承
┌──────────┴──────────────────────────┐
│             ABMgr                   │  ← AssetBundle 管理器
│  └ LoadAB(abName)                  │
│  └ LoadRes<T>(abName, resName)    │  ← 泛型资源加载
│  └ LoadResAsync(...)               │
│  └ UnLoad / ClearAB                │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│         SingletonMono<T>            │  ← Mono 单例，需手动挂载
│  └ static T GetInstance()           │
│  └ Awake 中赋值 instance             │
└─────────────────────────────────────┘
```

### 2.2 主入口

```
┌─────────────────────────────────────┐
│          Main : MonoBehaviour       │  ← 挂在 Main Camera 上
│  └ Start()                          │
│      └ LuaMgr.GetInstance().Init()  │
│      └ LuaMgr.GetInstance()         │
│          .DoLuaFile("Main")         │  ← 执行 Main.lua
└─────────────────────────────────────┘
```

### 2.3 虚拟网格体系

```
┌─────────────────────────────────────┐
│   BagGridLayout : MonoBehaviour     │  ← 挂在 Content 上
│  ┌─────────────────────────────────┐│
│  │ 序列化字段                       ││
│  │  └ int colCount = 4            ││
│  │  └ Vector2 cellSize = (176,176)││
│  │  └ Vector2 spacing = (8,8)     ││
│  │  └ RectOffset padding           ││
│  │  └ BagItemPool pool          ───┼──→ 引用 BagItemPool
│  │                                 ││
│  │ 运行时字段                       ││
│  │  └ ScrollRect scrollRect        ││
│  │  └ int itemCount / totalRows    ││
│  │  └ float cellStepY              ││
│  │                                 ││
│  │ 公开方法                         ││
│  │  └ Refresh(int count)           ││  ← Lua 调用入口
│  │                                 ││
│  │ 回调                             ││
│  │  └ onCellBind(int, GameObject)  ││  ← Lua 赋值
│  │                                 ││
│  │ 私有方法                         ││
│  │  └ Awake() — 初始化引用         ││
│  │  └ OnScroll() — 计算可视行      ││
│  │  └ PositionCell() — 定位格子    ││
│  └─────────────────────────────────┘│
└────────┬────────────────────────────┘
         │ 组合（同一个 Content 节点上）
┌────────┴────────────────────────────┐
│   BagItemPool : MonoBehaviour       │  ← 挂在 Content 上
│  ┌─────────────────────────────────┐│
│  │ 序列化字段                       ││
│  │  └ GameObject cellPrefab      ──┼──→ BagItemCell.prefab
│  │                                 ││
│  │ 运行时字段                       ││
│  │  └ List<GameObject> allCells    ││  ← 所有池中格子
│  │  └ Queue<GameObject> freeCells  ││  ← 空闲格子队列
│  │                                 ││
│  │ 公开方法                         ││
│  │  └ Init(int size)               ││  ← 创建 N 个格子
│  │  └ Get() → GameObject           ││  ← 取一个激活
│  │  └ RecycleAll()                 ││  ← 全部回收
│  └─────────────────────────────────┘│
└─────────────────────────────────────┘
```

### 2.4 场景对象树与组件挂载

```
SampleScene
├── Main Camera
│     ├── Camera
│     ├── AudioListener
│     └── Main.cs                        ← 入口脚本
│
├── Directional Light
│     └── Light
│
├── EventSystem
│     ├── EventSystem
│     └── StandaloneInputModule
│
└── Canvas (1920×1080, ScaleWithScreenSize)
      └── BagPanel (ToggleGroup)
            ├── bk                     ← 背景
            │     └── Image
            ├── bk2                    ← 背景2（装备槽区域）
            │     └── Image
            ├── svBag (ScrollRect)
            │     ├── scrollSensitivity: 25
            │     ├── movementType: Elastic
            │     ├── inertia: true
            │     │
            │     └── Viewport (Mask)
            │           └── Content
            │                 ├── BagGridLayout    ← 虚拟网格计算
            │                 ├── BagItemPool      ← 格子对象池
            │                 ├── BagItemCell  × N ← 池子实例化的格子
            │                 └── ...更多格子
            │
            ├── TabEquip (Toggle)       ← 装备标签
            ├── TabItem  (Toggle)       ← 物品标签
            ├── TabGem   (Toggle)       ← 宝石标签
            └── btnClose (Button)       ← 关闭按钮
```

### 2.5 BagItemCell 预制体结构

```
BagItemCell (根节点)
├── RectTransform: (176×176)
├── Image: 背景图（Sliced）
├── Button: Transition=None
│
├── imgIcon (子节点)
│     ├── RectTransform: (130×130)，居中
│     └── Image: 物品图标
│
├── imgSelect (子节点)
│     ├── RectTransform: Fill 父节点
│     ├── Image: 选中高亮框
│     └── 默认 SetActive(false)
│
└── txtCount (子节点)
      ├── RectTransform: 右下角
      └── TextMeshProUGUI: 数量文字
            └── 默认 SetActive(count > 1)
```

---

## 三、Lua 模块依赖图

```
┌─────────────┐
│  Main.lua   │  ← 入口：被 C# Main.cs 通过 require("Main") 加载
└──────┬──────┘
       │ require
       ▼
┌──────────────────────┐
│ BagPanelCtrl.lua     │  ← 背包面板主控制器
│                      │
│ 外部接口：            │
│  └ Init()            │  ← 获取 Grid 引用，绑定回调，设默认 Tab
│  └ OnTabChanged()    │  ← 过滤数据 → grid:Refresh()
│  └ OnCellBind()      │  ← 格子绑定回调 → ItemCellCtrl.Bind()
│                      │
│ 内部状态：            │
│  └ grid              │  ← BagGridLayout 的 Lua 引用
│  └ filteredItems[]   │  ← 当前 Tab 的过滤后数据
└──┬───────┬───────────┘
   │       │
   │require│require
   ▼       ▼
┌─────────────────┐ ┌──────────────────┐
│ Config/         │ │ Bag/             │
│ ItemConfig.lua  │ │ ItemCellCtrl.lua │
│                 │ │                  │
│ 纯数据表：       │ │ 格子 UI 控制器：  │
│  { id, name,   │ │  └ Bind()        │
│    type,        │ │    → 设图标/数量  │
│    quality,     │ │  └ SetSelected() │
│    count }      │ │    → 高亮切换    │
│  x 59 条       │ │                  │
└─────────────────┘ └──────────────────┘
```

---

## 四、数据流图

### 4.1 启动流程

```
MainCamera.Start()
  │
  ▼
LuaMgr.GetInstance().Init()         ← 创建 LuaEnv
  │
  ▼
LuaMgr.GetInstance().DoLuaFile("Main")
  │
  ▼
require("Main")                     ← 自定义加载器读取 Assets/Lua/Main.lua
  │
  ▼
require("Bag/BagPanelCtrl")
  ├── require("Config/ItemConfig")  ← 加载假数据
  └── require("Bag/ItemCellCtrl")   ← 加载格子控制器
  │
  ▼
BagPanelCtrl.Init()
  ├── GameObject.Find("Content")    ← 获取 Content
  ├── grid:GetComponent(BagGridLayout)
  ├── grid.onCellBind = OnCellBind  ← 绑定 Lua 回调
  ├── 绑定三个 Tab Toggle 事件
  └── 默认选中 TabEquip
        │
        ▼
  OnTabChanged("Equipment")
        │
        ▼
  过滤 ItemConfig(type="Equipment") → filteredItems[20]
        │
        ▼
  grid:Refresh(20)
```

### 4.2 网格刷新流程

```
Lua: BagPanelCtrl
  │
  │ grid:Refresh(count)
  ▼
C#: BagGridLayout.Refresh(int count)
  ├── 计算 totalRows = ceil(20/4) = 5
  ├── Content高度 = 5×184 + 24 = 944
  ├── 池子大小 = 4×(5+2) = 28
  ├── pool.Init(28)                   ← 创建 28 个 BagItemCell
  └── OnScroll(normalizedPosition)
        │
        ▼
C#: BagGridLayout.OnScroll()
  ├── 计算首行: floor(|offset|/184)
  ├── 计算末行: 首行 + ceil(viewH+176/184)
  ├── 上下各 +1 行缓冲
  ├── pool.RecycleAll()              ← 隐藏旧格子
  └── for 每个可见索引:
        ├── pool.Get()               ← 取空闲格子，激活
        ├── PositionCell(index)      ← 定位到正确坐标
        └── onCellBind(index, cell)  ← 回调 Lua
              │
              ▼
Lua: BagPanelCtrl.OnCellBind(index, cellGo)
  └── data = filteredItems[index+1]
        │
        ▼
Lua: ItemCellCtrl.Bind(cellGo, slotData)
  ├── 设 imgIcon.sprite              ← 图标
  ├── 设 txtCount.text               ← 数量
  └── 隐藏 imgSelect                  ← 清选中
```

### 4.3 Tab 切换流程（S5 改造后：真实数据 + 真实排序）

```
用户点击 TabItem
  │
  ▼
Toggle.onValueChanged(true)
  │
  ▼
Lua: BagPanelCtrl.OnTabChanged("Item")
  ├── InventoryManager.Sort()          ← C# 真实重排槽位（品质降序）
  ├── 遍历 inventorySlots，筛选 type=="Consumable"
  ├── 建立 displayToUid 映射（显示位置 → 物品 uid）
  └── grid:Refresh(#displayToUid)
        │
        ▼
  C#: BagGridLayout.Refresh(count)
      └── 同 4.2 网格刷新流程
```

### 4.4 回调关系总表（S5 改造后）

| 调用方 | 被调用方 | 方式 | 方向 |
|--------|---------|------|------|
| `Main.cs` (C#) | `ConfigManager` (C#) | 静态方法 `LoadItemConfig` | C# → C# |
| `Main.cs` (C#) | `Main.lua` (Lua) | `DoLuaFile -> require` | C# → Lua |
| `BagPanelCtrl.Init` (Lua) | `BagGridLayout.cs` (C#) | `GetComponent` 赋值 `onCellBind` | Lua → C# |
| `BagPanelCtrl.Refresh` (Lua) | `InventoryManager.Sort` (C#) | `inventoryMgr:Sort()` | Lua → C# |
| `BagPanelCtrl.OnTabChanged` (Lua) | `BagGridLayout.Refresh` (C#) | `grid:Refresh(count)` | Lua → C# |
| `BagGridLayout.OnScroll` (C#) | `BagPanelCtrl.OnCellBind` (Lua) | `onCellBind?.Invoke` | C# → Lua |
| `BagPanelCtrl.OnCellBind` (Lua) | `InventoryManager.GetSlotIndexByUid` (C#) | uid 反查真实槽位 | Lua → C# |
| `InventoryManager` (C#) | `InventoryEvent.OnSlotChanged` (C#) | 事件广播 | C# → C# |
| `InventoryEvent.OnSlotChanged` (C#) | `BagPanelCtrl.Refresh` (Lua) | 事件订阅（XLua "+" 语法） | C# → Lua |

---

## 五、XLua 桥接配置

```
GenConfig.cs
├── [LuaCallCSharp]                   ← 允许 Lua 访问以下 C# 类型
│     ├── ScrollRect
│     ├── Toggle / ToggleGroup
│     ├── Image / Button
│     ├── TextMeshProUGUI
│     ├── BagGridLayout
│     ├── BagItemPool
│     ├── InventoryManager
│     ├── PlayerData
│     ├── ItemDefinition / ItemInstance / ItemSlot
│     └── InventoryEvent
│
└── [CSharpCallLua]                   ← 允许 C# 调用以下 Lua 委托类型
      ├── Action<int, GameObject>    ← onCellBind 回调
      ├── Action<SlotChangeData>     ← OnSlotChanged 事件订阅
      ├── Action<int, int>           ← OnSelectionChanged 事件订阅
      ├── Action<int>                ← 预留
      └── Action                     ← 预留
```

---

## 六、开发阶段对照

```
S1 ✅ 预制体结构 (BagItemCell / BagPanel / MainPanel)
S2 ✅ 数据层 (ItemDefinition / ItemInstance / ItemSlot / PlayerData / ConfigManager)
S3 ✅ 数据管理层 (InventoryManager / InventoryEvent)
S4 ✅ 虚拟网格渲染 (BagGridLayout / BagItemPool)
S5 ✅ Lua 业务层 (BagPanelCtrl 接入真实数据 + 真实排序)
S6 ✅ 格子交互 (ItemCellCtrl)
S7 ❌ 联调 + 存档闭环
```

---

## 七、S5 关键设计：uid 锚定

```
显示位置 → 物品 uid → 反查真实槽位

原因：排序后显示顺序 != 真实槽位顺序，
      若用"显示索引 → 槽位索引"映射，数据变化后映射过期会操作错对象。
      uid 是物品身份（全局唯一），永远指向同一个物品，交互安全。

实现：
  BagPanelCtrl.displayToUid[显示位置] = slot.instance.uid
  OnCellBind: uid → InventoryManager.GetSlotIndexByUid(uid) → 真实槽位
```
