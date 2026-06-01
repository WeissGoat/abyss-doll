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
last_verified: 2026-06-01
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
| `shop_staging_formal_v2_concept.png` | `shop_staging` | `10_shop_staging_v2.md` | 左库存、中央陈列台、右订单 / 黑市侧箱的店面摆货参考。 |
| `business_settlement_formal_v2_concept.png` | `business_settlement` | `11_business_settlement_v2.md` | 顾客流、成交反馈、未售出风险和进入账单动作参考。 |
| `daily_bill_report_formal_v2_concept.png` | `daily_bill_report` | `12_daily_bill_report_v2.md` | 打开账本、收入 / 支出页和月租压力轨参考。 |

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
