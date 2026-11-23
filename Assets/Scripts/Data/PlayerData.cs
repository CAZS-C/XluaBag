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

    /// <summary>
    /// 背包槽位列表（真实数据的最终载体，替代 Lua 中的假数据）
    /// </summary>
    public List<ItemSlot> inventorySlots = new List<ItemSlot>();

    /// <summary>
    /// 本次运行是否成功从存档还原。
    /// 供 C# 侧判断/打日志用；用 NonSerialized 标记，避免它被 JsonUtility 写进存档（它不是玩家数据）。
    /// </summary>
    [System.NonSerialized]
    public bool loadedFromSave;

    private const string SAVE_KEY = "PlayerData";

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
        PlayerPrefs.SetString(SAVE_KEY, json);
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

        string json = PlayerPrefs.GetString(SAVE_KEY, "");
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
        PlayerPrefs.DeleteKey(SAVE_KEY);
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
