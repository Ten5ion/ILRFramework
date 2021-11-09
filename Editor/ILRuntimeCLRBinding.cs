#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using com.ilrframework.Runtime;
using ILRuntime.Runtime.Enviorment;
using UnityEngine;

[System.Reflection.Obfuscation(Exclude = true)]
public class ILRuntimeCLRBinding
{
    [MenuItem("ILRuntime/通过自动分析热更DLL生成CLR绑定")]
    public static async void GenerateCLRBindingByAnalysis()
    {
        // 用新的分析热更 dll 调用引用来生成绑定代码
        var domain = new AppDomain();
        
        var dllBytes = await ILRApp.LoadDll(ILRConfig.DllPath);
        
        using (var stream = new MemoryStream(dllBytes)) {
            domain.LoadAssembly(stream);

            // Crossbind Adapter is needed to generate the correct binding code
            InitILRuntime(domain);
            ILRuntime.Runtime.CLRBinding.BindingCodeGenerator.GenerateBindingCode(domain, ILRConfig.BINDING_PATH);

            AssetDatabase.Refresh();
        
            Debug.Log("已生成绑定代码");
        }
    }

    private static void InitILRuntime(AppDomain domain) {
        // 这里需要注册所有热更DLL中用到的跨域继承Adapter，否则无法正确抓取引
        
        ILRRegister.RegisterCrossBindingAdaptors(domain);
    }
}
#endif
