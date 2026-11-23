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
    public class InventoryManagerWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(InventoryManager);
			Utils.BeginObjectRegister(type, L, translator, 0, 8, 1, 0);
			
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "CanAdd", _m_CanAdd);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "AddItem", _m_AddItem);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "RemoveItem", _m_RemoveItem);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "MoveItem", _m_MoveItem);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetFilteredSlots", _m_GetFilteredSlots);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "Sort", _m_Sort);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "SelectSlot", _m_SelectSlot);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "GetSlotIndexByUid", _m_GetSlotIndexByUid);
			
			
			Utils.RegisterFunc(L, Utils.GETTER_IDX, "Slots", _g_get_Slots);
            
			
			
			Utils.EndObjectRegister(type, L, translator, null, null,
			    null, null, null);

		    Utils.BeginClassRegister(type, L, __CreateInstance, 1, 0, 0);
			
			
            
			
			
			
			Utils.EndClassRegister(type, L, translator);
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int __CreateInstance(RealStatePtr L)
        {
            
			try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
				if(LuaAPI.lua_gettop(L) == 1)
				{
					
					var gen_ret = new InventoryManager();
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to InventoryManager constructor!");
            
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_CanAdd(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                InventoryManager gen_to_be_invoked = (InventoryManager)translator.FastGetCSObj(L, 1);
            
            
			    int gen_param_count = LuaAPI.lua_gettop(L);
            
                if(gen_param_count == 3&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 3)) 
                {
                    int _defId = LuaAPI.xlua_tointeger(L, 2);
                    int _count = LuaAPI.xlua_tointeger(L, 3);
                    
                        var gen_ret = gen_to_be_invoked.CanAdd( _defId, _count );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                if(gen_param_count == 2&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)) 
                {
                    int _defId = LuaAPI.xlua_tointeger(L, 2);
                    
                        var gen_ret = gen_to_be_invoked.CanAdd( _defId );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
            return LuaAPI.luaL_error(L, "invalid arguments to InventoryManager.CanAdd!");
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_AddItem(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                InventoryManager gen_to_be_invoked = (InventoryManager)translator.FastGetCSObj(L, 1);
            
            
			    int gen_param_count = LuaAPI.lua_gettop(L);
            
                if(gen_param_count == 3&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 3)) 
                {
                    int _defId = LuaAPI.xlua_tointeger(L, 2);
                    int _count = LuaAPI.xlua_tointeger(L, 3);
                    
                        var gen_ret = gen_to_be_invoked.AddItem( _defId, _count );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                if(gen_param_count == 2&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)) 
                {
                    int _defId = LuaAPI.xlua_tointeger(L, 2);
                    
                        var gen_ret = gen_to_be_invoked.AddItem( _defId );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
            return LuaAPI.luaL_error(L, "invalid arguments to InventoryManager.AddItem!");
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_RemoveItem(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                InventoryManager gen_to_be_invoked = (InventoryManager)translator.FastGetCSObj(L, 1);
            
            
			    int gen_param_count = LuaAPI.lua_gettop(L);
            
                if(gen_param_count == 3&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 3)) 
                {
                    int _slotIndex = LuaAPI.xlua_tointeger(L, 2);
                    int _count = LuaAPI.xlua_tointeger(L, 3);
                    
                        var gen_ret = gen_to_be_invoked.RemoveItem( _slotIndex, _count );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                if(gen_param_count == 2&& LuaTypes.LUA_TNUMBER == LuaAPI.lua_type(L, 2)) 
                {
                    int _slotIndex = LuaAPI.xlua_tointeger(L, 2);
                    
                        var gen_ret = gen_to_be_invoked.RemoveItem( _slotIndex );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
            return LuaAPI.luaL_error(L, "invalid arguments to InventoryManager.RemoveItem!");
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_MoveItem(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                InventoryManager gen_to_be_invoked = (InventoryManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    int _fromIndex = LuaAPI.xlua_tointeger(L, 2);
                    int _toIndex = LuaAPI.xlua_tointeger(L, 3);
                    
                        var gen_ret = gen_to_be_invoked.MoveItem( _fromIndex, _toIndex );
                        LuaAPI.lua_pushboolean(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetFilteredSlots(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                InventoryManager gen_to_be_invoked = (InventoryManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    ItemType _type;translator.Get(L, 2, out _type);
                    
                        var gen_ret = gen_to_be_invoked.GetFilteredSlots( _type );
                        translator.Push(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_Sort(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                InventoryManager gen_to_be_invoked = (InventoryManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.Sort(  );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_SelectSlot(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                InventoryManager gen_to_be_invoked = (InventoryManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    int _index = LuaAPI.xlua_tointeger(L, 2);
                    
                    gen_to_be_invoked.SelectSlot( _index );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_GetSlotIndexByUid(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                InventoryManager gen_to_be_invoked = (InventoryManager)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    int _uid = LuaAPI.xlua_tointeger(L, 2);
                    
                        var gen_ret = gen_to_be_invoked.GetSlotIndexByUid( _uid );
                        LuaAPI.xlua_pushinteger(L, gen_ret);
                    
                    
                    
                    return 1;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_Slots(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                InventoryManager gen_to_be_invoked = (InventoryManager)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.Slots);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
		
		
		
		
    }
}
