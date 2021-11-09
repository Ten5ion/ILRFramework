#if UNITY_EDITOR

using UnityEditor;
using System.IO;
using com.ilrframework.Runtime;
using ILRuntime.Runtime.Enviorment;
using UnityEngine;

[System.Reflection.Obfuscation(Exclude = true)]
public class ILRuntimeCrossBinding
{
    [MenuItem("ILRuntime/生成跨域继承适配器")]
    private static void GenerateCrossbindAdapter() {
        // 由于跨域继承特殊性太多，自动生成无法实现完全无副作用生成，所以这里提供的代码自动生成主要是给大家生成个初始模版，简化大家的工作
        // 大多数情况直接使用自动生成的模版即可，如果遇到问题可以手动去修改生成后的文件，因此这里需要大家自行处理是否覆盖的问题
        
        // --------- 每次执行脚本前，只需在这里修改想要适配的基类---------
        var baseClassType = typeof(GameObject);
        // --------- 每次执行脚本前，只需在这里修改想要适配的基类---------
        
        var outputPath = Path.Combine(ILRConfig.ADAPTOR_PATH, $"{baseClassType.Name}Adaptor.cs");
        var nameSpace = "com.ilrframework.Runtime.Adaptor";
        using (var sw = new StreamWriter(outputPath)) {
            sw.WriteLine(CrossBindingCodeGenerator.GenerateCrossBindingAdapterCode(baseClassType, nameSpace));
        }
        AssetDatabase.Refresh();
    }
}

#endif
