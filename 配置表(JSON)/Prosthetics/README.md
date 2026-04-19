# 局外义体插件配置字段说明 (Prosthetics Config)

> 位于本目录下的 JSON 文件定义了在局外工坊制造的**“义体插件”**。
> 义体是**不占据背包网格**的装备。它提供稳定的被动加成，主要用于补充数值流失、或是放大某种网格拼图的构筑流派（比如为所有近战武器提供全局增伤）。

## 字段说明表

| 字段名 (Key) | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `ProstheticID`| string | 义体的全局唯一ID | 必填项，如 `pros_power_arm` |
| `Name` | string | 义体在UI上的显示名称 | |
| `Level` | enum | 义体的品级/阶位 | `Primary`(初级), `Advanced`(进阶), `Ultimate`(终极), `Forbidden`(违禁品/带副作用) |
| `SlotType` | enum | 该义体占据的魔偶安装槽位 | 每个部位只能装一个。如 `Head`(头部), `Core`(核心胸腔), `Arm`(臂部), `Leg`(腿部), `Module`(通用模块) |
| `PassiveEffect`| object | 穿戴后持续生效的被动触发效果 | 决定该义体的核心战术价值（见下表） |

---

## PassiveEffect (被动增益) 内部字段

义体绝不能只是无脑堆防御，必须是为了某种特定战术而生。

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `TargetTags` | array | 该效果所作用的网格内物品标签 | 若填 `["Global"]` 则不局限于特定物品。若填 `["Melee"]` 则只强化拥有近战标签的武器。 |
| `EffectType` | enum | 效果类型（底层逻辑钩子） | `DamageMultiplier`(增伤), `RestoreSANOnCombatEnd`(战斗结束后固定回SAN), `CooldownReduction`(减CD) |
| `Value` | float/int| 效果的具体数值系数 | 例如 `0.1` 代表增加10%伤害；`2` 代表恢复2点理智。 |