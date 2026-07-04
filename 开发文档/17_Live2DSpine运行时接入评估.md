---
id: dev_live2d_spine_runtime_eval
title: Live2D / Spine 运行时接入评估
type: dev
role: 程序
domain: live2d_character_animation
status: active
source_of_truth: true
related:
  - 美术文档/16_Live2D角色动画资产接入规格.md
  - 美术文档/17_Agent原生动态立绘资产接入规格.md
  - 开发文档/rules/02_Unity表现层与编辑器构建规范.md
  - 开发文档/rules/03_视觉资源系统程序开发规范.md
  - agent_status/program.md
  - agent_status/art.md
last_verified: 2026-06-14
update_rule: 修改 Agent-native DollPuppet、Live2D / Spine runtime 选择、许可门禁、导入路径、回滚策略或独立验收口径时同步本文档。
---

# Live2D / Spine 运行时接入评估

> 结论：首个 `doll_proto_0_live2d` 试点改为优先落地 `Agent-native DollPuppet`。Cubism / Spine 不再作为首版阻塞项，只作为外部导出兼容路线或 Spine JSON 实验支线。本文是运行时选择、包评估和导入门禁，不代表已经导入第三方包，也不改变当前 FormalV2 UI / ArtAcceptance 条件。

## 1. 当前工程事实

| 项 | 当前结论 |
|---|---|
| Unity 版本 | `UnityClient/ProjectSettings/ProjectVersion.txt` 为 `2022.3.60f1` |
| 当前 package 状态 | `UnityClient/Packages/manifest.json` 未引入 Cubism / Spine runtime |
| 当前命名空间冲突 | 本地脚本未发现 `namespace Live2D`、`namespace Spine`、`using Live2D` 或 `using Spine` |
| 当前动态立绘资产 | 没有 `Approved/DollsLive2D/doll_proto_0` Runtime / Prefab 交接包 |
| Prefab 查询能力 | `VisualAssetService.GetPrefab(string visualID)` 已存在，可作为首版动态立绘 Prefab 查询入口 |
| UI 技术栈 | 运行时仍使用纯 UGUI；首版优先 `RenderTexture + RawImage` |
| 当前首版路线 | `美术文档/17_Agent原生动态立绘资产接入规格.md` 定义 `DollPuppet` 为主线 |

## 2. 推荐 Runtime 决策

首选：`Agent-native DollPuppet`。

理由：

- 它以分层 PNG、rig JSON、motion JSON 和 expression JSON 为核心，agent 可生成、可 diff、可校验、可版本管理。
- 不依赖 Cubism / Spine GUI 工具即可推进首版正式动态立绘。
- 与现有程序底座兼容：仍以 `VisualID -> Prefab -> Presenter -> UGUI bridge -> 独立验收 runner` 为接入链路。
- 可以把具体动作 / 表情映射收在 `IDollDynamicPresenter` 或兼容的 `IDollLive2DPresenter`，避免 UI Controller 直接处理表现细节。

实验支线：`spine-unity` / Spine JSON。

适用条件：

- 已确认 Spine runtime 授权、Unity 兼容性和回滚策略。
- 需要验证 agent 生成公开 Spine JSON skeleton + atlas 的可行性。
- 只作为 `DollPuppet` 之后的兼容 / 对照实验，不作为首版必过项。

外部兼容支线：`Live2D Cubism SDK for Unity` 或 Spine Editor 正式导出。

适用条件：

- 项目获得 Cubism / Spine 工具链、外部 rigger 或已完成 runtime 包。
- SDK 来源、版本、许可、绑定工具版本和回滚 diff 已确认。
- 需要接入行业标准 Cubism / Spine 资产，而不是项目自有 `DollPuppet` 包。

## 3. 版本与许可门禁

正式导入前必须补齐以下人工确认项：

| 门禁 | 要求 |
|---|---|
| SDK 来源 | 只从官方发布渠道或已审查的项目 vendor 路径取得，不从聊天记录、网盘散包或未知镜像导入 |
| 版本 | 记录精确版本号、发布日期、下载来源和校验信息；版本必须明确支持 Unity 2022 LTS 或在本工程验证通过 |
| 许可 | 记录 Cubism / Spine 的使用条款、商业授权限制、二次分发限制和团队账号归属；Spine runtime 集成需要按 Spine Editor License / Runtimes License 复核 |
| 绑定工具 | Cubism / Spine 外部导出路线必须记录 Cubism Editor 或 Spine Editor 版本；DollPuppet 路线记录 JSON schema 版本和 importer 版本 |
| 回滚准备 | 导入前必须有干净 diff 或独立分支，且列出将新增 / 修改的 package、Assets、ProjectSettings 路径 |

未完成这些门禁前，不允许修改 `UnityClient/Packages/manifest.json` 或导入第三方 package。

`DollPuppet` 主线不需要引入第三方 package 才能开始。若后续启用 Unity 2D Animation / SpriteSkin package，必须作为独立程序任务评估和导入。

## 4. 导入路径建议

### DollPuppet

推荐资产路径：

```text
UnityClient/Assets/Art/Approved/DollPuppets/doll_proto_0/
  SourceRefs/
  Textures/
  Rig/
  Motions/
  Expressions/
  Prefabs/
```

推荐程序路径：

```text
UnityClient/Assets/Scripts/Visuals/DollPuppets/
UnityClient/Assets/Scripts/Editor/DollPuppets/
UnityClient/Assets/Scripts/Tests/DollPuppet*SmokeTest.cs
```

导入规则：

- `DollPuppet_doll_proto_0.prefab` 只保存表现结构、图层、参数、相机 / render target 支持组件或挂点，不保存玩法状态。
- `rig.json` / `motion.json` / `expression.json` 必须可解析并可通过 schema / smoke 验证。
- 动态 Prefab 缺失时必须回退 `doll_proto_0_stand`。

### Cubism

推荐导入方式：

```text
UnityClient/Assets/Live2D/Cubism/
```

推荐试点资产路径：

```text
UnityClient/Assets/Art/Approved/DollsLive2D/doll_proto_0/
  Runtime/
  Prefabs/
  SourceRefs/
```

导入规则：

- 如果 SDK 以 `.unitypackage` 方式提供，导入内容应集中在 `Assets/Live2D/Cubism` 或官方默认目录，不混入 `Assets/Scripts` 的业务代码目录。
- 如果使用 UPM / git package 方式，必须在专门任务中修改 `UnityClient/Packages/manifest.json` 和 `packages-lock.json`，并记录回滚 diff。
- `DollLive2D_doll_proto_0.prefab` 只保存表现结构、Cubism 组件、相机 / render target 支持组件或挂点，不保存 HP、SAN、金币、好感、背包等玩法状态。

### Spine

推荐导入方式：

```text
UnityClient/Assets/Spine/
```

或使用已审查的 UPM / git package。

导入前必须确认：

- Spine Editor 版本与 runtime 版本匹配。
- 导出数据格式、贴图图集和 shader 在 Unity 2022.3 下可编译；若采用 agent 生成 JSON skeleton，必须先用隔离样例证明 spine-unity 可加载。
- Runtime 许可允许项目使用和分发。

## 5. 工程影响

| 风险 | 控制 |
|---|---|
| Package 污染或难回滚 | 只在独立任务 / 分支导入，提交前列出新增目录、manifest 和 ProjectSettings diff |
| Batchmode 编译失败 | 导入后必须跑 C# build、Unity batchmode smoke 或等价编译检查 |
| UGUI 射线 / Canvas 层级被破坏 | 首版使用独立角色相机渲染到 `RenderTexture`，UGUI 用 `RawImage` 展示 |
| FormalV2 ArtAcceptance 被误影响 | Live2D 验收独立，不加入当前 FormalV2 必过项 |
| Prefab 缺失阻断主流程 | `DollLive2DPresenter` 必须先加载 fallback sprite；缺包、缺 prefab、缺 motion 时不阻断 UI |
| 玩法状态泄漏进 Prefab | Presenter 只接收状态快照和播放意图，Prefab 不保存领域状态 |
| 自研路线退化为粗糙小 rig | 使用正式 DollPuppet schema、Approved 包、Importer、Prefab 和独立多帧验收作为完成门槛 |

## 6. 回滚策略

导入任务必须能按以下顺序回滚：

1. 关闭所有引用新 runtime 的场景 / prefab 改动，恢复静态 fallback。
2. 删除导入的 SDK 目录及对应 `.meta`。
3. 如果修改过 `UnityClient/Packages/manifest.json` 或 `packages-lock.json`，恢复导入前版本。
4. 删除或回滚新增的 Live2D / Spine demo scenes、sample assets、editor menu 和 generated settings。
5. 运行文档校验、C# build 或 Unity batchmode 编译检查，确认工程回到静态 fallback 可运行状态。

## 7. 后续实现顺序

### 7.1 已落地：DollPuppet JSON 契约校验

`DP-DATA-01` 已新增 `DollPuppetJsonContract` 数据契约和静态 validator：

- 程序入口：`UnityClient/Assets/Scripts/Visuals/DollPuppetJsonContract.cs`
- 验证入口：`DollPuppetJsonValidator.ValidateRigJson(...)`、`ValidateMeshJson(...)`、`ValidateMotionJson(...)`、`ValidateExpressionJson(...)`
- focused smoke：`UnityClient/Assets/Scripts/Tests/DollPuppetJsonContractSmokeTest.cs`

当前契约覆盖：

- `rig.json`：`schema`、`dollId`、`visualId`、`fallbackSpriteId`、`canvas`、`layers`、`parameters`。
- `mesh.json`：`schema`、`meshes`、`vertices`、`triangles`。
- `motion.json`：`schema`、`motionId`、`loop`、`duration`、`tracks`、`keys`。
- `expression.json`：`schema`、`expressionId`、`overrides`。

Validator 使用 `Newtonsoft.Json` 解析真实 JSON，不使用字符串拼接判断；并拒绝在 DollPuppet JSON 中出现 `hp`、`san`、`gold`、`inventory`、`bond` 等玩法状态字段。`san_stress` 仍作为表现参数允许，由 Presenter 消费表现意图，不参与玩法计算。

该行只建立数据契约和静态校验，不生成 Prefab、不导入第三方 runtime、不修改 `VisualAssetRegistry` 数据结构，也不声明动态立绘资产已交付。

验证证据：`dotnet build UnityClient/Assembly-CSharp.csproj --no-restore` 通过；临时 .NET runner 引用同一份源码验证 rig / motion / expression 示例 JSON 均可解析并通过 validator；`Validate-Docs.ps1` 通过。

### 7.2 已落地：DollDynamic 命名兼容层

`DP-PROG-01` 已将首版动态立绘程序合同从 `Live2D` 语义抽出为通用 `DollDynamic` / `DollPuppet` 命名层：

- `DollDynamicVisualResolver` 默认 `ModelKind=DollPuppet`，继续使用 `doll_proto_0_live2d` / `doll_proto_0_stand` 作为首个试点 ID 与 fallback。
- `DollDynamicVisualLoadResult` 返回 `DynamicPrefab`、`StaticFallback` 或 `MissingFallback`，并保留 `dynamic_prefab_missing`、`dynamic_runtime_missing`、`required_motion_missing:<MotionID>` 等降级原因。
- `IDollDynamicPresenter`、`DollDynamicPresentationIntent` 和 `DollDynamicPresenterDriver` 定义与 Cubism / Spine 无关的 motion / expression / core glow / SAN stress 表现意图。
- 现有 `DollLive2DVisualResolver`、`IDollLive2DPresenter`、`DollLive2DMotionIDs`、`DollLive2DExpressionIDs` 和 `DollLive2DPresenterDriver` 保留为兼容包装，避免旧 runner / bridge / test 断链。
- `DollDynamicVisualResolverSmokeTest` 覆盖 DollPuppet 默认命名、动态 Prefab 命中、静态 fallback、缺 Prefab、缺 runtime、缺 motion、缺 fallback 和旧 `DollLive2D` 命名兼容。

该行不导入 Cubism / Spine package，不生成 Approved DollPuppet 包、不生成 Prefab、不修改正式 UI，也不声明动态立绘运行态截图已通过。

验证证据：`dotnet build UnityClient/Assembly-CSharp.csproj --no-restore` 通过，0 warning / 0 error；`Validate-Docs.ps1` 通过。

### 7.3 后续实现顺序

1. `DP-PROG-02`：在真实 Approved 包缺失时先实现 importer / runtime / prefab builder 的可测骨架或受限 fixture 路径，明确不声明 `doll_proto_0_live2d` 已交付。
2. `DP-UI-01`：复用或升级 UGUI `RenderTexture + RawImage` 显示桥，验证不遮挡按钮和射线。
3. `DP-VAL-01`：将独立 Live2D 多帧截图 runner 升级为 DollPuppet / DynamicDoll 验收 runner。
4. Cubism / Spine runtime 导入只能在许可、版本、绑定包齐备后单独执行。

## 8. 已落地的最小加载合同

`L2D-PROG-02` 已补最小程序合同：

- `VisualAssetService.TryGetPrefab(string visualID, out GameObject prefab)` 用于区分真实 prefab 命中和 `MissingPrefab` 降级。
- `DollLive2DVisualResolver` 固定首个试点 ID：`doll_proto_0_live2d` + `doll_proto_0_stand`。
- `DollLive2DVisualLoadResult` 返回 `DynamicPrefab`、`StaticFallback` 或 `MissingFallback`，并记录 `FallbackReason`。
- 支持 `runtime_package_missing`、`dynamic_prefab_missing`、`required_motion_missing:<MotionID>` 等降级原因。
- `DollLive2DVisualResolverSmokeTest.Run` 覆盖动态 prefab 命中、prefab 缺失、runtime 缺失、motion 缺失和 fallback sprite 缺失。

当前证据：`dotnet build UnityClient/Assembly-CSharp.csproj --no-restore` 通过；Unity MCP 未发现可用 Unity 会话，运行态 smoke 记录为 `validation_limited:UnityMCPNotDetected`。

## 9. 已落地的 Presenter 边界

`L2D-PROG-03` 已补不依赖 Cubism / Spine runtime 的 Presenter 边界：

- `IDollLive2DPresenter` 定义 `PlayIdle`、`PlayLowSanIdle`、`PlayRepairReact`、`PlayHitReact`、`SetExpression`、`SetCoreGlow` 和 `SetSanStress`。
- `DollLive2DPresentationIntent` 只表达表现意图，不携带 HP、SAN、金币、背包、好感或其他玩法状态。
- `DollLive2DPresenterDriver.Apply(...)` 负责把意图映射为 motion / expression / glow / stress 调用；优先级为 `hit_react > repair_react > low_san_idle > idle`。
- `FallbackDollLive2DPresenter` 只绑定 fallback sprite、记录当前 motion / expression / 参数值，不计算玩法规则。
- `DollLive2DPresenterSmokeTest.Run` 覆盖 fallback sprite 绑定、低 SAN 意图、维护反馈意图、受击优先级和参数 clamp。

当前证据：`dotnet build UnityClient/Assembly-CSharp.csproj --no-restore` 通过；Unity MCP 未发现可用 Unity 会话，运行态 smoke 记录为 `validation_limited:UnityMCPNotDetected`。

## 10. 已落地的 UGUI 显示桥

`L2D-UI-01` 已补独立 UGUI 桥接组件：

- `DollLive2DUGUIBridge` 管理 `RawImage` 动态显示层和 `Image` fallback 显示层。
- `AttachRenderCamera(Camera, width, height, depth)` 创建受控 `RenderTexture` 并赋给角色相机和 `RawImage`。
- `ShowDynamic(RenderTexture)` / `ShowFallback(Sprite)` / `ClearDynamic()` 明确动态与静态显示切换。
- 桥接组件自动将 `RawImage` / fallback `Image` 的 `raycastTarget=false`，避免遮挡核心按钮。
- `DollLive2DUGUIBridgeSmokeTest.Run` 覆盖 fallback sprite、动态 texture、清理动态 texture 和 render camera 绑定。

当前证据：`dotnet build UnityClient/Assembly-CSharp.csproj --no-restore` 通过；Unity MCP 未发现可用 Unity 会话，运行态 smoke 记录为 `validation_limited:UnityMCPNotDetected`。该桥接尚未挂入 `workshop_main` 或 `doll_room` 的正式运行时界面。

## 11. 当前结论

### 11.1 独立 Live2D 验收 runner

`L2D-VAL-01` 已新增独立验收入口，不复用 FormalV2 ArtAcceptance 的触发文件、输出目录或通过 / 失败条件。

运行入口：

```text
UnityClient/Logs/.live2d_acceptance_trigger = RUN_LIVE2D_ACCEPTANCE
```

Editor / batchmode 入口：

```text
Tools/P3 Art/Run Live2D Acceptance
Live2DAcceptanceEditorDaemon.RunFromBatchmode
```

输出目录：

```text
UnityClient/Logs/Live2DAcceptance/latest/
  report.json
  notes.txt
  screenshots/
    idle_0s.png
    idle_1s.png
    expression_2s.png
    low_san_idle_3s.png
    repair_react.png
    hit_react.png
    fallback.png
```

报告状态口径：

- `PASSED` 仅表示动态 Prefab 存在，且存在可接收 motion / expression intent 的 `IDollLive2DPresenter`，并完成截图。
- `FALLBACK_ONLY` 表示 runner 正常运行但只捕获到静态 fallback；当前没有真实 Live2D 动态验收通过。
- `FAILED` 表示截图、fallback 或报告输出失败。

当前工程事实：Cubism / Spine runtime 仍未导入，`doll_proto_0_live2d` Prefab 仍未交付；因此即使补跑 runner，预期状态也应是 `FALLBACK_ONLY` 或受限验证，而不是动态资产通过。该 runner 不修改 `screen_layouts.json`、Approved 资源、Manifest、Registry、`UnityClient/Packages/manifest.json` 或 FormalV2 ArtAcceptance 条件。

### 11.2 当前结论

- 推荐路线：Agent-native DollPuppet first。
- 实验路线：Spine JSON，需授权和 runtime 兼容性验证。
- 外部兼容路线：Cubism / Spine Editor 导出，需工具链或外部 rigger 交付。
- 当前不导入任何第三方包，不修改 `UnityClient/Packages/manifest.json`。
- 当前程序侧已完成最小 fallback 加载合同、Presenter 接口、独立 UGUI 显示桥，以及 `DollDynamic*` / `DollPuppet*` 命名兼容层。
- 当前仍缺 Approved DollPuppet 包、Importer / Prefab 运行态生成证据、正式界面桥接证据和 `DollPuppetAcceptance` 多帧截图报告；这些缺口不能用静态 fallback、触发文件或编译通过替代。
- 当前美术侧仍需要正式 masks、分层候选、表情候选、人工清理后的层图和 DollPuppet JSON 包。
