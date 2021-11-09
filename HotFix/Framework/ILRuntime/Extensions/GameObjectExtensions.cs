using System;
using HotFix.Framework.ILRuntime.Core;
using UnityEngine;

namespace HotFix.Framework.ILRuntime.Extensions
{
    public static class GameObjectExtensions
    {
        public static T AddILRBehaviour<T>(this GameObject go, bool manually = true) where T : ILRBehaviour {
            var behaviour = go.GetILRBehaviour<T>();
            if (behaviour != null) {
                Debug.LogError($"{go.name} already have {typeof(T)}");
                return behaviour;
            }
            
            var newBehaviour = Activator.CreateInstance<T>();
            newBehaviour.AttachGameObject(go, manually);
            return newBehaviour;
        }
        
        public static ILRBehaviour AddILRBehaviour(this GameObject go, Type t, bool manually = true) {
            var behaviour = go.GetILRBehaviour(t);
            if (behaviour != null) {
                if (manually) {
                    Debug.LogError($"{go.name} already have {t}");
                }
                return behaviour;
            }
            
            var newBehaviour = (ILRBehaviour) Activator.CreateInstance(t);
            newBehaviour.AttachGameObject(go, manually);
            return newBehaviour;
        }

        public static ILRBehaviour GetILRBehaviour(this GameObject go) {
            var script = ILRBehaviour.GetILRBehaviourInGameObject(go);
            return script;
        }
        
        public static T GetILRBehaviour<T>(this GameObject go) where T : ILRBehaviour {
            var script = ILRBehaviour.FindILRBehaviourInGameObject<T>(go);
            return script;
        }
        
        public static ILRBehaviour GetILRBehaviour(this GameObject go, Type t) {
            var script = ILRBehaviour.FindILRBehaviourInGameObject(go, t);
            return script;
        }
        
        public static bool RemoveILRBehaviour<T>(this GameObject go) where T : ILRBehaviour {
            var script = go.GetILRBehaviour<T>();
            if (script != null) {
                script.Destroy();
                return true;
            }

            return false;
        }
    }
}