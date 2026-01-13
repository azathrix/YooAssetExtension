#if YOOASSET_INSTALLED
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using Azathrix.Framework.Core;
using UnityEngine.TestTools;

namespace Azathrix.YooSystem.Tests.Runtime
{
    /// <summary>
    /// YooSystem 测试基类，等待框架启动完成后从框架获取系统实例
    /// </summary>
    public abstract class YooSystemTestBase
    {
        protected YooSystem _yooSystem;
        protected bool _initialized;

        [UnitySetUp]
        public IEnumerator BaseSetUp()
        {
            if (_initialized) yield break;

            // 等待框架启动完成（最多等待 30 秒）
            float timeout = 30f;
            float elapsed = 0f;
            while (!AzathrixFramework.IsStarted && elapsed < timeout)
            {
                yield return null;
                elapsed += Time.deltaTime;
            }

            if (!AzathrixFramework.IsStarted)
            {
                Assert.Ignore("Framework not started within timeout");
                yield break;
            }

            // 从框架获取 YooSystem
            _yooSystem = AzathrixFramework.GetSystem<YooSystem>();
            if (_yooSystem == null)
            {
                Assert.Ignore("YooSystem not registered in framework");
                yield break;
            }

            // 检查默认包是否已初始化
            if (_yooSystem.DefaultPackage == null)
            {
                Assert.Ignore("YooSystem not initialized (no default package)");
                yield break;
            }

            _initialized = true;
        }

        /// <summary>
        /// 跳过未初始化的测试
        /// </summary>
        protected void SkipIfNotInitialized()
        {
            if (!_initialized)
                Assert.Ignore("Skipped: YooSystem not initialized");
        }
    }
}
#endif
