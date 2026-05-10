# 深渊怪物配置字段说明 (Monsters Config)

> 位于本目录下的 JSON 文件定义了在深渊战斗节点中遭遇的敌对实体。
> 怪物不仅具备传统 RPG 的打血机制，更具备针对**“背包网格体系”**的破坏能力。

## 字段说明表

| 字段名 (Key) | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `MonsterID` | string | 怪物的全局唯一ID | 必填项，如 `elite_mutant_amalgam` |
| `Name` | string | 怪物显示名称 | |
| `Layer` | int | 推荐出没的深渊层数 | 用于标识怪物的数值跨度级别 |
| `HP` | int | 怪物生命值上限 | 测试 DPS 输出检测的沙袋血量 |
| `AttacksPerTurn`| int | 怪物每回合可执行的攻击次数| 决定了玩家生存端的单回合爆发压力 |
| `DamageValue` | int | 怪物每次攻击造成的基础伤害 | 扣除玩家防御后的净伤害 |
| `RewardID` | string | 击败该怪物后触发的奖励表 ID | 指向 `/Rewards`，新配置必须使用此字段 |
| `LootPool` | array | 旧版掉落池 | Deprecated，仅迁移期 fallback 使用 |
| `GridInterference`| enum | **网格干涉能力（核心特色）** | 怪物对玩家背包系统直接造成的影响 |
| `GridInterferenceParams`| object | 配合干涉能力生效的具体参数 | 因干涉技能而异（例如强行塞垃圾的具体物品ID）|

---

## GridInterference (网格干涉能力) 枚举选项

深渊的恐怖之处在于，怪物不仅要你的命，还要搞乱你的包。

1. **`None`**: 普通怪，纯粹的拼数值换血。
2. **`ReduceDamage`**: （软泥怪等）喷吐腐蚀液，短时间内强行降低玩家某把武器的伤害数值。
3. **`LockCell`**: （蜘蛛等）吐丝/结冰，暂时锁死玩家背包的特定格子，使其上的武器/防具处于宕机瘫痪状态。
4. **`AddCursedItem`**: （深渊畸变体）**寄生/塞垃圾**。无视玩家意愿，强行往背包空位塞入不可丢弃、持续掉SAN的【毒性战利品】，极大挤压玩家生存空间并造成精神污染。

---

## RewardID (奖励表引用)

怪物不应该直接维护复杂掉落逻辑。击败怪物时，战斗节点只读取怪物的 `RewardID`，再交给 `RewardSystem` 解析。

```json
{
  "MonsterID": "elite_scrap_guard",
  "RewardID": "reward_monster_elite_scrap_guard"
}
```

奖励表负责声明保底奖励、权重奖励、空掉落和奖励组合，详见 [`../Rewards/README.md`](../Rewards/README.md)。

---

## LootPool (旧版战利品掉落池)

`LootPool` 是早期 MVP 直连掉落字段。引入 `RewardSystem` 后，该字段只作为迁移期 fallback 保留。新怪物和新奖励不应继续扩展 `LootPool`。

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `ItemID` | string | 可能掉落的物品配置ID | 对应 `Items` 目录下的合法 ConfigID |
| `Weight` | int | 掉落权重 | 在本次掉落判定中被抽取的相对概率 |
