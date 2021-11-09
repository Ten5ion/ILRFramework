#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using com.aaframework.Runtime;
using ILRuntime.Mono.Cecil;
using UnityEngine;

namespace com.ilrframework.Runtime
{
    public struct ILRDllClass
    {
        public TypeDefinition typeDefinition;
        public string classFullName;
        public string classPath;
        public Dictionary<string, Type> publicFields;
        public List<string> publicMethods;
    }
    
    public class ILRDllAnalyzer
    {
        #region Parse HotFix dll
        
        // HotFix 中的所有类型
        private static readonly Dictionary<string, ILRDllClass> _hotfixClasses = new Dictionary<string, ILRDllClass>();
        public static Dictionary<string, ILRDllClass> HotfixClasses {
            get {
                if (_hotfixClasses.Count == 0) {
                    ParsingHotFixDll();
                }
                return _hotfixClasses;
            }
        }
        
        private static readonly Dictionary<string, string> _hotfixClassNamePathMap = new Dictionary<string, string>();
        public static Dictionary<string, string> HotfixClassNamePathMap {
            get {
                if (_hotfixClassNamePathMap.Count == 0) {
                    ParsingHotFixDll();
                }
                return _hotfixClassNamePathMap;
            }
        }

        /// <summary>
        /// 解析 HotFix 的 dll，用于获取类、成员变量、方法等信息
        /// </summary>
        // [MenuItem("ILRuntime/ParsingHotFixDll")]
        public static void ParsingHotFixDll() {
            var asset = AAManager.Instance.LoadAssetSync<TextAsset>(ILRConfig.DllPath);
            var dll = ILREncrypter.DecryptHotFixBytes(asset.bytes);
            
            using (var stream = new MemoryStream(dll)) {
                
                _hotfixClasses.Clear();
                _hotfixClassNamePathMap.Clear();
                
                var module = ModuleDefinition.ReadModule(stream);
                
                // 获取所有类型
                if (module.HasTypes) {
                    var ilrBehaviourBase = module.GetType("HotFix.Framework.ILRuntime.Core", "ILRBehaviour");
                    foreach (var t in module.GetTypes()) {
                        
                        if (t.IsClass && !t.IsAbstract && t.IsSubclassOf(ilrBehaviourBase)) {
                            
                            var item = new ILRDllClass();
                            item.typeDefinition = t;
                            
                            item.classFullName = t.FullName;
                            item.classPath = t.FullName.Replace('.', '/');
                            
                            _hotfixClassNamePathMap.Add(item.classPath, item.classFullName);
                            
                            item.publicFields = SearchPublicFields(t, ilrBehaviourBase);
                            item.publicMethods = SearchPublicMethods(t, ilrBehaviourBase);
                            
                            _hotfixClasses.Add(item.classFullName, item);
                        }
                    }
                }
            }
        }

        private static Dictionary<string, Type> SearchPublicFields(TypeDefinition clazz, TypeDefinition ilrBehaviourBase) {
            var publicFields = new Dictionary<string, Type>();
            
            var ilrBase = clazz;
            while (ilrBase != null) {
                try {
                    foreach (var fieldDefinition in ilrBase.Fields) {
                        var show = false;
                        if (fieldDefinition.IsPublic && !fieldDefinition.IsStatic) {
                            show = true;
                            foreach (var attr in fieldDefinition.CustomAttributes) {
                                var typeReference = attr.AttributeType;
                                if (typeReference.Name == "HideInInspector") {
                                    show = false;
                                    break;
                                }
                            }
                        }
                        else if (!fieldDefinition.IsPublic && !fieldDefinition.IsStatic) {
                            foreach (var attr in fieldDefinition.CustomAttributes) {
                                var typeReference = attr.AttributeType;
                                if (typeReference.Name == "ShowILInInspectorAttribute") {
                                    show = true;
                                    break;
                                }
                            }
                        }
                        
                        if (show) {
                            var fieldTypeReference = fieldDefinition.FieldType;
                            var fieldType = Type.GetType($"{fieldTypeReference.FullName}, {fieldTypeReference.Scope.Name}");

                            if (fieldType == null) {
                                if (fieldDefinition.DeclaringType.IsSubclassOf(ilrBehaviourBase)) {
                                    fieldType = typeof(ILRComponent);
                                }
                            }
                            
                            publicFields.Add(fieldDefinition.Name, fieldType);
                        }
                    }

                    if (ilrBase.BaseType != null) {
                        ilrBase = ilrBase.BaseType.Resolve();
                        if (ilrBase == ilrBehaviourBase)
                            break;
                    }
                    else {
                        break;
                    }
                }
                catch (Exception e) {
                    break;
                }
            }

            return publicFields;
        }
        
        private static List<string> SearchPublicMethods(TypeDefinition clazz, TypeDefinition ilrBehaviourBase) {
            var publicFields = new List<string>();
            // 添加一个空方法
            publicFields.Add("");
                            
            var ilrBase = clazz;
            while (ilrBase != null) {
                try {
                    foreach (var methodDefinition in ilrBase.Methods) {
                        if (methodDefinition.IsPublic && !methodDefinition.IsStatic && !methodDefinition.IsConstructor) {  
                            publicFields.Add(methodDefinition.Name);
                        }
                    }

                    if (ilrBase.BaseType != null) {
                        ilrBase = ilrBase.BaseType.Resolve();
                        if (ilrBase == ilrBehaviourBase)
                            break;
                    }
                    else {
                        break;
                    }
                }
                catch (Exception e) {
                    break;
                }
            }

            return publicFields;
        }
        
        #endregion
    }
    
    internal static class TypeDefinitionExtensions
    {
        /// <summary>
        /// Is childTypeDef a subclass of parentTypeDef. Does not test interface inheritance
        /// </summary>
        /// <param name="childTypeDef"></param>
        /// <param name="parentTypeDef"></param>
        /// <returns></returns>
        public static bool IsSubclassOf(this TypeDefinition childTypeDef, TypeDefinition parentTypeDef) {
            var ret = false;
            
            var typeDefinition = childTypeDef;
            while (!ret && typeDefinition.BaseType != null) {
                try {
                    typeDefinition = typeDefinition.BaseType.Resolve();
                    ret = typeDefinition == parentTypeDef;
                }
                catch (Exception e) {
                    break;
                }
            }

            return ret;
        }
    }
}

#endif