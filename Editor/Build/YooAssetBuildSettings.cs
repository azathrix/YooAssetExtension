#if YOOASSET_INSTALLED
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;

namespace Azathrix.YooSystem.Editor.Editor.Build
{
    /// <summary>
    /// YooAsset 构建设置
    /// </summary>
    [FilePath("ProjectSettings/YooAssetBuildSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class YooAssetBuildSettings : ScriptableSingleton<YooAssetBuildSettings>
    {
        public enum DllBuildMode
        {
            Auto,      // 自动检测缺失文件，智能编译
            ForceAll,  // 强制重新生成所有DLL（包括AOT）
            None       // 不编译DLL，直接使用现有文件
        }

        [Serializable]
        public class PackageSelectionEntry
        {
            public string packageName;
            public bool selected = true;
        }

        [Serializable]
        public class StepEnabledEntry
        {
            public string stepName;
            public bool enabled = true;
        }

        [SerializeField]
        private List<PackageSelectionEntry> _packageSelections = new();

        [SerializeField]
        private List<StepEnabledEntry> _stepEnabledStates = new();

        [SerializeField]
        public bool pipelineEnabled = true;

        // DLL编译模式
        [SerializeField]
        public DllBuildMode dllBuildMode = DllBuildMode.Auto;

        // 基础构建参数
        [SerializeField]
        public EBuildPipeline BuildPipeline = EBuildPipeline.ScriptableBuildPipeline;
        [SerializeField]
        public ECompressOption CompressOption = ECompressOption.LZ4;
        [SerializeField]
        public EFileNameStyle FileNameStyle = EFileNameStyle.HashName;

        // 输出目录
        [SerializeField]
        public string BuildOutputRoot = "Bundles";

        // 内置资源复制
        [SerializeField]
        public EBuildinFileCopyOption BuildinFileCopyOption = EBuildinFileCopyOption.None;
        [SerializeField]
        public string BuildinFileCopyParams = "";

        // 高级选项
        [SerializeField]
        public bool VerifyBuildingResult = true;
        [SerializeField]
        public bool EnableLog = true;
        [SerializeField]
        public bool DisableWriteTypeTree = false;
        [SerializeField]
        public bool IgnoreTypeTreeChanges = true;

        // SBP 参数
        [SerializeField]
        public bool SBPWriteLinkXML = true;
        [SerializeField]
        public string SBPCacheServerHost = "";
        [SerializeField]
        public int SBPCacheServerPort = 0;

        // 清除缓存选项
        [SerializeField]
        public bool ClearBuildCache = false;

        // 上传配置索引（每个pipeline独立）
        [SerializeField]
        public int UploadConfigIndex = 0;

        /// <summary>
        /// 获取完整的输出根目录
        /// </summary>
        public string GetBuildOutputRoot()
        {
            if (Path.IsPathRooted(BuildOutputRoot))
                return BuildOutputRoot;
            string projectPath = Application.dataPath.Replace("/Assets", "");
            return Path.Combine(projectPath, BuildOutputRoot);
        }

        /// <summary>
        /// 获取Package选择状态
        /// </summary>
        public bool GetPackageSelected(string packageName)
        {
            var entry = _packageSelections.Find(e => e.packageName == packageName);
            return entry?.selected ?? true; // 默认选中
        }

        /// <summary>
        /// 设置Package选择状态
        /// </summary>
        public void SetPackageSelected(string packageName, bool selected)
        {
            var entry = _packageSelections.Find(e => e.packageName == packageName);
            if (entry != null)
            {
                entry.selected = selected;
            }
            else
            {
                _packageSelections.Add(new PackageSelectionEntry
                {
                    packageName = packageName,
                    selected = selected
                });
            }
        }

        /// <summary>
        /// 清理不存在的Package选择记录
        /// </summary>
        public void CleanupSelections(IEnumerable<string> validPackageNames)
        {
            var validSet = new HashSet<string>(validPackageNames);
            _packageSelections.RemoveAll(e => !validSet.Contains(e.packageName));
        }

        /// <summary>
        /// 获取步骤启用状态
        /// </summary>
        public bool GetStepEnabled(string stepName)
        {
            var entry = _stepEnabledStates.Find(e => e.stepName == stepName);
            return entry?.enabled ?? true;
        }

        /// <summary>
        /// 设置步骤启用状态
        /// </summary>
        public void SetStepEnabled(string stepName, bool enabled)
        {
            var entry = _stepEnabledStates.Find(e => e.stepName == stepName);
            if (entry != null)
                entry.enabled = enabled;
            else
                _stepEnabledStates.Add(new StepEnabledEntry { stepName = stepName, enabled = enabled });
        }

        /// <summary>
        /// 保存设置
        /// </summary>
        public void Save()
        {
            Save(true);
        }
    }
}
#endif
