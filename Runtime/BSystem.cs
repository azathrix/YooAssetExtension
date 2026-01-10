#if YOOASSET_INSTALLED
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Azathrix.Framework.Core.Attributes;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Interfaces.DefaultSystems;
using Azathrix.Framework.Interfaces.SystemEvents;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;
using YooAsset;
using Object = UnityEngine.Object;

namespace Azathrix.YooAssetExtension
{
    /// <summary>
    /// YooAsset资源系统实现
    /// </summary>
    [SystemPriority(-1000)]
    public class BSystem : IResourcesSystem
    {
        public UniTask<T> LoadAsync<T>(string key) where T : Object
        {
            throw new NotImplementedException();
        }

        public T Load<T>(string key) where T : Object
        {
            throw new NotImplementedException();
        }

        public UniTask<GameObject> InstantiateAsync(string key, Transform parent = null)
        {
            throw new NotImplementedException();
        }

        public UniTask LoadSceneAsync(string key, LoadSceneMode mode)
        {
            throw new NotImplementedException();
        }
    }
}
#endif
