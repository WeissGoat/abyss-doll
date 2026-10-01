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
  - 美术文档/人设/05_零号立绘素材设计与交付清单.md
  - 开发文档/17_Live2DSpine运行时接入评估.md
  - 开发文档/18_全局叙事播放系统开发方案.md
last_verified: 2026-10-01
update_rule: 美术或 UI 视觉流水线任务完成后更新本文件。
---

# 美术 / UI 状态

## 最后更新

2026-10-01

## 当前关注

- Zero 立绘套组 `zero_dialogue_portrait_v1`（14 人）：新 hurt 已在 `zero_hurt_repair_20260808_07_gemini_contract` 以 91 分 guarded replacement 到 selected，套组一致性已按当前指纹 Finalize；Approved / Unity / Registry 待授权同步。
- Request Catalog：308 个 canonical Request 严格校验通过；旧 PromptRevision 与当前 Requirement 指纹不符已失效，308 项均为 `prompt_authoring_required`，正式生成前必须重新作者化。
- Formal V2 通用批次：`bg_combat_abyss`、`ui_icon_warning`、`bg_dungeon_map`、`ui_icon_locked`、`ui_icon_money` 已到 `registered`；UI Skin `ui_button_primary` 停在 `selected`。
- T0-01A 的 CG / 漫画页与 FormalV2 工坊素材按锚点、页级一致性、Approved、Unity 接入、运行时验收的顺序推进。
- GIF 逐帧 `ACTION_TEXT` 边车已接入；Illya 8 帧样例因第 4 帧跳变和 `temporal_flicker` 停在 `review_required`。

## 最近完成

- `art_import_formalv2_catalog_v2_bridge_batch_20260802_01`：`bg_combat_abyss`、`ui_icon_warning` 的 `processed/5` selected 已同步 Approved 并完成 Unity / Registry 现场校验，`.meta` / GUID 不变，claim=`registered`。
- `art_import_formalv2_standard_ui_batch_20260801_01`：`bg_dungeon_map`、`ui_icon_locked`、`ui_icon_money` 完成同类接入，claim=`registered`。
- `art_import_zero_pose_variation_20260801_03`：Zero command_ready / hurt / tired 姿势差分（92 / 91 / 93）claim=`registered`；hurt 之后的新 selected 尚未同步 Approved。
- `doll_zero_dialogue_neutral`：首个 `character_portrait_set` 正式接入，claim=`registered`。
- 生产安全收口：v2 证据隔离、Registrar 重算技术评审、selected 替换基线与保护维度门禁、立绘 Automatic / Resume、Approved 同步强制当前套组评审；美术工具全量回归 295/295。

## 下一步建议

- 授权后按当前套组指纹执行 Zero hurt 的 Approved → Unity → Registry；其余 10 个停在 selected 的 Zero 成员逐个授权，不把单项授权扩大为整套准入。
- 继续按 background / icon / standard_asset 分组扩大 Formal V2 通用批次：先作者化 PromptRevision，再 dry-run、provider smoke、Agent 评审和 guarded selection；UI Skin 与角色立绘走各自独立 route。
- 程序实际消费某个 VisualID 后，再用正式 TargetID 走 `p3-art-validation` ArtRun 推进到 `runtime_validated`。
- T0-01A 按角色、场景、风格锚点重做需替换的 Panel，页级 contact sheet 通过后再做同 VisualID 覆盖。

## 问题 / 阻塞

- 运行时验收缺目标合同：`art_validation_targets.json` 只有 4 个 TargetID 且 `required_visual_ids` 为空，未注册 `combat_hud / maintenance_panel`；`art_validation_flow_probe_20260802_01` 停在 `AwaitingLiveInspection`，需程序补合同后恢复。
- 离线 Registry candidate 有既有 `changed_existing=22`（T0 CG / 工坊 / Zero 已登记条目），需单独刷新；不影响 live `registered` 证据。
- `Generate-ArtIntegrationCandidates` 未把 Approved 状态下的新 selected 标为 replacement-ready，同名主动迭代仍依赖 ProductionRun 证据。
- 外部 provider 可能超时或限流；受限时记录 `validation_limited:*`，不得声明出图成功。
- T0-01A 最终运行时漫画播放、ArtAcceptance 与外部美术验收未封板。

## 关键证据入口

- 生产 / 导入证据（本地）：`UnityClient/Logs/P3ArtProduction/<ProductionRunID>/`、`UnityClient/Logs/P3ArtImport/<ArtImportRunID>/`，RunID 见上文。
- `美术文档/_generated/art_manifest.json`、`美术文档/_generated/art_generation_requests.json`
- `美术文档/人设/05_零号立绘素材设计与交付清单.md`
- `美术文档/00_美术流水线总览.md`、`美术文档/10_正式版核心纵切美术路线.md`
- `美术文档/18_CG底图与漫画式播放演出工作流.md`、`美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md`
