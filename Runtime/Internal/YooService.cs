#if YOOASSET_INSTALLED
using System.Collections.Generic;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace Azathrix.YooSystem
{
    /// <summary>
    /// YooAsset 内部服务类（静态）
    /// 提供 Package 管理、版本更新、下载等核心功能
    /// 外部应通过 YooSystem 访问
    /// </summary>
    internal static class YooService
    {
        private static readonly Dictionary<string, ResourcePackage> _packages = new();
        private static readonly Dictionary<string, string> _packageVersions = new();
        private static YooAssetSettings _settings;
        private static bool _initialized;

        public static YooAssetSettings Settings => _settings;
        public static bool IsInitialized => _initialized;
        public static ResourcePackage DefaultPackage => GetPackage();

        #region 初始化

        public static void Initialize(YooAssetSettings settings)
        {
            if (_initialized) return;
            _settings = settings;
            YooAssets.Initialize();
            _initialized = true;
        }

        #endregion

        #region Package 管理

        public static ResourcePackage GetPackage(string packageName = null)
        {
            if (string.IsNullOrEmpty(packageName))
                packageName = _settings?.DefaultPackageName;
            if (string.IsNullOrEmpty(packageName)) return null;

            if (_packages.TryGetValue(packageName, out var pkg))
                return pkg;

            var package = YooAssets.TryGetPackage(packageName);
            if (package != null)
                _packages[packageName] = package;
            return package;
        }

        public static bool IsPackageInitialized(string packageName = null)
        {
            var pkg = GetPackage(packageName);
            return pkg != null && pkg.InitializeStatus == EOperationStatus.Succeed;
        }

        #endregion

        #region Package 初始化

        public static async UniTask<bool> InitPackageAsync(string packageName)
        {
            var pkgConfig = GetPackageConfig(packageName);
            if (pkgConfig == null) return false;
            return await InitPackageInternalAsync(pkgConfig);
        }

        public static async UniTask<bool> InitAllPackagesAsync()
        {
            if (_settings?.Packages == null || _settings.Packages.Length == 0)
                return false;

            foreach (var pkgConfig in _settings.Packages)
            {
                if (!await InitPackageInternalAsync(pkgConfig))
                    return false;
            }

            var defaultPkg = GetPackage(_settings.DefaultPackageName);
            if (defaultPkg != null)
                YooAssets.SetDefaultPackage(defaultPkg);

            return true;
        }

        private static async UniTask<bool> InitPackageInternalAsync(PackageConfig pkgConfig)
        {
            var packageName = pkgConfig.packageName;
            var package = YooAssets.TryGetPackage(packageName) ?? YooAssets.CreatePackage(packageName);
            _packages[packageName] = package;

            if (package.InitializeStatus == EOperationStatus.Succeed)
            {
                Log.Info($"[YooAsset] Package {packageName} already initialized, skipping");
                return true;
            }

            var initOp = CreateInitOperation(package, pkgConfig);
            if (initOp == null)
            {
                if (_settings.PlayMode == EPlayMode.EditorSimulateMode)
                {
                    Log.Info($"[YooAsset] Package {packageName} skipped (no collector config)");
                    return true;
                }
                return false;
            }

            await UniTask.WaitUntil(() => initOp.IsDone);

            if (initOp.Status != EOperationStatus.Succeed)
            {
                Log.Error($"[YooAsset] Package init failed: {initOp.Error}");
                return false;
            }

            Log.Info($"[YooAsset] Package {packageName} initialized");
            return true;
        }

        private static InitializationOperation CreateInitOperation(ResourcePackage package, PackageConfig pkgConfig)
        {
            return _settings.PlayMode switch
            {
                EPlayMode.EditorSimulateMode => CreateEditorSimulateInit(package, pkgConfig.packageName),
                EPlayMode.OfflinePlayMode => CreateOfflineInit(package),
                EPlayMode.HostPlayMode => CreateHostInit(package, pkgConfig),
                EPlayMode.WebPlayMode => CreateWebInit(package, pkgConfig),
                _ => null
            };
        }

        private static InitializationOperation CreateEditorSimulateInit(ResourcePackage package, string packageName)
        {
#if UNITY_EDITOR
            try
            {
                var simulateBuildResult = EditorSimulateModeHelper.SimulateBuild(packageName);
                var editorFileSystem = FileSystemParameters.CreateDefaultEditorFileSystemParameters(simulateBuildResult.PackageRootDirectory);
                var param = new EditorSimulateModeParameters { EditorFileSystemParameters = editorFileSystem };
                return package.InitializeAsync(param);
            }
            catch (System.Exception e)
            {
                Log.Warning($"[YooAsset] {packageName} 没有配置资源，跳过初始化: {e.Message}");
                return null;
            }
#else
            return null;
#endif
        }

        private static InitializationOperation CreateOfflineInit(ResourcePackage package)
        {
            var buildinFileSystem = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            var param = new OfflinePlayModeParameters { BuildinFileSystemParameters = buildinFileSystem };
            return package.InitializeAsync(param);
        }

        private static InitializationOperation CreateHostInit(ResourcePackage package, PackageConfig pkgConfig)
        {
            var remoteUrl = _settings.GetHostServerURL(pkgConfig);
            var fallbackUrl = _settings.GetFallbackHostServerURL(pkgConfig);

            var fullUrl = BuildFullUrl(remoteUrl, pkgConfig.packageName);
            var fullFallbackUrl = BuildFullUrl(string.IsNullOrEmpty(fallbackUrl) ? remoteUrl : fallbackUrl, pkgConfig.packageName);

            var buildinFileSystem = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
            var cacheFileSystem = FileSystemParameters.CreateDefaultCacheFileSystemParameters(
                new RemoteServices(fullUrl, fullFallbackUrl));

            var param = new HostPlayModeParameters
            {
                BuildinFileSystemParameters = buildinFileSystem,
                CacheFileSystemParameters = cacheFileSystem
            };
            return package.InitializeAsync(param);
        }

        private static InitializationOperation CreateWebInit(ResourcePackage package, PackageConfig pkgConfig)
        {
            var remoteUrl = _settings.GetHostServerURL(pkgConfig);
            var fallbackUrl = _settings.GetFallbackHostServerURL(pkgConfig);

            var fullUrl = BuildFullUrl(remoteUrl, pkgConfig.packageName);
            var fullFallbackUrl = BuildFullUrl(string.IsNullOrEmpty(fallbackUrl) ? remoteUrl : fallbackUrl, pkgConfig.packageName);

            var webServerFileSystem = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
            var webRemoteFileSystem = FileSystemParameters.CreateDefaultWebRemoteFileSystemParameters(
                new RemoteServices(fullUrl, fullFallbackUrl));

            var param = new WebPlayModeParameters
            {
                WebServerFileSystemParameters = webServerFileSystem,
                WebRemoteFileSystemParameters = webRemoteFileSystem
            };
            return package.InitializeAsync(param);
        }

        private static string BuildFullUrl(string baseUrl, string packageName)
        {
            var parts = new List<string> { baseUrl.TrimEnd('/') };
            if (!string.IsNullOrEmpty(_settings.ProjectId))
                parts.Add(_settings.ProjectId);
            parts.Add(_settings.PlatformName);
            parts.Add(_settings.GameVersion);
            parts.Add(packageName);
            return string.Join("/", parts);
        }

        #endregion

        #region 版本管理

        public static async UniTask<(bool success, string version)> UpdateVersionAsync(string packageName = null)
        {
            var package = GetPackage(packageName);
            if (package == null) return (false, null);

            var op = package.RequestPackageVersionAsync();
            await UniTask.WaitUntil(() => op.IsDone);

            if (op.Status != EOperationStatus.Succeed)
            {
                Log.Error($"[YooAsset] Update version failed: {op.Error}");
                return (false, null);
            }

            var pkgName = packageName ?? _settings.DefaultPackageName;
            _packageVersions[pkgName] = op.PackageVersion;
            Log.Info($"[YooAsset] Package {pkgName} version: {op.PackageVersion}");
            return (true, op.PackageVersion);
        }

        public static async UniTask<bool> UpdateAllVersionsAsync()
        {
            foreach (var pkgConfig in _settings.Packages)
            {
                var (success, _) = await UpdateVersionAsync(pkgConfig.packageName);
                if (!success) return false;
            }
            return true;
        }

        public static async UniTask<bool> UpdateManifestAsync(string packageName = null)
        {
            var package = GetPackage(packageName);
            if (package == null) return false;

            var pkgName = packageName ?? _settings.DefaultPackageName;
            if (!_packageVersions.TryGetValue(pkgName, out var version))
            {
                var versionOp = package.RequestPackageVersionAsync();
                await UniTask.WaitUntil(() => versionOp.IsDone);
                if (versionOp.Status != EOperationStatus.Succeed)
                {
                    Log.Error($"[YooAsset] Request version failed: {versionOp.Error}");
                    return false;
                }
                version = versionOp.PackageVersion;
                _packageVersions[pkgName] = version;
            }

            var op = package.UpdatePackageManifestAsync(version);
            await UniTask.WaitUntil(() => op.IsDone);

            if (op.Status != EOperationStatus.Succeed)
            {
                Log.Error($"[YooAsset] Update manifest failed: {op.Error}");
                return false;
            }

            Log.Info($"[YooAsset] Package {pkgName} manifest updated");
            return true;
        }

        public static async UniTask<bool> UpdateAllManifestsAsync()
        {
            foreach (var pkgConfig in _settings.Packages)
            {
                if (!await UpdateManifestAsync(pkgConfig.packageName))
                    return false;
            }
            return true;
        }

        public static string GetCachedVersion(string packageName = null)
        {
            var pkgName = packageName ?? _settings?.DefaultPackageName;
            return _packageVersions.GetValueOrDefault(pkgName);
        }

        #endregion

        #region 下载管理

        public static ResourceDownloaderOperation CreateDownloaderByTags(string packageName, params string[] tags)
        {
            var package = GetPackage(packageName);
            return package?.CreateResourceDownloader(tags, _settings.DownloadingMaxNum, _settings.FailedTryAgain);
        }

        public static ResourceDownloaderOperation CreateDownloaderByPaths(string packageName, params string[] paths)
        {
            var package = GetPackage(packageName);
            return package?.CreateBundleDownloader(paths, _settings.DownloadingMaxNum, _settings.FailedTryAgain);
        }

        public static (int fileCount, long totalBytes) GetDownloadInfo(string[] tags, string packageName = null)
        {
            var pkg = GetPackage(packageName);
            if (pkg == null || pkg.InitializeStatus != EOperationStatus.Succeed)
                return (0, 0);

            try
            {
                var downloader = pkg.CreateResourceDownloader(tags, 1, 1);
                return (downloader.TotalDownloadCount, downloader.TotalDownloadBytes);
            }
            catch { return (0, 0); }
        }

        public static bool NeedDownload(string[] tags, string packageName = null)
        {
            var (count, _) = GetDownloadInfo(tags, packageName);
            return count > 0;
        }

        #endregion

        #region 缓存管理

        public static async UniTask ClearUnusedCacheAsync(string packageName = null)
        {
            var pkg = GetPackage(packageName);
            if (pkg == null) return;

            var op = pkg.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles);
            await UniTask.WaitUntil(() => op.IsDone);
            Log.Info($"[YooAsset] Unused cache cleared for package: {packageName ?? "default"}");
        }

        public static async UniTask ClearAllCacheAsync(string packageName = null)
        {
            var pkg = GetPackage(packageName);
            if (pkg == null) return;

            var op = pkg.ClearCacheFilesAsync(EFileClearMode.ClearAllBundleFiles);
            await UniTask.WaitUntil(() => op.IsDone);
            Log.Info($"[YooAsset] All cache cleared for package: {packageName ?? "default"}");
        }

        #endregion

        #region 资源卸载

        public static async UniTask UnloadUnusedAssetsAsync(string packageName = null)
        {
            var pkg = GetPackage(packageName);
            if (pkg == null) return;

            var op = pkg.UnloadUnusedAssetsAsync();
            await UniTask.WaitUntil(() => op.IsDone);
            Log.Info($"[YooAsset] Unused assets unloaded for package: {packageName ?? "default"}");
        }

        public static async UniTask ForceUnloadAllAssetsAsync(string packageName = null)
        {
            var pkg = GetPackage(packageName);
            if (pkg == null) return;

            var op = pkg.UnloadAllAssetsAsync();
            await UniTask.WaitUntil(() => op.IsDone);
            Log.Info($"[YooAsset] All assets force unloaded for package: {packageName ?? "default"}");
        }

        #endregion

        #region 辅助

        private static PackageConfig GetPackageConfig(string packageName)
        {
            packageName ??= _settings?.DefaultPackageName;
            if (_settings?.Packages == null) return null;

            foreach (var pkg in _settings.Packages)
            {
                if (pkg.packageName == packageName)
                    return pkg;
            }
            return null;
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
