---
id: art_ui_formal_v2_concepts
title: Formal V2 UI 概念参考图
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/design_boards/README.md
  - 美术文档/ui_design/formal_v2/concepts/review_index.md
last_verified: 2026-06-06
update_rule: 新增、替换或废弃 Formal V2 概念参考图时同步本文件。
---

# Formal V2 UI 概念参考图

本目录存放 AI 生成的 Formal V2 UI 设计参考图，用于辅助评审整体布局、视觉重心和界面氛围。

这些图片不是运行时素材，不进入 `UnityClient/Assets/Art/Approved`，不写入 Manifest，也不作为程序接入规格。程序接入仍以用户确认后的 active `screen_layouts.json` 为准。

## 使用规则

1. 只看结构、区域关系、主次行动和整体氛围。
2. 图片中的伪文字、乱码、装饰性小字不作为最终 UI 文案。
3. 图片中的控件细节不直接等同于 Unity Prefab 或 VisualID。
4. 用户确认结构后，先更新 Formal V2 文档，再迁移 active 规格，最后才进入素材生成和程序接入。
5. 若后续替换概念图，保留同屏幕命名并在本文件记录原因。

## 当前文件

| 文件 | 对应界面 | 对应设计文档 | 用途 |
|---|---|---|---|
| `workshop_main_formal_v2_concept.png` | `workshop_main` | `01_workshop_main_v2.md` | 魔偶中心安心房间、深渊入口和少量房间热点参考。 |
| `workshop_studio_formal_v2_concept.png` | `workshop_studio` | `01_workshop_main_v2.md` | 工作室子界面参考：左侧背包，右侧魔偶坐在机械改造椅上。 |
| `combat_hud_formal_v2_concept.png` | `combat_hud` | `02_combat_hud_v2.md` | 左右战斗舞台、底部背包指令区、战斗状态信息层级参考。 |
| `inventory_loot_formal_v2_concept.png` | `inventory_loot` | `03_inventory_loot_v2.md` | 半透明战斗场景清点层：中央背包，奖励散落在背包外。 |
| `dungeon_map_formal_v2_concept.png` | `dungeon_map` | `04_dungeon_map_v2.md` | 地图和节点作为视觉中心，暂不做常驻选中节点详情。 |
| `settlement_formal_v2_concept.png` | `settlement` | `05_settlement_v2.md` | 结算报告、收益损失、状态变化和下一步行动参考。 |
| `maintenance_panel_formal_v2_concept.png` | `maintenance_panel` | `06_maintenance_panel_v2.md` | `workshop_studio` 内维护子面板：护理舱、诊断板和维护方案托盘参考。 |
| `prosthetic_panel_formal_v2_concept.png` | `prosthetic_panel` | `07_prosthetic_panel_v2.md` | `workshop_studio` 内义体子面板：义体柜、工作托盘和魔偶改造椅参考。 |
| `chassis_upgrade_panel_formal_v2_concept.png` | `chassis_upgrade_panel` | `08_chassis_upgrade_panel_v2.md` | `workshop_studio` 内底盘子面板：蓝图桌、底盘对比和材料 token 参考。 |
| `sell_panel_formal_v2_concept.png` | `sell_panel` | `09_sell_panel_v2.md` | 小镇商店 / 市场交易：商店柜台、货架、价格反馈和买卖动作参考。 |
| `shop_staging_formal_v2_concept.png` | `shop_staging` | `10_shop_staging_v2.md` | 左库存、中央陈列台、右订单 / 黑市侧箱的店面摆货参考。 |
| `business_settlement_formal_v2_concept.png` | `business_settlement` | `11_business_settlement_v2.md` | 顾客流、成交反馈、未售出风险和进入账单动作参考。 |
| `daily_bill_report_formal_v2_concept.png` | `daily_bill_report` | `12_daily_bill_report_v2.md` | 打开账本、收入 / 支出页和月租压力轨参考。 |
| `layer_select_formal_v2_concept.png` | `layer_select` | `13_layer_select_v2.md` | 深渊剖面、锁定层、整备检查和开始下潜参考。 |
| `safe_room_formal_v2_concept.png` | `safe_room` | `14_safe_room_v2.md` | 安全营地、休整、背包整理、撤离和继续深入参考。 |
| `stairs_room_formal_v2_concept.png` | `stairs_room` | `15_stairs_room_v2.md` | 下降口、下一层风险、撤离 / 深入决策和最后整理参考。 |
| `order_board_formal_v2_concept.png` | `order_board` | `16_order_board_v2.md` | 公告板合同、目标物、期限、奖励和接取 / 提交参考。 |
| `rumor_board_formal_v2_concept.png` | `rumor_board` | `17_rumor_board_v2.md` | 情报纸条、价格波动、推荐计划和行动入口参考。 |
| `faction_shop_formal_v2_concept.png` | `faction_shop` | `18_faction_shop_v2.md` | 势力柜台、声望账本、货架、黑市风险和购买参考。 |
| `doll_interaction_formal_v2_concept.png` | `doll_interaction` | `19_doll_interaction_v2.md` | 魔偶触摸、对话、赠礼、护理和反馈参考。 |
| `scenario_event_formal_v2_concept.png` | `scenario_event` | `20_scenario_event_v2.md` | 场景叠加故事卡、选项、结果和继续参考。 |
| `doll_room_formal_v2_concept.png` | `doll_room` | `21_doll_room_v2.md` | 房间叙事、待机魔偶、纪念物、日记和低密度热点参考。 |

## 评审总览图

| 文件 | 内容 | 用途 |
|---|---|---|
| `contact_sheets/formal_v2_a_core_flow.png` | V2-A 核心流程 6 张概念图。 | 优先评审主流程结构、视觉重心和风格方向。 |
| `contact_sheets/formal_v2_b_outgame.png` | V2-B 局外功能 7 张概念图。 | 评审工作室子面板、小镇商店和经营链路。 |
| `contact_sheets/formal_v2_c_longterm.png` | V2-C 长期系统 / 叙事 9 张概念图。 | 评审深渊房间、小镇信息板、魔偶互动和叙事界面。 |
| `contact_sheets/formal_v2_all_concepts.png` | 22 张 Formal V2 AI 概念图总览。 | 横向检查整体风格统一性和信息密度。 |

这些 contact sheet 只是评审辅助图，不是新的界面设计源文件，也不进入 Approved、Manifest 或程序接入清单。

## 概念图覆盖状态

截至 2026-06-06，本目录只记录 AI 氛围概念图，不记录结构设计图。全部 Formal V2 结构设计图已经放在 `../design_boards/`，覆盖 21 个 active 界面和 `workshop_studio` 拆分图。

| 类别 | 数量 | 状态 |
|---|---:|---|
| 当前可评审 AI 概念图 | 22 | 保留在本目录根路径，覆盖 21 个 active 界面和 `workshop_studio`。 |
| 已归档 AI 概念图 | 4 | 旧维护 / 义体 / 底盘独立大场景和旧 `sell_panel` 语义已归档。 |
| 待补 / 待重出 AI 概念图 | 0 | 本轮已用内置 imagegen 补齐。 |
| 结构设计图 | 22 | 已在 `../design_boards/` 生成，不属于 AI 概念图。 |

2026-06-02 探测结果：本地 `tools/美术工具/ai_image_gateway.local.yaml` 的 NovelAI access token 返回 HTTP 401 Unauthorized，导致 13 张 AI 概念图当时未生成。

2026-06-06 补齐结果：已改用内置 imagegen 生成并保存 13 张缺口概念图，覆盖维护 / 义体 / 底盘子面板、小镇商店和 V2-C 九个界面。新增图片分辨率统一为 `1672x941`，只作为结构、氛围和视觉重心评审参考，不进入 Approved、Manifest 或程序接入清单。

## 2026-06-01 方向修正

本轮概念图已按用户反馈替换为更温暖、奇幻、低信息密度的版本：

- `workshop_main` 从功能工坊改为以魔偶为中心的安心房间。
- 新增 `workshop_studio`，承接背包、底盘、义体和改造椅。
- `inventory_loot` 改为战斗场景上叠半透明清点层，中央背包，奖励散落在背包外。
- `dungeon_map` 改为地图 / 节点主视觉，暂不需要常驻选中节点详情。
- `combat_hud` 和 `settlement` 保持原结构方向，但风格改为更干净、更奇幻、少硬核工业。

## 2026-06-01 V2-B 局外功能概念图

本轮曾补齐 7 个局外功能界面的概念参考图。根据 2026-06-01 反馈，当前目录只保留可继续评审的摆货、营业结算和每日账单三张图。

- 维护 / 义体 / 底盘升级不再作为独立大场景评审，改为 `workshop_studio` 内可切换的不同面板或弹出窗口，后续应围绕同一工作室底图重新出图。
- `sell_panel` 不再表达工坊即时卖出；工坊卖出由 `shop_staging` 出货分配承接。`sell_panel` 后续改作小镇商店 / 市场交易界面，需重新设计和出图。
- 摆货 / 营业结算 / 每日账单仍与 P4 经济压力流程一起评审，重点看是否从按钮列表转为清晰的店铺经营动作。
- `shop_staging` 当前概念图底部有少量 AI 裁切噪声，不影响评审中央陈列台和左右功能区；若该界面进入 active 迁移，应在正式 blockout 中重新约束安全边距。

## 归档文件

| 文件 | 归档位置 | 原因 |
|---|---|---|
| `maintenance_panel_formal_v2_concept.png` | `archive/2026-06-01_workshop_studio_and_shop_semantics/` | 场景应归入 `workshop_studio` 子面板，不作为独立大场景。 |
| `prosthetic_panel_formal_v2_concept.png` | `archive/2026-06-01_workshop_studio_and_shop_semantics/` | 场景应归入 `workshop_studio` 子面板，不作为独立大场景。 |
| `chassis_upgrade_panel_formal_v2_concept.png` | `archive/2026-06-01_workshop_studio_and_shop_semantics/` | 场景应归入 `workshop_studio` 子面板，不作为独立大场景。 |
| `sell_panel_formal_v2_concept.png` | `archive/2026-06-01_workshop_studio_and_shop_semantics/` | 语义错误：工坊卖出已由 `shop_staging` 承接，`sell_panel` 应改作小镇商店。 |

## 当前限制

- 当前图片分辨率为 `1672x941`，只作为横屏比例参考，不代表最终 Unity 参考分辨率。
- AI 生成图存在伪文字和局部装饰噪声，正式 UI 需要由 Unity 文本、图标和九宫格皮肤重建。
- 当前图片不替代低保真线框；如果结构争议较大，应继续补低保真 blockout 或 Figma / Unity mock。
