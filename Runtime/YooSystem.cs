#if YOOASSET_INSTALLED
using System;
using System.Collections.Generic;
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
        private readonly Dictionary<string, ResourcePackage> _packages = new();
        private readonly Dictionary<string, DownloadManager> _downloadManagers = new();

        // IDownloadMonitor
        public event Action<DownloadProgress> OnProgressChanged;
        public event Action<string> OnDownloadError;
#pragma warning disable CS0067 // 接口要求的事件，暂未使用
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
        {
            if (string.IsNullOrEmpty(packageName))
                return _defaultPackage;

            if (_packages.TryGetValue(packageName, out var package))
                return package;

            // 尝试从 YooAssets 获取（需要先检查是否已初始化）
            try
            {
                var pkg = YooAssets.TryGetPackage(packageName);
                if (pkg != null)
                    _packages[packageName] = pkg;
                return pkg;
            }
            catch
            {
                // YooAssets 未初始化
                return null;
            }
        }

        /// <summary>
        /// 获取指定包的下载管理器
        /// </summary>
        public DownloadManager GetDownloadManager(string packageName = null)
        {
            packageName ??= _settings.DefaultPackageName;

            if (_downloadManagers.TryGetValue(packageName, out var manager))
                return manager;

            var package = GetPackage(packageName);
            if (package == null)
                return null;

            manager = new DownloadManager(package, _settings);
            _downloadManagers[packageName] = manager;
            return manager;
        }

        /// <summary>
        /// 获取默认包的下载管理器（兼容旧 API）
        /// </summary>
        public DownloadManager DownloadManager => GetDownloadManager();

        #region Package & Asset Status

        /// <summary>
        /// 检查包是否已初始化
        /// </summary>
        public bool IsPackageInitialized(string packageName = null)
        {
            var pkg = GetPackage(packageName);
            return pkg != null && pkg.InitializeStatus == EOperationStatus.Succeed;
        }

        /// <summary>
        /// 检查资源路径是否有效
        /// </summary>
        public bool CheckAssetExists(string assetPath, string packageName = null)
        {
            var pkg = GetPackage(packageName);
            return pkg != null && pkg.CheckLocationValid(assetPath);
        }

        /// <summary>
        /// 获取指定标签需要下载的文件数量
        /// </summary>
        public int GetDownloadCount(string[] tags, string packageName = null)
        {
            var pkg = GetPackage(packageName);
            if (pkg == null) return 0;

            var downloader = pkg.CreateResourceDownloader(tags, 1, 1);
            return downloader.TotalDownloadCount;
        }

        /// <summary>
        /// 获取指定标签需要下载的总字节数
        /// </summary>
        public long GetDownloadBytes(string[] tags, string packageName = null)
        {
            var pkg = GetPackage(packageName);
            if (pkg == null) return 0;

            var downloader = pkg.CreateResourceDownloader(tags, 1, 1);
            return downloader.TotalDownloadBytes;
        }

        /// <summary>
        /// 检查指定标签的资源是否需要下载
        /// </summary>
        public bool NeedDownload(string[] tags, string packageName = null)
        {
            return GetDownloadCount(tags, packageName) > 0;
        }

        /// <summary>
        /// 检查包内所有资源是否需要下载
        /// </summary>
        public bool NeedDownloadAll(string packageName = null)
        {
            var pkg = GetPackage(packageName);
            if (pkg == null) return false;

            var downloader = pkg.CreateResourceDownloader(_settings.DownloadingMaxNum, _settings.FailedTryAgain);
            return downloader.TotalDownloadCount > 0;
        }

        /// <summary>
        /// 获取下载信息（文件数和字节数）
        /// </summary>
        public (int fileCount, long totalBytes) GetDownloadInfo(string[] tags, string packageName = null)
        {
            var pkg = GetPackage(packageName);
            if (pkg == null) return (0, 0);

            var downloader = pkg.CreateResourceDownloader(tags, 1, 1);
            return (downloader.TotalDownloadCount, downloader.TotalDownloadBytes);
        }

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
            // YooAsset 2.x 暂无直接获取缓存大小的API
            return new CacheInfo { TotalSize = 0, FileCount = 0 };
        }

        public async UniTask ClearUnusedCacheAsync(string packageName = null)
        {
            var pkg = GetPackage(packageName);
            if (pkg == null) return;

            var op = pkg.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles);
            await UniTask.WaitUntil(() => op.IsDone);
            Log.Info("[YooAsset] Unused cache cleared");
        }

        public async UniTask ClearAllCacheAsync(string packageName = null)
        {
            var pkg = GetPackage(packageName);
            if (pkg == null) return;

            var op = pkg.ClearCacheFilesAsync(EFileClearMode.ClearAllBundleFiles);
            await UniTask.WaitUntil(() => op.IsDone);
            Log.Info("[YooAsset] All cache cleared");
        }

        #endregion

        #region Resource Unload & Release

        /// <summary>
        /// 卸载未使用的资源（异步）
        /// </summary>
        public async UniTask UnloadUnusedAssetsAsync(string packageName = null)
        {
            var pkg = GetPackage(packageName);
            if (pkg == null) return;

            var op = pkg.UnloadUnusedAssetsAsync();
            await UniTask.WaitUntil(() => op.IsDone);
            Log.Info($"[YooAsset] Unused assets unloaded for package: {packageName ?? "default"}");
        }

        /// <summary>
        /// 强制卸载所有资源（异步）
        /// </summary>
        public async UniTask ForceUnloadAllAssetsAsync(string packageName = null)
        {
            var pkg = GetPackage(packageName);
            if (pkg == null) return;

            var op = pkg.UnloadAllAssetsAsync();
            await UniTask.WaitUntil(() => op.IsDone);
            Log.Info($"[YooAsset] All assets force unloaded for package: {packageName ?? "default"}");
        }

        /// <summary>
        /// 尝试卸载指定资源
        /// </summary>
        public bool TryUnloadUnusedAsset(string assetPath, string packageName = null)
        {
            var pkg = GetPackage(packageName);
            if (pkg == null) return false;

            pkg.TryUnloadUnusedAsset(assetPath);
            return true;
        }

        /// <summary>
        /// 卸载所有包的未使用资源
        /// </summary>
        public async UniTask UnloadAllPackagesUnusedAssetsAsync()
        {
            foreach (var pkgConfig in _settings.Packages)
            {
                await UnloadUnusedAssetsAsync(pkgConfig.packageName);
            }
        }

        /// <summary>
        /// 强制卸载所有包的所有资源
        /// </summary>
        public async UniTask ForceUnloadAllPackagesAssetsAsync()
        {
            foreach (var pkgConfig in _settings.Packages)
            {
                await ForceUnloadAllAssetsAsync(pkgConfig.packageName);
            }
        }

        #endregion

        #region Asset Handle Loading (Manual Release)

        /// <summary>
        /// 加载资源并返回 Handle（需要手动调用 Release）
        /// </summary>
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

        /// <summary>
        /// 同步加载资源并返回 Handle（需要手动调用 Release）
        /// </summary>
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

        /// <summary>
        /// 加载所有子资源并返回 Handle
        /// </summary>
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

        /// <summary>
        /// 加载子资源并返回 Handle
        /// </summary>
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

        /// <summary>
        /// 加载原生文件并返回 Handle
        /// </summary>
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

        /// <summary>
        /// 加载场景并返回 Handle（可用于卸载场景）
        /// </summary>
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

        UniTask<T> IResourcesLoader.LoadAsync<T>(string key)
        {
            return LoadAsync<T>(key, null);
        }

        T IResourcesLoader.Load<T>(string key)
        {
            return Load<T>(key, null);
        }

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
            {
                return handle.AssetObject as T;
            }

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
            {
                return handle.AssetObject as T;
            }

            Log.Error($"[YooAsset] Failed to load asset: {key} from package: {packageName ?? "default"}");
            return null;
        }

        #endregion

        #region IResourcesSystem

        public async UniTask<GameObject> InstantiateAsync(string key, Transform parent = null)
        {
            return await InstantiateAsync(key, null, parent);
        } 

        public async UniTask<GameObject> InstantiateAsync(string key, string packageName, Transform parent)
        {
            var prefab = await LoadAsync<GameObject>(key, packageName);
            return prefab != null ? UnityEngine.Object.Instantiate(prefab, parent) : null;
        }

        public async UniTask LoadSceneAsync(string key, LoadSceneMode mode)
        {
            await LoadSceneAsync(key, null, mode);
        }

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
            {
                Log.Error($"[YooAsset] Failed to load scene: {key} from package: {packageName ?? "default"}");
            }
        }

        #endregion

        public UniTask OnInitializeAsync()
        {
            // 获取已初始化的包（由 HotUpdatePhase 初始化）
            _settings = YooAssetSettings.Instance;

            // 注册所有已初始化的包
            foreach (var pkgConfig in _settings.Packages)
            {
                var pkg = YooAssets.TryGetPackage(pkgConfig.packageName);
                if (pkg != null)
                {
                    _packages[pkgConfig.packageName] = pkg;
                }
            }

            // 设置默认包
            _defaultPackage = GetPackage(_settings.DefaultPackageName);

            //将框架的资源加载切到YooAsset系统
            AzathrixFramework.ResourcesLoader = this;
            return UniTask.CompletedTask;
        }
    }
}
#endif
