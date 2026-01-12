#if YOOASSET_INSTALLED
using System.Collections.Generic;
using System.IO;
using Editor.Attributes;
using Editor.Interfaces;
using UnityEditor;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;
using PackFlowContext = Editor.Core.BuildContext;
using PackFlowPipeline = Editor.Interfaces.IBuildPipeline;
using YooBuildResult = YooAsset.Editor.BuildResult;

namespace Azathrix.YooSystem.Editor.Editor.Build.Steps
{
    /// <summary>
    /// YooAsset 打包步骤
    /// </summary>
    [PipelineStep("YooAsset")]
    public class YooAssetPackStep : IBuildStep
    {
        private readonly YooAssetBuildPipeline _pipeline;
        private bool _showAdvanced;
        private bool _enabled = true;

        public string Name => "YooAsset 打包";
        public int Order => 100;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    Settings.SetStepEnabled(Name, value);
                    Settings.Save();
                }
            }
        }
        public bool HasConfigGUI => true;

        private YooAssetBuildSettings Settings => YooAssetBuildSettings.instance;

        public YooAssetPackStep(PackFlowPipeline pipeline)
        {
            _pipeline = (YooAssetBuildPipeline)pipeline;
            _enabled = Settings.GetStepEnabled(Name);
        }

        public void DrawConfigGUI()
        {
            EditorGUI.BeginChangeCheck();

            // 基础参数
            Settings.BuildPipeline = (EBuildPipeline)EditorGUILayout.EnumPopup("构建管线", Settings.BuildPipeline);
            Settings.CompressOption = (ECompressOption)EditorGUILayout.EnumPopup("压缩选项", Settings.CompressOption);
            Settings.FileNameStyle = (EFileNameStyle)EditorGUILayout.EnumPopup("文件命名", Settings.FileNameStyle);

            // 输出目录
            EditorGUILayout.BeginHorizontal();
            Settings.BuildOutputRoot = EditorGUILayout.TextField("输出目录", Settings.BuildOutputRoot);
            if (GUILayout.Button("...", GUILayout.Width(25)))
            {
                var path = EditorUtility.OpenFolderPanel("选择输出目录", Settings.GetBuildOutputRoot(), "");
                if (!string.IsNullOrEmpty(path))
                    Settings.BuildOutputRoot = path;
            }
            EditorGUILayout.EndHorizontal();

            // 内置资源复制
            Settings.BuildinFileCopyOption = (EBuildinFileCopyOption)EditorGUILayout.EnumPopup("内置资源复制", Settings.BuildinFileCopyOption);
            if (Settings.BuildinFileCopyOption != EBuildinFileCopyOption.None)
                Settings.BuildinFileCopyParams = EditorGUILayout.TextField("复制参数", Settings.BuildinFileCopyParams);

            // 高级选项
            _showAdvanced = EditorGUILayout.Foldout(_showAdvanced, "高级选项");
            if (_showAdvanced)
            {
                EditorGUI.indentLevel++;
                Settings.VerifyBuildingResult = EditorGUILayout.Toggle("验证构建结果", Settings.VerifyBuildingResult);
                Settings.EnableLog = EditorGUILayout.Toggle("启用日志", Settings.EnableLog);
                Settings.DisableWriteTypeTree = EditorGUILayout.Toggle("禁用类型树", Settings.DisableWriteTypeTree);
                Settings.IgnoreTypeTreeChanges = EditorGUILayout.Toggle("忽略类型树变化", Settings.IgnoreTypeTreeChanges);

                if (Settings.BuildPipeline == EBuildPipeline.ScriptableBuildPipeline)
                {
                    EditorGUILayout.Space(3);
                    EditorGUILayout.LabelField("SBP 参数", EditorStyles.miniLabel);
                    Settings.SBPWriteLinkXML = EditorGUILayout.Toggle("生成 link.xml", Settings.SBPWriteLinkXML);
                    Settings.SBPCacheServerHost = EditorGUILayout.TextField("缓存服务器", Settings.SBPCacheServerHost);
                    if (!string.IsNullOrEmpty(Settings.SBPCacheServerHost))
                        Settings.SBPCacheServerPort = EditorGUILayout.IntField("缓存端口", Settings.SBPCacheServerPort);
                }
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
                Settings.Save();
        }

        public bool Execute(PackFlowContext context)
        {
            var selectedPackages = _pipeline.GetSelectedPackages();
            if (selectedPackages.Count == 0)
            {
                context.LogError("未选择任何资源包");
                return false;
            }

            var settings = YooAssetBuildSettings.instance;

            // 清除构建缓存
            if (settings.ClearBuildCache)
            {
                context.Log("清除构建缓存...");
                ClearBuildCache();
                settings.ClearBuildCache = false; // 只清除一次
                settings.Save();
            }

            string outputRoot = settings.GetBuildOutputRoot();
            string streamingRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            string packageVersion = GetPackageVersion();

            context.Log($"开始构建 {selectedPackages.Count} 个资源包");
            context.Log($"输出目录: {outputRoot}");
            context.Log($"版本号: {packageVersion}");

            // 保存所有打包的目录列表
            var outputDirs = new List<string>();

            foreach (var packageName in selectedPackages)
            {
                context.Log($"--- 打包 {packageName} ---");

                try
                {
                    var result = BuildPackage(packageName, settings, context.BuildTarget, outputRoot, streamingRoot, packageVersion);
                    if (!result.Success)
                    {
                        context.LogError($"{packageName} 打包失败: {result.ErrorInfo}");
                        return false;
                    }

                    context.Log($"{packageName} 打包成功: {result.OutputPackageDirectory}");

                    // 保存 Package 目录（不是版本目录）到上下文
                    var packageDir = Path.GetDirectoryName(result.OutputPackageDirectory);
                    context.SetData($"YooAsset_{packageName}_OutputDir", packageDir);
                    outputDirs.Add(packageDir);
                }
                catch (System.Exception e)
                {
                    context.LogError($"{packageName} 打包异常: {e.Message}");
                    return false;
                }
            }

            // 保存所有输出目录列表
            context.SetData("YooAssetOutputDirs", outputDirs);
            context.OutputRoot = outputRoot;
            context.Log("所有资源包构建完成");
            return true;
        }

        private YooBuildResult BuildPackage(string packageName, YooAssetBuildSettings settings,
            BuildTarget buildTarget, string outputRoot, string streamingRoot, string packageVersion)
        {
            // 根据选择的管线类型创建对应的参数和管线
            if (settings.BuildPipeline == EBuildPipeline.ScriptableBuildPipeline)
            {
                var buildParameters = new ScriptableBuildParameters
                {
                    BuildOutputRoot = outputRoot,
                    BuildinFileRoot = streamingRoot,
                    BuildPipeline = EBuildPipeline.ScriptableBuildPipeline.ToString(),
                    BuildBundleType = (int)EBuildBundleType.AssetBundle,
                    BuildTarget = buildTarget,
                    PackageName = packageName,
                    PackageVersion = packageVersion,
                    EnableSharePackRule = true,
                    VerifyBuildingResult = settings.VerifyBuildingResult,
                    FileNameStyle = settings.FileNameStyle,
                    BuildinFileCopyOption = settings.BuildinFileCopyOption,
                    BuildinFileCopyParams = settings.BuildinFileCopyParams,
                    CompressOption = settings.CompressOption,
                    ClearBuildCacheFiles = settings.ClearBuildCache,
                    UseAssetDependencyDB = true,
                    DisableWriteTypeTree = settings.DisableWriteTypeTree,
                    IgnoreTypeTreeChanges = settings.IgnoreTypeTreeChanges,
                    WriteLinkXML = settings.SBPWriteLinkXML,
                    CacheServerHost = settings.SBPCacheServerHost,
                    CacheServerPort = settings.SBPCacheServerPort,
                };

                var pipeline = new ScriptableBuildPipeline();
                return pipeline.Run(buildParameters, settings.EnableLog);
            }
            else if (settings.BuildPipeline == EBuildPipeline.BuiltinBuildPipeline)
            {
                var buildParameters = new BuiltinBuildParameters
                {
                    BuildOutputRoot = outputRoot,
                    BuildinFileRoot = streamingRoot,
                    BuildPipeline = EBuildPipeline.BuiltinBuildPipeline.ToString(),
                    BuildBundleType = (int)EBuildBundleType.AssetBundle,
                    BuildTarget = buildTarget,
                    PackageName = packageName,
                    PackageVersion = packageVersion,
                    EnableSharePackRule = true,
                    VerifyBuildingResult = settings.VerifyBuildingResult,
                    FileNameStyle = settings.FileNameStyle,
                    BuildinFileCopyOption = settings.BuildinFileCopyOption,
                    BuildinFileCopyParams = settings.BuildinFileCopyParams,
                    CompressOption = settings.CompressOption,
                    ClearBuildCacheFiles = settings.ClearBuildCache,
                    UseAssetDependencyDB = true,
                    DisableWriteTypeTree = settings.DisableWriteTypeTree,
                    IgnoreTypeTreeChanges = settings.IgnoreTypeTreeChanges,
                };

                var pipeline = new BuiltinBuildPipeline();
                return pipeline.Run(buildParameters, settings.EnableLog);
            }
            else if (settings.BuildPipeline == EBuildPipeline.RawFileBuildPipeline)
            {
                var buildParameters = new RawFileBuildParameters
                {
                    BuildOutputRoot = outputRoot,
                    BuildinFileRoot = streamingRoot,
                    BuildPipeline = EBuildPipeline.RawFileBuildPipeline.ToString(),
                    BuildBundleType = (int)EBuildBundleType.RawBundle,
                    BuildTarget = buildTarget,
                    PackageName = packageName,
                    PackageVersion = packageVersion,
                    VerifyBuildingResult = settings.VerifyBuildingResult,
                    FileNameStyle = settings.FileNameStyle,
                    BuildinFileCopyOption = settings.BuildinFileCopyOption,
                    BuildinFileCopyParams = settings.BuildinFileCopyParams,
                    ClearBuildCacheFiles = settings.ClearBuildCache,
                    UseAssetDependencyDB = true,
                };

                var pipeline = new RawFileBuildPipeline();
                return pipeline.Run(buildParameters, settings.EnableLog);
            }
            else
            {
                throw new System.NotImplementedException($"Unsupported build pipeline: {settings.BuildPipeline}");
            }
        }

        private string GetPackageVersion()
        {
            return System.DateTime.Now.ToString("yyyy-MM-dd-HHmmss");
        }

        private void ClearBuildCache()
        {
            // 清除 SBP 构建缓存
            var buildCachePath = Path.Combine(Application.dataPath, "..", "Library", "BuildCache");
            if (Directory.Exists(buildCachePath))
            {
                Directory.Delete(buildCachePath, true);
                Debug.Log($"[PackFlow] 已删除 BuildCache: {buildCachePath}");
            }

            // 清除 SBP 的 ScriptableBuildPipeline 缓存
            var sbpCachePath = Path.Combine(Application.dataPath, "..", "Library", "com.unity.scriptablebuildpipeline");
            if (Directory.Exists(sbpCachePath))
            {
                Directory.Delete(sbpCachePath, true);
                Debug.Log($"[PackFlow] 已删除 SBP 缓存: {sbpCachePath}");
            }
        }
    }
}
#endif
