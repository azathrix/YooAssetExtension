#if YOOASSET_INSTALLED
using System;
using System.Collections.Generic;
using Azathrix.Framework.Core.Startup;
using Azathrix.Framework.Tools;
using Azathrix.YooSystem.Interfaces;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace Azathrix.YooSystem.Startup
{
    /// <summary>
    /// 热更新阶段 - 在系统注册之前执行
    /// </summary>
    public class HotUpdatePhase : IHotUpdatePhase, IHotUpdateFlow
    {
        public string Id => "HotUpdate";
        public int Order => 50; // 在 ScanPhase(100) 之前

        // IHotUpdateFlow
        public HotUpdateState State { get; private set; }
        public string ErrorMessage { get; private set; }

        private readonly Dictionary<string, ResourcePackage> _packages = new();
        private readonly Dictionary<string, string> _packageVersions = new();
        private YooAssetSettings _settings;

        public async UniTask ExecuteAsync(PhaseContext context)
        {
            _settings = YooAssetSettings.Instance;

            Log.Info("[YooAsset] Starting hot update...");
            var success = await RunFullUpdateAsync();
            if (!success)
            {
                Log.Error($"[YooAsset] Hot update failed: {ErrorMessage}");
            }
        }

        #region IHotUpdateFlow

        /// <summary>
        /// 初始化单个包
        /// </summary>
        public async UniTask<bool> InitPackageAsync(string packageName = null)
        {
            var pkgConfig = GetPackageConfig(packageName);
            if (pkgConfig == null)
            {
                ErrorMessage = $"Package config not found: {packageName}";
                State = HotUpdateState.Failed;
                return false;
            }

            return await InitPackageInternalAsync(pkgConfig);
        }

        /// <summary>
        /// 初始化所有配置的包
        /// </summary>
        public async UniTask<bool> InitAllPackagesAsync()
        {
            State = HotUpdateState.InitPackage;
            YooAssets.Initialize();

            foreach (var pkgConfig in _settings.Packages)
            {
                if (!await InitPackageInternalAsync(pkgConfig))
                    return false;
            }

            // 设置第一个包为默认包
            if (_settings.Packages.Length > 0)
            {
                var defaultPkg = _packages.GetValueOrDefault(_settings.DefaultPackageName);
                if (defaultPkg != null)
                    YooAssets.SetDefaultPackage(defaultPkg);
            }

            return true;
        }

        private async UniTask<bool> InitPackageInternalAsync(PackageConfig pkgConfig)
        {
            State = HotUpdateState.InitPackage;
            var packageName = pkgConfig.packageName;

            var package = YooAssets.TryGetPackage(packageName) ?? YooAssets.CreatePackage(packageName);
            _packages[packageName] = package;

            InitializationOperation initOp = _settings.PlayMode switch
            {
                EPlayMode.EditorSimulateMode => InitEditorSimulate(package, packageName),
                EPlayMode.OfflinePlayMode => InitOffline(package, packageName),
                EPlayMode.HostPlayMode => InitHost(package, pkgConfig),
                EPlayMode.WebPlayMode => InitWebGL(package, pkgConfig),
                _ => null
            };

            if (initOp == null)
            {
                ErrorMessage = "Invalid play mode";
                State = HotUpdateState.Failed;
                return false;
            }

            await UniTask.WaitUntil(() => initOp.IsDone);

            if (initOp.Status != EOperationStatus.Succeed)
            {
                ErrorMessage = initOp.Error;
                State = HotUpdateState.Failed;
                Log.Error($"[YooAsset] Package init failed: {initOp.Error}");
                return false;
            }

            Log.Info($"[YooAsset] Package {packageName} initialized");
            return true;
        }

        public async UniTask<bool> UpdateVersionAsync()
        {
            return await UpdateVersionAsync(null);
        }

        public async UniTask<bool> UpdateVersionAsync(string packageName)
        {
            State = HotUpdateState.UpdateVersion;
            var package = GetPackage(packageName);
            if (package == null)
            {
                ErrorMessage = $"Package not found: {packageName}";
                State = HotUpdateState.Failed;
                return false;
            }

            var op = package.RequestPackageVersionAsync();
            await UniTask.WaitUntil(() => op.IsDone);

            if (op.Status != EOperationStatus.Succeed)
            {
                ErrorMessage = op.Error;
                State = HotUpdateState.Failed;
                Log.Error($"[YooAsset] Update version failed: {op.Error}");
                return false;
            }

            _packageVersions[packageName ?? _settings.DefaultPackageName] = op.PackageVersion;
            Log.Info($"[YooAsset] Package {packageName ?? "default"} version: {op.PackageVersion}");
            return true;
        }

        /// <summary>
        /// 更新所有包的版本
        /// </summary>
        public async UniTask<bool> UpdateAllVersionsAsync()
        {
            foreach (var pkgConfig in _settings.Packages)
            {
                if (!await UpdateVersionAsync(pkgConfig.packageName))
                    return false;
            }
            return true;
        }

        public async UniTask<bool> UpdateManifestAsync()
        {
            return await UpdateManifestAsync(null);
        }

        public async UniTask<bool> UpdateManifestAsync(string packageName)
        {
            State = HotUpdateState.UpdateManifest;
            var package = GetPackage(packageName);
            if (package == null)
            {
                ErrorMessage = $"Package not found: {packageName}";
                State = HotUpdateState.Failed;
                return false;
            }

            var pkgName = packageName ?? _settings.DefaultPackageName;
            if (!_packageVersions.TryGetValue(pkgName, out var version))
            {
                var versionOp = package.RequestPackageVersionAsync();
                await UniTask.WaitUntil(() => versionOp.IsDone);
                if (versionOp.Status != EOperationStatus.Succeed)
                {
                    ErrorMessage = versionOp.Error;
                    State = HotUpdateState.Failed;
                    return false;
                }
                version = versionOp.PackageVersion;
                _packageVersions[pkgName] = version;
            }

            var op = package.UpdatePackageManifestAsync(version);
            await UniTask.WaitUntil(() => op.IsDone);

            if (op.Status != EOperationStatus.Succeed)
            {
                ErrorMessage = op.Error;
                State = HotUpdateState.Failed;
                Log.Error($"[YooAsset] Update manifest failed: {op.Error}");
                return false;
            }

            Log.Info($"[YooAsset] Package {pkgName} manifest updated");
            return true;
        }

        /// <summary>
        /// 更新所有包的清单
        /// </summary>
        public async UniTask<bool> UpdateAllManifestsAsync()
        {
            foreach (var pkgConfig in _settings.Packages)
            {
                if (!await UpdateManifestAsync(pkgConfig.packageName))
                    return false;
            }
            return true;
        }

        public UniTask<ResourceDownloaderOperation> CreateDownloaderByTagsAsync(params string[] tags)
        {
            return CreateDownloaderByTagsAsync(null, tags);
        }

        public UniTask<ResourceDownloaderOperation> CreateDownloaderByTagsAsync(string packageName,
            params string[] tags)
        {
            State = HotUpdateState.CreateDownloader;
            var package = GetPackage(packageName);
            return UniTask.FromResult(package?.CreateResourceDownloader(tags,
                _settings.DownloadingMaxNum, _settings.FailedTryAgain));
        }

        public UniTask<ResourceDownloaderOperation> CreateDownloaderByPathsAsync(params string[] paths)
        {
            return CreateDownloaderByPathsAsync(null, paths);
        }

        public UniTask<ResourceDownloaderOperation> CreateDownloaderByPathsAsync(string packageName,
            params string[] paths)
        {
            State = HotUpdateState.CreateDownloader;
            var package = GetPackage(packageName);
            return UniTask.FromResult(package?.CreateBundleDownloader(paths,
                _settings.DownloadingMaxNum, _settings.FailedTryAgain));
        }

        public async UniTask<bool> RunFullUpdateAsync(string[] downloadTags = null)
        {
            try
            {
                if (!await InitAllPackagesAsync()) return false;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }

            if (_settings.PlayMode == EPlayMode.HostPlayMode)
            {
                if (!await UpdateAllVersionsAsync()) return false;
                if (!await UpdateAllManifestsAsync()) return false;

                // 下载每个包的资源
                foreach (var pkgConfig in _settings.Packages)
                {
                    var tags = pkgConfig.autoDownloadTags;
                    if (tags == null || tags.Length == 0) continue;

                    var downloader = await CreateDownloaderByTagsAsync(pkgConfig.packageName, tags);
                    if (downloader == null || downloader.TotalDownloadCount == 0) continue;

                    Log.Info(
                        $"[YooAsset] Package {pkgConfig.packageName} need download {downloader.TotalDownloadCount} files, {downloader.TotalDownloadBytes} bytes");

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

        #region Private Helpers

        private PackageConfig GetPackageConfig(string packageName)
        {
            packageName ??= _settings.DefaultPackageName;
            foreach (var pkg in _settings.Packages)
            {
                if (pkg.packageName == packageName)
                    return pkg;
            }
            return null;
        }

        private ResourcePackage GetPackage(string packageName)
        {
            packageName ??= _settings.DefaultPackageName;
            return _packages.GetValueOrDefault(packageName);
        }

        private InitializationOperation InitEditorSimulate(ResourcePackage package, string packageName)
        {
#if UNITY_EDITOR
            var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(packageName);
            var editorFileSystem = FileSystemParameters.CreateDefaultEditorFileSystemParameters(simulateBuildResult.PackageRootDirectory);
            var param = new EditorSimulateModeParameters();
            param.EditorFileSystemParameters = editorFileSystem;
            return package.InitializeAsync(param);
#else
            return null;
#endif
        }

        private InitializationOperation InitOffline(ResourcePackage package, string packageName)
        {
            var buildinFileSystem = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            var param = new OfflinePlayModeParameters();
            param.BuildinFileSystemParameters = buildinFileSystem;
            return package.InitializeAsync(param);
        }

        private InitializationOperation InitHost(ResourcePackage package, PackageConfig pkgConfig)
        {
            var remoteUrl = _settings.GetHostServerURL(pkgConfig);
            var fallbackUrl = _settings.GetFallbackHostServerURL(pkgConfig);

            // 构建完整URL
            var fullUrl = BuildFullUrl(remoteUrl, pkgConfig.packageName);
            var fullFallbackUrl = BuildFullUrl(string.IsNullOrEmpty(fallbackUrl) ? remoteUrl : fallbackUrl, pkgConfig.packageName);

            var buildinFileSystem = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            var cacheFileSystem = FileSystemParameters.CreateDefaultCacheFileSystemParameters(
                new RemoteServices(fullUrl, fullFallbackUrl));

            var param = new HostPlayModeParameters();
            param.BuildinFileSystemParameters = buildinFileSystem;
            param.CacheFileSystemParameters = cacheFileSystem;
            return package.InitializeAsync(param);
        }

        private InitializationOperation InitWebGL(ResourcePackage package, PackageConfig pkgConfig)
        {
            var remoteUrl = _settings.GetHostServerURL(pkgConfig);
            var fallbackUrl = _settings.GetFallbackHostServerURL(pkgConfig);

            var fullUrl = BuildFullUrl(remoteUrl, pkgConfig.packageName);
            var fullFallbackUrl = BuildFullUrl(string.IsNullOrEmpty(fallbackUrl) ? remoteUrl : fallbackUrl, pkgConfig.packageName);

            var webServerFileSystem = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
            var webRemoteFileSystem = FileSystemParameters.CreateDefaultWebRemoteFileSystemParameters(
                new RemoteServices(fullUrl, fullFallbackUrl));

            var param = new WebPlayModeParameters();
            param.WebServerFileSystemParameters = webServerFileSystem;
            param.WebRemoteFileSystemParameters = webRemoteFileSystem;
            return package.InitializeAsync(param);
        }

        private string BuildFullUrl(string baseUrl, string packageName)
        {
            var parts = new List<string>();
            parts.Add(baseUrl.TrimEnd('/'));
            if (!string.IsNullOrEmpty(_settings.ProjectId))
                parts.Add(_settings.ProjectId);
            parts.Add(_settings.PlatformName);
            parts.Add(_settings.GameVersion);
            parts.Add(packageName);
            return string.Join("/", parts);
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

            public string GetRemoteFallbackURL(string fileName) => $"{_fallbackUrl}/{fileName}";
        }

        #endregion
    }
}
#endif
