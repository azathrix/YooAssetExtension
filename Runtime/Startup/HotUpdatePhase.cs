#if YOOASSET_INSTALLED
using Azathrix.Framework.Core;
using Azathrix.Framework.Core.Startup;
using Azathrix.Framework.Core.Startup.Phases;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;

namespace Azathrix.YooAssetExtension
{
    /// <summary>
    /// 热更新阶段 - 在 RegisterPhase 之后执行
    /// </summary>
    public class HotUpdatePhase : IHotUpdatePhase
    {
        public string Id => "HotUpdate";
        public int Order => 250;

        public async UniTask ExecuteAsync(PhaseContext context)
        {
            // 编辑器模式下跳过热更新
            if (context.IsEditor) return;

            var settings = YooAssetSettings.Instance;
            if (!settings.autoInitOnStartup)
            {
                Log.Info("[YooAsset] Auto init disabled, skip hot update phase");
                return;
            }

            var hotUpdate = AzathrixFramework.GetSystem<IHotUpdateFlow>();
            if (hotUpdate == null)
            {
                Log.Warning("[YooAsset] IHotUpdateFlow not found, skip hot update phase");
                return;
            }

            Log.Info("[YooAsset] Starting hot update...");
            var success = await hotUpdate.RunFullUpdateAsync(settings.autoDownloadTags);
            if (!success)
            {
                Log.Error($"[YooAsset] Hot update failed: {hotUpdate.ErrorMessage}");
            }
        }
    }
}
#endif
