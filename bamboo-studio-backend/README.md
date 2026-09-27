# 竹拱工作室 · 后端

当前 0.8.0 的 Rhino/Karamba3D 连接服务、参数校验、多参数搜索与计算结果封装。配套前端仓库名称为 `bamboo-studio-frontend`。

后端以 Rhino 插件运行，真实力学求解调用 Karamba3D。它不是独立云服务器，也不是自编有限元求解器。仓库保留原始配套 GH 文件及可读 C# 源码。

## 分工

```text
前端桌面程序
  → 本机 HTTP + 临时认证令牌
  → LoopbackServer / Jobs
  → Input 参数检查
  → Solver 适配与候选搜索
  → Kernel 中的 Karamba3D 调用
  → 结果、诊断、构件应力与实际参数返回前端
```

生产源码 `src/BambooKarambaBridge/*.cs` 从当前后端逐字节复制；拆仓没有修改结构算法。源文件校验值见 `provenance.json`。

## 构建条件

- Windows x64，.NET 9 SDK（本次验证 9.0.316）。插件目标框架为 `net8.0-windows`。
- 本机 Rhino 8，含 `System/netcore/RhinoCommon.dll`。
- Grasshopper 的 `Grasshopper.dll`、`GH_IO.dll`。
- Karamba3D 的 `karambaCommon.dll`。本次编译对照安装版本 3.1.51222.0；其他版本需单独做兼容验证。
- 执行真实计算时需可用的 Karamba3D 许可。

Rhino/Karamba3D 的商业程序、许可和 DLL 不在本仓库内，请在构建电脑上安装。前端用户也需要在本机运行这一后端；上传代码到 GitHub 不会提供在线计算服务。

## 构建插件

在仓库根目录打开 PowerShell。默认查找 `C:\Program Files\Rhino 8`：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1
```

如果安装在其他位置，例如 `D:\Rhino 8`：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Build.ps1 -RhinoRoot "D:\Rhino 8"
```

Karamba3D 如果不在 Rhino 的 `Plug-ins/Karamba` 下，再传 `-KarambaRoot "实际安装文件夹"`。也可将 `Directory.Build.local.props.example` 复制为 `Directory.Build.local.props`，填写本机路径；后者已被 Git 忽略。

产物为 `artifacts/plugin/BambooKarambaBridge.rhp`。脚本只生成文件，不覆盖电脑上已安装的插件。Rhino 自身与 Karamba3D DLL 引用设置为不复制，不应把它们加入发布目录。

## 加载与联调

1. 保留原插件备份。若电脑已装当前 0.8.0，单纯上传仓库不需要换装插件。
2. 如需要测试新构建，在 Rhino 插件管理器中安装生成的 `.rhp`。同一 GUID 的插件只使用一份；已加载旧版本时需退出 Rhino 后切换文件，再重新启动。
3. 打开 Rhino 8，运行 `Grasshopper` 并确认 Karamba3D 可用。
4. 运行 `BambooKarambaStart`，等待本机服务启动提示。
5. 打开前端 App，点击重新连接，然后提交演示预设计算。
6. 需要停止服务时运行 `BambooKarambaStop`。

插件 GUID 为 `15C952B1-2C56-47A9-BA63-522DF7680F77`。首次启动时端口动态分配，不要硬编码截图中的端口。服务只监听 `127.0.0.1`，发现文件位于 `%LOCALAPPDATA%\BambooStudio\KarambaBridge\connection.json`。详见 [接口契约](docs/API.md)。

App 计算直接调用插件内部的核心，不依赖正在打开哪个 GH 画布。配套 GH 是同版本独立使用和研究核对的入口。

## 目录

| 路径 | 用途 |
|---|---|
| `src/BambooKarambaBridge/Plugin.cs` | Rhino 命令、程序集加载、服务生命周期 |
| `src/BambooKarambaBridge/LoopbackServer.cs` | 本机 HTTP 与认证 |
| `src/BambooKarambaBridge/Jobs.cs` | 任务状态、排队与取消 |
| `src/BambooKarambaBridge/Input.cs` | 参数白名单、范围、请求指纹 |
| `src/BambooKarambaBridge/Solver.cs` | App 参数到核心的适配、结果序列化 |
| `src/BambooKarambaBridge/Kernel.cs` | 几何、计算模型、Karamba3D 调用与搜索 |
| `grasshopper/BambooStudio-0.8.0.gh` | 原版 GH 的内容不变副本 |
| `grasshopper/components/` | 各脚本的独立编译检查源码 |
| `grasshopper/search-source/` | GH 搜索与几何的维护源码及测试入口 |
| `examples/compare-request.json` | 与前端演示预设一致的完整请求示例 |
| `tests/Search/` | 搜索逻辑测试；评分使用模拟器 |
| `tests/Adapter/` | 生产 Input 和序列化适配检查 |

GH 源码与画布同步方式见 [grasshopper/README.md](grasshopper/README.md)，不要把编译检查 DLL 当成可安装电池。

## 测试

只验证搜索软件逻辑，不需要 Rhino SDK：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Test.ps1 -CoreOnly
```

需要 .NET 8 运行时执行测试程序；装有 Rhino 的开发环境通常已有，若提示缺少运行时需补齐。

验证插件编译、生产适配与全部 GH 脚本：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Test.ps1 -RhinoRoot "D:\Rhino 8" -CheckGrasshopper
```

测试脚本不会启动 Rhino 或提交真实 Karamba3D 任务。搜索测试覆盖 33 项，适配测试覆盖 19 项；它们用于软件回归，不能替代结构求解与工程验算。报告生成在 `artifacts/test-results/`。

## 阈值与结果含义

“满足设定阈值”是满足本次输入启用的初筛条件，不是 Karamba3D 自动给出的规范合格证。默认位移上限在 `Limit` 未指定正值时取跨度 `W/250`；强度与屈曲按用户提供的材料阈值和启用状态评估。未启用的检查不能标成通过。

`solved`、`pass` 和各构型 `feasible` 是不同状态；计算成功仍可能不满足初筛或比较范围。参数、单位与状态说明见 [API.md](docs/API.md)。新增构型及算法修改入口见 [DEVELOPMENT.md](docs/DEVELOPMENT.md)。

## 上传 GitHub

见 [上传步骤](docs/UPLOAD.md)。不要上传本机发现文件、认证令牌、商业 SDK、安装目录、`bin/obj` 或临时计算记录。忽略规则已配置。

项目自身许可证尚未指定，见 [RIGHTS.md](RIGHTS.md)。
