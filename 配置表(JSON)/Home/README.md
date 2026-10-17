# Home：家里的台词

零号在家里说的话，`ZeroLineService` 按「事实」挑一句。设计见 `docs/specs/2026-10-17-home-loop.md`。

每个文件一个台词池：

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
- `unhurt` 回来时 HP 是满的。

夜谈另有（`ZeroLineService.NightFacts`）：

- `dive` / `rest`：今天下潜还是休息。
- `rent_paid` 今晚交上了房租；`rent_short` 交不起、欠着或要典当；`rent_soon` 两天内要交。
- `no_sales` 今晚一件没卖；`good_sales` 营业收入 300G 以上。
- `low_san` SAN 低于三成；`hurt` HP 低于一半；`worn` 磨损或侵蚀到 60。
