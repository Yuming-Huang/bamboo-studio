# 竹拱工作室：上传、安装与联调完整指南

适用版本：竹拱工作室 0.8.0，Windows x64 桌面版。更新日期：2026-09-27。

本文面向第一次使用本项目的人，按“准备文件 → 安装环境 → 加载插件 → 打开 App → 完成第一次计算”的顺序编写。每个主要步骤都附有成功标志。示例路径可以更改，但执行命令前必须替换为自己的实际路径。

## 1. 先选择自己的操作路线

| 你拿到的文件或当前目的 | 阅读顺序 |
|---|---|
| 有包含 `BambooStudio.exe` 的完整运行包 | 第 2–7 节，跳过源码构建，继续第 9–12 节 |
| 只有前端和后端源码，没有 EXE | 第 2–12 节，包括第 8 节构建 |
| 只是将源码上传 GitHub | 第 15 节；上传本身不用安装 Rhino |
| 已正常使用 0.8.0，只更新界面或文档 | 第 14 节；不要因此重装插件 |
| 只打开配套 GH，不使用 App | 完成 Rhino、GH、Karamba3D 安装，再读第 13 节 |

**两个 GitHub ZIP 是源码包，不是安装包，里面没有 EXE 和编译后的 `.rhp`。** 普通使用者可以向维护者获取完整运行包；只有源码时需按第 8 节构建。

App 界面在 Windows 桌面程序中运行，结构计算在同一台电脑的 Rhino/Karamba3D 中完成。上传 GitHub 不会自动生成在线计算网站，也不需要配置公网服务器。

## 2. 软件、版本与下载入口

### 2.1 标准复现环境

以下具体版本来自本次读取本机安装文件的结果，用于复现现有环境，不表示它们都是官方最新版本。

| 软件或组件 | 本项目要求及已核对版本 | 普通运行 | 源码构建 |
|---|---|---|---|
| 操作系统 | Windows x64；当前交付为 Windows 桌面版 | 必需 | 必需 |
| Rhino | **Rhino 8 for Windows，优先复现 8.34（SR34）**；本机完整版本 `8.34.26223.11001` | 必需 | 必需 |
| Rhino 的运行时 | **.NET Core / .NET 8**；插件目标框架为 `net8.0-windows` | 必需 | 必需 |
| Grasshopper | **Rhino 8 自带的 Grasshopper 1（GH1）**；本机 `Grasshopper.dll` 文件及程序集版本 `8.34.26223.11001` | 必需 | 必需 |
| Karamba3D | **Rhino 8 / .NET Core 版，3.1 系列**；当前代码对照 `3.1.51222`，程序集版本 `3.1.51222.0` | 必需 | 必需 |
| Karamba3D 许可 | 需覆盖实际模型规模及分析功能的有效许可 | 真实计算必需 | 真实计算必需 |
| Bamboo Karamba3D Bridge | 本项目 **0.8.0** 插件；管理器可能显示 `0.8.0.0` | 必需 | 构建后安装 |
| Microsoft Edge WebView2 Runtime | Evergreen，Windows x64 | App 必需 | App 必需 |
| .NET Desktop Runtime 8 x64 | 供 Rhino .NET 8 环境和目标为 .NET 8 的测试程序使用；先检查是否已有 | 缺少时补装 | 缺少时补装 |
| Node.js / npm | 源码声明 Node ≥20；本次构建使用 Node `24.19.0`、npm `11.17.0`，可使用 Node 24 系列 x64 安装包 | 不需要 | 前端需要 |
| .NET SDK | **9.0 系列 SDK x64**；本次使用 `9.0.316`，仓库允许同系列较新的 SDK | 不需要 | 前后端需要 |
| Git | 用于命令行克隆和推送；下载 ZIP 也可以构建 | 不需要 | 可选 |

**Grasshopper 的 DLL 版本随 Rhino 更新，并不是要求另找一个“Grasshopper 8.34”安装包。** 直接使用 Rhino 8 自带的 GH1，不要用 Rhino 5 的旧独立安装包或 Grasshopper 2 替代。

Rhino 8.20 起默认使用 .NET 8，更早的 Rhino 8 默认运行时可能为 .NET 7。这个差异只用于判断运行时，**不表示本项目已经在所有 Rhino 8.20 以上版本验收通过**。标准复现环境仍为上表组合；更换 Rhino 或 Karamba3D 版本后要重新编译并完成真实联调。[Rhino 官方运行时说明](https://www.rhino3d.com/docs/guides/netcore/)

找不到同版 Karamba3D 安装包时，请向维护者确认兼容替代版本，不要仅凭版本号同属 3.1 就判断 API 和原生计算库完全兼容。

### 2.2 插件清单

当前工作台需要：Rhino 自带的 **Grasshopper**、另行安装的 **Karamba3D**，以及本项目的 **Bamboo Karamba3D Bridge**。

当前代码及配套 GH 不要求另装 LunchBox、Kangaroo、Human 或 Elefront，也不需要 Rhino.Compute 或 Hops。历史参考图片中的电池不等于当前软件依赖。

Three.js、WebView2 SDK 与 Rhino3dm 是前端构建依赖，构建脚本会下载；不用把它们安装成 GH 插件。

### 2.3 官方下载入口

| 软件 | 入口 | 下载选项 |
|---|---|---|
| Rhino | [Rhino 下载](https://www.rhino3d.com/en/download/) | Rhino 8 for Windows，使用自己的有效授权 |
| Grasshopper | [Grasshopper 官方入口](https://www.rhino3d.com/download/grasshopper/) | 使用随 Rhino 安装的 GH1 |
| Karamba3D | [Karamba3D Get Started](https://karamba3d.com/get-started/) | Rhino 8、Windows、.NET Core，使用确认过的版本 |
| WebView2 Runtime | [微软 WebView2 下载](https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section) | Evergreen Standalone Installer → x64 |
| .NET Desktop Runtime 8 | [.NET 8 下载](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) | `.NET Desktop Runtime` → Windows x64 |
| .NET SDK 9 | [.NET 9 下载](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) | `SDK` → Windows x64，不是只下载 Runtime |
| Node.js | [Node.js 下载](https://nodejs.org/en/download) | Windows x64 安装包，源码构建使用 |

Rhino 和 Karamba3D 授权程序不随源码分发。使用学校或单位许可时，先确认账号、团队权限或授权服务器可以正常使用。

## 3. 解压与目录安排

完整解压到固定位置。下面是可选的示例结构：

```text
C:\BambooStudio\
├─ source\
│  ├─ bamboo-studio-frontend\
│  │  ├─ README.md
│  │  ├─ Build.ps1
│  │  └─ package.json ...
│  └─ bamboo-studio-backend\
│     ├─ README.md
│     ├─ Build.ps1
│     └─ grasshopper\BambooStudio-0.8.0.gh ...
└─ runtime\
   ├─ App\                         完整 App 发布目录
   ├─ Rhino连接插件\BambooKarambaBridge.rhp
   └─ 配套GH\BambooStudio-0.8.0.gh
```

已有运行包可以保持自身结构。不要在压缩软件内部直接运行文件，不要只复制一个 EXE 而漏掉 DLL 和子目录。

`.rhp` 先放进固定目录再安装。Rhino 会记录安装位置，拖入插件并不意味着 Rhino 自动复制了文件；安装后不要随意移动该文件。[Rhino 官方插件安装说明](https://www.rhino3d.com/en/docs/guides/scripts-plugins/how-to-use/)

前后端源码不必在同一个父目录。所有命令中的示例路径都应替换为自己的真实路径。

## 4. 安装 Rhino 并确认 .NET 8

### 4.1 首次安装

1. 保存已有 Rhino/GH 文件，关闭 App 和全部 Rhino 窗口。
2. 安装 Rhino 8 for Windows，优先复现第 2 节版本。
3. 按安装器完成安装和授权；只有安装器要求时才需重启整台电脑。
4. 打开 Rhino，在命令栏输入 `_SystemInfo`。
5. 核对 Rhino 完整版本和 .NET 运行时，保留一份系统信息用于排查。

**成功标志：** Rhino 可正常启动，版本符合预期，当前运行时为 .NET 8。

### 4.2 如果当前运行时不是 .NET 8

1. 如果缺少 .NET Desktop Runtime 8 x64，先按第 2.3 节安装。
2. 在 Rhino 输入 `_SetDotNetRuntime`。
3. 按命令行提示选择 `Runtime`，把模式设为 `NETCore`，不要选 `NETFramework`。
4. 保存文件，**退出所有 Rhino 进程后重新打开**。只关 GH 窗口不会切换运行时。
5. 再运行 `_SystemInfo`，确认当前实际版本为 .NET 8。
6. 如果仍为 .NET 7，可复制一份 Rhino 快捷方式，在“属性 → 目标”中于 EXE 路径后加空格和 `/netcore-8`：

   ```text
   "C:\Program Files\Rhino 8\System\Rhino.exe" /netcore-8
   ```

7. 退出所有 Rhino，再用该快捷方式打开并核对系统信息。

上述切换方式与启动参数见 [Rhino 官方说明](https://www.rhino3d.com/docs/guides/netcore/)。**构建源码需要 .NET 9 SDK，不代表 Rhino 8 应改用 .NET 9 运行。** 本项目要求 Rhino 使用 .NET 8。

## 5. 打开并检查 Grasshopper

1. 在 Rhino 命令栏输入 `Grasshopper`，回车。
2. 等待窗口和工具栏加载。
3. 在帮助/关于信息中查看 GH 信息；也可用 Rhino `_SystemInfo` 记录环境。
4. 要核对本指南中的 DLL 版本，可查看 Rhino 安装目录的 `Plug-ins\Grasshopper\Grasshopper.dll`，右键“属性 → 详细信息”。标准环境版本为 `8.34.26223.11001`。

**成功标志：** 输入命令能打开 Rhino 随附的 GH1，基础组件显示正常。

如果命令不存在或内置 GH 加载失败，先修复 Rhino 安装，不要通过旧版独立 GH 安装器补救。更新 Rhino 后应使用与其一起更新的 GH。

## 6. 安装 Karamba3D 与许可

### 6.1 只选择一种安装方式

本机标准环境的 Karamba 位于 Rhino 安装目录。新用户可以采用官方 MSI；已通过 PackageManager 安装的用户可以保留原方式，并按第 8 节配置其实际目录。

**同一 Rhino 8 不要同时安装 MSI 版和 PackageManager 版 Karamba3D。** 官方指出两种安装并存可能影响许可证命令。[Karamba3D 安装说明](https://manual.karamba3d.com/1-introduction/a.2-installation)

### 6.2 路线 A：官方 MSI

1. 关闭 App，保存并退出全部 Rhino。
2. 下载维护者确认过的 Karamba3D 版本，选择 **Rhino 8 / .NET Core / Windows**。
3. 不要选择 Rhino 7 版或 .NET Framework 版。
4. 运行 MSI，确认目标为自己的 Rhino 8 安装目录，保留计算库、GH 组件和许可组件。
5. 单位选择 SI。本项目界面使用 mm 等输入单位，代码会转换到计算核心单位，不要自己将所有参数再乘除 1000。
6. 完成安装后重新打开 Rhino。
7. 按第 6.4 节获取许可，再打开 Grasshopper，检查是否出现 Karamba3D 分类。

常见安装目录为 `C:\Program Files\Rhino 8\Plug-ins\Karamba\`；其他磁盘会相应变化。目录应包含 `karambaCommon.dll` 和配套原生计算库，不要只复制单个 DLL。[安装与单位设置](https://manual.karamba3d.com/1-introduction/a.2-installation)

### 6.3 路线 B：Rhino PackageManager

1. 打开 Rhino，输入 `_PackageManager`。
2. 搜索 Karamba3D；官方文档中的包名是 `karambaGH`，核对发布者和 Rhino 8 适用信息。
3. 选择维护者确认的版本；仅在所需版本属于预发布时启用预发布显示。
4. 安装后保存文件，退出所有 Rhino，再重新打开。
5. 按第 6.4 节获取许可，然后打开 GH，用 License 组件核对版本。

PackageManager 文件通常在 `%APPDATA%\McNeel\Rhinoceros\packages\8.0\` 下。包名及版本子目录以本机为准。源码构建参数 `KarambaRoot` 必须指向**直接包含 `karambaCommon.dll` 的文件夹**，而不是整个 packages 目录。GH C# 引用也可能需要调整。[官方 API 路径说明](https://scripting.karamba3d.com/1.-introduction/1.1-scripting-with-karamba3d)

### 6.4 许可激活与验证

使用 Cloud Zoo 云许可时：

1. 启动 Rhino，先不打开项目 GH。
2. 输入 `Karamba3DGetLicense`。
3. 按提示登录有授权的 Rhino 账号并获取许可；单位许可需相应团队权限。
4. 获取成功后输入 `Grasshopper`。
5. 在空白画布中放一个 Karamba3D 的 `License` 组件，将输出接到 Panel。
6. 核对版本、许可类型和状态，不应显示许可加载失败。
7. 可先运行官方随附的小示例，确认 Karamba 自身能计算。

云许可可在每次 Rhino 启动后用该命令加载。若 GH 已打开，取得许可后检查 License 组件并重算。网络许可或学校授权则按管理员提供的方式配置。[云许可官方步骤](https://manual.karamba3d.com/1-introduction/1.2-licenses/1.2.1-cloud-licenses)

工具栏出现图标仅表示组件已加载，不表示完整竹拱模型一定有可用许可。本项目的梁单元数量可能超出受限许可范围，应以 License 信息和实际错误判断。[官方许可检查](https://manual.karamba3d.com/troubleshooting/4.3.-miscellaneous-problems/licensing)

**成功标志：** GH 有 Karamba3D 分类，License 显示预期版本及有效许可，官方小示例能运行。

## 7. 安装 WebView2 Runtime

1. 关闭竹拱工作室 App。
2. 打开第 2.3 节微软下载页，选择 **Evergreen Standalone Installer → x64**。
3. 运行安装程序；已有正常版本时不必重复安装。
4. 安装后重开 App；仅在安装器要求时重启电脑。

安装的是 WebView2 **Runtime**，不是 Visual Studio 插件，也不要求用户用浏览器打开 App。[微软说明](https://learn.microsoft.com/microsoft-edge/webview2/concepts/distribution)

**成功标志：** App 显示六阶段界面，不再停留在缺少 WebView2 的提示。尚无 EXE 时先完成下一节。

## 8. 只有源码时：构建 App 和连接插件

已有完整运行包可跳过本节。构建只生成文件，不会自动替换已安装程序。

### 8.1 安装并检查开发工具

1. 安装 Node.js Windows x64，保留 npm 和加入 PATH 的选项。
2. 安装 .NET **9.0 系列 SDK x64**；只装 Runtime 不能编译。
3. 关闭旧 PowerShell，重新打开以读取更新后的 PATH。
4. 执行：

   ```powershell
   node --version
   npm.cmd --version
   dotnet --list-sdks
   dotnet --list-runtimes
   ```

5. 确认有 Node、npm 和 `9.0.xxx` SDK。执行后端 .NET 8 测试还需 `Microsoft.NETCore.App 8.0.x` 与 `Microsoft.WindowsDesktop.App 8.0.x`，缺少时补装 Desktop Runtime 8 x64。

SDK 9 与 Runtime 8 可并存。仓库的 `global.json` 选择 9.0 系列，只有 .NET 10 SDK 不一定符合要求。微软页面中应选 `Build apps - SDK`。[SDK 下载](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)

### 8.2 构建前端

1. 在前端根目录打开 PowerShell，目录中应直接存在 `Build.ps1` 和 `package.json`。
2. 按实际路径执行：

   ```powershell
   Set-Location -LiteralPath 'C:\BambooStudio\source\bamboo-studio-frontend'
   powershell -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1
   ```

3. 首次构建需联网下载 npm/NuGet 依赖。等待安装、测试、界面打包和桌面发布全部结束。
4. 成功后生成 `artifacts\desktop\BambooStudio.exe`。
5. 将整个 `artifacts\desktop` 内容放到运行目录 `runtime\App`，或者直接从生成目录启动。

App 发布目录自带 .NET 运行库，但仍需系统 WebView2 与实际计算用的 Rhino/Karamba 环境。不能仅复制 EXE。

### 8.3 构建后端

1. 先完成 Rhino、GH 和 Karamba 安装。
2. 在后端根目录执行：

   ```powershell
   Set-Location -LiteralPath 'C:\BambooStudio\source\bamboo-studio-backend'
   powershell -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1 -RhinoRoot 'C:\Program Files\Rhino 8'
   ```

3. Rhino 在其他盘时改为真实路径，例如 `-RhinoRoot 'D:\Rhino 8'`。
4. 成功后生成 `artifacts\plugin\BambooKarambaBridge.rhp`。
5. 将该 `.rhp` 放进固定的运行目录，再按第 9 节安装。

### 8.4 Karamba 不在标准安装目录时

在 GH 程序集信息、安装器记录或自己的 `%APPDATA%\McNeel\Rhinoceros\packages\8.0` 中查找 `karambaCommon.dll`。构建命令同时传入两个目录，下面最后一项必须替换：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1 -RhinoRoot 'C:\Program Files\Rhino 8' -KarambaRoot '替换为直接包含karambaCommon.dll的实际目录'
```

也可复制 `Directory.Build.local.props.example` 为 `Directory.Build.local.props`，填写 RhinoRoot、GrasshopperRoot 和 KarambaRoot。本机配置已被 Git 忽略，不要提交自己的安装路径。

**成功标志：** 前端有完整桌面发布目录，后端有 `.rhp`，构建未报缺少 SDK 或引用 DLL。

## 9. 安装 Bamboo Karamba3D Bridge

### 9.1 首次安装

1. 确认拿到本项目 **0.8.0** 的 `BambooKarambaBridge.rhp`。
2. 先放在固定位置，例如 `C:\BambooStudio\runtime\Rhino连接插件\BambooKarambaBridge.rhp`。
3. 如果来自可信交付包且 Windows 文件属性出现“解除锁定”，先解除该文件锁定；无此选项则跳过。
4. 在 Rhino 输入 `_PlugInManager`，或进入“工具 → 选项 → 插件”。
5. 点击 **Install / 安装**，选择该 `.rhp`；也可将 `.rhp` 拖入 Rhino 视口。
6. 找到 Bamboo Karamba3D / Bamboo Karamba3D Bridge，确认 Enabled 已启用，版本为 `0.8.0.0`。
7. 打开 details/详细信息，确认路径是刚才选定的文件。
8. 保存文件，**退出所有 Rhino 后重新打开一次**。

`.rhp` 安装在 Rhino 中，不是拖到 GH 画布上的电池。插件管理器可查看它注册的命令与路径。[Rhino 官方安装方法](https://www.rhino3d.com/en/docs/guides/scripts-plugins/how-to-use/)

### 9.2 已有旧插件时

1. 在插件管理器记下旧 Bamboo 插件的实际路径。
2. 保存 App 方案、Rhino 和 GH 文件。
3. 关闭 App，退出全部 Rhino，确保文件不再被占用。
4. 复制旧 `.rhp` 到备份目录，再用 0.8.0 文件替换原注册位置的旧文件。
5. 重开 Rhino，核对管理器版本和路径，再按第 10 节启动服务。

若仍显示旧版，先查实际加载路径，不要反复覆盖另一个没被注册的副本。插件 GUID 为 `15C952B1-2C56-47A9-BA63-522DF7680F77`，普通安装者不需要自行修改注册表。

**成功标志：** Rhino 能识别 `BambooKarambaStart` 和 `BambooKarambaStop`，版本与路径正确。

## 10. 第一次连接的准确顺序

1. 只开一个用于工作台的 Rhino 8 实例。
2. 云许可用户先执行 `Karamba3DGetLicense` 并确认成功。
3. 执行 `Grasshopper`，等待加载完成。
4. 必要时放置 License 组件检查许可；PackageManager 安装时也可由此确认 Karamba 程序集已加载。
5. 回到 **Rhino 命令栏**，输入：

   ```text
   BambooKarambaStart
   ```

6. 预期出现类似提示：

   ```text
   Bamboo Karamba3D 连接插件 v0.8.0 已启动，本机端口 XXXXX。
   保持 Rhino 打开，在 App 点击重新连接。
   ```

7. 保持 Rhino、GH 开着，双击 `App\BambooStudio.exe`。
8. 进入第五步“结构选型与多参数搜索”，点击“重新连接 Karamba3D”。
9. 确认已连接后，继续第 11 节实际计算。

端口 XXXXX 动态分配，不必与截图一致，也不需要手动配置。服务会在当前用户的下列位置生成连接发现文件：

```text
%LOCALAPPDATA%\BambooStudio\KarambaBridge\connection.json
```

App 自动读取它。文件含临时认证令牌，不要复制到别人的电脑，不要上传 GitHub 或公开其完整内容。

Rhino 与 App 应在同一台电脑、同一个 Windows 用户下运行，普通启动即可。初次联调建议保持 GH 窗口可见以便查看报错。

**“已连接”只说明服务可访问，不代表结构已计算成功。** 许可和模型问题仍可能在提交后出现，必须完成下一节才算联调成功。

## 11. 第一次计算与完整操作验收

### 11.1 使用演示预设

1. 首次启动可能已加载演示参数。有自己的方案时先导出或保存快照。
2. 在第一步或第五步展开 **“演示预设 · 已验证可计算显示”**，点击 **“载入演示参数”**。
3. 核对以下关键值：

   | 参数 | 演示值 |
   |---|---|
   | 场地长 / 宽 / 限高 | 17000 / 8000 / 6500 mm |
   | 端部预留 | 每端 2500 mm |
   | 跨度 / 总高 | 5600 / 4040 mm |
   | 阵列长度 / 榀距 | 12000 / 2000 mm |
   | 起拱高度 | 1800 mm |
   | 檩条 | 9 道 |
   | 主杆外径候选 | 100、150、200 mm |
   | 交叉拱倾角候选 | 20°、15°、25° |
   | 高度搜索 | 关闭 |
   | 搜索数量 | 21 个候选，以界面计划为准 |

4. 第五步确认已连接，点击计算按钮。
5. 等待状态和进度更新，不要反复提交或在计算中重启 Rhino。
6. 结束后依次选择当前构型，查看模型及数值。有候选未通过初筛时，展开说明，不要直接视为安装失败。

完整输入在后端 `examples/compare-request.json`；它是 API 请求示例，并不表示 App 有直接导入这个请求文件的按钮。普通用户使用界面的预设入口即可。

### 11.2 六阶段验收

| 阶段 | 试操作 | 应看到的成果 |
|---|---|---|
| 1 场地边界 | 调整长宽高 | 场地虚线框随参数变化 |
| 2 单榀模数 | 自动适配或手动调整 | 单榀在同一场地框内显示，尺寸关联正确 |
| 3 线性阵列 | 查看阵列和间距 | 单榀扩展为阵列，榀数与位置明确 |
| 4 檩条布置 | 调整布置输入 | 未选型草案包含檩条，几何可以更新 |
| 5 结构选型与多参数搜索 | 真实计算后选择方案 | 已求解候选有实际参数、数值和模型 |
| 5 编辑与对比 | 小幅修改后复核 | 构型不丢失，前后结果可对比，云图共用色标 |
| 6 屋面生成 | 明确选择原方案或当前方案 | 屋面可见，切换形式正常，保留初筛状态 |
| 导出 | 保存 `.3dm` 并在 Rhino 打开 | 导出模型与选中方案一致 |

### 11.3 正确理解结果状态

- “已计算但未通过初筛”可以同时有位移、应力等数值，因为求解完成与通过条件是两件事。
- 没有系统推荐时仍可能有各构型参考结果，应查看具体原因。
- 参数修改后需要复核，旧结果不能冒充新结果；没有求解的数据也不能当成零。
- 应力云图的颜色是按数值范围映射，不是安全等级。

`Limit` 未填正值时默认位移阈值为 `W/250`，演示跨度 5600 mm 对应 22.4 mm。演示 `Fc/Ft/Fb` 为 0，代表强度阈值未启用；演示屈曲检查也未开启。“通过已启用初筛项”不等于完成全部结构验算。

有效方案即使未通过初筛也可进入屋面步骤，界面会保留标记。第六步调整屋面外观不等于重新计算荷载。

## 12. 重启要求与日常开关顺序

### 12.1 安装或更新后如何重启

| 操作 | 需要关闭什么 | 完成后的顺序 |
|---|---|---|
| 安装/更新 Rhino | App、全部 Rhino，先保存 GH | 重开 Rhino；安装器要求时重启电脑 |
| 修改 .NET 模式 | 保存后退出全部 Rhino | 重开 Rhino，用 `_SystemInfo` 核对 |
| 安装/更新 Karamba | MSI 前关闭 Rhino；PackageManager 安装后也需退出 | 重开 Rhino → 获取许可 → GH → License 检查 |
| 首次安装/替换 Bamboo `.rhp` | 替换前关闭 App、全部 Rhino | Rhino → 许可 → GH → Start → App 重连 |
| 安装 WebView2 | App | 安装后重开 App |
| 安装 Node / SDK | 关闭旧构建终端 | 新开 PowerShell 检查版本 |
| 只更新 App | 保存方案并关闭 App | 完整替换发布目录后重开；后端没变时 Rhino 可保持运行 |
| 只改文档或上传 GitHub | 无需关闭 | 无需重启、无需重装 |

**退出全部 Rhino 进程才算重启 Rhino；只关 GH 或点击 App 重连不算。** 不必每一步都重启整台电脑。

### 12.2 每次日常启动

```text
打开 Rhino 8
  → 需要时获取 Karamba3D 许可
  → 打开 Grasshopper，确认 Karamba 已加载
  → Rhino 命令栏运行 BambooKarambaStart
  → 打开竹拱工作室 App
  → 第五步连接、计算、选择与编辑
  → 第六步生成屋面与导出
```

当前插件不承诺自动随 Rhino 启动服务，初次使用保持上述手动顺序。同一个 Windows 用户下只开一个 Bamboo 服务实例。

### 12.3 正常退出和重新连接

1. 先保存参数、快照或导出模型。
2. 等待任务完成，或申请取消并等待结束。
3. 关闭 App。
4. 如继续用 Rhino 但停止工作台服务，输入 `BambooKarambaStop`；正常退出 Rhino 也会停止服务。
5. Rhino 重启后重新获取许可、打开 GH、运行 Start，再让 App 重连。

服务重启会改变连接信息，旧任务不会自动续算。出现“连接已重启，请重新提交计算”时，重新提交当前输入。

## 13. 配套 GH 的使用

1. 完成 Rhino、GH 和 Karamba 环境安装。
2. 在 GH 的 File → Open 中打开后端仓库 `grasshopper\BambooStudio-0.8.0.gh`。
3. 等待 C# 组件编译完成，不要连续触发多轮搜索。
4. 若提示找不到 `karambaCommon.dll`，查看相应组件的程序集管理/Manage Assemblies 或脚本引用设置，改为本机同版 DLL 的实际路径。不同安装方式可能有不同路径。[官方路径说明](https://scripting.karamba3d.com/1.-introduction/1.1-scripting-with-karamba3d)
5. 查看各组件 Info/警告，检查必要输入与连接。
6. 第五步先计算并明确选定方案，第六步再检查屋面输出及显示组件预览。
7. 修改后另存自己的 `.gh`，保留交付原件。

App 调用连接插件内部计算核心，**不是遥控当前 GH 画布**。打开某个 GH 文件不会自动把参数同步到 App，也不必始终打开这个特定文件才能运行 App。

仅独立使用 GH 不需要启动 Bamboo App 服务，但需要 Karamba 和脚本引用正常。`grasshopper/components` 的编译检查 DLL 不是普通用户需要安装的插件。

## 14. 常见问题与处理顺序

| 现象 | 先检查 | 再处理 |
|---|---|---|
| App 提示缺少 WebView2 | Runtime x64 是否安装 | 按第 7 节安装并重开 App |
| 只复制 EXE 后 DLL 缺失 | 发布目录是否完整 | 重新解压整个 App，不随机补 DLL |
| Start 是未知命令 | `.rhp` 是否在 Rhino 注册并启用 | 第 9 节安装并重启，核对路径 |
| 插件 .NET 不兼容 | `_SystemInfo` 的运行时 | 按第 4 节使用 NETCore / .NET 8 |
| 找不到 Karamba | GH 与程序集是否加载 | 打开 GH、License 组件，核对安装目录 |
| Karamba 类型加载冲突 | MSI/YAK 是否并存，是否混用 DLL | 按原安装方式处理冲突副本后重启；不要混拷不同版本 DLL |
| Rhino 已启动，App 连接失败 | 是否同一用户、服务仍运行 | Stop/Start 后重连，不填截图中的旧端口 |
| 有 connection.json 仍连不上 | 是否是退出进程遗留文件 | 文件存在不等于服务可用，Stop/Start 重新生成 |
| 一计算就失败 | 许可、模型错误和具体日志 | 检查 License、官方小例子，再用演示预设重试 |
| 小例子能算，大模型失败 | 许可规模/功能限制 | 根据许可和错误处理，换端口不能解决 |
| 提示已有计算任务 | 旧任务是否结束 | 等待或取消，并等当前 Karamba 调用返回 |
| 服务文件被占用 | 另一 Rhino 是否开了服务 | 保留一个服务实例，不在运行中删除锁文件 |
| 仍显示 0.7.x | 实际加载路径 | 关闭全部 Rhino，替换真正注册的旧文件 |
| SDK 不匹配 | `dotnet --list-sdks` | 安装 9.0 系列 SDK，重开终端 |
| 找不到 RhinoCommon/karambaCommon | 真实安装目录 | 设置 RhinoRoot / KarambaRoot 或本机 props |
| npm 执行策略报错 | 是否调用了 npm.ps1 | 使用 `npm.cmd` 和本文构建命令，无需永久降低策略 |
| 没有系统推荐 | solved 状态与初筛理由 | 区分未求解、超限与比较条件，不因此重装 |
| 第六步屋面不显示 | 是否已明确选择有效方案、屋面开关 | 第五步确认采用对象，检查屋面开关与形式 |

保留 Rhino 命令历史与 App 原始错误，初次排查不需要关闭防火墙、开放公网端口、清空草稿或删除许可文件。

**只更新界面时：** 保存方案 → 关闭 App → 完整更新 App 发布目录 → 重开。若后端协议与插件没变，不用重装 Rhino/Karamba。仅 `.rhp` 发生变更时，按第 9.2 节更新并重启 Rhino。

## 15. 上传 GitHub

### 15.1 创建两个空仓库

| 字段 | 前端 | 后端 |
|---|---|---|
| Owner | 自己的账号或获授权组织 | 同左 |
| Repository name | `bamboo-studio-frontend` | `bamboo-studio-backend` |
| Description | 竹拱工作室前端：参数化设计界面、三维预览、方案编辑、应力云图与导出。 | 竹拱工作室后端：Rhino/Karamba3D 分析、多参数搜索与配套 GH。 |
| Visibility | 按共享范围选择 Private 或 Public | 同左 |
| Add README | Off | Off |
| Add .gitignore | No .gitignore | No .gitignore |
| Add license | 尚未决定项目许可时选 No license | 同左 |

源码已有 README 和忽略规则，创建空仓库即可。[GitHub 官方导入说明](https://docs.github.com/en/migrations/importing-source-code/using-the-command-line-to-import-source-code/adding-locally-hosted-code-to-github)

### 15.2 网页上传

1. 解压源码 ZIP。
2. 打开 GitHub 空仓库的上传已有文件入口。
3. 上传根目录内的文件及子目录，不直接上传 ZIP，不再额外套一层目录。
4. 确认 README、`.gitignore`、`.gitattributes`、锁文件均已包括；不要上传 `.git`。
5. 提交后对另一个仓库重复操作。
6. 仓库首页应直接显示 README，并可从“完整安装与联调”链接打开本文。

### 15.3 命令行上传

在相应仓库根目录运行：

```powershell
git init -b main
git add .
git status --short
git commit -m "Import Bamboo Studio 0.8.0 source"
git remote add origin https://github.com/YOUR_ACCOUNT/YOUR_REPOSITORY.git
git push -u origin main
```

替换账号和仓库名，前后端各执行一次。需要时按 Git 提示设置真实姓名及邮箱。已经有 origin 时先用 `git remote -v` 核对，不重复添加。

已经上传过旧版的人只需提交本次文档与清单更新，不用删除仓库重来。更新后的 ZIP 可用于新的完整上传。

### 15.4 给普通使用者提供运行包

维护者完成验收后，可创建与源码匹配的 Release，提供：完整 App 发布目录压缩包、对应 `.rhp`、配套 `.gh`、本指南及版本记录。这样使用者不用自己安装 Node/SDK 编译。

当前两个源码 ZIP 不包含这些编译产物；本文也不表示已替维护者发布 Release。不要分发商业 SDK、许可文件、连接令牌和个人配置。

## 16. 交接验收表与问题反馈

- [ ] 已完整解压运行包，或构建出 App 和 `.rhp`。
- [ ] Rhino 版本已记录，当前运行在 .NET 8。
- [ ] 随附 GH1 能正常打开。
- [ ] Karamba 版本和 License 状态正确。
- [ ] Bamboo 插件为 0.8.0，加载路径正确。
- [ ] Rhino 出现 Start 成功提示。
- [ ] App 显示已连接。
- [ ] 演示预设真实计算完成，候选模型与数值可以查看。
- [ ] 已试过选择方案、修改复核及应力前后对比。
- [ ] 屋面可见，导出 `.3dm` 能在 Rhino 打开。
- [ ] 保存并重开后，可以按日常流程继续使用。

失败时提供以下信息，不要只写“连接不上”：

```text
Windows 版本与架构：
Rhino 完整版本：
SystemInfo 显示的 .NET 运行时：
Grasshopper 是否正常打开：
Karamba3D 版本与 MSI/PackageManager 安装方式：
License 类型/状态（不提供密钥）：
Bamboo 插件版本与加载路径：
App 版本或源码提交号：
失败在本文第几节、第几步：
Rhino 原始报错：
App 原始报错：
是否用演示预设复现：
```

## 17. 依据与验证边界

本文已核对本机 Rhino.exe、Grasshopper.dll、RhinoCommon.dll、karambaCommon.dll 的具体版本；外部安装和许可说明依据文中官方链接。

拆仓时已完成：前端构建与 7 组回归测试、桌面 UI 回放、后端构建、33 项搜索测试、19 项适配测试和 8 个 GH 脚本编译检查，并从 ZIP 解压到独立目录重建及测试。

**这不能替代新电脑上的实际联调。** 桌面测试回放已有结果，搜索软件测试使用模拟评分；本次更新文档未启动 Rhino、重装插件或提交新结构计算。安装者以第 11 节的真实运行结果与第 16 节验收表为准。

接口细节见各仓库 `docs/API.md`，开发入口见 `docs/DEVELOPMENT.md`，拆仓验证记录见 `docs/VALIDATION.md`。本指南随两个仓库一起分发。
