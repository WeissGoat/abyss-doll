---
id: art_ui_formal_v1_structure_review
title: 正式版 UI 结构 V1 总览
type: art
role: 美术
domain: ui_design
status: active
source_of_truth: true
related:
  - 美术文档/ui_design/README.md
  - agent_status/art.md
  - 知识库/views/art.md
  - 美术文档/ui_design/ui_iteration_process.md
  - 美术文档/ui_design/formal_v1/business_settlement_v1.md
  - 美术文档/ui_design/formal_v1/combat_hud_v1.md
  - 美术文档/ui_design/formal_v1/chassis_upgrade_panel_v1.md
  - 美术文档/ui_design/formal_v1/daily_bill_report_v1.md
  - 美术文档/ui_design/formal_v1/doll_interaction_v1.md
  - 美术文档/ui_design/formal_v1/doll_room_v1.md
  - 美术文档/ui_design/formal_v1/dungeon_map_v1.md
  - 美术文档/ui_design/formal_v1/faction_shop_v1.md
  - 美术文档/ui_design/formal_v1/inventory_loot_v1.md
  - 美术文档/ui_design/formal_v1/layer_select_v1.md
  - 美术文档/ui_design/formal_v1/maintenance_panel_v1.md
  - 美术文档/ui_design/formal_v1/order_board_v1.md
  - 美术文档/ui_design/formal_v1/prosthetic_panel_v1.md
  - 美术文档/ui_design/formal_v1/rumor_board_v1.md
  - 美术文档/ui_design/formal_v1/safe_room_v1.md
  - 美术文档/ui_design/formal_v1/scenario_event_v1.md
  - 美术文档/ui_design/formal_v1/sell_panel_v1.md
  - 美术文档/ui_design/formal_v1/settlement_v1.md
  - 美术文档/ui_design/formal_v1/shop_staging_v1.md
  - 美术文档/ui_design/formal_v1/stairs_room_v1.md
  - 美术文档/ui_design/formal_v1/workshop_main_v1.md
  - 美术文档/ui_design/versions/README.md
  - 美术文档/ui_design/versions/formal_v1_candidate/README.md
  - 美术文档/09_运行时美术验收记录.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
last_verified: 2026-05-25
update_rule: 修改正式版 UI 结构、界面迁移顺序或程序接入口径时同步本文件。
---

# 正式版 UI 结构 V1 总览

> **定位：** 在 MVP UI 骨架已可运行的基础上，重新审查核心界面的正式版结构。Formal V1 只定义结构、信息层级、交互区域和程序对象边界，不追求最终美术品质、动画和 VFX 一次到位。

---

## 1. 为什么需要 Formal V1

当前 P0 三个界面已经通过运行时验收：

```text
workshop_main
combat_hud
inventory_loot
```

这说明美术资源接入链路、VisualID、Unity Registry、运行时截图验收是通的。但这不等于正式版 UI 结构已经成立。部分界面仍继承 MVP 验证阶段的结构，例如战斗界面以敌人卡片承载敌方表现，更像调试信息卡，而不是正式战斗舞台。

Formal V1 的目标是：

```text
MVP 可运行骨架
  -> Formal V1 正式结构
  -> 程序分批迁移
  -> 运行时验收
  -> 进入批次的素材 / 动效 / VFX 按正式标准验收
```

---

## 2. MVP Baseline 与 Formal V1 的关系

| 层级 | 作用 | 是否可回退 | 是否代表最终结构 |
|---|---|---:|---:|
| MVP Baseline | 功能闭环、资源接入、验收链路验证 | 是 | 否 |
| Formal V1 | 正式版主区域、信息层级、交互边界 | 否，需验收后替代 | 是，作为后续迭代基础 |
| Visual V2/V3 | 更高品质素材、字体、动效、VFX | 可逐项替换 | 不改变主结构 |

Formal V1 不应直接删除 MVP 经验。MVP Baseline 作为可运行对照保留，Formal V1 作为下一轮结构迁移目标。

---

## 3. Formal V1 结构原则

1. **舞台和 UI 分离。** 角色、敌人、房间、地图节点是游戏世界表现；面板、按钮、列表、状态条是 UI 控件。正式结构不应把所有对象都做成卡片。
2. **核心玩法区优先。** 背包、目标选择、战斗站位、结算结果必须一眼可读，装饰不能抢主流程。
3. **背包格保持 100x100。** 玩法计算以 100x100 格为主，美术只调整容器位置、边框、状态皮肤，不缩放玩法格来解决拥挤。
4. **文字不烘焙进图片。** 名称、价格、数值、按钮文案、说明继续由 Unity Text 渲染。
5. **VisualID 稳定优先。** Formal V1 可以新增 VisualID，但已稳定的通用组件尽量复用，后续替换图片不改程序绑定。
6. **先结构，后品质。** 第一轮可以继续用现有背景、立绘、怪物图、按钮皮肤；只要结构是正式版方向。
7. **逐界面迁移。** 不一次性推翻所有界面。每个界面单独设计、接入、截图验收、推进状态。

---

## 4. 界面优先级

| 优先级 | ScreenID | Formal V1 重点 |
|---|---|---|
| P0-A | `combat_hud` | 已写入 active Formal V1：左玩家、右敌方实体、底部居中背包、敌人脚下血条。 |
| P0-B | `workshop_main` | 已写入 active Formal V1：正式工坊工作台、魔偶维护、服务入口和出发整备。 |
| P0-C | `inventory_loot` | 已写入 active Formal V1：战后清点、左侧背包、右侧战利品缓存、容量压力和确认区。 |
| P1-A | `dungeon_map` | 已写入 active Formal V1：深渊路线地图、层级推进、选中节点详情和背包整理入口。 |
| P1-B | `settlement` | 已写入 active Formal V1：CombatOutcomeReport 五态结果报告、收益损失摘要、带出/遗失明细和返回工坊动作。 |
| P1-C | `layer_select` | 已写入 active Formal V1：出发层列表、锁定状态、当前选择和确认下潜。 |
| P1-D | `sell_panel` | 已写入 active Formal V1：仓库物品、单件出售、批量出售、估值和金币回流。 |
| P1-E | `prosthetic_panel` | 已写入 active Formal V1：配方、材料缺口、可制造状态和已装备状态。 |
| P1-F | `safe_room` | 已写入 active Formal V1：安全区休整、撤离/继续深入、背包整理和魔偶通讯。 |
| P1-G | `stairs_room` | 已写入 active Formal V1：阶梯房间、下一层风险、撤离/深入决策和背包整理。 |
| P1-H | `maintenance_panel` | 已写入 active Formal V1：机体维护、磨损/侵蚀、维护费用、材料缺口和下潜许可检查。 |
| P1-I | `daily_bill_report` | 已写入 active Formal V1：每日收支、未售出物、维护/租金压力和欠债风险。 |
| P2-A | `shop_staging` | 已写入 active Formal V1：仓库出货分配、普通渠道、订单渠道、黑市渠道和收益风险预览。 |
| P2-A2 | `business_settlement` | 已写入 active Formal V1：开始营业后的顾客流、成交爆点、金币增长、未售出/黑市风险摘要和进入每日账单过渡。 |
| P2-B | `order_board` | 已写入 active Formal V1：势力订单列表、目标物、截止日、奖励预览和接取/提交动作。 |
| P2-C | `rumor_board` | 已写入 active Formal V1：今日传闻、价格涨跌、选中详情和推荐行动。 |
| P2-D | `faction_shop` | 已写入 active Formal V1：势力声望、专属商品、黑市信任和交易风险。 |
| P2-E | `doll_interaction` | 已写入 active Formal V1：触摸、对话、赠礼、保养、特殊交互和反馈。 |
| P2-F | `scenario_event` | 已写入 active Formal V1：AVG、系统弹窗、气泡、LorePanel、事件日志和跳过摘要。 |
| P3-A | `doll_room` | 已写入 active Formal V1：房间背景、待机人偶、纪念物、窗外状态和日记。 |
| P3-B | `chassis_upgrade_panel` | 已写入 active Formal V1：当前底盘、下一底盘、容量变化、材料缺口、蓝图前置和升级确认。 |

P0 / P1 / P2 / P3 当前 21 个界面均已进入 active `screen_layouts.json` 规格。后续程序接入不读取本目录草案，而是读取 active 规格、`ui_design_handoff.md` 和可接入素材清单。

---

## 5. 迁移状态建议

当前 P0 / P1 / P2 / P3 已完成从 MVP Baseline 或新增界面规划到 Formal V1 active 规格的第一轮迁移。当前没有剩余 draft UI；后续新增界面仍按同一流程处理：先写版本设计文档，用户确认后修改 active `screen_layouts.json`。

`versions/formal_v1_candidate/` 只作为复杂界面的可选暂存区。`combat_hud` 本轮已在用户确认关键结构后直接写入 active `screen_layouts.json`；后续更复杂界面仍可先在 candidate 中试写结构化 JSON，再合并到 active。

建议新增或使用以下字段：

```json
{
  "StructureVersion": "FormalV1",
  "PreviousValidatedVersion": "MVPBaseline",
  "LayoutStatus": "active_spec"
}
```

状态解释：

| 状态 | 含义 |
|---|---|
| `draft` | Formal V1 结构已写入规格，等待审查或资源检查。 |
| `active_spec` | 已写入 active `screen_layouts.json`，可作为程序和素材生成入口。 |
| `handoff` | Formal V1 结构已确认，可交给程序迁移。 |
| `integrated` | 程序已接入 Formal V1，等待运行时验收。 |
| `validated` | Formal V1 已通过 ArtAcceptance 和美术侧截图验收。 |

---

## 6. 程序迁移口径

每个界面迁移时，程序侧应拿到：

1. 正式结构说明文档。
2. 结构差异表。
3. 需要新增或调整的 Unity 节点。
4. 继续复用的 VisualID。
5. 需要新增的 VisualID 或临时 fallback。
6. ArtAcceptance 需要截图和检查的运行时状态。

迁移时优先保留现有业务逻辑，先调整表现层结构。只有当 MVP 结构把游戏对象错误地建成 UI 卡片时，才需要调整对象边界，例如 `combat_hud` 的敌人表现。

---

## 7. 当前结论

1. P0 / P1 / P2 / P3 的 21 个界面均已进入 active Formal V1 规格。
2. `combat_hud` 已有第一批战斗实体和脚底阴影/目标光环入库，并已完成一次程序接入验证。
3. `workshop_main`、`inventory_loot`、`dungeon_map`、`settlement`、`layer_select`、`sell_panel`、`prosthetic_panel`、`safe_room`、`stairs_room`、`maintenance_panel`、`daily_bill_report`、`shop_staging`、`business_settlement`、`order_board`、`rumor_board`、`faction_shop`、`doll_interaction`、`scenario_event`、`doll_room`、`chassis_upgrade_panel` 当前重点是程序按 active 规格迁移后截图验收。
4. `settlement` 已补齐 `ui_settlement_outcome_victory`、`ui_settlement_outcome_hp_defeat`、`ui_settlement_outcome_san_collapse`、`ui_settlement_outcome_hp_san_defeat`、`ui_settlement_outcome_party_wipe` 五个结果徽记；`doll_room` 已补齐 `bg_doll_room_attic`、`ui_icon_diary`、`ui_room_memento_slot` 的 local_v0 Approved 可接入素材；`chassis_upgrade_panel` 已补齐 `ui_icon_chassis_upgrade`、`ui_icon_blueprint`、`ui_icon_material_need` 的素材需求入口；`business_settlement` 已补齐 `ui_icon_customer`、`ui_icon_sale_spark`、`ui_icon_business_settlement` 的素材需求入口；后续正式 AI 版可同名替换。
5. Formal V1 第一轮只保证正式结构、信息层级和资源槽位；后续 Visual V2/V3 再逐批替换更高品质素材、动画和 VFX。
6. 每轮实际生成、预处理或 Approved 同步后，美术侧必须刷新 `美术文档/_generated/可接入素材清单.md/json` 并保留快照，程序侧按 `program_integrate` 队列自助接入。
