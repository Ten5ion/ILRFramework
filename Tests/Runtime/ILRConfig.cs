using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using com.aaframework.Runtime;
using com.ilrframework.Runtime;
using HeSh.Game.Loading;
using UnityEngine;

public class ILRConfig : ILRConfigurator
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
}
