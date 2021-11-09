using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using com.aaframework.Runtime;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace com.ilrframework.Runtime
{
    /// <summary>
    /// 建议所有使用预制池的 prefab 挂载实现 IPoolComponent 接口的组件，以便 「初始化」 和 「重置」 工作
    /// </summary>
    public interface IPoolComponent
    {
        void OnSpawn();
        void OnDespawn();
    }
    
    #region PrefabPoolSystem
    
    /// <summary>
    /// 预制池
    /// 
    /// 注意:
    /// > 请遵循 Spawn 和 Despawn 配对调用
    /// > 在父节点 Destroy 前调用 Despawn 回收到池中，否则会削弱对象池的效能（父节点 Destroy 会带动子节点 Destroy
    ///   届时池子会被迫删除缓存，等再问池子 Spawn 的时候相当于增加了重新实例化的概率）
    /// > 尽量不要对 Spawn 出的 GameObject 进行 Destroy、DestroyImmediate、ReleaseInstance(Addressables) 等手动销毁的操作
    ///  （父节点 Destroy 前请及时调用 Despawn 以回收）。确实有需求的话，调用 DestroyAll 统一对池做清空处理
    /// > 切换场景时会自动清理池子
    /// 
    /// 使用方法：
    /// Step 1: 预制体的自定义 Component 实现 IPoolComponent
    /// Step 2: 通常在 loading 的时候调用 Prespawn 预生成一定数量的 GameObject
    /// Step 3: 在需要时调用 Spawn，对象收到 OnSpawn 时务必进行初始化工作
    /// Step 4: 不用的时候调用 Despawn 进行回收，对象收到 OnDespawn 时务必进行重置工作
    /// Step 5: 仔细阅读上面的注意事项，尤其不要手动调用 Destroy 相关方法
    /// 
    /// </summary>
    public sealed class ILRPrefabPoolSystem : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        private static void RuntimeInitializeOnLoad()
        {
            // 初始默认加载脚本
            var go = new GameObject("PrefabPoolSystem", typeof(ILRPrefabPoolSystem));
            DontDestroyOnLoad(go);
            Instance = go.GetComponent<ILRPrefabPoolSystem>();
            
            SceneManager.sceneUnloaded += delegate(Scene scene) {
                Instance.Reset();
            };
        }
        
        public static ILRPrefabPoolSystem Instance;
        
        // Key: Prefab asset path -> Value: Pool
        private readonly Dictionary<string, PrefabPool> _prefabPathPoolDic = new Dictionary<string, PrefabPool>();
        
        // Key: Prefab asset reference -> Value: Pool
        private readonly Dictionary<AssetReference, PrefabPool> _prefabReferencePoolDic = new Dictionary<AssetReference, PrefabPool>();
        
        // Key: Instantiated GameObject -> Value: Pool
        private readonly Dictionary<GameObject, PrefabPool> _despawnDic = new Dictionary<GameObject, PrefabPool>();

        /// <summary>
        /// 预生成一定数量的 Prefab 实例
        /// </summary>
        /// <param name="prefabAssetPath">prefab 资源的相对路径</param>
        /// <param name="spawnNum">预生产数量</param>
        /// <param name="persist">是否持久，即 DontDestroyOnLoad</param>
        public async Task Prespawn(string prefabAssetPath, int spawnNum, bool persist = false) {
            var pool = GetPool(prefabAssetPath);
            await pool.Prespawn(prefabAssetPath, spawnNum);
        }

        /// <summary>
        /// 预生成一定数量的 Prefab 实例
        /// </summary>
        /// <param name="prefabReference">prefab</param>
        /// <param name="spawnNum">预生产数量</param>
        /// <param name="persist">是否持久，即 DontDestroyOnLoad</param>
        public async Task Prespawn(AssetReference prefabReference, int spawnNum, bool persist = false) {
            var pool = GetPool(prefabReference);
            await pool.Prespawn(prefabReference, spawnNum);
        }

        /// <summary>
        /// 生产一个 GameObject
        /// </summary>
        /// <param name="prefabAssetPath">prefab 资源的相对路径</param>
        /// <returns></returns>
        public async Task<GameObject> Spawn(string prefabAssetPath) {
            var pool = GetPool(prefabAssetPath);
            var go = await pool.Spawn(prefabAssetPath);
            _despawnDic[go] = pool;
            return go;
        }

        /// <summary>
        /// 生产一个 GameObject
        /// </summary>
        /// <param name="prefabReference">prefab</param>
        /// <returns></returns>
        public async Task<GameObject> Spawn(AssetReference prefabReference) {
            var pool = GetPool(prefabReference);
            var go = await pool.Spawn(prefabReference);
            _despawnDic[go] = pool;
            return go;
        }

        /// <summary>
        /// 回收
        /// </summary>
        /// <param name="go">GameObject</param>
        /// <returns></returns>
        public bool Despawn(GameObject go) {
            var exist = _despawnDic.TryGetValue(go, out var pool);
            if (!exist) {
                return false;
            }

            if (pool.Despawn(go)) {
                _despawnDic.Remove(go);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 销毁对象池对象并清空对象池
        /// 注意！！！此操作较耗时，请在合适的时机调用
        /// </summary>
        /// <param name="prefabAssetPath"></param>
        public async Task DestroyAll(string prefabAssetPath) {
            var pool = GetPool(prefabAssetPath);
            await PoolDestroyAll(pool);
        }

        /// <summary>
        /// 销毁对象池对象并清空对象池
        /// 注意！！！此操作较耗时，请在合适的时机调用
        /// </summary>
        public async Task DestroyAll(AssetReference prefab) {
            var pool = GetPool(prefab);
            await PoolDestroyAll(pool);
        }

        private async Task PoolDestroyAll(PrefabPool pool) {
            var removes = new List<GameObject>();
            foreach (var pair in _despawnDic) {
                if (pair.Value == pool) removes.Add(pair.Key);
            }
            for (var i = 0; i < removes.Count; i++) {
                _despawnDic.Remove(removes[i]);
            }
            await pool.DestroyAll();
        }

        private void OnDestroyed(GameObject go) {
            _despawnDic.Remove(go);
        }

        /// <summary>
        /// 切换场景时调用
        /// </summary>
        private void Reset() {
            _despawnDic.Clear();
            
            foreach (var pair in _prefabPathPoolDic) {
                pair.Value.Reset();
            }
            
            foreach (var pair in _prefabReferencePoolDic) {
                pair.Value.Reset();
            }
        }

        private PrefabPool GetPool(string prefabAssetPath) {
            var exist = _prefabPathPoolDic.TryGetValue(prefabAssetPath, out var pool);
            if (!exist) {
                pool = new PrefabPool();
                _prefabPathPoolDic[prefabAssetPath] = pool;
            }

            return pool;
        }

        private PrefabPool GetPool(AssetReference prefabReference) {
            var exist = _prefabReferencePoolDic.TryGetValue(prefabReference, out var pool);
            if (!exist) {
                pool = new PrefabPool();
                _prefabReferencePoolDic[prefabReference] = pool;
            }

            return pool;
        }

        #region PrefabPoolGoData
        
        private struct PrefabPoolGoData
        {
            public GameObject GO;
            public IPoolComponent[] PoolComponents;
            public bool Persist;
            public PrefabPoolLifeCycleComponent LifeCycleComponent;
        }

        #endregion
        
        #region PrefabPool
        
        private class PrefabPool
        {
            /// <summary>
            /// 正在被使用的 GameObject
            /// </summary>
            private readonly Dictionary<GameObject, PrefabPoolGoData> _beingUsedGameObjects =
                new Dictionary<GameObject, PrefabPoolGoData>();

            /// <summary>
            /// 池内待命可用的 GameObject
            /// </summary>
            private readonly List<PrefabPoolGoData> _standbyGameObjects = new List<PrefabPoolGoData>();

            /// <summary>
            /// 预生成一定数量的 GameObject 实例到池中
            /// </summary>
            /// <param name="prefab">支持 string(预制体资源路径) 及 AssetReference 两种类型</param>
            /// <param name="spawnNum">预热数量</param>
            /// <param name="persist">是否持久，即 DontDestroyOnLoad</param>
            /// <typeparam name="T">支持 string(预制体资源路径) 及 AssetReference 两种类型</typeparam>
            public async Task Prespawn<T>(T prefab, int spawnNum, bool persist = false) {
                var tasks = new List<Task<GameObject>>();
                
                for (var i = 0; i < spawnNum; i++) {
                    tasks.Add(Spawn(prefab));
                }

                var results = await Task.WhenAll(tasks);
                for (var i = 0; i < results.Length; i++) {
                    Despawn(results[i]);
                }
            }

            /// <summary>
            /// 从池中生成一个 GameObject
            /// </summary>
            /// <param name="prefab"></param>
            /// <param name="persist"></param>
            /// <typeparam name="T"></typeparam>
            /// <returns></returns>
            /// <exception cref="Exception"></exception>
            public async Task<GameObject> Spawn<T>(T prefab, bool persist = false) {
                // 推迟一帧，因为有些 GameObject 在被直接或间接调用 Destroy 后还在destroy队列中没被真正销毁
                // 这时候从 _standbyGameObjects 中取出的对象并不为 null，这将导致刚从对象池中取出的对象在一帧就消失不见
                await WaitForEndOfFrameAsync();
                
                var data = new PrefabPoolGoData();

                var availableGo = false;
                while (!availableGo && _standbyGameObjects.Count > 0) {
                    data = _standbyGameObjects[0];
                    _standbyGameObjects.RemoveAt(0);
                    availableGo = data.GO != null;
                }
                
                if (!availableGo) {
                    GameObject go = null;
                    var paramType = prefab.GetType();
                    if (paramType == typeof(string)) {
                        go = await AAManager.Instance.InstantiateAsync(prefab as string);
                    } else if (paramType == typeof(AssetReference)) {
                        go = await AAManager.Instance.InstantiateAsync(prefab as AssetReference);
                    } else {
                        go = new GameObject();
                        const string errorMsg = "Spawn() only supports 'prefab asset path(string)' and 'AssetReference'";
#if UNITY_EDITOR
                        throw new Exception(errorMsg);
#else
                        Debug.LogError(errorMsg);
#endif
                    }

                    go.name = $"[Pool]{go.name}";
                    go.transform.localPosition = Vector3.zero;
                    go.transform.localScale = Vector3.one;

                    var lifeCycleComponent = go.GetComponent<PrefabPoolLifeCycleComponent>();
                    if (lifeCycleComponent == null) {
                        lifeCycleComponent = go.AddComponent<PrefabPoolLifeCycleComponent>();
                    }
                    
                    var poolComponents = go.GetComponents<IPoolComponent>();
                    data = new PrefabPoolGoData {
                        GO = go, PoolComponents = poolComponents, LifeCycleComponent = lifeCycleComponent,
                        Persist = persist
                    };
                    lifeCycleComponent.Data = data;
                    lifeCycleComponent.Pool = this;
                }
                
                data.GO.SetActive(true);
                for (var i = 0; i < data.PoolComponents.Length; i++) {
                    var comp = data.PoolComponents[i];
                    comp.OnSpawn();
                }
                _beingUsedGameObjects[data.GO] = data;

                return data.GO;
            }

            /// <summary>
            /// 回收 GameObject 到池中
            /// </summary>
            /// <param name="go"></param>
            /// <returns></returns>
            public bool Despawn(GameObject go) {
                var exist = _beingUsedGameObjects.TryGetValue(go, out var data);
                if (!exist) {
                    return false;
                }
                
                for (var i = 0; i < data.PoolComponents.Length; i++) {
                    var comp = data.PoolComponents[i];
                    comp.OnDespawn();
                }

                // 如果持久化，则移到 DontDestroyOnLoad 下
                if (data.Persist) {
                    data.GO.transform.SetParent(ILRPrefabPoolSystem.Instance.transform, true);
                }
                data.GO.SetActive(false);
                
                _beingUsedGameObjects.Remove(go);
                _standbyGameObjects.Add(data);

                return true;
            }

            /// <summary>
            /// 销毁对象池对象并清空对象池
            /// </summary>
            public async Task DestroyAll() {
                foreach (var pair in _beingUsedGameObjects) {
                    AAManager.Instance.ReleaseInstance(pair.Key);
                }
                
                for (var i = 0; i < _standbyGameObjects.Count; i++) {
                    var data = _standbyGameObjects[i];
                    AAManager.Instance.ReleaseInstance(data.GO);
                }
                
                Reset();
                
                await WaitForEndOfFrameAsync();
            }
            
            public void OnDestroyed(PrefabPoolGoData data) {
                _beingUsedGameObjects.Remove(data.GO);
                _standbyGameObjects.Remove(data);
            }

            public void Reset() {
                _beingUsedGameObjects.Clear();
                _standbyGameObjects.Clear();
            }

            /// <summary>
            /// 等待一帧结束（目的是等待Unity 的 Destroy 队列完成各对象销毁）
            /// </summary>
            private async Task WaitForEndOfFrameAsync() {
                var tcs = new TaskCompletionSource<bool>();
                ILRPrefabPoolSystem.Instance.StartCoroutine(WaitForEndOfFrame(tcs));
                await tcs.Task;
            }
            
            private IEnumerator WaitForEndOfFrame(TaskCompletionSource<bool> tcs) {
                yield return new WaitForEndOfFrame();
                tcs.SetResult(true);
            }
        }

        #endregion
        
        #region PrefabPoolLifeCycleComponent

        [DisallowMultipleComponent]
        private class PrefabPoolLifeCycleComponent : MonoBehaviour
        {
            public PrefabPool Pool;
            public PrefabPoolGoData Data;
            
            private void OnDestroy() {
                ILRPrefabPoolSystem.Instance.OnDestroyed(gameObject);
                Pool.OnDestroyed(Data);
            }
        }

        #endregion
    }

    #endregion
}
