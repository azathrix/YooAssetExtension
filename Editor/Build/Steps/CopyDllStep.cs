#if YOOASSET_INSTALLED
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Azathrix.PackFlow.Editor.Attributes;
using Azathrix.PackFlow.Editor.Core;
using Azathrix.PackFlow.Editor.Interfaces;
using UnityEditor;
using UnityEngine;
using Azathrix.YooSystem.Editor.Editor.Build;

namespace Azathrix.YooAssetExtension.Editor
{
    /// <summary>
    /// DLL 复制步骤 - 将编译好的DLL复制到项目中
    /// </summary>
    [PipelineStep("YooAsset")]
    public class CopyDllStep : IBuildStep
    {
        private readonly YooAssetBuildPipeline _pipeline;
        private bool _enabled = true;

        public string Name => "复制 DLL";
        public int Order => 20;
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    YooAssetBuildSettings.instance.SetStepEnabled(Name, value);
                    YooAssetBuildSettings.instance.Save();
                }
            }
        }
        public bool HasConfigGUI => false;

        public CopyDllStep(IBuildPipeline pipeline)
        {
            _pipeline = (YooAssetBuildPipeline)pipeline;
            _enabled = YooAssetBuildSettings.instance.GetStepEnabled(Name);
        }

        public void DrawConfigGUI()
        {
            // 无配置
        }

        public bool Execute(PackFlowBuildContext context)
        {
            // 检查是否有选中的热更包
            if (!_pipeline.HasHotUpdatePackageSelected())
            {
                context.Log("未选中热更包，跳过 DLL 复制");
                return true;
            }

            var settings = YooAssetBuildSettings.instance;
            if (settings.dllBuildMode == YooAssetBuildSettings.DllBuildMode.None)
            {
                context.Log("DLL编译模式为None，跳过复制");
                return true;
            }

            try
            {
                // 获取热更DLL列表
                var hotUpdateDlls = GetHotUpdateDllList();
                if (hotUpdateDlls == null || hotUpdateDlls.Count == 0)
                {
                    context.Log("未找到热更DLL配置，跳过复制");
                    return true;
                }

                string platform = context.BuildTarget.ToString();

                // 复制热更DLL
                CopyDlls(hotUpdateDlls, $"HybridCLRData/HotUpdateDlls/{platform}", context);

                // 复制AOT DLL
                var aotList = GetAOTAssemblyList();
                if (aotList != null && aotList.Count > 0)
                {
                    CopyDlls(aotList, $"HybridCLRData/AssembliesPostIl2CppStrip/{platform}", context);
                }

                AssetDatabase.Refresh();
                context.Log("DLL 复制完成");
                return true;
            }
            catch (Exception e)
            {
                context.LogError($"DLL 复制失败: {e.Message}");
                return false;
            }
        }

        private List<string> GetHotUpdateDllList()
        {
            // 尝试从 RuntimeConfig 获取热更DLL列表
            // 按优先级查找: Channel > GameCore > Default
            var configPaths = new[]
            {
                "Assets/GameChannel/Runtime/Resources/RuntimeConfigChannel.asset",
                "Assets/GameCore/Runtime/Resources/RuntimeConfigGameCore.asset",
                "Assets/Resources/RuntimeConfig.asset"
            };

            foreach (var path in configPaths)
            {
                var config = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (config != null)
                {
                    var field = config.GetType().GetField("hotUpdateDlls");
                    if (field != null)
                    {
                        return field.GetValue(config) as List<string>;
                    }
                }
            }

            // 默认列表
            return new List<string>
            {
                "Framework.dll",
                "Game.dll"
            };
        }

        private List<string> GetAOTAssemblyList()
        {
            var aotType = Type.GetType("AOTGenericReferences");
            if (aotType == null)
            {
                Debug.LogWarning("[CopyDll] AOTGenericReferences type not found");
                return null;
            }

            var listField = aotType.GetField("PatchedAOTAssemblyList", BindingFlags.Public | BindingFlags.Static);
            if (listField == null)
            {
                Debug.LogWarning("[CopyDll] PatchedAOTAssemblyList field not found");
                return null;
            }

            return listField.GetValue(null) as List<string>;
        }

        private void CopyDlls(List<string> files, string sourceFolder, PackFlowBuildContext context)
        {
            string projectPath = Application.dataPath.Replace("/Assets", "");
            string sourcePath = Path.Combine(projectPath, sourceFolder);
            string targetPath = Path.Combine(Application.dataPath, "AssemblyHotUpdate");

            // 容错：源目录不存在则跳过
            if (!Directory.Exists(sourcePath))
            {
                context.Log($"源目录不存在，跳过: {sourcePath}");
                return;
            }

            // 确保目标目录存在
            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);

            foreach (var file in files)
            {
                try
                {
                    var source = Path.Combine(sourcePath, file);
                    if (!File.Exists(source))
                    {
                        context.Log($"源文件不存在，跳过: {file}");
                        continue;
                    }

                    var target = Path.Combine(targetPath, file + ".bytes");
                    if (File.Exists(target))
                        File.Delete(target);

                    File.Copy(source, target);
                    context.Log($"复制DLL: {file}");
                }
                catch (Exception e)
                {
                    context.LogError($"复制 {file} 失败: {e.Message}");
                }
            }
        }
    }
}
#endif
