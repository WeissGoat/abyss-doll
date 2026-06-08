---
id: art_formal_v2_asset_candidate_review
title: FormalV2 素材候选审查记录
type: art
role: 美术
domain: art_pipeline
status: active
source_of_truth: true
related:
  - 美术文档/README.md
  - 美术文档/_generated/美术需求候选清单.md
  - 美术文档/_generated/缺图生成计划.md
last_verified: 2026-06-09
update_rule: 每轮美术需求候选扫描后，如有人工准入、暂缓或误报裁决，更新本文。
---

# FormalV2 素材候选审查记录

> 本文记录 `Scan-ArtRequirementCandidates.ps1` 扫出的潜在美术需求如何被美术侧裁决。它不是 Manifest，也不是程序接入口；确认准入后仍以 `art_requirements_seed.json`、`art_manifest.json`、缺图生成计划和可接入素材清单为准。

## 2026-06-09 FormalV2 候选准入

### 输入

- 候选快照：`美术文档/_generated/art_requirement_candidate_snapshots/20260609_055217_formalv2_goal_resume_20260609.md`
- 初始扫描结果：`new_candidate=84`、`approved_without_manifest=0`、`seed_only=0`、`manifest_managed=215`
- 当前目标：继续以 FormalV2 全量素材实际迭代为目标，处理配置、FormalV2 UI 设计和内容包中新出现但尚未纳入 Manifest 的素材需求。

### 准入原则

- 配置 JSON 已经直接引用的 VisualID 优先准入。
- FormalV2 active / 已确认设计中明确要求的场景背景，且不适合复用旧背景时准入。
- 内容包已定义、后续房间叙事需要展示的纪念物准入。
- 已完成配置或已锁定设计中的订单 / 传闻图标准入。
- 概念图、设计图、layout board、布局区块、列表区、strip、cabinet、panel 等扫描误报不准入。
- 已有正式替代 VisualID 的旧 ID 不准入，避免程序侧出现重复资产口径。

### 本轮准入

本轮新增 29 个 seed 条目，并已刷新 Manifest / Prompt / 队列。

| 类别 | VisualID | 数量 | 裁决 |
|---|---|---:|---|
| FormalV2 场景背景 | `bg_workshop_home_room`, `bg_workshop_studio`, `bg_town_shop`, `bg_shop_staging`, `bg_daily_bill` | 5 | 准入。用于工坊主界面、工作室、小镇商店、出货陈列和每日账本。 |
| 战斗反馈 | `ui_combat_feedback_echo_fade`, `ui_combat_feedback_scrap_break`, `ui_combat_feedback_slime_pop` | 3 | 准入。配置表怪物已通过 `DefeatFeedbackRef` 直接引用。 |
| 房间纪念物 | `memento_blackmarket_letter`, `memento_debt_shadow_window`, `memento_first_return_tag`, `memento_low_san_blanket`, `memento_pawn_empty_tag`, `memento_truth_red_thread`, `memento_wall_crack_first_defeat` | 7 | 准入。来自长期记忆内容包和房间视觉叙事规则。 |
| 订单图标 | `order_spore_cage_procurement_icon`, `order_crystal_scale_batch_icon`, `order_blackmarket_relic_box_icon`, `order_workshop_spore_core_frame_icon`, `order_mage_oracle_sample_icon`, `order_black_contested_core_buyout_icon` | 6 | 准入。覆盖 C2 / C3 已完成或已锁定的订单表现。 |
| 传闻图标 | `rumor_filter_shortage_icon`, `rumor_miner_lamps_icon`, `rumor_old_blades_icon`, `rumor_crystal_scale_contract_icon`, `rumor_acid_gland_shortage_icon`, `rumor_living_mycelium_shortage_icon`, `rumor_spore_amber_jeweler_icon`, `rumor_oracle_fossil_collector_icon` | 8 | 准入。覆盖第一层、第二层和第三层行情传闻表现。 |

### 本轮不准入 / 暂缓

| 类型 | 示例 | 裁决 |
|---|---|---|
| 已有 item 替代的订单目标物 | `order_contested_spore_core_icon`, `order_live_spore_cage_icon` | 不准入。已有 `item_order_contested_spore_core_icon`、`item_order_live_spore_cage_icon` 作为运行时物品图标，避免重复。 |
| 屏幕入口派生图标 | `faction_shop_icon`, `order_board_icon`, `rumor_board_icon` | 暂缓。当前已有共享 UI 图标和界面背景，除非 active UI 明确要求独立入口大图标，否则不进 Manifest。 |
| FormalV2 概念图 / 设计图误报 | `*_formal_v2_concept_icon`, `*_formal_v2_design_board_icon` | 不准入。这些是评审图文件，不是运行时素材。 |
| 布局区块误报 | `prosthetic_cabinet_icon`, `prosthetic_list_icon`, `order_risk_strip_icon`, `chassis_delta_strip_icon`, `rumor_notes_area_icon` | 不准入。它们是 UI 区域名或布局槽，不应作为独立 PNG 资产。 |
| 旧底盘 ID | `chassis_lv1_basic_icon`, `chassis_lv2_expanded_icon` | 暂缓。当前正式底盘口径已使用 `chassis_standard_frame_icon`、`chassis_compact_raider_icon`、`chassis_bulwark_carrier_icon`。 |
| 更细的商店 / 势力背景 | `bg_faction_shop`, `bg_faction_shop_counter` | 暂缓。先用 `bg_town_shop` 承接小镇商店 / 势力商店；如后续 faction_shop 需要专属背景，再拆分。 |

### 输出证据

- `art_requirements_seed.json`：新增 29 个 seed，`UpdatedAt=2026-06-09`。
- `art_manifest.json`：刷新后 `Entries=284`。
- `AI绘图提示词清单.md`：新增 29 项 Prompt / NegativePrompt / Spec。
- `美术需求候选清单.md`：刷新后 `new_candidate=55`、`seed_only=0`、`manifest_managed=244`。
- `可接入素材清单.md`：刷新后 `program_integrate=58`、`generate_needed=29`。
- `缺图生成计划.md`：刷新后 `planned=29`、`prompt_ready=29`，批次 `nai_formalv2_candidate_triage_20260609_01`。

### 后续动作

1. 当前 `NAI_ACCESS_TOKEN` 未设置，本轮不调用 NovelAI，不生成 mock / local_v0 冒充正式图。
2. token 和额度可用后，按 `缺图生成计划.md` 串行跑图：`-Concurrency 1`、`-DelaySeconds 1`。
3. 生成、预处理、筛选并同步 Approved 后，刷新 `可接入素材清单` 和 `程序接入交接清单`。
4. 剩余 55 个 `new_candidate` 下轮继续审查，重点过滤布局误报和旧 ID。
