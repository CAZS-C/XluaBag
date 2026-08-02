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
        LuaMgr.GetInstance().DoLuaFile("Main");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
