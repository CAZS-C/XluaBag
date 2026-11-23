using UnityEngine;

/// <summary>
/// 演示场景玩家控制器：
///   - 支持上下左右 + 四个斜向组合（共 8 个方向）
///   - 斜向输入会归一化，保证斜向速度和直线速度一致
///   - 用边界 clamp 代替物理碰撞，Demo 场景不需要 Rigidbody/Collider
///   - 背包打开或 HUD 弹层出现时通过 InputEnabled 锁住移动
///
/// 场景里只需要挂这个组件，身体/影子/朝向箭头由 Awake 运行时生成。
/// </summary>
public class DemoPlayerController : MonoBehaviour
{
    public static DemoPlayerController Instance { get; private set; }

    [Header("移动")]
    public float moveSpeed = 4.5f;
    public Vector2 boundsMin = new Vector2(-8.4f, -4.4f);
    public Vector2 boundsMax = new Vector2(8.4f, 4.4f);

    /// <summary>背包或弹层打开时置 false</summary>
    public bool InputEnabled = true;

    /// <summary>最后一次移动方向，丢弃物品时用来决定落点</summary>
    public Vector2 Facing { get; private set; } = Vector2.down;

    public Vector2 Position
    {
        get { return transform.position; }
    }

    private Transform facingArrow;

    private void Awake()
    {
        Instance = this;
        BuildVisuals();
    }

    private void Update()
    {
        if (!InputEnabled) return;

        // BagPanelView 是开关状态的唯一来源；这里再兜底一次，避免外部忘记设 InputEnabled
        if (BagPanelView.Instance != null && BagPanelView.Instance.IsOpen) return;

        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(x, y);

        if (input.sqrMagnitude < 0.01f) return;

        // 归一化后斜向（左上/左下/右上/右下）与直线速度一致，
        // 不会出现“斜着走更快”的问题。
        Vector2 dir = input.normalized;
        Facing = dir;
        Vector3 next = transform.position + (Vector3)(dir * moveSpeed * Time.deltaTime);
        next.x = Mathf.Clamp(next.x, boundsMin.x, boundsMax.x);
        next.y = Mathf.Clamp(next.y, boundsMin.y, boundsMax.y);
        transform.position = next;

        UpdateFacingArrow();
    }

    private void BuildVisuals()
    {
        if (transform.Find("Body") != null) return;

        // 影子
        GameObject shadow = new GameObject("Shadow");
        shadow.transform.SetParent(transform, false);
        shadow.transform.localPosition = new Vector3(0f, -0.32f, 0f);
        shadow.transform.localScale = new Vector3(0.95f, 0.38f, 1f);
        SpriteRenderer shadowSr = shadow.AddComponent<SpriteRenderer>();
        shadowSr.sprite = DemoArt.CircleSprite;
        shadowSr.color = new Color(0f, 0f, 0f, 0.28f);
        shadowSr.sortingOrder = 9;

        // 身体
        GameObject body = new GameObject("Body");
        body.transform.SetParent(transform, false);
        body.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
        SpriteRenderer bodySr = body.AddComponent<SpriteRenderer>();
        bodySr.sprite = DemoArt.CircleSprite;
        bodySr.color = new Color(0.95f, 0.76f, 0.34f, 1f);
        bodySr.sortingOrder = 10;

        // 朝向箭头
        GameObject arrow = new GameObject("FacingArrow");
        arrow.transform.SetParent(transform, false);
        arrow.transform.localPosition = new Vector3(0f, 0.18f, 0f);
        arrow.transform.localScale = new Vector3(0.22f, 0.22f, 1f);
        SpriteRenderer arrowSr = arrow.AddComponent<SpriteRenderer>();
        arrowSr.sprite = DemoArt.TriangleSprite;
        arrowSr.color = new Color(0.25f, 0.16f, 0.08f, 1f);
        arrowSr.sortingOrder = 11;
        facingArrow = arrow.transform;

        UpdateFacingArrow();
    }

    private void UpdateFacingArrow()
    {
        if (facingArrow == null) return;
        float angle = Mathf.Atan2(Facing.y, Facing.x) * Mathf.Rad2Deg - 90f;
        facingArrow.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
