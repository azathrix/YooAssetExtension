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
    /// 多 Package 测试 (PlayMode)
    /// 测试 DefaultPackage 和 TestPackage 的资源加载
    /// </summary>
    [TestFixture]
    public class MultiPackageTests : YooSystemTestBase
    {
        private const string TestPrefabName = "TestPrefab";
        private const string DefaultPackageName = "DefaultPackage";
        private const string TestPackageName = "TestPackage";

        #region Package Management Tests

        [Test]
        public void GetPackage_DefaultPackage_ReturnsPackage()
        {
            SkipIfNotInitialized();

            var pkg = _yooSystem.GetPackage(DefaultPackageName);
            Assert.IsNotNull(pkg, $"Should get {DefaultPackageName}");
            Assert.AreEqual(DefaultPackageName, pkg.PackageName);
        }

        [Test]
        public void GetPackage_TestPackage_ReturnsPackage()
        {
            SkipIfNotInitialized();

            var pkg = _yooSystem.GetPackage(TestPackageName);
            Assert.IsNotNull(pkg, $"Should get {TestPackageName}");
            Assert.AreEqual(TestPackageName, pkg.PackageName);
        }

        [Test]
        public void GetPackage_WithNullOrEmpty_ReturnsDefaultPackage()
        {
            SkipIfNotInitialized();

            var pkgNull = _yooSystem.GetPackage(null);
            var pkgEmpty = _yooSystem.GetPackage("");
            var defaultPkg = _yooSystem.DefaultPackage;

            Assert.AreEqual(defaultPkg, pkgNull, "Null should return default package");
            Assert.AreEqual(defaultPkg, pkgEmpty, "Empty string should return default package");
        }

        [Test]
        public void IsPackageInitialized_AllPackages_ReturnsTrue()
        {
            SkipIfNotInitialized();

            Assert.IsTrue(_yooSystem.IsPackageInitialized(DefaultPackageName),
                $"{DefaultPackageName} should be initialized");
            Assert.IsTrue(_yooSystem.IsPackageInitialized(TestPackageName),
                $"{TestPackageName} should be initialized");
        }

        [Test]
        public void DefaultPackage_IsDefaultPackage()
        {
            SkipIfNotInitialized();

            var settings = YooAssetSettings.Instance;
            var defaultPkg = _yooSystem.DefaultPackage;

            Assert.IsNotNull(defaultPkg, "DefaultPackage should not be null");
            Assert.AreEqual(settings.DefaultPackageName, defaultPkg.PackageName);
        }

        #endregion

        #region Cross-Package Asset Tests

        [Test]
        public void CheckAssetExists_InDefaultPackage_ReturnsTrue()
        {
            SkipIfNotInitialized();

            var exists = _yooSystem.CheckAssetExists(TestPrefabName, DefaultPackageName);
            Assert.IsTrue(exists, $"'{TestPrefabName}' should exist in {DefaultPackageName}");
        }

        [Test]
        public void CheckAssetExists_InTestPackage_ReturnsTrue()
        {
            SkipIfNotInitialized();

            var exists = _yooSystem.CheckAssetExists(TestPrefabName, TestPackageName);
            Assert.IsTrue(exists, $"'{TestPrefabName}' should exist in {TestPackageName}");
        }

        [UnityTest]
        public IEnumerator LoadAsync_FromDefaultPackage_Works()
        {
            SkipIfNotInitialized();

            GameObject result = null;
            var task = _yooSystem.LoadAsync<GameObject>(TestPrefabName, DefaultPackageName);
            yield return task.ToCoroutine(r => result = r);

            Assert.IsNotNull(result, $"Should load from {DefaultPackageName}");
        }

        [UnityTest]
        public IEnumerator LoadAsync_FromTestPackage_Works()
        {
            SkipIfNotInitialized();

            GameObject result = null;
            var task = _yooSystem.LoadAsync<GameObject>(TestPrefabName, TestPackageName);
            yield return task.ToCoroutine(r => result = r);

            Assert.IsNotNull(result, $"Should load from {TestPackageName}");
        }

        #endregion

        #region Unload Tests

        [UnityTest]
        public IEnumerator UnloadUnusedAssetsAsync_EachPackage_Works()
        {
            SkipIfNotInitialized();

            // 先加载资源
            var task = _yooSystem.LoadAsync<GameObject>(TestPrefabName);
            yield return task.ToCoroutine();

            // 卸载未使用资源
            var unloadTask = _yooSystem.UnloadUnusedAssetsAsync(DefaultPackageName);
            yield return unloadTask.ToCoroutine();

            // 不抛出异常即为成功
        }

        [UnityTest]
        public IEnumerator UnloadAllPackagesUnusedAssetsAsync_Works()
        {
            SkipIfNotInitialized();

            var task = _yooSystem.UnloadAllPackagesUnusedAssetsAsync();
            yield return task.ToCoroutine();

            // 不抛出异常即为成功
        }

        #endregion

        #region Cache Tests

        [UnityTest]
        public IEnumerator ClearUnusedCacheAsync_EachPackage_Works()
        {
            SkipIfNotInitialized();

            var task1 = _yooSystem.ClearUnusedCacheAsync(DefaultPackageName);
            yield return task1.ToCoroutine();

            var task2 = _yooSystem.ClearUnusedCacheAsync(TestPackageName);
            yield return task2.ToCoroutine();

            // 不抛出异常即为成功
        }

        #endregion
    }
}
#endif
