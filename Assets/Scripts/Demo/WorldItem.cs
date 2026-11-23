using UnityEngine;

/// <summary>
/// 地面物品的运行时表现。
/// 由一个图标、一个品质色光圈和一个影子组成；不挂碰撞体，
/// 拾取判定由 DemoWorld 用距离做最近物品查询。
/// </summary>
public class WorldItem : MonoBehaviour
{
    public int DefId { get; private set; }
    public int Count { get; private set; }
    public int WorldId { get; private set; }
    public bool IsDropped { get; private set; }
    public float PickupReadyTime { get; private set; }

    private Transform iconTransform;
    private Transform ringTransform;
    private float bobPhase;

    /// <summary>初始化数据与占位表现。worldId 由 DemoWorld 分配并写入存档。</summary>
    public void Initialize(ItemDefinition def, int count, int worldId, bool isDropped)
    {
        DefId = def.id;
        Count = Mathf.Max(1, count);
        WorldId = worldId;
        IsDropped = isDropped;
        bobPhase = Random.Range(0f, Mathf.PI * 2f);

        BuildVisuals(def);
    }

    /// <summary>丢弃/从存档恢复时设置拾取冷却，避免刚落地就被立刻捡起</summary>
    public void SetPickupReadyTime(float time)
    {
        PickupReadyTime = time;
    }

    private void BuildVisuals(ItemDefinition def)
    {
        Color qualityColor = GetQualityColor(def != null ? def.quality : QualityLevel.Common);

        // 影子
        GameObject shadow = new GameObject("Shadow");
        shadow.transform.SetParent(transform, false);
        shadow.transform.localPosition = new Vector3(0f, -0.28f, 0f);
        shadow.transform.localScale = new Vector3(0.8f, 0.32f, 1f);
        SpriteRenderer shadowSr = shadow.AddComponent<SpriteRenderer>();
        shadowSr.sprite = DemoArt.CircleSprite;
        shadowSr.color = new Color(0f, 0f, 0f, 0.25f);
        shadowSr.sortingOrder = 1;

        // 品质光圈
        GameObject ring = new GameObject("Ring");
        ring.transform.SetParent(transform, false);
        ring.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        ring.transform.localScale = new Vector3(0.92f, 0.92f, 1f);
        SpriteRenderer ringSr = ring.AddComponent<SpriteRenderer>();
        ringSr.sprite = DemoArt.RingSprite;
        ringSr.color = qualityColor;
        ringSr.sortingOrder = 2;
        ringTransform = ring.transform;

        // 物品图标：优先从图集取，失败时回退 Resources，最后用品质色圆形兜底
        Sprite icon = null;
        if (def != null && !string.IsNullOrEmpty(def.iconPath))
        {
            icon = IconAtlasLoader.Get(def.iconPath);
            if (icon == null)
            {
                icon = Resources.Load<Sprite>(def.iconPath);
            }
        }

        GameObject iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(transform, false);
        iconGo.transform.localScale = new Vector3(0.62f, 0.62f, 1f);
        SpriteRenderer iconSr = iconGo.AddComponent<SpriteRenderer>();
        iconSr.sprite = icon != null ? icon : DemoArt.CircleSprite;
        iconSr.color = icon != null ? Color.white : qualityColor;
        iconSr.sortingOrder = 3;
        iconTransform = iconGo.transform;
    }

    private void Update()
    {
        // 轻微上下浮动，让地面物品和静态地面区分开
        float bob = Mathf.Sin(Time.time * 2f + bobPhase) * 0.07f;
        if (iconTransform != null)
        {
            iconTransform.localPosition = new Vector3(0f, 0.08f + bob, 0f);
        }
        if (ringTransform != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 2f + bobPhase) * 0.02f;
            ringTransform.localScale = new Vector3(0.92f * pulse, 0.92f * pulse, 1f);
        }
    }

    public static Color GetQualityColor(QualityLevel quality)
    {
        switch (quality)
        {
            case QualityLevel.Rare: return new Color(0.27f, 0.57f, 1f, 0.95f);
            case QualityLevel.Epic: return new Color(0.72f, 0.32f, 1f, 0.95f);
            case QualityLevel.Legendary: return new Color(1f, 0.58f, 0.12f, 0.95f);
            default: return new Color(0.92f, 0.92f, 0.92f, 0.9f);
        }
    }
}
