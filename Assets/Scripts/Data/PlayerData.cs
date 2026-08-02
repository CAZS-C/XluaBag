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
    /// 保存数据到 PlayerPrefs
    /// </summary>
    public void Save()
    {
        string json = JsonUtility.ToJson(this);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 从 PlayerPrefs 读档；无存档时保持现状
    /// </summary>
    public void Load()
    {
        string json = PlayerPrefs.GetString(SAVE_KEY, "");
        if (!string.IsNullOrEmpty(json))
        {
            JsonUtility.FromJsonOverwrite(json, this);
        }
    }
}
