using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 演示场景世界层：
///   - 生成初始地面物品（固定 worldId，可被永久拾取）
///   - 恢复玩家丢弃的物品（动态 worldId，可反复捡起/丢弃）
///   - 最近物品查询、拾取、丢弃、存档同步
///
/// 这里不依赖物理碰撞，物品数量很少，直接遍历列表做距离判断即可。
/// </summary>
public class DemoWorld : MonoBehaviour
{
    public static DemoWorld Instance { get; private set; }

    [Serializable]
    public class DemoItemSpawn
    {
        public int defId;
        public int count = 1;
        public float x;
        public float y;
        public int worldId;
    }

    [Header("初始地面物品（留空则使用代码内的默认 6 件）")]
    public DemoItemSpawn[] initialItems;

    private readonly List<WorldItem> items = new List<WorldItem>();
    private bool initialized;

    private static readonly DemoItemSpawn[] DefaultSpawns =
    {
        new DemoItemSpawn { defId = 1001, count = 1, x = -7.0f, y = 2.4f, worldId = 1 }, // 铁剑
        new DemoItemSpawn { defId = 1009, count = 1, x = -4.6f, y = -2.7f, worldId = 2 }, // 火焰之剑
        new DemoItemSpawn { defId = 2001, count = 3, x = -2.1f, y = 2.2f, worldId = 3 }, // 生命药水
        new DemoItemSpawn { defId = 2006, count = 1, x = 0.9f, y = -2.5f, worldId = 4 }, // 传送卷轴
        new DemoItemSpawn { defId = 3001, count = 2, x = 2.9f, y = 2.3f, worldId = 5 }, // 红宝石
        new DemoItemSpawn { defId = 3015, count = 1, x = 4.4f, y = -2.9f, worldId = 6 }, // 神圣钻石
    };

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 由 DemoBootstrap 在 Lua 初始化完成后调用。
    /// 此时 PlayerData 已经读档，背包和世界状态可以直接对上。
    /// </summary>
    public void Initialize()
    {
        if (initialized) return;
        initialized = true;

        CreateGround();

        PlayerData pd = PlayerData.GetInstance();
        if (pd == null)
        {
            Debug.LogError("[DemoWorld] 找不到 PlayerData，地面物品无法初始化");
            return;
        }

        if (pd.collectedWorldIds == null) pd.collectedWorldIds = new List<int>();
        if (pd.droppedItems == null) pd.droppedItems = new List<PlayerData.DroppedItemData>();
        if (pd.nextGroundId < 1000) pd.nextGroundId = 1000;

        // 旧版本掉落物可能使用了 1~6，和静态物品 id 冲突。
        // 这里统一迁移到 1000+，并把迁移结果立即写回存档。
        bool migratedDroppedIds = false;
        for (int i = 0; i < pd.droppedItems.Count; i++)
        {
            PlayerData.DroppedItemData drop = pd.droppedItems[i];
            if (drop.worldId < 1000)
            {
                drop.worldId = pd.nextGroundId++;
                migratedDroppedIds = true;
            }
        }
        if (migratedDroppedIds) pd.Save();

        // 1. 初始物品：跳过已经捡过的 worldId
        DemoItemSpawn[] spawns = (initialItems != null && initialItems.Length > 0)
            ? initialItems
            : DefaultSpawns;

        for (int i = 0; i < spawns.Length; i++)
        {
            DemoItemSpawn spawn = spawns[i];
            if (pd.collectedWorldIds.Contains(spawn.worldId)) continue;
            Spawn(spawn.defId, spawn.count, new Vector2(spawn.x, spawn.y), spawn.worldId, false);
        }

        // 2. 玩家丢弃的物品：按存档坐标恢复
        for (int i = 0; i < pd.droppedItems.Count; i++)
        {
            PlayerData.DroppedItemData drop = pd.droppedItems[i];
            WorldItem item = Spawn(drop.defId, drop.count, new Vector2(drop.x, drop.y), drop.worldId, true);
            if (item != null) item.SetPickupReadyTime(Time.time + 0.5f);
        }
    }

    private void CreateGround()
    {
        if (transform.Find("Ground") != null) return;

        GameObject ground = new GameObject("Ground");
        ground.transform.SetParent(transform, false);
        ground.transform.position = Vector3.zero;
        SpriteRenderer sr = ground.AddComponent<SpriteRenderer>();
        sr.sprite = DemoArt.GridSprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(18f, 10f);
        sr.sortingOrder = -100;
    }

    private WorldItem Spawn(int defId, int count, Vector2 position, int worldId, bool isDropped)
    {
        ItemDefinition def = ItemDefinition.Get(defId);
        if (def == null)
        {
            Debug.LogWarning("[DemoWorld] 未注册的物品 defId=" + defId + "，跳过生成");
            return null;
        }

        GameObject go = new GameObject("WorldItem_" + defId + "_" + worldId);
        go.transform.SetParent(transform, false);
        go.transform.position = new Vector3(position.x, position.y, 0f);

        WorldItem item = go.AddComponent<WorldItem>();
        item.Initialize(def, count, worldId, isDropped);
        items.Add(item);
        return item;
    }

    /// <summary>查找玩家附近最近的可拾取物品；没有则返回 null</summary>
    public WorldItem FindNearest(Vector2 playerPosition, float range = 1.4f)
    {
        WorldItem nearest = null;
        float nearestSqr = range * range;

        for (int i = 0; i < items.Count; i++)
        {
            WorldItem item = items[i];
            if (item == null) continue;
            if (Time.time < item.PickupReadyTime) continue;

            float sqr = ((Vector2)item.transform.position - playerPosition).sqrMagnitude;
            if (sqr <= nearestSqr)
            {
                nearestSqr = sqr;
                nearest = item;
            }
        }
        return nearest;
    }

    /// <summary>
    /// 尝试拾取。先做容量预检查，避免 AddItem 部分放入后地面物品被销毁。
    /// 成功后会同步“已拾取固定物品 / 移除已恢复掉落物”并立即存档。
    /// </summary>
    public bool TryPickup(WorldItem item)
    {
        if (item == null) return false;

        ItemDefinition def = ItemDefinition.Get(item.DefId);
        InventoryManager inv = InventoryManager.GetInstance();
        if (def == null || inv == null) return false;

        if (!inv.CanAdd(item.DefId, item.Count))
        {
            if (DemoHUD.Instance != null) DemoHUD.Instance.ShowToast("背包已满，无法拾取 " + def.name);
            return false;
        }

        if (!inv.AddItem(item.DefId, item.Count)) return false;

        PlayerData pd = PlayerData.GetInstance();
        if (pd != null)
        {
            if (item.IsDropped)
            {
                pd.droppedItems.RemoveAll(d => d.worldId == item.WorldId);
            }
            else if (!pd.collectedWorldIds.Contains(item.WorldId))
            {
                pd.collectedWorldIds.Add(item.WorldId);
            }
            pd.Save();
        }

        if (DemoHUD.Instance != null)
        {
            string suffix = item.Count > 1 ? " ×" + item.Count : "";
            DemoHUD.Instance.ShowToast("获得 " + def.name + suffix);
        }

        items.Remove(item);
        Destroy(item.gameObject);
        return true;
    }

    /// <summary>
    /// 从玩家脚下丢出 1 个物品。
    /// 注意：调用方（Lua）必须先扣背包数量，再调用本方法。
    /// </summary>
    public void DropFromPlayer(int defId, int count = 1)
    {
        PlayerData pd = PlayerData.GetInstance();
        if (pd == null) return;

        if (pd.droppedItems == null) pd.droppedItems = new List<PlayerData.DroppedItemData>();
        if (pd.nextGroundId < 1000) pd.nextGroundId = 1000;

        Vector2 position = Vector2.zero;
        Vector2 facing = Vector2.down;
        DemoPlayerController player = DemoPlayerController.Instance;
        if (player != null)
        {
            position = player.Position + player.Facing * 0.9f;
            facing = player.Facing;
            position.x = Mathf.Clamp(position.x, player.boundsMin.x, player.boundsMax.x);
            position.y = Mathf.Clamp(position.y, player.boundsMin.y, player.boundsMax.y);
        }
        // 避免正好落在玩家中心，给一点朝向偏移
        if (position == Vector2.zero) position = facing * 0.9f;

        int worldId = pd.nextGroundId++;
        PlayerData.DroppedItemData data = new PlayerData.DroppedItemData
        {
            worldId = worldId,
            defId = defId,
            count = Mathf.Max(1, count),
            x = position.x,
            y = position.y,
        };
        pd.droppedItems.Add(data);

        WorldItem item = Spawn(defId, data.count, position, worldId, true);
        if (item != null) item.SetPickupReadyTime(Time.time + 1f);

        pd.Save();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
