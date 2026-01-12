#if YOOASSET_INSTALLED
using System.Collections.Generic;
using System.Linq;
using Editor.Core;
using Editor.UI;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;
using PackFlowContext = Editor.Core.BuildContext;

namespace Azathrix.YooSystem.Editor.Editor.Build
{
    /// <summary>
    /// YooAsset 构建管线
    /// </summary>
    public class YooAssetBuildPipeline : BuildPipelineBase
    {
        public override string Name => "YooAsset";
        public override string Description => "使用 YooAsset 打包资源";

        private YooAssetBuildSettings Settings => YooAssetBuildSettings.instance;

        // 重写 Enabled 属性，使用持久化存储
        public new bool Enabled
        {
            get => Settings.pipelineEnabled;
            set
            {
                if (Settings.pipelineEnabled != value)
                {
                    Settings.pipelineEnabled = value;
                    Settings.Save();
                }
            }
        }

        // 缓存热更包判断结果
        private readonly Dictionary<string, bool> _hotUpdatePackageCache = new();

        // 构造函数 - Steps 通过反射自动加载

        /// <summary>
        /// 获取所有可用的Package列表
        /// </summary>
        public IReadOnlyList<string> GetAvailablePackages()
        {
            var packages = new List<string>();
            var setting = AssetBundleCollectorSettingData.Setting;
            if (setting?.Packages != null)
            {
                foreach (var package in setting.Packages)
                    packages.Add(package.PackageName);
            }
            return packages;
        }

        /// <summary>
        /// 判断Package是否是热更包（包含.dll.bytes文件）
        /// </summary>
        public bool IsHotUpdatePackage(string packageName)
        {
            if (_hotUpdatePackageCache.TryGetValue(packageName, out var cached))
                return cached;

            var setting = AssetBundleCollectorSettingData.Setting;
            var package = setting?.Packages?.FirstOrDefault(p => p.PackageName == packageName);
            if (package == null)
            {
                _hotUpdatePackageCache[packageName] = false;
                return false;
            }

            foreach (var group in package.Groups)
            {
                foreach (var collector in group.Collectors)
                {
                    // 检查收集路径是否包含 AssemblyHotUpdate 目录或 .dll.bytes 文件
                    if (collector.CollectPath.Contains("AssemblyHotUpdate") ||
                        collector.CollectPath.EndsWith(".dll.bytes"))
                    {
                        _hotUpdatePackageCache[packageName] = true;
                        return true;
                    }
                }
            }

            _hotUpdatePackageCache[packageName] = false;
            return false;
        }

        /// <summary>
        /// 检查是否有选中的热更包
        /// </summary>
        public bool HasHotUpdatePackageSelected()
        {
            foreach (var pkg in GetAvailablePackages())
            {
                if (Settings.GetPackageSelected(pkg) && IsHotUpdatePackage(pkg))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 获取选中的Package列表
        /// </summary>
        public List<string> GetSelectedPackages()
        {
            var selected = new List<string>();
            foreach (var pkg in GetAvailablePackages())
            {
                if (Settings.GetPackageSelected(pkg))
                    selected.Add(pkg);
            }
            return selected;
        }

        /// <summary>
        /// 清除热更包缓存
        /// </summary>
        public void ClearHotUpdateCache()
        {
            _hotUpdatePackageCache.Clear();
        }

        /// <summary>
        /// 获取需要上传的目录列表
        /// </summary>
        public override IReadOnlyList<string> GetUploadDirectories(PackFlowContext context)
        {
            var dirs = new List<string>();
            var outputRoot = Settings.GetBuildOutputRoot();
            var platformDir = System.IO.Path.Combine(outputRoot, context.BuildTarget.ToString());

            foreach (var pkg in GetSelectedPackages())
            {
                var pkgDir = System.IO.Path.Combine(platformDir, pkg);
                if (System.IO.Directory.Exists(pkgDir))
                    dirs.Add(pkgDir);
            }
            return dirs;
        }

        /// <summary>
        /// 获取此管线使用的上传配置
        /// </summary>
        public override UploadConfig GetUploadConfig()
        {
            var registry = UploadConfigRegistry.instance;
            if (Settings.UploadConfigIndex >= 0 && Settings.UploadConfigIndex < registry.Configs.Count)
                return registry.Configs[Settings.UploadConfigIndex].config;
            return registry.Current;
        }

        /// <summary>
        /// 绘制上传配置选择UI
        /// </summary>
        public override void DrawUploadConfigGUI()
        {
            var registry = UploadConfigRegistry.instance;
            var names = registry.GetConfigNames();

            EditorGUILayout.BeginHorizontal();
            var newIndex = EditorGUILayout.Popup("服务器配置", Settings.UploadConfigIndex, names);
            if (newIndex != Settings.UploadConfigIndex)
            {
                Settings.UploadConfigIndex = newIndex;
                Settings.Save();
            }

            if (GUILayout.Button("管理", GUILayout.Width(45)))
                UploadConfigEditorWindow.ShowWindow();
            EditorGUILayout.EndHorizontal();

            var config = GetUploadConfig();
            if (config != null)
                EditorGUILayout.LabelField($"  类型: {config.apiType}  地址: {config.endpoint}", EditorStyles.miniLabel);
        }

        public override void DrawConfigGUI()
        {
            var packages = GetAvailablePackages();

            // 资源包选择
            EditorGUILayout.LabelField("资源包选项", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            if (packages.Count == 0)
            {
                EditorGUILayout.HelpBox("未找到 YooAsset Package 配置", MessageType.Warning);
            }
            else
            {
                foreach (var pkg in packages)
                {
                    var isHotUpdate = IsHotUpdatePackage(pkg);
                    var displayName = isHotUpdate ? $"{pkg} (程序集)" : pkg;

                    var selected = Settings.GetPackageSelected(pkg);
                    var newSelected = EditorGUILayout.ToggleLeft(displayName, selected);
                    if (newSelected != selected)
                    {
                        Settings.SetPackageSelected(pkg, newSelected);
                        Settings.Save();
                    }
                }
            }
            EditorGUILayout.EndVertical();

            // 构建选项
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("构建选项", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            var clearCache = EditorGUILayout.ToggleLeft("清除构建缓存 (解决 SBP 构建错误)", Settings.ClearBuildCache);
            if (clearCache != Settings.ClearBuildCache)
            {
                Settings.ClearBuildCache = clearCache;
                Settings.Save();
            }

            EditorGUILayout.EndVertical();
        }
    }

    /// <summary>
    /// 自动注册管线
    /// </summary>
    [InitializeOnLoad]
    public static class YooAssetPipelineRegistrar
    {
        static YooAssetPipelineRegistrar()
        {
            EditorApplication.delayCall += () =>
            {
                PipelineRegistry.Register(new YooAssetBuildPipeline());
            };
        }
    }
}
#endif
