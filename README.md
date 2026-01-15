<p align="center">
  <img src="https://img.icons8.com/fluency/96/package.png" alt="Yoo System Logo" width="96">
</p>

<h1 align="center">Yoo System</h1>

<p align="center">
  YooAsset 资源管理系统的 Azathrix Framework 集成扩展
</p>

<p align="center">
  <a href="https://github.com/azathrix/YooSystem"><img src="https://img.shields.io/badge/GitHub-Azathrix-black.svg" alt="GitHub"></a>
  <a href="https://www.npmjs.com/package/com.azathrix.yoo-system"><img src="https://img.shields.io/npm/v/com.azathrix.yoo-system.svg" alt="npm"></a>
  <a href="#license"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License"></a>
  <a href="https://unity.com/"><img src="https://img.shields.io/badge/Unity-6000.3+-black.svg" alt="Unity"></a>
</p>

---

## 特性

- 🚀 **自动热更新** - 启动时自动完成 Package 初始化、版本检查、清单更新、资源下载
- 📦 **多 Package 支持** - 支持多个资源包独立管理和按需加载
- ⚙️ **Profile 配置** - 类似 Addressable 的 Profile 系统，轻松切换开发/测试/生产环境
- 🔄 **资源加载** - 同步/异步加载，支持 Prefab、场景、原始文件等
- 📥 **下载管理** - 进度监控、暂停/恢复/取消、按标签或路径下载
- 💾 **缓存管理** - 清理未使用缓存、卸载未使用资源
- 🔌 **框架集成** - 无缝集成 Azathrix Framework，自动注册为 IResourcesLoader

## 安装

### 方式一：Package Manager 添加 Scope（推荐）

1. 打开 `Edit > Project Settings > Package Manager`
2. 在 `Scoped Registries` 中添加：
   - **Name**: `Azathrix`
   - **URL**: `https://registry.npmjs.org`
   - **Scope(s)**: `com.azathrix`
3. 点击 `Save`
4. 打开 `Window > Package Manager`
5. 切换到 `My Registries`
6. 找到 `Yoo System` 并安装

### 方式二：Git URL

1. 打开 `Window > Package Manager`
2. 点击 `+` > `Add package from git URL...`
3. 输入：`https://github.com/azathrix/YooSystem.git#latest`

> ⚠️ Git 方式无法自动解析依赖，需要先手动安装：
> - Azathrix Framework
> - UniTask
> - PackFlow

### 方式三：npm 命令

在项目的 `Packages` 目录下执行：

```bash
npm install com.azathrix.yoo-system
```

## 快速开始

### 1. 配置 YooAsset

打开 `Project Settings > Azathrix > YooAsset配置`，设置：

- **运行模式**：EditorSimulate（开发）/ Host（线上）
- **资源服务器地址**：热更新服务器 URL
- **资源包配置**：添加需要管理的 Package

### 2. 获取 YooSystem

```csharp
using Azathrix.YooSystem;

var yooSystem = AzathrixFramework.GetSystem<YooSystem>();
```

### 3. 加载资源

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

### 4. Handle 加载（需要手动释放）

```csharp
// 获取 Handle 以便手动管理生命周期
var handle = await yooSystem.LoadAssetWithHandleAsync<Sprite>("Assets/UI/icon.png");
var sprite = handle.AssetObject as Sprite;

// 使用完毕后释放
handle.Release();
```

### 5. 下载管理

```csharp
// 检查是否需要下载
if (yooSystem.NeedDownload(new[] { "level1" }))
{
    var (count, bytes) = yooSystem.GetDownloadInfo(new[] { "level1" });
    Debug.Log($"需要下载 {count} 个文件，共 {bytes} 字节");
}

// 获取下载管理器
var downloadManager = yooSystem.GetDownloadManager();

// 监听进度
downloadManager.OnTaskProgress += (taskId, progress) =>
{
    Debug.Log($"下载进度: {progress.CurrentCount}/{progress.TotalCount}");
};

// 按标签下载
await downloadManager.DownloadByTagsAsync("task1", "level1", "level2");
```

## 配置说明

### Profile 配置

| 字段 | 说明 | 默认值 |
|------|------|--------|
| playMode | 运行模式 | EditorSimulateMode |
| hostServerURL | 资源服务器地址 | http://127.0.0.1 |
| downloadingMaxNum | 最大并发下载数 | 10 |
| failedTryAgain | 失败重试次数 | 3 |
| autoInitOnStartup | 启动时自动初始化 | true |

### Package 配置

| 字段 | 说明 |
|------|------|
| packageName | 资源包名称 |
| hostServerURL | 包专用服务器地址（可选） |
| autoDownloadTags | 启动时自动下载的标签 |

## 运行模式

| 模式 | 说明 | 使用场景 |
|------|------|----------|
| EditorSimulateMode | 编辑器模拟，使用 AssetDatabase | 开发调试 |
| OfflinePlayMode | 离线模式，使用本地资源包 | 单机游戏 |
| HostPlayMode | 联机模式，支持热更新 | 线上环境 |
| WebPlayMode | WebGL 模式 | Web 平台 |

## API 参考

### 资源加载

| 方法 | 说明 |
|------|------|
| `LoadAsync<T>(key)` | 异步加载资源 |
| `Load<T>(key)` | 同步加载资源 |
| `InstantiateAsync(key, parent)` | 异步实例化 Prefab |
| `LoadSceneAsync(key, mode)` | 异步加载场景 |
| `LoadAssetWithHandleAsync<T>(key)` | 异步加载并返回 Handle |
| `LoadRawFileWithHandleAsync(key)` | 异步加载原始文件 |

### Package 管理

| 方法 | 说明 |
|------|------|
| `GetPackage(name)` | 获取资源包 |
| `IsPackageInitialized(name)` | 检查包是否已初始化 |
| `InitPackageAsync(name)` | 运行时初始化包 |
| `UpdateVersionAsync(name)` | 更新包版本 |
| `UpdateManifestAsync(name)` | 更新包清单 |

### 下载管理

| 方法 | 说明 |
|------|------|
| `GetDownloadManager(name)` | 获取下载管理器 |
| `NeedDownload(tags)` | 检查是否需要下载 |
| `GetDownloadInfo(tags)` | 获取下载信息 |
| `CreateDownloaderByTags(name, tags)` | 按标签创建下载器 |

### 缓存管理

| 方法 | 说明 |
|------|------|
| `ClearUnusedCacheAsync()` | 清理未使用缓存 |
| `ClearAllCacheAsync()` | 清理所有缓存 |
| `UnloadUnusedAssetsAsync()` | 卸载未使用资源 |

## 依赖

| 依赖 | 版本 | 说明 |
|------|------|------|
| com.azathrix.framework | 0.0.5+ | Azathrix 框架核心 |
| com.azathrix.unitask | 2.5.10+ | 异步任务库 |
| com.azathrix.pack-flow | 1.0.1+ | 打包工作流 |
| YooAsset | 2.3.x | 资源管理系统（自动安装） |

## 架构

```
外部代码 → YooSystem → YooService (internal) → YooAssets
```

- **YooSystem**: 对外暴露的系统接口，实现 ISystem、IResourcesLoader 等
- **YooService**: 内部静态服务类，统一管理 Package、版本、下载等
- **HotUpdatePhase**: 启动阶段，在系统注册前执行热更新流程
- **DownloadManager**: 下载管理器，提供细粒度下载控制

## License

MIT License

Copyright (c) 2024 Azathrix
