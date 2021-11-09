using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using com.aaframework.Runtime;
using com.ilrframework.Runtime.Bindings.CLRBindings;
using ILRuntime.Mono.Cecil.Pdb;
using UnityEngine;
using AppDomain = ILRuntime.Runtime.Enviorment.AppDomain;

namespace com.ilrframework.Runtime
{
    public class ILRApp : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        private static void RuntimeInitializeOnLoad() {
            var go = new GameObject("ILRApp", typeof(ILRApp));
            DontDestroyOnLoad(go);
        }
        
        public static ILRApp Instance { get; private set; }
        
        private enum LoadState
        {
            NotLoad,
            Loading,
            Loaded,
        }
        
        public static bool UNITY_EDITOR { get; private set; }
        public static bool DEBUG { get; private set; }

        public ILRConfigurator Configurator { get; private set; }
        
        #region MonoBehaviour

        private void Awake() {
            Instance = this;
            
#if UNITY_EDITOR
            UNITY_EDITOR = true;
#else
            UNITY_EDITOR = false;
#endif

#if DEBUG
            DEBUG = true;
#else
            DEBUG = false;
#endif
            
            InitConfigurator();
        }

        private async void Start() {
            Configurator.OnAADownloadBefore();
            
            var info = await AADownloader.Instance.CheckCatalogUpdate();
            if (info.NeedUpdate) {
                Configurator.OnAANeedDownload(info, () => {
                    Configurator.OnAADownloadAfter();
                    Configurator.OnStartLoading();
                });
            }
            else {
                await Configurator.OnStartLoading();
            }
        }

        #endregion

        #region ILRConfigurator

        private void InitConfigurator() {
            var clazzType = typeof(ILRConfigurator);
            
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies) {
                foreach (var type in assembly.GetTypes()) {
                    if (type.IsClass && !type.IsAbstract && clazzType.IsAssignableFrom(type)) {
                        Configurator = (ILRConfigurator)Activator.CreateInstance(type);
                        return;
                    }
                }
            }

            throw new NotImplementedException("ILRConfigurator Not Implemented.");
        }

        #endregion

        #region HotFix

        private static LoadState _hotFixAssemblyLoadState = LoadState.NotLoad;

        private MemoryStream _dllStream;
        private MemoryStream _pdbStream;
    
        // AppDomain 是 ILRuntime 的入口，最好是在一个单例类中保存，整个游戏全局就一个
        private AppDomain _domain = null;
        public AppDomain Domain {
            get {
                if (_domain == null) {
                    if (Application.isPlaying) {
                        throw new Exception("请先确保执行了 LoadHotFixAssembly");
                    }
                    else {
                        throw new Exception("请运行游戏后再调用");
                    }
                }
                return _domain;
            }
            private set => _domain = value;
        }

        public void Dispose() {
            CLRBindings.Shutdown(Domain);
            
            if (_dllStream != null) _dllStream.Close();
            if (_pdbStream != null) _pdbStream.Close();
            _dllStream = null;
            _pdbStream = null;

            Domain = null;
        }

        /// <summary>
        /// 加载热更代码
        /// </summary>
        public async Task LoadHotFixAssembly() {
            if (_hotFixAssemblyLoadState != LoadState.NotLoad) {
                var str = _hotFixAssemblyLoadState == LoadState.Loaded ? "Hotfix 已加载" : "已有一个 Hotfix 正在加载中";
                Debug.LogError($"{str}，请检查是否有多处正在尝试加载");
                return;
            }

            _hotFixAssemblyLoadState = LoadState.Loading;
        
            // 首先实例化 ILRuntime 的 AppDomain，AppDomain是一个应用程序域，每个AppDomain都是一个独立的沙盒
            Domain = new AppDomain();
        
            // 加载DLL，这个 DLL 文件是直接编译 HotFix_Project.sln 生成的
            var dll = await LoadDll(ILRConfig.DllPath);
            _dllStream = new MemoryStream(dll);
            
#if DEBUG
            // PDB 文件是调试数据库，如需要在日志中显示报错的行号，则必须提供 PDB 文件
            // 不过由于会额外耗用内存，正式发布时请将 PDB 去掉，下面 LoadAssembly 的时候 pdb 传 null 即可
            var pdb = await LoadDll(ILRConfig.PdbPath);
            _pdbStream = new MemoryStream(pdb);

            try {
                Domain.LoadAssembly(_dllStream, _pdbStream, new PdbReaderProvider());
            } catch {
                Debug.LogError($"加载热更DLL失败，请确保已经通过 HotFix.sln 编译过热更 DLL");
            }
#else
            try {
                Domain.LoadAssembly(_dllStream, null, new PdbReaderProvider());
            } catch {
                Debug.LogError($"加载热更DLL失败，请确保已经通过 HotFix.sln 编译过热更 DLL");
            }
#endif
            
            InitializeILRuntime();
            OnHotFixLoaded();
        }
        
        public static async Task<byte[]> LoadDll(string path) {
// #if UNITY_ANDROID
// #else
//             path = "file://" + path;
// #endif
//             var uri = new System.Uri(path);
//             
//             var getRequest = UnityWebRequest.Get(uri.AbsoluteUri);
//             await getRequest.SendWebRequest();
//             var data = getRequest.downloadHandler.data;
//             return data;

#if UNITY_EDITOR
            var asset = await AAManager.Instance.LoadAssetAsync<TextAsset>(path);
#else
            var asset = await AAManager.Instance.LoadAssetAsync<TextAsset>(path);
#endif
            return ILREncrypter.DecryptHotFixBytes(asset.bytes);
        }
        
        private void InitializeILRuntime() {
#if DEBUG && (UNITY_EDITOR || UNITY_ANDROID || UNITY_IPHONE)
            // 由于 Unity 的 Profiler 接口只允许在主线程使用，为了避免出异常，需要告诉 ILRuntime 主线程的线程 ID 才能正确将函数运行耗时报告给 Profiler
            Domain.UnityMainThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
            
            Domain.DebugService.StartDebugService(56000);
             
//             // 获取异常打印调用栈
//             // 注意只在 Editor 下生效，此处不能作为错误日志上报系统的来源
//             Domain.DebugService.OnBreakPoint += s => {
//                 var sb = new StringBuilder();
//
//                 var ds = Domain.DebugService;
//                 var t = ds.GetType();
//                 
//                 var curBreakpointFiled = t.GetField("curBreakpoint", BindingFlags.Instance | BindingFlags.Default | BindingFlags.Public | BindingFlags.NonPublic);
//                 var curBreakpoint = curBreakpointFiled.GetValue(ds);
//                 var breakpointType = curBreakpoint.GetType();
//
//                 var excProperty = breakpointType.GetProperty("Exception", BindingFlags.Instance | BindingFlags.Default | BindingFlags.Public | BindingFlags.NonPublic);
//                 var exception = (Exception) excProperty.GetValue(curBreakpoint);
//                 sb.AppendLine($"{exception.GetType().Name}: {exception.Message}");
//                 
//                 var intepreterProperty = breakpointType.GetProperty("Interpreter",
//                     BindingFlags.Instance | BindingFlags.Default | BindingFlags.Public | BindingFlags.NonPublic);
//                 var intepreter = (ILIntepreter) intepreterProperty.GetValue(curBreakpoint);
//                 var stackTrace = ds.GetStackTrace(intepreter);
//                 sb.AppendLine(stackTrace);
//                 
//                 Debug.LogError(sb.ToString());
//             };
#endif
            
            // 获取异常打印调用栈
            Domain.DebugService.OnILRuntimeException += s => {
                var str = $"ILException:\n{s}";
                Debug.LogError(str);
                ILRExceptionPanel.Show(str);
#if !DEBUG
                IncLogger.Instance.PutLog(IncLogTopic.Log, str, IncLogger.MessageLevel.Error);
#endif
            };
            
            // 这里做一些 ILRuntime 的注册
            ILRRegister.RegisterAll(Domain);
        }

        private void OnHotFixLoaded() {
            _hotFixAssemblyLoadState = LoadState.Loaded;
            
            Debug.Log("加载 HotFix 成功");
            
            // 反射 HotFix 中的定义，缓存所有继承 ILRBehaviour 的类名
            LoadHotFixIlrBehaviours();

            // 进入热更工程的入口场景
            EntryScene();
        }
        
        /// <summary>
        /// 缓存所有继承 ILRBehaviour 的类名
        /// </summary>
        private void LoadHotFixIlrBehaviours() {
            ILRComponent.AllIlrBehaviours.Clear();
            
            var hotFixTypes = Domain.LoadedTypes;
            var ilrBehaviourKey = "HotFix.Framework.ILRuntime.Core.ILRBehaviour";
            var exist = hotFixTypes.TryGetValue(ilrBehaviourKey, out _);
            if (!exist) {
                throw new Exception($"{ilrBehaviourKey} not exist in 'Domain.LoadedTypes'");
            }
                
            var keys = hotFixTypes.Keys.ToArray();
            var ilrBehaviour = hotFixTypes[ilrBehaviourKey].ReflectionType;
            foreach (var key in keys) {
                var t = hotFixTypes[key].ReflectionType;
                if (t.IsClass && !t.IsAbstract && IsSubclassOf(t, ilrBehaviour)) {
                    ILRComponent.AllIlrBehaviours.Add(t.FullName);
                }
            }
        }

        private bool IsSubclassOf(Type child, Type parent) {
            var ret = false;
            
            var t = child;
            while (!ret && t.BaseType != null) {
                t = t.BaseType;
                ret = t == parent;
            }

            return ret;
        }

        private async void EntryScene() {
            // await Task.Delay(5000);
            
            const string className = "HotFix.Framework.ILRuntime.Core.ILREntry";
            const string funcName = "EnterILRuntime";
            
            var type = Domain.LoadedTypes[className];
            var method = type.GetMethod(funcName, 1);
            using (var ctx = Domain.BeginInvoke(method)) {
                ctx.PushObject($"{Configurator.EntryScenePath()}");
                ctx.Invoke();
            }
        }

        #endregion
    }
}
