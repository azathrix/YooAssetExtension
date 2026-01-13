# YooSystem 测试配置

## 测试资源

- `TestAssets/TestPrefab.prefab` - 测试用 Prefab

## 使用方法

### 1. 导入测试 Collector 配置

在 YooAsset Collector 窗口中：
1. 点击 "导入" 按钮
2. 选择 `Packages/YooAssetExtension/Tests/TestCollectorSetting.asset`

### 2. 配置 YooAssetSettings

确保 `Assets/Resources/YooAssetSettings.asset` 中包含 `TestPackage`：

```yaml
packages:
- packageName: TestPackage
  autoDownloadTags: [test]
```

### 3. 运行测试

#### EditorSimulateMode（快速测试）
- 设置 `activeProfileIndex: 0`
- 直接运行 Test Runner

#### HostPlayMode（热更测试）
1. 打包 TestPackage
2. 启动本地服务器（端口 8080）
3. 设置 `activeProfileIndex: 1`
4. 运行 Test Runner

## 测试覆盖

- 包初始化
- 资源加载（LoadAsync, Load）
- Prefab 实例化（InstantiateAsync）
- 资源检查（CheckAssetExists）
- 下载信息查询
- 缓存管理
- 资源卸载
