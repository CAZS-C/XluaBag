using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        LuaMgr.GetInstance().Init();
        // 先加载配置表（物品定义注册表），供后续 Lua 业务逻辑使用
        ConfigManager.LoadItemConfig();

        // 读档必须在执行 Main.lua 之前：
        // Main.lua 需要根据"背包是否已有数据"决定要不要塞测试物品，
        // 顺序反了会导致测试数据与存档数据叠加、物品翻倍。
        PlayerData playerData = PlayerData.GetInstance();
        if (playerData != null)
        {
            playerData.Load();
        }
        else
        {
            Debug.LogError("[Main] 场景中未挂载 PlayerData，跳过读档");
        }

        bool luaStarted = LuaMgr.GetInstance().DoLuaFile("Main");
        if (luaStarted)
        {
            // 热更新健康检查：Lua 成功初始化后，才算当前资源版本可用。
            // 下次启动如果 pending != confirmed，会自动回滚到上一版本。
            UpdateStateStore.ConfirmCurrent();
        }
        else
        {
            Debug.LogError("[Main] Main.lua 执行失败，背包功能未初始化");
        }
    }

    /// <summary>
    /// 退出时释放 LuaEnv：先让 Lua 侧退订 C# 静态事件，再销毁 Lua 状态机。
    /// </summary>
    private void OnApplicationQuit()
    {
        LuaMgr.GetInstance().Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
