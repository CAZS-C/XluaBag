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
    public class DemoPlayerStatsWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(DemoPlayerStats);
			Utils.BeginObjectRegister(type, L, translator, 0, 4, 2, 2);
			
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "Heal", _m_Heal);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "Equip", _m_Equip);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "ValidateEquipped", _m_ValidateEquipped);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SyncHUD", _m_SyncHUD);
			
			
			Utils.RegisterFunc(L, Utils.GETTER_IDX, "maxHp", _g_get_maxHp);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "hp", _g_get_hp);
            
			Utils.RegisterFunc(L, Utils.SETTER_IDX, "maxHp", _s_set_maxHp);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "hp", _s_set_hp);
            
			
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
					
					var gen_ret = new DemoPlayerStats();
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to DemoPlayerStats constructor!");
            
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_Heal(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                DemoPlayerStats gen_to_be_invoked = (DemoPlayerStats)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    int _amount = LuaAPI.xlua_tointeger(L, 2);
                    
                        var gen_ret = gen_to_be_invoked.Heal( _amount );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_Equip(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                DemoPlayerStats gen_to_be_invoked = (DemoPlayerStats)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    int _uid = LuaAPI.xlua_tointeger(L, 2);
                    string _displayName = LuaAPI.lua_tostring(L, 3);
                    
                    gen_to_be_invoked.Equip( _uid, _displayName );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_ValidateEquipped(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                DemoPlayerStats gen_to_be_invoked = (DemoPlayerStats)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.ValidateEquipped(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SyncHUD(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                DemoPlayerStats gen_to_be_invoked = (DemoPlayerStats)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.SyncHUD(  );
                    
                    
                    
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
			    translator.Push(L, DemoPlayerStats.Instance);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_maxHp(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                DemoPlayerStats gen_to_be_invoked = (DemoPlayerStats)translator.FastGetCSObj(L, 1);
                LuaAPI.xlua_pushinteger(L, gen_to_be_invoked.maxHp);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_hp(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                DemoPlayerStats gen_to_be_invoked = (DemoPlayerStats)translator.FastGetCSObj(L, 1);
                LuaAPI.xlua_pushinteger(L, gen_to_be_invoked.hp);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_maxHp(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                DemoPlayerStats gen_to_be_invoked = (DemoPlayerStats)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.maxHp = LuaAPI.xlua_tointeger(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_hp(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                DemoPlayerStats gen_to_be_invoked = (DemoPlayerStats)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.hp = LuaAPI.xlua_tointeger(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
		
		
		
		
    }
}
