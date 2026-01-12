using System;

namespace Azathrix.YooSystem.Interfaces
{
    /// <summary>
    /// 下载进度数据
    /// </summary>
    public struct DownloadProgress
    {
        public int TotalCount;
        public int CurrentCount;
        public long TotalBytes;
        public long CurrentBytes;
        public float Progress => TotalBytes > 0 ? (float)CurrentBytes / TotalBytes : 0;
    }

    /// <summary>
    /// 下载监控接口
    /// </summary>
    public interface IDownloadMonitor
    {
        /// <summary>
        /// 下载进度变化事件
        /// </summary>
        event Action<DownloadProgress> OnProgressChanged;

        /// <summary>
        /// 下载错误事件
        /// </summary>
        event Action<string> OnDownloadError;

        /// <summary>
        /// 下载完成事件
        /// </summary>
        event Action OnDownloadComplete;

        /// <summary>
        /// 是否正在下载
        /// </summary>
        bool IsDownloading { get; }

        /// <summary>
        /// 当前下载进度
        /// </summary>
        DownloadProgress CurrentProgress { get; }

        /// <summary>
        /// 开始监控
        /// </summary>
        void StartMonitor();

        /// <summary>
        /// 停止监控
        /// </summary>
        void StopMonitor();
    }
}
