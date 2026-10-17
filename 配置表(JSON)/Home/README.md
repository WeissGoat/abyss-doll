# Home：家里的台词

零号在家里说的话（`ZeroLineService` 按「事实」挑一句），她的小心愿和架子上的纪念物（`wishes.json`）。设计见 `docs/specs/2026-10-17-home-loop.md`。

台词文件每个一个池：

| 字段 | 说明 |
|---|---|
| `PoolID` | `arrival`（进门时「今天最记得的事」）、`night`（营业后在阁楼的夜谈） |
| `Lines[].Tags` | 这一句需要的事实，全部满足才可选；空数组表示任何时候都行 |
| `Lines[].Text` | 台词，口吻同序章：短句，少形容词，不称呼玩家 |

挑选：满足条件的句子里，`Tags` 条数最多的优先；同档按天数轮换，同一天总是同一句。

## 事实

下潜（`DiveLogService.Facts`，两个池都有）：

- 当次下潜的重点，只有一个：`defeated` 战败、`nearly_fell` HP 掉到两成以下、`boss_defeated` 打倒首领、`elite_bypassed` 绕开了精英、`left_behind` 有战利品没装下、`ordinary` 其他。
- `layer1` / `layer2`：在哪一层。
- `empty_handed` 什么都没带回来；`good_haul` 带回估值 500G 以上。
- `unhurt` 回来时 HP 是满的；`held_on` 这次下潜里她「撑住」过一次（羁绊 3 级）。

夜谈另有（`ZeroLineService.NightFacts`）：

- `dive` / `rest`：今天下潜还是休息。
- `rent_paid` 今晚交上了房租；`rent_short` 交不起、欠着或要典当；`rent_soon` 两天内要交。
- `no_sales` 今晚一件没卖；`good_sales` 营业收入 300G 以上。
- `bond1` … `bond5`：羁绊到了这一级（及以下各级都算）；`wish_done` 今天完成了一个心愿。
- `low_san` SAN 低于三成；`hurt` HP 低于一半；`worn` 磨损或侵蚀到 60；`grimy` 身上还有 3 块以上污渍没擦。

## 心愿和纪念物（`wishes.json`）

早上零号头顶可能冒出一个心愿（第 3 天起，上一个结束后隔一天，同时最多一个）。完成：羁绊 +2，架子上多一件纪念物，她说 `DoneLine`。过了 `Days`（默认 4）天没完成就悄悄放下，没有惩罚。

| 字段 | 说明 |
|---|---|
| `WishID` | 唯一 ID |
| `Text` / `Hint` | 气泡里的心愿，和下面一行怎么完成 |
| `Kind` | `BringTag` 带回带某些标签的物品，在大厅点气泡交给她（物品放上架子）；`VisitNode` 下潜走到某类节点（`Param`：营地、宝藏、事件…）；`ReachLayer` 下潜到第 `Param` 层；`BossDefeated` 打倒首领；`Unhurt` 一次下潜一点不受伤；`Rest` 休息一天；`Clean` 把污渍擦干净（只在她身上有污渍时出现） |
| `Tags` | `BringTag` 用：物品带其中任一标签即可 |
| `MinDay` | 最早第几天出现 |
| `Days` | 几天后放下，默认 4 |
| `DoneLine` | 完成时她说的话，也是纪念物上的那句 |

`Milestones`：第一次打倒首领、第一次战败、第一次交上房租、第一次完成心愿时，架子上自动多一件（`MilestoneID` 固定，`Name`、`IconID`、`Line` 可改）。

## 家的改造（`upgrades.json`）

在工作室工作台的「改造」栏买，一次买断，存档。

| `Effect` | `Value` | 效果 |
|---|---|---|
| `MaintenanceDiscount` | 0.15 | 维护的金钱花费打折 |
| `RestSANBonus` | 0.2 | 休息多回的 SAN（占上限的比例） |
| `NightSAN` | 5 | 每晚营业之后 SAN 回复 |
| `ShowcaseColumns` | 1 | 橱窗多几列 |
| `ShelfSlots` | 3 | 架子多几个位置（基础 6 个） |
