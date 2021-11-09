using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
#endif

namespace com.ilrframework.Runtime
{
    [Serializable]
    public class ILRComponentField
    {
        public string field;
        public UnityEngine.Object obj;
        public string value;

        [NonSerialized] public Type fieldType;
    }
    
    [Serializable]
    public class ILRComponentEvent
    {
        public Object obj;
        public string compName;
        public List<ILRComponentEventHandler> eventHandlers;
    }

    [Serializable]
    public class ILRComponentEventHandler
    {
        public string eventName;
        public string methodName;
    }
    
    [DisallowMultipleComponent]
    public class ILRComponent : MonoBehaviour, IPoolComponent
    {
        #region Behaviours
        
        public static readonly List<string> AllIlrBehaviours = new List<string>();

        #endregion
        
        #region Bind fields

        public static Action<ILRComponent> ComponentAwakeAction;

        /// <summary>
        /// 想要绑定的脚本（热更工程中的脚本，继承 ILRBehaviour）
        /// </summary>
        [ShowInInspector, SerializeField, LabelText("HotFix Script")]
        [ValueDropdown("DropDownClasses", DropdownWidth = 300, ExpandAllMenuItems = true)]
        [OnValueChanged("OnChangeIlRBehaviour"), OnInspectorInit("OnChangeIlRBehaviour")]
        private string ilrBehaviourPath;
        
        [ReadOnly, HideLabel, PropertySpace(5)]
        public string ilrBehaviour;

        /// <summary>
        /// 绑定字段
        /// </summary>
        [NonSerialized, ShowInInspector, CustomValueDrawer("DrawIlrComponentFields")]
        private bool _showIlrComponentFields;
        
        /// <summary>
        /// 绑定字段
        /// </summary>
        [HideInInspector]
        public List<ILRComponentField> ilrComponentFields;
        
        /// <summary>
        /// 绑定事件
        /// </summary>
        [NonSerialized, ShowInInspector, CustomValueDrawer("DrawIlrComponentEvents")]
        private bool _showIlrComponentEvents;
        
        /// <summary>
        /// 绑定事件
        /// </summary>
        [HideInInspector]
        public List<ILRComponentEvent> ilrComponentEvents;

        #endregion

        public Action OnEnableAction;
        public Action AwakeAction;
        public Action StartAction;
        public Action OnDisableAction;
        public Action OnDestroyAction;

        public Action OnTransformParentChangedAction;

        public Action OnPoolSpawnAction;
        public Action OnPoolDespawnAction;
        
        private void OnEnable() {
            if (OnEnableAction != null) OnEnableAction.Invoke();
        }

        private void Awake() {
            ComponentAwakeAction.Invoke(this);
            
            if (AwakeAction != null) AwakeAction.Invoke();
        }

        private void Start() {
            if (StartAction != null) StartAction.Invoke();
        }

        // 禁止使用 Update 不要实现！！！请在 HotFix 内使用 LoopSystem 派发出来的 Update 事件 
        // void Update()
        // {
        //     // 禁止使用 Update 不要实现！！！请在 HotFix 内使用 LoopSystem 派发出来的 Update 事件 
        // }
        // 禁止使用 Update 不要实现！！！请在 HotFix 内使用 LoopSystem 派发出来的 Update 事件 

        private void OnDisable() {
            if (OnDisableAction != null) OnDisableAction.Invoke();
        }

        private void OnDestroy() {
            if (OnDestroyAction != null) OnDestroyAction.Invoke();
        }

        private void OnTransformParentChanged() {
            if (OnTransformParentChangedAction != null) OnTransformParentChangedAction.Invoke();
        }

        #region IPoolComponent
        
        public void OnSpawn() {
            if (OnPoolSpawnAction != null) OnPoolSpawnAction.Invoke();
        }

        public void OnDespawn() {
            if (OnPoolDespawnAction != null) OnPoolDespawnAction.Invoke();
        }

        #endregion
        
        
        
#if UNITY_EDITOR
        public void SetILRBehaviourWithFullName(string behaviour) {
            ilrBehaviour = behaviour;
            ilrBehaviourPath = behaviour.Replace('.', '/');
            OnChangeIlRBehaviour();
        }

        private IEnumerable DropDownClasses() {
            return ILRDllAnalyzer.HotfixClassNamePathMap.Keys;
        }

        [NonSerialized] private ILRDllClass _dllClass;

        [NonSerialized]
        private readonly List<string> _previousFieldNames = new List<string>();

        private string[] _publicMethodArray;
        private string[] _publicMethodNameArray;
        
        [NonSerialized]
        public readonly List<ILRComponentField> _refreshComponentFields = new List<ILRComponentField>();
        
        private void OnChangeIlRBehaviour() {
            if (string.IsNullOrEmpty(ilrBehaviourPath)) return;
            
            var exist = ILRDllAnalyzer.HotfixClassNamePathMap.TryGetValue(ilrBehaviourPath, out ilrBehaviour);
            if (!exist) return;
                
            exist = ILRDllAnalyzer.HotfixClasses.TryGetValue(ilrBehaviour, out var dllClass);
            if (!exist) return;

            _dllClass = dllClass;
            
            _publicMethodArray = dllClass.publicMethods.ToArray();
            _publicMethodNameArray = dllClass.publicMethods.ToArray();
            _publicMethodNameArray[0] = "No Function";
            
            UpdateIlrComponentFields(dllClass);
            UpdateComponentEvents(dllClass);
            
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// 更新字段列表
        /// </summary>
        /// <param name="dllClass"></param>
        private void UpdateIlrComponentFields(ILRDllClass dllClass) {
            if (ilrComponentFields == null) {
                ilrComponentFields = new List<ILRComponentField>();
            }
            
            _previousFieldNames.Clear();

            foreach (var component in ilrComponentFields) {
                _previousFieldNames.Add(component.field);
            }
                
            _refreshComponentFields.Clear();
                
            var currentFieldNames = dllClass.publicFields.Keys;
            foreach (var currentFieldName in currentFieldNames) {
                var fieldType = dllClass.publicFields[currentFieldName];
                var item = ilrComponentFields.Find(i => i.field == currentFieldName);
                if (item != null) {
                    item.fieldType = fieldType;
                    _refreshComponentFields.Add(item);
                }
                else {
                    _refreshComponentFields.Add(new ILRComponentField
                        { field = currentFieldName, fieldType = fieldType }
                    );
                }
            }

            ilrComponentFields.Clear();
            foreach (var componentField in _refreshComponentFields) {
                ilrComponentFields.Add(componentField);
            }
        }

        private void getNoIlrChild(Transform trans, ref List<GameObject> objs) {
            objs.Add(trans.gameObject);
            foreach (Transform child in trans) {
                var obj = child.gameObject;
                if (obj.GetComponent<ILRComponent>() == null) {
                    getNoIlrChild(child, ref objs);
                }
            }
        }

        private readonly List<Object> _previousEventComps = new List<Object>();
        private readonly List<ILRComponentEvent> _refreshComponentEvents = new List<ILRComponentEvent>();
        /// <summary>
        /// 更新 UI 事件列表
        /// </summary>
        /// <param name="dllClass"></param>
        private void UpdateComponentEvents(ILRDllClass dllClass) {
            if (ilrComponentEvents == null) {
                ilrComponentEvents = new List<ILRComponentEvent>();
            }
            
            _previousEventComps.Clear();
            foreach (var ilrComponentEvent in ilrComponentEvents) {
                _previousEventComps.Add(ilrComponentEvent.obj);
            }
            
            var currentComps = new List<Object>();
            _refreshComponentEvents.Clear();

            var btnsList = new List<Button>();
            var objs = new List<GameObject>();
            getNoIlrChild(transform, ref objs);
            for (var i = 0; i < objs.Count; i++) {
                if (objs[i].GetComponent<Button>() != null) {
                    btnsList.Add(objs[i].GetComponent<Button>());
                }
            }
            var allButtons = btnsList.ToArray();
            
            currentComps.AddRange(allButtons);
            
            foreach (var comp in currentComps) {
                var item = ilrComponentEvents.Find(i => i.obj == comp);
                if (item != null) {
                    item.compName = comp.GetType().Name;
                }
                else {
                    item = new ILRComponentEvent {
                        obj = comp,
                        compName = comp.GetType().Name,
                        eventHandlers = new List<ILRComponentEventHandler>()
                    };
                }

                if (comp is Button && item.eventHandlers.Find(i => i.eventName == ComponentEventOnClick) == null) {
                    item.eventHandlers.Add(new ILRComponentEventHandler { eventName = ComponentEventOnClick });
                }
                
                _refreshComponentEvents.Add(item);
            }
            
            ilrComponentEvents.Clear();
            foreach (var componentEvent in _refreshComponentEvents) {
                ilrComponentEvents.Add(componentEvent);
            }
        }

        private Color FieldTitleBarBgColor = new Color(0.21f,0.77f,0.67f, 0.7f);
        private Color EventTitleBarBgColor = new Color(0.77f, 0.29f, 0.02f, 0.7f);
        
        private bool DrawIlrComponentFields(bool b) {
            if (ilrComponentFields == null) return b;
            
            GUILayout.Space(10);
            
            SirenixEditorGUI.BeginVerticalList();
            
            SirenixEditorGUI.BeginHorizontalToolbar();
            var rect = SirenixEditorGUI.BeginInlineBox();
            SirenixEditorGUI.DrawSolidRect(rect, FieldTitleBarBgColor);
            GUILayout.Label("Fields");
            SirenixEditorGUI.EndInlineBox();
            SirenixEditorGUI.EndHorizontalToolbar();
            
            foreach (var field in ilrComponentFields) {
                SirenixEditorGUI.BeginListItem();
                
                var value = field;
                
                var change = false;

                var fieldNicifyName = ObjectNames.NicifyVariableName(value.field);
                    
                if (value.fieldType == typeof(GameObject) || typeof(Object).IsAssignableFrom(value.fieldType)
                    // typeof(Component).IsAssignableFrom(value.fieldType) ||
                    // value.fieldType == typeof(UnityEngine.Shader) ||
                    // value.fieldType == typeof(UnityEngine.Texture) ||
                    // value.fieldType == typeof(UnityEngine.Sprite) ||
                    // value.fieldType == typeof(UnityEngine.Material) ||
                    // value.fieldType.FullName == "Spine.Unity.SkeletonDataAsset"
                    ) {
                    
                    var target = SirenixEditorFields.UnityObjectField(fieldNicifyName, value.obj, value.fieldType, true);
                    change = target != value.obj;
                    value.obj = target;
                }
                else {
                    if (value.fieldType == typeof(int)) {
                        var oldValue = Convert.ToInt32(string.IsNullOrEmpty(value.value) ? "0" : value.value);
                        var newValue = SirenixEditorFields.IntField(fieldNicifyName, oldValue);
                        change = newValue != oldValue;
                        value.value = $"{newValue}";
                    }
                    else if (value.fieldType == typeof(bool)) {
                        var oldValue = Convert.ToInt32(string.IsNullOrEmpty(value.value) ? "0" : value.value);
                        var newValue = EditorGUILayout.Toggle(fieldNicifyName, oldValue == 1);
                        value.value = $"{(newValue ? 1 : 0)}";
                    }
                    else if (value.fieldType == typeof(float)) {
                        var oldValue = (float) Convert.ToDouble(string.IsNullOrEmpty(value.value) ? "0" : value.value);
                        var newValue = SirenixEditorFields.FloatField(fieldNicifyName, oldValue);
                        change = Math.Abs(newValue - oldValue) > 0.000001f;
                        value.value = $"{newValue}";
                    }
                    else if (value.fieldType == typeof(double)) {
                        var oldValue = Convert.ToDouble(string.IsNullOrEmpty(value.value) ? "0" : value.value);
                        var newValue = SirenixEditorFields.DoubleField(fieldNicifyName, oldValue);
                        change = Math.Abs(newValue - oldValue) > 0.000001f;
                        value.value = $"{newValue}";
                    }
                    else if (value.fieldType == typeof(long)) {
                        var oldValue = Convert.ToInt64(string.IsNullOrEmpty(value.value) ? "0" : value.value);
                        var newValue = SirenixEditorFields.LongField(fieldNicifyName, oldValue);
                        change = newValue != oldValue;
                        value.value = $"{newValue}";
                    }
                    else if (value.fieldType == typeof(string)) {
                        var oldValue = value.value;
                        var newValue = SirenixEditorFields.TextField(fieldNicifyName, oldValue);
                        if (newValue != null) {
                            change = !newValue.Equals(oldValue);
                            value.value = newValue;
                        }
                    }
                    else if (value.fieldType == typeof(decimal)) {
                        var oldValue = Convert.ToDecimal(string.IsNullOrEmpty(value.value) ? "0" : value.value);
                        var newValue = SirenixEditorFields.DecimalField(fieldNicifyName, oldValue);
                        change = newValue != oldValue;
                        value.value = $"{newValue}";
                    }
                    else if (value.fieldType == typeof(Color)) {
                        if (string.IsNullOrEmpty(value.value)) {
                            value.value = "1,1,1,1";
                        }
                        var strs = value.value.Split(',');
                        var oldValue = new Color(1, 1, 1, 1);
                        for (var i = 0; i < strs.Length; i++) {
                            oldValue[i] = (float) (string.IsNullOrEmpty(strs[i]) ? 1 : Convert.ToDouble(strs[i]));
                        }
                        var newValue = SirenixEditorFields.ColorField(fieldNicifyName, oldValue);
                        change = !newValue.Equals(oldValue);
                        if (change) {
                            strs[0] = $"{newValue.r}";
                            strs[1] = $"{newValue.g}";
                            strs[2] = $"{newValue.b}";
                            strs[3] = $"{newValue.a}";
                            value.value = string.Join(",", strs);
                        }
                    }
                    else {
                        GUILayout.BeginHorizontal();
                        
                        var color = GUI.contentColor;
                        GUI.contentColor = new Color(1f, 0.87f, 0.35f);
                        EditorGUILayout.LabelField($"{value.field}");
                        
                        GUILayout.FlexibleSpace();
                        
                        EditorGUILayout.LabelField($"<{value.fieldType}>");
                        GUI.contentColor = color;
                        
                        GUILayout.EndHorizontal();
                    }
                }

                if (change) {
                    EditorUtility.SetDirty(this);
                }
                
                SirenixEditorGUI.EndListItem();
            }
            
            SirenixEditorGUI.EndVerticalList();
            
            return b;
        }

        private const string ComponentEventOnClick = "onClick";
        private const string ComponentEventOnLongPress = "onLongPress";
        private const string ComponentEventOnTouchDownOutside = "onTouchDownOutSide";
        
        private bool DrawIlrComponentEvents(bool b) {
            if (ilrComponentEvents == null) return b;
            
            GUILayout.Space(10);
            
            SirenixEditorGUI.BeginVerticalList();
            
            SirenixEditorGUI.BeginHorizontalToolbar();
            var rect = SirenixEditorGUI.BeginInlineBox();
            SirenixEditorGUI.DrawSolidRect(rect, EventTitleBarBgColor);
            GUILayout.Label("Events");
            SirenixEditorGUI.EndInlineBox();
            SirenixEditorGUI.EndHorizontalToolbar();
            
            foreach (var componentEvent in ilrComponentEvents) {
                var value = componentEvent;
                
                SirenixEditorFields.UnityObjectField(value.obj, value.obj.GetType(), true);

                SirenixEditorGUI.BeginVerticalList();
                
                foreach (var handler in value.eventHandlers) {
                    SirenixEditorGUI.BeginListItem();
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    var oldValue = handler.methodName ?? "";
                    handler.methodName = SirenixEditorFields.Dropdown(handler.eventName, oldValue, _publicMethodArray, _publicMethodNameArray);

                    if (!oldValue.Equals(handler.methodName)) {
                        EditorUtility.SetDirty(this);
                    }
                    
                    GUILayout.EndHorizontal();
                    SirenixEditorGUI.EndListItem();
                }
                
                SirenixEditorGUI.EndVerticalList();
            }
            
            SirenixEditorGUI.EndVerticalList();

            return b;
        }
#endif
    }
}
