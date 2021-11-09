#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace com.ilrframework.Editor
{
    public static class CopyPathInHierarchy
    {
        [MenuItem("GameObject/Copy Path", false, -3000)]
        private static void CopySelectedPath() {
            var path = GetHierarchyPath(Selection.activeTransform);
            Debug.Log($"Selected GameObject Path: '{path}'");
            EditorGUIUtility.systemCopyBuffer = path;
        }
        
        [MenuItem("GameObject/Copy Path", true)]
        private static bool ValidateCopySelectedPath() {
            return Selection.activeTransform != null;
        }
        
        private static string GetHierarchyPath(Transform transform) {
            if (transform == null)
                return "";
 
            var path = transform.name;
 
            while (transform.parent != null) {
                transform = transform.parent;
                path = $"{transform.name}/{path}";
            }

            if (path.StartsWith("Canvas (Environment)")) {
                var index = path.IndexOf('/', 0);
                path = path.Substring(index + 1);
                index = path.IndexOf('/', 0);
                path = path.Substring(index + 1);
            }

            return path;
        }
    }
}

#endif