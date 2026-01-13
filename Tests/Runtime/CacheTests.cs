#if YOOASSET_INSTALLED
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Azathrix.YooSystem;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace Azathrix.YooSystem.Tests.Runtime
{
    /// <summary>
    /// 缓存和下载测试 (PlayMode)
    /// </summary>
    [TestFixture]
    public class CacheTests : YooSystemTestBase
    {
        private const string DefaultPackageName = "DefaultPackage";

        #region GetCacheInfo Tests

        [Test]
        public void GetCacheInfo_ReturnsCacheInfo()
        {
            SkipIfNotInitialized();

            var info = _yooSystem.GetCacheInfo();

            Assert.GreaterOrEqual(info.TotalSize, 0);
            Assert.GreaterOrEqual(info.FileCount, 0);
            Debug.Log($"Cache info: {info.FileCount} files, {info.TotalSize} bytes");
        }

        [Test]
        public void GetCacheInfo_WithPackageName_Works()
        {
            SkipIfNotInitialized();

            var settings = YooAssetSettings.Instance;
            foreach (var pkgConfig in settings.Packages)
            {
                var info = _yooSystem.GetCacheInfo(pkgConfig.packageName);
                Debug.Log($"Package {pkgConfig.packageName} cache: {info.FileCount} files, {info.TotalSize} bytes");
            }
        }

        #endregion

        #region ClearCache Tests

        [UnityTest]
        public IEnumerator ClearUnusedCacheAsync_Works()
        {
            SkipIfNotInitialized();

            var task = _yooSystem.ClearUnusedCacheAsync();
            yield return task.ToCoroutine();
        }

        [UnityTest]
        public IEnumerator ClearUnusedCacheAsync_WithPackageName_Works()
        {
            SkipIfNotInitialized();

            var task = _yooSystem.ClearUnusedCacheAsync(DefaultPackageName);
            yield return task.ToCoroutine();
        }

        [UnityTest]
        public IEnumerator ClearAllCacheAsync_Works()
        {
            SkipIfNotInitialized();

            var task = _yooSystem.ClearAllCacheAsync();
            yield return task.ToCoroutine();
        }

        [UnityTest]
        public IEnumerator ClearAllCacheAsync_WithPackageName_Works()
        {
            SkipIfNotInitialized();

            var task = _yooSystem.ClearAllCacheAsync(DefaultPackageName);
            yield return task.ToCoroutine();
        }

        #endregion

        #region Download Manager Tests

        [Test]
        public void GetDownloadManager_ReturnsManager()
        {
            SkipIfNotInitialized();

            var manager = _yooSystem.GetDownloadManager();

            Assert.IsNotNull(manager, "Should return download manager");
        }

        [Test]
        public void GetDownloadManager_WithPackageName_ReturnsManager()
        {
            SkipIfNotInitialized();

            var manager = _yooSystem.GetDownloadManager(DefaultPackageName);

            Assert.IsNotNull(manager, "Should return download manager for specific package");
        }

        [Test]
        public void DownloadManager_Property_ReturnsManager()
        {
            SkipIfNotInitialized();

            var manager = _yooSystem.DownloadManager;

            Assert.IsNotNull(manager, "DownloadManager property should return manager");
        }

        #endregion

        #region Download Info Tests

        [Test]
        public void GetDownloadCount_ReturnsCount()
        {
            SkipIfNotInitialized();

            var count = _yooSystem.GetDownloadCount(new[] { "TestTag" });

            Assert.GreaterOrEqual(count, 0, "Download count should be >= 0");
        }

        [Test]
        public void GetDownloadBytes_ReturnsBytes()
        {
            SkipIfNotInitialized();

            var bytes = _yooSystem.GetDownloadBytes(new[] { "TestTag" });

            Assert.GreaterOrEqual(bytes, 0, "Download bytes should be >= 0");
        }

        [Test]
        public void GetDownloadInfo_ReturnsTuple()
        {
            SkipIfNotInitialized();

            var (fileCount, totalBytes) = _yooSystem.GetDownloadInfo(new[] { "TestTag" });

            Assert.GreaterOrEqual(fileCount, 0, "File count should be >= 0");
            Assert.GreaterOrEqual(totalBytes, 0, "Total bytes should be >= 0");
        }

        [Test]
        public void NeedDownload_ReturnsBool()
        {
            SkipIfNotInitialized();

            var needDownload = _yooSystem.NeedDownload(new[] { "TestTag" });

            Debug.Log($"NeedDownload: {needDownload}");
        }

        [Test]
        public void NeedDownloadAll_ReturnsBool()
        {
            SkipIfNotInitialized();

            var needDownloadAll = _yooSystem.NeedDownloadAll();

            Debug.Log($"NeedDownloadAll: {needDownloadAll}");
        }

        #endregion

        #region Monitor Tests

        [Test]
        public void StartMonitor_DoesNotThrow()
        {
            SkipIfNotInitialized();

            Assert.DoesNotThrow(() => _yooSystem.StartMonitor());
        }

        [Test]
        public void StopMonitor_DoesNotThrow()
        {
            SkipIfNotInitialized();

            Assert.DoesNotThrow(() => _yooSystem.StopMonitor());
        }

        [Test]
        public void StartAndStopMonitor_Works()
        {
            SkipIfNotInitialized();

            _yooSystem.StartMonitor();
            _yooSystem.StopMonitor();
        }

        #endregion
    }
}
#endif
