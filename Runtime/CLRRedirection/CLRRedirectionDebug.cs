using System.Collections.Generic;
using ILRuntime.CLR.Method;
using ILRuntime.CLR.Utils;
using ILRuntime.Runtime.Enviorment;
using ILRuntime.Runtime.Intepreter;
using ILRuntime.Runtime.Stack;
using UnityEngine;

namespace com.ilrframework.Runtime.CLRRedirection
{
    public static class CLRRedirectionDebug
    {
        public static unsafe void Register(AppDomain appDomain) {
            var t = typeof(Debug);
            var mDebugLog = t.GetMethod("Log", new System.Type[] { typeof(object) });
            appDomain.RegisterCLRMethodRedirection(mDebugLog, Log_01);
            
            mDebugLog = t.GetMethod("LogWarning", new System.Type[] { typeof(object) });
            appDomain.RegisterCLRMethodRedirection(mDebugLog, LogWarning_01);
            
            mDebugLog = t.GetMethod("LogError", new System.Type[] { typeof(object) });
            appDomain.RegisterCLRMethodRedirection(mDebugLog, LogError_01);
        }
        
        // 编写重定向方法对于刚接触 ILRuntime 的朋友可能比较困难，比较简单的方式是通过 CLR 绑定生成绑定代码，然后在这个基础上改
        // 比如下面这个代码是从 UnityEngine_Debug_Binding 里面复制来改的
        // 如何使用 CLR 绑定请看相关教程和文档
        private static unsafe StackObject* Log_01(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj) {
            // ILRuntime的调用约定为被调用者清理堆栈，因此执行这个函数后需要将参数从堆栈清理干净，并把返回值放在栈顶
            // 具体请看 ILRuntime 实现原理文档
            AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            // 这个是最后方法返回后 esp 栈指针的值，应该返回清理完参数并指向返回值，这里是只需要返回清理完参数的值即可
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);
            // 取Log方法的参数，如果有两个参数的话，第一个参数是esp - 2,第二个参数是esp -1, 因为Mono的bug，直接-2值会错误
            // 所以要调用 ILIntepreter.Minus
            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);

            // 这里是将栈指针上的值转换成 object，如果是基础类型可直接通过 ptr->Value 和 ptr->ValueLow 访问到值
            // 具体请看 ILRuntime 实现原理文档
            object message = typeof(object).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            // 所有非基础类型都得调用 Free 来释放托管堆栈
            __intp.Free(ptr_of_this_method);

            // 在真实调用 Debug.Log 前，我们先获取 DLL 内的堆栈
            var stacktrace = __domain.DebugService.GetStackTrace(__intp);

            // 我们在输出信息后面加上 DLL 堆栈
            UnityEngine.Debug.Log(message + "\n" + stacktrace);

            return __ret;
        }
        
        private static unsafe StackObject* LogWarning_01(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj) {
            AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Object @message = (System.Object)typeof(System.Object).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            // 在真实调用 Debug.Log 前，我们先获取 DLL 内的堆栈
            var stacktrace = __domain.DebugService.GetStackTrace(__intp);

            // 我们在输出信息后面加上 DLL 堆栈
            Debug.LogWarning($"{message}\n{stacktrace}");

            return __ret;
        }
        
        private static unsafe StackObject* LogError_01(ILIntepreter __intp, StackObject* __esp, IList<object> __mStack, CLRMethod __method, bool isNewObj) {
            AppDomain __domain = __intp.AppDomain;
            StackObject* ptr_of_this_method;
            StackObject* __ret = ILIntepreter.Minus(__esp, 1);

            ptr_of_this_method = ILIntepreter.Minus(__esp, 1);
            System.Object @message = (System.Object)typeof(System.Object).CheckCLRTypes(StackObject.ToObject(ptr_of_this_method, __domain, __mStack));
            __intp.Free(ptr_of_this_method);

            // 在真实调用 Debug.Log 前，我们先获取 DLL 内的堆栈
            var stacktrace = __domain.DebugService.GetStackTrace(__intp);

            // 我们在输出信息后面加上 DLL 堆栈
            Debug.LogError($"{message}\n{stacktrace}");

            return __ret;
        }
    }
}
