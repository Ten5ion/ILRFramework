using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;

namespace HotFix.Game.Common
{
    public interface IUpdatable
    {
        void OnUpdate(float dt);
    }
    
    public class LoopSystem : GenericSingleton<LoopSystem>
    {
        private readonly List<IUpdatable> _updaters = new List<IUpdatable>();
        private event Action<float> OnUpdateListeners;

        public void Init() {
            _updaters.Clear();

            RegisterUnityEnginePlayerLoopUpdate();
        }
        
        public void AddUpdatable(IUpdatable u) {
            _updaters.Add(u);
        }
        
        public void RemoveUpdatable(IUpdatable u) {
            _updaters.Remove(u);
        }

        private void BroadcastUpdate(float dt) {
            var len = _updaters.Count;
            for (var i = 0; i < len; i++) {
                _updaters[i].OnUpdate(dt);
            }
        }

        public void AddUpdateListener(Action<float> listener) {
            OnUpdateListeners += listener;
        }

        public void RemoveUpdateListener(Action<float> listener) {
            OnUpdateListeners -= listener;
        }

        private void RegisterUnityEnginePlayerLoopUpdate() {
            var updateType = typeof(UnityEngine.PlayerLoop.Update);
            
            var defaultPlayerLoop = PlayerLoop.GetDefaultPlayerLoop();
            for (var i = 0; i < defaultPlayerLoop.subSystemList.Length; i++) {
                var subSystem = defaultPlayerLoop.subSystemList[i];
                if (subSystem.type == updateType) {
                    subSystem.updateDelegate = OnUnityEnginePlayerLoopUpdate;
                    defaultPlayerLoop.subSystemList[i] = subSystem;
                    break;
                }
            }
            
            PlayerLoop.SetPlayerLoop(defaultPlayerLoop);
        }

        private void OnUnityEnginePlayerLoopUpdate() {
            var dt = Time.deltaTime;
            
            BroadcastUpdate(dt);

            if (OnUpdateListeners != null) OnUpdateListeners(dt);
        }
    }
}