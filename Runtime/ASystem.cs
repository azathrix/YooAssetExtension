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

namespace Azathrix.YooAssetExtension
{
    /// <summary>
    /// YooAsset资源系统实现
    /// </summary>
    [SystemPriority(-1000)]
    [RequireSystem(typeof(YooAssetResourceSystem))]
    public class ASystem : ISystem
    {
    }
}
#endif
