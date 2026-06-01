---
id: art_ui_formal_v2_maintenance_panel
title: Maintenance Panel Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/maintenance_panel_v1.md
last_verified: 2026-06-01
update_rule: 编写或确认 maintenance_panel Formal V2 详细方案时同步本文件。
---

# Maintenance Panel Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于确认 `maintenance_panel` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`maintenance_panel` Formal V1 已经覆盖状态、磨损、侵蚀、费用和下潜许可，但整体仍像维护数据面板：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 维护行为缺少空间感 | 状态、费用、按钮并列在面板中 | 改成魔偶维护舱 / 护理工作台，先看魔偶状态，再选维护方案。 |
| 行动过多 | Full Repair、修复剂、暂缓、返回等权重接近 | 默认只突出一个“执行当前方案”，其他作为方案切换或危险折叠。 |
| 费用和材料像表格 | 玩家需要读列表才能判断能否维护 | 用少量材料 token、金币图标和缺口标记表达。 |
| 下潜许可和维护方案关系不直观 | 下潜许可单独成块 | 把下潜许可放到诊断结果中，直接指出阻塞项。 |

---

## 2. 玩家目标

玩家进入维护界面时，真实目标是：

1. 判断魔偶现在是否适合继续下潜。
2. 选择一个最合适的维护方案。
3. 看懂维护会消耗什么、恢复什么、解除什么风险。
4. 执行维护或明确承担暂缓风险。

---

## 3. Formal V2 体验定位

`maintenance_panel` 是 `workshop_studio` 内的“维护模式页”，不是独立大场景，也不是维修清单。

```text
workshop_studio
  -> 切换到维护模式
  -> 魔偶维护舱 / 改造椅状态
  -> 状态诊断
  -> 三个维护方案
  -> 费用 / 材料 token
  -> 执行当前方案
```

视觉目标：

* 复用 `workshop_studio` 的工作室底图、魔偶改造椅和机械臂。
* 魔偶或核心部件是中心对象。
* 维护方案像桌上的工具和药剂，而不是按钮列表。
* 玩家默认只需要比较当前选中的方案。
* 严重风险用温和但明确的警示表达，不把画面做成冷硬控制台。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Header: maintenance title / money / close                  │
│                                                            │
│ Care Bay                  Diagnosis Board                  │
│ doll, HP/SAN, wear        dive permit, risk, result        │
│                                                            │
│ Treatment Tray: repair / purify / permit check             │
│ Cost Tokens + Selected Plan Result        Primary Action   │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `studio_background` | `0,0 1920x1080` | 复用 `workshop_studio` 背景，维护模式只替换面板和局部器械。 |
| `care_bay` | `180,170 620x600` | 魔偶护理舱、状态条、磨损和侵蚀可视化。 |
| `diagnosis_board` | `850,170 520x360` | 当前风险、下潜许可、维护后预览。 |
| `treatment_tray` | `850,560 760x210` | 三个维护方案卡：磨损修复、侵蚀净化、下潜许可检查。 |
| `cost_token_row` | `850,800 420x120` | 当前方案费用、材料、缺口。 |
| `maintenance_action_panel` | `1300,790 310x140` | 执行当前方案、返回、暂缓。 |

---

## 5. 信息层级

默认阅读顺序：

1. 魔偶是否能继续下潜。
2. 当前最大风险是磨损、侵蚀、SAN 还是材料不足。
3. 当前选中维护方案会改善什么。
4. 费用和材料是否满足。
5. 执行维护或暂缓。

不默认展开：

* 所有状态字段。
* 长材料清单。
* 维护公式。
* 多个并列确认按钮。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `ConfirmSelectedMaintenance` | 执行当前维护方案，唯一最高权重按钮。 |
| Secondary | 切换维护方案、返回工坊 | 方案卡 / 小按钮。 |
| Tertiary | 查看材料来源、查看详细状态 | 折叠详情或小图标。 |
| Danger | 暂缓维护继续下潜 | 默认降级，必要时二次确认。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| Full Repair | 作为默认选中方案或一键综合方案，不和其他动作并列。 |
| Use Repair Kit | 并入“磨损修复”方案，材料足够时显示为方案可执行。 |
| Purify / Corruption | 作为“侵蚀净化”方案卡。 |
| Dive Permit | 并入诊断板，必要时作为“出发检查”方案卡。 |
| Postpone | 降级为危险动作，默认不抢主行动。 |
| Close | 小型次行动。 |

---

## 8. 场景隐喻

维护界面像同一个工作室里切换出的安静护理台：

* 魔偶继续位于 `workshop_studio` 的改造椅 / 护理位，不切换到完全不同房间。
* 桌面上有布、黄铜工具、玻璃药剂和修补材料。
* 侵蚀以暗色污渍、裂纹或薄雾表现。
* 下潜许可像盖章的检查牌，而不是系统状态字段。

---

## 9. 程序迁移影响

确认后建议层级：

```text
MaintenancePanel
  StudioBackground
  StudioModeTabs
  CareBay
  DiagnosisBoard
  TreatmentTray
  CostTokenRow
  MaintenanceActionPanel
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| 魔偶 HP / SAN / Bond / 疲劳 | Doll / Growth 只读快照。 |
| 磨损 / 侵蚀 | MaintenanceService 只读诊断。 |
| 下潜许可 | DiveReadinessService。 |
| 材料 / 金币缺口 | Inventory / Economy 只读快照。 |
| 执行维护 | 领域服务，不在 UI Controller 中扣费或改状态。 |

---

## 10. 素材需求变化

第一版复用：

| VisualID | 用法 |
|---|---|
| `bg_workshop_studio` / `bg_workshop_day` | 工作室背景，优先复用 `workshop_studio`。 |
| `doll_proto_0_stand` | 魔偶临时展示。 |
| `ui_panel_main` / `ui_panel_info` | 诊断板、方案卡、费用区。 |
| `ui_icon_wear_repair` | 磨损修复方案。 |
| `ui_icon_corruption_purify` | 侵蚀净化方案。 |
| `ui_icon_dive_permit` | 下潜许可检查。 |
| `ui_icon_warning` / `ui_icon_money` | 风险和费用。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `ui_maintenance_care_bay_frame` | 魔偶护理舱框体。 |
| `ui_maintenance_treatment_card` | 维护方案卡皮肤。 |
| `ui_maintenance_cost_token` | 材料 / 金币 token 底板。 |

---

## 10.1 概念图状态

旧 `maintenance_panel_formal_v2_concept.png` 已归档到 `concepts/archive/2026-06-01_workshop_studio_and_shop_semantics/`。归档原因：画面把维护做成独立大场景，但当前方向应是 `workshop_studio` 内的可切换维护模式页。

后续重新出图时，提示词必须明确：

* same cozy fantasy steampunk workshop studio as the doll modification room
* maintenance mode overlay
* shared doll chair and shared room background
* no separate room, no standalone clinic scene

---

## 11. UX 验收标准

运行时截图需要满足：

1. 3 秒内能看出这是魔偶维护界面，不是普通设置面板。
2. 魔偶状态、最大风险和是否允许下潜清楚可读。
3. 默认最高权重行动只有一个。
4. 三个维护方案是可比较的方案卡，不是散乱按钮。
5. 材料 / 金币缺口用图标和短文本表达，不形成长表格。
6. 暂缓维护是危险或低权重动作。

---

## 12. 用户确认问题

建议确认以下 3 点：

1. `maintenance_panel` 是否采用“护理舱 + 诊断板 + 维护方案托盘”的结构。
2. 是否把 Full Repair / Repair Kit / Purify / Dive Permit 收敛成方案选择，再由一个主按钮执行。
3. 是否允许暂缓维护默认降级，并在有下潜风险时二次确认。
