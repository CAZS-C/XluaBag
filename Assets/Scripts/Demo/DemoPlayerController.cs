using UnityEngine;

/// <summary>
/// 演示场景玩家控制器：
///   - 只做最简单的上下左右四向移动（不允许斜向，避免斜向速度比直线快）
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

        // 严格四向：同时按两个方向时只保留位移更大的那个轴
        if (Mathf.Abs(x) > Mathf.Abs(y))
        {
            y = 0f;
        }
        else if (Mathf.Abs(y) > 0f)
        {
            x = 0f;
        }
        else
        {
            return;
        }

        Vector2 dir = new Vector2(Mathf.Sign(x), Mathf.Sign(y));
        if (Mathf.Abs(x) < 0.01f) dir.x = 0f;
        if (Mathf.Abs(y) < 0.01f) dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return;

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
