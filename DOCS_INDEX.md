---
id: docs_index
title: 文档索引
type: index
role: 全局
domain: knowledge_base
status: generated
source_of_truth: false
related:
  - 知识库/README.md
last_verified: 2026-05-23
update_rule: 由 tools/docs/Generate-DocsIndex.ps1 生成，不手写维护。
---

# 文档索引

> 本文件由 `tools/docs/Generate-DocsIndex.ps1` 生成。不要手写维护。

## 概览

- 文档总数：292
- 已补元数据：288
- 缺少元数据：4
- 事实来源文档：196
- 关联边数：1529
- 跨职能关联：315
- `related` 只需单向填写；反向链接由本脚本写入 `docs_index.json` 的 `referenced_by`，关联数按双向合并统计。

## 事实来源

- [美术 / UI 状态](agent_status/art.md) - `status` / `art_pipeline`
- [策划 / 数值 状态](agent_status/design.md) - `status` / `design_balance`
- [游戏导演 / 制作人 状态](agent_status/director.md) - `status` / `game_direction_production`
- [程序 / Unity 状态](agent_status/program.md) - `status` / `unity_programming`
- [Project P3 智能体入口](AGENTS.md) - `entry` / `agent_workflow`
- [项目状态](PROJECT_STATUS.md) - `status` / `project_status`
- [文档维护与新增控制规则](rules/01_文档维护与新增控制规则.md) - `rule` / `document_governance`
- [智能体任务路由与完成协议](rules/02_智能体任务路由与完成协议.md) - `rule` / `agent_workflow`
- [全局 Agent Rules 入口](rules/README.md) - `entry` / `agent_rules`
- [智能体开工健康检查](tools/agent/README.md) - `tool` / `agent_workflow`
- [程序开发大纲与系统索引](开发文档/00_程序开发大纲.md) - `dev` / `program_architecture`
- [核心数据容器系统 (Core Data System)](开发文档/01_核心数据与实体容器(CoreData).md) - `dev` / `core_data`
- [网格背包与计算系统 (Grid System)](开发文档/02_网格背包与计算系统(GridSystem).md) - `dev` / `grid_inventory`
- [深渊与战斗循环系统 (Dungeon & Combat System)](开发文档/03_深渊与战斗循环(DungeonCombat).md) - `dev` / `dungeon_combat`
- [工坊与养成逻辑 (Workshop & Crafting System)](开发文档/04_工坊与养成逻辑(WorkshopSystem).md) - `dev` / `workshop`
- [表现层架构与事件总线 (View & EventBus System)](开发文档/05_表现层架构与事件总线(ViewAndEventBus).md) - `dev` / `presentation_layer`
- [奖励与掉落系统 (RewardSystem)](开发文档/10_奖励与掉落系统(RewardSystem).md) - `dev` / `reward_loot`
- [怪物 AI 与行动系统 (MonsterActionAI)](开发文档/11_怪物AI与行动系统(MonsterActionAI).md) - `dev` / `monster_ai`
- [程序开发优化建议与重构路线](开发文档/12_程序开发优化建议与重构路线.md) - `dev` / `program_refactor`
- [Unity 运行时美术自动验收方案](开发文档/14_Unity运行时美术自动验收方案.md) - `dev` / `runtime_art_validation`
- [P0配置Validator与自动验收底座需求](开发文档/15_P0配置Validator与自动验收底座需求.md) - `dev` / `config_validation`
- [程序主流程闭环与架构收口推进计划](开发文档/16_程序主流程闭环与架构收口推进计划.md) - `dev` / `main_flow_architecture`
- [Live2D / Spine 运行时接入评估](开发文档/17_Live2DSpine运行时接入评估.md) - `dev` / `live2d_character_animation`
- [全局叙事播放系统开发方案](开发文档/18_全局叙事播放系统开发方案.md) - `dev` / `narrative_playback`
- [Unity MCP 验收编排层设计](开发文档/19_UnityMCP验收编排层设计.md) - `dev` / `test_automation`
- [p3-art-validation V2 MCP 实时优先实现计划](开发文档/20_UnityMCP验收编排层实现计划.md) - `dev` / `runtime_art_validation`
- [开发文档归档目录入口](开发文档/archive/README.md) - `dev` / `program_docs_archive`
- [开发文档目录入口](开发文档/README.md) - `dev` / `program_docs_index`
- [程序开发总规则](开发文档/rules/00_程序开发总规则.md) - `dev` / `program_general_rules`
- [客户端分层与领域架构规范](开发文档/rules/01_客户端分层与领域架构规范.md) - `dev` / `client_architecture`
- [Unity 表现层与编辑器构建规范](开发文档/rules/02_Unity表现层与编辑器构建规范.md) - `dev` / `unity_presentation`
- [视觉资源系统程序开发规范](开发文档/rules/03_视觉资源系统程序开发规范.md) - `dev` / `visual_asset_system`
- [自动化测试与验收流程规范](开发文档/rules/04_自动化测试与验收流程规范.md) - `dev` / `test_automation`
- [程序开发规范目录入口](开发文档/rules/README.md) - `dev` / `program_rules_index`
- [全局与玩家实体定义 (Player & Global Entities)](开发文档/数据与实体定义/01_全局与玩家实体.md) - `dev` / `program_architecture`
- [人偶与状态实体定义 (Doll & Status Entities)](开发文档/数据与实体定义/02_人偶与状态实体.md) - `dev` / `program_architecture`
- [物品与网格实体定义 (Item & Inventory Entities)](开发文档/数据与实体定义/03_物品与网格实体.md) - `dev` / `program_architecture`
- [深渊与战斗实体定义 (Dungeon & Combat Entities)](开发文档/数据与实体定义/04_深渊与战斗实体.md) - `dev` / `dungeon_combat`
- [经济与社会实体定义 (Economy & Social Entities)](开发文档/数据与实体定义/05_经济与社会实体.md) - `dev` / `program_architecture`
- [基准价值与空间本位模型 (Space-Value Standard)](数值模型设计/00_基准价值与空间本位模型.md) - `balance` / `balance_space_value`
- [经济循环与通缩模型 (Economy & Deflation Model)](数值模型设计/01_经济循环与通缩模型.md) - `balance` / `balance_economy`
- [战斗伤害与生存公式 (Combat & Survival Formulas)](数值模型设计/02_战斗伤害与生存公式.md) - `balance` / `balance_combat`
- [深渊产出与掉落期望 (Loot Generation & Expectation)](数值模型设计/03_深渊产出与掉落期望.md) - `balance` / `balance_loot`
- [T0-01 序章首次循环](版本规划/0-12小时细案/T0-01_序章首次循环.md) - `plan` / `candidate_loop_detail_design`
- [T0-01 序章首次循环开发总方案](版本规划/0-12小时细案/T0-01_序章首次循环开发总方案.md) - `development_plan` / `prologue_first_loop_development`
- [T0-01A 开局人偶状态到首次下潜许可实现设计](版本规划/0-12小时细案/T0-01A_开局人偶状态到首次下潜许可实现设计.md) - `implementation_design` / `prologue_first_loop_implementation`
- [T0-01A 开局人偶状态到首次下潜许可开发方案](版本规划/0-12小时细案/T0-01A_开局人偶状态到首次下潜许可开发方案.md) - `development_plan` / `prologue_first_loop_development`
- [T0-VAL-01 T0-01A 运行时效果验收记录](版本规划/0-12小时细案/T0-VAL-01_最终实际效果优化文档.md) - `validation_effect_optimization` / `prologue_first_loop_runtime_acceptance`
- [T1 第一层搜打撤成形](版本规划/0-12小时细案/T1_第一层搜打撤成形.md) - `plan` / `candidate_loop_detail_design`
- [正式版核心纵切开发路线](版本规划/09_正式版核心纵切开发路线.md) - `plan` / `formal_vertical_slice`
- [纵切批次与需求文档承接矩阵](版本规划/11_纵切批次与需求文档承接矩阵.md) - `plan` / `vertical_batch_requirement_coverage`
- [正式版全局体验总线与全系统宏观大纲](版本规划/13_正式版全局体验总线与开放节奏.md) - `plan` / `global_experience_spine`
- [0-12 小时候选主循环细案设计](版本规划/14_0-12小时候选主循环细案设计.md) - `plan` / `candidate_loop_detail_design`
- [版本规划阅读入口](版本规划/README.md) - `entry` / `version_planning`
- [知识库规范](知识库/README.md) - `kb` / `knowledge_base`
- [美术流水线总览](美术文档/00_美术流水线总览.md) - `art` / `art_pipeline`
- [Manifest 规范](美术文档/01_Manifest规范.md) - `art` / `art_manifest`
- [资源规格与接入规范](美术文档/02_资源规格与接入规范.md) - `art` / `art_asset_spec`
- [AI 美术资产质量与筛选准入规范](美术文档/03_AI生成与筛选规范.md) - `art` / `ai_art_quality`
- [美术风格基准](美术文档/04_美术风格基准.md) - `art` / `art_style`
- [AI 图片生成能力与网关契约](美术文档/05_AI图片网关接入方案.md) - `art` / `ai_image_gateway`
- [运行时美术验收记录](美术文档/09_运行时美术验收记录.md) - `art` / `runtime_art_validation`
- [正式版核心纵切美术路线](美术文档/10_正式版核心纵切美术路线.md) - `art` / `formal_art_route`
- [正式纵切 UI 与素材覆盖矩阵](美术文档/13_正式纵切UI与素材覆盖矩阵.md) - `art` / `ui_design`
- [FormalV2 素材候选审查记录](美术文档/14_FormalV2素材候选审查记录.md) - `art` / `art_pipeline`
- [Live2D角色动画资产接入规格](美术文档/16_Live2D角色动画资产接入规格.md) - `art` / `live2d_character_animation`
- [Agent原生动态立绘资产接入规格](美术文档/17_Agent原生动态立绘资产接入规格.md) - `art` / `dynamic_doll_puppet`
- [CG底图与漫画式播放演出工作流](美术文档/18_CG底图与漫画式播放演出工作流.md) - `art` / `narrative_cg_art_pipeline`
- [T0-01 序章 CG 细案](美术文档/19_T0-01序章CG细案.md) - `art` / `narrative_cg_art_pipeline`
- [GIF 小循环人物替换工作流](美术文档/20_GIF小循环人物替换工作流.md) - `art` / `gif_character_replacement`
- [美术归档文档](美术文档/archive/README.md) - `art` / `art_archive`
- [美术文档索引](美术文档/README.md) - `art` / `art_pipeline`
- [营业结算演出界面 Formal V1](美术文档/ui_design/formal_v1/business_settlement_v1.md) - `art` / `ui_design`
- [底盘升级界面 Formal V1](美术文档/ui_design/formal_v1/chassis_upgrade_panel_v1.md) - `art` / `ui_design`
- [战斗界面 Formal V1](美术文档/ui_design/formal_v1/combat_hud_v1.md) - `art` / `ui_design`
- [每日账单报告界面 Formal V1](美术文档/ui_design/formal_v1/daily_bill_report_v1.md) - `art` / `ui_design`
- [人偶交互界面 Formal V1](美术文档/ui_design/formal_v1/doll_interaction_v1.md) - `art` / `ui_design`
- [深渊地图界面 Formal V1](美术文档/ui_design/formal_v1/dungeon_map_v1.md) - `art` / `ui_design`
- [势力商店界面 Formal V1](美术文档/ui_design/formal_v1/faction_shop_v1.md) - `art` / `ui_design`
- [战利品拾取界面 Formal V1](美术文档/ui_design/formal_v1/inventory_loot_v1.md) - `art` / `ui_design`
- [出发层选择界面 Formal V1](美术文档/ui_design/formal_v1/layer_select_v1.md) - `art` / `ui_design`
- [机体维护整备界面 Formal V1](美术文档/ui_design/formal_v1/maintenance_panel_v1.md) - `art` / `ui_design`
- [势力订单板界面 Formal V1](美术文档/ui_design/formal_v1/order_board_v1.md) - `art` / `ui_design`
- [义体制造界面 Formal V1](美术文档/ui_design/formal_v1/prosthetic_panel_v1.md) - `art` / `ui_design`
- [传闻情报板界面 Formal V1](美术文档/ui_design/formal_v1/rumor_board_v1.md) - `art` / `ui_design`
- [深渊安全区界面 Formal V1](美术文档/ui_design/formal_v1/safe_room_v1.md) - `art` / `ui_design`
- [剧本事件界面 Formal V1](美术文档/ui_design/formal_v1/scenario_event_v1.md) - `art` / `ui_design`
- [正式版 UI 结构 V1 总览](美术文档/ui_design/formal_v1/screen_structure_review.md) - `art` / `ui_design`
- [工坊出售界面 Formal V1](美术文档/ui_design/formal_v1/sell_panel_v1.md) - `art` / `ui_design`
- [结算界面 Formal V1](美术文档/ui_design/formal_v1/settlement_v1.md) - `art` / `ui_design`
- [出货分配界面 Formal V1](美术文档/ui_design/formal_v1/shop_staging_v1.md) - `art` / `ui_design`
- [深渊阶梯房间界面 Formal V1](美术文档/ui_design/formal_v1/stairs_room_v1.md) - `art` / `ui_design`
- [工坊主界面 Formal V1](美术文档/ui_design/formal_v1/workshop_main_v1.md) - `art` / `ui_design`
- [Formal V2 UX/UI 重构总方案](美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md) - `art` / `ui_design`
- [Formal V2 UX/UI 设计层](美术文档/ui_design/formal_v2/README.md) - `art` / `ui_design`
- [UI 交付检查清单](美术文档/ui_design/handoff_checklist.md) - `art` / `art_pipeline`
- [UI 设计流水线](美术文档/ui_design/README.md) - `art` / `art_pipeline`
- [UI 设计迭代与版本迁移流程](美术文档/ui_design/ui_iteration_process.md) - `art` / `ui_design`
- [UI 设计版本迁移记录](美术文档/ui_design/versions/migration_log.md) - `art` / `ui_design`
- [UI 设计版本管理](美术文档/ui_design/versions/README.md) - `art` / `ui_design`
- [人设参考获取规则](美术文档/人设/01_人设参考获取规则.md) - `art` / `character_design_reference`
- [零号原型参考：失明少女（漆黑的子弹）](美术文档/人设/02_零号原型参考_失明少女.md) - `art` / `character_design_reference`
- [零号初版人设方案](美术文档/人设/03_零号初版人设方案.md) - `art` / `character_design`
- [零号立绘素材设计与交付清单](美术文档/人设/05_零号立绘素材设计与交付清单.md) - `art` / `character_portrait_asset_design`
- [人设文档入口](美术文档/人设/README.md) - `art` / `character_design`
- [正式配置设计与填充推进计划](设计文档/config/26_正式配置设计与填充推进计划.md) - `plan` / `formal_config_authoring`
- [Items正式配置承接审计](设计文档/config/audits/27_Items正式配置承接审计.md) - `audit` / `formal_config_authoring`
- [Monsters正式配置承接审计](设计文档/config/audits/28_Monsters正式配置承接审计.md) - `audit` / `formal_config_authoring`
- [Dungeons正式配置承接审计](设计文档/config/audits/29_Dungeons正式配置承接审计.md) - `audit` / `formal_config_authoring`
- [Rewards正式配置承接审计](设计文档/config/audits/30_Rewards正式配置承接审计.md) - `audit` / `formal_config_authoring`
- [局外成长正式配置承接审计](设计文档/config/audits/36_局外成长正式配置承接审计.md) - `audit` / `formal_config_authoring`
- [经济压力正式配置承接审计](设计文档/config/audits/46_经济压力正式配置承接审计.md) - `audit` / `formal_config_authoring`
- [第一层正式配置落地设计](设计文档/config/designs/31_第一层正式配置落地设计.md) - `config_design` / `formal_config_authoring`
- [第二层正式配置落地设计](设计文档/config/designs/32_第二层正式配置落地设计.md) - `config_design` / `formal_config_authoring`
- [第三层正式配置落地设计](设计文档/config/designs/33_第三层正式配置落地设计.md) - `config_design` / `formal_config_authoring`
- [局外成长正式配置落地设计](设计文档/config/designs/37_局外成长正式配置落地设计.md) - `config_design` / `formal_config_authoring`
- [局外成长底盘正式配置落地设计](设计文档/config/designs/40_局外成长底盘正式配置落地设计.md) - `config_design` / `formal_config_authoring`
- [局外成长效果特质正式配置落地设计](设计文档/config/designs/41_局外成长效果特质正式配置落地设计.md) - `config_design` / `formal_config_authoring`
- [局外成长义体正式配置落地设计](设计文档/config/designs/42_局外成长义体正式配置落地设计.md) - `config_design` / `formal_config_authoring`
- [局外成长制造维护正式配置落地设计](设计文档/config/designs/43_局外成长制造维护正式配置落地设计.md) - `config_design` / `formal_config_authoring`
- [局外成长人偶特质房间正式配置落地设计](设计文档/config/designs/44_局外成长人偶特质房间正式配置落地设计.md) - `config_design` / `formal_config_authoring`
- [经济压力核心正式配置落地设计](设计文档/config/designs/47_经济压力核心正式配置落地设计.md) - `config_design` / `formal_config_authoring`
- [经济压力传闻正式配置落地设计](设计文档/config/designs/48_经济压力传闻正式配置落地设计.md) - `config_design` / `formal_config_authoring`
- [经济压力势力正式配置落地设计](设计文档/config/designs/49_经济压力势力正式配置落地设计.md) - `config_design` / `formal_config_authoring`
- [经济压力订单正式配置落地设计](设计文档/config/designs/50_经济压力订单正式配置落地设计.md) - `config_design` / `formal_config_authoring`
- [局外成长正式配置ID锁定与冲突检查](设计文档/config/gates/39_局外成长正式配置ID锁定与冲突检查.md) - `audit` / `formal_config_authoring`
- [经济压力正式配置ID锁定与冲突检查](设计文档/config/gates/53_经济压力正式配置ID锁定与冲突检查.md) - `audit` / `formal_config_authoring`
- [经济压力正式配置README字段口径检查](设计文档/config/gates/54_经济压力正式配置README字段口径检查.md) - `audit` / `formal_config_authoring`
- [经济压力订单ID最终锁定表](设计文档/config/gates/55_经济压力订单ID最终锁定表.md) - `audit` / `formal_config_authoring`
- [正式配置源落地准入门禁](设计文档/config/gates/56_正式配置源落地准入门禁.md) - `gate` / `formal_config_authoring`
- [前三层正式配置实现任务拆分](设计文档/config/tasks/34_前三层正式配置实现任务拆分.md) - `task_split` / `formal_config_authoring`
- [局外成长正式配置实现任务拆分](设计文档/config/tasks/38_局外成长正式配置实现任务拆分.md) - `task_breakdown` / `formal_config_authoring`
- [经济压力正式配置实现任务拆分](设计文档/config/tasks/52_经济压力正式配置实现任务拆分.md) - `task_breakdown` / `formal_config_authoring`
- [前三层正式配置Validator与固定Seed验收样例](设计文档/config/validation/35_前三层正式配置Validator与固定Seed验收样例.md) - `acceptance_spec` / `formal_config_acceptance`
- [局外成长正式配置Validator与固定验收样例](设计文档/config/validation/45_局外成长正式配置Validator与固定验收样例.md) - `acceptance_spec` / `formal_config_acceptance`
- [经济压力正式配置Validator与固定验收样例](设计文档/config/validation/51_经济压力正式配置Validator与固定验收样例.md) - `acceptance_spec` / `formal_config_acceptance`
- [正式版内容生产规格](设计文档/content_packs/18_正式版内容生产规格.md) - `spec` / `content_authoring`
- [第一层正式核心内容包](设计文档/content_packs/19_第一层正式核心内容包.md) - `content_pack` / `content_authoring`
- [第二层背包压力内容包](设计文档/content_packs/20_第二层背包压力内容包.md) - `content_pack` / `content_authoring`
- [人偶成长修复内容包](设计文档/content_packs/21_人偶成长修复内容包.md) - `content_pack` / `content_authoring`
- [小镇经济月租内容包](设计文档/content_packs/22_小镇经济月租内容包.md) - `content_pack` / `content_authoring`
- [长期记忆剧情内容包](设计文档/content_packs/23_长期记忆剧情内容包.md) - `content_pack` / `content_authoring`
- [势力订单声望内容包](设计文档/content_packs/24_势力订单声望内容包.md) - `content_pack` / `content_authoring`
- [第三层路线侵蚀内容包](设计文档/content_packs/25_第三层路线侵蚀内容包.md) - `content_pack` / `content_authoring`
- [策划文档开发交付审计](设计文档/delivery/00_策划文档开发交付审计.md) - `audit` / `design_delivery`
- [策划交付落地矩阵](设计文档/delivery/12_策划交付落地矩阵.md) - `matrix` / `design_delivery`
- [策划跨系统验收场景矩阵](设计文档/delivery/13_策划跨系统验收场景矩阵.md) - `matrix` / `design_acceptance`
- [策划配置表现验收承接规格](设计文档/delivery/14_策划配置表现验收承接规格.md) - `handoff` / `design_delivery`
- [P0主干配置表现验收承接清单](设计文档/delivery/15_P0主干配置表现验收承接清单.md) - `checklist` / `design_delivery`
- [P1人偶成长情感承接清单](设计文档/delivery/16_P1人偶成长情感承接清单.md) - `checklist` / `design_delivery`
- [P2长期循环叙事承接清单](设计文档/delivery/17_P2长期循环叙事承接清单.md) - `checklist` / `design_delivery`
- [系统关联总图：全系统内在关联与数据流向](设计文档/GDD/GDD_00_系统关联总图.md) - `gdd` / `system_overview`
- [详案_01：背包战斗与局内网格机制 (GDD_01)](设计文档/GDD/GDD_01_背包战斗与局内网格机制.md) - `gdd` / `grid_inventory`
- [详案_02：深渊地图遍历与搜打撤抉择 (GDD_02)](设计文档/GDD/GDD_02_深渊地图遍历与搜打撤抉择.md) - `gdd` / `dungeon_exploration`
- [详案_03：人偶实体对象与好感双轨机制 (GDD_03)](设计文档/GDD/GDD_03_人偶实体对象与好感双轨机制.md) - `gdd` / `doll_relationship`
- [详案_04：小镇循环与经济物价波浪模型 (GDD_04)](设计文档/GDD/GDD_04_小镇循环与经济物价波浪模型.md) - `gdd` / `town_economy`
- [详案_05：剧本调度引擎与世界观封装逻辑 (GDD_05)](设计文档/GDD/GDD_05_剧本调度引擎与世界观封装逻辑.md) - `gdd` / `scenario_worldview`
- [详案_06：物品系统与物品生命周期 (GDD_06)](设计文档/GDD/GDD_06_物品系统与物品生命周期.md) - `gdd` / `item_lifecycle`
- [详案_07：时间日程系统与天数轮转机制 (GDD_07)](设计文档/GDD/GDD_07_时间日程系统与天数轮转机制.md) - `gdd` / `time_schedule`
- [详案_08：人偶养成子模块全案 (GDD_08)](设计文档/GDD/GDD_08_人偶养成子模块全案.md) - `gdd` / `doll_growth`
- [详案_09：标签与特质系统 (GDD_09)](设计文档/GDD/GDD_09_标签与特质系统.md) - `gdd` / `tag_trait`
- [详案_10：势力声望与订单系统 (GDD_10)](设计文档/GDD/GDD_10_势力声望与订单系统.md) - `gdd` / `faction_order`
- [详案_11：人偶房间与视觉叙事系统 (GDD_11)](设计文档/GDD/GDD_11_人偶房间与视觉叙事系统.md) - `gdd` / `doll_room`
- [详案_12：人偶交互管理器 (GDD_12)](设计文档/GDD/GDD_12_人偶交互管理器.md) - `gdd` / `doll_interaction`
- [设计文档阅读入口](设计文档/README.md) - `entry` / `design_delivery`
- [剧情大纲](设计文档/剧情/00_剧情大纲.md) - `narrative_design` / `narrative_design`
- [序章演出与对话节奏](设计文档/剧情/01_序章演出与对话节奏.md) - `narrative_design` / `narrative_design`
- [剧情设计阅读入口](设计文档/剧情/README.md) - `entry` / `narrative_design`
- [This Is the Police 演出参考](设计文档/参考/01_This_Is_the_Police_演出参考.md) - `reference` / `design_reference`
- [设计参考阅读入口](设计文档/参考/README.md) - `entry` / `design_reference`
- [局外时间与日程口径规则卡](设计文档/规则卡/01_局外时间与日程口径规则卡.md) - `rule_card` / `time_schedule`
- [物品、背包、旋转与生命周期规则卡](设计文档/规则卡/02_物品背包旋转与生命周期规则卡.md) - `rule_card` / `item_inventory_lifecycle`
- [战斗回合与怪物意图规则卡](设计文档/规则卡/03_战斗回合与怪物意图规则卡.md) - `rule_card` / `combat_turn_intent`
- [小镇经济结算与压力链规则卡](设计文档/规则卡/04_小镇经济结算与压力链规则卡.md) - `rule_card` / `town_economy_settlement`
- [局外成长与维护规则卡](设计文档/规则卡/05_局外成长与维护规则卡.md) - `rule_card` / `outgame_growth_maintenance`
- [标签与特质规则卡](设计文档/规则卡/06_标签与特质规则卡.md) - `rule_card` / `tag_trait_rules`
- [势力声望与订单规则卡](设计文档/规则卡/07_势力声望与订单规则卡.md) - `rule_card` / `faction_order_rules`
- [人偶核心状态与好感双轨规则卡](设计文档/规则卡/08_人偶核心状态与好感双轨规则卡.md) - `rule_card` / `doll_core_state_affection`
- [人偶交互事件与反馈规则卡](设计文档/规则卡/09_人偶交互事件与反馈规则卡.md) - `rule_card` / `doll_interaction_rules`
- [剧本调度与事件队列规则卡](设计文档/规则卡/10_剧本调度与事件队列规则卡.md) - `rule_card` / `scenario_event_queue`
- [人偶房间布局与视觉叙事规则卡](设计文档/规则卡/11_人偶房间布局与视觉叙事规则卡.md) - `rule_card` / `doll_room_rules`
- [局外底盘配置字段说明 (Chassis Config)](配置表(JSON)/Chassis/README.md) - `config` / `config_chassis`
- [工坊制造配方字段说明 (Crafting Recipes Config)](配置表(JSON)/CraftingRecipes/README.md) - `config` / `config_crafting`
- [人偶基础档案配置字段说明 (Dolls Config)](配置表(JSON)/Dolls/README.md) - `config` / `config_dolls`
- [深渊地图层级配置说明 (Dungeons Config)](配置表(JSON)/Dungeons/README.md) - `config` / `config_dungeons`
- [小镇经济配置字段说明 (Economy Config)](配置表(JSON)/Economy/README.md) - `config` / `config_economy`
- [效果配置字典说明 (EffectEnums Config)](配置表(JSON)/Effects/README.md) - `config` / `config_effects`
- [势力配置字段说明 (Factions Config)](配置表(JSON)/Factions/README.md) - `config` / `config_factions`
- [局内物品与网格实体字段说明 (Items Config)](配置表(JSON)/Items/README.md) - `config` / `config_items`
- [深渊怪物配置字段说明 (Monsters Config)](配置表(JSON)/Monsters/README.md) - `config` / `config_monsters`
- [Narrative 配置说明](配置表(JSON)/Narrative/README.md) - `config_readme` / `narrative_config`
- [订单配置字段说明 (Orders Config)](配置表(JSON)/Orders/README.md) - `config` / `config_orders`
- [义体插件配置字段说明 (Prosthetics Config)](配置表(JSON)/Prosthetics/README.md) - `config` / `config_prosthetics`
- [配置表数据总览说明 (Configuration Overview)](配置表(JSON)/README.md) - `config` / `config_data`
- [奖励表配置字段说明 (Rewards Config)](配置表(JSON)/Rewards/README.md) - `config` / `config_rewards`
- [传闻配置字段说明 (Rumors Config)](配置表(JSON)/Rumors/README.md) - `config` / `config_rumors`

## 关联网络

- `0-12 小时细案 Owner <-> Owner`：3 条
- `Owner <-> 全局`：2 条
- `Owner <-> 剧情`：5 条
- `Owner <-> 游戏导演`：9 条
- `Owner <-> 知识库`：1 条
- `Owner <-> 程序`：5 条
- `Owner <-> 策划`：8 条
- `Owner <-> 美术`：5 条
- `全局 <-> 兼容`：5 条
- `全局 <-> 剧情`：2 条
- `全局 <-> 游戏导演`：20 条
- `全局 <-> 知识库`：7 条
- `全局 <-> 程序`：15 条
- `全局 <-> 策划`：54 条
- `全局 <-> 美术`：11 条
- `兼容 <-> 游戏导演`：5 条
- `兼容 <-> 知识库`：1 条
- `剧情 <-> 游戏导演`：8 条
- `剧情 <-> 知识库`：1 条
- `剧情 <-> 程序`：1 条
- `剧情 <-> 策划`：12 条
- `游戏导演 <-> 知识库`：1 条
- `游戏导演 <-> 程序`：2 条
- `游戏导演 <-> 策划`：16 条
- `游戏导演 <-> 美术`：2 条
- `知识库 <-> 程序`：1 条
- `知识库 <-> 策划`：1 条
- `知识库 <-> 美术`：1 条
- `程序 <-> 策划`：62 条
- `程序 <-> 美术`：43 条
- `策划 <-> 美术`：6 条

## 按职能分组

### 0-12 小时细案 Owner

| 文档 | 类型 | 状态 | 领域 | 关联 | 元数据 |
|---|---|---|---|---|---|
| [T0-01 开局人偶状态到首次下潜许可](版本规划/_archive/0-12小时细案_2026-06-14_拆分稿/T0-01_开局人偶状态到首次下潜许可.md) | `plan` | `historical` | `candidate_loop_detail_design` | 1 | 完整 |
| [T0-02 首次下潜到第一笔战利品](版本规划/_archive/0-12小时细案_2026-06-14_拆分稿/T0-02_首次下潜到第一笔战利品.md) | `plan` | `historical` | `candidate_loop_detail_design` | 1 | 完整 |
| [T0-03 回城出售到维护 / 休整闭环](版本规划/_archive/0-12小时细案_2026-06-14_拆分稿/T0-03_回城出售到维护休整闭环.md) | `plan` | `historical` | `candidate_loop_detail_design` | 1 | 完整 |

### Owner

| 文档 | 类型 | 状态 | 领域 | 关联 | 元数据 |
|---|---|---|---|---|---|
| [Gemini 角色立绘输出契约实施计划](docs/plans/2026-08-08-gemini-portrait-output-contract.md) | `plan` | `active` | `ai_image_gateway_art_pipeline` | 1 | 完整 |
| [Gemini 角色立绘输出契约设计](docs/specs/2026-08-08-gemini-portrait-output-contract-design.md) | `design` | `active` | `ai_image_gateway_art_pipeline` | 3 | 完整 |
| [T0-01A 开局人偶状态到首次下潜许可实现设计](版本规划/0-12小时细案/T0-01A_开局人偶状态到首次下潜许可实现设计.md) | `implementation_design` | `active` | `prologue_first_loop_implementation` | 7 | 完整 |
| [T0-01A 开局人偶状态到首次下潜许可开发方案](版本规划/0-12小时细案/T0-01A_开局人偶状态到首次下潜许可开发方案.md) | `development_plan` | `active` | `prologue_first_loop_development` | 10 | 完整 |
| [T0-01 序章首次循环](版本规划/0-12小时细案/T0-01_序章首次循环.md) | `plan` | `active` | `candidate_loop_detail_design` | 13 | 完整 |
| [T0-01 序章首次循环开发总方案](版本规划/0-12小时细案/T0-01_序章首次循环开发总方案.md) | `development_plan` | `active` | `prologue_first_loop_development` | 13 | 完整 |
| [T0-VAL-01 T0-01A 运行时效果验收记录](版本规划/0-12小时细案/T0-VAL-01_最终实际效果优化文档.md) | `validation_effect_optimization` | `active` | `prologue_first_loop_runtime_acceptance` | 5 | 完整 |
| [T1 第一层搜打撤成形](版本规划/0-12小时细案/T1_第一层搜打撤成形.md) | `plan` | `active` | `candidate_loop_detail_design` | 6 | 完整 |
| [Owner 智能体阅读入口](知识库/views/owner.md) | `view` | `active` | `agent_context_view` | 4 | 完整 |

### PM

| 文档 | 类型 | 状态 | 领域 | 关联 | 元数据 |
|---|---|---|---|---|---|
| [正式版长期版本节点规划](版本规划/12_正式版长期版本节点规划.md) | `archive_notice` | `archived` | `formal_version_milestones` | 0 | 完整 |

### 全局

| 文档 | 类型 | 状态 | 领域 | 关联 | 元数据 |
|---|---|---|---|---|---|
| [Project P3 智能体入口](AGENTS.md) | `entry` | `active` | `agent_workflow` | 24 | 完整 |
| [文档索引](DOCS_INDEX.md) | `index` | `generated` | `knowledge_base` | 1 | 完整 |
| [项目状态](PROJECT_STATUS.md) | `status` | `active` | `project_status` | 64 | 完整 |
| [智能体状态页说明](agent_status/README.md) | `status` | `active` | `agent_status` | 7 | 完整 |
| [Project P3 Agent Responsibility Routing Refactor Implementation Plan](docs/plans/2026-07-17-agents-routing-refactor.md) | `plan` | `historical` | `agent_workflow` | 1 | 完整 |
| [Agent 状态页与 Role 阅读范围优化实施计划](docs/plans/2026-07-18-agent-status-and-role-disclosure.md) | `plan` | `historical` | `agent_workflow` | 0 | 完整 |
| [Project P3 Role Vocabulary and Knowledge Index Implementation Plan](docs/plans/2026-07-18-role-vocabulary-and-index.md) | `plan` | `historical` | `agent_workflow` | 1 | 完整 |
| [Project P3 AGENTS 职责路由重构设计](docs/specs/2026-07-17-agents-routing-refactor-design.md) | `design` | `historical` | `agent_workflow` | 1 | 完整 |
| [Agent 状态页与 Role 阅读范围优化设计](docs/specs/2026-07-18-agent-status-and-role-disclosure-design.md) | `design` | `historical` | `agent_workflow` | 0 | 完整 |
| [Project P3 Role 词表与知识索引归属设计](docs/specs/2026-07-18-role-vocabulary-and-index-design.md) | `design` | `historical` | `agent_workflow` | 1 | 完整 |
| [Agent 工作流与美术流程减负设计](docs/specs/2026-10-01-lighter-agent-workflow-design.md) | `design` | `historical` | `agent_workflow` | 5 | 完整 |
| [工作流第二轮精简设计](docs/specs/2026-10-01-workflow-round2-design.md) | `design` | `active` | `agent_workflow` | 7 | 完整 |
| [文档维护与新增控制规则](rules/01_文档维护与新增控制规则.md) | `rule` | `active` | `document_governance` | 7 | 完整 |
| [智能体任务路由与完成协议](rules/02_智能体任务路由与完成协议.md) | `rule` | `active` | `agent_workflow` | 12 | 完整 |
| [全局 Agent Rules 入口](rules/README.md) | `entry` | `active` | `agent_rules` | 5 | 完整 |
| [智能体开工健康检查](tools/agent/README.md) | `tool` | `active` | `agent_workflow` | 7 | 完整 |
| [正式版核心纵切开发路线](版本规划/09_正式版核心纵切开发路线.md) | `plan` | `active` | `formal_vertical_slice` | 19 | 完整 |
| [最小可玩版本 (MVP) 核心闭环内容清单](版本规划/_archive/mvp_2026-05/00_最小MVP体验闭环内容清单.md) | `plan` | `historical` | `mvp_planning` | 3 | 完整 |
| [MVP 客户端开发里程碑与节点规划](版本规划/_archive/mvp_2026-05/02_开发里程碑与节点规划.md) | `plan` | `historical` | `mvp_planning` | 3 | 完整 |
| [MVP 验证需要补充的功能开发](版本规划/_archive/mvp_2026-05/04_MVP验证需要补充的功能开发.md) | `plan` | `historical` | `mvp_planning` | 3 | 完整 |
| [MVP 白盒试玩收口推进计划](版本规划/_archive/mvp_2026-05/06_MVP白盒试玩收口推进计划.md) | `plan` | `historical` | `mvp_planning` | 3 | 完整 |
| [MVP 自动化试玩验收方案](版本规划/_archive/mvp_2026-05/07_MVP自动化试玩验收方案.md) | `plan` | `historical` | `mvp_planning` | 2 | 完整 |
| [MVP 自动化试玩首轮报告](版本规划/_archive/mvp_2026-05/08_MVP自动化试玩首轮报告.md) | `plan` | `historical` | `mvp_planning` | 2 | 完整 |

### 兼容

| 文档 | 类型 | 状态 | 领域 | 关联 | 元数据 |
|---|---|---|---|---|---|
| [PM / 版本规划 兼容入口](agent_status/pm.md) | `status` | `archived` | `legacy_pm_route` | 7 | 完整 |
| [PM 智能体兼容入口](知识库/views/pm.md) | `view` | `archived` | `legacy_pm_route` | 6 | 完整 |

### 剧情

| 文档 | 类型 | 状态 | 领域 | 关联 | 元数据 |
|---|---|---|---|---|---|
| [剧情智能体阅读入口](知识库/views/narrative.md) | `view` | `active` | `agent_context_view` | 3 | 完整 |
| [剧情大纲](设计文档/剧情/00_剧情大纲.md) | `narrative_design` | `active` | `narrative_design` | 12 | 完整 |
| [序章演出与对话节奏](设计文档/剧情/01_序章演出与对话节奏.md) | `narrative_design` | `active` | `narrative_design` | 7 | 完整 |
| [剧情设计阅读入口](设计文档/剧情/README.md) | `entry` | `active` | `narrative_design` | 13 | 完整 |

### 未分类

| 文档 | 类型 | 状态 | 领域 | 关联 | 元数据 |
|---|---|---|---|---|---|
| [Formal V2 Prompt Readiness](美术文档/archive/formal_v2_prompt_readiness_snapshots/20260609_052040_formalv2_program_integrate_prompt_ready_ascii_cmd_20260609.md) | `unclassified` | `missing_metadata` | `unclassified` | 0 | 缺失：id, title, type, role, domain, status |
| [Formal V2 Prompt Readiness](美术文档/archive/formal_v2_prompt_readiness_snapshots/20260609_084349_formalv2_continue_20260609.md) | `unclassified` | `missing_metadata` | `unclassified` | 0 | 缺失：id, title, type, role, domain, status |
| [Formal V2 Prompt Readiness](美术文档/archive/formal_v2_prompt_readiness_snapshots/20260609_090042_formalv2_after_acceptance_check_20260609.md) | `unclassified` | `missing_metadata` | `unclassified` | 0 | 缺失：id, title, type, role, domain, status |
| [Formal V2 Prompt Readiness](美术文档/archive/formal_v2_prompt_readiness_snapshots/20260609_131044_formalv2_remaining_items_after_sync_20260609.md) | `unclassified` | `missing_metadata` | `unclassified` | 0 | 缺失：id, title, type, role, domain, status |

### 游戏导演

| 文档 | 类型 | 状态 | 领域 | 关联 | 元数据 |
|---|---|---|---|---|---|
| [游戏导演 / 制作人 状态](agent_status/director.md) | `status` | `active` | `game_direction_production` | 17 | 完整 |
| [纵切批次与需求文档承接矩阵](版本规划/11_纵切批次与需求文档承接矩阵.md) | `plan` | `active` | `vertical_batch_requirement_coverage` | 25 | 完整 |
| [正式版全局体验总线与全系统宏观大纲](版本规划/13_正式版全局体验总线与开放节奏.md) | `plan` | `active` | `global_experience_spine` | 11 | 完整 |
| [0-12 小时候选主循环细案设计](版本规划/14_0-12小时候选主循环细案设计.md) | `plan` | `active` | `candidate_loop_detail_design` | 14 | 完整 |
| [版本规划阅读入口](版本规划/README.md) | `entry` | `active` | `version_planning` | 9 | 完整 |
| [游戏导演 / 制作人阅读入口](知识库/views/director.md) | `view` | `active` | `agent_context_view` | 15 | 完整 |

### 知识库

| 文档 | 类型 | 状态 | 领域 | 关联 | 元数据 |
|---|---|---|---|---|---|
| [Agent 路由 P0 治理加固实施计划](docs/plans/2026-07-18-agent-routing-p0-hardening.md) | `plan` | `historical` | `agent_workflow` | 0 | 完整 |
| [Agent 路由 P0 治理加固设计](docs/specs/2026-07-18-agent-routing-p0-hardening-design.md) | `design` | `historical` | `agent_workflow` | 0 | 完整 |
| [知识库规范](知识库/README.md) | `kb` | `active` | `knowledge_base` | 14 | 完整 |

### 程序

| 文档 | 类型 | 状态 | 领域 | 关联 | 元数据 |
|---|---|---|---|---|---|
| [程序 / Unity 状态](agent_status/program.md) | `status` | `active` | `unity_programming` | 20 | 完整 |
| [AI 图片网关透明流式模式实施计划](docs/plans/2026-07-18-ai-image-gateway-transparent-streaming.md) | `plan` | `historical` | `ai_image_gateway` | 0 | 完整 |
| [P3 MCP 参数 Schema 实现计划](docs/plans/2026-08-02-p3-mcp-parameter-schema.md) | `plan` | `historical` | `mcp_validation` | 1 | 完整 |
| [AI 图片网关透明流式模式设计](docs/specs/2026-07-18-ai-image-gateway-transparent-streaming-design.md) | `design` | `historical` | `ai_image_gateway` | 0 | 完整 |
| [P3 MCP 参数 Schema 设计](docs/specs/2026-08-02-p3-mcp-parameter-schema-design.md) | `design` | `historical` | `mcp_validation` | 1 | 完整 |
| [Config Tools](tools/config/README.md) | `tool` | `active` | `config_tooling` | 5 | 完整 |
| [程序开发大纲与系统索引](开发文档/00_程序开发大纲.md) | `dev` | `active` | `program_architecture` | 23 | 完整 |
| [核心数据容器系统 (Core Data System)](开发文档/01_核心数据与实体容器(CoreData).md) | `dev` | `active` | `core_data` | 10 | 完整 |
| [网格背包与计算系统 (Grid System)](开发文档/02_网格背包与计算系统(GridSystem).md) | `dev` | `active` | `grid_inventory` | 5 | 完整 |
| [深渊与战斗循环系统 (Dungeon & Combat System)](开发文档/03_深渊与战斗循环(DungeonCombat).md) | `dev` | `active` | `dungeon_combat` | 10 | 完整 |
| [工坊与养成逻辑 (Workshop & Crafting System)](开发文档/04_工坊与养成逻辑(WorkshopSystem).md) | `dev` | `active` | `workshop` | 6 | 完整 |
| [表现层架构与事件总线 (View & EventBus System)](开发文档/05_表现层架构与事件总线(ViewAndEventBus).md) | `dev` | `active` | `presentation_layer` | 10 | 完整 |
| [奖励与掉落系统 (RewardSystem)](开发文档/10_奖励与掉落系统(RewardSystem).md) | `dev` | `active` | `reward_loot` | 6 | 完整 |
| [怪物 AI 与行动系统 (MonsterActionAI)](开发文档/11_怪物AI与行动系统(MonsterActionAI).md) | `dev` | `active` | `monster_ai` | 3 | 完整 |
| [程序开发优化建议与重构路线](开发文档/12_程序开发优化建议与重构路线.md) | `dev` | `active` | `program_refactor` | 6 | 完整 |
| [Unity 运行时美术自动验收方案](开发文档/14_Unity运行时美术自动验收方案.md) | `dev` | `active` | `runtime_art_validation` | 12 | 完整 |
| [P0配置Validator与自动验收底座需求](开发文档/15_P0配置Validator与自动验收底座需求.md) | `dev` | `active` | `config_validation` | 19 | 完整 |
| [程序主流程闭环与架构收口推进计划](开发文档/16_程序主流程闭环与架构收口推进计划.md) | `dev` | `active` | `main_flow_architecture` | 7 | 完整 |
| [Live2D / Spine 运行时接入评估](开发文档/17_Live2DSpine运行时接入评估.md) | `dev` | `active` | `live2d_character_animation` | 6 | 完整 |
| [全局叙事播放系统开发方案](开发文档/18_全局叙事播放系统开发方案.md) | `dev` | `active` | `narrative_playback` | 17 | 完整 |
| [Unity MCP 验收编排层设计](开发文档/19_UnityMCP验收编排层设计.md) | `dev` | `active` | `test_automation` | 12 | 完整 |
| [p3-art-validation V2 MCP 实时优先实现计划](开发文档/20_UnityMCP验收编排层实现计划.md) | `dev` | `active` | `runtime_art_validation` | 8 | 完整 |
| [开发文档目录入口](开发文档/README.md) | `dev` | `active` | `program_docs_index` | 7 | 完整 |
| [架构评估与收口建议](开发文档/archive/06_架构评估与收口建议.md) | `dev` | `archived` | `architecture_review` | 5 | 完整 |
| [开发文档归档目录入口](开发文档/archive/README.md) | `dev` | `active` | `program_docs_archive` | 2 | 完整 |
| [程序开发总规则](开发文档/rules/00_程序开发总规则.md) | `dev` | `active` | `program_general_rules` | 14 | 完整 |
| [客户端分层与领域架构规范](开发文档/rules/01_客户端分层与领域架构规范.md) | `dev` | `active` | `client_architecture` | 8 | 完整 |
| [Unity 表现层与编辑器构建规范](开发文档/rules/02_Unity表现层与编辑器构建规范.md) | `dev` | `active` | `unity_presentation` | 17 | 完整 |
| [视觉资源系统程序开发规范](开发文档/rules/03_视觉资源系统程序开发规范.md) | `dev` | `active` | `visual_asset_system` | 15 | 完整 |
| [自动化测试与验收流程规范](开发文档/rules/04_自动化测试与验收流程规范.md) | `dev` | `active` | `test_automation` | 13 | 完整 |
| [程序开发规范目录入口](开发文档/rules/README.md) | `dev` | `active` | `program_rules_index` | 7 | 完整 |
| [全局与玩家实体定义 (Player & Global Entities)](开发文档/数据与实体定义/01_全局与玩家实体.md) | `dev` | `active` | `program_architecture` | 1 | 完整 |
| [人偶与状态实体定义 (Doll & Status Entities)](开发文档/数据与实体定义/02_人偶与状态实体.md) | `dev` | `active` | `program_architecture` | 8 | 完整 |
| [物品与网格实体定义 (Item & Inventory Entities)](开发文档/数据与实体定义/03_物品与网格实体.md) | `dev` | `active` | `program_architecture` | 6 | 完整 |
| [深渊与战斗实体定义 (Dungeon & Combat Entities)](开发文档/数据与实体定义/04_深渊与战斗实体.md) | `dev` | `active` | `dungeon_combat` | 6 | 完整 |
| [经济与社会实体定义 (Economy & Social Entities)](开发文档/数据与实体定义/05_经济与社会实体.md) | `dev` | `active` | `program_architecture` | 6 | 完整 |
| [程序智能体阅读入口](知识库/views/program.md) | `view` | `active` | `agent_context_view` | 14 | 完整 |
| [Project P3 美术自动验收（截图）流程优化与真实数据驱动演进方案](美术文档/archive/10_美术验收截图优化与真实数据驱动演进方案.md) | `other` | `historical` | `runtime_art_validation` | 3 | 完整 |

### 策划

| 文档 | 类型 | 状态 | 领域 | 关联 | 元数据 |
|---|---|---|---|---|---|
| [策划 / 数值 状态](agent_status/design.md) | `status` | `active` | `design_balance` | 76 | 完整 |
| [基准价值与空间本位模型 (Space-Value Standard)](数值模型设计/00_基准价值与空间本位模型.md) | `balance` | `active` | `balance_space_value` | 16 | 完整 |
| [经济循环与通缩模型 (Economy & Deflation Model)](数值模型设计/01_经济循环与通缩模型.md) | `balance` | `active` | `balance_economy` | 8 | 完整 |
| [战斗伤害与生存公式 (Combat & Survival Formulas)](数值模型设计/02_战斗伤害与生存公式.md) | `balance` | `active` | `balance_combat` | 7 | 完整 |
| [深渊产出与掉落期望 (Loot Generation & Expectation)](数值模型设计/03_深渊产出与掉落期望.md) | `balance` | `active` | `balance_loot` | 8 | 完整 |
| [数值模型沙盘推演方法论 (Numerical Sandboxing)](版本规划/_archive/mvp_2026-05/03_mvp数值要求.md) | `plan` | `historical` | `mvp_planning` | 3 | 完整 |
| [MVP 需要补充的配置调整](版本规划/_archive/mvp_2026-05/05_MVP需要补充的配置调整.md) | `plan` | `historical` | `mvp_planning` | 3 | 完整 |
| [策划智能体阅读入口](知识库/views/design.md) | `view` | `active` | `agent_context_view` | 12 | 完整 |
| [系统关联总图：全系统内在关联与数据流向](设计文档/GDD/GDD_00_系统关联总图.md) | `gdd` | `active` | `system_overview` | 37 | 完整 |
| [详案_01：背包战斗与局内网格机制 (GDD_01)](设计文档/GDD/GDD_01_背包战斗与局内网格机制.md) | `gdd` | `active` | `grid_inventory` | 18 | 完整 |
| [详案_02：深渊地图遍历与搜打撤抉择 (GDD_02)](设计文档/GDD/GDD_02_深渊地图遍历与搜打撤抉择.md) | `gdd` | `active` | `dungeon_exploration` | 16 | 完整 |
| [详案_03：人偶实体对象与好感双轨机制 (GDD_03)](设计文档/GDD/GDD_03_人偶实体对象与好感双轨机制.md) | `gdd` | `active` | `doll_relationship` | 19 | 完整 |
| [详案_04：小镇循环与经济物价波浪模型 (GDD_04)](设计文档/GDD/GDD_04_小镇循环与经济物价波浪模型.md) | `gdd` | `active` | `town_economy` | 19 | 完整 |
| [详案_05：剧本调度引擎与世界观封装逻辑 (GDD_05)](设计文档/GDD/GDD_05_剧本调度引擎与世界观封装逻辑.md) | `gdd` | `active` | `scenario_worldview` | 12 | 完整 |
| [详案_06：物品系统与物品生命周期 (GDD_06)](设计文档/GDD/GDD_06_物品系统与物品生命周期.md) | `gdd` | `active` | `item_lifecycle` | 14 | 完整 |
| [详案_07：时间日程系统与天数轮转机制 (GDD_07)](设计文档/GDD/GDD_07_时间日程系统与天数轮转机制.md) | `gdd` | `active` | `time_schedule` | 16 | 完整 |
| [详案_08：人偶养成子模块全案 (GDD_08)](设计文档/GDD/GDD_08_人偶养成子模块全案.md) | `gdd` | `active` | `doll_growth` | 23 | 完整 |
| [详案_09：标签与特质系统 (GDD_09)](设计文档/GDD/GDD_09_标签与特质系统.md) | `gdd` | `active` | `tag_trait` | 11 | 完整 |
| [详案_10：势力声望与订单系统 (GDD_10)](设计文档/GDD/GDD_10_势力声望与订单系统.md) | `gdd` | `active` | `faction_order` | 18 | 完整 |
| [详案_11：人偶房间与视觉叙事系统 (GDD_11)](设计文档/GDD/GDD_11_人偶房间与视觉叙事系统.md) | `gdd` | `active` | `doll_room` | 14 | 完整 |
| [详案_12：人偶交互管理器 (GDD_12)](设计文档/GDD/GDD_12_人偶交互管理器.md) | `gdd` | `active` | `doll_interaction` | 16 | 完整 |
| [设计文档阅读入口](设计文档/README.md) | `entry` | `active` | `design_delivery` | 47 | 完整 |
| [第四层组合压力内容包](设计文档/_archive/content_backlog/26_第四层组合压力内容包.md) | `content_pack` | `draft` | `content_authoring` | 1 | 完整 |
| [正式配置设计与填充推进计划](设计文档/config/26_正式配置设计与填充推进计划.md) | `plan` | `active` | `formal_config_authoring` | 34 | 完整 |
| [Items正式配置承接审计](设计文档/config/audits/27_Items正式配置承接审计.md) | `audit` | `active` | `formal_config_authoring` | 13 | 完整 |
| [Monsters正式配置承接审计](设计文档/config/audits/28_Monsters正式配置承接审计.md) | `audit` | `active` | `formal_config_authoring` | 15 | 完整 |
| [Dungeons正式配置承接审计](设计文档/config/audits/29_Dungeons正式配置承接审计.md) | `audit` | `active` | `formal_config_authoring` | 15 | 完整 |
| [Rewards正式配置承接审计](设计文档/config/audits/30_Rewards正式配置承接审计.md) | `audit` | `active` | `formal_config_authoring` | 16 | 完整 |
| [局外成长正式配置承接审计](设计文档/config/audits/36_局外成长正式配置承接审计.md) | `audit` | `active` | `formal_config_authoring` | 20 | 完整 |
| [经济压力正式配置承接审计](设计文档/config/audits/46_经济压力正式配置承接审计.md) | `audit` | `active` | `formal_config_authoring` | 21 | 完整 |
| [第一层正式配置落地设计](设计文档/config/designs/31_第一层正式配置落地设计.md) | `config_design` | `active` | `formal_config_authoring` | 14 | 完整 |
| [第二层正式配置落地设计](设计文档/config/designs/32_第二层正式配置落地设计.md) | `config_design` | `active` | `formal_config_authoring` | 14 | 完整 |
| [第三层正式配置落地设计](设计文档/config/designs/33_第三层正式配置落地设计.md) | `config_design` | `active` | `formal_config_authoring` | 14 | 完整 |
| [局外成长正式配置落地设计](设计文档/config/designs/37_局外成长正式配置落地设计.md) | `config_design` | `active` | `formal_config_authoring` | 21 | 完整 |
| [局外成长底盘正式配置落地设计](设计文档/config/designs/40_局外成长底盘正式配置落地设计.md) | `config_design` | `active` | `formal_config_authoring` | 16 | 完整 |
| [局外成长效果特质正式配置落地设计](设计文档/config/designs/41_局外成长效果特质正式配置落地设计.md) | `config_design` | `active` | `formal_config_authoring` | 19 | 完整 |
| [局外成长义体正式配置落地设计](设计文档/config/designs/42_局外成长义体正式配置落地设计.md) | `config_design` | `active` | `formal_config_authoring` | 18 | 完整 |
| [局外成长制造维护正式配置落地设计](设计文档/config/designs/43_局外成长制造维护正式配置落地设计.md) | `config_design` | `active` | `formal_config_authoring` | 20 | 完整 |
| [局外成长人偶特质房间正式配置落地设计](设计文档/config/designs/44_局外成长人偶特质房间正式配置落地设计.md) | `config_design` | `active` | `formal_config_authoring` | 19 | 完整 |
| [经济压力核心正式配置落地设计](设计文档/config/designs/47_经济压力核心正式配置落地设计.md) | `config_design` | `active` | `formal_config_authoring` | 15 | 完整 |
| [经济压力传闻正式配置落地设计](设计文档/config/designs/48_经济压力传闻正式配置落地设计.md) | `config_design` | `active` | `formal_config_authoring` | 18 | 完整 |
| [经济压力势力正式配置落地设计](设计文档/config/designs/49_经济压力势力正式配置落地设计.md) | `config_design` | `active` | `formal_config_authoring` | 18 | 完整 |
| [经济压力订单正式配置落地设计](设计文档/config/designs/50_经济压力订单正式配置落地设计.md) | `config_design` | `active` | `formal_config_authoring` | 20 | 完整 |
| [局外成长正式配置ID锁定与冲突检查](设计文档/config/gates/39_局外成长正式配置ID锁定与冲突检查.md) | `audit` | `active` | `formal_config_authoring` | 18 | 完整 |
| [经济压力正式配置ID锁定与冲突检查](设计文档/config/gates/53_经济压力正式配置ID锁定与冲突检查.md) | `audit` | `active` | `formal_config_authoring` | 17 | 完整 |
| [经济压力正式配置README字段口径检查](设计文档/config/gates/54_经济压力正式配置README字段口径检查.md) | `audit` | `active` | `formal_config_authoring` | 17 | 完整 |
| [经济压力订单ID最终锁定表](设计文档/config/gates/55_经济压力订单ID最终锁定表.md) | `audit` | `active` | `formal_config_authoring` | 10 | 完整 |
| [正式配置源落地准入门禁](设计文档/config/gates/56_正式配置源落地准入门禁.md) | `gate` | `active` | `formal_config_authoring` | 7 | 完整 |
| [前三层正式配置实现任务拆分](设计文档/config/tasks/34_前三层正式配置实现任务拆分.md) | `task_split` | `active` | `formal_config_authoring` | 13 | 完整 |
| [局外成长正式配置实现任务拆分](设计文档/config/tasks/38_局外成长正式配置实现任务拆分.md) | `task_breakdown` | `active` | `formal_config_authoring` | 19 | 完整 |
| [经济压力正式配置实现任务拆分](设计文档/config/tasks/52_经济压力正式配置实现任务拆分.md) | `task_breakdown` | `active` | `formal_config_authoring` | 21 | 完整 |
| [前三层正式配置Validator与固定Seed验收样例](设计文档/config/validation/35_前三层正式配置Validator与固定Seed验收样例.md) | `acceptance_spec` | `active` | `formal_config_acceptance` | 7 | 完整 |
| [局外成长正式配置Validator与固定验收样例](设计文档/config/validation/45_局外成长正式配置Validator与固定验收样例.md) | `acceptance_spec` | `active` | `formal_config_acceptance` | 21 | 完整 |
| [经济压力正式配置Validator与固定验收样例](设计文档/config/validation/51_经济压力正式配置Validator与固定验收样例.md) | `acceptance_spec` | `active` | `formal_config_acceptance` | 16 | 完整 |
| [正式版内容生产规格](设计文档/content_packs/18_正式版内容生产规格.md) | `spec` | `active` | `content_authoring` | 15 | 完整 |
| [第一层正式核心内容包](设计文档/content_packs/19_第一层正式核心内容包.md) | `content_pack` | `active` | `content_authoring` | 24 | 完整 |
| [第二层背包压力内容包](设计文档/content_packs/20_第二层背包压力内容包.md) | `content_pack` | `active` | `content_authoring` | 25 | 完整 |
| [人偶成长修复内容包](设计文档/content_packs/21_人偶成长修复内容包.md) | `content_pack` | `active` | `content_authoring` | 28 | 完整 |
| [小镇经济月租内容包](设计文档/content_packs/22_小镇经济月租内容包.md) | `content_pack` | `active` | `content_authoring` | 25 | 完整 |
| [长期记忆剧情内容包](设计文档/content_packs/23_长期记忆剧情内容包.md) | `content_pack` | `active` | `content_authoring` | 21 | 完整 |
| [势力订单声望内容包](设计文档/content_packs/24_势力订单声望内容包.md) | `content_pack` | `active` | `content_authoring` | 25 | 完整 |
| [第三层路线侵蚀内容包](设计文档/content_packs/25_第三层路线侵蚀内容包.md) | `content_pack` | `active` | `content_authoring` | 25 | 完整 |
| [策划文档开发交付审计](设计文档/delivery/00_策划文档开发交付审计.md) | `audit` | `active` | `design_delivery` | 31 | 完整 |
| [策划交付落地矩阵](设计文档/delivery/12_策划交付落地矩阵.md) | `matrix` | `active` | `design_delivery` | 18 | 完整 |
| [策划跨系统验收场景矩阵](设计文档/delivery/13_策划跨系统验收场景矩阵.md) | `matrix` | `active` | `design_acceptance` | 16 | 完整 |
| [策划配置表现验收承接规格](设计文档/delivery/14_策划配置表现验收承接规格.md) | `handoff` | `active` | `design_delivery` | 8 | 完整 |
| [P0主干配置表现验收承接清单](设计文档/delivery/15_P0主干配置表现验收承接清单.md) | `checklist` | `active` | `design_delivery` | 18 | 完整 |
| [P1人偶成长情感承接清单](设计文档/delivery/16_P1人偶成长情感承接清单.md) | `checklist` | `active` | `design_delivery` | 17 | 完整 |
| [P2长期循环叙事承接清单](设计文档/delivery/17_P2长期循环叙事承接清单.md) | `checklist` | `active` | `design_delivery` | 20 | 完整 |
| [This Is the Police 演出参考](设计文档/参考/01_This_Is_the_Police_演出参考.md) | `reference` | `active` | `design_reference` | 7 | 完整 |
| [设计参考阅读入口](设计文档/参考/README.md) | `entry` | `active` | `design_reference` | 2 | 完整 |
| [局外时间与日程口径规则卡](设计文档/规则卡/01_局外时间与日程口径规则卡.md) | `rule_card` | `active` | `time_schedule` | 14 | 完整 |
| [物品、背包、旋转与生命周期规则卡](设计文档/规则卡/02_物品背包旋转与生命周期规则卡.md) | `rule_card` | `active` | `item_inventory_lifecycle` | 14 | 完整 |
| [战斗回合与怪物意图规则卡](设计文档/规则卡/03_战斗回合与怪物意图规则卡.md) | `rule_card` | `active` | `combat_turn_intent` | 12 | 完整 |
| [小镇经济结算与压力链规则卡](设计文档/规则卡/04_小镇经济结算与压力链规则卡.md) | `rule_card` | `active` | `town_economy_settlement` | 26 | 完整 |
| [局外成长与维护规则卡](设计文档/规则卡/05_局外成长与维护规则卡.md) | `rule_card` | `active` | `outgame_growth_maintenance` | 27 | 完整 |
| [标签与特质规则卡](设计文档/规则卡/06_标签与特质规则卡.md) | `rule_card` | `active` | `tag_trait_rules` | 26 | 完整 |
| [势力声望与订单规则卡](设计文档/规则卡/07_势力声望与订单规则卡.md) | `rule_card` | `active` | `faction_order_rules` | 24 | 完整 |
| [人偶核心状态与好感双轨规则卡](设计文档/规则卡/08_人偶核心状态与好感双轨规则卡.md) | `rule_card` | `active` | `doll_core_state_affection` | 23 | 完整 |
| [人偶交互事件与反馈规则卡](设计文档/规则卡/09_人偶交互事件与反馈规则卡.md) | `rule_card` | `active` | `doll_interaction_rules` | 20 | 完整 |
| [剧本调度与事件队列规则卡](设计文档/规则卡/10_剧本调度与事件队列规则卡.md) | `rule_card` | `active` | `scenario_event_queue` | 15 | 完整 |
| [人偶房间布局与视觉叙事规则卡](设计文档/规则卡/11_人偶房间布局与视觉叙事规则卡.md) | `rule_card` | `active` | `doll_room_rules` | 22 | 完整 |
| [局外底盘配置字段说明 (Chassis Config)](配置表(JSON)/Chassis/README.md) | `config` | `active` | `config_chassis` | 16 | 完整 |
| [工坊制造配方字段说明 (Crafting Recipes Config)](配置表(JSON)/CraftingRecipes/README.md) | `config` | `active` | `config_crafting` | 21 | 完整 |
| [人偶基础档案配置字段说明 (Dolls Config)](配置表(JSON)/Dolls/README.md) | `config` | `active` | `config_dolls` | 21 | 完整 |
| [深渊地图层级配置说明 (Dungeons Config)](配置表(JSON)/Dungeons/README.md) | `config` | `active` | `config_dungeons` | 18 | 完整 |
| [小镇经济配置字段说明 (Economy Config)](配置表(JSON)/Economy/README.md) | `config` | `active` | `config_economy` | 12 | 完整 |
| [效果配置字典说明 (EffectEnums Config)](配置表(JSON)/Effects/README.md) | `config` | `active` | `config_effects` | 17 | 完整 |
| [势力配置字段说明 (Factions Config)](配置表(JSON)/Factions/README.md) | `config` | `active` | `config_factions` | 11 | 完整 |
| [局内物品与网格实体字段说明 (Items Config)](配置表(JSON)/Items/README.md) | `config` | `active` | `config_items` | 17 | 完整 |
| [深渊怪物配置字段说明 (Monsters Config)](配置表(JSON)/Monsters/README.md) | `config` | `active` | `config_monsters` | 10 | 完整 |
| [Narrative 配置说明](配置表(JSON)/Narrative/README.md) | `config_readme` | `active` | `narrative_config` | 4 | 完整 |
| [订单配置字段说明 (Orders Config)](配置表(JSON)/Orders/README.md) | `config` | `active` | `config_orders` | 15 | 完整 |
| [义体插件配置字段说明 (Prosthetics Config)](配置表(JSON)/Prosthetics/README.md) | `config` | `active` | `config_prosthetics` | 16 | 完整 |
| [配置表数据总览说明 (Configuration Overview)](配置表(JSON)/README.md) | `config` | `active` | `config_data` | 23 | 完整 |
| [奖励表配置字段说明 (Rewards Config)](配置表(JSON)/Rewards/README.md) | `config` | `active` | `config_rewards` | 21 | 完整 |
| [传闻配置字段说明 (Rumors Config)](配置表(JSON)/Rumors/README.md) | `config` | `active` | `config_rumors` | 11 | 完整 |

### 美术

| 文档 | 类型 | 状态 | 领域 | 关联 | 元数据 |
|---|---|---|---|---|---|
| [美术 / UI 状态](agent_status/art.md) | `status` | `active` | `art_pipeline` | 30 | 完整 |
| [Approved 到 Unity 导入与 Registry 登记自动化实施计划](docs/plans/2026-07-18-art-approved-unity-registry-automation.md) | `plan` | `historical` | `art_asset_integration` | 0 | 完整 |
| [GIF 小循环人物替换实现计划](docs/plans/2026-07-18-gif-character-replacement.md) | `plan` | `historical` | `gif_character_replacement` | 1 | 完整 |
| [Art Runtime Validated State Implementation Plan](docs/plans/2026-07-19-art-runtime-validated-state.md) | `plan` | `historical` | `runtime_art_validation` | 0 | 完整 |
| [Formal V2 Replacement And UI Skin Implementation Plan](docs/plans/2026-07-19-formal-v2-replacement-and-ui-skin.md) | `plan` | `historical` | `art_pipeline` | 0 | 完整 |
| [Character Portrait Reference And Background Processing Implementation Plan](docs/plans/2026-07-26-character-portrait-reference-and-background-processing.md) | `plan` | `historical` | `character_portrait_production` | 0 | 完整 |
| [Art Catalog Integrity Implementation Plan](docs/plans/2026-08-02-art-catalog-integrity.md) | `plan` | `historical` | `art_pipeline` | 0 | 完整 |
| [Character Portrait Replacement and Set Gates Implementation Plan](docs/plans/2026-08-02-character-portrait-replacement-and-set-gates.md) | `plan` | `historical` | `character_portrait_production` | 0 | 完整 |
| [Approved 到 Unity 导入与 Registry 登记自动化设计](docs/specs/2026-07-18-art-approved-unity-registry-automation-design.md) | `design` | `historical` | `art_asset_integration` | 0 | 完整 |
| [美术资产统一数字轮次加工与安全背景处理设计](docs/specs/2026-07-18-unified-art-processing-rounds-design.md) | `design` | `historical` | `art_asset_processing` | 0 | 完整 |
| [零号对话中性立绘首轮流程试跑设计](docs/specs/2026-07-18-zero-dialogue-neutral-character-portrait-pilot-design.md) | `design` | `historical` | `character_portrait_production` | 0 | 完整 |
| [美术素材 Runtime Validated 状态收敛设计](docs/specs/2026-07-19-art-runtime-validated-state-design.md) | `design` | `historical` | `runtime_art_validation` | 0 | 完整 |
| [角色立绘参考资产解析与背景处理能力设计](docs/specs/2026-07-26-character-portrait-reference-and-background-processing-design.md) | `design` | `historical` | `character_portrait_production` | 0 | 完整 |
| [美术工具](tools/美术工具/README.md) | `tool` | `active` | `art_tooling` | 13 | 完整 |
| [MVP 美术与UI表现需求清单 (Art & UI Pipeline)](版本规划/_archive/mvp_2026-05/01_MVP美术与UI需求清单.md) | `plan` | `historical` | `mvp_planning` | 2 | 完整 |
| [美术智能体阅读入口](知识库/views/art.md) | `view` | `active` | `agent_context_view` | 18 | 完整 |
| [美术流水线总览](美术文档/00_美术流水线总览.md) | `art` | `active` | `art_pipeline` | 14 | 完整 |
| [Manifest 规范](美术文档/01_Manifest规范.md) | `art` | `active` | `art_manifest` | 6 | 完整 |
| [资源规格与接入规范](美术文档/02_资源规格与接入规范.md) | `art` | `active` | `art_asset_spec` | 10 | 完整 |
| [AI 美术资产质量与筛选准入规范](美术文档/03_AI生成与筛选规范.md) | `art` | `active` | `ai_art_quality` | 6 | 完整 |
| [美术风格基准](美术文档/04_美术风格基准.md) | `art` | `active` | `art_style` | 10 | 完整 |
| [AI 图片生成能力与网关契约](美术文档/05_AI图片网关接入方案.md) | `art` | `active` | `ai_image_gateway` | 9 | 完整 |
| [运行时美术验收记录](美术文档/09_运行时美术验收记录.md) | `art` | `active` | `runtime_art_validation` | 9 | 完整 |
| [正式版核心纵切美术路线](美术文档/10_正式版核心纵切美术路线.md) | `art` | `active` | `formal_art_route` | 20 | 完整 |
| [正式纵切 UI 与素材覆盖矩阵](美术文档/13_正式纵切UI与素材覆盖矩阵.md) | `art` | `active` | `ui_design` | 24 | 完整 |
| [FormalV2 素材候选审查记录](美术文档/14_FormalV2素材候选审查记录.md) | `art` | `active` | `art_pipeline` | 1 | 完整 |
| [Live2D角色动画资产接入规格](美术文档/16_Live2D角色动画资产接入规格.md) | `art` | `active` | `live2d_character_animation` | 9 | 完整 |
| [Agent原生动态立绘资产接入规格](美术文档/17_Agent原生动态立绘资产接入规格.md) | `art` | `active` | `dynamic_doll_puppet` | 12 | 完整 |
| [CG底图与漫画式播放演出工作流](美术文档/18_CG底图与漫画式播放演出工作流.md) | `art` | `active` | `narrative_cg_art_pipeline` | 4 | 完整 |
| [T0-01 序章 CG 细案](美术文档/19_T0-01序章CG细案.md) | `art` | `active` | `narrative_cg_art_pipeline` | 6 | 完整 |
| [GIF 小循环人物替换工作流](美术文档/20_GIF小循环人物替换工作流.md) | `art` | `active` | `gif_character_replacement` | 5 | 完整 |
| [美术文档索引](美术文档/README.md) | `art` | `active` | `art_pipeline` | 23 | 完整 |
| [零号AI后端出图提示词对比](美术文档/archive/04_零号AI后端出图提示词对比.md) | `art` | `historical` | `character_design_generation` | 4 | 完整 |
| [MVP 素材接入状态同步](美术文档/archive/06_MVP素材接入状态同步.md) | `art` | `historical` | `mvp_art_archive` | 2 | 完整 |
| [MVP UI 重新设计同步](美术文档/archive/07_MVP_UI重新设计同步.md) | `art` | `historical` | `mvp_ui_archive` | 2 | 完整 |
| [Unity 运行时美术验收工具需求与交付状态](美术文档/archive/08_Unity运行时美术验收工具需求.md) | `art` | `historical` | `runtime_art_validation` | 4 | 完整 |
| [P0 UI 骨架接入交付](美术文档/archive/11_P0_UI骨架接入交付.md) | `art` | `historical` | `ui_handoff` | 7 | 完整 |
| [P1 Formal V1 UI 接入准备](美术文档/archive/12_P1_UI骨架接入准备.md) | `art` | `historical` | `ui_handoff` | 7 | 完整 |
| [FormalV2 Runtime Acceptance Checklist](美术文档/archive/15_FormalV2运行时验收待办清单.md) | `art` | `historical` | `art_acceptance` | 3 | 完整 |
| [美术归档文档](美术文档/archive/README.md) | `art` | `active` | `art_archive` | 7 | 完整 |
| [UI 设计流水线](美术文档/ui_design/README.md) | `art` | `active` | `art_pipeline` | 23 | 完整 |
| [营业结算演出界面 Formal V1](美术文档/ui_design/formal_v1/business_settlement_v1.md) | `art` | `active` | `ui_design` | 7 | 完整 |
| [底盘升级界面 Formal V1](美术文档/ui_design/formal_v1/chassis_upgrade_panel_v1.md) | `art` | `active` | `ui_design` | 4 | 完整 |
| [战斗界面 Formal V1](美术文档/ui_design/formal_v1/combat_hud_v1.md) | `art` | `active` | `ui_design` | 3 | 完整 |
| [每日账单报告界面 Formal V1](美术文档/ui_design/formal_v1/daily_bill_report_v1.md) | `art` | `active` | `ui_design` | 4 | 完整 |
| [人偶交互界面 Formal V1](美术文档/ui_design/formal_v1/doll_interaction_v1.md) | `art` | `active` | `ui_design` | 5 | 完整 |
| [人偶房间界面 Formal V1](美术文档/ui_design/formal_v1/doll_room_v1.md) | `art` | `active` | `ui_design` | 5 | 完整 |
| [深渊地图界面 Formal V1](美术文档/ui_design/formal_v1/dungeon_map_v1.md) | `art` | `active` | `ui_design` | 2 | 完整 |
| [势力商店界面 Formal V1](美术文档/ui_design/formal_v1/faction_shop_v1.md) | `art` | `active` | `ui_design` | 5 | 完整 |
| [战利品拾取界面 Formal V1](美术文档/ui_design/formal_v1/inventory_loot_v1.md) | `art` | `draft` | `ui_design` | 2 | 完整 |
| [出发层选择界面 Formal V1](美术文档/ui_design/formal_v1/layer_select_v1.md) | `art` | `active` | `ui_design` | 2 | 完整 |
| [机体维护整备界面 Formal V1](美术文档/ui_design/formal_v1/maintenance_panel_v1.md) | `art` | `active` | `ui_design` | 4 | 完整 |
| [势力订单板界面 Formal V1](美术文档/ui_design/formal_v1/order_board_v1.md) | `art` | `active` | `ui_design` | 4 | 完整 |
| [义体制造界面 Formal V1](美术文档/ui_design/formal_v1/prosthetic_panel_v1.md) | `art` | `active` | `ui_design` | 2 | 完整 |
| [传闻情报板界面 Formal V1](美术文档/ui_design/formal_v1/rumor_board_v1.md) | `art` | `active` | `ui_design` | 4 | 完整 |
| [深渊安全区界面 Formal V1](美术文档/ui_design/formal_v1/safe_room_v1.md) | `art` | `active` | `ui_design` | 3 | 完整 |
| [剧本事件界面 Formal V1](美术文档/ui_design/formal_v1/scenario_event_v1.md) | `art` | `active` | `ui_design` | 5 | 完整 |
| [正式版 UI 结构 V1 总览](美术文档/ui_design/formal_v1/screen_structure_review.md) | `art` | `active` | `ui_design` | 32 | 完整 |
| [工坊出售界面 Formal V1](美术文档/ui_design/formal_v1/sell_panel_v1.md) | `art` | `active` | `ui_design` | 2 | 完整 |
| [结算界面 Formal V1](美术文档/ui_design/formal_v1/settlement_v1.md) | `art` | `active` | `ui_design` | 2 | 完整 |
| [出货分配界面 Formal V1](美术文档/ui_design/formal_v1/shop_staging_v1.md) | `art` | `active` | `ui_design` | 4 | 完整 |
| [深渊阶梯房间界面 Formal V1](美术文档/ui_design/formal_v1/stairs_room_v1.md) | `art` | `active` | `ui_design` | 3 | 完整 |
| [工坊主界面 Formal V1](美术文档/ui_design/formal_v1/workshop_main_v1.md) | `art` | `draft` | `ui_design` | 2 | 完整 |
| [Formal V2 UX/UI 重构总方案](美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md) | `art` | `draft` | `ui_design` | 32 | 完整 |
| [Workshop Main Formal V2 方案](美术文档/ui_design/formal_v2/01_workshop_main_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Combat HUD Formal V2 方案](美术文档/ui_design/formal_v2/02_combat_hud_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Inventory Loot Formal V2 方案](美术文档/ui_design/formal_v2/03_inventory_loot_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Dungeon Map Formal V2 方案](美术文档/ui_design/formal_v2/04_dungeon_map_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Settlement Formal V2 方案](美术文档/ui_design/formal_v2/05_settlement_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Maintenance Panel Formal V2 方案](美术文档/ui_design/formal_v2/06_maintenance_panel_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Prosthetic Panel Formal V2 方案](美术文档/ui_design/formal_v2/07_prosthetic_panel_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Chassis Upgrade Panel Formal V2 方案](美术文档/ui_design/formal_v2/08_chassis_upgrade_panel_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Sell Panel Formal V2 方案](美术文档/ui_design/formal_v2/09_sell_panel_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Shop Staging Formal V2 方案](美术文档/ui_design/formal_v2/10_shop_staging_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Business Settlement Formal V2 方案](美术文档/ui_design/formal_v2/11_business_settlement_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Daily Bill Report Formal V2 方案](美术文档/ui_design/formal_v2/12_daily_bill_report_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Layer Select Formal V2 方案](美术文档/ui_design/formal_v2/13_layer_select_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Safe Room Formal V2 方案](美术文档/ui_design/formal_v2/14_safe_room_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Stairs Room Formal V2 方案](美术文档/ui_design/formal_v2/15_stairs_room_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Order Board Formal V2 方案](美术文档/ui_design/formal_v2/16_order_board_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Rumor Board Formal V2 方案](美术文档/ui_design/formal_v2/17_rumor_board_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Faction Shop Formal V2 方案](美术文档/ui_design/formal_v2/18_faction_shop_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Doll Interaction Formal V2 方案](美术文档/ui_design/formal_v2/19_doll_interaction_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Scenario Event Formal V2 方案](美术文档/ui_design/formal_v2/20_scenario_event_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Doll Room Formal V2 方案](美术文档/ui_design/formal_v2/21_doll_room_v2.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Formal V2 UX/UI 设计层](美术文档/ui_design/formal_v2/README.md) | `art` | `draft` | `ui_design` | 33 | 完整 |
| [Formal V2 UI 概念参考图](美术文档/ui_design/formal_v2/concepts/README.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Formal V2 UI 概念图评审索引](美术文档/ui_design/formal_v2/concepts/review_index.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [Formal V2 UI 结构设计图](美术文档/ui_design/formal_v2/design_boards/README.md) | `art` | `draft` | `ui_design` | 5 | 完整 |
| [UI 交付检查清单](美术文档/ui_design/handoff_checklist.md) | `art` | `active` | `art_pipeline` | 6 | 完整 |
| [UI 设计迭代与版本迁移流程](美术文档/ui_design/ui_iteration_process.md) | `art` | `active` | `ui_design` | 9 | 完整 |
| [UI 设计版本管理](美术文档/ui_design/versions/README.md) | `art` | `active` | `ui_design` | 6 | 完整 |
| [Formal V1 Candidate Optional Staging](美术文档/ui_design/versions/formal_v1_candidate/README.md) | `art` | `draft` | `ui_design` | 3 | 完整 |
| [UI 设计版本迁移记录](美术文档/ui_design/versions/migration_log.md) | `art` | `active` | `ui_design` | 16 | 完整 |
| [MVP UI Baseline 2026-05-22](美术文档/ui_design/versions/mvp_baseline_2026-05-22/README.md) | `art` | `frozen` | `ui_design` | 3 | 完整 |
| [人设参考获取规则](美术文档/人设/01_人设参考获取规则.md) | `art` | `active` | `character_design_reference` | 8 | 完整 |
| [零号原型参考：失明少女（漆黑的子弹）](美术文档/人设/02_零号原型参考_失明少女.md) | `art` | `active` | `character_design_reference` | 7 | 完整 |
| [零号初版人设方案](美术文档/人设/03_零号初版人设方案.md) | `art` | `active` | `character_design` | 8 | 完整 |
| [零号立绘素材设计与交付清单](美术文档/人设/05_零号立绘素材设计与交付清单.md) | `art` | `active` | `character_portrait_asset_design` | 8 | 完整 |
| [零号高频对话差分Gemini首批筛选记录](美术文档/人设/AI出图/zero_dialogue_differences_20260712_01/selection_review.md) | `art` | `active` | `character_dialogue_portrait_review` | 1 | 完整 |
| [零号P0身份锚点Gemini首批筛选记录](美术文档/人设/AI出图/zero_portrait_p0_20260711_01/selection_review.md) | `art` | `active` | `character_portrait_generation_review` | 1 | 完整 |
| [人设文档入口](美术文档/人设/README.md) | `art` | `active` | `character_design` | 10 | 完整 |

## 元数据缺口

- [Formal V2 Prompt Readiness](美术文档/archive/formal_v2_prompt_readiness_snapshots/20260609_052040_formalv2_program_integrate_prompt_ready_ascii_cmd_20260609.md)：缺少 `id, title, type, role, domain, status`
- [Formal V2 Prompt Readiness](美术文档/archive/formal_v2_prompt_readiness_snapshots/20260609_084349_formalv2_continue_20260609.md)：缺少 `id, title, type, role, domain, status`
- [Formal V2 Prompt Readiness](美术文档/archive/formal_v2_prompt_readiness_snapshots/20260609_090042_formalv2_after_acceptance_check_20260609.md)：缺少 `id, title, type, role, domain, status`
- [Formal V2 Prompt Readiness](美术文档/archive/formal_v2_prompt_readiness_snapshots/20260609_131044_formalv2_remaining_items_after_sync_20260609.md)：缺少 `id, title, type, role, domain, status`
