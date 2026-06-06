---
id: art_ui_formal_v2_concept_review_index
title: Formal V2 UI 概念图评审索引
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/concepts/README.md
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/design_boards/README.md
last_verified: 2026-06-06
update_rule: 新增、替换或废弃 Formal V2 概念图总览时同步本文件。
---

# Formal V2 UI 概念图评审索引

> 本索引用于快速评审 Formal V2 的 AI 氛围概念图。正式接入仍以用户确认后的 active `screen_layouts.json` 为准。

## 总览

| 分组 | Contact Sheet | 评审重点 |
|---|---|---|
| 全部界面 | `contact_sheets/formal_v2_all_concepts.png` | 整体风格是否统一、信息密度是否偏高、是否偏离温暖奇幻 + 轻蒸汽工艺。 |
| V2-A 核心流程 | `contact_sheets/formal_v2_a_core_flow.png` | 工坊、战斗、拾取、地图、结算是否建立正式主流程。 |
| V2-B 局外功能 | `contact_sheets/formal_v2_b_outgame.png` | 工作室子面板、商店和经营链路是否从按钮菜单转成清晰场景。 |
| V2-C 长期 / 叙事 | `contact_sheets/formal_v2_c_longterm.png` | 深渊房间、小镇信息板、魔偶互动和剧情事件是否有稳定视觉容器。 |

## 建议评审顺序

1. 先看 `formal_v2_all_concepts.png`，判断整体风格是否接受。
2. 再看 V2-A：`workshop_main` / `workshop_studio`、`combat_hud`、`inventory_loot`、`dungeon_map`、`settlement`。
3. 再看 V2-B：维护 / 义体 / 底盘是否都像 `workshop_studio` 内的子面板；`sell_panel` 是否明确是小镇商店；经营链路是否清晰。
4. 最后看 V2-C：深渊房间、小镇信息板、魔偶长期房间和剧情事件是否需要风格或结构微调。

## 评审口径

只评审：

- 视觉中心是否正确。
- 主行动是否清楚。
- 信息密度是否舒服。
- 场景隐喻是否成立。
- 风格是否足够温暖、奇幻、生活化。

暂不评审：

- AI 图里的伪文字和乱码。
- 单个按钮、文本、图标的最终样式。
- Unity Prefab 层级和运行时布局细节。
- Approved 运行时素材质量。
