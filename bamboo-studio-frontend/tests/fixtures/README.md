# 测试数据

`recorded-karamba-preset.json` 是现有 0.8.0 演示预设的 Karamba3D 计算结果记录，包含 21 个候选；仅去掉任务 ID 等外围任务字段，保留原始 result。单元测试和 `--ui-test` 回放读取它，不会在正式运行中充当计算后端。

`synthetic-export.json` 是为参数导出测试编写的微型假数据，不是结构计算结果。

回放通过表示界面能处理已有结果，不表示在当前机器完成了新的 Karamba3D 求解。正式运行只连接本机 Rhino 插件。
