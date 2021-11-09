using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace HotFix.Framework.ILRuntime.Extensions
{
    public static class ReflecCustomAttributeHack
    {
        private const string CUSTOM_ATTRIBUTES = "customAttributes";
        
        /// <summary>
        /// 获取私有变量
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="fieldName"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetPrivateField<T>(this object instance, string fieldName) {
            var flag = BindingFlags.Instance | BindingFlags.NonPublic;
            var type = instance.GetType();
            var field = type.GetField(fieldName, flag);
            return (T) field.GetValue(instance);
        }

        /// <summary>
        /// 不知道什么原因，在 HotFix 工程中调用 GetCustomAttribute 或返回 null，这里补充一个 hack 方法
        /// </summary>
        /// <returns></returns>
        public static T GetCustomAttribute_Hack<T>(this FieldInfo fieldInfo) where T : Attribute {
            var targetType = typeof(T);
            
            // 一定要调一下下面的 IsDefined，因为内部会触发 InitializeCustomAttribute，否则拿不到 customAttribute
            fieldInfo.IsDefined(targetType);
            
            var allAttributes = fieldInfo.GetPrivateField<Attribute[]>(CUSTOM_ATTRIBUTES);
            for (var i = 0; i < allAttributes.Length; i++) {
                var att = allAttributes[i];
                if (att != null && att.GetType() == targetType) {
                    return (T) att;
                }
            }

            return null;
        }
        
        /// <summary>
        /// 不知道什么原因，在 HotFix 工程中调用 GetCustomAttribute 或返回 null，这里补充一个 hack 方法
        /// </summary>
        /// <returns></returns>
        public static Attribute GetCustomAttribute_Hack(this FieldInfo fieldInfo, Type type) {
            // 一定要调一下下面的 IsDefined，因为内部会触发 InitializeCustomAttribute，否则拿不到 customAttribute
            fieldInfo.IsDefined(type);
            
            var allAttributes = fieldInfo.GetPrivateField<Attribute[]>(CUSTOM_ATTRIBUTES);
            for (var i = 0; i < allAttributes.Length; i++) {
                var att = allAttributes[i];
                if (att != null && att.GetType() == type) {
                    return att;
                }
            }

            return null;
        }
        
        /// <summary>
        /// 不知道什么原因，在 HotFix 工程中调用 GetCustomAttribute 或返回 null，这里补充一个 hack 方法
        /// </summary>
        /// <returns></returns>
        public static Attribute[] GetCustomAttributes_Hack(this FieldInfo fieldInfo, Type type) {
            // 一定要调一下下面的 IsDefined，因为内部会触发 InitializeCustomAttribute，否则拿不到 customAttribute
            fieldInfo.IsDefined(type);
            
            var ret = new List<Attribute>();
            
            var allAttributes = fieldInfo.GetPrivateField<Attribute[]>(CUSTOM_ATTRIBUTES);
            for (var i = 0; i < allAttributes.Length; i++) {
                var att = allAttributes[i];
                if (att != null && att.GetType() == type) {
                    ret.Add(att);
                }
            }

            return ret.ToArray();
        }
        
        /// <summary>
        /// 不知道什么原因，在 HotFix 工程中调用 GetCustomAttribute 或返回 null，这里补充一个 hack 方法
        /// </summary>
        /// <returns></returns>
        public static T GetCustomAttribute_Hack<T>(this PropertyInfo propertyInfo) where T : Attribute {
            var targetType = typeof(T);
            
            // 一定要调一下下面的 IsDefined，因为内部会触发 InitializeCustomAttribute，否则拿不到 customAttribute
            propertyInfo.IsDefined(targetType);
            
            var allAttributes = propertyInfo.GetPrivateField<Attribute[]>(CUSTOM_ATTRIBUTES);
            for (var i = 0; i < allAttributes.Length; i++) {
                var att = allAttributes[i];
                if (att != null && att.GetType() == targetType) {
                    return (T) att;
                }
            }

            return null;
        }
        
        /// <summary>
        /// 不知道什么原因，在 HotFix 工程中调用 GetCustomAttribute 或返回 null，这里补充一个 hack 方法
        /// </summary>
        /// <returns></returns>
        public static Attribute GetCustomAttribute_Hack(this PropertyInfo propertyInfo, Type type) {
            // 一定要调一下下面的 IsDefined，因为内部会触发 InitializeCustomAttribute，否则拿不到 customAttribute
            propertyInfo.IsDefined(type);
            
            var allAttributes = propertyInfo.GetPrivateField<Attribute[]>(CUSTOM_ATTRIBUTES);
            for (var i = 0; i < allAttributes.Length; i++) {
                var att = allAttributes[i];
                if (att != null && att.GetType() == type) {
                    return att;
                }
            }

            return null;
        }
        
        /// <summary>
        /// 不知道什么原因，在 HotFix 工程中调用 GetCustomAttribute 或返回 null，这里补充一个 hack 方法
        /// </summary>
        /// <returns></returns>
        public static T GetCustomAttribute_Hack<T>(this MethodInfo methodInfo) where T : Attribute {
            var targetType = typeof(T);
            
            // 一定要调一下下面的 IsDefined，因为内部会触发 InitializeCustomAttribute，否则拿不到 customAttribute
            methodInfo.IsDefined(targetType);
            
            var allAttributes = methodInfo.GetPrivateField<Attribute[]>(CUSTOM_ATTRIBUTES);
            for (var i = 0; i < allAttributes.Length; i++) {
                var att = allAttributes[i];
                if (att != null && att.GetType() == targetType) {
                    return (T) att;
                }
            }

            return null;
        }
        
        /// <summary>
        /// 不知道什么原因，在 HotFix 工程中调用 GetCustomAttribute 或返回 null，这里补充一个 hack 方法
        /// </summary>
        /// <returns></returns>
        public static Attribute GetCustomAttribute_Hack(this MethodInfo methodInfo, Type type) {
            // 一定要调一下下面的 IsDefined，因为内部会触发 InitializeCustomAttribute，否则拿不到 customAttribute
            methodInfo.IsDefined(type);
            
            var allAttributes = methodInfo.GetPrivateField<Attribute[]>(CUSTOM_ATTRIBUTES);
            for (var i = 0; i < allAttributes.Length; i++) {
                var att = allAttributes[i];
                if (att != null && att.GetType() == type) {
                    return att;
                }
            }

            return null;
        }
    }
}