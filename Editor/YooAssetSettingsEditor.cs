using UnityEditor;
using UnityEngine;
using Azathrix.EnvInstaller.Editor.UI;

namespace Azathrix.YooAssetExtension.Editor
{
    /// <summary>
    /// YooAsset配置编辑器
    /// </summary>
    [CustomEditor(typeof(YooAssetSettings))]
    public class YooAssetSettingsEditor : UnityEditor.Editor
    {
        private static readonly string[] RequiredDependencies = { "com.tuyoogame.yooasset" };

        public override void OnInspectorGUI()
        {
            // 检查依赖
            if (!EnvDependencyUI.DrawDependencyCheck(RequiredDependencies))
            {
                EditorGUILayout.HelpBox("YooAsset 未安装，部分功能不可用。", MessageType.Warning);
                EditorGUILayout.Space();
            }

#if YOOASSET_INSTALLED
            EditorGUILayout.HelpBox("YooAsset 已安装", MessageType.Info);
#endif

            EditorGUILayout.Space();
            base.OnInspectorGUI();
        }
    }
}
