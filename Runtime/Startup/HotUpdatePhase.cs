#if YOOASSET_INSTALLED
using System;
using Azathrix.Framework.Core.Startup;
using Azathrix.Framework.Tools;
using Azathrix.YooSystem.Interfaces;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace Azathrix.YooSystem.Startup
{
    /// <summary>
    /// 热更新阶段 - 在系统注册之前执行
    /// </summary>
    public class HotUpdatePhase : IHotUpdatePhase, IHotUpdateFlow
    {
        public string Id => "HotUpdate";
        public int Order => 50;

        public HotUpdateState State { get; private set; }
        public string ErrorMessage { get; private set; }

        public async UniTask ExecuteAsync(PhaseContext context)
        {
            var settings = YooAssetSettings.Instance;
            YooService.Initialize(settings);

            Log.Info("[YooAsset] Starting hot update...");
            var success = await RunFullUpdateAsync();
            if (!success)
            {
                Log.Error($"[YooAsset] Hot update failed: {ErrorMessage}");
            }
        }

        #region IHotUpdateFlow

        public async UniTask<bool> InitPackageAsync(string packageName = null)
        {
            State = HotUpdateState.InitPackage;
            var success = await YooService.InitPackageAsync(packageName ?? YooService.Settings.DefaultPackageName);
            if (!success)
            {
                ErrorMessage = $"Package init failed: {packageName}";
                State = HotUpdateState.Failed;
            }
            return success;
        }

        public async UniTask<bool> InitAllPackagesAsync()
        {
            State = HotUpdateState.InitPackage;
            var success = await YooService.InitAllPackagesAsync();
            if (!success)
            {
                ErrorMessage = "Init all packages failed";
                State = HotUpdateState.Failed;
            }
            return success;
        }

        public async UniTask<bool> UpdateVersionAsync()
        {
            return await UpdateVersionAsync(null);
        }

        public async UniTask<bool> UpdateVersionAsync(string packageName)
        {
            State = HotUpdateState.UpdateVersion;
            var (success, _) = await YooService.UpdateVersionAsync(packageName);
            if (!success)
            {
                ErrorMessage = $"Update version failed: {packageName}";
                State = HotUpdateState.Failed;
            }
            return success;
        }

        public async UniTask<bool> UpdateAllVersionsAsync()
        {
            State = HotUpdateState.UpdateVersion;
            var success = await YooService.UpdateAllVersionsAsync();
            if (!success)
            {
                ErrorMessage = "Update all versions failed";
                State = HotUpdateState.Failed;
            }
            return success;
        }

        public async UniTask<bool> UpdateManifestAsync()
        {
            return await UpdateManifestAsync(null);
        }

        public async UniTask<bool> UpdateManifestAsync(string packageName)
        {
            State = HotUpdateState.UpdateManifest;
            var success = await YooService.UpdateManifestAsync(packageName);
            if (!success)
            {
                ErrorMessage = $"Update manifest failed: {packageName}";
                State = HotUpdateState.Failed;
            }
            return success;
        }

        public async UniTask<bool> UpdateAllManifestsAsync()
        {
            State = HotUpdateState.UpdateManifest;
            var success = await YooService.UpdateAllManifestsAsync();
            if (!success)
            {
                ErrorMessage = "Update all manifests failed";
                State = HotUpdateState.Failed;
            }
            return success;
        }

        public UniTask<ResourceDownloaderOperation> CreateDownloaderByTagsAsync(params string[] tags)
        {
            return CreateDownloaderByTagsAsync(null, tags);
        }

        public UniTask<ResourceDownloaderOperation> CreateDownloaderByTagsAsync(string packageName, params string[] tags)
        {
            State = HotUpdateState.CreateDownloader;
            var downloader = YooService.CreateDownloaderByTags(packageName, tags);
            return UniTask.FromResult(downloader);
        }

        public UniTask<ResourceDownloaderOperation> CreateDownloaderByPathsAsync(params string[] paths)
        {
            return CreateDownloaderByPathsAsync(null, paths);
        }

        public UniTask<ResourceDownloaderOperation> CreateDownloaderByPathsAsync(string packageName, params string[] paths)
        {
            State = HotUpdateState.CreateDownloader;
            var downloader = YooService.CreateDownloaderByPaths(packageName, paths);
            return UniTask.FromResult(downloader);
        }

        public async UniTask<bool> RunFullUpdateAsync(string[] downloadTags = null)
        {
            try
            {
                if (!await InitAllPackagesAsync()) return false;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
                return false;
            }

            var settings = YooService.Settings;
            if (settings.PlayMode == EPlayMode.HostPlayMode)
            {
                if (!await UpdateAllVersionsAsync()) return false;
                if (!await UpdateAllManifestsAsync()) return false;

                foreach (var pkgConfig in settings.Packages)
                {
                    var tags = pkgConfig.autoDownloadTags;
                    if (tags == null || tags.Length == 0) continue;

                    var downloader = YooService.CreateDownloaderByTags(pkgConfig.packageName, tags);
                    if (downloader == null || downloader.TotalDownloadCount == 0) continue;

                    Log.Info($"[YooAsset] Package {pkgConfig.packageName} need download {downloader.TotalDownloadCount} files, {downloader.TotalDownloadBytes} bytes");

                    State = HotUpdateState.Downloading;
                    downloader.BeginDownload();
                    await UniTask.WaitUntil(() => downloader.IsDone);

                    if (downloader.Status != EOperationStatus.Succeed)
                    {
                        ErrorMessage = downloader.Error;
                        State = HotUpdateState.Failed;
                        Log.Error($"[YooAsset] Download failed for {pkgConfig.packageName}: {downloader.Error}");
                        return false;
                    }
                }
            }

            State = HotUpdateState.Done;
            Log.Info("[YooAsset] Hot update completed");
            return true;
        }

        #endregion
    }
}
#endif
