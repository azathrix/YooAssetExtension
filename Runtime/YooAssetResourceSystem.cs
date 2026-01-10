#if YOOASSET_INSTALLED
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Azathrix.Framework.Core.Attributes;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Interfaces.DefaultSystems;
using Azathrix.Framework.Interfaces.SystemEvents;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace Azathrix.YooAssetExtension
{
    /// <summary>
    /// YooAsset资源系统实现
    /// </summary>
    [SystemPriority(-1000)]
    public class YooAssetResourceSystem : IResourcesSystem, ISystemInitialize,
        IHotUpdateFlow, IDownloadMonitor, ICacheManager
    {
        private ResourcePackage _defaultPackage;
        private YooAssetSettings _settings;
        private ResourceDownloaderOperation _currentDownloader;
        private DownloadManager _downloadManager;
        private string _currentVersion;

        // IHotUpdateFlow
        public HotUpdateState State { get; private set; }
        public string ErrorMessage { get; private set; }

        // IDownloadMonitor
        public event Action<DownloadProgress> OnProgressChanged;
        public event Action<string> OnDownloadError;
        public event Action OnDownloadComplete;
        public bool IsDownloading => _currentDownloader != null && !_currentDownloader.IsDone;
        public DownloadProgress CurrentProgress { get; private set; }

        /// <summary>
        /// 获取下载管理器
        /// </summary>
        public DownloadManager DownloadManager => _downloadManager;

        /// <summary>
        /// 获取默认资源包
        /// </summary>
        public ResourcePackage DefaultPackage => _defaultPackage;

        /// <summary>
        /// 当前版本
        /// </summary>
        public string CurrentVersion => _currentVersion;

        public async UniTask OnInitializeAsync()
        {
            _settings = YooAssetSettings.Instance;
            YooAssets.Initialize();
            Log.Info("[YooAsset] Initialized");
        }

        #region IHotUpdateFlow

        public async UniTask<bool> InitPackageAsync(string packageName = null)
        {
            State = HotUpdateState.InitPackage;
            packageName ??= _settings.defaultPackageName;

            _defaultPackage = YooAssets.TryGetPackage(packageName)
                ?? YooAssets.CreatePackage(packageName);
            YooAssets.SetDefaultPackage(_defaultPackage);

            InitializationOperation initOp = _settings.playMode switch
            {
                EPlayMode.EditorSimulateMode => InitEditorSimulate(packageName),
                EPlayMode.OfflinePlayMode => InitOffline(packageName),
                EPlayMode.HostPlayMode => InitHost(packageName),
                EPlayMode.WebPlayMode => InitWebGL(packageName),
                _ => null
            };

            if (initOp == null)
            {
                ErrorMessage = "Invalid play mode";
                State = HotUpdateState.Failed;
                return false;
            }

            await initOp.ToUniTask();

            if (initOp.Status != EOperationStatus.Succeed)
            {
                ErrorMessage = initOp.Error;
                State = HotUpdateState.Failed;
                Log.Error($"[YooAsset] Package init failed: {initOp.Error}");
                return false;
            }

            _downloadManager = new DownloadManager(_defaultPackage, _settings);
            Log.Info($"[YooAsset] Package {packageName} initialized");
            return true;
        }

        public async UniTask<bool> UpdateVersionAsync()
        {
            State = HotUpdateState.UpdateVersion;
            var op = _defaultPackage.UpdatePackageVersionAsync();
            await op.ToUniTask();

            if (op.Status != EOperationStatus.Succeed)
            {
                ErrorMessage = op.Error;
                State = HotUpdateState.Failed;
                Log.Error($"[YooAsset] Update version failed: {op.Error}");
                return false;
            }

            _currentVersion = op.PackageVersion;
            Log.Info($"[YooAsset] Version: {_currentVersion}");
            return true;
        }

        public async UniTask<bool> UpdateManifestAsync()
        {
            State = HotUpdateState.UpdateManifest;

            if (string.IsNullOrEmpty(_currentVersion))
            {
                var versionOp = _defaultPackage.UpdatePackageVersionAsync();
                await versionOp.ToUniTask();
                if (versionOp.Status != EOperationStatus.Succeed)
                {
                    ErrorMessage = versionOp.Error;
                    State = HotUpdateState.Failed;
                    return false;
                }
                _currentVersion = versionOp.PackageVersion;
            }

            var op = _defaultPackage.UpdatePackageManifestAsync(_currentVersion);
            await op.ToUniTask();

            if (op.Status != EOperationStatus.Succeed)
            {
                ErrorMessage = op.Error;
                State = HotUpdateState.Failed;
                Log.Error($"[YooAsset] Update manifest failed: {op.Error}");
                return false;
            }

            Log.Info("[YooAsset] Manifest updated");
            return true;
        }

        public async UniTask<ResourceDownloaderOperation> CreateDownloaderByTagsAsync(params string[] tags)
        {
            State = HotUpdateState.CreateDownloader;
            return _defaultPackage.CreateResourceDownloader(tags,
                _settings.downloadingMaxNum, _settings.failedTryAgain);
        }

        public async UniTask<ResourceDownloaderOperation> CreateDownloaderByPathsAsync(params string[] paths)
        {
            State = HotUpdateState.CreateDownloader;
            return _defaultPackage.CreateBundleDownloader(paths,
                _settings.downloadingMaxNum, _settings.failedTryAgain);
        }

        public async UniTask<bool> RunFullUpdateAsync(string[] downloadTags = null)
        {
            if (!await InitPackageAsync()) return false;

            if (_settings.playMode == EPlayMode.HostPlayMode)
            {
                if (!await UpdateVersionAsync()) return false;
                if (!await UpdateManifestAsync()) return false;

                var tags = downloadTags ?? _settings.autoDownloadTags;
                if (tags != null && tags.Length > 0)
                {
                    var downloader = await CreateDownloaderByTagsAsync(tags);
                    if (downloader.TotalDownloadCount > 0)
                    {
                        Log.Info($"[YooAsset] Need download {downloader.TotalDownloadCount} files, {downloader.TotalDownloadBytes} bytes");

                        _currentDownloader = downloader;
                        State = HotUpdateState.Downloading;
                        StartMonitor();
                        downloader.BeginDownload();
                        await downloader.ToUniTask();
                        StopMonitor();

                        if (downloader.Status != EOperationStatus.Succeed)
                        {
                            ErrorMessage = downloader.Error;
                            State = HotUpdateState.Failed;
                            Log.Error($"[YooAsset] Download failed: {downloader.Error}");
                            return false;
                        }

                        OnDownloadComplete?.Invoke();
                    }
                }
            }

            State = HotUpdateState.Done;
            Log.Info("[YooAsset] Hot update completed");
            return true;
        }

        #endregion

        #region IDownloadMonitor

        public void StartMonitor()
        {
            if (_currentDownloader == null) return;
            _currentDownloader.OnDownloadProgressCallback = OnDownloadProgressInternal;
            _currentDownloader.OnDownloadErrorCallback = OnDownloadErrorInternal;
        }

        public void StopMonitor()
        {
            if (_currentDownloader != null)
            {
                _currentDownloader.OnDownloadProgressCallback = null;
                _currentDownloader.OnDownloadErrorCallback = null;
            }
        }

        private void OnDownloadProgressInternal(int totalCount, int currentCount, long totalBytes, long currentBytes)
        {
            CurrentProgress = new DownloadProgress
            {
                TotalCount = totalCount,
                CurrentCount = currentCount,
                TotalBytes = totalBytes,
                CurrentBytes = currentBytes
            };
            OnProgressChanged?.Invoke(CurrentProgress);
        }

        private void OnDownloadErrorInternal(string fileName, string error)
        {
            OnDownloadError?.Invoke($"{fileName}: {error}");
        }

        #endregion

        #region ICacheManager

        public CacheInfo GetCacheInfo(string packageName = null)
        {
            // YooAsset 2.x 暂无直接获取缓存大小的API
            return new CacheInfo { TotalSize = 0, FileCount = 0 };
        }

        public async UniTask ClearUnusedCacheAsync(string packageName = null)
        {
            var pkg = string.IsNullOrEmpty(packageName) ? _defaultPackage : YooAssets.GetPackage(packageName);
            if (pkg == null) return;

            var op = pkg.ClearUnusedCacheFilesAsync();
            await op.ToUniTask();
            Log.Info("[YooAsset] Unused cache cleared");
        }

        public async UniTask ClearAllCacheAsync(string packageName = null)
        {
            var pkg = string.IsNullOrEmpty(packageName) ? _defaultPackage : YooAssets.GetPackage(packageName);
            if (pkg == null) return;

            var op = pkg.ClearAllCacheFilesAsync();
            await op.ToUniTask();
            Log.Info("[YooAsset] All cache cleared");
        }

        #endregion

        #region IResourcesLoader

        public async UniTask<T> LoadAsync<T>(string key) where T : UnityEngine.Object
        {
            if (_defaultPackage == null)
            {
                Log.Error("[YooAsset] Package not initialized");
                return null;
            }

            var handle = _defaultPackage.LoadAssetAsync<T>(key);
            await handle.ToUniTask();

            if (handle.Status == EOperationStatus.Succeed)
            {
                return handle.AssetObject as T;
            }

            Log.Error($"[YooAsset] Failed to load asset: {key}");
            return null;
        }

        public T Load<T>(string key) where T : UnityEngine.Object
        {
            if (_defaultPackage == null)
            {
                Log.Error("[YooAsset] Package not initialized");
                return null;
            }

            var handle = _defaultPackage.LoadAssetSync<T>(key);

            if (handle.Status == EOperationStatus.Succeed)
            {
                return handle.AssetObject as T;
            }

            Log.Error($"[YooAsset] Failed to load asset: {key}");
            return null;
        }

        #endregion

        #region IResourcesSystem

        public async UniTask<GameObject> InstantiateAsync(string key, Transform parent = null)
        {
            var prefab = await LoadAsync<GameObject>(key);
            return prefab != null ? UnityEngine.Object.Instantiate(prefab, parent) : null;
        }

        public async UniTask LoadSceneAsync(string key, LoadSceneMode mode)
        {
            if (_defaultPackage == null)
            {
                Log.Error("[YooAsset] Package not initialized");
                return;
            }

            var op = _defaultPackage.LoadSceneAsync(key, mode);
            await op.ToUniTask();

            if (op.Status != EOperationStatus.Succeed)
            {
                Log.Error($"[YooAsset] Failed to load scene: {key}");
            }
        }

        #endregion

        #region Private Helpers

        private InitializationOperation InitEditorSimulate(string packageName)
        {
            var param = new EditorSimulateModeParameters();
            param.SimulateManifestFilePath = EditorSimulateModeHelper.SimulateBuild(packageName);
            return _defaultPackage.InitializeAsync(param);
        }

        private InitializationOperation InitOffline(string packageName)
        {
            return _defaultPackage.InitializeAsync(new OfflinePlayModeParameters());
        }

        private InitializationOperation InitHost(string packageName)
        {
            var param = new HostPlayModeParameters();
            param.BuildinQueryServices = new GameQueryServices();
            param.RemoteServices = new RemoteServices(_settings.hostServerURL, _settings.fallbackHostServerURL);
            return _defaultPackage.InitializeAsync(param);
        }

        private InitializationOperation InitWebGL(string packageName)
        {
            var param = new WebPlayModeParameters();
            param.BuildinQueryServices = new GameQueryServices();
            param.RemoteServices = new RemoteServices(_settings.hostServerURL, _settings.fallbackHostServerURL);
            return _defaultPackage.InitializeAsync(param);
        }

        private class GameQueryServices : IBuildinQueryServices
        {
            public bool Query(string packageName, string fileName, string fileCRC) => false;
            public bool QueryStreamingAssets(string packageName, string fileName) => false;
        }

        private class RemoteServices : IRemoteServices
        {
            private readonly string _defaultUrl;
            private readonly string _fallbackUrl;

            public RemoteServices(string defaultUrl, string fallbackUrl)
            {
                _defaultUrl = defaultUrl;
                _fallbackUrl = fallbackUrl;
            }

            public string GetRemoteMainURL(string fileName) => $"{_defaultUrl}/{fileName}";

            public string GetRemoteFallbackURL(string fileName) =>
                string.IsNullOrEmpty(_fallbackUrl) ? GetRemoteMainURL(fileName) : $"{_fallbackUrl}/{fileName}";
        }

        #endregion
    }
}
#endif
