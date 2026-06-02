---
id: art_ui_formal_v2_rumor_board
title: Rumor Board Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/rumor_board_v1.md
last_verified: 2026-06-02
update_rule: 编写或确认 rumor_board Formal V2 详细方案时同步本文件。
---

# Rumor Board Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于确认 `rumor_board` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`rumor_board` Formal V1 已覆盖今日传闻、价格波动、详情和推荐行动，但仍像行情表：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 情报感不足 | 传闻和价格波动像列表字段 | 改为市场情报桌：纸条、行情牌、地图针和价格标签。 |
| 推荐行动不够直观 | Plan Expedition 像普通按钮 | 推荐下潜目标做成地图小卡，主行动只有一个。 |
| 涨跌信息像表格 | 价格倍率读起来费力 | 用上升 / 下降标签、颜色和物品图标表达。 |
| 可信度弱 | 传闻可信度容易埋在文本中 | 用蜡封、破损纸条、问号印章表现可信度。 |

---

## 2. 玩家目标

玩家进入传闻板时，目标是：

1. 看今天最值得关注的市场或深渊情报。
2. 判断哪些物品适合卖、哪些目标适合下潜。
3. 看懂传闻可信度和剩余时间。
4. 选择是否根据传闻规划行动。

---

## 3. Formal V2 体验定位

`rumor_board` 是“市场情报桌”，不是价格表。

```text
情报桌
  -> 今日传闻纸条
  -> 价格涨跌牌
  -> 选中传闻详情
  -> 推荐行动
```

视觉目标：

* 传闻像散落在桌上的纸条和剪报。
* 价格波动像挂着小标签的物品卡。
* 玩家一次只读一条重点传闻。
* 推荐行动是“根据这条情报做什么”。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Header: date / market mood / close                         │
│                                                            │
│ Rumor Notes             Price Wave Tags     Detail Card     │
│ pinned notes            item up/down        selected rumor  │
│                                                            │
│ Recommendation Map Card                  Primary Action    │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `rumor_board_background` | `0,0 1920x1080` | 工坊 / 市场情报角背景。 |
| `rumor_notes_area` | `170,160 580x650` | 今日传闻纸条列表。 |
| `price_wave_tags` | `790,180 420x610` | 涨价 / 跌价物品标签。 |
| `rumor_detail_card` | `1260,180 430x400` | 选中传闻影响对象、可信度、有效期。 |
| `recommendation_map_card` | `1260,620 430x220` | 推荐下潜目标或出售建议。 |
| `rumor_action_panel` | `1260,870 430x100` | Plan Expedition / Close。 |

---

## 5. 信息层级

默认阅读顺序：

1. 今日关键传闻。
2. 明显价格涨跌。
3. 选中传闻影响对象和有效期。
4. 推荐行动。
5. 返回或规划下潜。

不默认展开：

* 全部价格公式。
* 传闻来源长文本。
* 全部影响链条。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `PlanExpeditionFromRumor` 或 `MarkTradeTarget` | 根据选中传闻给一个主行动。 |
| Secondary | 选择传闻、选择价格标签、返回 | 列表 / 小按钮。 |
| Tertiary | 查看可信度说明、查看过期规则 | 折叠详情。 |
| Danger | 低可信传闻强规划 | 如存在，警示但不阻断。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| 传闻列表行按钮 | 移除，列表只负责选择。 |
| Plan Expedition | 作为详情卡下方唯一主行动。 |
| 价格详情 | 点击价格标签展开，不常驻长列表。 |
| Close | 次行动。 |

---

## 8. 场景隐喻

传闻板像工坊角落的情报桌：

* 桌上有剪报、手写纸条、市场小票和地图。
* 涨价 / 跌价是贴在物品图标旁的小标签。
* 可信度低的传闻纸张破损、墨迹模糊或带问号印章。
* 推荐行动像被圈出的地图点或待售物品。

---

## 9. 程序迁移影响

确认后建议层级：

```text
RumorBoardPanel
  RumorBoardBackground
  RumorNotesArea
  PriceWaveTags
  RumorDetailCard
  RecommendationMapCard
  RumorActionPanel
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| 今日传闻 | RumorService。 |
| 价格波动 | Economy / PriceWave state。 |
| 推荐行动 | Planning / Dungeon / Trade preview。 |
| 有效期和可信度 | Rumor config / runtime state。 |

---

## 10. 素材需求变化

第一版复用：

| VisualID | 用法 |
|---|---|
| `bg_workshop_day` | 情报桌背景临时复用。 |
| `ui_panel_main` / `ui_panel_info` | 详情卡和推荐卡。 |
| `ui_list_row_normal` / `ui_list_row_selected` | 传闻纸条临时底板。 |
| `ui_icon_rumor` | 传闻。 |
| `ui_icon_price_up` / `ui_icon_price_down` | 涨跌标签。 |
| `ui_icon_money` / `ui_icon_warning` | 价格和风险。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `ui_rumor_note_paper` | 传闻纸条。 |
| `ui_price_wave_tag` | 价格涨跌标签。 |
| `ui_rumor_credibility_stamp` | 可信度印章。 |

---

## 11. UX 验收标准

1. 3 秒内能看出这是传闻 / 行情界面。
2. 价格涨跌方向不用读长文本也能判断。
3. 当前选中传闻和推荐行动清楚。
4. 低可信或即将过期传闻有视觉提示。
5. 不形成价格表或情报字段墙。

---

## 12. 概念图提示词

```text
Use case: ui-mockup
Asset type: Formal V2 game UI concept art for rumor_board
Primary request: A cozy fantasy and light steampunk market rumor board interface.
Scene/backdrop: warm workshop intelligence desk with parchment notes, market receipts, small map, brass lamp, pinned strings.
Subject: left area with pinned rumor notes, center price up and price down item tags, right selected rumor detail card, small recommendation map card, one clear planning button.
Style: warm hand-painted fantasy UI, parchment and wood, soft brass details, low information density, no real readable text.
Composition: 16:9 landscape, rumor notes and price tags as visual center.
Avoid: stock market terminal, spreadsheet, modern analytics dashboard, dense text, real readable words, logos, watermark.
```
