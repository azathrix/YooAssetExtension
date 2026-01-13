# YooSystem

YooAsset 资源管理系统的 Azathrix Framework 集成扩展。

## 功能

- 自动热更新流程（Package 初始化、版本检查、清单更新、资源下载）
- 多 Package 支持
- Profile 配置（类似 Addressable 的 Profile 系统）
- 资源加载（同步/异步）
- 下载管理（进度监控、暂停/恢复/取消）
- 缓存管理

## 安装

### 依赖

- Unity 6000.3+
- [YooAsset](https://github.com/tuyoogame/YooAsset) 2.3.x
- Azathrix Framework
- UniTask

### 通过 Package Manager

```json
{
  "dependencies": {
    "com.azathrix.yoo-system": "0.0.1"
  }
}
```

## 配置

在 `Project Settings > Azathrix > YooAsset配置` 中配置：

### Profile 配置

| 字段 | 说明 |
|------|------|
| playMode | 运行模式（EditorSimulate/Offline/Host/Web） |
| hostServerURL | 资源服务器地址 |
| downloadingMaxNum | 最大并发下载数 |
| failedTryAgain | 失败重试次数 |

### Package 配置

| 字段 | 说明 |
|------|------|
| packageName | 资源包名称 |
| hostServerURL | 包专用服务器地址（可选） |
| autoDownloadTags | 启动时自动下载的标签 |

## 使用

### 获取 YooSystem

```csharp
var yooSystem = AzathrixFramework.GetSystem<YooSystem>();
```

### 资源加载

```csharp
// 异步加载
var prefab = await yooSystem.LoadAsync<GameObject>("Assets/Prefabs/Player.prefab");

// 同步加载
var config = yooSystem.Load<TextAsset>("Assets/Configs/game.json");

// 实例化
var player = await yooSystem.InstantiateAsync("Assets/Prefabs/Player.prefab", parent);

// 场景加载
await yooSystem.LoadSceneAsync("Assets/Scenes/Game.unity", LoadSceneMode.Single);
```

### Handle 加载（需要手动释放）

```csharp
// 获取 Handle 以便手动管理生命周期
var handle = await yooSystem.LoadAssetWithHandleAsync<Sprite>("Assets/UI/icon.png");
var sprite = handle.AssetObject as Sprite;

// 使用完毕后释放
handle.Release();
```

### 下载管理

```csharp
// 获取下载管理器
var downloadManager = yooSystem.GetDownloadManager();

// 检查是否需要下载
if (yooSystem.NeedDownload(new[] { "level1" }))
{
    var (count, bytes) = yooSystem.GetDownloadInfo(new[] { "level1" });
    Debug.Log($"需要下载 {count} 个文件，共 {bytes} 字节");
}

// 按标签下载
downloadManager.OnTaskProgress += (taskId, progress) =>
{
    Debug.Log($"下载进度: {progress.CurrentCount}/{progress.TotalCount}");
};
await downloadManager.DownloadByTagsAsync("task1", "level1", "level2");

// 暂停/恢复/取消
downloadManager.PauseAll();
downloadManager.ResumeAll();
downloadManager.CancelAll();
```

### 动态 Package 管理

```csharp
// 运行时初始化新 Package
await yooSystem.InitPackageAsync("DLCPackage");

// 更新版本和清单
var (success, version) = await yooSystem.UpdateVersionAsync("DLCPackage");
await yooSystem.UpdateManifestAsync("DLCPackage");

// 创建下载器
var downloader = yooSystem.CreateDownloaderByTags("DLCPackage", "dlc1");
downloader.BeginDownload();
```

### 缓存管理

```csharp
// 清理未使用缓存
await yooSystem.ClearUnusedCacheAsync();

// 清理所有缓存
await yooSystem.ClearAllCacheAsync();

// 卸载未使用资源
await yooSystem.UnloadUnusedAssetsAsync();
```

## 运行模式

| 模式 | 说明 | 使用场景 |
|------|------|----------|
| EditorSimulateMode | 编辑器模拟，使用 AssetDatabase | 开发调试 |
| OfflinePlayMode | 离线模式，使用本地资源包 | 单机游戏 |
| HostPlayMode | 联机模式，支持热更新 | 线上环境 |
| WebPlayMode | WebGL 模式 | Web 平台 |

## 架构

```
外部代码 → YooSystem → YooService (internal) → YooAssets
```

- `YooSystem`: 对外暴露的系统接口，实现 ISystem、IResourcesLoader 等
- `YooService`: 内部静态服务类，统一管理 Package、版本、下载等
- `HotUpdatePhase`: 启动阶段，在系统注册前执行热更新流程
- `DownloadManager`: 下载管理器，提供细粒度下载控制

## License

MIT
