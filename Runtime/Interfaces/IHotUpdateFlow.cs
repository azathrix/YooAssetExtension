#if YOOASSET_INSTALLED
using Azathrix.Framework.Interfaces;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace Azathrix.YooAssetExtension
{
    /// <summary>
    /// 热更新流程状态
    /// </summary>
    public enum HotUpdateState
    {
        None,
        InitPackage,
        UpdateVersion,
        UpdateManifest,
        CreateDownloader,
        Downloading,
        Done,
        Failed
    }

    /// <summary>
    /// 热更新流程接口
    /// </summary>
    public interface IHotUpdateFlow : ISystem
    {
        /// <summary>
        /// 当前状态
        /// </summary>
        HotUpdateState State { get; }

        /// <summary>
        /// 错误信息
        /// </summary>
        string ErrorMessage { get; }

        /// <summary>
        /// 初始化资源包
        /// </summary>
        UniTask<bool> InitPackageAsync(string packageName = null);

        /// <summary>
        /// 更新版本
        /// </summary>
        UniTask<bool> UpdateVersionAsync();

        /// <summary>
        /// 更新清单
        /// </summary>
        UniTask<bool> UpdateManifestAsync();

        /// <summary>
        /// 创建下载器（按标签）
        /// </summary>
        UniTask<ResourceDownloaderOperation> CreateDownloaderByTagsAsync(params string[] tags);

        /// <summary>
        /// 创建下载器（按路径）
        /// </summary>
        UniTask<ResourceDownloaderOperation> CreateDownloaderByPathsAsync(params string[] paths);

        /// <summary>
        /// 执行完整热更新流程
        /// </summary>
        UniTask<bool> RunFullUpdateAsync(string[] downloadTags = null);
    }
}
#endif
