using System;
using Azathrix.Framework.Settings;
using UnityEngine;

namespace Azathrix.YooSystem
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
    /// 资源包配置
    /// </summary>
    [Serializable]
    public class PackageConfig
    {
        [Tooltip("资源包名称")] public string packageName = "DefaultPackage";

        [Tooltip("资源搜索优先级（数值越小优先级越高）")]
        public int priority = 0;

        [Tooltip("资源服务器地址（留空则使用 Profile 的全局地址）")]
        public string hostServerURL;

        [Tooltip("备用资源服务器地址")] public string fallbackHostServerURL;

        [Tooltip("自动下载的资源标签")] public string[] autoDownloadTags = new string[0];
    }

    /// <summary>
    /// Profile 配置（类似 Addressable 的 Profile）
    /// </summary>
    [Serializable]
    public class ProfileConfig
    {
        [Tooltip("Profile 名称")] public string profileName = "Default";

        [Header("运行模式")] [Tooltip("编辑器模拟模式：使用AssetDatabase加载\n离线模式：使用本地资源包\n联机模式：支持热更新")]
        public EPlayMode playMode = EPlayMode.EditorSimulateMode;

        [Header("远程配置")] [Tooltip("全局资源服务器地址")]
        public string hostServerURL = "http://127.0.0.1";

        [Tooltip("全局备用资源服务器地址")] public string fallbackHostServerURL = "";

        [Header("下载配置")] [Tooltip("同时下载的最大文件数")]
        public int downloadingMaxNum = 10;

        [Tooltip("失败重试最大次数")] public int failedTryAgain = 3;

        [Tooltip("加载时自动下载缺失资源")] public bool autoDownloadOnLoad = false;

        [Header("启动配置")] [Tooltip("是否在框架启动时自动初始化YooAsset")]
        public bool autoInitOnStartup = true;

        [Header("资源包配置")] [Tooltip("要初始化的资源包列表（按优先级排序搜索资源）")]
        public PackageConfig[] packages = new PackageConfig[1] { new PackageConfig() };
    }

    /// <summary>
    /// YooAsset系统配置
    /// </summary>
    [SettingsPath("YooAssetSettings")]
    [ShowSetting("YooSystem")]
    public class YooAssetSettings : SettingsBase<YooAssetSettings>
    {
        [Header("Profile 配置")] [Tooltip("配置组列表（类似 Addressable 的 Profile）")]
        public ProfileConfig[] profiles = new ProfileConfig[1] { new ProfileConfig() };

        [Tooltip("当前激活的 Profile 索引")] public int activeProfileIndex = 0;

        /// <summary>
        /// 获取当前激活的 Profile
        /// </summary>
        public ProfileConfig ActiveProfile =>
            profiles != null && activeProfileIndex >= 0 && activeProfileIndex < profiles.Length
                ? profiles[activeProfileIndex]
                : null;

        /// <summary>
        /// 获取当前运行模式
        /// </summary>
        public EPlayMode PlayMode => ActiveProfile?.playMode ?? EPlayMode.EditorSimulateMode;

        /// <summary>
        /// 获取全局资源服务器地址
        /// </summary>
        public string HostServerURL => ActiveProfile?.hostServerURL ?? "http://127.0.0.1";

        /// <summary>
        /// 获取全局备用服务器地址
        /// </summary>
        public string FallbackHostServerURL => ActiveProfile?.fallbackHostServerURL ?? "";

        /// <summary>
        /// 获取项目ID（从框架配置读取）
        /// </summary>
        public string ProjectId => AzathrixFrameworkSettings.Instance?.projectId ?? "";

        /// <summary>
        /// 获取游戏版本（从框架配置读取）
        /// </summary>
        public string GameVersion => AzathrixFrameworkSettings.Instance?.Version ?? "1.0.0";

        /// <summary>
        /// 获取当前平台名称
        /// </summary>
        public string PlatformName
        {
            get
            {
#if UNITY_ANDROID
                return "Android";
#elif UNITY_IOS
                return "iOS";
#elif UNITY_WEBGL
                return "WebGL";
#elif UNITY_STANDALONE_WIN
                return "StandaloneWindows64";
#elif UNITY_STANDALONE_OSX
                return "StandaloneOSX";
#elif UNITY_STANDALONE_LINUX
                return "StandaloneLinux64";
#else
                return Application.platform.ToString();
#endif
            }
        }

        /// <summary>
        /// 获取同时下载的最大文件数
        /// </summary>
        public int DownloadingMaxNum => ActiveProfile?.downloadingMaxNum ?? 10;

        /// <summary>
        /// 获取失败重试次数
        /// </summary>
        public int FailedTryAgain => ActiveProfile?.failedTryAgain ?? 3;

        /// <summary>
        /// 是否自动初始化
        /// </summary>
        public bool AutoInitOnStartup => ActiveProfile?.autoInitOnStartup ?? true;

        /// <summary>
        /// 加载时是否自动下载缺失资源
        /// </summary>
        public bool AutoDownloadOnLoad => ActiveProfile?.autoDownloadOnLoad ?? false;

        /// <summary>
        /// 获取资源包配置列表
        /// </summary>
        public PackageConfig[] Packages => ActiveProfile?.packages ?? new PackageConfig[0];

        /// <summary>
        /// 获取默认包名称（第一个包）
        /// </summary>
        public string DefaultPackageName =>
            Packages.Length > 0 ? Packages[0].packageName : "DefaultPackage";

        /// <summary>
        /// 获取指定包的服务器地址（如果包没有配置则使用全局地址）
        /// </summary>
        public string GetHostServerURL(PackageConfig package)
        {
            return string.IsNullOrEmpty(package?.hostServerURL) ? HostServerURL : package.hostServerURL;
        }

        /// <summary>
        /// 获取指定包的备用服务器地址
        /// </summary>
        public string GetFallbackHostServerURL(PackageConfig package)
        {
            return string.IsNullOrEmpty(package?.fallbackHostServerURL)
                ? FallbackHostServerURL
                : package.fallbackHostServerURL;
        }

        // 兼容旧 API（已废弃，建议使用新的 Profile 配置）
        [Obsolete("Use ActiveProfile.playMode instead")] public EPlayMode playMode => PlayMode;

        [Obsolete("Use DefaultPackageName instead")] public string defaultPackageName => DefaultPackageName;

        [Obsolete("Use HostServerURL instead")] public string hostServerURL => HostServerURL;

        [Obsolete("Use FallbackHostServerURL instead")]
        public string fallbackHostServerURL => FallbackHostServerURL;

        [Obsolete("Use DownloadingMaxNum instead")] public int downloadingMaxNum => DownloadingMaxNum;

        [Obsolete("Use FailedTryAgain instead")] public int failedTryAgain => FailedTryAgain;

        [Obsolete("Use AutoInitOnStartup instead")] public bool autoInitOnStartup => AutoInitOnStartup;

        [Obsolete("Use Packages[0].autoDownloadTags instead")]
        public string[] autoDownloadTags => Packages.Length > 0 ? Packages[0].autoDownloadTags : new string[0];
    }
}
