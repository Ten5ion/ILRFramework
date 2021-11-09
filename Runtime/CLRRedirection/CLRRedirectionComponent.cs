using System;
using System.Collections.Generic;
using System.Reflection;
using ILRuntime.CLR.Method;
using ILRuntime.CLR.Utils;
using ILRuntime.Runtime.Intepreter;
using ILRuntime.Runtime.Stack;
using UnityEngine;
using AppDomain = ILRuntime.Runtime.Enviorment.AppDomain;

namespace com.ilrframework.Runtime.CLRRedirection
{
    public static class CLRRedirectionComponent
    {
        public static unsafe void Register(AppDomain appDomain) {
            var type = typeof(UnityEngine.Component);
            var flag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            var args = new Type[]{};
            var method = type.GetMethod("get_gameObject", flag, null, args, null);
            appDomain.RegisterCLRMethodRedirection(method, get_gameObject__01);
        }

        private static unsafe StackObject* get_gameObject__01(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj)
        {
            AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            UnityEngine.Component instance_of_this_method = (UnityEngine.Component)typeof(UnityEngine.Component).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);
            
            // 对象为空时打印错误日志
            if (instance_of_this_method == null) {
                var stackTrace = __domain.DebugService.GetStackTrace(__intp);
                stackTrace =
                    $"NullReferenceException: Object reference not set to an instance of an object\n{stackTrace}";
                Debug.LogError(stackTrace);
            }

            var result_of_this_method = instance_of_this_method.gameObject;

            return ILIntepreter.PushObject(__ret, __mStack, result_of_this_method);
        }
    }
}