---
id: art_ui_formal_v1_sell_panel
title: 工坊出售界面 Formal V1
type: art
role: 美术
domain: ui_design
status: active
source_of_truth: true
related:
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/ui_design/formal_v2/09_sell_panel_v2.md
last_verified: 2026-05-24
update_rule: 修改出售界面正式结构或程序迁移要求时同步本文件。
---

# 工坊出售界面 Formal V1

> **目标：** 把出售界面从临时列表升级为正式工坊交易弹窗。仓库物品、单件出售、批量出售、估值和金币回流必须在同一主面板内清晰表达。

---

## 1. Formal V1 结构

```text
Workshop Sell Modal
  modal_backdrop
  sell_card
    sell_header
      title / summary / Sell All / Close
    sell_list
      row: item icon / name / source / value / Sell
```

| ZoneID | Rect | 目标 |
|---|---|---|
| `modal_backdrop` | `0,0 1920x1080` | 隔离工坊底层，形成弹窗焦点。 |
| `sell_card` | `380,150 1160x780` | 出售主容器。 |
| `sell_header` | `420,180 1080x140` | 标题、仓库摘要、批量出售和关闭按钮。 |
| `sell_list` | `430,330 1060x560` | 可出售物品列表。 |

当前已写入 active `screen_layouts.json`，状态为 `active_spec / FormalV1`。

---

## 2. 信息层级

| 层级 | 内容 |
|---|---|
| 最高 | 当前可卖物品列表和单行 Sell 按钮。 |
| 次高 | Sell All 危险操作和预计收益。 |
| 辅助 | 物品来源、数量、估值说明。 |

文字、价格、数量、物品名全部由 Unity Text 渲染，不烘焙进图片。

---

## 3. 可复用资源

| VisualID | 用途 |
|---|---|
| `ui_panel_main` | 出售主容器和列表底纹。 |
| `ui_list_row_normal` | 普通出售行。 |
| `ui_list_row_selected` | 选中或焦点出售行，后续可选。 |
| `ui_button_danger` | Sell All。 |
| `ui_button_secondary` | 单行 Sell 和 Close。 |
| `ui_icon_money` | 价值/金币提示。 |
| `ui_title_divider` | 标题分隔。 |

---

## 4. 程序迁移要求

关键路径：

| 路径 | VisualID / 组件 |
|---|---|
| `WorkshopSellPanel_Runtime/SellPanel_Card` | `Panel.Main` / `ui_panel_main` |
| `WorkshopSellPanel_Runtime/SellPanel_Card/TitleDivider_Image` | `Title.Divider` / `ui_title_divider` |
| `WorkshopSellPanel_Runtime/SellPanel_Card/SellAll_Button` | `Button.Danger` / `ui_button_danger` |
| `WorkshopSellPanel_Runtime/SellPanel_Card/Close_Button` | `Button.Secondary` / `ui_button_secondary` |
| `WorkshopSellPanel_Runtime/SellPanel_Card/SellList_Scroll` | `Panel.Main` / `ui_panel_main` |
| `SellRow_*` | `List.Row.Normal` / `ui_list_row_normal` |
| `SellRow_*/Sell_Button` | `Button.Secondary` / `ui_button_secondary` |

出售面板打开时义体面板应关闭。装饰面板不阻挡 ScrollRect、按钮和列表行射线。

---

## 5. 验收标准

1. 出售面板打开时至少展示 1 条可出售物品行。
2. 每行包含图标、名称、来源、估值和 Sell 按钮。
3. Sell All 和单行 Sell 可点击，点击后金币和列表刷新。
4. 列表可滚动，行皮肤不阻挡按钮或 ScrollRect。
5. 关闭按钮返回工坊主界面，义体面板保持关闭。
