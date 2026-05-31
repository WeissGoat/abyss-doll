---
id: art_ui_formal_v2_readme
title: Formal V2 UX/UI 设计层
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: true
related:
  - 美术文档/README.md
  - 美术文档/ui_design/README.md
  - 美术文档/ui_design/ui_iteration_process.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v2/01_workshop_main_v2.md
  - 美术文档/ui_design/formal_v2/02_combat_hud_v2.md
  - 美术文档/ui_design/formal_v2/03_inventory_loot_v2.md
  - 美术文档/ui_design/formal_v2/04_dungeon_map_v2.md
  - 美术文档/ui_design/formal_v2/05_settlement_v2.md
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
  - 知识库/views/art.md
last_verified: 2026-05-29
update_rule: 新增 Formal V2 设计批次、确认规则或界面设计文档时同步本文件。
---

# Formal V2 UX/UI 设计层

> **定位：** Formal V2 是针对 Formal V1 “功能按钮堆叠、正式感不足”的 UX/UI 重构设计层。这里的文档是 design draft，不是程序接入口。

## 当前规则

1. `screen_layouts.json` 仍是当前 active 对接规格，程序和素材生成只读 active。
2. `formal_v2/*.md` 只记录新设计方案；用户确认前不改 active，不生成素材，不要求程序接入。
3. 每个界面先完成详细方案，再按确认顺序迁移到 active。
4. Formal V2 的核心目标是重建玩家流程、主次行动、场景隐喻和信息层级，不是简单换按钮皮肤。
5. Formal V2 允许继续复用已稳定 VisualID；新增 VisualID 必须等 active 更新后再进入 Manifest。
6. Figma、Unity MCP 或额外自动化工具只作为设计 / 验收辅助；任何正式接入口仍必须回到 active `screen_layouts.json`、Manifest 和 ArtAcceptance。

## 文件职责

| 文件 | 职责 |
|---|---|
| `00_formal_v2_ux_ui_overview.md` | Formal V2 总体 UX/UI 重构方案、批次、设计原则和验收门槛。 |
| `01_workshop_main_v2.md` | 工坊 Hub 审查入口，按确认顺序扩展为完整方案。 |
| `02_combat_hud_v2.md` | 战斗界面审查入口，按确认顺序扩展为完整方案。 |
| `03_inventory_loot_v2.md` | 战利品清点界面审查入口，按确认顺序扩展为完整方案。 |
| `04_dungeon_map_v2.md` | 深渊地图界面审查入口，按确认顺序扩展为完整方案。 |
| `05_settlement_v2.md` | 结算界面审查入口，按确认顺序扩展为完整方案。 |

## 当前完成度

截至 2026-05-31，V2-A 五个核心界面都已补齐可评审草案：

| ScreenID | 草案状态 | 当前建议 |
|---|---|---|
| `workshop_main` | 已完成详细草案 | 优先给用户确认。 |
| `combat_hud` | 已完成详细草案 | 工坊确认后审。 |
| `inventory_loot` | 已完成详细草案 | 和战斗 / 战后流程一起审。 |
| `dungeon_map` | 已完成详细草案 | 和 P2 地图节奏一起审。 |
| `settlement` | 已完成详细草案 | 和 CombatOutcomeReport / 账本流一起审。 |

## 设计确认顺序

| 批次 | 界面 | 目标 |
|---|---|---|
| V2-A | `workshop_main` | 把主界面从按钮菜单改为正式工坊 Hub。 |
| V2-A | `combat_hud` | 把战斗界面从信息堆叠改为战斗舞台 + 背包指令区。 |
| V2-A | `inventory_loot` | 把拾取界面改为撤离清点、容量压力和带出决策。 |
| V2-A | `dungeon_map` | 把地图界面改为路线决策、风险阅读和节点预览。 |
| V2-A | `settlement` | 把结算界面改为结果报告、损失收益和下一步行动。 |
| V2-B | `maintenance_panel`、`prosthetic_panel`、`chassis_upgrade_panel`、`sell_panel`、`shop_staging`、`daily_bill_report`、`business_settlement` | 把局外功能面板收敛为工坊工作台、账本、市场和制造维护子流程。 |
| V2-C | `order_board`、`rumor_board`、`faction_shop`、`layer_select`、`safe_room`、`stairs_room`、`doll_interaction`、`doll_room`、`scenario_event` | 把长期系统、深渊房间和叙事界面补成正式结构。 |

## 单界面方案模板

每个 Formal V2 界面文档必须覆盖：

1. 当前 Formal V1 问题。
2. 玩家在该界面的真实目标。
3. Formal V2 体验定位。
4. 主区域结构。
5. 信息层级。
6. 主行动 / 次行动 / 危险行动。
7. 需要隐藏、合并或降级的按钮。
8. 场景隐喻。
9. 程序迁移影响。
10. 素材需求变化。
11. 运行时 UX 验收标准。
