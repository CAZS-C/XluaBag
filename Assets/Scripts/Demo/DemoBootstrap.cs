using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// DemoScene 的启动协调器。
///
/// 不在 Start 里直接生成世界：Main.cs 必须先完成 LuaEnv、配置表、读档和
/// Main.lua 初始化，BagPanelCtrl 也才能拿到 BagPanelView。因此由 Main.cs
/// 在 Lua 成功后显式调用 Initialize，保证时序稳定。
/// </summary>
public class DemoBootstrap : MonoBehaviour
{
    public static DemoBootstrap Instance { get; private set; }

    private bool initialized;
    private DemoPlayerController player;
    private DemoWorld world;
    private DemoPlayerStats stats;
    private DemoHUD hud;
    private BagPanelView bagView;

    private void Awake()
    {
        Instance = this;
    }

    /// <summary>由 Main.cs 在 Main.lua 执行成功后调用</summary>
    public void Initialize()
    {
        if (initialized) return;
        initialized = true;

        player = DemoPlayerController.Instance != null
            ? DemoPlayerController.Instance
            : FindObjectOfType<DemoPlayerController>();
        world = DemoWorld.Instance != null
            ? DemoWorld.Instance
            : FindObjectOfType<DemoWorld>();
        stats = DemoPlayerStats.Instance != null
            ? DemoPlayerStats.Instance
            : FindObjectOfType<DemoPlayerStats>();
        hud = DemoHUD.Instance != null
            ? DemoHUD.Instance
            : FindObjectOfType<DemoHUD>();
        bagView = BagPanelView.Instance != null
            ? BagPanelView.Instance
            : FindObjectOfType<BagPanelView>();

        // 初始状态：先初始化 HUD，再让 stats 同步 HP/装备，避免 SetEquipped(null)
        // 覆盖掉刚刚从存档恢复的装备显示。
        if (hud != null) hud.Initialize();
        if (bagView != null) bagView.Hide();
        if (world != null) world.Initialize();
        if (stats != null) stats.SyncHUD();
        if (player != null) player.InputEnabled = true;
    }

    private void Update()
    {
        if (!initialized || player == null || world == null || hud == null) return;

        if (bagView != null && bagView.IsOpen)
        {
            hud.SetPrompt("");
            return;
        }

        WorldItem nearest = world.FindNearest(player.Position);
        if (nearest == null)
        {
            hud.SetPrompt("");
            return;
        }

        ItemDefinition def = ItemDefinition.Get(nearest.DefId);
        string name = def != null ? def.name : "物品";
        string suffix = nearest.Count > 1 ? " ×" + nearest.Count : "";
        hud.SetPrompt("F 拾取 " + name + suffix);

        if (Input.GetKeyDown(KeyCode.F))
        {
            world.TryPickup(nearest);
        }
    }

    /// <summary>清空背包/世界存档并重载当前 DemoScene，方便反复演示</summary>
    public void ResetDemo()
    {
        PlayerData pd = PlayerData.GetInstance();
        if (pd != null) pd.ClearSave();

        // Main.lua 是通过 require('Main') 执行的，package.loaded 会缓存模块。
        // 重载场景前先释放 LuaEnv，下一局 Main.Start 才会重新执行 Main.lua、添加 starter。
        LuaMgr.GetInstance().Dispose();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
