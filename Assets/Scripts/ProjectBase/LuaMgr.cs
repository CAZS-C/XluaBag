using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using XLua;

/// <summary>
/// Lua管理器
/// </summary>
public class LuaMgr : BaseManager<LuaMgr>
{
    private LuaEnv luaEnv;

    public void Init()
    {
        //防止重复创建 LuaEnv（Main 和测试脚本可能都调用 Init）
        if (luaEnv != null)
            return;

        //唯一的解析器
        luaEnv = new LuaEnv();
        //添加重定向委托函数
        luaEnv.AddLoader(MyCustomLoader);
        luaEnv.AddLoader(MyCustomLoaderFormAB);
    }

    //Lua总表
    //用于之后 lua访问C#时 使用 通过总表获取lua中各种内容
    public LuaTable Global
    {
        get
        {
            // Dispose 之后返回 null，避免访问已释放的 LuaEnv
            return luaEnv != null ? luaEnv.Global : null;
        }
    }

    private byte[] MyCustomLoader(ref string filepath)
    {
        //测试传入的参数是什么
        Debug.Log(filepath);
        //决定Lua文件所在路径
        //将 require 中的点号转为路径分隔符，例如 "Bag.BagPanelCtrl" → "Bag/BagPanelCtrl"
        string path = Application.dataPath + "/Lua/" + filepath.Replace(".", "/") + ".lua";
        //C#自带的文件读取类
        if (File.Exists(path))
        {
            return File.ReadAllBytes(path);
        }
        else
            Debug.Log("MyCustomLoader重定向失败");

        return null;
    }

    //再写一个Load 用于从AB包加载Lua文件
    private byte[] MyCustomLoaderFormAB(ref string filepath)
    {
        //改为我们的AB包管理器加载
        //路径约定与 Editor/LuaBundleTool 一致：
        //  资源名（Lua 里的 require）= "Bag.BagPanelCtrl" 或 "Bag/BagPanelCtrl"
        //  包内资源路径        = "Assets/ABRes/Lua/Bag/BagPanelCtrl.lua.txt"
        // 用"工程相对路径 + 扩展名"作为 LoadAsset 的键最精确，
        // 避免不同目录下的同名 Lua 文件互相覆盖。
        try
        {
            string rel = filepath.Replace(".", "/");
            string assetPath = "Assets/ABRes/Lua/" + rel + ".lua.txt";
            TextAsset file2 = ABMgr.GetInstance().LoadRes<TextAsset>("lua", assetPath);
            if (file2 != null)
            {
                Debug.Log("[LuaMgr] 从 AB 加载 Lua：" + assetPath);
                return file2.bytes;
            }
            else
            {
                Debug.Log("MyCustomLoaderFormAB重定向失败：" + assetPath);
            }
        }
        catch (System.Exception e)
        {
            Debug.Log("AB包尚未构建，从AB加载Lua失败：" + e.Message);
        }
        return null;
    }

    /// <summary>
    /// 执行lua文件。
    /// 返回 false 表示 Lua 抛异常（已记录日志）；调用方可以据此决定是否继续初始化，
    /// 避免异常直接冒到 MonoBehaviour.Start 导致半个模块初始化。
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="formWhere"></param>
    public bool DoLuaFile(string fileName, string formWhere = null)
    {
        if (luaEnv == null)
        {
            Debug.LogError("[LuaMgr] DoLuaFile 失败：LuaEnv 尚未初始化");
            return false;
        }

        string str = string.Format("require('{0}')", fileName);
        try
        {
            luaEnv.DoString(str);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("[LuaMgr] 执行 " + fileName + " 失败：" + e);
            return false;
        }
    }

    //执行Lua脚本
    public void DoString(string luaScript, string fromWhere = null)
    {
        if (luaEnv == null)
        {
            Debug.LogError("[LuaMgr] DoString 失败：LuaEnv 尚未初始化");
            return;
        }
        luaEnv.DoString(luaScript, fromWhere);
    }

    //释放垃圾
    public void Tick()
    {
        if (luaEnv == null)
        {
            return;
        }
        luaEnv.Tick();
    }

    //销毁
    public void Dispose()
    {
        if (luaEnv == null)
        {
            return;
        }

        // 先让 Lua 侧退订 C# 静态事件（BagPanelCtrl.Shutdown），
        // 否则 LuaEnv 释放后，InventoryEvent 里仍留着指向旧 Lua 环境的 DelegateBridge。
        try
        {
            luaEnv.DoString("local m = package.loaded['Bag.BagPanelCtrl']; if m and m.Shutdown then m.Shutdown() end");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[LuaMgr] Lua 侧 Shutdown 失败：" + e.Message);
        }

        try
        {
            luaEnv.Tick();
            luaEnv.Dispose();
            Debug.Log("[LuaMgr] LuaEnv 已释放");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[LuaMgr] Dispose 失败：" + e.Message);
        }
        finally
        {
            luaEnv = null;
        }
    }
    
    
}
