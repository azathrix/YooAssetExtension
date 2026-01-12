#if YOOASSET_INSTALLED
using Azathrix.Framework.Core.Startup;

namespace Azathrix.YooSystem.Startup
{
    /// <summary>
    /// 热更新阶段标记接口
    /// </summary>
    public interface IHotUpdatePhase : IStartupPhase { }
}
#endif
