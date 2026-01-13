#if YOOASSET_INSTALLED
using System;
using Azathrix.Framework.Core;
using Azathrix.Framework.Core.Attributes;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Interfaces.SystemEvents;
using Azathrix.Framework.Tools;
using Azathrix.YooSystem.Download;
using Azathrix.YooSystem.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
using SceneHandle = YooAsset.SceneHandle;

namespace Azathrix.YooSystem
{
    /// <summary>
    /// YooAsset资源系统实现
    /// </summary>
    [SystemPriority(-1000)]
    public class YooSystem : ISystem, IDownloadMonitor, ICacheManager, ISystemInitialize, IResourcesLoader
    {
        private ResourcePackage _defaultPackage;
        private YooAssetSettings _settings;
        private ResourceDownloaderOperation _currentDownloader;
        private readonly System.Collections.Generic.Dictionary<string, DownloadManager> _downloadManagers = new();

        // IDownloadMonitor
        public event Action<DownloadProgress> OnProgressChanged;
        public event Action<string> OnDownloadError;
#pragma warning disable CS0067
        public event Action OnDownloadComplete;
#pragma warning restore CS0067
        public bool IsDownloading => _currentDownloader != null && !_currentDownloader.IsDone;
        public DownloadProgress CurrentProgress { get; private set; }

        /// <summary>
        /// 获取默认资源包
        /// </summary>
        public ResourcePackage DefaultPackage => _defaultPackage;

        /// <summary>
        /// 获取指定名称的资源包（null 返回默认包）
        /// </summary>
        public ResourcePackage GetPackage(string packageName = null)
            => YooService.GetPackage(packageName);

        /// <summary>
        /// 获取指定包的下载管理器
        /// </summary>
        public DownloadManager GetDownloadManager(string packageName = null)
        {
            if (_settings == null) return null;
            packageName ??= _settings.DefaultPackageName;

            if (_downloadManagers.TryGetValue(packageName, out var manager))
                return manager;

            var package = GetPackage(packageName);
            if (package == null) return null;

            manager = new DownloadManager(package, _settings);
            _downloadManagers[packageName] = manager;
            return manager;
        }

        /// <summary>
        /// 获取默认包的下载管理器
        /// </summary>
        public DownloadManager DownloadManager => GetDownloadManager();

        #region Package & Asset Status

        /// <summary>
        /// 检查包是否已初始化
        /// </summary>
        public bool IsPackageInitialized(string packageName = null)
            => YooService.IsPackageInitialized(packageName);

        /// <summary>
        /// 检查资源路径是否有效
        /// </summary>
        public bool CheckAssetExists(string assetPath, string packageName = null)
        {
            var pkg = GetPackage(packageName);
            if (pkg == null || pkg.InitializeStatus != EOperationStatus.Succeed)
                return false;

            try { return pkg.CheckLocationValid(assetPath); }
            catch { return false; }
        }

        /// <summary>
        /// 获取指定标签需要下载的文件数量
        /// </summary>
        public int GetDownloadCount(string[] tags, string packageName = null)
        {
            var (count, _) = YooService.GetDownloadInfo(tags, packageName);
            return count;
        }

        /// <summary>
        /// 获取指定标签需要下载的总字节数
        /// </summary>
        public long GetDownloadBytes(string[] tags, string packageName = null)
        {
            var (_, bytes) = YooService.GetDownloadInfo(tags, packageName);
            return bytes;
        }

        /// <summary>
        /// 检查指定标签的资源是否需要下载
        /// </summary>
        public bool NeedDownload(string[] tags, string packageName = null)
            => YooService.NeedDownload(tags, packageName);

        /// <summary>
        /// 检查包内所有资源是否需要下载
        /// </summary>
        public bool NeedDownloadAll(string packageName = null)
        {
            var pkg = GetPackage(packageName);
            if (pkg == null || pkg.InitializeStatus != EOperationStatus.Succeed) return false;

            try
            {
                var downloader = pkg.CreateResourceDownloader(_settings.DownloadingMaxNum, _settings.FailedTryAgain);
                return downloader.TotalDownloadCount > 0;
            }
            catch { return false; }
        }

        /// <summary>
        /// 获取下载信息（文件数和字节数）
        /// </summary>
        public (int fileCount, long totalBytes) GetDownloadInfo(string[] tags, string packageName = null)
            => YooService.GetDownloadInfo(tags, packageName);

        #endregion

        #region 动态 Package 管理 API

        /// <summary>
        /// 初始化单个 Package
        /// </summary>
        public UniTask<bool> InitPackageAsync(string packageName)
            => YooService.InitPackageAsync(packageName);

        /// <summary>
        /// 更新 Package 版本
        /// </summary>
        public UniTask<(bool success, string version)> UpdateVersionAsync(string packageName = null)
            => YooService.UpdateVersionAsync(packageName);

        /// <summary>
        /// 更新 Package 清单
        /// </summary>
        public UniTask<bool> UpdateManifestAsync(string packageName = null)
            => YooService.UpdateManifestAsync(packageName);

        /// <summary>
        /// 按标签创建下载器
        /// </summary>
        public ResourceDownloaderOperation CreateDownloaderByTags(string packageName, params string[] tags)
            => YooService.CreateDownloaderByTags(packageName, tags);

        /// <summary>
        /// 按路径创建下载器
        /// </summary>
        public ResourceDownloaderOperation CreateDownloaderByPaths(string packageName, params string[] paths)
            => YooService.CreateDownloaderByPaths(packageName, paths);

        #endregion

        #region IDownloadMonitor

        public void StartMonitor()
        {
            if (_currentDownloader == null) return;
            _currentDownloader.DownloadUpdateCallback = OnDownloadProgressInternal;
            _currentDownloader.DownloadErrorCallback = OnDownloadErrorInternal;
        }

        public void StopMonitor()
        {
            if (_currentDownloader != null)
            {
                _currentDownloader.DownloadUpdateCallback = null;
                _currentDownloader.DownloadErrorCallback = null;
            }
        }

        private void OnDownloadProgressInternal(DownloadUpdateData data)
        {
            CurrentProgress = new DownloadProgress
            {
                TotalCount = data.TotalDownloadCount,
                CurrentCount = data.CurrentDownloadCount,
                TotalBytes = data.TotalDownloadBytes,
                CurrentBytes = data.CurrentDownloadBytes
            };
            OnProgressChanged?.Invoke(CurrentProgress);
        }

        private void OnDownloadErrorInternal(DownloadErrorData data)
        {
            OnDownloadError?.Invoke($"{data.FileName}: {data.ErrorInfo}");
        }

        #endregion

        #region ICacheManager

        public CacheInfo GetCacheInfo(string packageName = null)
        {
            return new CacheInfo { TotalSize = 0, FileCount = 0 };
        }

        public UniTask ClearUnusedCacheAsync(string packageName = null)
            => YooService.ClearUnusedCacheAsync(packageName);

        public UniTask ClearAllCacheAsync(string packageName = null)
            => YooService.ClearAllCacheAsync(packageName);

        #endregion

        #region Resource Unload & Release

        public UniTask UnloadUnusedAssetsAsync(string packageName = null)
            => YooService.UnloadUnusedAssetsAsync(packageName);

        public UniTask ForceUnloadAllAssetsAsync(string packageName = null)
            => YooService.ForceUnloadAllAssetsAsync(packageName);

        public bool TryUnloadUnusedAsset(string assetPath, string packageName = null)
        {
            var pkg = GetPackage(packageName);
            if (pkg == null) return false;
            pkg.TryUnloadUnusedAsset(assetPath);
            return true;
        }

        public async UniTask UnloadAllPackagesUnusedAssetsAsync()
        {
            foreach (var pkgConfig in _settings.Packages)
                await UnloadUnusedAssetsAsync(pkgConfig.packageName);
        }

        public async UniTask ForceUnloadAllPackagesAssetsAsync()
        {
            foreach (var pkgConfig in _settings.Packages)
                await ForceUnloadAllAssetsAsync(pkgConfig.packageName);
        }

        #endregion

        #region Asset Handle Loading

        public async UniTask<AssetHandle> LoadAssetWithHandleAsync<T>(string key, string packageName = null) where T : UnityEngine.Object
        {
            var package = GetPackage(packageName);
            if (package == null)
            {
                Log.Error($"[YooAsset] Package not initialized: {packageName ?? "default"}");
                return null;
            }

            var handle = package.LoadAssetAsync<T>(key);
            await UniTask.WaitUntil(() => handle.IsDone);

            if (handle.Status != EOperationStatus.Succeed)
            {
                Log.Error($"[YooAsset] Failed to load asset: {key}");
                return null;
            }
            return handle;
        }

        public AssetHandle LoadAssetWithHandle<T>(string key, string packageName = null) where T : UnityEngine.Object
        {
            var package = GetPackage(packageName);
            if (package == null)
            {
                Log.Error($"[YooAsset] Package not initialized: {packageName ?? "default"}");
                return null;
            }

            var handle = package.LoadAssetSync<T>(key);
            if (handle.Status != EOperationStatus.Succeed)
            {
                Log.Error($"[YooAsset] Failed to load asset: {key}");
                return null;
            }
            return handle;
        }

        public async UniTask<AllAssetsHandle> LoadAllAssetsWithHandleAsync<T>(string key, string packageName = null) where T : UnityEngine.Object
        {
            var package = GetPackage(packageName);
            if (package == null)
            {
                Log.Error($"[YooAsset] Package not initialized: {packageName ?? "default"}");
                return null;
            }

            var handle = package.LoadAllAssetsAsync<T>(key);
            await UniTask.WaitUntil(() => handle.IsDone);

            if (handle.Status != EOperationStatus.Succeed)
            {
                Log.Error($"[YooAsset] Failed to load all assets: {key}");
                return null;
            }
            return handle;
        }

        public async UniTask<SubAssetsHandle> LoadSubAssetsWithHandleAsync<T>(string key, string packageName = null) where T : UnityEngine.Object
        {
            var package = GetPackage(packageName);
            if (package == null)
            {
                Log.Error($"[YooAsset] Package not initialized: {packageName ?? "default"}");
                return null;
            }

            var handle = package.LoadSubAssetsAsync<T>(key);
            await UniTask.WaitUntil(() => handle.IsDone);

            if (handle.Status != EOperationStatus.Succeed)
            {
                Log.Error($"[YooAsset] Failed to load sub assets: {key}");
                return null;
            }
            return handle;
        }

        public async UniTask<RawFileHandle> LoadRawFileWithHandleAsync(string key, string packageName = null)
        {
            var package = GetPackage(packageName);
            if (package == null)
            {
                Log.Error($"[YooAsset] Package not initialized: {packageName ?? "default"}");
                return null;
            }

            var handle = package.LoadRawFileAsync(key);
            await UniTask.WaitUntil(() => handle.IsDone);

            if (handle.Status != EOperationStatus.Succeed)
            {
                Log.Error($"[YooAsset] Failed to load raw file: {key}");
                return null;
            }
            return handle;
        }

        public async UniTask<SceneHandle> LoadSceneWithHandleAsync(string key, LoadSceneMode mode, string packageName = null)
        {
            var package = GetPackage(packageName);
            if (package == null)
            {
                Log.Error($"[YooAsset] Package not initialized: {packageName ?? "default"}");
                return null;
            }

            var handle = package.LoadSceneAsync(key, mode);
            await UniTask.WaitUntil(() => handle.IsDone);

            if (handle.Status != EOperationStatus.Succeed)
            {
                Log.Error($"[YooAsset] Failed to load scene: {key}");
                return null;
            }
            return handle;
        }

        #endregion

        #region IResourcesLoader

        UniTask<T> IResourcesLoader.LoadAsync<T>(string key) => LoadAsync<T>(key, null);
        T IResourcesLoader.Load<T>(string key) => Load<T>(key, null);

        public async UniTask<T> LoadAsync<T>(string key, string packageName = null) where T : UnityEngine.Object
        {
            var package = GetPackage(packageName);
            if (package == null)
            {
                Log.Error($"[YooAsset] Package not initialized: {packageName ?? "default"}");
                return null;
            }

            var handle = package.LoadAssetAsync<T>(key);
            await UniTask.WaitUntil(() => handle.IsDone);

            if (handle.Status == EOperationStatus.Succeed)
                return handle.AssetObject as T;

            Log.Error($"[YooAsset] Failed to load asset: {key} from package: {packageName ?? "default"}");
            return null;
        }

        public T Load<T>(string key, string packageName = null) where T : UnityEngine.Object
        {
            var package = GetPackage(packageName);
            if (package == null)
            {
                Log.Error($"[YooAsset] Package not initialized: {packageName ?? "default"}");
                return null;
            }

            var handle = package.LoadAssetSync<T>(key);
            if (handle.Status == EOperationStatus.Succeed)
                return handle.AssetObject as T;

            Log.Error($"[YooAsset] Failed to load asset: {key} from package: {packageName ?? "default"}");
            return null;
        }

        #endregion

        #region IResourcesSystem

        public async UniTask<GameObject> InstantiateAsync(string key, Transform parent = null)
            => await InstantiateAsync(key, null, parent);

        public async UniTask<GameObject> InstantiateAsync(string key, string packageName, Transform parent)
        {
            var prefab = await LoadAsync<GameObject>(key, packageName);
            return prefab != null ? UnityEngine.Object.Instantiate(prefab, parent) : null;
        }

        public async UniTask LoadSceneAsync(string key, LoadSceneMode mode)
            => await LoadSceneAsync(key, null, mode);

        public async UniTask LoadSceneAsync(string key, string packageName, LoadSceneMode mode)
        {
            var package = GetPackage(packageName);
            if (package == null)
            {
                Log.Error($"[YooAsset] Package not initialized: {packageName ?? "default"}");
                return;
            }

            var op = package.LoadSceneAsync(key, mode);
            await UniTask.WaitUntil(() => op.IsDone);

            if (op.Status != EOperationStatus.Succeed)
                Log.Error($"[YooAsset] Failed to load scene: {key} from package: {packageName ?? "default"}");
        }

        #endregion

        public UniTask OnInitializeAsync()
        {
            _settings = YooAssetSettings.Instance;
            _defaultPackage = YooService.DefaultPackage;
            AzathrixFramework.ResourcesLoader = this;
            return UniTask.CompletedTask;
        }
    }
}
#endif
