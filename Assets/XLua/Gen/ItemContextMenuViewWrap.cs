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
    public class ItemContextMenuViewWrap 
    {
        public static void __Register(RealStatePtr L)
        {
			ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			System.Type type = typeof(ItemContextMenuView);
			Utils.BeginObjectRegister(type, L, translator, 0, 2, 4, 3);
			
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "Show", _m_Show);
			Utils.RegisterFunc(L, Utils.METHOD_IDX, "Hide", _m_Hide);
			
			
			Utils.RegisterFunc(L, Utils.GETTER_IDX, "IsOpen", _g_get_IsOpen);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "onUse", _g_get_onUse);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "onDrop", _g_get_onDrop);
            Utils.RegisterFunc(L, Utils.GETTER_IDX, "onCancel", _g_get_onCancel);
            
			Utils.RegisterFunc(L, Utils.SETTER_IDX, "onUse", _s_set_onUse);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "onDrop", _s_set_onDrop);
            Utils.RegisterFunc(L, Utils.SETTER_IDX, "onCancel", _s_set_onCancel);
            
			
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
					
					var gen_ret = new ItemContextMenuView();
					translator.Push(L, gen_ret);
                    
					return 1;
				}
				
			}
			catch(System.Exception gen_e) {
				return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
			}
            return LuaAPI.luaL_error(L, "invalid arguments to ItemContextMenuView constructor!");
            
        }
        
		
        
		
        
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_Show(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                ItemContextMenuView gen_to_be_invoked = (ItemContextMenuView)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    UnityEngine.Vector2 _screenPosition;translator.Get(L, 2, out _screenPosition);
                    bool _canUse = LuaAPI.lua_toboolean(L, 3);
                    string _useText = LuaAPI.lua_tostring(L, 4);
                    
                    gen_to_be_invoked.Show( _screenPosition, _canUse, _useText );
                    
                    
                    
                    return 0;
                }
                
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _m_Hide(RealStatePtr L)
        {
		    try {
            
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
            
            
                ItemContextMenuView gen_to_be_invoked = (ItemContextMenuView)translator.FastGetCSObj(L, 1);
            
            
                
                {
                    
                    gen_to_be_invoked.Hide(  );
                    
                    
                    
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
			    translator.Push(L, ItemContextMenuView.Instance);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_IsOpen(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                ItemContextMenuView gen_to_be_invoked = (ItemContextMenuView)translator.FastGetCSObj(L, 1);
                LuaAPI.lua_pushboolean(L, gen_to_be_invoked.IsOpen);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_onUse(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                ItemContextMenuView gen_to_be_invoked = (ItemContextMenuView)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.onUse);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_onDrop(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                ItemContextMenuView gen_to_be_invoked = (ItemContextMenuView)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.onDrop);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _g_get_onCancel(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                ItemContextMenuView gen_to_be_invoked = (ItemContextMenuView)translator.FastGetCSObj(L, 1);
                translator.Push(L, gen_to_be_invoked.onCancel);
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 1;
        }
        
        
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_onUse(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                ItemContextMenuView gen_to_be_invoked = (ItemContextMenuView)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.onUse = translator.GetDelegate<System.Action>(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_onDrop(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                ItemContextMenuView gen_to_be_invoked = (ItemContextMenuView)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.onDrop = translator.GetDelegate<System.Action>(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
        [MonoPInvokeCallbackAttribute(typeof(LuaCSFunction))]
        static int _s_set_onCancel(RealStatePtr L)
        {
		    try {
                ObjectTranslator translator = ObjectTranslatorPool.Instance.Find(L);
			
                ItemContextMenuView gen_to_be_invoked = (ItemContextMenuView)translator.FastGetCSObj(L, 1);
                gen_to_be_invoked.onCancel = translator.GetDelegate<System.Action>(L, 2);
            
            } catch(System.Exception gen_e) {
                return LuaAPI.luaL_error(L, "c# exception:" + gen_e);
            }
            return 0;
        }
        
		
		
		
		
    }
}
