
using com.aaframework.Runtime;
using com.ilrframework.Runtime;
using HotFix.Game.Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HotFix.Framework.ILRuntime.Core
{
    /// <summary>
    /// 这里是主工程调用热更工程的入口
    /// </summary>
    public class ILREntry
    {
        public static async void EnterILRuntime(string firstScene) {
            CheckEnvironment();
            
            // await Task.Delay(5000);

            // 一定要在进入 HotFix 的最开始就调用
            ILRComponentHook.InitMagicMethodInfos();
            
            AAManager.Instance.Init();
            
            // 初始化循环系统
            LoopSystem.Instance.Init();
            // 初始化消息系统
            LoopSystem.Instance.AddUpdatable(MessageSystem.Instance);
            
            // AudioManager.Instance.Init();
            //
            // FirstLoadingPanel.SetProgress(0.65f);
            //
            // // 加载配置
            // await M3ConfigHelper.Instance.LoadConfigsAsync();
            //
            // FirstLoadingPanel.SetProgress(0.98f);
            //
            // // 加载第一个场景
            // Debug.Log($"Load first scene '{firstScene}'");
            //
            // await SceneManager.Instance.LoadScene(firstScene);
            //
            // FirstLoadingPanel.SetProgress(1f);
        }

        private static void CheckEnvironment() {
            var str = "当前 HotFix 环境为 ";

#if DEBUG
            str += "Debug，";
#else
            str += "Release，";
#endif
            if (ILRApp.UNITY_EDITOR) {
                str += "Editor 模式，平台为 ";
            }
            else {
                str += "非 Editor 模式，平台为 ";
            }

#if UNITY_IPHONE
            str += "iOS";
#endif

#if UNITY_ANDROID
            str += "Andriod";
#endif
            Debug.Log($"<color=#50994c>{str}</color>");
        }
    }
}