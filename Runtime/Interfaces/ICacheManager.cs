using Cysharp.Threading.Tasks;

namespace Azathrix.YooSystem.Interfaces
{
    /// <summary>
    /// 缓存信息
    /// </summary>
    public struct CacheInfo
    {
        public long TotalSize;
        public int FileCount;
    }

    /// <summary>
    /// 缓存管理接口
    /// </summary>
    public interface ICacheManager
    {
        /// <summary>
        /// 获取缓存信息
        /// </summary>
        CacheInfo GetCacheInfo(string packageName = null);

        /// <summary>
        /// 清理未使用的缓存
        /// </summary>
        UniTask ClearUnusedCacheAsync(string packageName = null);

        /// <summary>
        /// 清理所有缓存
        /// </summary>
        UniTask ClearAllCacheAsync(string packageName = null);
    }
}
