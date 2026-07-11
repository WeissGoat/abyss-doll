---
id: dev_19_unity_mcp_validation_orchestration
title: Unity MCP 验收编排层设计
type: dev
role: 程序
domain: test_automation
status: active
source_of_truth: true
related:
  - 开发文档/README.md
  - 开发文档/00_程序开发大纲.md
  - 开发文档/14_Unity运行时美术自动验收方案.md
  - 开发文档/15_P0配置Validator与自动验收底座需求.md
  - tools/agent/README.md
  - 知识库/views/program.md
last_verified: 2026-07-12
update_rule: 修改 P3 validation Skill、Unity MCP 自定义工具、Validation Profile、证据契约、状态判定或 MCP 默认验收入口时同步本文档。
---

# Unity MCP 验收编排层设计

> 定位：本文定义 P3 如何使用 Codex 项目 Skill、现有 PowerShell / Python 自动化脚本和 Unity MCP，形成一个可追踪、可轮询、可恢复、不会误报完成的统一验收编排层。
>
> 本文不重写 `AutoTestDaemon`、`ArtAcceptanceRunner`、`T0ValidationFinalCaptureRunner` 或 `Invoke-P0Validation.ps1` 已有业务逻辑；它定义这些能力怎样被 MCP-first 工作流调用、关联和汇总。

## 1. 背景

项目当前已经具备以下验收能力：

- PowerShell / Python 负责配置同步、JSON / Manifest / UI Spec 静态校验、批量扫描和报告聚合。
- `AutoTestDaemon` 负责执行项目现有的静态 `SmokeTest.Run()` 体系。
- `ArtAcceptanceRunner` 负责运行时界面、VisualID、UI 层级和截图验收。
- `T0ValidationFinalCaptureRunner` 负责 T0 语义节点和固定截图集。
- `Invoke-P0Validation.ps1` 负责 P0 配置、Smoke、UI 和 ArtAcceptance 摘要的统一入口。
- Unity MCP v10.0.0 已连接项目 Unity 2022.3.60f1 实例，可以读取 Editor 状态、Console、场景、测试资源并控制 PlayMode。

当前主要问题不是缺少 Runner，而是 Unity 交互仍具有明显黑盒特征：

1. 外部智能体依赖写触发文件并轮询报告，不知道 Unity 是否已消费命令。
2. 编译、Domain Reload、PlayMode、Console 和截图生成之间缺少统一可观测状态。
3. `latest` 报告可能属于旧代码、旧配置或旧 UI，容易被误当成本轮证据。
4. 自动化通过、Owner 自验和主职能外部验收容易被混写成一个“完成”。
5. Unity MCP 当前开放的通用工具面过宽，不适合作为项目验收的稳定业务接口。

## 2. 目标

### 2.1 核心目标

建立一个 MCP-first、脚本与 Unity 各司其职的验收编排层：

```text
p3-validation Codex Skill
  -> 确定性脚本执行面
  -> Unity MCP 执行面
  -> 统一证据契约
  -> 唯一确定性总判定
  -> 受 ClaimCeiling 限制的用户回复
```

完成后，智能体日常验证不再需要手工完成以下工作：

- 手写 `.test_trigger`、`.art_acceptance_trigger`。
- 反复切回 Unity 确认编译是否开始。
- 人工查看 PlayMode 是否进入或退出。
- 手工筛选本轮新增 Console error。
- 在多个 `latest` 目录中判断哪个报告属于本轮。
- 手工寻找本轮截图、报告和语义失败节点。

### 2.2 成功标准

- 首次返回 Unity 当前状态不超过 2 秒。
- 长任务能持续返回步骤、进度和阻塞原因，不需要人工读 Console。
- 每个证据都能追溯到唯一 `RunID`、来源 Runner 和输入指纹。
- `Blocked`、`Limited`、旧报告或缺失截图误报为 `Passed` 的次数为 0。
- Domain Reload 后 Unity 验收 Job 能恢复或诚实失败。
- 自动化报告不能越过项目外部验收门禁声明功能或美术封板完成。

## 3. 范围外

首轮明确不做：

- 不修改玩法规则、领域服务或正式玩家流程。
- 不改变纯 UGUI 运行时 UI 约束。
- 不使用 MCP `manage_ui` 引入 UI Toolkit、UXML、USS 或 `UIDocument`。
- 不接管 AI 出图、Manifest、Approved 筛选或 Visual V2 生产流程。
- 不把全部静态 Smoke 迁移到 NUnit。
- 不建设远程 MCP CI 服务。
- 不自动批准主策、主程、主美或游戏导演验收。
- 不在首轮删除现有 Runner、PowerShell 自动化或文件触发入口。
- 不允许 Unity MCP 自定义工具启动 PowerShell、Python 或任意外部进程。

## 4. 总体架构

### 4.1 一个总入口、两个执行面

```text
用户 / 实现 Owner
  -> p3-validation Skill
      -> 选择 Validation Profile
      -> 创建 RunID 与 EvidenceRoot
      -> 执行静态验证子流程
      -> 执行 p3_run_unity_profile
      -> Merge-P3ValidationEvidence.ps1
      -> validation-summary.json / .md
      -> 按 ClaimCeiling 回复
```

确定性脚本执行面负责：

- `Sync-Configs.ps1`。
- JSON、Manifest、UI Spec、文档和工作区校验。
- 文件到文件的确定性转换和批量扫描。
- CI 可重复执行的无 Unity 检查。
- 证据索引和最终报告的确定性合并。

Unity MCP 执行面负责：

- Unity 实例发现和固定路由。
- Editor readiness、编译、Domain Reload、AssetDatabase、PlayMode 状态。
- Unity Console baseline 和 delta。
- 项目 Smoke、ArtAcceptance、T0 Capture Runner。
- 运行时截图、UI / Registry snapshot 和原始报告快照。
- Unity 长任务的轮询、取消和 Editor 状态恢复。

### 4.2 单一事实原则

允许脚本和 MCP 分别产生原始证据，但禁止：

- 两边各自维护一份总步骤状态表。
- 两边各自输出整个切片的最终 `Passed`。
- MCP 重新实现已有静态 Validator 算法。
- PowerShell 通过旧 `latest` 推测 Unity 本轮是否执行。

最终总判定只由确定性合并器输出。

## 5. 组件设计

### 5.1 `p3-validation` Codex Skill

职责：

1. 读取项目状态、相关事实来源和 Validation Profile。
2. 解析用户目标，选择允许的 Profile，不临时拼装任意命令。
3. 读取 `mcpforunity://instances` 并固定精确 Unity 实例 ID。
4. 创建统一 `RunID` 和证据根目录。
5. 调用静态脚本执行面。
6. 调用 Unity MCP 子编排器。
7. 调用确定性证据合并器。
8. 根据 `ClaimCeiling` 组织最终回复。

Skill 不负责：

- 在自然语言中自行决定业务测试通过。
- 修改 Runner 生成的原始报告。
- 把 warning、blocked 或 limited 隐藏成成功。
- 在报告要求外自动扩大验证范围。

### 5.2 静态验证子流程

静态子流程继续复用项目现有脚本。每个步骤必须输出统一的 `step-result@1`，或由轻量适配脚本把旧输出转换为该格式。

首轮步骤注册表：

| StepID | 现有能力 | 说明 |
|---|---|---|
| `config_sync` | `tools/config/Sync-Configs.ps1 -Clean` | 配置源同步。 |
| `config_static_validate` | `Invoke-P0Validation.ps1` 的非 Unity 部分 | 配置结构、引用和固定样例。 |
| `ui_spec_validate` | `Validate-UIDesign.ps1` | active UI 规格校验。 |
| `art_manifest_check` | 现有美术生成物校验 | 只校验，不出图或同步 Approved。 |
| `docs_validate` | `Generate-DocsIndex.ps1` / `Validate-Docs.ps1` | 仅文档任务或 Profile 要求时执行。 |
| `workspace_health` | `Invoke-AgentHealthCheck.ps1` | 输出风险，不自动清理。 |

### 5.3 Unity MCP 子编排器

`p3_run_unity_profile` 是 Unity 执行面的总入口。它只组合 Unity 内部能力，不调用外部脚本，也不输出项目总判定。

内部原子能力：

| 工具 | 职责 |
|---|---|
| `p3_unity_readiness` | 读取实例、编译、Domain Reload、PlayMode、未保存场景和可接管性。 |
| `p3_run_smoke_profile` | 执行注册表中的静态 Smoke 集合，不接受任意反射方法名。 |
| `p3_run_art_acceptance` | 复用 `ArtAcceptanceRunner`，支持登记的全量或聚焦 ScreenTag 集。 |
| `p3_capture_t0` | 复用 T0 截图 Runner 和语义断言。 |
| `p3_collect_unity_evidence` | 收集 Console delta、Editor 状态、截图和原始报告索引。 |
| `p3_run_unity_profile` | 组合上述原子能力，维护 Job、进度、超时、排他锁和恢复。 |

### 5.4 确定性合并器

建议入口：

```text
tools/agent/p3-validation/Merge-P3ValidationEvidence.ps1
```

职责：

- 验证所有必需步骤的 Schema 和 `RunID`。
- 校验 artifact 时间、哈希和输入指纹。
- 按固定优先级计算总状态。
- 生成机器可读 JSON 和人工可读 Markdown。
- 计算 `ClaimCeiling`。

合并器不调用 Unity，不重新运行任何测试，也不使用 LLM 解释结果。

## 6. Validation Profile

### 6.1 Profile 原则

Profile 是白名单步骤的声明式组合，不是任意命令列表。

最低字段：

```text
id
version
static_steps[]
unity_profile_id
required_evidence[]
timeout_seconds
strict_warnings
editor_control
external_review_required[]
```

Profile 和步骤注册表变更必须版本化。报告必须记录 `profile_id` 和 `profile_version`。

### 6.2 首轮 Profile

#### `smoke_focus`

用于程序修改后的快速回归：

- Unity readiness。
- AssetDatabase refresh 和编译门禁。
- 指定的已登记 Smoke 集合。
- Console baseline / delta。

#### `art_runtime`

用于运行时美术和 UI 验收：

- active UI Spec 静态校验。
- 指定 ArtAcceptance ScreenTag 集。
- UI / Registry snapshot。
- 必需 GameView 截图和 Console delta。
- 外部主美验收标记为 `Required`。

#### `t0_seal`

用于 T0-01A / 后续 T0 封板候选：

- T0 相关配置、UI 和资源静态校验。
- T0 聚焦 Smoke 集合。
- T0 固定语义截图集。
- Profile 指定的 FormalV2 ArtAcceptance 画面。
- 外部主美和游戏导演验收标记为 `Required`。

#### `p0_full`

用于完整 P0 门禁：

- 配置同步和 ConfigValidator。
- P0 Smoke registry。
- UI Spec 校验。
- ArtAcceptance freshness；Profile 要求时重跑。
- 完整统一报告。

## 7. 运行状态机

```text
CREATED
  -> PREFLIGHT
  -> PREPARE
  -> EXECUTE_STATIC / EXECUTE_UNITY
  -> COLLECT_EVIDENCE
  -> MERGE_AND_DECIDE
  -> PASSED | FAILED | BLOCKED | LIMITED | CANCELLED
```

### 7.1 `PREFLIGHT`

检查：

- Profile 存在且版本可用。
- Unity 实例唯一或已固定。
- 没有其他修改型 P3 Validation Job 占用实例。
- Scene / Prefab Stage 没有未保存修改。
- EvidenceRoot 可写。
- 不存在相同 `RunID` 的已完成证据包。

### 7.2 `PREPARE`

顺序：

1. 按 Profile 同步配置。
2. 记录 Unity Editor 初始状态和 Console baseline。
3. Refresh AssetDatabase。
4. 等待编译和 Domain Reload 完成。
5. 编译错误直接记为 `Blocked`，不进入业务 Smoke。

### 7.3 `EXECUTE`

静态步骤可以在无共享写入冲突时并行。Unity 修改型步骤在同一实例内串行执行。

基础设施失败可以限次重试；业务断言失败不得自动重跑到通过。

### 7.4 `COLLECT_EVIDENCE`

所有报告、截图和 Console delta 必须快照到本 `RunID` 目录。只存在于旧 `latest` 的文件不计入本轮证据。

### 7.5 `MERGE_AND_DECIDE`

总状态优先级：

1. 必需业务断言失败：`Failed`。
2. 必需步骤无法执行：`Blocked`。
3. 必需范围只有受限证据：`Limited`。
4. 全部必需步骤执行并通过：`Passed`。

`Cancelled` 单独表达用户或系统取消，不等同于失败。

## 8. Unity 会话控制

默认策略：`exclusive_restore`。

执行前记录：

- Unity 实例 ID。
- PlayMode / Pause 状态。
- 活动 Scene。
- Prefab Stage。
- 当前选择对象。
- 是否处于编译 / 更新。

行为：

- 有未保存 Scene 或 Prefab Stage 时默认阻塞，不自动保存或丢弃。
- Unity 修改型验证 Job 使用实例级排他锁。
- 已有 Job 时返回 `blocked:unity_validation_job_active` 和占用 RunID。
- 运行结束后只恢复可安全恢复的 Editor 状态。
- 如果恢复失败，验证结果保持原业务状态，但增加 `validation_limited:editor_state_restore_failed`。

## 9. 异步 Job 协议

修改型自定义工具使用 MCP polling：

```text
start  -> PendingResponse(job_id, current_step, progress)
status -> pending | complete | error | cancelled
cancel -> 在当前原子步骤的安全点退出
```

Job 状态持久化到 `Library/P3ValidationJobs/` 或 `McpJobStateStore` 对应的 Library 路径，至少保存：

- `RunID`。
- 当前步骤和进度。
- EvidenceRoot。
- 原 Editor 状态。
- 当前 Runner source RunID。
- 最后错误和恢复动作。
- Job lock 所属 Unity instance。

Domain Reload 后必须从持久化状态恢复；不能仅依赖静态字段。

## 10. 证据目录

```text
UnityClient/Logs/P3Validation/
  latest_run.json
  runs/<RunID>/
    request.json
    inputs.json
    steps/
    unity/
      editor_before.json
      console_baseline.json
      console_delta.json
      editor_after.json
    screenshots/
    source_reports/
    evidence-index.json
    mcp-invocations.jsonl
    validation-summary.json
    validation-summary.md
```

`latest_run.json` 只指向最新 RunID，不复制或改写结论。

## 11. 统一步骤结果契约

Schema：`p3-validation/step-result@1`。

最低字段：

```json
{
  "schema_version": "p3-validation/step-result@1",
  "run_id": "20260712_120000_t0_seal",
  "profile_id": "t0_seal",
  "profile_version": "1",
  "step_id": "unity_t0_capture",
  "executor": "unity_mcp",
  "required": true,
  "attempt": 1,
  "status": "Passed",
  "started_at": "",
  "finished_at": "",
  "duration_ms": 0,
  "error_count": 0,
  "warning_count": 0,
  "blocked_count": 0,
  "limitation_count": 0,
  "errors": [],
  "warnings": [],
  "validation_limitations": [],
  "artifacts": [],
  "input_fingerprint": "",
  "source_run_id": "",
  "next_action": ""
}
```

Artifact 最低字段：

```text
kind
path
source_path
sha256
size
captured_at
mime_type
```

## 12. 输入指纹

`inputs.json` 至少记录：

- 当前 Git HEAD。
- 相关工作区文件摘要；不把整个脏工作区文本写进报告。
- Unity 版本和目标平台。
- Profile ID / version。
- active `screen_layouts.json` 哈希。
- 配置源相关目录哈希。
- 目标程序集 / 脚本输入哈希。
- 目标 Approved / Registry 输入哈希，仅在相关 Profile 中记录。

后续如果支持断点续跑，只有输入指纹未变化的 `Passed` 步骤才允许复用。

## 13. Console 证据

### 13.1 Baseline / Delta

- 默认不清空 Console。
- 运行前保存 baseline。
- 每条日志使用类型、消息、堆栈和出现序号形成稳定指纹。
- 运行后只把新增或再次出现的日志计入 delta。
- 原始 baseline 和 delta 都进入证据包。

### 13.2 判定

- 新增编译 error：`Blocked`。
- Smoke / Runner 明确产生的 `LogError` 或 Exception：对应步骤 `Failed`。
- 运行前已有但本轮未再次出现的 error：保留在 baseline，不自动归为本轮失败。
- warning 是否阻塞由 Profile 的 `strict_warnings` 和允许清单决定。
- 不允许通过 Clear Console 隐藏错误。

## 14. 截图证据

每个必需截图声明：

- `ScreenTag`。
- 来源 Runner。
- 期望分辨率。
- 本轮最早生成时间。
- 必需语义断言。
- 是否需要外部人工复核。

有效截图必须：

- 在本 RunID 开始后生成。
- 非空且尺寸合法。
- 被复制进本轮 `screenshots/`。
- 写入 SHA-256 和原始来源路径。
- 通过对应 Runner 的语义断言。

MCP `manage_camera` 的通用 GameView 截图可以作为诊断或补充证据，但不能替代 T0 语义截图或 ArtAcceptance 指定 ScreenTag。

## 15. 三层结论与声明上限

最终报告分离三个维度。

### 15.1 `AutomationStatus`

表达工具是否完整执行：

```text
Passed | Failed | Blocked | Limited | Cancelled
```

### 15.2 `OwnerValidation`

表达自动证据是否支持实现 Owner 自验：

```text
NotRun | Passed | Failed | Limited
```

### 15.3 `ExternalReview`

按职能分别记录：

```text
NotRequired | Required | Approved | Rejected
```

### 15.4 `ClaimCeiling`

```text
evidence_collected
automation_passed
owner_self_validation_passed
eligible_for_external_review
externally_accepted
```

Skill 最终回复必须遵守报告的 `ClaimCeiling`。例如：

- `AutomationStatus=Passed` 只能声明自动化通过。
- 主美 `ExternalReview=Required` 时不能声明美术封板。
- 游戏导演未验收 T0 时不能声明完整序章完成。

## 16. MCP 工具安全边界

### 16.1 输入白名单

项目自定义工具只接受：

- 已登记的 Profile ID。
- 已登记的 Smoke Profile ID。
- 已登记的 Acceptance Profile ID。
- 已登记的 Capture Profile ID。
- 合法 RunID。
- 枚举化 Editor control policy。

不接受：

- 任意 C# 代码。
- 任意反射方法名。
- 任意菜单路径。
- 任意磁盘输出路径。
- 任意外部进程命令。

### 16.2 允许写入

- Refresh AssetDatabase。
- 等待编译和 Domain Reload。
- 受控进入 / 退出 PlayMode。
- 调用已登记 Runner。
- 写 `UnityClient/Logs/P3Validation/` 和 Library Job 状态。
- 生成验收截图和结构化报告。

### 16.3 禁止写入

- 正式 Scene、Prefab、脚本、配置或 Approved 资源。
- 项目根目录外文件。
- Unity Package 或 MCP 配置。
- 运行时 UI Toolkit 资产。

### 16.4 工具组

日常验证常开：

```text
core + testing + docs + P3 custom tools
```

默认关闭：

- `ui`：只处理 UI Toolkit，与项目纯 UGUI 约束冲突。
- `asset_gen`：不属于验收编排范围。
- `vfx`、`profiling`：仅在对应任务显式开启。
- `scripting_ext/execute_code`：验收 Skill 不使用任意代码执行。

## 17. 自动恢复和禁止掩盖

允许自动恢复：

- Editor 状态短暂 stale：限次重试。
- AssetDatabase 正在更新：等待完成。
- Domain Reload：从 JobState 恢复。
- 截图首帧空白：重新等待布局稳定后重截一次。
- 原始报告仍在写入：等待原子完成标记。

禁止自动掩盖：

- 业务 Smoke 失败后循环重跑到绿。
- 缺图后使用 fallback 并判正式通过。
- 旧 `latest` 冒充本 RunID 证据。
- 清空 Console 后忽略旧错误。
- 缺少外部验收时标记最终完成。

## 18. 分阶段迁移

### Phase 0：基线冻结

工作：

- 固定当前 P0、Smoke、ArtAcceptance 和 T0 的成功 / 失败报告样例。
- 锁定 Schema v1、Profile v1 和状态优先级。
- 建立失败注入样例。

出口：现有结果口径可被自动比较。

### Phase 1：只读可观测

工作：

- 实现 `p3_unity_readiness`。
- 实现实例固定、Console baseline / delta、Editor before / after。
- 不进入 PlayMode，不调用 Runner。

出口：连续 10 次状态和 Console 采集无误关联，不修改 Editor 状态。

### Phase 2：原子 MCP 适配

工作：

- 包装 Smoke、ArtAcceptance、T0 Capture。
- 增加 polling、RunID 快照和 JobState。
- 保持相同 Runner 和业务判定。

出口：MCP 和旧入口调用同一 Runner 时，核心结果、截图数量和失败断言一致；旧 `latest` 不会混入。

### Phase 3：Unity 子编排器

工作：

- 实现 `p3_run_unity_profile`。
- 增加实例排他锁、`exclusive_restore`、超时和取消。
- 落地首轮 Unity Profile 注册表。

出口：Domain Reload、编译等待、截图空白、任务冲突、取消和恢复均输出正确状态。

### Phase 4：`p3-validation` Skill

工作：

- 编排静态脚本和 Unity MCP。
- 实现确定性合并器。
- 落地四个首轮 Profile。
- 生成三层结论和 `ClaimCeiling`。

出口：每个 Profile 都有成功与失败样例；Skill 回复不扩大声明。

### Phase 5：默认入口切换

工作：

- Agent 日常验证默认使用 `p3-validation` Skill。
- 手工触发文件和通用 `execute_menu_item` 降级为兼容 / 诊断入口。
- PowerShell 静态检查和 CI 继续保留。

出口：连续日常使用不需要人工清场、读 Console 或寻找报告。

## 19. 失败注入与测试策略

必须覆盖：

- Unity 状态 stale。
- Unity 实例断开或多实例未固定。
- 编译 error 和编译超时。
- Domain Reload 中断。
- 已有修改型 Job 占用实例。
- 未保存 Scene / Prefab Stage。
- Smoke 业务断言失败。
- ArtAcceptance / T0 截图缺失、空白或语义失败。
- 旧 `latest` 时间较新但 source RunID 不匹配。
- 运行中 cancel。
- Editor 状态恢复失败。

测试分层：

1. Schema 和合并器单元测试。
2. Profile 白名单和参数校验测试。
3. JobState、Domain Reload 和排他锁 EditMode 测试。
4. 使用假 Runner 的工具契约测试。
5. 使用真实 Unity 的 Smoke / ArtAcceptance / T0 集成测试。
6. 四个 Profile 的 golden report 回归。

## 20. 旧入口替换策略

| 旧能力 | MCP 稳定后的定位 |
|---|---|
| 人工写 `.test_trigger` / `.art_acceptance_trigger` | Agent 默认停用；脚本自动化和故障回退保留。 |
| `execute_menu_item` 启动 Runner | 自定义工具不可用时的诊断入口。 |
| `AutoTestDaemon` / ArtAcceptance / T0 Runner | 继续作为执行核心，首轮不重写。 |
| PowerShell 静态验证和 CI | 继续保留。 |
| 手工读 Console、等待 PlayMode、找截图 | 由 MCP 完全替代。 |

后续只有在 MCP 原生实现具备明确更好效果、相同或更强证据以及完整失败注入覆盖时，才替换对应旧执行核心。

## 21. 回滚

本设计的回滚不要求回退游戏业务代码：

- 关闭 Unity MCP 的 Project Scoped Tools。
- 禁用或移除 `p3-validation` Skill。
- 恢复现有脚本、菜单和触发文件路径。

MCP 证据目录位于 `Logs`，Job 状态位于 `Library`，不改变项目玩法、配置、美术或 UI 事实来源。

## 22. 完成口径

本设计进入“实现完成”至少需要：

1. `p3-validation` Skill 可被 Codex 识别。
2. 六个 P3 Unity MCP 工具可发现，且 Project Scoped Tools 已显式开启。
3. 四个首轮 Profile 可运行。
4. Schema、合并器、JobState 和失败注入测试通过。
5. `smoke_focus`、`art_runtime`、`t0_seal`、`p0_full` 均有成功和失败证据包。
6. Console delta、截图归属、旧 `latest` 排除和 ClaimCeiling 有聚焦测试。
7. 文档、工具 README 和 `agent_status/program.md` 已回写。
8. Owner 自验完成；外部验收按 Profile 要求分别记录，未验收时不得标记最终完成。

## 23. 当前状态

截至 2026-07-12：

- 本文设计已完成并经用户逐段确认。
- Unity MCP v10.0.0 已安装并能发现当前 Unity 实例。
- 项目现有 Runner、PowerShell 和报告体系继续作为实现输入。
- 尚未创建 `p3-validation` Skill、自定义 MCP 工具、Profile 注册表、统一 Schema 或合并器。
- 因此当前只能声明“设计完成，等待实现计划”，不能声明 MCP 验收编排层已经落地。
