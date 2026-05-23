---
id: art_ui_formal_v1_structure_review
title: 正式版 UI 结构 V1 总览
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: true
related:
  - 美术文档/ui_design/README.md
  - agent_status/art.md
  - 知识库/views/art.md
  - 美术文档/ui_design/ui_iteration_process.md
  - 美术文档/ui_design/formal_v1/combat_hud_v1.md
  - 美术文档/ui_design/formal_v1/dungeon_map_v1.md
  - 美术文档/ui_design/formal_v1/inventory_loot_v1.md
  - 美术文档/ui_design/formal_v1/settlement_v1.md
  - 美术文档/ui_design/formal_v1/workshop_main_v1.md
  - 美术文档/ui_design/versions/README.md
  - 美术文档/ui_design/versions/formal_v1_candidate/README.md
  - 美术文档/09_运行时美术验收记录.md
  - 美术文档/10_正式版核心纵切美术路线.md
last_verified: 2026-05-24
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
  -> 素材品质 / 动效 / VFX 逐步迭代
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
7. **逐界面迁移。** 不一次性推翻 8 个界面。每个界面单独设计、接入、截图验收、推进状态。

---

## 4. 界面优先级

| 优先级 | ScreenID | Formal V1 重点 |
|---|---|---|
| P0-A | `combat_hud` | 已写入 active Formal V1：左玩家、右敌方实体、底部居中背包、敌人脚下血条。 |
| P0-B | `workshop_main` | 从功能按钮堆叠改为正式工坊工作台、魔偶维护和出发整备结构。 |
| P0-C | `inventory_loot` | 保留左右取舍，但强化背包/战利品/详情/确认的正式结算结构。 |
| P1 | `dungeon_map` | 从节点按钮容器改为深渊路线地图、层级推进和背包整理入口。 |
| P1 | `settlement` | 从结果弹窗改为胜利/战败结算仪式界面。 |

出售、义体制造、层选择仍属核心流程，但更接近功能弹窗。它们可以在上述 5 个主界面结构稳定后再进入 Formal V1 细化。

---

## 5. 迁移状态建议

现有 `screen_layouts.json` 的状态仍保留 MVP Baseline 验收结果。Formal V1 在确认前先写在本目录中。待某个界面 Formal V1 方案确认后，再修改当前 active `screen_layouts.json`。

`versions/formal_v1_candidate/` 只作为复杂界面的可选暂存区。`combat_hud` 本轮已在用户确认关键结构后直接写入 active `screen_layouts.json`；后续更复杂界面仍可先在 candidate 中试写结构化 JSON，再合并到 active。

建议新增或使用以下字段：

```json
{
  "StructureVersion": "FormalV1",
  "PreviousValidatedVersion": "MVPBaseline",
  "LayoutStatus": "draft"
}
```

状态解释：

| 状态 | 含义 |
|---|---|
| `draft` | Formal V1 结构已写入规格，等待审查或资源检查。 |
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

1. `combat_hud` 已进入 active Formal V1 规格，第一批 `monster_*_combat` 战斗实体和脚底阴影/目标光环已入库，下一步交给程序接入并截图验收。
2. `workshop_main` 和 `inventory_loot` 可以复用较多现有 UI，但主区域关系需要从功能堆叠转为正式工作流。
3. `dungeon_map` 和 `settlement` 的 P1 设计不应急着 handoff，应先按 Formal V1 审查后再交付。
4. Formal V1 第一轮不要求重画全部素材，但要求程序接入后的截图从结构上看像正式游戏界面。
