---
id: art_ui_formal_v2_ux_ui_overview
title: Formal V2 UX/UI 重构总方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: true
related:
  - 美术文档/README.md
  - 美术文档/ui_design/README.md
  - 美术文档/ui_design/ui_iteration_process.md
  - 美术文档/ui_design/formal_v2/README.md
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
  - 美术文档/ui_design/formal_v2/design_boards/README.md
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/09_运行时美术验收记录.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
  - agent_status/art.md
  - 知识库/views/art.md
last_verified: 2026-06-08
update_rule: 修改 Formal V2 总目标、批次、验收门槛或 active 迁移规则时同步本文件。
---

# Formal V2 UX/UI 重构总方案

> **定位：** Formal V2 针对当前 Formal V1 的核心 UX 问题：界面像功能按钮集合，而不是正式游戏界面。本文只定义设计方向和迁移门槛，不直接作为程序接入口。

---

## 1. 现状判断

Formal V1 已解决以下问题：

* 21 个界面有 active `screen_layouts.json` 规格。
* VisualID、Manifest、Approved、Registry 和 ArtAcceptance 链路已经跑通。
* 程序可以按 active 规格做截图覆盖和资源登记。

但 Formal V1 仍没有解决正式 UX/UI 的核心问题：

| 问题 | 具体表现 | 后果 |
|---|---|---|
| 功能入口平铺 | 主界面和子面板把出售、维护、义体、订单、账单等功能做成同等级按钮 | 玩家无法判断当前最重要行动，界面像调试菜单。 |
| 场景隐喻不足 | 工坊、账本、工作台、出发门、订单板等空间关系没有形成 | UI 缺少游戏世界感，正式感弱。 |
| 信息层级程序化 | 文本、数值、按钮、列表直接堆在面板中 | 玩家只能读字段，不能快速理解决策。 |
| 操作层级混乱 | 主行动、次行动、危险行动、关闭 / 返回动作视觉权重接近 | 容易误操作，操作目标不清晰。 |
| 验收标准偏技术 | 当前 ArtAcceptance 主要证明资源加载、截图覆盖和 missing sprite | 不能判断界面是否像正式游戏。 |

因此 Formal V2 不以“继续补素材”为主线，而以“重新建立玩家流程、信息架构和界面正式感”为主线。

---

## 2. Formal V2 目标

Formal V2 的目标：

```text
Formal V1 功能区域规格
  -> Formal V2 玩家流程与正式界面结构
  -> 用户逐界面确认
  -> active screen_layouts.json 逐界面迁移
  -> 程序接入
  -> ArtAcceptance + UX 验收
  -> 再进入素材 / 动效 / VFX 精修
```

Formal V2 不承诺一次完成所有最终美术品质，但必须让进入迁移的界面具备正式 UI 骨架：

1. 玩家能一眼看出当前界面的主要任务。
2. 主行动只有一个，次行动被明确降级。
3. 功能入口不再平铺成按钮墙。
4. 信息按“玩家决策”组织，而不是按“程序字段”堆叠。
5. 每个界面有明确的场景隐喻或功能容器。
6. 运行时截图可以判断结构、主次行动、空间关系和文本密度。

2026-06-08，用户已评审并认可 `concepts/review_index.md` 当前 Formal V2 概念方向；本方案进入 V2-A active 迁移阶段。迁移范围先限定在 `workshop_main` / `workshop_studio`、`combat_hud`、`inventory_loot`、`dungeon_map` 和 `settlement`，其余 V2-B / V2-C 界面仍保持 design draft，不作为本批次程序接入口。

---

## 3. 设计原则

### 3.0 视觉风格基调

Formal V2 的默认风格从“硬核工业 / 暗色系统面板”调整为“日系二次元地底奇幻 + 轻蒸汽工艺”：

| 方向 | 要求 |
|---|---|
| 核心气质 | 更接近日系二次元游戏视觉、温暖手绘、童话式地底奇幻和生活感；工坊应先让玩家感到安心。 |
| 内部参考 | `来自深渊` 只作为内部审美参照：巨大垂直地底、童话好奇、未知危险、可爱与残酷并存；AI prompt 不直接写作品名。 |
| 项目参考 | `美术文档/美术风格参考/火山的女儿主界面.png`、`兰斯地图.png`。 |
| 蒸汽朋克比例 | 作为轻工艺细节存在：器械、义体、黄铜、玻璃、齿轮和暖灯；不成为冰冷硬核科幻或欧美暗黑工业。 |
| 画面质感 | 干净线条、柔和赛璐珞阴影、明亮但不轻飘的色彩、低写实厚涂比例。 |
| 信息密度 | 默认减少面板和按钮数量，突出一个主视觉对象和一个主行动。 |
| UI 语言 | 大图形、清晰热点、少量状态图标；避免密集文本墙和调试式横排字段。 |

默认负面方向：

```text
欧美暗黑奇幻、厚重写实油画、硬核工业控制台、冷硬科幻 UI、密集按钮墙、伪文字堆叠。
```

`dungeon_map` 还必须避免海面 / 水域底图和悬浮孤立节点。地图底图应像深渊剖面、地层羊皮纸、洞窟路线板或某一层的生态地貌，节点要嵌在岩台、地层、道路、草甸、树根、遗迹平台、铜质地图牌或路线结构中。

层地图按“可推进的大地图”理解，不按单屏静态节点板理解：一张地图可以很大，玩家沿路线不断前进，镜头随当前节点向前推移。美术上要留出前景、中景、远景和路线延展感，让玩家相信当前屏幕只是大地图的一段。

每一层可以有独立生态主题，不必都画成深渊洞窟。可以是地底草原、地下森林、晶洞、遗迹、雾谷、矿坑或水边湿地；关键是仍符合“巨大未知空间、童话好奇、危险潜伏、节点与地貌融合”的方向。

### 3.0.1 Formal V2 美术系统标准

Formal V2 的美术系统不只是一批新概念图，而是一套贯穿 UI 结构、运行时素材、AI 出图和验收的共同标准。后续所有 Formal V2 设计、素材清单和跑图计划都按本节判断是否同属一套视觉系统。

| 维度 | 标准 |
|---|---|
| 视觉母题 | 日系二次元地底奇幻冒险 + 轻蒸汽工艺；画面有童话好奇、生活温度和未知危险。 |
| UI 气质 | 少按钮、强主视觉、清晰主行动；优先像游戏场景中的可交互物，而不是调试面板或网页后台。 |
| 组件语言 | 面板、按钮、列表、节点、背包格和状态条都应带轻工艺边框、柔和阴影和可读轮廓；避免硬核工业控制台。 |
| 信息密度 | 默认低密度；每屏保留一个视觉中心、一个主行动和少量关键状态。详情通过二级展开、侧栏或弹窗承接。 |
| 场景融合 | 背景、角色、地图、面板和图标需要共享材质线索，例如黄铜、旧木、布料、暖灯、生物荧光和手绘地貌。 |
| 可迭代性 | 结构和素材质量分开验收；local_v0 可用于结构验证，但不能被称为最终美术。 |

### 3.0.2 质量层级

Formal V2 使用以下质量层级管理素材，不再用“有图 / 没图”粗略判断美术完成度：

| QualityTier | 含义 | 可用于 | 不可用于 |
|---|---|---|---|
| `local_v0` | 本地生成或临时占位的可运行素材，命名、尺寸和 `.meta` 可接入，但视觉质量不是正式版。 | 程序结构接入、截图链路验证、布局和可读性初验。 | 最终美术验收、风格统一性结论、宣传或正式截图。 |
| `formal_ai_v2` | 按 Formal V2 风格、Prompt、Spec 和筛选流程生成的正式 AI 图，并通过预处理 / 同步 / 基础技术检查。 | 正式纵切截图、运行时美术验收、后续动效和 VFX 叠加。 | 仍不能替代最终人工 polish；若构图或语义不达标，需要继续替换。 |
| `final_polish` | 在 `formal_ai_v2` 基础上完成必要人工修正、统一线条 / 色彩 / 文字空区 / 边缘噪声后的最终候选。 | 候选版本、对外展示、长期保留资产。 | 未经过 Manifest、Approved、`.meta` 和验收记录的素材不能直接标为该层级。 |

同名质量替换必须保持 `VisualID`、Approved 路径、DisplaySpec、Unity `.meta` / GUID 和程序绑定不变。新增缺图生成和已接入素材质量替换必须分成不同批次，不能混在同一次同步里。

### 3.0.3 概念图、结构图和运行时素材边界

Formal V2 目前同时使用三类图，它们的用途不能混淆：

| 类型 | 目录 | 用途 | 是否进入 Manifest / Approved |
|---|---|---|---|
| AI 概念图 | `美术文档/ui_design/formal_v2/concepts/` | 评审氛围、风格、视觉中心和信息密度。 | 否。不能作为运行时素材，也不能给程序登记。 |
| 结构设计图 | `美术文档/ui_design/formal_v2/design_boards/` | 评审 16:9 布局、区域关系、主行动和信息层级。 | 否。不能替代 active `screen_layouts.json`。 |
| 运行时素材 | `UnityClient/Assets/Art/Approved/` | Unity 实际读取的图标、背景、面板、角色和 UI skin。 | 是。必须由 Manifest、Prompt、Spec、预处理和同步流程管理。 |

设计图 / 概念图默认使用 Codex 内置 `image_gen`。如果当前工具环境没有暴露 `image_gen`，必须先提醒用户并等待确认；不能自动切到 NovelAI、AI 图片网关、mock 或本地脚本。运行时素材生成才使用 NovelAI / AI 图片网关，并且必须遵守串行生成：每次只请求一张图，图间隔 1 秒。

### 3.1 先玩家目标，后功能按钮

每个界面先回答：

```text
玩家打开这个界面，是为了做什么决定？
```

按钮只能服务这个决定。不能因为程序已有功能，就把所有入口直接摆出来。

### 3.2 主行动唯一

每个界面在默认状态下只允许一个最高权重主行动。例如：

| 界面 | 主行动 |
|---|---|
| `workshop_main` | 出发 / 进入整备流程 |
| `combat_hud` | 执行当前回合行动 / 结束回合 |
| `inventory_loot` | 确认带出 |
| `dungeon_map` | 进入选中节点 |
| `settlement` | 返回工坊 / 继续结算后流程 |

出售、维护、查看详情、切换标签、关闭等动作必须视觉降级。

### 3.3 场景入口替代按钮菜单

正式 UI 应把高频功能映射到稳定空间：

| 功能 | Formal V2 表达 |
|---|---|
| 下潜 | 工坊出发门 / 升降机 / 深渊入口。 |
| 背包整理 | 工作台 / 底盘装配台。 |
| 维护 | 魔偶维护舱。 |
| 出货分配 | 工坊陈列台 / 出货台 / 账本终端。 |
| 小镇商店 / 市场 | 店铺柜台 / 商店货架 / 买卖账本。 |
| 订单 | 公告板 / 委托板。 |
| 传闻 | 市场行情板 / 情报纸条。 |
| 账单 | 账本 / 月租压力栏。 |

这些入口可以仍由 UGUI 按钮实现，但视觉和布局上不能像普通按钮列表。

### 3.4 信息按决策分组

信息分组优先级：

1. 当前状态：我现在有什么问题。
2. 可行动作：我能做什么。
3. 后果预览：做了会怎样。
4. 细节展开：需要时再看。

不要把所有字段默认展开。

### 3.4.1 低密度优先

Formal V2 的第一轮概念图和 active 迁移都按低信息密度设计：

1. 每屏优先只有一个视觉中心。
2. 默认只显示当前决策必要信息。
3. 详情、列表、数值拆到子界面或二级展开。
4. 大面积背景、角色、地图或背包本体允许占屏，UI 不需要填满画面。

### 3.5 列表必须有详情区和操作区

凡是列表界面，统一采用：

```text
列表区 -> 详情区 -> 操作区
```

列表行只显示用于选择的摘要；详情区解释后果；操作区放确认按钮。列表行本身不堆所有按钮。

### 3.6 背包仍是玩法对象

背包格继续以 `100x100` 为标准。Formal V2 可以调整背包容器、位置、装饰和状态反馈，但不改变玩法格尺寸。

### 3.7 视觉资源可迭代，UX 结构先稳定

Formal V2 可以继续复用 local_v0 或现有背景做结构验证。高质量图、动画、VFX 后续同名替换，不阻塞 UX 结构确认。

---

## 4. 全局 UI 模型

### 4.1 主流程

Formal V2 的主流程组织为：

```text
工坊 Hub
  -> 出发整备
  -> 深渊地图
  -> 战斗 / 房间节点
  -> 战利品清点
  -> 结算
  -> 工坊账本 / 维护 / 出售
```

工坊 Hub 是局外主入口，不再承担所有功能细节。细节功能应进入对应子界面。

### 4.2 工坊 Hub 分区

工坊主界面拆成两个层级：

| 层级 | 作用 |
|---|---|
| `workshop_main` | 以魔偶为中心的家 / 房间，表达安全感、日常状态和少量主入口。 |
| `workshop_studio` | 专门的改造工作室，承接背包、底盘、义体、改造椅和机械操作。 |

`workshop_main` 不再承担完整工作台和账本信息，只保留少量热点：

| 区域 | 作用 |
|---|---|
| 深渊入口 | 主行动，进入出发 / 下潜流程。 |
| 魔偶 / 房间中心 | 展示人偶状态、情绪和互动入口。 |
| 工作室入口 | 进入 `workshop_studio`，再处理背包、底盘、义体和改造。 |
| 市场 / 账本入口 | 作为低权重角落入口，不默认展开订单和账单。 |
| 轻状态条 | 金币、日期、月租、风险摘要，以图标化方式保留。 |

这些区域是主界面构图的骨架，而不是按钮分组标题。

### 4.3 子界面模型

子界面分为四类：

| 类型 | 代表界面 | 结构 |
|---|---|---|
| 舞台型 | `combat_hud`、`doll_room`、`safe_room`、`stairs_room` | 世界 / 角色 / 房间为主，UI 控件服务当前情境。 |
| 决策型 | `dungeon_map`、`layer_select`、`settlement`、`inventory_loot` | 中央决策对象 + 侧边详情 + 明确主行动。 |
| 工作室子面板型 | `maintenance_panel`、`prosthetic_panel`、`chassis_upgrade_panel` | 共用 `workshop_studio` 场景底图，在工作室内切换护理、义体和底盘面板。 |
| 商店交易型 | `sell_panel`、`faction_shop` | 商店货架 / 柜台 + 交易详情 + 买入 / 卖出操作。 |
| 信息板型 | `order_board`、`rumor_board`、`daily_bill_report`、`business_settlement` | 摘要列表 + 详情 + 风险 / 奖励 / 行动。 |

### 4.4 Formal V2 UI Skin 资产清单

Formal V2 UI Skin 先按“可复用组件”管理，不按单屏零散出图。下表是后续 Manifest / Prompt / 质量替换计划的美术侧基准；已有 VisualID 可以复用，缺失项再进入 `art_requirements_seed.json` 或后续 active 规格。

| 资产组 | 代表 VisualID / 组件 | Domain | Type | 尺寸策略 | Alpha | 优先级 | 屏幕暴露 |
|---|---|---|---|---|---|---|---|
| 主面板皮肤 | `ui_panel_main`、`ui_panel_info`、`ui_settlement_victory_panel`、`ui_settlement_defeat_panel` | ui | panel | 1024x768 或 nine-slice 源图，运行时 sliced | true | P0 | 全局、结算、信息板、工作室子面板 |
| 主 / 次 / 危险按钮 | `ui_button_primary`、`ui_button_secondary`、`ui_button_danger` | ui | button | 512x128 或 nine-slice 源图，文字运行时叠加 | true | P0 | 全局主行动、返回、撤离、出售、风险确认 |
| 列表行和选中态 | `ui_list_row_normal`、`ui_list_row_selected` | ui | list_row | 1024x128 或 nine-slice 源图 | true | P0 | 订单、传闻、商店、账本、结算、义体列表 |
| 标题与分割装饰 | `ui_title_divider` | ui | divider | 512x128，可横向拉伸 | true | P1 | 结算、信息板、商店、工作室子面板 |
| 地图节点皮肤 | `ui_dungeon_node_plate`、`ui_dungeon_route_line` | ui | map_frame | 节点 512x512；路线 512x128，可旋转/拉伸 | true | P0 | `dungeon_map`、后续 `layer_select` |
| 背包格与掉落区 | `ui_inventory_cell`、`ui_inventory_cell_selected`、`ui_loot_drop_zone`、`ui_loot_pickup_panel` | ui | inventory_skin | 格子围绕 100x100 玩法格；面板 nine-slice | true | P0 | `combat_hud`、`inventory_loot`、房间整理 |
| 战斗状态条 | `ui_combat_status_bar_hp`、`ui_combat_status_bar_shield` | ui | status_bar | 512x64 或 nine-slice 源图，填充值运行时控制 | true | P0 | `combat_hud` |
| 战斗反馈与标记 | `ui_combat_feedback_hit`、`ui_combat_feedback_shield_break`、`ui_combat_grid_lock_marker`、`ui_combat_junk_preview_marker` | ui | combat_feedback | 256x256 或 512x512，根据覆盖范围 contain | true | P0 | `combat_hud` |
| 结果徽记 | `ui_settlement_outcome_*` | ui | emblem | 512x512，主体居中，文字运行时叠加 | true | P0 | `settlement` |
| 弹窗壳和确认框 | 复用 `ui_panel_main` + `ui_button_*`，必要时新增 `ui_modal_frame` | ui | modal | 1024x768 或 nine-slice 源图 | true | P1 | 风险确认、剧情事件、商店交易确认 |
| 空状态 / 锁定 / 装备态 | `ui_icon_locked`、`ui_icon_equipped`、后续空状态插图 | ui | state_icon | 图标 512x512；空状态可 1024x768 | true | P1 | 商店、订单、义体、底盘、层选择 |
| 共享系统图标 | `ui_icon_money`、`ui_icon_warning`、`ui_icon_order`、`ui_icon_rumor`、`ui_icon_faction` 等 | ui | icon | 512x512，contain，文字/数字运行时叠加 | true | P1-P2 | 全局状态、经济、小镇、叙事 |

处理顺序：先做 P0 的主面板、按钮、列表行、地图节点、背包格、战斗状态条和结果徽记；再做 P1 的标题装饰、弹窗壳、锁定/装备态和高频共享图标；P2 只处理低频或仍处 draft 的小图标。UI Skin 图不烘焙可读文字、数字和按钮文案。

### 4.5 Formal V2 场景 / 背景资产清单

场景和背景属于配置表难以直接扫出的 preset 需求。Formal V2 先按场景功能定义，再决定是否复用现有 `bg_*`、新增 preset，或只保留概念图。未进入 active 的界面只记录规划，不进入 Manifest 和跑图队列。

| 场景组 | 代表 ScreenID | Runtime VisualID 建议 | 尺寸 / 适配 | Alpha | 状态 | 美术要求 |
|---|---|---|---|---|---|---|
| 工坊主界面房间 | `workshop_main` | 复用 `bg_workshop_day`，后续可升级为 FormalV2 同名替换 | 1920x1080，cover，UI 安全区不烘焙文字 | false | V2-A active | 魔偶中心安心房间，暖灯、旧木、布料、轻蒸汽工艺；减少控制台感。 |
| 工坊改造室 | `workshop_studio`、`maintenance_panel`、`prosthetic_panel`、`chassis_upgrade_panel` | 待定 `bg_workshop_studio`，未 active 前仅规划 | 1920x1080，cover，右侧改造椅和左侧工具/背包区需留空间 | false | draft planning | 机械感更强但仍温暖；魔偶坐在改造椅上，维护 / 义体 / 底盘作为同场景子面板。 |
| 通用战斗舞台 | `combat_hud` | 复用 `bg_combat_abyss`，后续按层主题同名替换或扩展 | 1920x1080，cover，地面需支持左右实体落点 | false | V2-A active | 横版舞台，左人偶右敌方，前景地面清晰，背景低噪声。 |
| 可推进层地图 | `dungeon_map` | 复用 `bg_dungeon_map`，后续可按层新增背景组 | 1920x1080 或更大滚动图，支持前中远景和镜头前移 | false | V2-A active | 大地图纵深、路线延展、节点嵌入地貌；每层可有草原、森林、晶洞、遗迹等独立生态。 |
| 层选择剖面 | `layer_select` | `bg_layer_select` | 1920x1080，cover | false | FormalV1 active + V2 draft | 深渊剖面、层级锁定和整备检查；不做密集菜单墙。 |
| 安全屋 | `safe_room` | `bg_safe_room` | 1920x1080，cover | false | FormalV1 active + V2 draft | 完全不透明 PNG；营地、暖灯、可休整空间，避免透明黑洞。 |
| 阶梯房间 | `stairs_room` | `bg_stairs_room` | 1920x1080，cover | false | FormalV1 active + V2 draft | 完全不透明 PNG；下降口、下一层风险和撤离/深入决策空间。 |
| 战利品清点叠层 | `inventory_loot` | 不单独新增背景，复用战斗场景 + 半透明 UI 层 | UI 层按背包和散落奖励布局；背景由战斗截图或战斗底图承接 | mixed | V2-A active | 背后仍能读出战斗场景，但 UI 遮罩不能影响背包格和奖励可读性。 |
| 结算报告背景 | `settlement` | `bg_settlement_victory`、`bg_settlement_defeat` | 1920x1080，cover | false | V2-A active | 撤离成功 / 战败损伤的报告氛围，背景低噪声，报告面板是视觉中心。 |
| 小镇商店 / 市场 | `sell_panel`、`faction_shop` | 待定 `bg_town_shop` / `bg_faction_shop`，未 active 前仅规划 | 1920x1080，cover，柜台与货架分区 | false | FormalV1 active + V2 draft | 温暖小镇店铺、柜台、货架、账本；`sell_panel` 不再是工坊估价柜台。 |
| 经营链路 | `shop_staging`、`business_settlement`、`daily_bill_report` | 可复用商店 / 账本背景，必要时新增 `bg_shop_staging`、`bg_daily_bill` | 1920x1080 或面板式背景 | false | FormalV1 active + V2 draft | 出货陈列台、营业演出、每日账本分开表达；金币增长和月租压力不能只靠文字。 |
| 人偶房间 | `doll_room`、`doll_interaction` | `bg_doll_room_attic`，后续可同名 FormalV2 替换 | 1920x1080，cover | false | FormalV1 active + V2 draft | 待机魔偶、纪念物、日记和生活痕迹；房间应像家，不像功能面板。 |
| 剧情事件表面 | `scenario_event` | 不急于新增背景；默认叠加在当前场景或使用轻量故事卡背景 | 1024x768 卡面或全屏半透明叠层 | mixed | FormalV1 active + V2 draft | 故事卡、选项和结果保持低密度；不烘焙可读文字。 |

背景默认 `Alpha=false`，除非明确是 UI 叠层、故事卡或半透明遮罩。环境背景不允许出现大面积透明 / 半透明导致黑底穿透。概念图只用于评审氛围；进入运行时前必须拆成 Manifest 管理的 `bg_*` 或 UI skin 资产。

---

## 5. 组件层级

### 5.1 按钮层级

| 层级 | 数量限制 | 用法 |
|---|---:|---|
| Primary Action | 每屏 1 个 | 推进流程，例如出发、进入节点、确认带出、返回工坊。 |
| Secondary Action | 每个区域 1-3 个 | 查看、切换、整理、打开子面板。 |
| Tertiary / Icon Action | 可多个 | 小工具、筛选、排序、展开、收起。 |
| Danger Action | 默认隐藏或二次确认 | 丢弃、放弃、撤离失败相关、黑市风险动作。 |

主行动不能和普通入口排成同一列。

### 5.2 面板层级

| 面板 | 用法 |
|---|---|
| Stage / Scene | 背景和角色 / 房间 / 战斗对象。 |
| Main Panel | 当前界面的主要决策对象。 |
| Detail Panel | 选中对象详情和后果预览。 |
| Utility Panel | 筛选、列表、历史记录、辅助入口。 |
| Modal | 高风险确认或临时流程，不承载主界面长期信息。 |

### 5.3 文本密度规则

1. 标题只说明当前任务，不写长说明。
2. 列表行最多两行文字。
3. 详情区长文本必须分段，不能压到按钮和边框。
4. 关键数值优先图标 + 数字，不整句描述。
5. 长解释默认放详情区或 hover / 展开区。

---

## 6. 设计批次

### V2-A：核心主流程

| ScreenID | Formal V2 目标 | 确认顺序 |
|---|---|---:|
| `workshop_main` | 从按钮菜单改为正式工坊 Hub，建立空间入口和主行动。 | 1 |
| `combat_hud` | 从状态 / 按钮堆叠改为战斗舞台、敌我站位和背包指令区。 | 2 |
| `inventory_loot` | 从拾取弹窗改为撤离清点和容量压力决策。 | 3 |
| `dungeon_map` | 从节点按钮图改为路线规划和风险决策界面。 | 4 |
| `settlement` | 从结果面板改为战斗报告 / 撤离报告。 | 5 |

V2-A 通过后，才能批量迁移局外功能面板。

### V2-B：局外功能面板

| ScreenID | Formal V2 目标 |
|---|---|
| `maintenance_panel` | `workshop_studio` 的维护子面板：状态诊断 -> 方案选择 -> 费用 / 材料 -> 执行。 |
| `prosthetic_panel` | `workshop_studio` 的义体子面板：配方列表 -> 义体预览 -> 材料缺口 -> 制造 / 装备。 |
| `chassis_upgrade_panel` | `workshop_studio` 的底盘子面板：当前 / 下一底盘对比 -> 容量变化 -> 蓝图 / 材料。 |
| `sell_panel` | 小镇商店 / 市场交易：商店库存、玩家货物、价格反馈、买入 / 卖出。 |
| `shop_staging` | 出货分配台：普通渠道、订单渠道、黑市渠道。 |
| `daily_bill_report` | 账本日结：收入、支出、月租压力和未售出风险。 |
| `business_settlement` | 营业演出：顾客流、成交反馈和进入账单。 |

截至 2026-06-01，V2-B 七个局外功能界面已补齐详细草案。根据后续语义修正，`maintenance_panel`、`prosthetic_panel` 和 `chassis_upgrade_panel` 应作为 `workshop_studio` 内可切换的不同面板或弹出窗口，而不是独立大场景；`sell_panel` 应改作小镇商店 / 市场交易界面，工坊卖出和出货分配由 `shop_staging` 承接。它们仍是 design draft，不修改 active `screen_layouts.json`，不触发素材生成，也不要求程序接入。

### V2-C：长期系统和叙事

| ScreenID | Formal V2 目标 |
|---|---|
| `order_board` | 委托板：势力、目标物、期限、奖励和接取 / 提交。 |
| `rumor_board` | 行情情报板：价格波、推荐出售和风险提示。 |
| `faction_shop` | 势力柜台：声望、信任、商品和黑市风险。 |
| `layer_select` | 下潜选择：层级、锁定条件、推荐整备和确认。 |
| `safe_room` | 安全区：休整、撤离、继续深入和背包整理。 |
| `stairs_room` | 阶梯房间：下一层风险、撤离 / 深入决策。 |
| `doll_interaction` | 人偶互动：关系反馈、交互次数、礼物 / 对话结果。 |
| `doll_room` | 人偶房间：待机、纪念物、日记和长期状态。 |
| `scenario_event` | 剧情事件：角色、对白、选项、系统结果和跳过摘要。 |

截至 2026-06-02，V2-C 九个长期系统和叙事界面已补齐详细草案：`13_layer_select_v2.md` 到 `21_doll_room_v2.md`。Formal V2 当前已覆盖 21 个 active ScreenID；`workshop_studio` 作为 `workshop_main` 的设计拆分存在，不是额外 active ScreenID。

---

## 7. 单界面交付格式

每个 Formal V2 界面文档必须包含以下章节：

```text
1. Formal V1 问题
2. 玩家目标
3. Formal V2 体验定位
4. 主结构草案
5. 信息层级
6. 行动层级
7. 按钮合并 / 降级 / 隐藏策略
8. 场景隐喻
9. 程序迁移影响
10. 素材需求变化
11. UX 验收标准
12. 用户确认问题
```

用户确认后，才允许：

1. 修改 `screen_layouts.json`。
2. 更新 `component_catalog.json` 或 `design_tokens.json`。
3. 更新 `art_requirements_seed.json`。
4. 刷新 Manifest、Prompt、可接入素材清单。
5. 要求程序接入。

---

## 8. UX 验收门槛

Formal V2 的运行时验收不只看技术项，还要新增 UX 检查：

| 类别 | 检查项 |
|---|---|
| 主目标 | 截图中 3 秒内能判断当前界面要玩家做什么。 |
| 主行动 | 最高权重按钮只有一个，位置和视觉权重明确。 |
| 按钮负载 | 默认可见普通文字按钮不超过 6 个；超过则必须分区、折叠或图标化。 |
| 空间隐喻 | 工坊 / 战斗 / 地图 / 账本 / 工作台等界面类型能通过布局和背景读出。 |
| 信息密度 | 列表行、详情区和按钮不重叠，中文长文本不压边。 |
| 列表结构 | 列表界面必须有选中态、详情区和操作区。 |
| 风险表达 | 危险行动、黑市风险、战败损失、月租压力必须视觉区分。 |
| 背包规则 | 100x100 格不被压缩；背包对象和物品层不因装饰变形。 |

### 8.1 静态美术验收门禁

Formal V2 在要求程序接入或运行 ArtAcceptance 前，先由美术侧完成静态验收。静态验收只能证明素材和设计已准备好进入接入 / 替换，不能证明 Unity 运行时已经正确显示。

| 门禁 | 输入 | 通过标准 | 输出 / 记录 | 不能证明 |
|---|---|---|---|---|
| 概念评审 | `concepts/`、`concepts/review_index.md` | 风格、视觉中心、低信息密度和场景气质符合 Formal V2；AI 伪文字不作为 UI 文字。 | 评审结论、归档旧图、保留候选概念图。 | 不证明运行时素材可用，不进入 Manifest。 |
| 结构评审 | `design_boards/`、单屏 `*_v2.md` | 16:9 布局有明确视觉中心、主行动、信息层级和按钮降级策略。 | 可迁移 / 需修改 / 暂缓的屏幕结论。 | 不替代 active `screen_layouts.json`。 |
| Active 合同 | `screen_layouts.json`、`component_catalog.json` | 用户确认后才写入 active；`Validate-UIDesign.ps1` 通过。 | UI handoff 刷新。 | 不证明 Unity prefab 已改。 |
| Manifest / Prompt / Spec | `art_manifest.json`、`AI绘图提示词清单.md` | 每个运行时 VisualID 有英文 Prompt、负面词、结构化 Spec、尺寸、Alpha 和输出路径。 | 缺图 / 质量替换队列刷新。 | 不证明图已经生成。 |
| PNG 技术检查 | `Approved` 或 `_IncomingAI/<VisualID>/processed` | 尺寸符合 SourceSpec；背景按要求不透明；图标有 alpha；主体不贴边；无需可读文字。 | 技术修复 / spec_review / 可筛选候选。 | 不证明风格最终达标。 |
| Contact sheet 筛选 | `_IncomingAI/<VisualID>/contact_sheet`、`selected/` | 每个 VisualID 明确选中图；未选中时不得自动把 mock 当正式图。 | `selected/` 或人工备注。 | 不证明已同步 Approved。 |
| Approved / QualityTier | `UnityClient/Assets/Art/Approved`、Manifest | 新缺图进入 Approved；同名替换保持 VisualID、路径、`.meta` / GUID；`QualityTier` 正确更新。 | 可接入素材清单、质量替换清单和快照。 | 不证明运行时绑定或截图通过。 |
| Runtime ArtAcceptance | Unity 截图、Registry、验收记录 | 资源加载、missing sprite、截图覆盖、布局和 UX 人工验收通过。 | `09_运行时美术验收记录.md` 和状态页。 | 只有到此层才可说运行时美术验收通过。 |

静态门禁的状态命名：`concept_ready` 表示概念可评审，`layout_ready` 表示结构可迁移，`prompt_ready` 表示可跑图，`approved_ready` 表示可交程序登记 / 替换，`runtime_validated` 只能由 Unity 验收证据赋值。`local_v0` 最高只能到结构验证和可接入初验，不能标为最终美术通过。
若技术验收通过但 UX 验收失败，只能标记为“资源 / 截图接入通过”，不能标记为 Formal V2 通过。

---

## 9. 程序交接原则

程序侧只在以下条件满足后接入 Formal V2：

1. 对应界面 Formal V2 文档完成。
2. 用户确认该界面方案。
3. active `screen_layouts.json` 已更新为 `StructureVersion=FormalV2`。
4. `Validate-UIDesign.ps1` 通过。
5. 程序交接清单明确列出需要接入的 VisualID、层级和截图点。

在确认前，程序侧继续以 Formal V1 active 规格为准，不按 `formal_v2/*.md` 修改 Unity。

---

## 10. 工具策略

Formal V2 的重点是先把玩家目标、信息层级和主行动设计清楚。工具可以提高评审和验收质量，但不应成为当前设计确认的前置阻塞。

| 工具 | 当前建议 | 适合解决的问题 | 不作为前置的原因 |
|---|---|---|---|
| Figma | V2-A 结构确认后再接入 | 线框图、组件库、交互标注、开发切图参考 | 现在核心问题是信息架构和流程，不是高保真稿不足。 |
| Unity MCP / Editor 自动化 | 程序侧稳定后评估 | 自动打开场景、运行截图、读取层级、做布局回归 | 当前已有 ArtAcceptance 工具，先不要并行两套验收入口。 |
| 截图标注工具 | 立即可用，轻量推进 | 在 ArtAcceptance 截图上标注按钮堆叠、文本压边、主行动不清 | 不要求额外工程接入，适合美术验收。 |
| PlayMode 布局扫描 | 跟随程序迭代 | 检查文字溢出、重叠、射线遮挡、格子尺寸 | 需要程序侧配合暴露快照字段。 |

分阶段策略：

1. **当前阶段：文档线框。** 先用 `formal_v2/*.md` 确认结构，不修改 active。
2. **结构确认前：结构设计图。** `design_boards/` 已生成 21 个 active 界面和 `workshop_studio` 的 16:9 layout board，用来评审视觉中心、主行动和信息层级。
3. **active 迁移时：Unity 验收。** 继续以 `Validate-UIDesign.ps1`、ArtAcceptance 和截图人工验收为准。
4. **Formal V2 第一批接入后：评估 Unity MCP。** 如果重复截图、层级检查、文本溢出检查耗时明显，再让程序接入 Unity MCP 或增强 Editor 自动化。

工具接入原则：

* Figma 稿只能作为设计参考，不能替代 active `screen_layouts.json`。
* Unity MCP / 自动化截图只能作为验收工具，不能替代美术人工判断。
* 任何工具输出都要回写到 `screen_layouts.json`、Manifest、验收记录或状态页，不能只留在外部工具里。

---

## 11. 当前下一步

1. 按 P3 mission `ART-V2-02` 迁移 V2-A active `screen_layouts.json`：`workshop_main` / `workshop_studio`、`combat_hud`、`inventory_loot`、`dungeon_map`、`settlement`。
2. 迁移后运行 `Validate-UIDesign.ps1`，确保 active 规格仍是程序可读合同。
3. active 通过后再刷新 Manifest、Prompt、UI handoff 和程序接入交接清单。
4. 根据新生成队列拆分可复用素材、新增素材、local_v0 质量替换项和程序接入项。
5. V2-B / V2-C 暂不进入本批次 active 迁移；后续按 `workshop_studio` 子面板、经营链路、小镇商店和长期 / 叙事界面分批确认。
