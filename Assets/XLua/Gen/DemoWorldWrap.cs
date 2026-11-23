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
    public class DemoWorldWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(DemoWorld);
			Utils.BeginObjectRegister(type, L, translator, 0, 4, 1, 1);
			
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "Initialize", _m_Initialize);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "FindNearest", _m_FindNearest);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "TryPickup", _m_TryPickup);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "DropFromPlayer", _m_DropFromPlayer);
			
			
			Utils.RegisterFunc(L, Utils.GETTER_IDX, "initialItems", _g_get_initialItems);
            
			Utils.RegisterFunc(L, Utils.SETTER_IDX, "initialItems", _s_set_initialItems);
            
			
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
					
					var gen_ret = new DemoWorld();
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to DemoWorld constructor!");
            
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_Initialize(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                DemoWorld gen_to_be_invoked = (DemoWorld)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.Initialize(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_FindNearest(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                DemoWorld gen_to_be_invoked = (DemoWorld)translator.FastGetCSObj(L, 1);
            
            
			    int gen_param_count = LuaAPI.lua_gettop(L);
            
                if(gen_param_count == 3&& translator.Assignable<UnityEngine.Vector2>(L, 2)&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 3)) 
                {
                    UnityEngine.Vector2 _playerPosition;translator.Get(L, 2, out _playerPosition);
                    float _range = (float)LuaAPI.lua_tonumber(L, 3);
                    
                        var gen_ret = gen_to_be_invoked.FindNearest( _playerPosition, _range );
                        translator.Push(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                if(gen_param_count == 2&& translator.Assignable<UnityEngine.Vector2>(L, 2)) 
                {
                    UnityEngine.Vector2 _playerPosition;translator.Get(L, 2, out _playerPosition);
                    
                        var gen_ret = gen_to_be_invoked.FindNearest( _playerPosition );
                        translator.Push(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
            return LuaAPI.luaL_error(L, "invalid arguments to DemoWorld.FindNearest!");
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_TryPickup(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                DemoWorld gen_to_be_invoked = (DemoWorld)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    WorldItem _item = (WorldItem)translator.GetObject(L, 2, typeof(WorldItem));
                    
                        var gen_ret = gen_to_be_invoked.TryPickup( _item );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_DropFromPlayer(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                DemoWorld gen_to_be_invoked = (DemoWorld)translator.FastGetCSObj(L, 1);
            
            
			    int gen_param_count = LuaAPI.lua_gettop(L);
            
                if(gen_param_count == 3&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 3)) 
                {
                    int _defId = LuaAPI.xlua_tointeger(L, 2);
                    int _count = LuaAPI.xlua_tointeger(L, 3);
                    
                    gen_to_be_invoked.DropFromPlayer( _defId, _count );
                    
                    
                    
                    return 0;
                }
                if(gen_param_count == 2&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)) 
                {
                    int _defId = LuaAPI.xlua_tointeger(L, 2);
                    
                    gen_to_be_invoked.DropFromPlayer( _defId );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
            return LuaAPI.luaL_error(L, "invalid arguments to DemoWorld.DropFromPlayer!");
            
        }
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_Instance(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			    translator.Push(L, DemoWorld.Instance);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_initialItems(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                DemoWorld gen_to_be_invoked = (DemoWorld)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.initialItems);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_initialItems(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                DemoWorld gen_to_be_invoked = (DemoWorld)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.initialItems = (DemoWorld.DemoItemSpawn[])translator.GetObject(L, 2, typeof(DemoWorld.DemoItemSpawn[]));
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
		
		
		
		
    }
}
