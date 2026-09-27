# 前后端接口契约 · 0.8.0

两仓库各保留一份本文。接口变更时同步更新两份，并同步生产代码和测试；接口 schema 目前为 `bamboo-local/v1`。

## 通信边界

WebView2 页面请求由前端桌面宿主转发。真实后端运行在本机 Rhino 进程中，监听 `http://127.0.0.1:<动态端口>/`。

插件启动后生成 `%LOCALAPPDATA%\BambooStudio\KarambaBridge\connection.json`，包含 `url`、`token` 等字段。客户端每次请求读取发现信息，发送 `X-Bamboo-Token` 头。令牌随服务启动变化，禁止作为代码或示例数据提交。

服务拒绝带 `Origin` 的请求，不提供浏览器直接跨域调用入口。普通网页不能绕过桌面宿主直连，也不应为了部署网页将服务地址改成 `0.0.0.0`。

## HTTP 接口

| 方法 | 路径 | 输入 / 返回 |
|---|---|---|
| GET | `/api/status` | 连接、引擎版本、插件版本和忙碌状态 |
| POST | `/api/jobs` | 创建 compare 或 single 任务，成功为 202 和 `{ "id": "..." }` |
| GET | `/api/jobs/{id}` | 任务进度、错误和完成结果 |
| POST | `/api/jobs/{id}/cancel` | 申请取消；当前 Karamba 调用返回后停止 |

状态接口可返回 `connected: true`，但每个模型实际提交时才验证相应许可和模型能否求解，因此“连接成功”不等于“本次计算成功”。

任务状态为 `queued`、`running`、`complete`、`failed`、`cancelled`。进度字段为 `done`、`total`、`message`；仅 `complete` 时提供 `result`。同一时间只接受一个活动任务，同时提交返回 409。任务记录最多保留 8 个，重启插件会清空。

其他常见状态：400 为参数无效，403 为认证失败，404 为任务不存在，413 为请求过大。当前 JSON 请求上限 32768 字节。桌面宿主找不到后端时还会生成 503 错误。

## 请求与参数

```json
{
  "schema": "bamboo-local/v1",
  "mode": "compare",
  "parameters": {}
}
```

这里的空 `parameters` 仅展示信封格式，不能直接用于求解。可运行的完整输入位于后端 `examples/compare-request.json`。

`compare` 比较候选构型并搜索参数；`single` 复核当前选中的具体参数，不重新执行跨构型搜索。编辑单一方案时，传递其完整实际参数，不能把原搜索输入当成选中方案的参数。

| 参数组 | 常用字段 | 单位 / 说明 |
|---|---|---|
| 场地 | `SiteL`、`SiteW`、`SiteH`、`SiteEnd` | mm，场地尺寸及端部预留 |
| 模数 | `W`、`H`、`LegH`、`Foot`、`LegAngle` | 长度 mm，角度 °，Foot 为枚举 |
| 阵列 | `L`、`S`、`End`、`Axis` | 长度与间距 mm，当前优化不改变固定榀距 |
| 檩条 | `PMode`、`PurlinCount`、`Pmax` | 道数无单位；Pmax 为 mm |
| 截面 | `ArchD/T`、`PurlinD/T`、`PointLowerD/T`、`PointWebD/T` | 字段实际分别为 ArchD、ArchT 等，均为 mm |
| 材料 | `E`、`G`、`Nu`、`Density`、`Fc/Ft/Fb` | 刚度和强度 MPa，密度 kg/m³，泊松比无量纲 |
| 荷载 | `RoofG`、`RoofQ`、`W0`、`SnowQ` | kN/m²，其余风向及系数按输入定义 |
| 初筛 | `Limit`、`Buckle`、`AreaTol` | 位移 mm；屈曲开关；屋面面积比较容差比例 |
| 搜索 | `HeightSearch`、`Hmin/Hmax`、`Samples`、`Diameters`、`MaxCandidates` | 高度系数、样本数量、直径列表、候选上限 |
| 构型专属 | `CrossAngles`、`TrussDepths`、`PanelCounts`、`PointEs`、`PointSlopes`、`PointFootGaps`、`PointPanelCounts` | 列表分别用于交叉角度、桁高、分格、外挑、坡度及拱脚间隙搜索 |

权威参数白名单和数值范围在后端 `Input.cs`。当前注册 ID 为 `0=单圆拱`、`1=单尖拱`、`2=交叉拱`、`4=复合圆拱`、`5=复合尖拱`。ID 不连续，应保留已有映射，不要根据数组下标推算构型。

## 结果与判定

结果 schema 为 `bamboo-local-result/v1`，引擎标识 `engineId: "karamba3d"`。当前方案在 `C`；搜索模式另外返回 `types`、`candidates`、`baseline`、`B`、`relativeBest`、`recommendation` 和 `search` 等字段。

| 字段 | 含义 |
|---|---|
| `parameters` | 该层结果对应的参数；候选结果内的 parameters 是候选实际参数 |
| `solved` | 求解是否完成，有没有可用数值 |
| `pass` | 已启用的初筛项是否通过 |
| `types[].feasible` | 搜索完成、类型内候选资格和初筛共同判定 |
| `types[].result` | 可行的本型代表结果；可能为 null |
| `types[].reference` | 已计算的参考结果，未通过初筛时仍可能有值 |
| `B` | 满足搜索比较条件的推荐；可能为 null |
| `relativeBest` | 无完整推荐时可用于比较的相对优选；不能当作通过验算 |
| `displacementMm`、`stressMPa`、`massKg` | mm、MPa、kg；未求解值为 null，不能显示为 0 |
| `members` | 构件几何与应力数据，用于三维云图 |

位移上限是用户指定的正 `Limit`，否则后端取 `W/250`。材料强度是否启用由输入和核心条件决定；屈曲诊断受 `Buckle` 控制。`AreaTol` 等比较条件不是材料强度标准。前后应力图共用色标，也不等于安全等级着色。

搜索取消、失败或输入变化后不能继续把旧结果标成新计算。几何预览、计算状态与进入屋面步骤的资格分开处理：未通过初筛仍可生成外观，但需保留明确标记。

## 仅由前端处理的接口

`POST /api/export-rhino` 和 `POST /api/save-image` 由桌面宿主处理，不属于 Rhino 计算服务。参数 JSON 保存也属于前端文件操作。不要把文件对话框逻辑移入计算插件。

## 单位与验收

API 采用 mm、MPa、kN、kg；内核与 Karamba3D 之间负责单位转换。修改转换逻辑时应与 GH 在相同参数、材料、荷载、支承和离散条件下对照。记录回放及模拟测试不证明当前机器完成了真实求解。
