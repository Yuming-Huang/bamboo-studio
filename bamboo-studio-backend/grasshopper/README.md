# 配套 Grasshopper

`BambooStudio-0.8.0.gh` 是原始 0.8.0 电池文件的改名副本，内容未改。校验值见 `../provenance.json`。用 Rhino 8 的 Grasshopper 打开，Karamba3D 应已安装并有可用许可。

`components/` 保存各电池的独立 C# 编译检查源码；`component-ports.json` 记录输入输出端口。文件名 01–06 对应主流程，07 为显示、08 为方案编辑。源文件为聚合文件，多个电池各自包含公共代码，不能合并到一个 C# 项目一起编译。

这里提供现成 GH 和可读、可编译的 C# 源码；没有提供从零重建画布布局与连线的自动打包器。修改 C# 后需同步相应 GH 脚本组件、核对端口并在 Rhino 中保存为新 GH，单独编译不会修改 `.gh`。

`search-source/` 是原 GH 工程维护的几何和搜索源码，供搜索回归测试使用。App 后端执行的是 `src/BambooKarambaBridge/Kernel.cs`，不会在运行时导入这里的源文件。修改算法必须同步两条路径，并重新做真实 Karamba3D 对照，不能只让模拟评分测试通过。

原 GH 内部历史阶段标签保持原样；前端当前阶段名称单独维护，不要把标签改名等同于计算逻辑改变。
