using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家数据 — 持有金币、背包槽位等运行时数据，并负责存档/读档。
///
/// 设计要点：
///   1. 继承 SingletonMono，需在场景中挂载到一个 GameObject 上（生命周期随场景）。
///   2. 存档使用 JsonUtility + PlayerPrefs，形成"取物品 → 进背包 → 存档 → 重启 → 读档还原"闭环。
///   3. 存档序列化的是 ItemSlot → ItemInstance，其中的 defId 会在加载后由
///      ItemDefinition 注册表解析回模板，不重复存储模板内容。
/// </summary>
public class PlayerData : SingletonMono<PlayerData>
{
    /// <summary>
    /// 玩家金币
    /// </summary>
    public int gold;

    /// <summary>演示版生命值；使用药水后由 DemoPlayerStats 写回并随存档保存</summary>
    public int hp = 60;

    /// <summary>
    /// 背包槽位列表（真实数据的最终载体，替代 Lua 中的假数据）
    /// </summary>
    public List<ItemSlot> inventorySlots = new List<ItemSlot>();

    /// <summary>
    /// 当前装备的物品 uid（0 = 未装备）。
    /// 演示版只记录一件主武器/护甲，不做完整装备栏。
    /// </summary>
    public int equippedUid;

    /// <summary>
    /// 已拾取的固定地面物品 worldId。
    /// DemoScene 重新进入时据此跳过已经捡过的初始物品，避免重复刷新。
    /// </summary>
    public List<int> collectedWorldIds = new List<int>();

    /// <summary>
    /// 玩家主动丢弃到地面的物品。
    /// 与背包共用一个存档，退出重进后还能在原地捡回来。
    /// </summary>
    public List<DroppedItemData> droppedItems = new List<DroppedItemData>();

    /// <summary>
    /// 掉落物 worldId 的下一个可用值。
    /// 从 1000 开始，避开 DemoScene 固定物品的 1~6，防止动态掉落和静态物品撞 id。
    /// </summary>
    public int nextGroundId = 1000;

    /// <summary>
    /// 地面掉落物的存档结构（只存 defId / 数量 / 坐标，不存 ItemDefinition 引用）
    /// </summary>
    [System.Serializable]
    public class DroppedItemData
    {
        public int worldId;
        public int defId;
        public int count;
        public float x;
        public float y;
    }

    /// <summary>
    /// 本次运行是否成功从存档还原。
    /// 供 C# 侧判断/打日志用；用 NonSerialized 标记，避免它被 JsonUtility 写进存档（它不是玩家数据）。
    /// </summary>
    [System.NonSerialized]
    public bool loadedFromSave;

    private const string DEFAULT_SAVE_KEY = "PlayerData";
    private const string DEMO_SAVE_KEY = "PlayerData_Demo";

    /// <summary>
    /// 当前存档 key。DemoScene 与普通 SampleScene 使用不同 key：
    /// 演示重置不会清掉 SampleScene 的 67 件压测存档，两者互不污染。
    /// </summary>
    private static string saveKey = DEFAULT_SAVE_KEY;

    public static void SetDemoSaveKey(bool demoMode)
    {
        saveKey = demoMode ? DEMO_SAVE_KEY : DEFAULT_SAVE_KEY;
    }

    public static string CurrentSaveKey
    {
        get { return saveKey; }
    }

    /// <summary>
    /// 初始化背包容量（预留空槽，默认 99 格）
    /// </summary>
    public void InitInventory(int capacity = 99)
    {
        inventorySlots.Clear();
        for (int i = 0; i < capacity; i++)
        {
            inventorySlots.Add(new ItemSlot());
        }
    }

    /// <summary>
    /// 保存数据到 PlayerPrefs。
    /// 触发时机：OnApplicationQuit（编辑器里停止 Play Mode 也会触发），以及 Inspector 右键菜单手动调用。
    /// </summary>
    public void Save()
    {
        string json = JsonUtility.ToJson(this);
        PlayerPrefs.SetString(saveKey, json);
        PlayerPrefs.Save();
        Debug.Log($"[PlayerData] 存档完成，非空格子 {CountNonEmptySlots()}");
    }

    /// <summary>
    /// 从 PlayerPrefs 读档。
    /// 返回值：true = 成功还原存档；false = 没有存档或存档损坏（调用方按"新号"处理）。
    /// </summary>
    public bool Load()
    {
        loadedFromSave = false;

        string json = PlayerPrefs.GetString(saveKey, "");
        if (string.IsNullOrEmpty(json))
        {
            Debug.Log("[PlayerData] 无存档，按新号初始化");
            return false;
        }

        try
        {
            // FromJsonOverwrite：把 JSON 字段覆盖到现有对象上，保留对象引用（场景里的 PlayerData 继续有效）
            JsonUtility.FromJsonOverwrite(json, this);
        }
        catch (System.Exception e)
        {
            // 存档损坏不应该让游戏启动失败：打错误日志后按无存档处理
            Debug.LogError("[PlayerData] 存档解析失败，按新号处理：" + e.Message);
            return false;
        }

        if (inventorySlots == null)
        {
            inventorySlots = new List<ItemSlot>();
        }

        // 修复 uid 计数器：静态计数器每次运行从 1 开始，读档后必须抬到"存档最大 uid + 1"，
        // 否则本局新增物品的 uid 会与存档中的旧 uid 重复，破坏 UI 的 uid 锚定。
        int maxUid = 0;
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            ItemSlot slot = inventorySlots[i];
            if (slot != null && !slot.IsEmpty && slot.instance.uid > maxUid)
            {
                maxUid = slot.instance.uid;
            }
        }
        ItemInstance.EnsureUidCounterAbove(maxUid);

        // 新增字段的旧存档兼容：JsonUtility 对缺失字段保留当前值，
        // 但如果旧 JSON 里显式写了 null，这里仍要兜底成空列表。
        if (collectedWorldIds == null) collectedWorldIds = new List<int>();
        if (droppedItems == null) droppedItems = new List<DroppedItemData>();
        // 旧存档的计数器从 1 开始；动态掉落物必须使用 1000+，避免和初始物品 id 冲突
        if (nextGroundId < 1000) nextGroundId = 1000;
        if (hp <= 0) hp = 60; // 旧存档没有 hp 字段时保持默认，异常 0 也兜底

        // 装备 uid 必须仍然存在于背包中，否则 HUD 会显示一件已经不存在/被丢掉的装备
        if (equippedUid > 0)
        {
            bool equippedFound = false;
            for (int i = 0; i < inventorySlots.Count; i++)
            {
                ItemSlot slot = inventorySlots[i];
                if (slot != null && !slot.IsEmpty && slot.instance.uid == equippedUid)
                {
                    equippedFound = true;
                    break;
                }
            }
            if (!equippedFound) equippedUid = 0;
        }

        loadedFromSave = true;
        Debug.Log($"[PlayerData] 读档完成，非空格子 {CountNonEmptySlots()}，最大 uid {maxUid}");
        return true;
    }

    /// <summary>
    /// 退出时自动存档（编辑器里停止 Play Mode、打包后关闭游戏都会触发）。
    /// </summary>
    private void OnApplicationQuit()
    {
        Save();
    }

    /// <summary>
    /// 切到后台时存档。
    /// 移动端进程被系统杀掉时 OnApplicationQuit 不一定触发，OnApplicationPause(true) 更可靠。
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Save();
        }
    }

    /// <summary>
    /// 清空存档（Inspector 中右键组件可调用），用于重新验证"首次运行"流程。
    /// </summary>
    [ContextMenu("清空存档")]
    public void ClearSave()
    {
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
        Debug.Log("[PlayerData] 存档已清空");
    }

    /// <summary>
    /// 统计非空格子数（仅用于日志与验证，不影响数据）
    /// </summary>
    public int CountNonEmptySlots()
    {
        if (inventorySlots == null) return 0;
        int count = 0;
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i] != null && !inventorySlots[i].IsEmpty) count++;
        }
        return count;
    }
}
