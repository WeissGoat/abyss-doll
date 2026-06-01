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
  - 美术文档/ui_design/formal_v2/06_maintenance_panel_v2.md
  - 美术文档/ui_design/formal_v2/07_prosthetic_panel_v2.md
  - 美术文档/ui_design/formal_v2/08_chassis_upgrade_panel_v2.md
  - 美术文档/ui_design/formal_v2/09_sell_panel_v2.md
  - 美术文档/ui_design/formal_v2/10_shop_staging_v2.md
  - 美术文档/ui_design/formal_v2/11_business_settlement_v2.md
  - 美术文档/ui_design/formal_v2/12_daily_bill_report_v2.md
  - 美术文档/ui_design/formal_v2/concepts/README.md
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
  - 知识库/views/art.md
last_verified: 2026-06-01
update_rule: 新增 Formal V2 设计批次、确认规则或界面设计文档时同步本文件。
---

# Formal V2 UX/UI 设计层

> **定位：** Formal V2 是针对 Formal V1 “功能按钮堆叠、正式感不足”的 UX/UI 重构设计层。这里的文档是 design draft，不是程序接入口。

## 当前规则

1. `screen_layouts.json` 仍是当前 active 对接规格，程序和素材生成只读 active。
2. `formal_v2/*.md` 只记录新设计方案；用户确认前不改 active，不生成素材，不要求程序接入。
3. 每个界面先完成详细方案，再按确认顺序迁移到 active。
4. Formal V2 的核心目标是重建玩家流程、主次行动、场景隐喻和信息层级，不是简单换按钮皮肤。
5. Formal V2 默认风格为温暖奇幻 + 轻蒸汽工艺 + 低信息密度，避免硬核工业控制台和密集按钮墙。
6. Formal V2 允许继续复用已稳定 VisualID；新增 VisualID 必须等 active 更新后再进入 Manifest。
7. Figma、Unity MCP 或额外自动化工具只作为设计 / 验收辅助；任何正式接入口仍必须回到 active `screen_layouts.json`、Manifest 和 ArtAcceptance。
8. `concepts/` 只存放 Formal V2 概念参考图；这些图片不进入 Approved、Manifest 或程序接入队列。

## 文件职责

| 文件 | 职责 |
|---|---|
| `00_formal_v2_ux_ui_overview.md` | Formal V2 总体 UX/UI 重构方案、批次、设计原则和验收门槛。 |
| `01_workshop_main_v2.md` | 工坊主界面和工作室拆分方案：主界面是魔偶中心安心房间，工作室承接背包和改造椅。 |
| `02_combat_hud_v2.md` | 战斗界面草案：战斗舞台、底部背包指令区、敌方意图和单主行动。 |
| `03_inventory_loot_v2.md` | 战利品清点界面草案：半透明战斗场景清点层、中央背包和散落奖励。 |
| `04_dungeon_map_v2.md` | 深渊地图界面草案：地图 / 节点视觉中心、低按钮密度和节点详情降级。 |
| `05_settlement_v2.md` | 结算界面草案：结果报告、收益损失、状态变化和下一步行动。 |
| `06_maintenance_panel_v2.md` | `workshop_studio` 内维护子面板草案：共用工作室场景，切换护理舱、诊断板和维护方案托盘。 |
| `07_prosthetic_panel_v2.md` | `workshop_studio` 内义体子面板草案：共用工作室场景，切换义体柜、工作托盘和魔偶改造椅。 |
| `08_chassis_upgrade_panel_v2.md` | `workshop_studio` 内底盘子面板草案：共用工作室场景，切换蓝图桌、当前 / 下一底盘对比和材料 token。 |
| `09_sell_panel_v2.md` | 小镇商店 / 市场交易界面草案：从出货分配中拆出，负责商店浏览、买入 / 卖出和价格反馈。 |
| `10_shop_staging_v2.md` | 营业前摆货界面草案：店面陈列台、订单 / 黑市侧抽屉和 Start Business 主行动。 |
| `11_business_settlement_v2.md` | 营业结算演出界面草案：顾客流、成交爆点、金币增长和进入账单。 |
| `12_daily_bill_report_v2.md` | 每日账单界面草案：打开账本、今日结论、月租压力轨和结束当天。 |
| `concepts/` | AI 生成的 Formal V2 概念参考图，仅用于结构、氛围和视觉重心评审。 |

## 当前完成度

截至 2026-06-01，active UI 规格共有 21 个界面。Formal V2 已补齐 12 个界面 / 子界面的可评审草案，其中 9 张概念图仍保留在 `concepts/` 当前目录；维护、义体、底盘升级和旧 `sell_panel` 概念图已归档，等待按新语义重新出图。剩余 9 个 active 界面尚未补 Formal V2 草案。

| ScreenID | 草案状态 | 当前建议 |
|---|---|---|
| `workshop_main` / `workshop_studio` | 已完成详细草案 | 优先给用户确认主界面与工作室拆分。 |
| `combat_hud` | 已完成详细草案 | 工坊确认后审。 |
| `inventory_loot` | 已完成详细草案 | 和战斗 / 战后流程一起审。 |
| `dungeon_map` | 已完成详细草案 | 和 P2 地图节奏一起审。 |
| `settlement` | 已完成详细草案 | 和 CombatOutcomeReport / 账本流一起审。 |
| `maintenance_panel` | 已完成详细草案，需重出概念图 | 作为 `workshop_studio` 的维护子面板审，不再作为独立大场景。 |
| `prosthetic_panel` | 已完成详细草案，需重出概念图 | 作为 `workshop_studio` 的义体子面板审，不再作为独立大场景。 |
| `chassis_upgrade_panel` | 已完成详细草案，需重出概念图 | 作为 `workshop_studio` 的底盘子面板审，不再作为独立大场景。 |
| `sell_panel` | 已按小镇商店语义修订，需重出概念图 | 不再负责工坊即时卖出；工坊卖出 / 出货由 `shop_staging` 承接。 |
| `shop_staging` | 已完成详细草案 | 和 P4 经济压力的营业前摆货流程一起审。 |
| `business_settlement` | 已完成详细草案 | 和 `daily_bill_report` 区分营业演出 / 最终账单。 |
| `daily_bill_report` | 已完成详细草案 | 和月租 / 债务压力表现一起审。 |
| `order_board` | 未开始 V2 草案 | V2-C。 |
| `rumor_board` | 未开始 V2 草案 | V2-C。 |
| `faction_shop` | 未开始 V2 草案 | V2-C。 |
| `layer_select` | 未开始 V2 草案 | V2-C。 |
| `safe_room` | 未开始 V2 草案 | V2-C。 |
| `stairs_room` | 未开始 V2 草案 | V2-C。 |
| `doll_interaction` | 未开始 V2 草案 | V2-C。 |
| `doll_room` | 未开始 V2 草案 | V2-C。 |
| `scenario_event` | 未开始 V2 草案 | V2-C。 |

## 设计确认顺序

| 批次 | 界面 | 目标 |
|---|---|---|
| V2-A | `workshop_main` / `workshop_studio` | 把主界面从按钮菜单改为魔偶中心安心房间，并把背包 / 改造拆到工作室。 |
| V2-A | `combat_hud` | 把战斗界面从信息堆叠改为战斗舞台 + 背包指令区。 |
| V2-A | `inventory_loot` | 把拾取界面改为半透明战斗场景清点层，中央背包，奖励散落在背包外。 |
| V2-A | `dungeon_map` | 把地图界面改为地图 / 节点视觉中心，只保留必要按钮，暂不做常驻节点详情。 |
| V2-A | `settlement` | 把结算界面改为结果报告、损失收益和下一步行动。 |
| V2-B | `maintenance_panel`、`prosthetic_panel`、`chassis_upgrade_panel` | 作为 `workshop_studio` 内可切换子面板或弹出窗口审，不再作为独立大场景。 |
| V2-B | `shop_staging`、`daily_bill_report`、`business_settlement` | 把局外经营流程收敛为出货分配、营业演出和每日账本。 |
| V2-B | `sell_panel` | 改作小镇商店 / 市场交易界面，和工坊出货分配解耦。 |
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
