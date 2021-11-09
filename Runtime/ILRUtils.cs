using UnityEditor;
#if UNITY_EDITOR
#endif

namespace com.ilrframework.Runtime
{
    public static class ILRUtils
    {
        public enum CurrentSystemPlatform
        {
            IPHONE,
            ANDROID,
            EDITOR,
        }
        
        public static string EditorPrefs_GetString(string key) {
#if UNITY_EDITOR
            return EditorPrefs.GetString(key);
#else
            throw new Exception("只能在 UNITY_EDITOR 下调用");
#endif
        }

        public static void EditorApplication_SetPlaying(bool isPlaying) {
#if UNITY_EDITOR
            EditorApplication.isPlaying = isPlaying;
#else
            throw new Exception("只能在 UNITY_EDITOR 下调用");
#endif    
        }

        public static CurrentSystemPlatform GetCurrentSystemPlatform() {
#if UNITY_IPHONE && !UNITY_EDITOR
            return CurrentSystemPlatform.IPHONE;
#endif
            
#if UNITY_ANDROID && !UNITY_EDITOR
            return CurrentSystemPlatform.ANDROID;
#endif
            
#if UNITY_EDITOR
            return CurrentSystemPlatform.EDITOR;
#endif
        }
    }
}
