using UnityEngine;
using Azathrix.Framework.Settings;

namespace Azathrix.YooAssetExtension
{
    /// <summary>
    /// 运行模式
    /// </summary>
    public enum EPlayMode
    {
        /// <summary>
        /// 编辑器模拟模式
        /// </summary>
        EditorSimulateMode,
        /// <summary>
        /// 离线模式
        /// </summary>
        OfflinePlayMode,
        /// <summary>
        /// 联机模式
        /// </summary>
        HostPlayMode,
        /// <summary>
        /// WebGL模式
        /// </summary>
        WebPlayMode
    }

    /// <summary>
    /// YooAsset系统配置
    /// </summary>
    [SettingsName("YooAsset配置")]
    public class YooAssetSettings : SettingsBase<YooAssetSettings>
    {
        [Header("运行模式")]
        [Tooltip("编辑器模拟模式：使用AssetDatabase加载\n离线模式：使用本地资源包\n联机模式：支持热更新")]
        public EPlayMode playMode = EPlayMode.EditorSimulateMode;

        [Header("包配置")]
        [Tooltip("默认资源包名称")]
        public string defaultPackageName = "DefaultPackage";

        [Header("远程配置")]
        [Tooltip("资源服务器地址")]
        public string hostServerURL = "http://127.0.0.1";

        [Tooltip("备用资源服务器地址")]
        public string fallbackHostServerURL = "";

        [Header("下载配置")]
        [Tooltip("同时下载的最大文件数")]
        public int downloadingMaxNum = 10;

        [Tooltip("失败重试最大次数")]
        public int failedTryAgain = 3;

        [Header("启动配置")]
        [Tooltip("是否在框架启动时自动初始化YooAsset")]
        public bool autoInitOnStartup = true;

        [Tooltip("自动下载的资源标签（留空则不自动下载）")]
        public string[] autoDownloadTags = new string[0];
    }
}
