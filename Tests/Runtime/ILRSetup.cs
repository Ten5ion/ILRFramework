using System;
using System.Threading.Tasks;
using com.aaframework.Runtime;
using com.ilrframework.Runtime;
using HeSh.Game.Loading;
using AppDomain = ILRuntime.Runtime.Enviorment.AppDomain;

public class ILRSetup : ILRConfigurator
{
    public override string EntryScenePath() {
        return "Scenes/LobbyScene.unity";
    }

    public override void OnAADownloadBefore() {
        
    }

    public override void OnAANeedDownload(AADownloader.AAUpdateInfo updateInfo, Action downloadFinished) {
        AADownloadPanel.Instance.Show(updateInfo.DownloadSize, downloadFinished);
    }

    public override void OnAADownloadAfter() {
        
    }

    public override async Task OnStartLoading() {
        await ILRApp.Instance.LoadHotFixAssembly();
    }

    public override void RegisterCrossBindingAdaptors(ILRuntime.Runtime.Enviorment.AppDomain appDomain) {
        // appDomain.RegisterCrossBindingAdaptor(new xxx());
    }

    public override void RegisterDelegates(ILRuntime.Runtime.Enviorment.AppDomain appDomain) {
        var delegateManager = appDomain.DelegateManager;
        // delegateManager.RegisterMethodDelegate<int>();
    }

    public override void BindValueTypes(ILRuntime.Runtime.Enviorment.AppDomain appDomain) {
        // appDomain.RegisterValueTypeBinder(typeof(Vector3), new Vector3Binder());
    }

    public override void RegisterCLRRedirection(ILRuntime.Runtime.Enviorment.AppDomain appDomain) {
        // CLRRedirectionDebug.Register(appDomain);
    }
}
