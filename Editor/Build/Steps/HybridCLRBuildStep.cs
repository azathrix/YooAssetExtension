// #if YOOASSET_INSTALLED
// using System.IO;
// using UnityEditor;
// using UnityEngine;
// using Azathrix.PackFlow;
// using PackFlowContext = Azathrix.PackFlow.BuildContext;
//
// namespace Azathrix.YooAssetExtension.Editor
// {
//     /// <summary>
//     /// HybridCLR 编译步骤
//     /// </summary>
//     [PipelineStep("YooAsset")]
//     public class HybridCLRBuildStep : IBuildStep
//     {
//         private readonly YooAssetBuildPipeline _pipeline;
//         private bool _enabled = true;
//
//         public string Name => "HybridCLR 编译";
//         public int Order => 10;
//         public bool Enabled
//         {
//             get => _enabled;
//             set
//             {
//                 if (_enabled != value)
//                 {
//                     _enabled = value;
//                     Settings.SetStepEnabled(Name, value);
//                     Settings.Save();
//                 }
//             }
//         }
//         public bool HasConfigGUI => true;
//
//         private YooAssetBuildSettings Settings => YooAssetBuildSettings.instance;
//
//         [System.Flags]
//         private enum MissingFiles
//         {
//             None = 0,
//             HotUpdateDlls = 1,
//             AOTDlls = 2,
//             AOTGenericRef = 4,
//             LinkXml = 8
//         }
//
//         public HybridCLRBuildStep(IBuildPipeline pipeline)
//         {
//             _pipeline = (YooAssetBuildPipeline)pipeline;
//             _enabled = Settings.GetStepEnabled(Name);
//         }
//
//         public void DrawConfigGUI()
//         {
//             EditorGUI.BeginChangeCheck();
//
//             Settings.dllBuildMode = (YooAssetBuildSettings.DllBuildMode)
//                 EditorGUILayout.EnumPopup("编译模式", Settings.dllBuildMode);
//
//             var hint = Settings.dllBuildMode switch
//             {
//                 YooAssetBuildSettings.DllBuildMode.Auto => "自动检测缺失文件，智能编译",
//                 YooAssetBuildSettings.DllBuildMode.ForceAll => "强制重新生成所有DLL（包括AOT）",
//                 YooAssetBuildSettings.DllBuildMode.None => "不编译DLL，直接使用现有文件",
//                 _ => ""
//             };
//             EditorGUILayout.HelpBox(hint, MessageType.Info);
//
//             if (EditorGUI.EndChangeCheck())
//                 Settings.Save();
//         }
//
//         public bool Execute(PackFlowContext context)
//         {
//             // 检查是否有选中的热更包
//             if (!_pipeline.HasHotUpdatePackageSelected())
//             {
//                 context.Log("未选中热更包，跳过 HybridCLR 编译");
//                 return true;
//             }
//
//             var settings = YooAssetBuildSettings.instance;
//             if (settings.dllBuildMode == YooAssetBuildSettings.DllBuildMode.None)
//             {
//                 context.Log("DLL编译模式为None，跳过编译");
//                 return true;
//             }
//
// #if HYBRIDCLR_INSTALLED
//             try
//             {
//                 if (settings.dllBuildMode == YooAssetBuildSettings.DllBuildMode.ForceAll)
//                 {
//                     context.Log("强制编译所有DLL...");
//                     HybridCLR.Editor.Commands.PrebuildCommand.GenerateAll();
//                     return true;
//                 }
//
//                 // Auto模式
//                 var missing = CheckMissingFiles(context.BuildTarget);
//                 if (missing == MissingFiles.None)
//                 {
//                     context.Log("所有文件已存在，只编译热更DLL");
//                     HybridCLR.Editor.Commands.CompileDllCommand.CompileDll(context.BuildTarget);
//                 }
//                 else if (NeedFullBuild(missing))
//                 {
//                     context.Log($"缺失关键文件，执行完整编译");
//                     HybridCLR.Editor.Commands.PrebuildCommand.GenerateAll();
//                 }
//                 else
//                 {
//                     context.Log("只编译热更DLL");
//                     HybridCLR.Editor.Commands.CompileDllCommand.CompileDll(context.BuildTarget);
//                 }
//
//                 return true;
//             }
//             catch (System.Exception e)
//             {
//                 context.LogError($"HybridCLR 编译失败: {e.Message}");
//                 return false;
//             }
// #else
//             context.Log("未安装 HybridCLR，跳过编译");
//             return true;
// #endif
//         }
//
//         private MissingFiles CheckMissingFiles(BuildTarget buildTarget)
//         {
//             MissingFiles missing = MissingFiles.None;
//             string platform = buildTarget.ToString();
//             string projectPath = Application.dataPath.Replace("/Assets", "");
//
//             // 检查热更DLLs
//             string hotUpdatePath = Path.Combine(projectPath, "HybridCLRData", "HotUpdateDlls", platform);
//             if (!Directory.Exists(hotUpdatePath) || Directory.GetFiles(hotUpdatePath, "*.dll").Length == 0)
//                 missing |= MissingFiles.HotUpdateDlls;
//
//             // 检查AOT DLLs
//             string aotPath = Path.Combine(projectPath, "HybridCLRData", "AssembliesPostIl2CppStrip", platform);
//             if (!Directory.Exists(aotPath) || Directory.GetFiles(aotPath, "*.dll").Length == 0)
//                 missing |= MissingFiles.AOTDlls;
//
//             // 检查AOTGenericReferences.cs
//             string aotRefPath = Path.Combine(Application.dataPath, "HybridCLRGenerate", "AOTGenericReferences.cs");
//             if (!File.Exists(aotRefPath))
//                 missing |= MissingFiles.AOTGenericRef;
//
//             // 检查link.xml
//             string linkXmlPath = Path.Combine(Application.dataPath, "HybridCLRGenerate", "link.xml");
//             if (!File.Exists(linkXmlPath))
//                 missing |= MissingFiles.LinkXml;
//
//             return missing;
//         }
//
//         private bool NeedFullBuild(MissingFiles missing)
//         {
//             // 如果只缺少热更DLL，不需要完整编译
//             return (missing & ~MissingFiles.HotUpdateDlls) != MissingFiles.None;
//         }
//     }
// }
// #endif
