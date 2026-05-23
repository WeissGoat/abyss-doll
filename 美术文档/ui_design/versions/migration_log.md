---
id: art_ui_migration_log
title: UI 设计版本迁移记录
type: art
role: 美术
domain: ui_design
status: active
source_of_truth: true
related:
  - 美术文档/ui_design/README.md
  - 美术文档/ui_design/ui_iteration_process.md
  - 美术文档/ui_design/versions/README.md
  - 美术文档/ui_design/versions/mvp_baseline_2026-05-22/README.md
  - 美术文档/ui_design/versions/formal_v1_candidate/README.md
last_verified: 2026-05-24
update_rule: 每次 UI 设计版本冻结、候选版本合并、程序接入或运行时验收后同步本文件。
---

# UI 设计版本迁移记录

> **定位：** 记录 UI 设计从一个版本迁移到另一个版本的过程，避免未冻结 baseline、未记录确认结论就修改 active，导致历史丢失或程序对接混乱。

---

## 1. 当前版本

| 项 | 值 |
|---|---|
| Active 规格 | `美术文档/ui_design/screen_layouts.json` |
| 当前 baseline | `versions/mvp_baseline_2026-05-22` |
| 当前 candidate | `versions/formal_v1_candidate`，可选暂存 |
| 迁移策略 | 默认设计确认后直接修改 active；复杂界面可先用 candidate 试写 |

---

## 2. 迁移总览

| ScreenID | Baseline Version | Candidate Version | Active 状态 | 迁移状态 | 备注 |
|---|---|---|---|---|---|
| `combat_hud` | `mvp_baseline_2026-05-22` | 可选 `formal_v1_candidate` | `active_spec` | `active_spec` | 已按 Formal V1 改为左玩家、右敌方实体、底部居中背包、敌人脚下血条；等待素材与程序接入。 |
| `workshop_main` | `mvp_baseline_2026-05-22` | 无 | `active_spec` | `active_spec` | 已写入 Formal V1 active 规格：正式工坊工作台、出发主行动、服务入口、魔偶维护和背包装配工作台。 |
| `inventory_loot` | `mvp_baseline_2026-05-22` | 无 | `active_spec` | `active_spec` | 已写入 Formal V1 active 规格：战后清点、左侧背包、右侧战利品缓存、容量压力和确认区。 |
| `dungeon_map` | `mvp_baseline_2026-05-22` | 无 | `active_spec` | `active_spec` | 已写入 Formal V1 active 规格：深渊路线地图、层级信息、选中节点详情和背包整理入口。 |
| `settlement` | `mvp_baseline_2026-05-22` | 无 | `active_spec` | `active_spec` | 已写入 Formal V1 active 规格：胜利/战败背景、结算主卡、摘要、明细和返回工坊行动。 |
| `sell_panel` | `mvp_baseline_2026-05-22` | 无 | `active_spec` | `active_spec` | 已写入 Formal V1 active 规格：出售主卡、仓库列表、单件出售、批量出售和估值。 |
| `prosthetic_panel` | `mvp_baseline_2026-05-22` | 无 | `active_spec` | `active_spec` | 已写入 Formal V1 active 规格：义体配方列表、材料缺口、制造按钮和已装备状态。 |
| `layer_select` | `mvp_baseline_2026-05-22` | 无 | `active_spec` | `active_spec` | 已写入 Formal V1 active 规格：出发层列表、锁定层、当前选择和确认下潜。 |

---

## 3. 状态枚举

| 状态 | 含义 |
|---|---|
| `pending` | 未进入当前候选版本。 |
| `design_draft` | 已有设计文档，等待确认。 |
| `active_spec` | 已写入 active `screen_layouts.json`，等待校验或交付。 |
| `candidate_design` | 已有候选结构文档，尚未写入候选 JSON；仅复杂界面使用。 |
| `candidate_spec` | 已写入候选 JSON，等待审查。 |
| `ready_to_merge` | 候选规格确认，可合并到 active。 |
| `merged_to_active` | 已合并到 active，等待程序接入。 |
| `integrated` | 程序已接入，等待运行时验收。 |
| `validated` | 当前 active 版本已通过运行时验收。 |
| `rejected` | 候选方案被否决或需重做。 |

---

## 4. 迁移记录

### 2026-05-23：建立 UI 版本化流程

* 冻结 `mvp_baseline_2026-05-22`。
* 建立 `formal_v1_candidate`，但明确为复杂界面的可选暂存区。
* 规定 `screen_layouts.json` 只代表 active 对接规格。
* Formal V1 默认在用户确认后逐界面修改 active；candidate 不作为必经流程。

### 2026-05-24：`combat_hud` 写入 Formal V1 active 规格

* 根据用户确认，将战斗背包从右下改为底部居中。
* 敌人主表现从卡片迁移为右侧战斗实体舞台，敌人血条和护盾条放到实体脚下。
* 明确现有 `monster_*_portrait` 只作为临时 fallback，正式验收需要 `monster_*_combat` 透明战斗实体素材。
* 新增 `ui_combat_entity_shadow` 与 `ui_combat_target_ring` 作为 Formal V1 战斗实体落点和选中提示资源。

### 2026-05-24：`workshop_main` / `inventory_loot` 写入 Formal V1 active 规格

* `workshop_main` 从 MVP Baseline 的主界面骨架迁移为正式工坊工作台结构，拆分 `status_cluster`、`expedition_panel`、`service_panel`、`backpack_workbench` 和 `doll_bay`。
* `inventory_loot` 从 MVP Baseline 的拾取弹窗迁移为正式战后清点结构，拆分 `loot_modal`、`inventory_grid`、`loot_cache`、`capacity_summary`、`item_detail` 和 `confirm_area`。
* 两个界面均保留背包玩法格 `100x100` 与 `5` 间距，不为了构图压缩格子规则。
* `Validate-UIDesign.ps1` 已通过，并重新生成 `美术文档/ui_design/_generated/ui_design_handoff.md`。

### 2026-05-24：P1 五个界面写入 Formal V1 active 规格

* `dungeon_map` 从节点按钮容器迁移为正式深渊路线地图结构，拆分 `layer_header`、`route_canvas`、`selected_node_detail` 和 `inventory_controls`。
* `settlement` 从结果弹窗迁移为胜利/战败结算仪式结构，拆分 `result_card`、`result_header`、`summary_area`、`loot_breakdown` 和 `continue_action`。
* `sell_panel`、`prosthetic_panel`、`layer_select` 从功能弹窗草案迁移为 active Formal V1 规格，并补齐对应正式结构文档。
* `Validate-UIDesign.ps1` 已通过，并重新生成 `美术文档/ui_design/_generated/ui_design_handoff.md`。
