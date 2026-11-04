# Balance

数值目标，给生成脚本、检查脚本和战斗实验台用。**游戏运行时不读这个目录。**

- `balance_targets.json`：每层的基准（本层物品和到达时的每 AP 伤害、每 AP 护盾、被动甲）、每种战斗的回合和扣血目标、怪物出手按"要几 AP 的盾、漏多少"的定法、物品预算比例、每只怪物的手感系数、实验台的参考配装。
- 每个数的来历和规则：`设计文档/数值/02_战斗数值.md`。改这里的数，同时改那份文档里对应的表。
- 怪物的血量和出手由它生成：`python tools/balance/apply_monster_targets.py --write`，然后 `.\tools\config\Sync-Configs.ps1 -Clean`。
- 检查：`python tools/balance/check_budget.py`（`--all` 列出全部，`--strict` 有超预算时退出码 1）。
- 实战量测：EditMode 测试 `FightLabTest`（报告在 `UnityClient/Logs/FightLabReport.txt`）。

经济（`Economy` 一节）：

- 每层一格的基准价、稀有度 / 形状 / 风险系数、装备的折扣、逐件的系数，一周的收入模型和每周房租占比的目标。
- 每个数的来历和规则：`设计文档/数值/01_经济数值.md`。
- 战利品和装备的 `BaseValue` 由它生成：`python tools/balance/apply_item_values.py --write`，然后同步配置。消耗品和任务物品手定。
- 检查掉落和房租：`python tools/balance/check_economy.py`（`--rewards` 列出每张奖励表的期望，`--strict` 不达标时退出码 1）。房租本身在 `Economy/economy_town_v1.json`。
