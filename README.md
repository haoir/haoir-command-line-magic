# HaOir Minecraft Launcher

## 项目说明

HaOir 是一个基于 WPF (.NET 8) 和 CmlLib.Core 的 Minecraft 启动器，支持原版、Forge、Fabric、NeoForge 和服务端下载。

## 依赖说明

- .NET 8.0 SDK（Windows）
- CmlLib.Core 4.1.0
- CmlLib.Core.Auth.Microsoft 3.4.0

## 编译指南

### 1. 安装依赖

在项目根目录执行：

```bash
dotnet restore
```

### 2. 准备背景图片

创建 `Images` 文件夹，放入两张高清 Minecraft 风景图：

```
Images/
├── bg1.jpg
└── bg2.jpg
```

### 3. 编译为独立可执行文件

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true
```

生成的 exe 文件位于：

```
bin/Release/net8.0-windows/win-x64/publish/HaOir.exe
```

### 4. 运行

双击 `HaOir.exe` 即可启动。

## 功能列表

- ✅ 原版版本列表获取与启动
- ✅ Forge 版本列表获取与启动
- ✅ Fabric 版本列表获取与启动
- ✅ NeoForge 版本支持（静态列表）
- ✅ 离线模式启动（自定义用户名）
- ✅ 正版模式启动（微软账户登录）
- ✅ 服务端 jar 下载
- ✅ 毛玻璃 UI 效果
- ✅ 背景图片循环渐变动画
- ✅ 窗口拖动与关闭

## 使用说明

1. 启动后点击右下角"开始游戏"按钮
2. 在左侧选择游戏类型（原版/Forge/Fabric/NeoForge/服务端）
3. 在右侧选择具体版本号
4. 在弹窗中选择登录模式：
   - **离线模式**：输入任意用户名，点击确定
   - **正版模式**：点击确定后将打开微软登录页，完成授权后自动启动
5. 选择"服务端"类型时，点击确定将下载对应版本的 server.jar 到启动器目录

## 文件结构

```
HaOir/
├── App.xaml                  # 应用入口
├── App.xaml.cs
├── MainWindow.xaml           # 主窗口 UI
├── MainWindow.xaml.cs        # 主窗口逻辑
├── HaOir.csproj              # 项目配置
├── Images/                   # 背景图片
│   ├── bg1.jpg
│   └── bg2.jpg
└── README.md                 # 本文件
```

## 注意事项

- 正版登录需要有效的 Microsoft 账户和 Minecraft 许可证
- 服务端下载需要网络连接
- 首次启动会自动下载 Java 和 Minecraft 资源
