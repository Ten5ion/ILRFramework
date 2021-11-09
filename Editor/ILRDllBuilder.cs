using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using UnityEditor;
using UnityEngine;

namespace com.ilrframework.Editor
{
    public class ILRDllBuilder
    {
        public enum BuildType
        {
            Debug,
            Release
        }
        
        public enum Platform
        {
            iOS,
            Android
        }
        
        // [InitializeOnLoadMethod]
        [MenuItem("ILRuntime/Compile")]
        public static void Compile() {
            var buildType = BuildType.Debug;
            var platform = Platform.iOS;
// #if DEBUG
//             buildType = BuildType.Debug;
// #else
            buildType = BuildType.Release;
// #endif
            
#if UNITY_IPHONE
            platform = Platform.iOS;
#elif UNITY_ANDROID
            platform = Platform.Android;
#endif
            
            BuildILRDll(buildType, platform);
        }

        public static void BuildILRDll(BuildType buildType, Platform platform) {
            var csProjectPath = Path.Combine(Application.dataPath, "..", "HotFix", "HotFix.csproj");
            var defines = new List<string>();
            var refDlls = new List<string>();
            var csFiles = new List<string>();
            LoadHotFixProject(csProjectPath, buildType, platform, ref defines, ref refDlls, ref csFiles);
            
            var csFileList = new List<Microsoft.CodeAnalysis.SyntaxTree>();
            var dllFileList = new List<MetadataReference>();
            var options = new CSharpParseOptions(LanguageVersion.Latest, preprocessorSymbols: defines);
            
            // Add Dlls
            foreach (var dllPath in refDlls) {
                var corPath = Path.Combine("HotFix", dllPath);
                var dll = MetadataReference.CreateFromFile(corPath);
                if (dll != null) {
                    dllFileList.Add(dll);
                }
            }

            // Add global Dlls
            var globalDlls = GetGlobalReferences();
            dllFileList.AddRange(globalDlls);
            
            foreach (var filePath in csFiles) {
                var path = Path.Combine("HotFix", filePath);
                if (!File.Exists(path)) continue;
                var str = ReadTextFile(path);
                var syntaxTree = CSharpSyntaxTree.ParseText(str, options, path, Encoding.UTF8);
                if (syntaxTree != null) {
                    csFileList.Add(syntaxTree);
                }
            }

            var optimizationLevel = OptimizationLevel.Debug;
            if (buildType == BuildType.Release) {
                optimizationLevel = OptimizationLevel.Release;
            }
            
            var compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: optimizationLevel, warningLevel: 4, allowUnsafe: true);

            var compileDllPath = ILRDllHandler.SlnBuildDllFullPath;
            var compilePdbPath = ILRDllHandler.SlnBuildPdbFullPath;
            
            var assemblyName = Path.GetFileNameWithoutExtension(compileDllPath);
            
            var compilation = CSharpCompilation.Create(assemblyName, csFileList, dllFileList, compilationOptions);
            
            EmitResult result;
            var emitOptions = new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb, pdbFilePath: compilePdbPath);
            using (var dllStream = new MemoryStream()) {
                using (var pdbStream = new MemoryStream()) {
                    result = compilation.Emit(dllStream, pdbStream, options: emitOptions);
                    WriteTextFile(compileDllPath, dllStream.GetBuffer());
                    WriteTextFile(compilePdbPath, pdbStream.GetBuffer());
                }
            }
            
            if (result.Success) {
                Debug.Log("Compile HotFix Success");
            
                ILRDllHandler.HandleHotFixDllAndPdb(File.Exists(compileDllPath), File.Exists(compilePdbPath));
            
                AssetDatabase.Refresh();
            } else {
                var failureList = (from diagnostic in result.Diagnostics where diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error select diagnostic).ToList();
                foreach (var item in failureList) {
                    Debug.LogError(item.ToString());
                }
            }
        }

        private static void LoadHotFixProject(string csProjectPath, BuildType buildType, Platform platform, ref List<string> defines, ref List<string> refDlls, ref List<string> csFiles) {
            var refProjs = new List<string>();
            
            var document = new XmlDocument();
            document.Load(csProjectPath);
            
            XmlNode projNode = null;
            foreach (XmlNode node in document.ChildNodes) {
                if (node.Name == "Project") {
                    projNode = node;
                    break;
                }
            }
            
            foreach (XmlNode node in projNode.ChildNodes) {
                if (node.Name == "PropertyGroup") {
                    // Add Defines
                    foreach (XmlNode childNode in node.ChildNodes) {
                        if (childNode.Name == "DefineConstants") {
                            var define = childNode.InnerText;
                            defines.AddRange(define.Split(';'));
                        }
                    }
                }
                if (node.Name == "ItemGroup") {
                    foreach (XmlNode childNode in node.ChildNodes) {
                        // Add reference Dlls
                        if (childNode.Name == "Reference" && childNode.FirstChild != null) {
                            var dllFilePath = childNode.FirstChild.InnerText.Replace("\\", "/");
                            refDlls.Add(dllFilePath);
                        }
                        // Add cs Files
                        if (childNode.Name == "Compile" && childNode.Attributes != null && childNode.Attributes.Count > 0) {
                            var csFilePath = childNode.Attributes[0].Value.Replace("\\", "/");
                            csFiles.Add(csFilePath);
                        }
                        // Add reference projects
                        if (childNode.Name == "ProjectReference" && childNode.Attributes != null && childNode.Attributes.Count > 0) {
                            var  csprojFilePath = childNode.Attributes[0].Value;
                            refProjs.Add(csprojFilePath);
                        }
                    }
                }
            }
            
            foreach (var refProj in refProjs) {
                if (refProj.ToLower().Contains("editor")) {
                    continue;
                }
                LoadHotFixProject(refProj, buildType, platform, ref defines, ref refDlls, ref csFiles);
                
                var dllFilePath = "Library/ScriptAssemblies/" + refProj.Replace(".csproj", ".dll");
                if (File.Exists(dllFilePath)) {
                    refDlls.Add(dllFilePath);
                }
            }
            
            defines = defines.Distinct().ToList();
            
            for (var i = defines.Count - 1; i >= 0; i--) {
                if (defines[i].Equals("UNITY_EDITOR")) {
                    defines.RemoveAt(i);
                    continue;
                }
                
                if (platform == Platform.iOS && defines[i].Equals("UNITY_ANDROID")) {
                    defines.RemoveAt(i);
                    continue;
                }
                
                if (platform == Platform.Android && defines[i].Equals("UNITY_IPHONE")) {
                    defines.RemoveAt(i);
                    continue;
                }

                if (buildType == BuildType.Debug && defines[i].Equals("RELEASE")) {
                    defines.RemoveAt(i);
                    continue;
                }
                
                if (buildType == BuildType.Release && defines[i].Equals("DEBUG")) {
                    defines.RemoveAt(i);
                    continue;
                }
            }
            
            refDlls = refDlls.Distinct().ToList();
        }
        
        private static string ReadTextFile(string path) {
            if (File.Exists(path)) {
                using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                    var bytes = new byte[fileStream.Length];
                    fileStream.Read(bytes, 0, bytes.Length);
                    var str = System.Text.Encoding.UTF8.GetString(bytes);
                    return str;
                }
            }
            return null;
        }

        private static void WriteTextFile(string path, byte[] bytes) {
            using (var fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write)) {
                fileStream.Write(bytes, 0, bytes.Length);
            }
        }
        
        private static List<MetadataReference> GetGlobalReferences() {
            var dllFileList = new List<MetadataReference>();
            
            var assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location);
            
            var assemblies = new [] {
                Path.Combine(assemblyPath, "mscorlib.dll"),
                Path.Combine(assemblyPath, "System.dll"),
                Path.Combine(assemblyPath, "System.Core.dll"),
                Path.Combine(assemblyPath, "System.Xml.Linq.dll"),
                Path.Combine(assemblyPath, "System.Data.DataSetExtensions.dll"),
                Path.Combine(assemblyPath, "System.Data.dll"),
                Path.Combine(assemblyPath, "System.Xml.dll"),
            };
            
            for (var i = 0; i < assemblies.Length; i++) {
                var assembly = assemblies[i];
                var dll = MetadataReference.CreateFromFile(assembly);
                if (dll != null) {
                    dllFileList.Add(dll);
                }
            }
            
            return dllFileList;
        }
    }
}