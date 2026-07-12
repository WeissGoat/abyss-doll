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
  - 开发文档/20_UnityMCP验收编排层实现计划.md
  - 开发文档/rules/04_自动化测试与验收流程规范.md
  - tools/agent/README.md
  - 知识库/views/program.md
last_verified: 2026-07-12
update_rule: 修改程序验收、美术迭代验收、发布聚合、共享证据契约或 Unity MCP 验收边界时同步本文件。
---

# Unity MCP 验收编排层设计

> 本文是 P3 MCP 验收架构的事实来源。日常程序自动化测试与美术运行时迭代验收必须分开；二者只共享 RunID、Unity readiness、Console、截图、JobState、证据 Schema 和确定性合并能力。

## 1. 设计结论

采用三个独立入口和一个共享核心：

```text
p3-program-validation   程序自动化测试
p3-art-validation       美术 / UI 运行时迭代与验收
p3-release-validation   发布级证据聚合
            ↓
      P3ValidationCore
```

核心规则：

- 程序写代码后的日常验收只跑程序自动化测试，不自动启动 ArtAcceptance。
- 美术 / UI 日常工作只处理界面、布局、素材绑定、截图、视觉诊断和受控迭代，不运行完整 P0，也不修改领域规则。
- 发布聚合只读取已经完成的 ProgramRunID 和 ArtRunID，不重新执行测试。
- 程序通过不代表美术通过；美术截图通过不代表功能正确。
- 当前 `p3-validation` 尚未成为正式工作流入口，没有需要兼容的历史调用或正式证据，因此本轮采用直接拆分，不维护旧混合 Profile 兼容层。

## 2. 总体架构

```text
共享基础设施：P3ValidationCore
  ├─ RunID / EvidenceRoot
  ├─ Unity instance pinning
  ├─ readiness / compile / PlayMode
  ├─ Console baseline / delta
  ├─ screenshot / report collection
  ├─ JobState / instance lock
  ├─ step-result schema
  └─ deterministic merger

程序入口：p3-program-validation
  ├─ smoke_focus
  ├─ t0_functional
  └─ p0_full

美术入口：p3-art-validation
  ├─ art_focus
  ├─ art_runtime
  ├─ art_iteration
  └─ t0_art_seal

发布入口：p3-release-validation
  ├─ ProgramRunID
  ├─ ArtRunID
  └─ ReleaseSummary
```

共享核心不是用户 Skill，不负责选择职能目标。它只提供两边都需要的确定性能力。

## 3. 程序自动化测试

### 3.1 入口与职责

入口为 `p3-program-validation`。

标准链路：

```text
读取程序事实与任务范围
→ 选择程序 Profile
→ 创建 ProgramRunID
→ 静态校验
→ Unity readiness
→ 编译门禁
→ 注册 Smoke
→ Console delta
→ 程序证据合并
→ ProgramValidationSummary
```

程序侧负责：

- 编译、配置结构和引用合法性。
- 领域规则、服务调用和状态变化。
- Smoke、P0、T0 功能路径。
- UI 的程序契约：对象存在、交互可达、Controller 调用真实服务、VisualID 可解析、状态能回写。

程序侧不负责：

- 布局、颜色、留白、视觉层级和素材审美。
- 截图封板和主美复核。
- 自动启动 ArtAcceptance。
- 声明 UI 或美术完成。

### 3.2 程序 Profile

| Profile | 用途 | 必需内容 |
|---|---|---|
| `smoke_focus` | 单功能快速回归 | 编译、指定白名单 Smoke、Console delta |
| `t0_functional` | T0 功能纵切 | 配置、T0 状态、功能 Smoke、玩家路径 |
| `p0_full` | 完整程序门禁 | ConfigValidator、P0 Smoke、程序 UI 契约 |

`p0_full` 不包含 `art_acceptance_report`、截图或主美验收字段。

## 4. 美术迭代与验收

### 4.1 入口与职责

入口为 `p3-art-validation`。

标准链路：

```text
读取 active UI / 美术规格
→ 选择界面或 ScreenTag
→ 创建 ArtRunID
→ Unity readiness
→ 进入目标运行时状态
→ 截图与层级快照
→ 视觉诊断
→ 可选受控迭代
→ 重载 / 编译 / PlayMode
→ 重新截图和前后对比
→ ArtValidationSummary
→ 主美复核
```

美术侧负责：

- UGUI 结构、RectTransform、布局和视觉层级。
- Approved Sprite、VisualID 和运行时素材绑定。
- 遮罩、射线、CanvasGroup、文本和表现动效。
- ScreenTag、截图、UI snapshot、Registry snapshot 和视觉诊断。
- MCP 辅助的 before/after 迭代闭环。

美术侧不负责：

- HP、SAN、金币、战斗、掉落、经济和领域规则。
- 修改配置事实或领域服务。
- 用 `manage_ui` 引入 UI Toolkit。
- 自动批准 AI 素材或擅自改变 active UI 设计方向。
- 用截图成功替代程序功能通过。

### 4.2 美术 Profile

| Profile | 用途 | 是否允许修改 |
|---|---|---:|
| `art_focus` | 单界面或单 ScreenTag 聚焦诊断 | 否 |
| `art_runtime` | 一组运行时界面验收 | 否 |
| `art_iteration` | UI / 美术受控修正循环 | 是 |
| `t0_art_seal` | T0 固定画面和语义截图封板 | 否 |

### 4.3 Art iteration 修改白名单

允许：

- 调整注册目标的 RectTransform、锚点、间距和层级。
- 修改 Image、Text、颜色、透明度、CanvasGroup、遮罩和射线。
- 切换到已 Approved 且已登记的 VisualID。
- 执行注册的 UI rebuild 和指定 ScreenTag 重跑。

禁止：

- 任意 C#、任意菜单、任意资产路径或任意反射方法。
- 修改领域逻辑、游戏配置、Approved 素材内容或 active 设计方向。
- 绕过程序接口直接伪造玩家状态。

每轮迭代保留：

```text
iterations/<iteration-id>/
  before.png
  diagnosis.json
  changes.json
  after.png
  console-delta.json
  result.json
```

## 5. 共享证据契约

程序和美术使用不同 RunID：

```text
ProgramRunID = <timestamp>_program_<profile>
ArtRunID     = <timestamp>_art_<profile>
```

目录：

```text
UnityClient/Logs/P3Validation/
  program-runs/<ProgramRunID>/
  art-runs/<ArtRunID>/
  release-runs/<ReleaseRunID>/
```

统一 `step-result` 增加：

```json
{
  "schema_version": "p3-validation/step-result@2",
  "validation_domain": "program",
  "run_id": "...",
  "profile_id": "...",
  "step_id": "...",
  "required": true,
  "status": "Passed",
  "artifacts": []
}
```

`validation_domain` 只允许：

```text
program | art | release | infrastructure
```

ProgramRunID 不接受 art 步骤；ArtRunID 不接受 program 步骤。共享 readiness、Console、恢复和锁步骤使用 `infrastructure`。

## 6. 失败归属

程序错误：

```text
program_failed:compile_error
program_failed:config_validation
program_failed:smoke_assertion
program_failed:runtime_exception
program_blocked:unity_unavailable
program_limited:console_delta_unavailable
```

美术错误：

```text
art_failed:missing_visual
art_failed:layout_contract
art_failed:semantic_screenshot
art_failed:runtime_binding
art_blocked:target_screen_unreachable
art_limited:screenshot_unavailable
art_review_required:visual_quality
```

共享基础设施错误：

```text
infra_blocked:validation_job_active
infra_limited:domain_reload_recovery
infra_limited:editor_restore_failed
infra_limited:custom_mcp_tools_unavailable
```

目标界面因程序问题不可达时，美术验收标记 `Blocked` 并生成程序交接，不自行修玩法逻辑。程序修复和美术复验分别创建新的 RunID。

## 7. 三层结论与声明上限

程序结果：

```text
ProgramAutomationStatus
ProgramOwnerValidation
ProgramClaimCeiling
```

美术结果：

```text
ArtAutomationStatus
ArtOwnerValidation
ArtExternalReview
ArtClaimCeiling
```

状态优先级：

```text
Failed > Blocked > Limited > Cancelled > Passed
```

`ClaimCeiling`：

```text
evidence_collected
automation_passed
owner_validated
externally_reviewed
```

自动截图和规则检查通过后，主美未复核时仍必须保持：

```text
ArtExternalReview = Required
ArtClaimCeiling = owner_validated
```

## 8. 发布聚合

入口为 `p3-release-validation`。首轮 Profile：

```text
vertical_slice_release
t0_release
```

输入：

```text
ProgramRunID
ArtRunID
```

发布聚合器只读取证据，检查：

- 两侧 Profile 是否满足发布要求。
- Git HEAD、配置、UI Spec、Registry 和 Approved 输入指纹是否兼容。
- Program、Art Owner 和外部 Reviewer 状态是否满足门禁。

规则：

```text
程序失败 → Release Failed
美术失败 → Release Failed
任一侧 Blocked → Release Blocked
任一侧 Limited → Release Limited
自动化均通过但外审未完成 → Release ReviewRequired
全部门禁与外审通过 → Release Passed
```

发布聚合器不运行测试、不修改 Unity、不覆盖两侧原始结论。

## 9. Skill 与 MCP 工具

### 9.1 Skills

```text
.codex/skills/p3-program-validation/
.codex/skills/p3-art-validation/
.codex/skills/p3-release-validation/
```

删除旧 `.codex/skills/p3-validation/`，不提供兼容路由。

### 9.2 共享 MCP 工具

```text
p3_validation_readiness
p3_validation_collect_console
p3_validation_collect_evidence
```

### 9.3 程序 MCP 工具

```text
p3_program_run_smoke
p3_program_run_profile
```

### 9.4 美术 MCP 工具

```text
p3_art_open_target
p3_art_capture
p3_art_run_acceptance
p3_art_run_profile
p3_art_compare_iteration
```

所有工具只接收白名单 Profile、Smoke set、ScreenTag、目标 ID 和修改动作，不接收任意 C#、菜单或文件系统路径。

## 10. 测试门禁

### 10.1 程序侧

- 不会启动 ArtAcceptance。
- 不要求截图或主美验收。
- Smoke 失败输出 `ProgramAutomationStatus=Failed`。
- 编译不可用输出 `Blocked`。
- 程序通过不能声明视觉通过。

### 10.2 美术侧

- 不运行完整 P0，不修改领域状态。
- `art_focus` 只诊断。
- `art_iteration` 必须记录 before/after。
- 目标不可达时转交程序。
- 自动检查通过后仍保持主美外审要求。
- 只使用 Approved 素材。

### 10.3 发布侧

- Program Pass + Art Fail → Release Fail。
- Program Fail + Art Pass → Release Fail。
- 两边 Pass + 主美未验收 → Release ReviewRequired。
- 输入指纹不一致 → Release Blocked。
- 两边及外审全部通过 → Release Pass。
- 聚合器不会重新运行两侧验收。

## 11. 直接替换与清理

由于旧 `p3-validation` 没有正式使用记录，本次不做兼容迁移：

- 删除旧混合 Skill 和四个混合 Profile。
- 删除本轮测试生成的 `e2e_*` RunID。
- 删除旧混合 Profile 的 golden fixtures。
- 不保留 deprecated 周期、旧 RunID 读取层或双格式报告。

保留并重构：

- RunID、EvidenceRoot 和 Artifact hash。
- Editor readiness、Console delta、JobState 和实例锁。
- Smoke 执行服务、ArtAcceptance/T0 adapter。
- 确定性状态优先级和 ClaimCeiling。

## 12. 完成口径

实现完成必须同时满足：

1. 三个新 Skill 可被 Codex 识别，旧 Skill 已删除。
2. 程序和美术 Profile 使用独立事实来源和独立 RunID 根目录。
3. `step-result@2` 强制校验 `validation_domain`。
4. Program Merger 拒绝 art 步骤，Art Merger 拒绝 program 步骤。
5. 程序 Profile 不启动 ArtAcceptance。
6. `art_iteration` 只允许白名单 UGUI / VisualID 修改，并保留 before/after。
7. 发布聚合只读取两侧证据和输入指纹。
8. 程序、美术、发布三类成功与受控失败测试均通过。
9. 当前文档、开发规则、Skill、状态页和知识库索引已同步。

## 13. 当前状态

- 本设计已由用户确认采用方案 B。
- 旧 `p3-validation` 首版实现存在，但尚未成为正式工作流，也没有需要兼容的正式证据。
- 下一步先生成直接拆分的详细实现计划，再执行代码、Profile、Skill 和证据格式重构。
