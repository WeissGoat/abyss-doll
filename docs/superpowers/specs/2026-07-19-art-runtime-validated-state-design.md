---
id: spec_art_runtime_validated_state
title: 美术素材 Runtime Validated 状态收敛设计
type: design
role: 美术
domain: runtime_art_validation
status: active
source_of_truth: false
related: []
last_verified: 2026-07-19
update_rule: 修改 registered 后的运行时绑定检查、美术验收完成口径、ArtRun 最终 claim、失败路由或用户可见状态时更新本文档。
---

# 美术素材 Runtime Validated 状态收敛设计

## 1. 结论

Project P3 对用户和跨职能交接只暴露以下美术素材状态：

```text
registered
-> runtime_validated
-> player_path_verified
-> regression_passed
```

不再把 `runtime_bound` 作为独立的用户可见状态、Manifest 字段或人工维护状态。

`runtime_validated` 表示：指定正式运行时目标实际消费了已注册素材，Unity 运行时技术检查通过，并且 Agent 已完成该目标的美术验收。绑定检查仍然存在，但只作为 ArtRun 内部检查项，不单独形成公开进度状态。

本设计不改变 `registered` 的现有完成口径，也不把正常玩家路径或大范围回归合并进 `runtime_validated`。

## 2. 设计动机

原状态讨论中存在：

```text
registered -> runtime_bound -> runtime_validated
```

但 `runtime_bound` 对项目使用者价值有限：

- 美术验收必须看到真实运行时目标，天然会检查目标是否消费正确素材；
- 单独暴露 `runtime_bound` 容易被误解为画面已经通过；
- 程序接入、绑定检查和视觉审核分成多个外部状态，会增加手工回写和跨职能沟通成本；
- 当前项目更关心“Unity 已可取用”和“运行时美术已通过”两个稳定结果。

因此将绑定检查并入运行时美术验收，在 ArtRun Finalize 时统一形成 `runtime_validated` claim。

## 3. 状态语义

### 3.1 `registered`

保持现有定义：

- Approved 正式资源存在；
- Unity AssetDatabase 已识别正确资源类型和 importer；
- live Registry 中 VisualID 唯一；
- `TryGetEntry` 成功；
- Registry 返回资源的路径和 GUID 与 Approved 一致；
- Manifest 主 `Status=approved`，`RegistryStatus=registered`。

`registered` 只说明素材已经可由程序按 VisualID 使用，不说明任何运行时目标已经展示或通过美术验收。

### 3.2 `runtime_validated`

必须在同一个 ArtRun 中同时满足：

1. 目标是已注册的正式 runtime TargetID；
2. 目标实际消费请求中的 VisualID；
3. live Sprite / Prefab / 资源路径和 GUID 与 Approved / Registry 一致；
4. 目标组件不是验收专用临时对象或伪造玩家状态对象；
5. Game View、bounded UGUI snapshot 和目标技术检查有效；
6. DisplaySpec、适配、裁切、透明边缘、遮挡、层级和可读性符合目标事实；
7. Agent 审核结论为通过；
8. Console 没有目标相关阻塞错误；
9. ArtRun 证据完整且时间、Unity 实例、PlayMode generation 和 TargetID 一致。

只有 ArtRun Finalize 可以产生 `runtime_validated`。不允许手工修改 Manifest 或生成清单来声明通过。

### 3.3 `player_path_verified`

保持独立：玩家可以从正常游戏入口到达目标，而不是只通过 Runner、debug 菜单、测试对象或直接调用 Controller。

一个目标可以是 `runtime_validated` 但尚未 `player_path_verified`。

### 3.4 `regression_passed`

保持独立：目标进入规定范围的视觉回归，并且该回归 RunID 通过。

`art_focus` 或单个 `art_runtime` 通过不能扩大声明为 `regression_passed`。

## 4. 对外状态与内部检查分离

对外只显示：

```text
registered | runtime_validated | player_path_verified | regression_passed
```

ArtRun 内部保留结构化检查：

```json
{
  "checks": {
    "registry_check": "passed",
    "binding_check": "passed",
    "runtime_target_check": "passed",
    "display_check": "passed",
    "console_check": "passed",
    "agent_review": "passed"
  },
  "claim": "runtime_validated"
}
```

这些检查用于归因、恢复和路由，不是额外的用户状态。

不新增以下字段：

```text
Manifest.RuntimeBound
Manifest.RuntimeBindingStatus
RegistryStatus=runtime_bound
人工 runtime_bound checkbox
```

## 5. 标准 ArtRun 流程

```text
REGISTERED_TARGET_SELECT
-> ART_RUN_CREATE
-> UNITY_INSTANCE_LOCK
-> VALIDATION_READINESS
-> TARGET_OPEN
-> TARGET_INSPECT
-> REGISTRY_AND_BINDING_CHECK
-> LIVE_GAME_VIEW_INSPECTION
-> DISPLAY_AND_ART_REVIEW
-> EVIDENCE_CAPTURE_IF_REQUIRED
-> ART_RUN_PROFILE_COMPLETE
-> RUNTIME_VALIDATION_FINALIZE
-> RUNTIME_VALIDATED
```

绑定检查在视觉审核前执行，因为无法证明目标消费正确素材时，后续画面审核没有有效对象。状态写回在最后的 `RUNTIME_VALIDATION_FINALIZE` 统一执行，避免中途暴露半完成状态。

## 6. Binding Check

Binding Check 是内部硬门禁，至少验证：

- 请求 VisualID 当前为 `RegistryStatus=registered`；
- TargetID 是 validation core 已注册的正式目标；
- 目标 bounded snapshot 中存在预期组件；
- 组件当前使用的 Sprite / Prefab 非空；
- live 资源 AssetPath 和 GUID 与 Registry 目标一致；
- 目标对象不是只为验收临时创建的替代对象；
- TargetID、组件路径和资源字段可以写入证据。

对于动态创建的 UGUI，不能只扫描 Prefab 或代码字符串；必须在 live target 打开后检查实际组件值。

对于一个目标消费多个 VisualID 的情况，每个 VisualID 单独记录检查结果。

## 7. Agent 美术审核

Agent 基于 active UI / 美术事实、Game View 和 bounded snapshot 检查：

- DisplaySpec 和参考容器；
- `contain` / `cover` / 9-slice 行为；
- pivot、anchor、脚底基线和安全区；
- 裁切、透明边缘、Mask、CanvasGroup 和层级遮挡；
- 小尺寸辨识、状态差分和套组切换稳定性；
- 不应阻挡的 raycast；
- 目标相关 Console error / warning；
- 目标事实规定的构图、身份、风格和语义。

Agent 审核失败时，不产生 `runtime_validated`，但保留 Binding Check 结果用于失败路由。

## 8. Finalize 决策矩阵

| Binding Check | Agent / Display Review | Final claim | 路由 |
|---|---|---|---|
| failed | 未执行或无效 | 保持 `registered` | `program_binding_required` |
| passed | failed | 保持 `registered` | `art_iteration_required` |
| passed | limited | 保持 `registered` | `validation_limited:<reason>` |
| passed | passed | `runtime_validated` | 可进入玩家路径验证 |

不对外暴露“绑定已通过但视觉失败”的中间状态。该事实只保存在 ArtRun `checks.binding_check=passed` 中，避免程序重复排查，同时保持用户状态简单。

## 9. 多目标与套组聚合

运行时验收以以下组合为最小事实单元：

```text
VisualID + TargetID
```

例如：

```text
doll_zero_dialogue_neutral + workshop_main_dialogue_portrait
doll_zero_dialogue_neutral + doll_room_dialogue_portrait
```

单个 TargetID 通过，只能声明该目标的 `runtime_validated`。

如果需要声明一个 VisualID 的全局 `runtime_validated`，必须存在明确的 required TargetID 列表，并且所有 required targets 都有新鲜通过证据。没有 required targets 事实时，只允许 target-scoped claim，不能猜测全局完成。

角色立绘套组还需要每个必需成员和套组切换检查通过，才能声明 AssetSetID 级 `runtime_validated`。

## 10. 证据模型

继续使用现有 ArtRunID，不新增 RuntimeBindingRunID 或第二套进度表。

ArtRun summary 增加或收敛为：

```json
{
  "schema": "p3-art-run-summary@next",
  "art_run_id": "art_run_zero_neutral_workshop_01",
  "profile": "art_focus",
  "unity_instance": "UnityClient@c0741596",
  "target_id": "workshop_main_dialogue_portrait",
  "visual_ids": ["doll_zero_dialogue_neutral"],
  "checks": {
    "registry_check": "passed",
    "binding_check": "passed",
    "runtime_target_check": "passed",
    "display_check": "passed",
    "console_check": "passed",
    "agent_review": "passed"
  },
  "bindings": [
    {
      "visual_id": "doll_zero_dialogue_neutral",
      "component_path": "Canvas/Workshop/Dialogue/Portrait",
      "asset_path": "Assets/Art/Approved/Dolls/doll_zero_dialogue_neutral.png",
      "asset_guid": "7d7b3a5d2f28634469b19bb5aa52664b"
    }
  ],
  "claim": "runtime_validated",
  "status": "passed"
}
```

实际 schema 名称和版本由实现计划锁定；设计要求是保留内部检查、精确目标和最终 claim，不要求照抄示例字段顺序。

## 11. Profile 行为

### `art_focus`

单个 TargetID。通过 Binding Check、画面审核和 Agent review 后，产生 target-scoped `runtime_validated`。

### `art_runtime`

一组已注册 TargetID。逐目标检查，只有请求范围内全部必需目标通过，Run summary 才为 passed。

### `art_iteration`

开始前检查 binding；持久化调整后退出并重新进入 PlayMode，再次检查 binding 和画面。只有 `after` 通过才产生 `runtime_validated`。

### `t0_art_seal`

每个固定 seal target 必须单独满足 Binding Check 和 Agent review。运行时通过不自动等于正常玩家路径通过。

### `art_regression`

导入 legacy ArtAcceptance 证据时，仍需把目标绑定检查映射进统一 checks。完整回归通过后可进一步声明 `regression_passed`。

## 12. 失败分类与 Owner

| 失败代码 | Owner | 含义 |
|---|---|---|
| `art_blocked:visual_not_registered` | 美术生产 / Unity 接入 | VisualID 尚未 registered 或 live Registry 不一致 |
| `art_blocked:runtime_binding_missing` | 程序 | 目标未消费预期 VisualID |
| `art_blocked:runtime_binding_mismatch` | 程序 | 目标消费了错误路径、GUID 或资源类型 |
| `art_blocked:target_screen_unreachable` | 程序 / Owner | 无法打开正式运行时目标 |
| `art_failed:display_spec` | 美术 / UI | 容器、裁切、适配或层级不符合事实 |
| `art_failed:agent_review` | 美术 | 构图、身份、风格、语义或状态差分未通过 |
| `validation_limited:*` | 对应环境 Owner | 证据不完整，不能形成最终 claim |

Binding Check 已通过但视觉失败时，路由到美术迭代，不重新派发 Registry 或程序绑定任务。

## 13. 生成交接与状态写回

- Manifest 主 `Status` 保持 `approved`；`RegistryStatus` 保持 `registered`。
- `runtime_validated` 不手写进生成 Manifest。
- ArtRun summary 是运行时验收事实来源。
- 现有程序交接 / 美术验收生成物从 ArtRun summary 派生 runtime validation 状态和下一步动作。
- 绑定失败进入程序交接队列；视觉失败进入美术迭代队列；通过后从当前 runtime validation 队列移除。
- 项目状态页只在跨职能交接、项目级阻塞或完成口径变化时回写。

旧 `runtime_bound` 证据如已存在，只作为兼容输入映射为 `checks.binding_check=passed`，不得单独形成新的用户状态。

## 14. 安全边界

- 美术验收流程不自动修改业务代码、Prefab、配置源或领域状态。
- 不通过任意 C#、反射路径或临时对象伪造 binding passed。
- 不把 Runner 创建的验收专用对象当作正式 runtime target。
- 不因画面看起来正确而跳过 live AssetPath / GUID 检查。
- 不因 binding passed 而跳过 Agent 美术审核。
- 不因 `runtime_validated` 而扩大声明为玩家路径或完整回归通过。

## 15. 实现影响面

后续实现计划预计涉及：

- `.codex/skills/p3-art-validation` 的状态口径与证据说明；
- validation core 的 target inspection / ArtRun finalize；
- ArtRun summary schema 与兼容读取；
- 程序交接和美术验收生成物的状态路由；
- `美术文档/00_美术流水线总览.md`、`01_Manifest规范.md`、`02_资源规格与接入规范.md`；
- `开发文档/rules/03_视觉资源系统程序开发规范.md`；
- 状态页和专项测试。

本设计阶段不修改上述事实来源和运行代码；它们在实现通过验证后同步更新。

## 16. 测试与验收

### 单元测试

- 未 registered 的 VisualID 不能进入 runtime validation；
- binding 缺失、错误 GUID、错误资源类型均不能形成 `runtime_validated`；
- binding passed 但 display / Agent review failed 时保持公开状态 `registered`；
- 所有内部 checks passed 时才写最终 claim；
- 旧 `runtime_bound` 输入只映射内部 binding check；
- 多 TargetID 聚合不遗漏 required target；
- target-scoped claim 不被扩大为 VisualID 全局通过。

### Unity MCP 集成测试

- 正式 runtime target 能返回实际组件路径、资源路径和 GUID；
- 动态 UGUI 在 live target 中能验证当前 Sprite；
- 验收专用临时对象不能通过正式 binding gate；
- Game View、bounded snapshot、capture ticket 和 PlayMode generation 一致；
- 目标 Console 错误阻断 Finalize；
- ArtRun Finalize 幂等，不重复生成状态或证据。

### 首个试点

使用已 registered 的 `doll_zero_dialogue_neutral`，选择一个正式 UGUI TargetID：

1. 未接入时应返回 `art_blocked:runtime_binding_missing`；
2. 程序完成 VisualID 消费后，Binding Check 应通过；
3. 画面或 Agent review 未通过时公开状态仍为 `registered`；
4. 全部通过后，ArtRun claim 为 `runtime_validated`；
5. 不声明 `player_path_verified`，除非另有正常玩家入口证据。

## 17. 完成口径

实现完成必须满足：

1. 用户可见状态中不存在独立 `runtime_bound`；
2. `registered -> runtime_validated` 是唯一运行时美术晋级路径；
3. ArtRun 内部保留可归因的 binding / display / Agent review checks；
4. 只有 Finalize 全部通过才产生 `runtime_validated`；
5. 多目标、套组和 player-path claim 不被错误聚合；
6. 程序和美术失败能路由到正确 Owner；
7. 不新增第二套人工进度表或手工 Manifest 状态；
8. Skill、事实文档、validation core、生成交接和测试口径一致。
