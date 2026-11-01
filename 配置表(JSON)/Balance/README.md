# Balance

数值尺子的目标值，给检查脚本和以后的数值测试用。**游戏运行时不读这个目录。**

- `balance_targets.json`：每层的每 AP 伤害、普通怪出手、物品预算比例、每场战斗的目标回合数。
- 每个数的来历和规则：`设计文档/数值/04_回合与AP尺子.md`。
- 检查：`python tools/balance/check_budget.py`（`--all` 列出全部，`--strict` 有超预算时退出码 1）。

改这里的数，要同时改 04 文档里对应的表。
