#if USE_UNI_LUA
using LuaAPI = UniLua.Lua;
using RealStatePtr = UniLua.ILuaState;
using LuaCSFunction = UniLua.CSharpFunctionDelegate;
#else
using LuaAPI = XLua.LuaDLL.Lua;
using RealStatePtr = System.IntPtr;
using LuaCSFunction = XLua.LuaDLL.lua_CSFunction;
#endif

using XLua;
using System.Collections.Generic;


namespace XLua.CSObjectWrap
{
    using Utils = XLua.Utils;
    public class DemoHUDWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(DemoHUD);
			Utils.BeginObjectRegister(type, L, translator, 0, 6, 0, 0);
			
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "Initialize", _m_Initialize);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "InstallButtonFeedback", _m_InstallButtonFeedback);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetHP", _m_SetHP);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetEquipped", _m_SetEquipped);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SetPrompt", _m_SetPrompt);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "ShowToast", _m_ShowToast);
			
			
			
			
			
			Utils.EndObjectRegister(type, L, translator, null, null,
			    null, null, null);

		    Utils.BeginClassRegister(type, L, __CreateInstance, 1, 1, 0);
			
			
            
			Utils.RegisterFunc(L, Utils.CLS_GETTER_IDX, "Instance", _g_get_Instance);
            
			
			
			Utils.EndClassRegister(type, L, translator);
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CreateInstance(RealStatePtr L)
        {
            
			try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
				if(LuaAPI.lua_gettop(L) == 1)
				{
					
					var gen_ret = new DemoHUD();
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to DemoHUD constructor!");
            
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_Initialize(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                DemoHUD gen_to_be_invoked = (DemoHUD)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.Initialize(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_InstallButtonFeedback(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                DemoHUD gen_to_be_invoked = (DemoHUD)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.InstallButtonFeedback(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetHP(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                DemoHUD gen_to_be_invoked = (DemoHUD)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    int _current = LuaAPI.xlua_tointeger(L, 2);
                    int _max = LuaAPI.xlua_tointeger(L, 3);
                    
                    gen_to_be_invoked.SetHP( _current, _max );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetEquipped(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                DemoHUD gen_to_be_invoked = (DemoHUD)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    string _displayName = LuaAPI.lua_tostring(L, 2);
                    
                    gen_to_be_invoked.SetEquipped( _displayName );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SetPrompt(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                DemoHUD gen_to_be_invoked = (DemoHUD)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    string _text = LuaAPI.lua_tostring(L, 2);
                    
                    gen_to_be_invoked.SetPrompt( _text );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_ShowToast(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                DemoHUD gen_to_be_invoked = (DemoHUD)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    string _message = LuaAPI.lua_tostring(L, 2);
                    
                    gen_to_be_invoked.ShowToast( _message );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_Instance(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			    translator.Push(L, DemoHUD.Instance);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
		
		
		
		
    }
}
