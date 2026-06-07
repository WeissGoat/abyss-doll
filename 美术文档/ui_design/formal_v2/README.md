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
  - 美术文档/ui_design/formal_v2/13_layer_select_v2.md
  - 美术文档/ui_design/formal_v2/14_safe_room_v2.md
  - 美术文档/ui_design/formal_v2/15_stairs_room_v2.md
  - 美术文档/ui_design/formal_v2/16_order_board_v2.md
  - 美术文档/ui_design/formal_v2/17_rumor_board_v2.md
  - 美术文档/ui_design/formal_v2/18_faction_shop_v2.md
  - 美术文档/ui_design/formal_v2/19_doll_interaction_v2.md
  - 美术文档/ui_design/formal_v2/20_scenario_event_v2.md
  - 美术文档/ui_design/formal_v2/21_doll_room_v2.md
  - 美术文档/ui_design/formal_v2/concepts/README.md
  - 美术文档/ui_design/formal_v2/concepts/review_index.md
  - 美术文档/ui_design/formal_v2/design_boards/README.md
  - tools/美术工具/README.md
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
  - 知识库/views/art.md
last_verified: 2026-06-07
update_rule: 新增 Formal V2 设计批次、确认规则或界面设计文档时同步本文件。
---

# Formal V2 UX/UI 设计层

> **定位：** Formal V2 是针对 Formal V1 “功能按钮堆叠、正式感不足”的 UX/UI 重构设计层。这里的文档是 design draft，不是程序接入口。

## 当前规则

1. `screen_layouts.json` 仍是当前 active 对接规格，程序和素材生成只读 active。
2. `formal_v2/*.md` 只记录新设计方案；用户确认前不改 active，不生成素材，不要求程序接入。
3. 2026-06-08 用户已评审并认可 `concepts/review_index.md` 当前 Formal V2 概念方向；下一阶段允许先迁移 V2-A 核心流程到 active，但 V2-B / V2-C 仍保持 draft，等待后续批次确认。
4. 每个界面先完成详细方案，再按确认顺序迁移到 active。
5. Formal V2 的核心目标是重建玩家流程、主次行动、场景隐喻和信息层级，不是简单换按钮皮肤。
6. Formal V2 默认风格为日系二次元地底奇幻 + 轻蒸汽工艺 + 低信息密度，避免欧美暗黑写实、硬核工业控制台和密集按钮墙。
7. Formal V2 允许继续复用已稳定 VisualID；新增 VisualID 必须等 active 更新后再进入 Manifest。
8. Figma、Unity MCP 或额外自动化工具只作为设计 / 验收辅助；任何正式接入口仍必须回到 active `screen_layouts.json`、Manifest 和 ArtAcceptance。
9. `concepts/` 只存放 Formal V2 概念参考图；这些图片不进入 Approved、Manifest 或程序接入队列。
10. 设计图 / 概念图默认用 Codex 内置 `image_gen` 生成；若当前工具环境没有暴露 `image_gen`，必须先提醒用户并等待确认，不能自动切到 NovelAI、AI 图片网关、mock 或本地脚本。

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
| `13_layer_select_v2.md` | 下潜层选择界面草案：深渊剖面、锁定层、整备检查和开始下潜。 |
| `14_safe_room_v2.md` | 安全屋界面草案：安全营地、休整、背包整理、撤离和继续深入。 |
| `15_stairs_room_v2.md` | 阶梯房间界面草案：下降口、下一层风险、撤离 / 深入决策和最后整理。 |
| `16_order_board_v2.md` | 订单委托板草案：公告板合同、目标物、期限、奖励和接取 / 提交。 |
| `17_rumor_board_v2.md` | 传闻行情板草案：情报纸条、价格波动、推荐计划和行动入口。 |
| `18_faction_shop_v2.md` | 势力商店草案：势力柜台、声望账本、货架、黑市风险和购买。 |
| `19_doll_interaction_v2.md` | 魔偶互动草案：围绕魔偶的触摸、对话、赠礼、护理和反馈。 |
| `20_scenario_event_v2.md` | 剧情事件草案：当前场景叠加故事卡、选项、结果和继续。 |
| `21_doll_room_v2.md` | 魔偶房间草案：房间叙事、待机魔偶、纪念物、日记和低密度热点。 |
| `concepts/` | AI 生成的 Formal V2 概念参考图，仅用于结构、氛围和视觉重心评审。 |
| `concepts/review_index.md` | AI 概念图评审索引，按 V2-A / V2-B / V2-C 汇总 contact sheet 和评审口径。 |
| `design_boards/` | 结构设计图：每个 active 界面一张 16:9 layout board，外加 `workshop_studio` 拆分图。 |

## 当前完成度

截至 2026-06-08，active UI 规格共有 21 个界面。Formal V2 已补齐 21 个 active 界面的可评审草案，并额外保留 `workshop_studio` 作为 `workshop_main` 的设计拆分图。用户已评审并认可 `concepts/review_index.md` 当前概念方向；下一阶段进入 V2-A active 迁移，优先处理 `workshop_main` / `workshop_studio`、`combat_hud`、`inventory_loot`、`dungeon_map` 和 `settlement`。V2-B / V2-C 仍作为 draft 保留，不在本批次触发程序接入。

当前设计图覆盖：

| 类型 | 数量 | 位置 | 状态 |
|---|---:|---|---|
| Formal V2 结构设计图 | 22 | `design_boards/` | 21 个 active 界面 + `workshop_studio` 已全部生成。 |
| AI 概念参考图 | 22 | `concepts/` | 21 个 active 界面 + `workshop_studio` 已全部生成。 |
| 已归档旧概念图 | 8 | `concepts/archive/` | 4 张语义废弃图 + 3 张风格重出前旧图 + 1 张层地图纵深重出前旧图。 |
| 待重出 AI 概念图 | 0 | `concepts/README.md` 记录 | 2026-06-07 已用 Codex 内置 `image_gen` 重出 `workshop_main`、`combat_hud`、`dungeon_map`，并再次按层地图纵深规则重出 `dungeon_map`。 |

| ScreenID | 草案状态 | 当前建议 |
|---|---|---|
| `workshop_main` / `workshop_studio` | 已完成详细草案和结构设计图 | 优先给用户确认主界面与工作室拆分。 |
| `combat_hud` | 已完成详细草案和结构设计图 | 工坊确认后审。 |
| `inventory_loot` | 已完成详细草案和结构设计图 | 和战斗 / 战后流程一起审。 |
| `dungeon_map` | 已完成详细草案和结构设计图 | 和 P2 地图节奏一起审。 |
| `settlement` | 已完成详细草案和结构设计图 | 和 CombatOutcomeReport / 账本流一起审。 |
| `maintenance_panel` | 已完成详细草案和结构设计图 | 作为 `workshop_studio` 的维护子面板审，不再作为独立大场景。 |
| `prosthetic_panel` | 已完成详细草案和结构设计图 | 作为 `workshop_studio` 的义体子面板审，不再作为独立大场景。 |
| `chassis_upgrade_panel` | 已完成详细草案和结构设计图 | 作为 `workshop_studio` 的底盘子面板审，不再作为独立大场景。 |
| `sell_panel` | 已按小镇商店语义修订，并完成结构设计图 | 不再负责工坊即时卖出；工坊卖出 / 出货由 `shop_staging` 承接。 |
| `shop_staging` | 已完成详细草案和结构设计图 | 和 P4 经济压力的营业前摆货流程一起审。 |
| `business_settlement` | 已完成详细草案和结构设计图 | 和 `daily_bill_report` 区分营业演出 / 最终账单。 |
| `daily_bill_report` | 已完成详细草案和结构设计图 | 和月租 / 债务压力表现一起审。 |
| `order_board` | 已完成详细草案和结构设计图 | V2-C 评审。 |
| `rumor_board` | 已完成详细草案和结构设计图 | V2-C 评审。 |
| `faction_shop` | 已完成详细草案和结构设计图 | V2-C 评审。 |
| `layer_select` | 已完成详细草案和结构设计图 | V2-C 评审。 |
| `safe_room` | 已完成详细草案和结构设计图 | V2-C 评审。 |
| `stairs_room` | 已完成详细草案和结构设计图 | V2-C 评审。 |
| `doll_interaction` | 已完成详细草案和结构设计图 | V2-C 评审。 |
| `doll_room` | 已完成详细草案和结构设计图 | V2-C 评审。 |
| `scenario_event` | 已完成详细草案和结构设计图 | V2-C 评审。 |

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
