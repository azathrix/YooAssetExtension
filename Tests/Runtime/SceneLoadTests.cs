#if YOOASSET_INSTALLED
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Azathrix.YooSystem;
using Cysharp.Threading.Tasks;
using SceneHandle = YooAsset.SceneHandle;

namespace Azathrix.YooSystem.Tests.Runtime
{
    /// <summary>
    /// 场景加载测试 (PlayMode)
    /// 需要在 TestPackage 中配置 TestScene 场景资源
    /// </summary>
    [TestFixture]
    public class SceneLoadTests : YooSystemTestBase
    {
        private const string TestSceneName = "TestScene";
        private const string TestPackageName = "TestPackage";

        #region LoadSceneAsync Tests

        [UnityTest]
        public IEnumerator LoadSceneAsync_Additive_LoadsScene()
        {
            SkipIfNotInitialized();

            var initialSceneCount = SceneManager.sceneCount;

            var task = _yooSystem.LoadSceneAsync(TestSceneName, TestPackageName, LoadSceneMode.Additive);
            yield return task.ToCoroutine();

            Assert.AreEqual(initialSceneCount + 1, SceneManager.sceneCount,
                "Scene count should increase after loading");

            // 卸载测试场景
            var scene = SceneManager.GetSceneByName(TestSceneName);
            if (scene.isLoaded)
            {
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        #endregion

        #region LoadSceneWithHandle Tests

        [UnityTest]
        public IEnumerator LoadSceneWithHandleAsync_ReturnsValidHandle()
        {
            SkipIfNotInitialized();

            SceneHandle handle = null;
            var task = _yooSystem.LoadSceneWithHandleAsync(TestSceneName, LoadSceneMode.Additive, TestPackageName);
            yield return task.ToCoroutine(r => handle = r);

            Assert.IsNotNull(handle, "Should return valid handle");
            Assert.IsTrue(handle.IsValid, "Handle should be valid");

            // 通过 handle 卸载场景
            if (handle != null && handle.IsValid)
            {
                yield return handle.UnloadAsync();
            }
        }

        [UnityTest]
        public IEnumerator LoadSceneWithHandle_ThenUnload_Works()
        {
            SkipIfNotInitialized();

            var initialSceneCount = SceneManager.sceneCount;

            // 加载
            SceneHandle handle = null;
            var loadTask = _yooSystem.LoadSceneWithHandleAsync(TestSceneName, LoadSceneMode.Additive, TestPackageName);
            yield return loadTask.ToCoroutine(r => handle = r);

            Assert.AreEqual(initialSceneCount + 1, SceneManager.sceneCount, "Scene should be loaded");

            // 卸载
            if (handle != null && handle.IsValid)
            {
                yield return handle.UnloadAsync();
            }

            // 等待一帧让卸载完成
            yield return null;

            Assert.AreEqual(initialSceneCount, SceneManager.sceneCount, "Scene should be unloaded");
        }

        #endregion
    }
}
#endif
