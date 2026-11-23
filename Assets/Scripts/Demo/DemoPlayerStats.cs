using UnityEngine;

/// <summary>
/// 演示版玩家状态：HP + 当前装备。
/// 只为“使用药水/装备”提供可见反馈，不做战斗和属性计算。
/// </summary>
public class DemoPlayerStats : MonoBehaviour
{
    public static DemoPlayerStats Instance { get; private set; }

    public int maxHp = 100;
    public int hp = 60;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// 回血。返回 false 表示当前已满血，调用方不应消耗药水。
    /// </summary>
    public bool Heal(int amount)
    {
        if (amount <= 0) return false;
        if (hp >= maxHp) return false;

        hp = Mathf.Min(maxHp, hp + amount);
        PlayerData pd = PlayerData.GetInstance();
        if (pd != null) pd.hp = hp; // 由调用方（Lua）在操作后统一 Save
        if (DemoHUD.Instance != null) DemoHUD.Instance.SetHP(hp, maxHp);
        return true;
    }

    /// <summary>装备一件物品（只记录 uid，不做属性计算）</summary>
    public void Equip(int uid, string displayName)
    {
        PlayerData pd = PlayerData.GetInstance();
        if (pd != null) pd.equippedUid = uid;

        if (DemoHUD.Instance != null) DemoHUD.Instance.SetEquipped(displayName);
    }

    /// <summary>
    /// 校验当前装备是否还在背包里。
    /// 丢弃/使用导致物品消失后调用，避免 HUD 继续显示一件不存在的装备。
    /// </summary>
    public void ValidateEquipped()
    {
        PlayerData pd = PlayerData.GetInstance();
        if (pd == null) return;

        if (pd.equippedUid <= 0)
        {
            if (DemoHUD.Instance != null) DemoHUD.Instance.SetEquipped(null);
            return;
        }

        InventoryManager inv = InventoryManager.GetInstance();
        int slotIndex = inv != null ? inv.GetSlotIndexByUid(pd.equippedUid) : -1;
        if (slotIndex < 0)
        {
            pd.equippedUid = 0;
            if (DemoHUD.Instance != null) DemoHUD.Instance.SetEquipped(null);
            return;
        }

        ItemDefinition def = inv.Slots[slotIndex].instance.Definition;
        if (DemoHUD.Instance != null)
        {
            DemoHUD.Instance.SetEquipped(def != null ? def.name : null);
        }
    }

    /// <summary>初始化 HUD 时同步一次 HP/装备显示</summary>
    public void SyncHUD()
    {
        PlayerData pd = PlayerData.GetInstance();
        if (pd != null && pd.hp > 0)
        {
            hp = Mathf.Clamp(pd.hp, 1, maxHp);
        }

        if (DemoHUD.Instance != null)
        {
            DemoHUD.Instance.SetHP(hp, maxHp);
        }
        ValidateEquipped();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
