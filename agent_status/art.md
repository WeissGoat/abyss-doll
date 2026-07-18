---
id: agent_status_art
title: 美术 / UI 状态
type: status
role: 美术
domain: art_pipeline
status: active
source_of_truth: true
related:
  - 版本规划/09_正式版核心纵切开发路线.md
  - agent_status/README.md
  - PROJECT_STATUS.md
  - 开发文档/00_程序开发大纲.md
  - 开发文档/14_Unity运行时美术自动验收方案.md
  - 开发文档/rules/03_视觉资源系统程序开发规范.md
  - agent_status/program.md
  - 设计文档/GDD/GDD_00_系统关联总图.md
  - agent_status/design.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - 美术文档/05_AI图片网关接入方案.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
  - 美术文档/ui_design/ui_iteration_process.md
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/README.md
  - 知识库/views/art.md
  - 美术文档/16_Live2D角色动画资产接入规格.md
  - 美术文档/17_Agent原生动态立绘资产接入规格.md
  - 美术文档/18_CG底图与漫画式播放演出工作流.md
  - 美术文档/19_T0-01序章CG细案.md
  - 美术文档/20_GIF小循环人物替换工作流.md
  - .codex/skills/p3-generate-image/SKILL.md
  - .codex/skills/p3-art-asset-production/SKILL.md
  - .codex/skills/p3-narrative-cg-comic/SKILL.md
  - 版本规划/0-12小时细案/T0-01_序章首次循环开发总方案.md
  - 美术文档/人设/README.md
  - 美术文档/人设/01_人设参考获取规则.md
  - 美术文档/人设/02_零号原型参考_失明少女.md
  - 美术文档/人设/03_零号初版人设方案.md
  - 美术文档/人设/04_零号AI后端出图提示词对比.md
  - 美术文档/人设/05_零号立绘素材设计与交付清单.md
  - 开发文档/17_Live2DSpine运行时接入评估.md
  - 开发文档/18_全局叙事播放系统开发方案.md
last_verified: 2026-07-18
update_rule: 美术或 UI 视觉流水线任务完成后更新本文件。
---

# 美术 / UI 状态

## 最后更新

2026-07-18

## 当前关注

- 美术生产已按 `standard_asset`、`character_portrait_set` 和 `_legacy_runs` 分 Profile 管理；工具、Manifest、Approved 和 Unity 接入必须沿同一 Profile 路由。
- 首个 `character_portrait_set` 试跑当前停在 `decision_required`：候选身份和尺寸基本成立，但自动去白底误删白布、皮肤和浅色裙面，尚未进入 selected / Approved。
- 正式 UI / 素材接入仍以 active `screen_layouts.json`、Manifest、程序交接清单和 ArtAcceptance 为准；静态预览、contact sheet 或生成物不能替代运行时证据。
- T0-01A 的 CG / 漫画页和 FormalV2 工坊素材继续按锚点、一致性、Approved、Unity 接入和运行时验收顺序推进。
- GIF 小循环人物替换继续使用 Gemini 图生图；客户端已具备透明 SSE，但双参考图仍需观察上游长响应完整性。

## 最近完成

- `_IncomingAI` 已迁移为 Profile 目录，现有 standard asset 工作区和历史目录均保留可追溯路径，Approved / Registry 文件哈希未改变。
- `p3-art-asset-production`、`generate-image`、`p3-narrative-cg-comic` 和 `p3-art-validation` 已完成职责分工；美术验收不再维护人工主美审批状态机。
- GIF 小循环人物替换工具已完成离线测试；图片网关透明 SSE 已实现，真实 Gemini 文生图在 `109.672s` 成功并于 `0.0s` 收到首事件，双参考图仍按受限证据处理。

## 下一步建议

- 为 `doll_zero_dialogue_neutral` 使用显式前景 mask / 保白去底方式，修复透明背景契约后从 `SELECTION_DECISION` 恢复生产；未通过前不改 Approved、Manifest、Registry 或 Unity。
- T0-01A 继续按角色、场景、风格锚点重做需要替换的 Panel，并在页级 contact sheet 通过后再进行同 VisualID 覆盖。
- 运行时 UI / 视觉问题优先走 `p3-art-validation`；正式资产准入走 `p3-art-asset-production`，纯生成 / 差分走 `generate-image`。

## 问题 / 阻塞

- 当前角色立绘阻塞为 `failed:transparent_background_contract_failed`；自动去白底不能直接处理白色服饰和眼罩。
- 外部图片 provider 仍可能出现超时或限流；受限时不得声明生成成功、视觉一致性通过或正式资产验收完成。
- 双参考图流式 smoke 在 `292.906s` 后由上游关闭不完整 chunked response，记录 `validation_limited:stream_request_failed_before_success_evidence`；未生成可解码图片，不推进 GIF 预审或正式资产状态。
- T0-01A 最终运行时漫画播放、ArtAcceptance 和外部美术验收仍未封板。

## 关键证据入口

- `UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_zero_dialogue_neutral/production_decision.json`
- `UnityClient/Logs/P3ArtProduction/zero_dialogue_neutral_pilot_20260718_01/summary.json`
- `docs/superpowers/specs/2026-07-18-ai-image-gateway-transparent-streaming-design.md`
- `美术文档/00_美术流水线总览.md`
- `美术文档/10_正式版核心纵切美术路线.md`
- `美术文档/18_CG底图与漫画式播放演出工作流.md`
- `美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md`
