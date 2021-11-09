using System;
using com.ilrframework.Runtime.CLRRedirection;
using com.ilrframework.Runtime.Adaptor;
using ILRuntime.Runtime.Intepreter;
using UnityEngine;
using AppDomain = ILRuntime.Runtime.Enviorment.AppDomain;

namespace com.ilrframework.Runtime
{
    public static class ILRRegister
    {
        public static void RegisterAll(AppDomain appDomain) {
            // 注册跨域继承适配器
            RegisterCrossBindingAdaptors(appDomain);
            
            // Delegate 的注册
            RegisterDelegates(appDomain);

            // 绑定值类型
            BindValueTypes(appDomain);
            
            // 注册 LitJson
            RegisterLitJson(appDomain);

            // CLR 重定向
            RegisterCLRRedirection(appDomain);
            
            // 初始化 CLR 绑定请放在初始化的最后一步！！
            CLRBinding(appDomain);
        }

        /// <summary>
        /// 注册跨域继承适配器
        /// </summary>
        /// <param name="appDomain"></param>
        public static void RegisterCrossBindingAdaptors(AppDomain appDomain) {
            appDomain.RegisterCrossBindingAdaptor(new IAsyncStateMachineAdapter());
            
            ILRApp.Instance.Configurator.RegisterCrossBindingAdaptors(appDomain);
        }

        /// <summary>
        /// Delegate 的注册
        /// </summary>
        /// <param name="appDomain"></param>
        private static void RegisterDelegates(AppDomain appDomain) {
            ILRApp.Instance.Configurator.RegisterDelegates(appDomain);
                
            var delegateManager = appDomain.DelegateManager;
                
            // 如果忘记注册，则在运行 Unity 的时候会报错：
            // KeyNotFoundException: Cannot find Delegate Adapter for:HotFix.Samples.TestDelegate.CallbackMethod(Int32 a), Please add following code:
            // appdomain.DelegateManager.RegisterMethodDelegate<System.Int32>();
            
            /*
             * 委托适配器（DelegateAdapter）
             * 同一个参数组合的委托，只需要注册一次即可，例如：
             *      delegate void SomeDelegate(int a, float b);
             *      Action<int, float> act;
             * 这两个委托都只需要注册一个适配器即可。 注册方法如下
             *      appDomain.DelegateManager.RegisterMethodDelegate<int, float>();
             * 如果是带返回类型的委托，例如：
             *      delegate bool SomeFunction(int a, float b);
             *      Func<int, float, bool> act;
             * 需要按照以下方式注册
             *      appDomain.DelegateManager.RegisterFunctionDelegate<int, float, bool>();
             */
            delegateManager.RegisterMethodDelegate<int>();
            delegateManager.RegisterMethodDelegate<string>();
            delegateManager.RegisterFunctionDelegate<int, string>();
            delegateManager.RegisterFunctionDelegate<System.Single>();
            delegateManager.RegisterMethodDelegate<System.Single>();
            delegateManager.RegisterMethodDelegate<UnityEngine.Color>();
            delegateManager.RegisterFunctionDelegate<UnityEngine.Color>();
            delegateManager.RegisterMethodDelegate<System.Single, System.Single>();
            delegateManager.RegisterMethodDelegate<System.Int32, System.Boolean>();
            delegateManager.RegisterFunctionDelegate<ILTypeInstance, ILTypeInstance, System.Int32>();
            delegateManager.RegisterFunctionDelegate<ILTypeInstance, System.Boolean>();
            delegateManager.RegisterMethodDelegate<System.String, System.Boolean>();
            delegateManager.RegisterMethodDelegate<ILRComponent>();
            delegateManager.RegisterMethodDelegate<System.String, System.Action<UnityEngine.U2D.SpriteAtlas>>();
            delegateManager.RegisterMethodDelegate<UnityEngine.Vector2>();
            delegateManager.RegisterFunctionDelegate<System.Int32, System.Int32, System.Int32>();
            
            /*
             * 委托转换器（DelegateConvertor）
             * ILRuntime 内部是使用 Action，以及 Func 这两个系统自带委托类型来生成的委托实例
             * 所以如果你需要将一个不是 Action 或者 Func 类型的委托实例传到 ILRuntime 外部使用的话
             * 除了委托适配器，还需要额外写一个转换器，将 Action 和 Func 转换成你真正需要的那个委托类型。
             */
            delegateManager.RegisterDelegateConvertor<UnityEngine.Events.UnityAction>((act) =>
            {
                return new UnityEngine.Events.UnityAction(() =>
                {
                    ((Action)act)();
                });
            });

            delegateManager.RegisterDelegateConvertor<System.Comparison<ILTypeInstance>>((act) =>
            {
                return new System.Comparison<ILTypeInstance>((x, y) =>
                {
                    return ((Func<ILTypeInstance, ILTypeInstance, System.Int32>)act)(x, y);
                });
            });
            
            delegateManager.RegisterDelegateConvertor<System.Predicate<ILTypeInstance>>((act) =>
            {
                return new System.Predicate<ILTypeInstance>((obj) =>
                {
                    return ((Func<ILTypeInstance, System.Boolean>)act)(obj);
                });
            });
            
            delegateManager.RegisterDelegateConvertor<UnityEngine.Events.UnityAction<System.Int32>>((act) =>
            {
                return new UnityEngine.Events.UnityAction<System.Int32>((arg0) =>
                {
                    ((Action<System.Int32>)act)(arg0);
                });
            });

            delegateManager.RegisterDelegateConvertor<UnityEngine.Events.UnityAction<UnityEngine.Vector2>>((act) =>
            {
                return new UnityEngine.Events.UnityAction<UnityEngine.Vector2>((arg0) =>
                {
                    ((Action<UnityEngine.Vector2>)act)(arg0);
                });
            });
            
            delegateManager.RegisterDelegateConvertor<System.Comparison<System.Int32>>((act) =>
            {
                return new System.Comparison<System.Int32>((x, y) =>
                {
                    return ((Func<System.Int32, System.Int32, System.Int32>)act)(x, y);
                });
            });
            
            delegateManager.RegisterDelegateConvertor<UnityEngine.LowLevel.PlayerLoopSystem.UpdateFunction>((act) =>
            {
                return new UnityEngine.LowLevel.PlayerLoopSystem.UpdateFunction(() =>
                {
                    ((Action)act)();
                });
            });
        }

        /// <summary>
        /// 绑定值类型
        /// </summary>
        /// <param name="appDomain"></param>
        private static void BindValueTypes(AppDomain appDomain) {
            ILRApp.Instance.Configurator.BindValueTypes(appDomain);
            
            appDomain.RegisterValueTypeBinder(typeof(Vector3), new Vector3Binder());
            appDomain.RegisterValueTypeBinder(typeof(Quaternion), new QuaternionBinder());
            appDomain.RegisterValueTypeBinder(typeof(Vector2), new Vector2Binder());
        }

        /// <summary>
        /// 注册 LitJson
        /// </summary>
        /// <param name="appDomain"></param>
        private static void RegisterLitJson(AppDomain appDomain) {
            LitJson.JsonMapper.RegisterILRuntimeCLRRedirection(appDomain);
        }

        /// <summary>
        /// CLR 重定向
        /// 注册重定向一定要在 CLR 绑定之前，因为谁先注册了后面注册的就会丢弃
        /// 所以想要实现自己的重定向时一定要先在这里注册，后面半自动生成的 CLR 绑定也不会覆盖掉
        /// </summary>
        private static unsafe void RegisterCLRRedirection(AppDomain appDomain) {
            ILRApp.Instance.Configurator.RegisterCLRRedirection(appDomain);
            
            // Debug.Log 的重定向
            CLRRedirectionDebug.Register(appDomain);
            // Component 的重定向
            CLRRedirectionComponent.Register(appDomain);
        }

        /// <summary>
        /// 初始化 CLR 绑定请放在初始化的最后一步！！
        /// 放在最后一步是因为 RegisterCLRMethodRedirection 的实现中，会判断 redirectMap 是否存在已注册的，如果已经有人注册
        /// 那么就会跳过，这样就可以不覆盖自定义的重定向了
        /// </summary>
        /// <param name="appDomain"></param>
        private static void CLRBinding(AppDomain appDomain) {
            // CLRBindings.Initialize(appDomain);
            
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies) {
                foreach (var type in assembly.GetTypes()) {
                    if (type.FullName == "ILRuntime.Runtime.Generated.CLRBindings") {
                        type.InvokeMember("Initialize",
                            System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, null,
                            new object[] { appDomain });
                        return;
                    }
                }
            }
        }

        public static void ShutdownCLRBindings(AppDomain appDomain) {
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies) {
                foreach (var type in assembly.GetTypes()) {
                    if (type.FullName == "ILRuntime.Runtime.Generated.CLRBindings") {
                        type.InvokeMember("Shutdown",
                            System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, null,
                            new object[] { appDomain });
                        return;
                    }
                }
            }
        }
    }
}
