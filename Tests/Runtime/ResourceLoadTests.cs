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
    /// 资源加载测试 (PlayMode)
    /// 基于真实资源测试加载功能
    /// </summary>
    [TestFixture]
    public class ResourceLoadTests : YooSystemTestBase
    {
        private const string TestPrefabName = "TestPrefab";
        private const string TestSpriteName = "TestSprite";      // Sprite Atlas 或多子资源
        private const string TestRawFileName = "TestRawFile";    // 原始文件 (.txt/.json/.bytes)

        #region CheckAssetExists Tests

        [Test]
        public void CheckAssetExists_WithTestPrefab_ReturnsTrue()
        {
            SkipIfNotInitialized();

            var exists = _yooSystem.CheckAssetExists(TestPrefabName);
            Assert.IsTrue(exists, $"Asset '{TestPrefabName}' should exist in DefaultPackage");
        }

        [Test]
        public void CheckAssetExists_WithInvalidAsset_ReturnsFalse()
        {
            SkipIfNotInitialized();

            var exists = _yooSystem.CheckAssetExists("NonExistentAsset_12345");
            Assert.IsFalse(exists);
        }

        #endregion

        #region LoadAsync Tests

        [UnityTest]
        public IEnumerator LoadAsync_WithTestPrefab_ReturnsGameObject()
        {
            SkipIfNotInitialized();

            GameObject result = null;
            var task = _yooSystem.LoadAsync<GameObject>(TestPrefabName);
            yield return task.ToCoroutine(r => result = r);

            Assert.IsNotNull(result, $"Should load '{TestPrefabName}' successfully");
            Assert.AreEqual(TestPrefabName, result.name);
        }

        [UnityTest]
        public IEnumerator LoadAsync_MultipleTimes_ReturnsSameAsset()
        {
            SkipIfNotInitialized();

            GameObject result1 = null;
            GameObject result2 = null;

            var task1 = _yooSystem.LoadAsync<GameObject>(TestPrefabName);
            yield return task1.ToCoroutine(r => result1 = r);

            var task2 = _yooSystem.LoadAsync<GameObject>(TestPrefabName);
            yield return task2.ToCoroutine(r => result2 = r);

            Assert.IsNotNull(result1);
            Assert.IsNotNull(result2);
            Assert.AreEqual(result1, result2, "Loading same asset should return same reference");
        }

        #endregion

        #region Load Sync Tests

        [Test]
        public void Load_WithTestPrefab_ReturnsGameObject()
        {
            SkipIfNotInitialized();

            var result = _yooSystem.Load<GameObject>(TestPrefabName);

            Assert.IsNotNull(result, $"Should load '{TestPrefabName}' synchronously");
            Assert.AreEqual(TestPrefabName, result.name);
        }

        #endregion

        #region LoadAssetWithHandle Tests

        [UnityTest]
        public IEnumerator LoadAssetWithHandleAsync_ReturnsValidHandle()
        {
            SkipIfNotInitialized();

            AssetHandle handle = null;
            var task = _yooSystem.LoadAssetWithHandleAsync<GameObject>(TestPrefabName);
            yield return task.ToCoroutine(r => handle = r);

            Assert.IsNotNull(handle, "Should return valid handle");
            Assert.IsTrue(handle.IsValid, "Handle should be valid");
            Assert.IsNotNull(handle.AssetObject, "Handle should contain asset");

            handle.Release();
        }

        [Test]
        public void LoadAssetWithHandle_Sync_ReturnsValidHandle()
        {
            SkipIfNotInitialized();

            var handle = _yooSystem.LoadAssetWithHandle<GameObject>(TestPrefabName);

            Assert.IsNotNull(handle, "Should return valid handle");
            Assert.IsTrue(handle.IsValid, "Handle should be valid");

            handle.Release();
        }

        #endregion

        #region LoadAllAssetsWithHandle Tests

        [UnityTest]
        public IEnumerator LoadAllAssetsWithHandleAsync_ReturnsValidHandle()
        {
            SkipIfNotInitialized();

            // 使用 TestPrefab 所在目录加载所有资源
            AllAssetsHandle handle = null;
            var task = _yooSystem.LoadAllAssetsWithHandleAsync<GameObject>(TestPrefabName);
            yield return task.ToCoroutine(r => handle = r);

            // 如果资源存在，handle 应该有效
            if (handle != null && handle.IsValid)
            {
                Assert.IsTrue(handle.AllAssetObjects.Count > 0, "Should load at least one asset");
                handle.Release();
            }
        }

        #endregion

        #region LoadSubAssetsWithHandle Tests

        [UnityTest]
        public IEnumerator LoadSubAssetsWithHandleAsync_WithSprite_ReturnsHandle()
        {
            SkipIfNotInitialized();

            // 需要 Sprite Atlas 或包含子资源的文件
            if (!_yooSystem.CheckAssetExists(TestSpriteName))
            {
                Assert.Ignore($"Test asset '{TestSpriteName}' not configured");
                yield break;
            }

            SubAssetsHandle handle = null;
            var task = _yooSystem.LoadSubAssetsWithHandleAsync<Sprite>(TestSpriteName);
            yield return task.ToCoroutine(r => handle = r);

            Assert.IsNotNull(handle, "Should return handle");
            if (handle != null && handle.IsValid)
            {
                handle.Release();
            }
        }

        #endregion

        #region LoadRawFileWithHandle Tests

        [UnityTest]
        public IEnumerator LoadRawFileWithHandleAsync_ReturnsHandle()
        {
            SkipIfNotInitialized();

            // 需要原始文件资源
            if (!_yooSystem.CheckAssetExists(TestRawFileName))
            {
                Assert.Ignore($"Test asset '{TestRawFileName}' not configured");
                yield break;
            }

            RawFileHandle handle = null;
            var task = _yooSystem.LoadRawFileWithHandleAsync(TestRawFileName);
            yield return task.ToCoroutine(r => handle = r);

            Assert.IsNotNull(handle, "Should return handle");
            if (handle != null && handle.IsValid)
            {
                Assert.IsFalse(string.IsNullOrEmpty(handle.GetRawFileText()), "Should have content");
                handle.Release();
            }
        }

        #endregion

        #region InstantiateAsync Tests

        [UnityTest]
        public IEnumerator InstantiateAsync_CreatesInstance()
        {
            SkipIfNotInitialized();

            GameObject instance = null;
            var task = _yooSystem.InstantiateAsync(TestPrefabName);
            yield return task.ToCoroutine(r => instance = r);

            Assert.IsNotNull(instance, "Should instantiate prefab");
            Assert.IsTrue(instance.name.StartsWith(TestPrefabName), "Instance name should match prefab");
            Assert.IsTrue(instance.activeInHierarchy, "Instance should be active in scene");

            Object.Destroy(instance);
        }

        [UnityTest]
        public IEnumerator InstantiateAsync_WithParent_SetsParentCorrectly()
        {
            SkipIfNotInitialized();

            var parent = new GameObject("TestParent").transform;
            GameObject instance = null;

            var task = _yooSystem.InstantiateAsync(TestPrefabName, parent);
            yield return task.ToCoroutine(r => instance = r);

            Assert.IsNotNull(instance, "Should instantiate prefab");
            Assert.AreEqual(parent, instance.transform.parent, "Instance should be child of parent");

            Object.Destroy(parent.gameObject);
        }

        #endregion
    }
}
#endif
