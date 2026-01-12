using System.Linq;
using Azathrix.EnvInstaller.Editor.UI;
using UnityEditor;
using UnityEngine;

namespace Azathrix.YooSystem.Editor.Editor
{
    /// <summary>
    /// YooAsset配置编辑器
    /// </summary>
    [CustomEditor(typeof(YooAssetSettings))]
    public class YooAssetSettingsEditor : UnityEditor.Editor
    {
        private static readonly string[] RequiredDependencies = { "com.tuyoogame.yooasset" };

        private SerializedProperty _profilesProperty;
        private SerializedProperty _activeProfileIndexProperty;
        private string[] _profileNames;

        private void OnEnable()
        {
            _profilesProperty = serializedObject.FindProperty("profiles");
            _activeProfileIndexProperty = serializedObject.FindProperty("activeProfileIndex");
            UpdateProfileNames();
        }

        private void UpdateProfileNames()
        {
            var settings = target as YooAssetSettings;
            if (settings?.profiles == null || settings.profiles.Length == 0)
            {
                _profileNames = new[] { "Default" };
                return;
            }

            _profileNames = settings.profiles.Select((p, i) =>
                string.IsNullOrEmpty(p.profileName) ? $"Profile {i}" : p.profileName).ToArray();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

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

            // Profile 切换下拉菜单
            DrawProfileSelector();

            EditorGUILayout.Space();

            // 绘制当前 Profile 的配置
            DrawCurrentProfileConfig();

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawProfileSelector()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Profile 配置", EditorStyles.boldLabel);

            UpdateProfileNames();

            EditorGUI.BeginChangeCheck();
            var newIndex = EditorGUILayout.Popup("当前 Profile", _activeProfileIndexProperty.intValue, _profileNames);
            if (EditorGUI.EndChangeCheck())
            {
                _activeProfileIndexProperty.intValue = Mathf.Clamp(newIndex, 0, _profilesProperty.arraySize - 1);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("添加 Profile", GUILayout.Width(100)))
            {
                AddProfile();
            }

            EditorGUI.BeginDisabledGroup(_profilesProperty.arraySize <= 1);
            if (GUILayout.Button("删除当前", GUILayout.Width(100)))
            {
                RemoveCurrentProfile();
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("复制当前", GUILayout.Width(100)))
            {
                DuplicateCurrentProfile();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void DrawCurrentProfileConfig()
        {
            if (_profilesProperty.arraySize == 0) return;

            var currentIndex = Mathf.Clamp(_activeProfileIndexProperty.intValue, 0, _profilesProperty.arraySize - 1);
            var currentProfile = _profilesProperty.GetArrayElementAtIndex(currentIndex);

            // 确保 _profileNames 数组有效
            if (_profileNames == null || currentIndex >= _profileNames.Length)
                UpdateProfileNames();

            var profileName = (currentIndex < _profileNames.Length) ? _profileNames[currentIndex] : $"Profile {currentIndex}";

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Profile: {profileName}", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            // Profile 名称
            EditorGUILayout.PropertyField(currentProfile.FindPropertyRelative("profileName"),
                new GUIContent("Profile 名称"));

            EditorGUILayout.Space();

            // 运行模式
            EditorGUILayout.PropertyField(currentProfile.FindPropertyRelative("playMode"),
                new GUIContent("运行模式"));

            EditorGUILayout.Space();

            // 远程配置
            EditorGUILayout.LabelField("远程配置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(currentProfile.FindPropertyRelative("hostServerURL"),
                new GUIContent("服务器地址"));
            EditorGUILayout.PropertyField(currentProfile.FindPropertyRelative("fallbackHostServerURL"),
                new GUIContent("备用服务器"));

            EditorGUILayout.Space();

            // 下载配置
            EditorGUILayout.LabelField("下载配置", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(currentProfile.FindPropertyRelative("downloadingMaxNum"),
                new GUIContent("最大并发数"));
            EditorGUILayout.PropertyField(currentProfile.FindPropertyRelative("failedTryAgain"),
                new GUIContent("重试次数"));

            EditorGUILayout.Space();

            // 启动配置
            EditorGUILayout.PropertyField(currentProfile.FindPropertyRelative("autoInitOnStartup"),
                new GUIContent("自动初始化"));

            EditorGUILayout.Space();

            // 资源包配置
            EditorGUILayout.LabelField("资源包配置", EditorStyles.boldLabel);
            var packagesProperty = currentProfile.FindPropertyRelative("packages");
            DrawPackagesList(packagesProperty);

            EditorGUI.indentLevel--;
            EditorGUILayout.EndVertical();
        }

        private void DrawPackagesList(SerializedProperty packagesProperty)
        {
            for (int i = 0; i < packagesProperty.arraySize; i++)
            {
                var pkg = packagesProperty.GetArrayElementAtIndex(i);
                var pkgName = pkg.FindPropertyRelative("packageName").stringValue;
                if (string.IsNullOrEmpty(pkgName)) pkgName = $"Package {i}";

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(pkgName, EditorStyles.boldLabel);
                if (GUILayout.Button("×", GUILayout.Width(20)) && packagesProperty.arraySize > 1)
                {
                    packagesProperty.DeleteArrayElementAtIndex(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(pkg.FindPropertyRelative("packageName"),
                    new GUIContent("包名称"));
                EditorGUILayout.PropertyField(pkg.FindPropertyRelative("hostServerURL"),
                    new GUIContent("服务器地址（可选）"));
                EditorGUILayout.PropertyField(pkg.FindPropertyRelative("fallbackHostServerURL"),
                    new GUIContent("备用服务器（可选）"));
                EditorGUILayout.PropertyField(pkg.FindPropertyRelative("autoDownloadTags"),
                    new GUIContent("自动下载标签"));
                EditorGUI.indentLevel--;

                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("添加资源包"))
            {
                packagesProperty.InsertArrayElementAtIndex(packagesProperty.arraySize);
                var newPkg = packagesProperty.GetArrayElementAtIndex(packagesProperty.arraySize - 1);
                newPkg.FindPropertyRelative("packageName").stringValue = $"Package{packagesProperty.arraySize}";
                newPkg.FindPropertyRelative("hostServerURL").stringValue = "";
                newPkg.FindPropertyRelative("fallbackHostServerURL").stringValue = "";
            }
        }

        private void AddProfile()
        {
            _profilesProperty.InsertArrayElementAtIndex(_profilesProperty.arraySize);
            var newProfile = _profilesProperty.GetArrayElementAtIndex(_profilesProperty.arraySize - 1);
            newProfile.FindPropertyRelative("profileName").stringValue = $"Profile {_profilesProperty.arraySize}";
            _activeProfileIndexProperty.intValue = _profilesProperty.arraySize - 1;
            UpdateProfileNames();
        }

        private void RemoveCurrentProfile()
        {
            if (_profilesProperty.arraySize <= 1) return;

            var currentIndex = _activeProfileIndexProperty.intValue;
            _profilesProperty.DeleteArrayElementAtIndex(currentIndex);
            _activeProfileIndexProperty.intValue = Mathf.Clamp(currentIndex, 0, _profilesProperty.arraySize - 1);
            UpdateProfileNames();
        }

        private void DuplicateCurrentProfile()
        {
            var currentIndex = _activeProfileIndexProperty.intValue;
            _profilesProperty.InsertArrayElementAtIndex(currentIndex);
            var newProfile = _profilesProperty.GetArrayElementAtIndex(currentIndex + 1);
            newProfile.FindPropertyRelative("profileName").stringValue += " (Copy)";
            _activeProfileIndexProperty.intValue = currentIndex + 1;
            UpdateProfileNames();
        }
    }
}
