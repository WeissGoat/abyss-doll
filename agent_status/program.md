---
id: agent_status_program
title: 程序 / Unity 状态
type: status
role: 程序
domain: unity_programming
status: active
source_of_truth: true
related:
  - 版本规划/0-12小时细案/T0-01_序章首次循环开发总方案.md
  - 版本规划/0-12小时细案/T0-01A_开局人偶状态到首次下潜许可开发方案.md
  - 版本规划/09_正式版核心纵切开发路线.md
  - agent_status/README.md
  - PROJECT_STATUS.md
  - 开发文档/rules/01_客户端分层与领域架构规范.md
  - 开发文档/rules/00_程序开发总规则.md
  - 开发文档/00_程序开发大纲.md
  - 开发文档/15_P0配置Validator与自动验收底座需求.md
  - 开发文档/16_程序主流程闭环与架构收口推进计划.md
  - 设计文档/GDD/GDD_00_系统关联总图.md
  - agent_status/design.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - agent_status/art.md
  - 知识库/views/program.md
  - 美术文档/16_Live2D角色动画资产接入规格.md
  - 美术文档/17_Agent原生动态立绘资产接入规格.md
  - 开发文档/17_Live2DSpine运行时接入评估.md
  - 开发文档/18_全局叙事播放系统开发方案.md
  - 配置表(JSON)/Narrative/README.md
last_verified: 2026-10-02
update_rule: 程序、Unity、验证或工程边界任务完成后更新本文档。
---

# 程序 / Unity 状态

## 最后更新

2026-10-01

## 当前关注

- 按完整玩家结果推进：运行时 UI 保持纯 UGUI，游戏规则留在后端 / 领域服务，配置源来自 `配置表(JSON)`。
- 程序与美术验证分两条 lane；程序验证只负责编译、配置、Smoke、P0 / T0 功能路径，不替代美术验收；里程碑发布门禁按 `开发文档/rules/04` 的检查清单。
- T0-01A 的 NARR-00..04 与 T0-FLOW-01..05 已有程序承接和 headless / smoke 证据；当前重点是 `SEAL-FLOW`、`SEAL-UI`、FormalV2 工坊 zone、首潜许可卡和连续截图防回归。

## 最近完成

- smoke 测试已包装为 Unity Test Framework 的 EditMode 用例（`P3.SmokeTests.Editor`，`cc1c6f2`），可用 Unity MCP `run_tests` 按全部、分类或名称运行；2026-10-02 全量 66 个用例 56 个通过，失败见“问题 / 阻塞”。
- Claude Code 已通过仓库根目录 `.mcp.json` 接入 `unityMCP`（与 Codex 同一命令），服务端启动和 46 个内置工具已验证；`p3_*` 自定义工具待 Unity Editor 运行时复核（`validation_limited:unity_editor_not_running`）。
- P3 MCP 工具参数 schema 已修复（`9133477`），带参数直调已通过；`execute_custom_tool` 只作诊断后备。
- 静态 Approved Sprite 的 Unity 导入与 Registry 登记已有脚本 + MCP 编排入口，live 证据齐全才写 `RegistryStatus=registered`；Windows PowerShell 5.1 中文路径乱码已修复。
- ArtRun 内部 binding gate 已接入 validation core：`finalize_runtime_validated` 只在最终 capture 和 Agent review 通过后写运行时证据；真实 Prefab / UGUI 绑定仍归程序。
- 已 `registered` 的美术资源：`bg_combat_abyss`、`ui_icon_warning`、`bg_dungeon_map`、`ui_icon_locked`、`ui_icon_money`、Zero neutral 与三张姿势差分；均不含 UGUI 消费或 `runtime_validated`。
- AI 图片网关：透明 SSE、尾部断流保护、同连接 JSON fallback 与 Gemini Flow2API 结构化画幅（`1024x1536` → `three-four + 2k`）已真实验证。
- 全局叙事播放系统已接入配置加载、触发调度、状态回写、UGUI 对白层、过程 CG 容器和命令桥；运行时仍需按场景补证据。
- T0-01A 启动零号、苏醒对白、首潜确认和真实 `DungeonManager.StartRunAtLayer(1)` 链路已有实现 / smoke 证据，不等于完整 T0 验收。

## 下一步建议

- 按 T0-01A 开发方案补 `SEAL-FLOW-01`、`SEAL-UI-01..03`、`SEAL-VAL-01`，优先验证正常玩家 UI 可达、状态真实变化和固定截图语义。
- 修复 10 个已知失败的 smoke 用例，或确认断言已过时后更新测试。
- 修改系统契约时同步开发文档、Validator、测试和受影响状态页；需要运行时画面时转 `p3-art-validation`。

## 问题 / 阻塞

- 10 个 smoke 用例已知失败（2026-10-02 全量 `run_tests`；同日旧 AutoTestDaemon 全量和 2026-08-02 旧框架证据里同样失败，不是包装引入）。修复或更新过时断言前，全量结果不能当作程序通过证据：
  - Edit Mode 下调用 `Destroy`：`PrologueFirstDiveDepartureSmokeTest`、`PrologueFirstDiveLayerConfirmSmokeTest`、`PrologueFirstDivePermissionSmokeSuite`、`PrologueHalfOpenNarrativeFlowSmokeTest`。
  - 断言与当前 UI、文案或数值不符：`CombatHUDIntentBindingSmokeTest`、`CombatOutcomeReportSmokeTest`、`CombatReadabilityTextServiceSmokeTest`、`MonsterIntentPreviewServiceSmokeTest`、`WorkshopSmokeTest`。
  - 验证命令被拒时打出 Error 日志：`NarrativeCommandBridgeSmokeTest`。
- Unity Editor / MCP 现场验证仍可能受环境和既有 MCPForUnity 版本 warning 限制；受限时必须记录 `validation_limited:*`。
- T0-01A 的商业化连续画面、最终美术接入和专业 Role 外部验收未完成。
- Gemini 双参考图在长提示词或生成超过约 300 秒时可能被上游断流；外部 provider 稳定性不扩大声明。

## 关键证据入口

- `开发文档/00_程序开发大纲.md`、`开发文档/18_全局叙事播放系统开发方案.md`
- `开发文档/19_UnityMCP验收编排层设计.md`、`开发文档/20_UnityMCP验收编排层实现计划.md`
- `UnityClient/Assets/Editor/P3Validation/art_validation_targets.json`
- `版本规划/0-12小时细案/T0-01A_开局人偶状态到首次下潜许可开发方案.md`
- `配置表(JSON)/Narrative/README.md`
- `tools/ai-image-gateway/docs/openai_compatible_relay_integration.md`
- `tools/agent/README.md`（健康检查与 Unity MCP 客户端配置）
