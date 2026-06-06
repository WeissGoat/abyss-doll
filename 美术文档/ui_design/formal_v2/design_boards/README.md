---
id: art_ui_formal_v2_design_boards
title: Formal V2 UI 结构设计图
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v2/concepts/README.md
  - 美术文档/ui_design/formal_v2/concepts/review_index.md
  - tools/美术工具/README.md
last_verified: 2026-06-06
update_rule: 新增或替换 Formal V2 UI 结构设计图时同步本文件。
---

# Formal V2 UI 结构设计图

本目录存放 Formal V2 的结构设计图。它们用于评审界面布局、视觉重心、主行动和信息层级，不是运行时素材，不进入 `UnityClient/Assets/Art/Approved`，不写入 Manifest，也不作为程序接入规格。

这些图与 `../concepts/` 的 AI 概念参考图分工不同：`concepts/` 主要看氛围和画面方向；本目录主要看功能结构和迁移前的 UI 骨架。

## 当前文件

| 文件 | ScreenID | 对应文档 | 说明 |
|---|---|---|---|
| `workshop_main_formal_v2_design_board.png` | `workshop_main` | `01_workshop_main_v2.md` | 工坊主界面 |
| `workshop_studio_formal_v2_design_board.png` | `workshop_studio` | `01_workshop_main_v2.md` | 工作室拆分界面 |
| `combat_hud_formal_v2_design_board.png` | `combat_hud` | `02_combat_hud_v2.md` | 战斗界面 |
| `inventory_loot_formal_v2_design_board.png` | `inventory_loot` | `03_inventory_loot_v2.md` | 战利品清点 |
| `dungeon_map_formal_v2_design_board.png` | `dungeon_map` | `04_dungeon_map_v2.md` | 深渊地图 |
| `settlement_formal_v2_design_board.png` | `settlement` | `05_settlement_v2.md` | 战斗结算 |
| `maintenance_panel_formal_v2_design_board.png` | `maintenance_panel` | `06_maintenance_panel_v2.md` | 维护子面板 |
| `prosthetic_panel_formal_v2_design_board.png` | `prosthetic_panel` | `07_prosthetic_panel_v2.md` | 义体子面板 |
| `chassis_upgrade_panel_formal_v2_design_board.png` | `chassis_upgrade_panel` | `08_chassis_upgrade_panel_v2.md` | 底盘升级子面板 |
| `sell_panel_formal_v2_design_board.png` | `sell_panel` | `09_sell_panel_v2.md` | 小镇商店 / 市场 |
| `shop_staging_formal_v2_design_board.png` | `shop_staging` | `10_shop_staging_v2.md` | 营业前摆货 |
| `business_settlement_formal_v2_design_board.png` | `business_settlement` | `11_business_settlement_v2.md` | 营业结算演出 |
| `daily_bill_report_formal_v2_design_board.png` | `daily_bill_report` | `12_daily_bill_report_v2.md` | 每日账单 |
| `layer_select_formal_v2_design_board.png` | `layer_select` | `13_layer_select_v2.md` | 下潜层选择 |
| `safe_room_formal_v2_design_board.png` | `safe_room` | `14_safe_room_v2.md` | 安全屋 |
| `stairs_room_formal_v2_design_board.png` | `stairs_room` | `15_stairs_room_v2.md` | 阶梯房间 |
| `order_board_formal_v2_design_board.png` | `order_board` | `16_order_board_v2.md` | 订单委托板 |
| `rumor_board_formal_v2_design_board.png` | `rumor_board` | `17_rumor_board_v2.md` | 传闻行情板 |
| `faction_shop_formal_v2_design_board.png` | `faction_shop` | `18_faction_shop_v2.md` | 势力商店 |
| `doll_interaction_formal_v2_design_board.png` | `doll_interaction` | `19_doll_interaction_v2.md` | 魔偶互动 |
| `scenario_event_formal_v2_design_board.png` | `scenario_event` | `20_scenario_event_v2.md` | 剧情事件 |
| `doll_room_formal_v2_design_board.png` | `doll_room` | `21_doll_room_v2.md` | 魔偶房间 |

## 使用规则

1. 只用于 Formal V2 草案评审和 active 迁移前沟通。
2. 图中文字和控件命名是结构标注，不是最终 UI 文案。
3. 若用户确认某个界面，仍需先更新 active `screen_layouts.json` 并通过 `Validate-UIDesign.ps1`。
4. 程序侧不得直接按本目录 PNG 接入 Unity；正式接入口仍是 active UI 规格和美术交接清单。
