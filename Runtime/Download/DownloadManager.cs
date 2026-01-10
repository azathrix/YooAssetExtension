#if YOOASSET_INSTALLED
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace Azathrix.YooAssetExtension
{
    /// <summary>
    /// 下载管理器 - 提供更细粒度的下载控制
    /// </summary>
    public class DownloadManager
    {
        private readonly ResourcePackage _package;
        private readonly YooAssetSettings _settings;
        private readonly List<ResourceDownloaderOperation> _activeDownloaders = new();

        public event Action<string, DownloadProgress> OnTaskProgress;
        public event Action<string> OnTaskComplete;
        public event Action<string, string> OnTaskError;

        public DownloadManager(ResourcePackage package, YooAssetSettings settings)
        {
            _package = package;
            _settings = settings;
        }

        /// <summary>
        /// 按标签下载
        /// </summary>
        public async UniTask<bool> DownloadByTagsAsync(string taskId, params string[] tags)
        {
            var downloader = _package.CreateResourceDownloader(tags,
                _settings.downloadingMaxNum, _settings.failedTryAgain);
            return await ExecuteDownload(taskId, downloader);
        }

        /// <summary>
        /// 按路径下载
        /// </summary>
        public async UniTask<bool> DownloadByPathsAsync(string taskId, params string[] paths)
        {
            var downloader = _package.CreateBundleDownloader(paths,
                _settings.downloadingMaxNum, _settings.failedTryAgain);
            return await ExecuteDownload(taskId, downloader);
        }

        /// <summary>
        /// 获取下载大小（按标签）
        /// </summary>
        public long GetDownloadSizeByTags(params string[] tags)
        {
            var downloader = _package.CreateResourceDownloader(tags, 1, 1);
            return downloader.TotalDownloadBytes;
        }

        /// <summary>
        /// 获取下载大小（按路径）
        /// </summary>
        public long GetDownloadSizeByPaths(params string[] paths)
        {
            var downloader = _package.CreateBundleDownloader(paths, 1, 1);
            return downloader.TotalDownloadBytes;
        }

        /// <summary>
        /// 获取下载文件数量（按标签）
        /// </summary>
        public int GetDownloadCountByTags(params string[] tags)
        {
            var downloader = _package.CreateResourceDownloader(tags, 1, 1);
            return downloader.TotalDownloadCount;
        }

        /// <summary>
        /// 获取下载文件数量（按路径）
        /// </summary>
        public int GetDownloadCountByPaths(params string[] paths)
        {
            var downloader = _package.CreateBundleDownloader(paths, 1, 1);
            return downloader.TotalDownloadCount;
        }

        private async UniTask<bool> ExecuteDownload(string taskId, ResourceDownloaderOperation downloader)
        {
            if (downloader.TotalDownloadCount == 0)
            {
                OnTaskComplete?.Invoke(taskId);
                return true;
            }

            _activeDownloaders.Add(downloader);

            downloader.OnDownloadProgressCallback = (total, current, totalBytes, currentBytes) =>
            {
                OnTaskProgress?.Invoke(taskId, new DownloadProgress
                {
                    TotalCount = total,
                    CurrentCount = current,
                    TotalBytes = totalBytes,
                    CurrentBytes = currentBytes
                });
            };

            downloader.OnDownloadErrorCallback = (fileName, error) =>
            {
                OnTaskError?.Invoke(taskId, $"{fileName}: {error}");
            };

            downloader.BeginDownload();
            await downloader.ToUniTask();

            _activeDownloaders.Remove(downloader);

            if (downloader.Status == EOperationStatus.Succeed)
            {
                OnTaskComplete?.Invoke(taskId);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 暂停所有下载
        /// </summary>
        public void PauseAll()
        {
            foreach (var d in _activeDownloaders)
                d.PauseDownload();
        }

        /// <summary>
        /// 恢复所有下载
        /// </summary>
        public void ResumeAll()
        {
            foreach (var d in _activeDownloaders)
                d.ResumeDownload();
        }

        /// <summary>
        /// 取消所有下载
        /// </summary>
        public void CancelAll()
        {
            foreach (var d in _activeDownloaders)
                d.CancelDownload();
            _activeDownloaders.Clear();
        }

        /// <summary>
        /// 是否有活跃的下载任务
        /// </summary>
        public bool HasActiveDownloads => _activeDownloaders.Count > 0;
    }
}
#endif
