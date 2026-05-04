# 全局文件搜索

[English](README_EN.md)

一个基于 `Avalonia UI` + `.NET 8` 构建的桌面端全局文件搜索工具，用于在本地磁盘或指定目录中快速检索文件与文件夹，并提供搜索历史、结果保存、背景个性化和仪表盘统计等能力。

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4.svg)
![Avalonia](https://img.shields.io/badge/Avalonia-12.0-8B44AC.svg)
![Platform](https://img.shields.io/badge/platform-Windows-lightgrey.svg)

## 界面预览

> ```
> ├── 仪表盘页：展示累计搜索次数、耗时、最近记录与粒子动效
> ├── 搜索页：输入关键词、选择磁盘、实时显示扫描进度与命中结果
> ├── 历史页：查看过往搜索会话与手动保存的结果快照
> └── 设置页：配置背景图片/视频、透明度与播放选项
> ```

## 项目简介

本项目面向 Windows 桌面场景，提供一个现代化、多页面、可视化的本地文件检索应用。  
它不仅支持按关键词进行全局搜索，还支持：

- 多磁盘搜索和指定目录搜索
- 隐藏文件、系统文件、文件夹的搜索开关
- 精确匹配与模糊匹配
- 搜索结果导出
- 搜索历史记录与统计
- 将搜索结果中的一个或多个条目保存到历史中，便于后续回看
- 可配置背景图片 / 背景视频
- 仪表盘统计与轻量动效展示

## 功能特性

### 1. 文件搜索

- 支持输入文件名、关键词进行全局搜索
- 支持选择一个或多个磁盘作为搜索范围
- 支持指定某个文件夹作为搜索根目录
- 支持包含或排除：
  - 系统文件
  - 隐藏文件
  - 文件夹
- 支持搜索模式：
  - 精确搜索
  - 模糊搜索
  - 精确 + 模糊混合搜索
- 搜索过程中显示：
  - 已扫描条目数量
  - 实时命中数量
  - 当前扫描位置
  - 已耗时
- 支持中途停止搜索，并记录已用时间

### 2. 搜索结果管理

- 搜索结果列表支持选中高亮
- 支持打开文件
- 支持打开所在目录
- 支持删除选中项
- 支持按文件后缀过滤结果
- 支持将结果导出为日志文件
- 支持多选搜索结果，并保存到对应的历史记录中

### 3. 搜索历史

- 自动记录每次搜索会话
- 历史中记录的信息包括：
  - 搜索时间
  - 搜索范围
  - 搜索词
  - 扫描数量
  - 命中数量
  - 搜索耗时
  - 搜索状态（已完成 / 已停止）
- 支持在某次历史记录中查看手动保存的文件或文件夹
- 支持从历史页直接打开保存项或打开其所在位置
- 支持清空历史记录

### 4. 仪表盘

- 展示累计搜索次数
- 展示累计搜索耗时
- 展示最近搜索记录
- 展示当前时间与日期
- 提供轻量粒子动效与伪 3D 科技风格视觉效果

### 5. 个性化设置

- 支持背景图片
- 支持背景视频
- 支持背景透明度调整
- 支持控制背景视频播放与静音状态
- 支持本地持久化用户设置

## 技术栈

### 核心框架

- `.NET 8`
- `Avalonia 12`
- `Avalonia.Desktop`
- `Avalonia.Themes.Fluent`

### MVVM 与状态管理

- `CommunityToolkit.Mvvm`

### 媒体能力

- `LibVLCSharp.Avalonia`
- `VideoLAN.LibVLC.Windows`

### 数据存储

- `System.Text.Json`
- 本地 JSON 文件持久化搜索历史与用户偏好

## 项目结构

```text
全局文件搜索/
├─ .github/
│  └─ workflows/
│     └─ dotnet.yml        GitHub Actions CI 配置
├─ Assets/                 图标与资源文件
├─ Models/                 数据模型
├─ Services/               核心服务层
├─ ViewModels/             视图模型层
├─ Views/                  Avalonia 视图
├─ App.axaml               应用级样式与资源
├─ App.axaml.cs            应用入口初始化
├─ Program.cs              程序启动入口
├─ ViewLocator.cs          ViewModel 与 View 绑定定位
├─ 全局文件搜索.csproj      项目配置
├─ README.md               中文说明文档
├─ README_EN.md            英文说明文档
└─ LICENSE                 MIT 许可证
```

## 主要模块说明

### `Views/`

界面层，负责页面布局与交互展示，主要页面包括：

- `MainWindow`：主窗口和导航容器
- `DashboardView`：仪表盘
- `SearchView`：文件搜索页
- `HistoryView`：搜索历史页
- `SettingsView`：设置页

### `ViewModels/`

业务交互核心，负责将界面与服务层连接起来：

- `MainWindowViewModel`
  - 管理页面切换
  - 持有各子页面 ViewModel
- `SearchViewModel`
  - 搜索流程控制
  - 搜索状态更新
  - 结果过滤 / 导出 / 保存到历史
- `HistoryViewModel`
  - 搜索历史读取
  - 保存项展示与打开
- `DashboardViewModel`
  - 统计信息聚合
  - 最近搜索展示
  - 粒子动画和动态效果
- `SettingsViewModel`
  - 背景资源加载
  - 偏好设置持久化

### `Services/`

服务层负责实际功能实现：

- `FileSearchService`
  - 核心搜索引擎
  - 遍历磁盘与目录
  - 生成搜索结果
  - 统计扫描进度与耗时
- `SearchHistoryService`
  - 搜索历史读写
  - 最近记录获取
  - 保存搜索结果快照
- `AppPreferencesService`
  - 用户设置读写
- `BackgroundMediaResolver`
  - 背景资源类型解析（图片 / 视频 / 空）

### `Models/`

数据承载对象，主要包括：

- `SearchResultItem`：单个搜索结果
- `SearchHistoryEntry`：单次搜索历史
- `SavedResultSnapshot`：历史中的保存结果快照
- `AppPreferences`：本地设置
- `DriveItem`：磁盘项
- `BackgroundMediaDescriptor`：背景媒体描述

## 架构说明

项目采用典型的 `MVVM` 架构：

1. `View`
   - 负责界面布局和绑定
   - 不承载复杂业务逻辑
2. `ViewModel`
   - 负责状态管理、命令、交互流程
   - 将服务层结果转为可绑定数据
3. `Service`
   - 负责搜索、历史、设置、背景解析等核心能力
4. `Model`
   - 负责描述业务数据结构

这种设计的优点：

- 结构清晰
- 页面职责分离
- 便于后续扩展
- 便于维护和重构

## 搜索流程概览

1. 用户在搜索页输入关键词
2. 选择磁盘或指定目录
3. `SearchViewModel` 组装 `SearchRequest`
4. `FileSearchService` 执行遍历与匹配
5. 通过进度回调实时更新扫描状态
6. 搜索完成或停止后记录历史
7. 用户可将选中的搜索结果保存到该次历史记录中
8. 历史页可重新查看这些保存项

## 本地数据存储

应用会在本地用户目录下保存搜索历史与偏好配置，便于下次启动继续使用：

- 搜索历史：`LocalApplicationData/全局文件搜索/search_history.json`
- 应用设置：`LocalApplicationData/全局文件搜索/` 下的偏好文件

## 运行环境

### 开发环境

- Windows
- .NET 8 SDK

### 安装依赖

```bash
dotnet restore
```

### 启动项目

```bash
dotnet run
```

### 构建项目

```bash
dotnet build
```

### 发布项目

```bash
dotnet publish -c Release -r win-x64 --self-contained true
```


## 适用场景

- 本地文件快速定位
- 多磁盘文件检索
- 临时整理和筛选搜索结果
- 保留常用搜索结果快照，便于后续继续打开
- 作为 `Avalonia + MVVM` 桌面应用的实践示例

## 后续可扩展方向

- 增加文件内容检索
- 增加搜索结果排序方式切换
- 增加收藏夹 / 标签系统
- 增加批量操作能力
- 增加更丰富的预览能力
- 增加跨平台适配优化

## License

本项目采用 [MIT](LICENSE) 许可证开源。
