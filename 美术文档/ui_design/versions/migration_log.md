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
  - 美术文档/ui_design/formal_v1/maintenance_panel_v1.md
  - 美术文档/ui_design/formal_v1/daily_bill_report_v1.md
  - 美术文档/ui_design/formal_v1/order_board_v1.md
  - 美术文档/ui_design/formal_v1/rumor_board_v1.md
  - 美术文档/ui_design/formal_v1/shop_staging_v1.md
  - 美术文档/ui_design/formal_v1/faction_shop_v1.md
  - 美术文档/ui_design/formal_v1/doll_interaction_v1.md
  - 美术文档/ui_design/formal_v1/scenario_event_v1.md
  - 美术文档/ui_design/formal_v1/doll_room_v1.md
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
| `safe_room` | 新增 Formal V1 | 无 | `active_spec` | `active_spec` | 已写入 Formal V1 active 规格：安全区休整、撤离/继续深入、背包整理和魔偶通讯。 |
| `stairs_room` | 新增 Formal V1 | 无 | `active_spec` | `active_spec` | 已写入 Formal V1 active 规格：阶梯房间、下一层风险、撤离/深入决策和背包整理。 |
| `maintenance_panel` | 新增 Formal V1 | 无 | `active_spec` | `active_spec` | 已写入 Formal V1 active 规格：机体维护、磨损/侵蚀、维护费用、材料缺口和下潜许可检查。 |
| `daily_bill_report` | 新增 Formal V1 | 无 | `active_spec` | `active_spec` | 已写入 Formal V1 active 规格：每日收支、未售出物、维护/租金压力和欠债风险。 |
| `shop_staging` | 新增 Formal V1 | 无 | `active_spec` | `active_spec` | 已写入 Formal V1 active 规格：仓库出货分配、普通渠道、订单渠道、黑市渠道和收益风险预览。 |
| `order_board` | 新增 Formal V1 | 无 | `active_spec` | `active_spec` | 已写入 Formal V1 active 规格：势力订单列表、目标物、截止日、奖励预览和接取/提交动作。 |
| `rumor_board` | 新增 Formal V1 | 无 | `active_spec` | `active_spec` | 已写入 Formal V1 active 规格：今日传闻、价格涨跌、选中详情和推荐行动。 |
| `faction_shop` | 新增 Formal V1 | 无 | 未进入 active | `design_draft` | 已有设计草案：势力声望、专属商品、黑市信任和交易风险；等待用户确认后写入 active。 |
| `doll_interaction` | 新增 Formal V1 | 无 | 未进入 active | `design_draft` | 已有设计草案：触摸、对话、赠礼、保养、特殊交互和反馈；等待用户确认后写入 active。 |
| `scenario_event` | 新增 Formal V1 | 无 | 未进入 active | `design_draft` | 已有设计草案：AVG、系统弹窗、气泡、LorePanel、事件日志和跳过摘要；等待用户确认后写入 active。 |
| `doll_room` | 新增 Formal V1 | 无 | 未进入 active | `design_draft` | 已有设计草案：房间背景、待机人偶、纪念物、窗外状态和日记；等待用户确认后写入 active。 |

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

### 2026-05-24：A3 房间节点写入 Formal V1 active 规格

* `safe_room` 作为深渊安全区界面进入 active 规格，拆分 `rest_status_panel`、`room_action_panel`、`safe_inventory_workbench` 和 `doll_comm_panel`。
* `stairs_room` 作为阶梯房间界面进入 active 规格，拆分 `next_layer_briefing`、`stairs_choice_panel`、`carry_risk_panel` 和 `stairs_inventory_workbench`。
* 两个房间界面都复用全局背包对象，保持背包格 `100x100` 与 `5` 间距，不创建第二套背包数据。
* `Validate-UIDesign.ps1` 已通过，并重新生成 `美术文档/ui_design/_generated/ui_design_handoff.md`。

### 2026-05-24：维护与每日账单写入 Formal V1 active 规格

* `maintenance_panel` 作为 A3 局外成长维护界面进入 active 规格，拆分 `doll_condition_panel`、`dive_readiness_panel`、`material_cost_list`、`wear_corrosion_panel` 和 `repair_action_panel`。
* `daily_bill_report` 作为 A4 经济压力账单界面进入 active 规格，拆分 `bill_summary_panel`、`pressure_warning_panel`、`unsold_goods_panel`、`income_expense_list` 和 `bill_action_panel`。
* 新增 `ui_icon_maintenance`、`ui_icon_bill`、`ui_icon_warning` 三个 preset UI 图标需求，用于维护、账单和风险提示。
* 两个界面都复用 `bg_workshop_day` 与通用面板/按钮/列表组件，不新增整张不可拆账单或维护背景。

### 2026-05-24：A4 出货、订单与传闻写入 Formal V1 active 规格

* `shop_staging` 作为 A4 经济压力出货分配界面进入 active 规格，拆分 `storage_item_list`、`channel_lane_board`、`staging_preview_panel`、`staging_action_panel` 和 `staging_bottom_hint`。
* `order_board` 作为 A4 势力订单板界面进入 active 规格，拆分 `order_list_panel`、`order_detail_panel`、`order_reward_panel` 和 `order_action_panel`。
* `rumor_board` 作为 A4 时间 / 传闻决策界面进入 active 规格，拆分 `today_rumor_list`、`price_wave_panel`、`rumor_detail_panel`、`recommendation_panel` 和 `rumor_bottom_hint`。
* 新增 `ui_icon_shop_channel`、`ui_icon_black_market`、`ui_icon_order`、`ui_icon_faction`、`ui_icon_deadline`、`ui_icon_rumor`、`ui_icon_price_up`、`ui_icon_price_down` 八个 preset UI 图标需求，后续按 Manifest / Prompt 流程串行跑图。

### 2026-05-24：P3 四个界面建立 Formal V1 设计草案

* `faction_shop` 已有设计草案，用于承接势力声望、专属商品、黑市信任和交易风险。
* `doll_interaction` 已有设计草案，用于承接触摸、对话、赠礼、保养、特殊交互和反馈。
* `scenario_event` 已有设计草案，用于承接 AVG、系统弹窗、气泡、LorePanel、事件日志和跳过摘要。
* `doll_room` 已有设计草案，用于承接房间背景、待机人偶、纪念物、窗外状态和日记。
* 这四个界面尚未写入 active `screen_layouts.json`，暂不作为程序接入口，也不触发 Manifest / Prompt / 素材生成。
