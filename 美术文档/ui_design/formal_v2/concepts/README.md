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
| `workshop_main_formal_v2_concept.png` | `workshop_main` | `01_workshop_main_v2.md` | 工坊 Hub、深渊入口、工作台与侧栏功能分区参考。 |
| `combat_hud_formal_v2_concept.png` | `combat_hud` | `02_combat_hud_v2.md` | 左右战斗舞台、底部背包指令区、战斗状态信息层级参考。 |
| `inventory_loot_formal_v2_concept.png` | `inventory_loot` | `03_inventory_loot_v2.md` | 战利品清点、容量压力、带出决策和对比区域参考。 |
| `dungeon_map_formal_v2_concept.png` | `dungeon_map` | `04_dungeon_map_v2.md` | 路线图、节点风险阅读、节点详情和进入行动参考。 |
| `settlement_formal_v2_concept.png` | `settlement` | `05_settlement_v2.md` | 结算报告、收益损失、状态变化和下一步行动参考。 |

## 当前限制

- 当前图片分辨率为 `1672x941`，只作为横屏比例参考，不代表最终 Unity 参考分辨率。
- AI 生成图存在伪文字和局部装饰噪声，正式 UI 需要由 Unity 文本、图标和九宫格皮肤重建。
- 当前图片不替代低保真线框；如果结构争议较大，应继续补低保真 blockout 或 Figma / Unity mock。
