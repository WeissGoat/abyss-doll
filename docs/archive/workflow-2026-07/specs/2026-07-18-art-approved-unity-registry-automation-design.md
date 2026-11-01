---
id: spec_art_approved_unity_registry_automation
title: Approved 到 Unity 导入与 Registry 登记自动化设计
type: design
role: 美术
domain: art_asset_integration
status: historical
source_of_truth: false
related: []
last_verified: 2026-10-01
update_rule: 历史设计记录，不再更新；现行事实以 美术文档/00_美术流水线总览.md 与 p3-art-asset-production 的 references/approved-and-unity.md 为准。
---

# Approved 到 Unity 导入与 Registry 登记自动化设计

> 历史设计：已于 2026-07-19 实施（`2765a4b`、`8973481`、`a6ce212`）。现行事实以 `美术文档/00_美术流水线总览.md` 与 `p3-art-asset-production` 的 `references/approved-and-unity.md` 为准。

## 1. 结论

采用“现有脚本负责磁盘安全写入，Unity MCP 负责 live Editor 事实与菜单编排”的方案，将正式静态 Sprite 的后半段流程收敛为：

```text
selected
-> approved
-> unity_imported
-> registered
```

本设计不新增 Unity Editor 侧项目工具，不新增 `unity_available` 状态，也不把 Prefab、UGUI、游戏状态绑定或运行时截图验收纳入本轮。现有 `Sync-ApprovedArt.ps1` 继续作为 Approved 文件和 `.meta` / GUID 安全 Owner；Codex 通过 Unity MCP 锁定实例、刷新 AssetDatabase、等待 Editor 空闲、检查 importer、执行现有 Registry 菜单并读取 live Registry。

最终完成含义是：指定 VisualID 的正式素材已进入 Approved，当前 Unity Editor 已把它识别为符合合同的 Sprite，并且 `VisualAssetRegistry` 能以该 VisualID 唯一返回预期 Sprite。程序如何在 Prefab / UGUI / 游戏流程中消费该 VisualID，属于后续运行接入任务。

## 2. 目标与非目标

目标：

- 将 Approved 同步、Unity 导入和静态 Sprite Registry 登记变成 Agent 可重复执行、可暂停、可恢复的标准链路。
- 继续复用 Manifest、`Sync-ApprovedArt.ps1`、现有 Registry 菜单和生成清单，不建立第二套资产台账。
- 对每个 VisualID 留下 selected、Approved、`.meta`、GUID、importer、Registry 和 Console 的可追溯证据。
- 支持交互模式的人工门禁，也支持用户提前授权后的全自动执行。
- 同一流程同时支持新 VisualID 和同 VisualID 正式图替换，但不得放宽覆盖与 GUID 保护。
- 将 Unity readiness 收敛为内部等待机制，不暴露成新的业务阶段。

非目标：

- 不创建或修改 Prefab、UGUI、Animator、DollPuppet、Live2D 或其他运行时绑定。
- 不进入 PlayMode，不运行 Game View、美术验收、ArtAcceptance、T0 或程序回归。
- 不为 Prefab、动态立绘、Live2D、VFX、Audio 强行复用静态 Sprite Registry。
- 不修改 `VisualAssetRegistryEditorTools.cs` 或新增项目侧 MCP custom tool。
- 不让 `_IncomingAI` 成为 Unity、Registry 或运行时代码的引用来源。
- 不自动回滚已经成功写入的 Approved 文件；后续 Unity 阶段失败时保留明确恢复点。

## 3. 状态与事实归属

三个状态分别由不同证据成立：

| 状态 | Owner | 成立条件 | 持久化位置 |
|---|---|---|---|
| `approved` | 现有美术脚本 | Approved 文件已按 Manifest OutputPath 写入，selected 来源、hash 和 `.meta` / GUID 门禁通过 | Manifest `Status=approved`、`ApprovedPath`、`approved-sync.json` |
| `unity_imported` | Unity MCP + live Editor | AssetDatabase 能在预期路径加载 Texture2D 和 Sprite，live GUID 与 `.meta` 一致，最终 importer 满足 Asset Contract | `unity-import.json` |
| `registered` | Unity MCP + live Registry | Registry 中该 VisualID 恰好一个有效条目，Sprite 非空，路径和 GUID 与目标 Approved Sprite 一致，`TryGetEntry` 返回同一对象 | Manifest `RegistryStatus=registered`、`registry-result.json` |

Manifest 主 `Status` 继续停在生产轴的 `approved`；不把 `unity_imported` 写成临时 Manifest 字段。接入轴使用现有 `RegistryStatus=registered`，并由 live Unity 证据支撑。旧脚本若仍兼容读取主 `Status=registered`，不得据此反推本流程必须改写主状态。

## 4. 职责边界

### 4.1 现有脚本继续负责

- 根据 `ProductionProfile + VisualID` 解析工作区。
- 解析 Manifest `SelectedPath`、`selected/` 和最新安全数字轮次。
- 执行 Approved dry-run 和正式同步。
- 校验 Manifest VisualID、OutputPath、候选路径、hash、尺寸和格式。
- 新目标与覆盖授权。
- 保留已有 `.meta`，执行严格 GUID guard。
- 更新 Manifest 的 `SelectedPath`、`ApprovedPath`、`Status=approved`。
- 刷新可接入素材清单及必要 snapshot。

不得绕过 `Sync-ApprovedArt.ps1` 手工复制正式 PNG。

### 4.2 Unity MCP 负责

- 锁定唯一 Unity 实例。
- 读取 Editor state、菜单和工具组状态。
- 刷新 AssetDatabase 并等待编译 / 导入空闲。
- 读取本次 VisualID 的 live 资产类型、GUID、Sprite 和 importer。
- 执行 `Tools/P3 Art/Rebuild Approved Sprite Registry`。
- 读取本次 VisualID 的 live Registry 实际内容。
- 执行 `Tools/P3 Art/Validate Approved Display Specs`。
- 采集操作前后的 Console，并计算本次 delta。

菜单返回 `success=true` 只证明菜单被调用，不能单独证明 `unity_imported` 或 `registered`。

### 4.3 Agent 编排层负责

- 建立 ArtImportRunID、权限、VisualID 范围和输入指纹。
- 调用脚本阶段与 MCP 阶段，并把结果归并到同一证据包。
- 根据交互模式决定是否在 Approved 门禁暂停。
- 对失败分类，决定重试、恢复、请求授权或停止。
- 完成后刷新生成交接清单并回写受影响状态页。

## 5. Codex 会话工具组策略

常开策略保持：

```text
core=true
testing=true
docs=true
其他=false
```

本流程实际依赖 `core` 中的：

```text
set_active_instance
refresh_unity
execute_menu_item
manage_asset
read_console
MCP resources: editor_state / menu-items / instances
```

`core` 只能确认资产存在、类型和 GUID，不能读取 TextureImporter 全字段或 ScriptableObject 的 Entries。进入 `UNITY_IMPORT` 前，Agent按需临时启用：

```text
scripting_ext=true
```

仅使用其中的只读 `execute_code`，对本次 VisualID 返回 importer 和 live Registry 的结构化快照；证据完成后立即恢复：

```text
scripting_ext=false
```

不启用 `ui`。`testing` 和 `docs` 可以保持会话常开，但本流程不运行测试，也不以测试或 API 文档查询替代导入事实。`asset_gen`、`vfx`、`profiling` 与本链路无关，保持关闭。

## 6. 证据结构

每次执行使用独立 ArtImportRunID：

```text
UnityClient/Logs/P3ArtImport/<ArtImportRunID>/
  request.json
  approved-plan.json
  approved-sync.json
  unity-import.json
  registry-result.json
  console-baseline.json
  console-delta.json
  generated-writeback.json
  summary.json
```

ArtImportRunID 是一次接入执行证据，不是第二套项目进度表。若它由一个 P3ArtProduction 生产任务触发，生产 `summary.json` 只引用该 ArtImportRunID 和最终 claim，不复制整套证据。

### 6.1 `request.json`

至少记录：

```json
{
  "schema": "p3-art-import-request@1",
  "art_import_run_id": "art_import_20260718_zero_portraits_01",
  "mode": "interactive",
  "unity_instance": "UnityClient@c0741596",
  "visual_ids": ["doll_zero_dialogue_neutral"],
  "permissions": {
    "allow_approved_sync": false,
    "allow_existing_target_overwrite": false,
    "allow_new_approved_target": false
  },
  "scope": {
    "include": ["approved_sync", "unity_import", "sprite_registry"],
    "exclude": ["runtime_binding", "playmode", "art_acceptance", "program_regression"]
  }
}
```

### 6.2 `approved-plan.json`

逐项记录：

- VisualID、ProductionProfile、selected 实际来源与 SHA-256；
- OutputPath、目标是否存在、`.meta` 是否存在、当前 GUID；
- 新建、同路径覆盖或无需复制；
- Manifest / OutputPath 冲突；
- Approved 全局 basename 冲突；
- 是否需要人工授权；
- `Sync-ApprovedArt.ps1 -DryRun` 的归一化结果。

### 6.3 `unity-import.json`

逐项记录 live Editor 结果：

```json
{
  "visual_id": "doll_zero_dialogue_neutral",
  "asset_path": "Assets/Art/Approved/Dolls/doll_zero_dialogue_neutral.png",
  "asset_database_guid": "...",
  "meta_guid": "...",
  "main_asset_type": "UnityEngine.Texture2D",
  "sprite_loaded": true,
  "source_width": 1024,
  "source_height": 1536,
  "importer": {
    "texture_type": "Sprite",
    "sprite_import_mode": "Single",
    "alpha_is_transparency": true,
    "max_texture_size": 2048,
    "filter_mode": "Bilinear",
    "mipmap_enabled": false
  },
  "status": "passed"
}
```

Importer 预期值必须从 Manifest / Asset Contract 和现有分类规则得出，不得只按 VisualID 前缀猜测。

### 6.4 `registry-result.json`

逐项记录：

- live `VisualAssetRegistry.asset` 的路径和 GUID；
- 原始 Entries 中该 VisualID 的匹配数量；
- `TryGetEntry` 是否成功；
- 返回 Sprite 的 AssetPath 和 GUID；
- 是否与本次 Approved 目标完全一致；
- 重复 VisualID、空 Sprite、错误路径或错误 GUID；
- Registry 重建前后目标变化；
- 全局重复 / 陈旧条目只作为独立诊断，不混入目标通过结论。

`registered` 的硬条件是目标 VisualID 在原始 Entries 中匹配数量恰好为 1，且 live lookup 指向预期 Sprite。由于现有重建菜单不会主动删除陈旧条目，不能用 `TotalEntries` 或 `EntriesUpdated` 代替目标检查。

## 7. 标准执行状态机

```text
REQUEST_NORMALIZE
-> SOURCE_AND_PERMISSION_AUDIT
-> APPROVED_PLAN
-> APPROVED_GATE
-> APPROVED_SYNC
-> UNITY_INSTANCE_LOCK
-> CONSOLE_BASELINE
-> UNITY_REFRESH
-> WAIT_UNITY_IDLE
-> IMPORT_PRE_SNAPSHOT
-> REGISTRY_REBUILD
-> WAIT_UNITY_IDLE
-> IMPORT_POST_SNAPSHOT
-> REGISTRY_LIVE_CHECK
-> DISPLAY_SPEC_VALIDATE
-> CONSOLE_DELTA
-> GENERATED_WRITEBACK
-> REGISTERED
```

`WAIT_UNITY_IDLE` 是内部基础设施步骤，不是用户可见状态。它检查：

- 目标实例仍连接且没有歧义；
- 未进入 PlayMode 切换；
- `is_compiling=false`；
- `is_domain_reload_pending=false`；
- `assets.is_updating=false`；
- `refresh.is_refresh_in_progress=false`。

`editor_state.advice.ready_for_tools=false` 且仅因为短暂 `stale_status` 时，按 MCP 建议做短轮询和一次刷新；超过重试预算后输出 infrastructure limited，不把它包装成美术业务门禁。

## 8. 详细执行流程

### 8.1 请求与预检

1. 接收一个或多个明确 VisualID，读取 Manifest、Profile 工作区、selected 决策和 OutputPath。
2. 建立 ArtImportRunID 和 `request.json`，锁定模式、权限、实例和本次范围。
3. 运行 Approved 预检与 `Sync-ApprovedArt.ps1 -DryRun`。
4. 检查所有 Approved 文件 basename 是否唯一。现有 Registry 重建按文件名生成 VisualID，并扫描整个 Approved；任意同名路径冲突都可能产生非局部结果，因此目标冲突必须阻断，非目标冲突也必须在调用全局重建前解决或显式缩小方案。
5. 写 `approved-plan.json`，在任何正式文件变化前给出准确计划。

### 8.2 Approved 人工门禁

交互模式在以下情况暂停：

- 首次核心角色正式进入 Approved；
- 覆盖已有 Approved；
- 创建新的 Approved 路径；
- `.meta` 缺失、GUID 风险或目标路径异常；
- Manifest VisualID / OutputPath / selected 来源冲突；
- 全局 Approved basename 冲突。

用户已提前明确授权全自动时，可自动处理授权范围内的新建或同路径覆盖；仍不得跳过事实冲突、路径越界、`.meta` / GUID 失败或 basename 冲突。

### 8.3 Approved 正式同步

1. 调用现有 `Sync-ApprovedArt.ps1`，精确传入 VisualID 和所需授权参数。
2. 对每个目标比较 selected SHA-256 与 Approved SHA-256。
3. 已有 `.meta` 的目标比较同步前后字节和 GUID；同 VisualID 替换必须完全不变。
4. 写 `approved-sync.json`。同步失败时停止，不调用 Unity 菜单。

### 8.4 Unity 导入与 Registry

1. `set_active_instance` 锁定 request 中的精确 `Name@hash`。
2. 读取 `editor_state` 和 Console baseline。
3. `refresh_unity(scope=assets, mode=force, compile=none, wait_for_ready=true)`。
4. 执行内部 `WAIT_UNITY_IDLE`。
5. 临时启用 `scripting_ext`，只读采集本次目标的 pre-snapshot，确认路径、live GUID、Texture2D、Sprite 和 importer 可访问。
6. 执行 `Tools/P3 Art/Rebuild Approved Sprite Registry`。该菜单会统一静态 Approved Sprite importer 并重建 / 更新 Registry。
7. 再次刷新并等待 Unity 空闲。
8. 采集 post-snapshot。以 post-snapshot 判定最终 `unity_imported`，因为 Registry 菜单中的 `EnsureApprovedSprites` 可能修正 importer 并触发 reimport。
9. 采集 live Registry 目标快照，验证唯一条目、lookup、Sprite 路径和 GUID。
10. 执行 `Tools/P3 Art/Validate Approved Display Specs`，记录菜单总结和相关警告。
11. 读取 Console after，按 baseline 去重形成 `console-delta.json`。
12. 关闭 `scripting_ext`，恢复会话工具组。

全局 DisplaySpec 菜单会扫描整个 Approved。与本次 VisualID 无关的已有 warning 进入 `global_preexisting_warnings`，不直接把本次目标判失败；任何本次 VisualID warning、新增 error、异常堆栈或导入失败都会阻断 `registered`。

### 8.5 写回与完成

只有 `unity-import.json` 和 `registry-result.json` 对每个目标都通过，且 Console 没有本次新增错误时：

1. 将对应 Manifest entry 的 `RegistryStatus` 更新为 `registered`，主 `Status` 保持 `approved`。
2. 刷新 `Generate-ArtIntegrationCandidates.ps1`。
3. 刷新 `Generate-ArtRegistryGapChecklist.ps1`；若程序交接清单是当前流程的直接消费者，再刷新 `Generate-ArtProgramHandoff.ps1`。
4. 写 `generated-writeback.json`，记录 owning script、输出文件和输入指纹。
5. 写 `summary.json`，最终 claim 为 `registered`。
6. 回写 `agent_status/art.md`；仅当 Unity / Registry 跨职能交接状态实质变化时更新 `agent_status/program.md`。只有项目级阻塞或交接变化才更新 `PROJECT_STATUS.md`。

生成清单可能把已登记素材路由为 `acceptance_needed`。这表示下一阶段可做运行时绑定 / 验收，不表示本次“素材在 Unity 中可用”的任务失败。

## 9. 新增脚本编排入口

实现阶段新增一个薄编排入口：

```text
tools/美术工具/Invoke-ArtApprovedUnityRegistration.ps1
tools/美术工具/art_approved_unity_registration.py
```

PowerShell 只提供稳定 CLI，Python 负责 JSON schema、指纹、预检、证据合并和 Manifest `RegistryStatus` 写回。建议阶段：

```powershell
# 只生成计划和证据目录，不改 Approved
.\tools\美术工具\Invoke-ArtApprovedUnityRegistration.ps1 `
  -Phase Plan `
  -VisualID doll_zero_dialogue_neutral `
  -ArtImportRunID art_import_20260718_zero_portraits_01

# 通过门禁后调用现有 Sync-ApprovedArt
.\tools\美术工具\Invoke-ArtApprovedUnityRegistration.ps1 `
  -Phase SyncApproved `
  -ArtImportRunID art_import_20260718_zero_portraits_01 `
  -AuthorizeApprovedSync

# MCP 证据齐全后校验、回写与刷新生成清单
.\tools\美术工具\Invoke-ArtApprovedUnityRegistration.ps1 `
  -Phase Finalize `
  -ArtImportRunID art_import_20260718_zero_portraits_01
```

该入口不尝试从 PowerShell 直接调用 MCP，也不复制 `Sync-ApprovedArt.ps1` 的文件逻辑。MCP 调用由 Agent 在 `SyncApproved` 和 `Finalize` 之间按本设计编排。`Finalize` 必须验证证据 schema、VisualID 集合、路径、GUID、hash、实例和时间顺序一致，不能信任一份手写 `registered=true`。

## 10. 幂等与恢复

- `Plan` 可重复运行，不改 Approved、Manifest 或 Registry。
- 同一 ArtImportRunID 只能绑定同一 VisualID 集合、Manifest 指纹、selected hash 和实例；输入变化后必须新建 RunID。
- `SyncApproved` 已成功而 Unity 不可用时，不自动回滚 Approved；状态保持 `approved`，从 `UNITY_INSTANCE_LOCK` 恢复。
- Unity 已导入而 Registry 检查失败时，不手改 `VisualAssetRegistry.asset` YAML；解决冲突后重跑现有 Registry 菜单和 live 检查。
- Registry 已正确登记但 Console 采集受限时，结果标记 `limited`，不得直接写 `RegistryStatus=registered`；补齐 Console 证据后可原 RunID 恢复。
- 同 VisualID 同路径替换在 GUID 未变时可安全重跑；新目标重跑不得隐式获得第一次没有授予的覆盖权限。
- `Finalize` 重跑若发现 Manifest 已为 `RegistryStatus=registered` 且证据完全一致，应返回幂等成功，不重复追加 Notes。

## 11. 失败分类

| 代码 | 含义 | 恢复点 |
|---|---|---|
| `blocked:approved_authorization_required` | 需要首次核心资产、覆盖或新路径授权 | `APPROVED_GATE` |
| `blocked:approved_visualid_collision` | Approved 中存在 basename / VisualID 冲突 | `APPROVED_PLAN` |
| `failed:approved_sync` | selected、OutputPath、复制、hash 或 `.meta` guard 失败 | `APPROVED_SYNC` |
| `limited:unity_instance_unavailable` | 目标 Unity 实例未连接或无法唯一锁定 | `UNITY_INSTANCE_LOCK` |
| `limited:unity_not_idle` | 超过等待预算仍编译、刷新或状态陈旧 | `WAIT_UNITY_IDLE` |
| `failed:unity_import_mismatch` | live 类型、GUID、Sprite 或 importer 不符合合同 | `IMPORT_POST_SNAPSHOT` |
| `failed:registry_missing` | live Registry 无目标 VisualID | `REGISTRY_LIVE_CHECK` |
| `failed:registry_duplicate` | 原始 Entries 中目标 VisualID 不唯一 | `REGISTRY_LIVE_CHECK` |
| `failed:registry_asset_mismatch` | Registry 指向错误路径或 GUID | `REGISTRY_LIVE_CHECK` |
| `failed:console_delta` | 本次新增 error、异常或目标相关 warning | `CONSOLE_DELTA` |
| `limited:console_delta_unavailable` | 无法取得可靠 Console delta | `CONSOLE_DELTA` |

失败和 limited 都必须保留已完成阶段证据和准确 resume state，不得把部分成功扩大声明为 `registered`。

## 12. 已发现的现状风险与设计处理

### 12.1 Editor state 可能只因 `stale_status` 不 ready

当前实例曾出现 `ready_for_tools=false`、`blocking_reasons=["stale_status"]`，同时实际未编译、未更新。设计将其作为短暂基础设施状态处理：刷新、短轮询、有限重试；不新增 Unity Readiness Gate 业务状态。

### 12.2 core 工具不能读取 importer 和 Registry Entries

`manage_asset` 可以确认 live AssetDatabase 路径、类型和 GUID，但不能读取 TextureImporter 细节，也不能展开 `VisualAssetRegistry` ScriptableObject。按需启用 `scripting_ext` 是在“不改 Editor 侧代码”前提下完成 live 精确检查的最小方案。

### 12.3 Registry 重建是全局菜单

现有菜单扫描整个 `Assets/Art/Approved`，按文件名生成 VisualID，并且只新增 / 更新，不删除陈旧条目。设计因此增加 Approved basename 预检、目标唯一性检查和重建前后 live 快照，禁止用菜单调用成功或总条目数作为完成证据。

### 12.4 DisplaySpec 校验也是全局扫描

现有菜单可能输出与本次无关的历史 warning。设计区分目标失败、新增 Console delta 和全局既有问题，避免一个无关旧 warning 永久阻断单资产登记，同时保留全局风险证据。

### 12.5 工具 README 有旧候选解析描述

当前 `tools/美术工具/README.md` 的 `Sync-ApprovedArt` 段仍描述平坦 processed 和“取第一张”的旧口径，而脚本与 active 美术规范已经使用共享数字轮次 Resolver。实现本方案时应同步修正文档，但不得借机改写无关美术流水线事实。

## 13. 测试与验收

### 13.1 脚本单元测试

- Plan 只读且生成稳定 request / approved-plan schema。
- VisualID、OutputPath、selected hash 和 `.meta` / GUID 预检。
- Approved basename 冲突检测。
- 新目标、已有目标、同 VisualID 替换的权限矩阵。
- Finalize 拒绝缺文件、旧时间戳、VisualID 集合不一致、错误实例、GUID 不一致和伪造状态。
- Finalize 幂等，不重复写 Manifest Notes。

### 13.2 Unity MCP 集成测试

- 锁定唯一实例并完成 refresh / idle。
- 新 Approved Sprite 在 live AssetDatabase 中可加载为 Texture2D 和 Sprite。
- Registry 菜单执行后目标 VisualID 唯一指向预期 Sprite。
- 同 VisualID 替换前后 `.meta` / GUID 不变。
- importer 不合规时经现有菜单修正，post-snapshot 符合合同。
- Registry 重复、错误引用和缺失均不能写 `RegistryStatus=registered`。
- Console 中本次新增 error 会阻断完成。
- 流程结束后 `scripting_ext=false`，其他会话工具组不被意外开启。

### 13.3 端到端试点

首个正式试点使用已完成 selected 的 `zero_dialogue_portrait_v1`，先以一个 VisualID 验证新建 Approved、Unity import 和 Registry，再扩展到余下成员。首次核心角色 Approved 必须经过用户授权；试点最大声明为 `registered`，不包含 runtime binding 或运行时画面通过。

## 14. 完成口径

实现完成必须满足：

1. Agent 能从一个或多个 selected VisualID 建立 ArtImportRunID 和 Approved dry-run 计划。
2. 所有正式文件变化仍由 `Sync-ApprovedArt.ps1` 完成。
3. live Unity 证据能证明路径、类型、GUID、Sprite 和 importer 正确。
4. live Registry 证据能证明每个目标 VisualID 唯一指向预期 Sprite。
5. Console delta 不含本次新增阻塞错误。
6. Manifest 主 `Status=approved`，接入轴为 `RegistryStatus=registered`。
7. 接入和 Registry 生成清单已由 owning scripts 刷新。
8. 没有创建 Prefab / UGUI 绑定、进入 PlayMode 或运行 ArtAcceptance。
9. 工具组在流程后恢复为 core + testing + docs 常开、其余关闭。
10. 文档、脚本测试、Unity MCP 试点和状态回写均有新鲜证据。
