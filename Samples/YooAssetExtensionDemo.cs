#if YOOASSET_INSTALLED
using System;
using Azathrix.Framework.Core;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Azathrix.YooSystem.Samples.Samples
{
    /// <summary>
    /// YooAssetExtension 功能测试 Demo
    /// 挂载到场景中的 GameObject 上运行测试
    /// </summary>
    public class YooAssetExtensionDemo : MonoBehaviour
    {
        [Header("测试配置")]
        [SerializeField] private string testAssetPath = "Assets/Prefabs/TestPrefab";
        [SerializeField] private string testPackageName = "DefaultPackage";
        [SerializeField] private string[] testDownloadTags = { "Level1" };

        private YooSystem _yooSystem;

        private async void Start()
        {
            // 等待框架初始化完成
            await UniTask.WaitUntil(() => AzathrixFramework.IsStarted);

            _yooSystem = AzathrixFramework.GetSystem<YooSystem>();
            if (_yooSystem == null)
            {
                Log.Error("[Demo] YooSystem not found!");
                return;
            }

            Log.Info("========== YooAssetExtension Demo Start ==========");

            await RunAllTests();

            Log.Info("========== YooAssetExtension Demo End ==========");
        }

        private async UniTask RunAllTests()
        {
            TestPackageStatus();
            TestAssetExists();
            TestDownloadInfo();
            await TestAsyncLoad();
            TestSyncLoad();
            TestCacheInfo();
        }

        /// <summary>
        /// 测试包状态检测
        /// </summary>
        private void TestPackageStatus()
        {
            Log.Info("\n--- Test: Package Status ---");

            // 检测默认包
            var defaultInitialized = _yooSystem.IsPackageInitialized();
            Log.Info($"Default package initialized: {defaultInitialized}");

            // 检测指定包
            var specificInitialized = _yooSystem.IsPackageInitialized(testPackageName);
            Log.Info($"Package '{testPackageName}' initialized: {specificInitialized}");

            // 获取包实例
            var package = _yooSystem.GetPackage(testPackageName);
            Log.Info($"Package instance: {(package != null ? package.PackageName : "null")}");
        }

        /// <summary>
        /// 测试资源存在性检测
        /// </summary>
        private void TestAssetExists()
        {
            Log.Info("\n--- Test: Asset Exists ---");

            var exists = _yooSystem.CheckAssetExists(testAssetPath);
            Log.Info($"Asset '{testAssetPath}' exists in default package: {exists}");

            var existsInPackage = _yooSystem.CheckAssetExists(testAssetPath, testPackageName);
            Log.Info($"Asset '{testAssetPath}' exists in '{testPackageName}': {existsInPackage}");
        }

        /// <summary>
        /// 测试下载信息获取
        /// </summary>
        private void TestDownloadInfo()
        {
            Log.Info("\n--- Test: Download Info ---");

            // 检测是否需要下载
            var needDownload = _yooSystem.NeedDownload(testDownloadTags);
            Log.Info($"Tags {string.Join(",", testDownloadTags)} need download: {needDownload}");

            // 获取下载数量和大小
            var downloadCount = _yooSystem.GetDownloadCount(testDownloadTags);
            var downloadBytes = _yooSystem.GetDownloadBytes(testDownloadTags);
            Log.Info($"Download count: {downloadCount}, bytes: {FormatBytes(downloadBytes)}");

            // 获取完整下载信息
            var (fileCount, totalBytes) = _yooSystem.GetDownloadInfo(testDownloadTags);
            Log.Info($"Download info - files: {fileCount}, total: {FormatBytes(totalBytes)}");

            // 检测所有资源是否需要下载
            var needDownloadAll = _yooSystem.NeedDownloadAll();
            Log.Info($"Package need download all: {needDownloadAll}");
        }

        /// <summary>
        /// 测试异步加载
        /// </summary>
        private async UniTask TestAsyncLoad()
        {
            Log.Info("\n--- Test: Async Load ---");

            try
            {
                // 从默认包加载（先检查是否存在）
                if (_yooSystem.CheckAssetExists(testAssetPath))
                {
                    var prefab = await _yooSystem.LoadAsync<GameObject>(testAssetPath);
                    Log.Info($"Async load from default: {(prefab != null ? prefab.name : "null")}");
                }
                else
                {
                    Log.Info("Async load from default: skipped (asset not in default package)");
                }

                // 从指定包加载
                if (_yooSystem.CheckAssetExists(testAssetPath, testPackageName))
                {
                    var prefab2 = await _yooSystem.LoadAsync<GameObject>(testAssetPath, testPackageName);
                    Log.Info($"Async load from '{testPackageName}': {(prefab2 != null ? prefab2.name : "null")}");

                    // 实例化测试
                    if (prefab2 != null)
                    {
                        var instance = await _yooSystem.InstantiateAsync(testAssetPath, testPackageName, null);
                        Log.Info($"Instantiate result: {(instance != null ? instance.name : "null")}");
                        if (instance != null) Destroy(instance, 2f);
                    }
                }
                else
                {
                    Log.Info($"Async load from '{testPackageName}': skipped (asset not found)");
                }
            }
            catch (Exception e)
            {
                Log.Warning($"Async load test failed: {e.Message}");
            }
        }

        /// <summary>
        /// 测试同步加载
        /// </summary>
        private void TestSyncLoad()
        {
            Log.Info("\n--- Test: Sync Load ---");

            try
            {
                // 从默认包加载（先检查是否存在）
                if (_yooSystem.CheckAssetExists(testAssetPath))
                {
                    var prefab = _yooSystem.Load<GameObject>(testAssetPath);
                    Log.Info($"Sync load from default: {(prefab != null ? prefab.name : "null")}");
                }
                else
                {
                    Log.Info("Sync load from default: skipped (asset not in default package)");
                }

                // 从指定包加载
                if (_yooSystem.CheckAssetExists(testAssetPath, testPackageName))
                {
                    var prefab2 = _yooSystem.Load<GameObject>(testAssetPath, testPackageName);
                    Log.Info($"Sync load from '{testPackageName}': {(prefab2 != null ? prefab2.name : "null")}");
                }
                else
                {
                    Log.Info($"Sync load from '{testPackageName}': skipped (asset not found)");
                }
            }
            catch (Exception e)
            {
                Log.Warning($"Sync load test failed: {e.Message}");
            }
        }

        /// <summary>
        /// 测试缓存信息
        /// </summary>
        private void TestCacheInfo()
        {
            Log.Info("\n--- Test: Cache Info ---");

            var cacheInfo = _yooSystem.GetCacheInfo();
            Log.Info($"Cache - files: {cacheInfo.FileCount}, size: {FormatBytes(cacheInfo.TotalSize)}");
        }

        /// <summary>
        /// 测试下载功能（需要手动调用）
        /// </summary>
        [ContextMenu("Test Download")]
        public async void TestDownload()
        {
            Log.Info("\n--- Test: Download ---");

            var downloadManager = _yooSystem.GetDownloadManager(testPackageName);
            if (downloadManager == null)
            {
                Log.Error("DownloadManager not available");
                return;
            }

            downloadManager.OnTaskProgress += (taskId, progress) =>
            {
                Log.Info($"[{taskId}] Progress: {progress.CurrentCount}/{progress.TotalCount}, " +
                          $"{FormatBytes(progress.CurrentBytes)}/{FormatBytes(progress.TotalBytes)}");
            };

            downloadManager.OnTaskComplete += taskId =>
            {
                Log.Info($"[{taskId}] Download complete!");
            };

            downloadManager.OnTaskError += (taskId, error) =>
            {
                Log.Error($"[{taskId}] Error: {error}");
            };

            var success = await downloadManager.DownloadByTagsAsync("test-download", testDownloadTags);
            Log.Info($"Download result: {success}");
        }

        /// <summary>
        /// 测试清理缓存（需要手动调用）
        /// </summary>
        [ContextMenu("Clear Unused Cache")]
        public async void TestClearUnusedCache()
        {
            Log.Info("Clearing unused cache...");
            await _yooSystem.ClearUnusedCacheAsync();
            Log.Info("Unused cache cleared");
        }

        [ContextMenu("Clear All Cache")]
        public async void TestClearAllCache()
        {
            Log.Info("Clearing all cache...");
            await _yooSystem.ClearAllCacheAsync();
            Log.Info("All cache cleared");
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F2} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024f * 1024f):F2} MB";
            return $"{bytes / (1024f * 1024f * 1024f):F2} GB";
        }
    }
}
#endif
