using System;
using System.Threading.Tasks;
using com.aaframework.Runtime;

namespace com.ilrframework.Runtime
{
    public abstract class ILRConfigurator
    {
        /// <summary>
        /// 入口场景
        /// </summary>
        public abstract string EntryScenePath();

        /// <summary>
        /// 在 AA 检查更新之前，可以做一些事情
        /// </summary>
        public abstract void OnAADownloadBefore();

        /// <summary>
        /// 需要下载更新，此时弹窗提示下载大小，并可以点击下载等
        /// 下载结束后一定要 Callback downloadFinished
        /// </summary>
        public abstract void OnAANeedDownload(AADownloader.AAUpdateInfo updateInfo, Action downloadFinished);

        /// <summary>
        /// 在 AA 下载更新之后，可以做一些事情
        /// </summary>
        public abstract void OnAADownloadAfter();
        
        /// <summary>
        /// 可以加载资源了
        /// </summary>
        /// <returns></returns>
        public abstract Task OnStartLoading();

        /// <summary>
        /// 注册跨域继承适配器
        /// </summary>
        /// <param name="appDomain"></param>
        public abstract void RegisterCrossBindingAdaptors(ILRuntime.Runtime.Enviorment.AppDomain appDomain);

        /// <summary>
        /// Delegate 的注册
        /// </summary>
        /// <param name="appDomain"></param>
        public abstract void RegisterDelegates(ILRuntime.Runtime.Enviorment.AppDomain appDomain);

        /// <summary>
        /// 绑定值类型
        /// </summary>
        /// <param name="appDomain"></param>
        public abstract void BindValueTypes(ILRuntime.Runtime.Enviorment.AppDomain appDomain);

        /// <summary>
        /// CLR 重定向
        /// </summary>
        public abstract void RegisterCLRRedirection(ILRuntime.Runtime.Enviorment.AppDomain appDomain);
    }
}