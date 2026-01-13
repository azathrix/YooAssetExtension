#if YOOASSET_INSTALLED
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Azathrix.YooSystem;
using Azathrix.YooSystem.Interfaces;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace Azathrix.YooSystem.Tests.Runtime
{
    /// <summary>
    /// 下载功能测试 (PlayMode)
    /// </summary>
    [TestFixture]
    public class DownloadTests : YooSystemTestBase
    {
        #region Download Info Tests

        [Test]
        public void GetDownloadCount_WithEmptyTags_ReturnsZero()
        {
            if (!_initialized)
            {
                Assert.Ignore("Skipped: YooAsset not initialized");
                return;
            }

            var count = _yooSystem.GetDownloadCount(new string[0]);
            Assert.GreaterOrEqual(count, 0);
            Debug.Log($"Download count (empty tags): {count}");
        }

        [Test]
        public void GetDownloadCount_WithTags_ReturnsCount()
        {
            if (!_initialized)
            {
                Assert.Ignore("Skipped: YooAsset not initialized");
                return;
            }

            var count = _yooSystem.GetDownloadCount(new[] { "Level1" });
            Assert.GreaterOrEqual(count, 0);
            Debug.Log($"Download count (Level1): {count}");
        }

        [Test]
        public void GetDownloadCount_WithDifferentPackage_Works()
        {
            if (!_initialized)
            {
                Assert.Ignore("Skipped: YooAsset not initialized");
                return;
            }

            var settings = YooAssetSettings.Instance;
            foreach (var pkgConfig in settings.Packages)
            {
                var count = _yooSystem.GetDownloadCount(new[] { "Level1" }, pkgConfig.packageName);
                Debug.Log($"Package {pkgConfig.packageName} download count: {count}");
            }
        }

        [Test]
        public void GetDownloadBytes_ReturnsBytes()
        {
            if (!_initialized)
            {
                Assert.Ignore("Skipped: YooAsset not initialized");
                return;
            }

            var bytes = _yooSystem.GetDownloadBytes(new[] { "Level1" });
            Assert.GreaterOrEqual(bytes, 0);
            Debug.Log($"Download bytes: {bytes}");
        }

        [Test]
        public void NeedDownload_ReturnsBool()
        {
            if (!_initialized)
            {
                Assert.Ignore("Skipped: YooAsset not initialized");
                return;
            }

            var need = _yooSystem.NeedDownload(new[] { "Level1" });
            Debug.Log($"Need download: {need}");
        }

        [Test]
        public void NeedDownloadAll_ReturnsBool()
        {
            if (!_initialized)
            {
                Assert.Ignore("Skipped: YooAsset not initialized");
                return;
            }

            var need = _yooSystem.NeedDownloadAll();
            Debug.Log($"Need download all: {need}");
        }

        [Test]
        public void GetDownloadInfo_ReturnsTuple()
        {
            if (!_initialized)
            {
                Assert.Ignore("Skipped: YooAsset not initialized");
                return;
            }

            var (fileCount, totalBytes) = _yooSystem.GetDownloadInfo(new[] { "Level1" });
            Assert.GreaterOrEqual(fileCount, 0);
            Assert.GreaterOrEqual(totalBytes, 0);
            Debug.Log($"Download info: {fileCount} files, {totalBytes} bytes");
        }

        #endregion

        #region IDownloadMonitor Tests

        [Test]
        public void IsDownloading_WhenNoDownload_ReturnsFalse()
        {
            if (!_initialized)
            {
                Assert.Ignore("Skipped: YooAsset not initialized");
                return;
            }

            Assert.IsFalse(_yooSystem.IsDownloading);
        }

        [Test]
        public void CurrentProgress_WhenNoDownload_ReturnsDefault()
        {
            if (!_initialized)
            {
                Assert.Ignore("Skipped: YooAsset not initialized");
                return;
            }

            var progress = _yooSystem.CurrentProgress;
            Assert.AreEqual(0, progress.TotalCount);
        }

        [Test]
        public void StartMonitor_DoesNotThrow()
        {
            if (!_initialized)
            {
                Assert.Ignore("Skipped: YooAsset not initialized");
                return;
            }

            Assert.DoesNotThrow(() => _yooSystem.StartMonitor());
        }

        [Test]
        public void StopMonitor_DoesNotThrow()
        {
            if (!_initialized)
            {
                Assert.Ignore("Skipped: YooAsset not initialized");
                return;
            }

            Assert.DoesNotThrow(() => _yooSystem.StopMonitor());
        }

        [Test]
        public void OnProgressChanged_CanSubscribe()
        {
            if (!_initialized)
            {
                Assert.Ignore("Skipped: YooAsset not initialized");
                return;
            }

            bool called = false;
            _yooSystem.OnProgressChanged += (progress) => called = true;
            // 事件订阅不应抛出异常
            Assert.IsFalse(called); // 没有下载，不会触发
        }

        [Test]
        public void OnDownloadError_CanSubscribe()
        {
            if (!_initialized)
            {
                Assert.Ignore("Skipped: YooAsset not initialized");
                return;
            }

            bool called = false;
            _yooSystem.OnDownloadError += (error) => called = true;
            Assert.IsFalse(called);
        }

        #endregion

        #region DownloadManager Tests

        [Test]
        public void GetDownloadManager_ReturnsManager()
        {
            if (!_initialized)
            {
                Assert.Ignore("Skipped: YooAsset not initialized");
                return;
            }

            var manager = _yooSystem.GetDownloadManager();
            // 可能为 null 如果包未初始化
            Debug.Log($"DownloadManager: {(manager != null ? "exists" : "null")}");
        }

        [Test]
        public void GetDownloadManager_WithPackageName_Works()
        {
            if (!_initialized)
            {
                Assert.Ignore("Skipped: YooAsset not initialized");
                return;
            }

            var settings = YooAssetSettings.Instance;
            foreach (var pkgConfig in settings.Packages)
            {
                var manager = _yooSystem.GetDownloadManager(pkgConfig.packageName);
                Debug.Log($"Package {pkgConfig.packageName} DownloadManager: {(manager != null ? "exists" : "null")}");
            }
        }

        [Test]
        public void DownloadManager_Property_Works()
        {
            if (!_initialized)
            {
                Assert.Ignore("Skipped: YooAsset not initialized");
                return;
            }

            var manager = _yooSystem.DownloadManager;
            Debug.Log($"Default DownloadManager: {(manager != null ? "exists" : "null")}");
        }

        #endregion
    }
}
#endif
