using System;
using System.Collections.Generic;
using System.Reflection;
using com.ilrframework.Runtime;
using HotFix.Framework.ILRuntime.Extensions;
using UnityEngine;

namespace HotFix.Framework.ILRuntime.Core
{
    public class ILRComponentHook
    {
        // 找出所有 magic methods（生命周期等函数）
        public static readonly string[] MagicMethodNames = new[] {
            "Awake", "OnEnable", "Start", "Update", "OnDisable", "OnDestroy", "OnTransformParentChanged"
        };
        
        public static readonly Dictionary<string, List<string>> MagicMethodInfos =
            new Dictionary<string, List<string>>();

        public static void InitMagicMethodInfos() {
            ILRComponent.ComponentAwakeAction += ComponentAwakeAction;
            
            MagicMethodInfos.Clear();
            
            var ilrBehaviours = ILRComponent.AllIlrBehaviours;

            var ilrBehaviourBase = typeof(ILRBehaviour);
            
            var len = ilrBehaviours.Count;
            var mLength = MagicMethodNames.Length;
            for (var i = 0; i < len; i++) {
                var ilrBehaviour = ilrBehaviours[i];
                var declaredMethods = new List<string>();
                MagicMethodInfos.Add(ilrBehaviour, declaredMethods);
                
                var t = Type.GetType(ilrBehaviour);

                for (var j = 0; j < mLength; j++) {
                    var methodName = MagicMethodNames[j];
                    var clazz = t;
                    while (clazz != null && clazz != ilrBehaviourBase) {
                        var methodInfo = clazz.GetMethod(methodName);
                        if (methodInfo != null) {
                            declaredMethods.Add(methodName);
                            break;
                        }
                        clazz = clazz.BaseType;
                    }
                }
            }
        }
        
        private static void ComponentAwakeAction(ILRComponent ilrComponent) {
            if (string.IsNullOrEmpty(ilrComponent.ilrBehaviour)) return;
            
            var t = Type.GetType(ilrComponent.ilrBehaviour);
            if (t == null) {
                Debug.LogError($"'{ilrComponent.ilrBehaviour}' not exist in {ilrComponent.gameObject.name}. Please make sure to fill in correctly.");
                return;
            }

            var go = ilrComponent.gameObject;
            go.AddILRBehaviour(t, false);
        }
    }
}