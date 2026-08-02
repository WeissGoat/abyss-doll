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
last_verified: 2026-08-02
update_rule: 程序、Unity、验证或工程边界任务完成后更新本文档。
---

# 程序 / Unity 状态

## 最后更新

2026-08-02

## 当前关注

- 程序正式版按完整玩家结果推进，运行时 UI 保持纯 UGUI，游戏规则留在后端 / 领域服务，配置源来自 `配置表(JSON)`。
- `p3-program-validation`、`p3-art-validation`、`p3-release-validation` 已分离；程序验证只负责编译、配置、Smoke、P0 / T0 功能路径，不替代美术验收或发布聚合。
- T0-01A 的 NARR-00..04 和 T0-FLOW-01..05 已有程序承接与 headless / smoke 证据；当前重点是 `SEAL-FLOW`、`SEAL-UI`、FormalV2 工坊 zone、首潜许可卡和连续截图防回归。
- AI 图片网关 chat image provider 已支持请求级透明 SSE、完整图片事件后的尾部断流保留和同连接 JSON fallback；GIF 客户端已显式启用 stream、规范化 provider 输出尺寸，并把 `incomplete chunked read` 纳入瞬时重试。

## 最近完成

- `art_import_formalv2_standard_ui_batch_20260801_01` completed the protected integration of three standard art assets: Approved replacement preserved `.meta` / GUID, live Unity AssetDatabase import, Sprite DisplaySpec, Registry uniqueness, and Console delta all passed. The claim is `registered`; it does not include Prefab / UGUI binding, runtime consumption, or `runtime_validated`.
- `Select-ArtCandidate.ps1` now updates nested batch summaries by `ProductionRunID`, preventing a stale `review_required` summary after `selection-decision.json` is complete; regression coverage is 13/13 passing.

- `art_import_formalv2_catalog_v2_bridge_batch_20260802_01` 已把 `bg_combat_abyss` 与 `ui_icon_warning` 的最新 `processed/5` selected 完成 Approved -> live Unity import -> Registry 复核：Unity MCP 实例为 `UnityClient@c0741596`，两项 importer 的尺寸、Sprite/Single、alpha、MaxSize、Bilinear 和 mipmap 均符合合同；Registry 原始匹配数均为 1、`TryGetEntry=true`，路径/GUID 分别保持 `9f676444d813871408a98a32295d2790` 与 `d39849aa2b024a80adabac006c44a720`；目标 Console error / warning 为 0。当前状态为 `registered`，不含 UGUI 绑定、PlayMode、ArtAcceptance 或 `runtime_validated`。
- `doll_zero_dialogue_neutral` 已用 live Unity MCP 完成首次真实静态 Sprite 接入：AssetDatabase GUID `7d7b3a5d2f28634469b19bb5aa52664b`，Registry 原始匹配数为 1 且 `TryGetEntry=true`，目标 Console error / warning 为 0；当前程序边界仅为 VisualID 可登记，不含 Prefab / UGUI 消费。
- `art_import_zero_pose_variation_20260801_03` 已完成三张 Zero 姿势差分的 Approved -> Unity import -> Registry：三项 Unity GUID 与 Registry Sprite 路径一致，`match_count=1` / `TryGetEntry=true`，importer 为 1024x1536 Sprite、透明、Bilinear、无 Mipmap，目标 Console error/warning 为 0。该 Run 仅声明 `registered`，不包含 Prefab / UGUI 绑定或 `runtime_validated`。
- Finalize 编排已修复 Windows PowerShell 5.1 中文生成物路径乱码：标准生成物路径移入 Python 默认值，失败 Run 可幂等重跑并完成 `summary.claim=registered`。
- 静态 Approved Sprite 的 Unity 导入与 Registry 登记已增加脚本 + MCP 编排入口：live AssetDatabase / importer / Registry / Console 证据齐全后才写 `RegistryStatus=registered`；该能力不创建 Prefab / UGUI 绑定，也不扩大为程序功能或运行时美术通过。
- `registered -> runtime_validated` 的 ArtRun 内部 binding gate 已接入 validation core：`p3_art_inspect_target` 记录实际 UGUI Sprite、Approved 路径/GUID 与 Registry 对照，`finalize_runtime_validated` 只在最终 capture 和 Agent review 通过后写运行时证据；程序仍负责真实 Prefab / UGUI 绑定，不新增 `runtime_bound` 状态。
- 全局叙事播放系统已接入配置加载、触发调度、状态回写、UGUI 对白层、过程 CG 容器和命令桥，运行时仍需按场景补验证证据。
- Unity MCP 验收编排已拆分为程序、美术和发布 lanes；程序 lane 不再自动启动 ArtAcceptance。
- T0-01A 的启动零号、苏醒对白、首潜确认和真实 `DungeonManager.StartRunAtLayer(1)` 链路已有实现 / smoke 证据，但不等于完整 T0 验收完成。
- `tools/ai-image-gateway` 已完成透明流式接入与尾部断流保护，全量测试 `123 passed`；GIF 工具测试 `50 passed, 2 subtests passed`。真实 Gemini 双参考图简洁提示词 smoke 在 `66.688s` 成功，8 个 SSE 事件、首事件 `1.719s`，并完成 8 帧 GIF Run 重编码。

## 下一步建议

- 按 T0-01A 开发方案补 `SEAL-FLOW-01`、`SEAL-UI-01..03` 和 `SEAL-VAL-01`，优先验证正常玩家 UI 可达、状态真实变化和固定截图语义。
- 继续保持程序验证与美术验证分离；需要运行时画面时使用对应的 `p3-art-validation` 或 `p3-release-validation` 路由。
- 修改系统契约时同步开发文档、Validator、测试和受影响状态页；不要把运行时副本或截图当作配置源。

## 问题 / 阻塞

- 美术运行时验收试跑发现 Target Registry 尚不能承接正式 `runtime_validated`：`combat_hud / maintenance_panel` 未注册，现有四个 TargetID 的 `required_visual_ids` 全为空，Finalize 必然缺少运行时绑定合同。另有 MCP 暴露问题：`p3_art_*` 第一类工具 schema 被生成为空对象，带参数直调会被客户端拒绝；通用 `execute_custom_tool` 可成功启动 ArtRun，说明 Unity C# 工具合同本身可用。需要在程序侧补目标/VisualID 合同并修复工具 schema 暴露后恢复 `art_validation_flow_probe_20260802_01`。
- Unity Editor / MCP 现场验证仍可能受环境和既有 MCPForUnity 版本 warning 限制；受限时必须记录 `validation_limited:*`。
- T0-01A 的商业化连续画面、最终美术接入和专业 Role 外部验收仍未完成。
- Gemini 双参考图在长提示词或生成超过约 300 秒时仍可能由上游关闭 chunked response；简洁提示词已取得端到端成功，但外部 provider 稳定性仍不应扩大声明。

## 关键证据入口

- `开发文档/00_程序开发大纲.md`
- `开发文档/18_全局叙事播放系统开发方案.md`
- `开发文档/19_UnityMCP验收编排层设计.md`
- `开发文档/20_UnityMCP验收编排层实现计划.md`
- `UnityClient/Logs/P3ArtImport/art_import_zero_dialogue_neutral_plan_20260718_02/summary.json`
- `UnityClient/Logs/P3ArtImport/art_import_formalv2_standard_stylebatch_20260725_01/summary.json`
- `docs/superpowers/specs/2026-07-18-ai-image-gateway-transparent-streaming-design.md`
- `tools/ai-image-gateway/docs/openai_compatible_relay_integration.md`
- `版本规划/0-12小时细案/T0-01A_开局人偶状态到首次下潜许可开发方案.md`
- `配置表(JSON)/Narrative/README.md`
