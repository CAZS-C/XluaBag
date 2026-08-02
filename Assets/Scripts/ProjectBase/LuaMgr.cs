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
            return luaEnv.Global;
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
        //无需转换点号，AB 中文件路径统一使用斜杠
        try
        {
            TextAsset file2 = ABMgr.GetInstance().LoadRes<TextAsset>("lua", filepath.Replace(".", "/") + ".lua");
            if (file2 != null)
                return file2.bytes;
            else
                Debug.Log("MyCustomLoaderFormAB重定向失败");
        }
        catch (System.Exception e)
        {
            Debug.Log("AB包尚未构建，从AB加载Lua失败：" + e.Message);
        }
        return null;
    }

    /// <summary>
    /// 执行lua文件
    /// </summary>
    /// <param name="fileName"></param>
    /// <param name="formWhere"></param>
    public void DoLuaFile(string fileName, string formWhere = null)
    {
        string str = string.Format("require('{0}')", fileName);
        luaEnv.DoString(str);
    }

    //执行Lua脚本
    public void DoString(string luaScript, string fromWhere = null)
    {
        luaEnv.DoString(luaScript, fromWhere);
    }

    //释放垃圾
    public void Tick()
    {
        luaEnv.Tick();
    }

    //销毁
    public void Dispose()
    {
        luaEnv.Tick();
        luaEnv.Dispose();
        
    }
    
    
}
