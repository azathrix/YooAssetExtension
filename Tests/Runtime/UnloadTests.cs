#if YOOASSET_INSTALLED
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Azathrix.YooSystem;
using Cysharp.Threading.Tasks;

namespace Azathrix.YooSystem.Tests.Runtime
{
    /// <summary>
    /// 资源卸载测试 (PlayMode)
    /// </summary>
    [TestFixture]
    public class UnloadTests : YooSystemTestBase
    {
        private const string TestPrefabName = "TestPrefab";
        private const string DefaultPackageName = "DefaultPackage";

        #region UnloadUnusedAssets Tests

        [UnityTest]
        public IEnumerator UnloadUnusedAssetsAsync_Works()
        {
            SkipIfNotInitialized();

            // 先加载资源
            var loadTask = _yooSystem.LoadAsync<GameObject>(TestPrefabName);
            yield return loadTask.ToCoroutine();

            // 卸载未使用资源
            var unloadTask = _yooSystem.UnloadUnusedAssetsAsync();
            yield return unloadTask.ToCoroutine();
        }

        [UnityTest]
        public IEnumerator UnloadUnusedAssetsAsync_WithPackageName_Works()
        {
            SkipIfNotInitialized();

            var task = _yooSystem.UnloadUnusedAssetsAsync(DefaultPackageName);
            yield return task.ToCoroutine();
        }

        #endregion

        #region ForceUnloadAllAssets Tests

        [UnityTest]
        public IEnumerator ForceUnloadAllAssetsAsync_Works()
        {
            SkipIfNotInitialized();

            var task = _yooSystem.ForceUnloadAllAssetsAsync();
            yield return task.ToCoroutine();

            // 强制卸载后，重新加载应该仍然可以工作
            GameObject result = null;
            var loadTask = _yooSystem.LoadAsync<GameObject>(TestPrefabName);
            yield return loadTask.ToCoroutine(r => result = r);

            Assert.IsNotNull(result, "Should be able to reload after force unload");
        }

        [UnityTest]
        public IEnumerator ForceUnloadAllAssetsAsync_WithPackageName_Works()
        {
            SkipIfNotInitialized();

            var task = _yooSystem.ForceUnloadAllAssetsAsync(DefaultPackageName);
            yield return task.ToCoroutine();
        }

        #endregion

        #region TryUnloadUnusedAsset Tests

        [UnityTest]
        public IEnumerator TryUnloadUnusedAsset_Works()
        {
            SkipIfNotInitialized();

            // 先加载资源
            var loadTask = _yooSystem.LoadAsync<GameObject>(TestPrefabName);
            yield return loadTask.ToCoroutine();

            // 尝试卸载
            var result = _yooSystem.TryUnloadUnusedAsset(TestPrefabName);
            Debug.Log($"TryUnloadUnusedAsset result: {result}");

            yield return null;
        }

        [UnityTest]
        public IEnumerator TryUnloadUnusedAsset_WithPackageName_Works()
        {
            SkipIfNotInitialized();

            var loadTask = _yooSystem.LoadAsync<GameObject>(TestPrefabName);
            yield return loadTask.ToCoroutine();

            var result = _yooSystem.TryUnloadUnusedAsset(TestPrefabName, DefaultPackageName);
            Debug.Log($"TryUnloadUnusedAsset with package result: {result}");

            yield return null;
        }

        #endregion

        #region UnloadAllPackages Tests

        [UnityTest]
        public IEnumerator UnloadAllPackagesUnusedAssetsAsync_Works()
        {
            SkipIfNotInitialized();

            var task = _yooSystem.UnloadAllPackagesUnusedAssetsAsync();
            yield return task.ToCoroutine();
        }

        [UnityTest]
        public IEnumerator ForceUnloadAllPackagesAssetsAsync_Works()
        {
            SkipIfNotInitialized();

            var task = _yooSystem.ForceUnloadAllPackagesAssetsAsync();
            yield return task.ToCoroutine();

            // 强制卸载后，重新加载应该仍然可以工作
            GameObject result = null;
            var loadTask = _yooSystem.LoadAsync<GameObject>(TestPrefabName);
            yield return loadTask.ToCoroutine(r => result = r);

            Assert.IsNotNull(result, "Should be able to reload after force unload all packages");
        }

        #endregion
    }
}
#endif
