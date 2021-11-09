using System;
using System.Collections.Generic;
using System.Reflection;
using com.ilrframework.Runtime;
using HotFix.Framework.ILRuntime.Extensions;
using HotFix.Game.Common;
using UnityEngine;
using UnityEngine.UI;

namespace HotFix.Framework.ILRuntime.Core
{
    public abstract class ILRBehaviour : IUpdatable
    {
        public GameObject gameObject { get; private set; }

        private Transform _transform;

        public Transform transform {
            get {
                if (_transform == null) {
                    Debug.LogWarning($"{GetType().FullName}: '_transform' is null");
                }
                return _transform;
            }
            private set => _transform = value;
        }

        private bool _attachManually = true;
        
        public void AttachGameObject(GameObject go, bool manually) {
            if (go == null) return;
            
            gameObject = go;
            transform = gameObject.transform;
            _attachManually = manually;
            
            _ilrComponent = gameObject.GetComponent<ILRComponent>();

            // Bind by inspector assign
            if (_ilrComponent != null) {
                BindFieldsByInspector();
                BindEventsByInspector();
            }

            // 脚本注册
            Register();

            // 放在 Bind 的后面，否则会找不到引用
            InitIlrComponent();
        }

        public T GetComponent<T>() {
            return gameObject.GetComponent<T>();
        }

        public T AddComponent<T>() where T : Component {
            return gameObject.AddComponent<T>();
        }

        public void Destroy() {
            if (_hasMagicMethodOnDestroy) {
                OnDestroy();
            }
            
            Destroyed();
        }

        private void Destroyed() {
            if (_hasMagicMethodUpdate) {
                LoopSystem.Instance.RemoveUpdatable(this);
            }
            
            Unregister();
            
            gameObject = null;
            transform = null;
        }
        
        // 业务层不要动这个！！！
        private static readonly Dictionary<GameObject, List<ILRBehaviour>> AllIlrBehaviours =
            new Dictionary<GameObject, List<ILRBehaviour>>();
        
        private void Register() {
            var exist = AllIlrBehaviours.TryGetValue(gameObject, out var list);
            if (!exist) {
                list = new List<ILRBehaviour>();
                AllIlrBehaviours.Add(gameObject, list);
            }
            list.Add(this);
        }
        
        private void Unregister() {
            if (gameObject == null) return;
            
            var exist = AllIlrBehaviours.TryGetValue(gameObject, out var list);
            if (!exist) return;
            
            list.Remove(this);
        }

        public static T FindILRBehaviourInGameObject<T>(GameObject go) where T : ILRBehaviour {
            var targetType = typeof(T);
            return (T) FindILRBehaviourInGameObject(go, targetType);
        }

        public static ILRBehaviour FindILRBehaviourInGameObject(GameObject go, Type t) {
            var exist = AllIlrBehaviours.TryGetValue(go, out var list);
            if (!exist) return null;

            var len = list.Count;
            for (var i = 0; i < len; i++) {
                var script = list[i];
                if (script.GetType() == t) {
                    return script;
                }
            }

            return null;
        }

        public static ILRBehaviour GetILRBehaviourInGameObject(GameObject go) {
            var exist = AllIlrBehaviours.TryGetValue(go, out var list);
            if (!exist) return null;

            if (list.Count > 0) {
                var script = list[0];
                return script;
            }

            return null;
        }

        #region Life Circle Methods

        protected virtual void Awake() { }
        protected virtual void OnEnable() { }
        protected virtual void Start() { }

        protected virtual void Update() { }

        protected virtual void OnDisable() { }
        protected virtual void OnDestroy() { }
        protected virtual void OnTransformParentChanged() { }

        protected virtual void OnPoolSpawn() { }
        
        protected virtual void OnPoolDespawn() { }

        #endregion

        #region ILRMonoBehaviour
        
        private ILRComponent _ilrComponent;

        private bool _hasMagicMethodUpdate = false;
        private bool _hasMagicMethodOnDestroy = false;
        
        private void InitIlrComponent() {
            if (_ilrComponent == null) {
                _ilrComponent = gameObject.AddComponent<ILRComponent>();
            }

            var t = GetType();
            var classFullName = t.FullName;
            var exist = ILRComponentHook.MagicMethodInfos.TryGetValue(classFullName, out var declaredMethods);
            if (!exist) {
                // 此时表示该类并没有声明任何 MagicMethod
                return;
            }

            if (_attachManually) {
                if (declaredMethods.Contains("Awake")) {
                    Awake();
                }
                
                if (declaredMethods.Contains("OnEnable")) {
                    OnEnable();
                }
            }

            var len = declaredMethods.Count;
            for (var i = 0; i < len; i++) {
                var methodName = declaredMethods[i];
                switch (methodName) {
                    case "Awake": {
                        _ilrComponent.AwakeAction += Awake;
                        break;
                    }
                    case "OnEnable": {
                        _ilrComponent.OnEnableAction += OnEnable;
                        break;
                    }
                    case "Start": {
                        _ilrComponent.StartAction += Start;
                        break;
                    }
                    case "Update": {
                        _hasMagicMethodUpdate = true;
                        LoopSystem.Instance.AddUpdatable(this);
                        break;
                    }
                    case "OnDisable": {
                        _ilrComponent.OnDisableAction += OnDisable;
                        break;
                    }
                    case "OnDestroy": {
                        _hasMagicMethodOnDestroy = true;
                        _ilrComponent.OnDestroyAction += Destroy;
                        break;
                    }
                    case "OnTransformParentChanged": {
                        _ilrComponent.OnTransformParentChangedAction += OnTransformParentChanged;
                        break;
                    }
                }
            }
        }
        
        #endregion

        #region Bind by attributes

        private bool BindFieldByGameObject(FieldInfo fieldInfo, GameObject target) {
            var fieldType = fieldInfo.FieldType;

            if (fieldType == typeof(GameObject)) {
                fieldInfo.SetValue(this, target);
                return true;
            }
            
            if (fieldType == typeof(Transform)) {
                fieldInfo.SetValue(this, target.transform);
                return true;
            }
            
            if (typeof(ILRBehaviour).IsAssignableFrom(fieldType)) {
                var ilrBehaviour = target.gameObject.GetILRBehaviour(fieldType);
                fieldInfo.SetValue(this, ilrBehaviour);
                return true;
            }
            
            if (typeof(Component).IsAssignableFrom(fieldType)) {
                var targetComponent = target.GetComponent(fieldType);
                if (targetComponent != null) {
                    fieldInfo.SetValue(this, targetComponent);
                    return true;
                }
                Debug.LogError($"Failed to bind {transform.gameObject.name}'s field <{fieldInfo.Name}>. Component<{fieldType}> not found.");
                return false;
            }

            return false;
        }

        /*
        private void BindFieldsByAttributes() {
            var t = GetType();
            var fieldInfos = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var len = fieldInfos.Length;
            var bindAttributeType = typeof(ILRComponentBindAttribute);
            
            for (var i = 0; i < len; i++) {
                var fieldInfo = fieldInfos[i];
                // 一定要调一下下面的 IsDefined，因为内部会触发 InitializeCustomAttribute，否则拿不到 customAttribute
                if (!fieldInfo.IsDefined(bindAttributeType)) continue;

                ILRComponentBindAttribute att = null;
                var atts = fieldInfo.GetCustomAttributes_Hack(bindAttributeType);
                for (var j = 0; j < atts.Length; j++) {
                    var attItem = (ILRComponentBindAttribute) atts[j];
                    if (attItem.ClassName.Length == 0 || attItem.ClassName == t.Name) {
                        att = attItem;
                        break;
                    }
                }
                
                if (att == null) {
                    Debug.LogError($"Failed to bind {transform.gameObject.name}'s field <{fieldInfo.Name}>. Can not get {bindAttributeType.Name}.");
                    continue;
                }

                var target = att.TransformPath.Length == 0 ? transform : transform.Find(att.TransformPath);
                if (target == null) {
                    if (att.WarningNotFound) {
                        Debug.LogError($"Failed to bind {transform.gameObject.name}'s field <{fieldInfo.Name}>. Transform path '{att.TransformPath}' dose not exist.");
                    }
                    continue;
                }
                
                var fieldType = fieldInfo.FieldType;

                var success = BindFieldByGameObject(fieldInfo, target.gameObject);
                if (!success) {
                    Debug.LogError($"Failed to bind {transform.gameObject.name}'s field <{fieldInfo.Name}>. Does not support type {fieldType}.");
                }
            }
        }
        */

        #endregion

        #region Bind by inspector assign

        private void BindFieldsByInspector() {
            var t = GetType();
            
            var len = _ilrComponent.ilrComponentFields.Count;
            for (var i = 0; i < len; i++) {
                var item = _ilrComponent.ilrComponentFields[i];
                
                var fieldInfo = t.GetField(item.field);
                if (fieldInfo == null) {
                    Debug.LogError($"Field '{item.field}' does not exist.");
                    continue;
                }

                var fieldType = fieldInfo.FieldType;
                if (typeof(ILRBehaviour).IsAssignableFrom(fieldType)) {
                    if (item.obj != null) {
                        var gameObject = ((ILRComponent)item.obj).gameObject;
                        var ilrBehaviour = gameObject.GetILRBehaviour(fieldType);
                        if (ilrBehaviour == null) {
                            ilrBehaviour = gameObject.AddILRBehaviour(fieldType, false);
                        }
                        fieldInfo.SetValue(this, ilrBehaviour);
                    }
                }
                else {
                    if (item.obj != null) {
                        fieldInfo.SetValue(this, item.obj);
                    }
                    else if (!string.IsNullOrEmpty(item.value)) {
                        if (fieldType == typeof(int)) {
                            fieldInfo.SetValue(this, Convert.ToInt32(item.value));
                        } else if (fieldType == typeof(bool)) {
                            fieldInfo.SetValue(this, Convert.ToInt32(item.value) == 1);
                        } else if (fieldType == typeof(float)) {
                            fieldInfo.SetValue(this, (float) Convert.ToDouble(item.value));
                        } else if (fieldType == typeof(double)) {
                            fieldInfo.SetValue(this, Convert.ToDouble(item.value));
                        } else if (fieldType == typeof(long)) {
                            fieldInfo.SetValue(this, Convert.ToInt64(item.value));
                        } else if (fieldType == typeof(string)) {
                            fieldInfo.SetValue(this, item.value);
                        } else if (fieldType == typeof(decimal)) {
                            fieldInfo.SetValue(this, Convert.ToDecimal(item.value));
                        } else if (fieldType == typeof(Color)) {
                            var strs = item.value.Split(',');
                            var color = new Color(
                                (float) Convert.ToDouble(strs[0]),
                                (float) Convert.ToDouble(strs[1]),
                                (float) Convert.ToDouble(strs[2]),
                                (float) Convert.ToDouble(strs[3])
                                );
                            fieldInfo.SetValue(this, color);
                        }
                    }
                }
            }
        }

        private void BindEventsByInspector() {
            var t = GetType();
            var caller = this;
            
            var len = _ilrComponent.ilrComponentEvents.Count;
            for (var i = 0; i < len; i++) {
                var item = _ilrComponent.ilrComponentEvents[i];

                var handlersLen = item.eventHandlers.Count;
                for (var j = 0; j < handlersLen; j++) {
                    var handler = item.eventHandlers[j];
                    if (string.IsNullOrEmpty(handler.methodName)) continue;
                    
                    var methodInfo = t.GetMethod(handler.methodName);
                    if (methodInfo == null) {
                        Debug.LogError($"Failed to bind the method <{handler.methodName}>. Dos not exist in {t.Name}");
                        continue;
                    }
                    
                    if (item.compName.Equals("Button")) {
                        var component = ((Component)item.obj).GetComponent<Button>();
                        if (handler.eventName.Equals("onClick")) {
                            component.onClick.AddListener(() => {
                                methodInfo.Invoke(caller, null);
                            });
                        }
                    } 
                    // else if (item.compName.Equals("ExtButton")) {
                    //     var component = ((Component)item.obj).GetComponent<ExtButton>();
                    //     if (handler.eventName.Equals("onClick")) {
                    //         component.onClick.AddListener(() => {
                    //             methodInfo.Invoke(caller, null);
                    //         });
                    //     } else if (handler.eventName.Equals("onLongPress")) {
                    //         component.onLongPress.AddListener(() => {
                    //             methodInfo.Invoke(caller, null);
                    //         });
                    //     } else if (handler.eventName.Equals("onTouchDownOutSide")) {
                    //         component.onTouchDownOutSide.AddListener(() => {
                    //             methodInfo.Invoke(caller, null);
                    //         });
                    //     }
                    // } else if (item.compName.Equals("CUIButton")) {
                    //     var component = ((Component)item.obj).GetComponent<CUIButton>();
                    //     if (handler.eventName.Equals("onClick")) {
                    //         component.onClick.AddListener(() => {
                    //             methodInfo.Invoke(caller, null);
                    //         });
                    //     } else if (handler.eventName.Equals("onLongPress")) {
                    //         component.onLongPress.AddListener(() => {
                    //             methodInfo.Invoke(caller, null);
                    //         });
                    //     }
                    // }
                }
            }
        }

        #endregion

        public void OnUpdate(float dt) {
            Update();
        }
    }
}