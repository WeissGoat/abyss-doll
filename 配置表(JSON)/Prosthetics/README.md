# 义体插件配置字段说明 (Prosthetics Config)

本目录定义局外工坊可制造并装备的义体插件。义体不占用背包网格，通过统一的 `Effects` 字段接入 `EffectFactory`，和物品效果使用同一套 `EffectData` 数据结构。

## 字段说明

| 字段 | 类型 | 说明 |
| :--- | :--- | :--- |
| `ProstheticID` | string | 义体全局唯一 ID |
| `Name` | string | UI 与日志显示名称 |
| `Level` | string | 品级，MVP 使用 `Primary` |
| `SlotType` | string | 安装槽位，如 `Core`、`Arm`；同槽位同时只保留一个义体 |
| `Effects` | array | 统一效果列表，元素结构与物品 `Combat.Effects` 一致 |

## Effects

义体不再使用旧字段 `PassiveEffect` / `PassiveEffects`。所有义体效果统一写为：

```json
{
  "EffectID": "DamageMultiplier",
  "Level": 1,
  "Target": "Melee",
  "Params": [0.1]
}
```

字段口径：

| 字段 | 类型 | 说明 |
| :--- | :--- | :--- |
| `EffectID` | string | 对应 C# `EffectBase` 派生类，由 `EffectFactory` 实例化 |
| `Level` | int | 效果等级 |
| `Target` | string | 对物品类效果可填物品 Tag，如 `Melee`；全局物品效果可填 `Global`；战斗事件效果可填 `Self` |
| `Params` | float[] | 效果参数，含义见 `/Effects/EffectEnums.json` |

## MVP 样例

`pros_power_arm`：

```json
{
  "ProstheticID": "pros_power_arm",
  "Name": "Power Arm Amplifier",
  "Level": "Primary",
  "SlotType": "Arm",
  "Effects": [
    {
      "EffectID": "DamageMultiplier",
      "Level": 1,
      "Target": "Melee",
      "Params": [0.1]
    }
  ]
}
```

`pros_cooling_system`：

```json
{
  "ProstheticID": "pros_cooling_system",
  "Name": "Stabilized Cooling System",
  "Level": "Primary",
  "SlotType": "Core",
  "Effects": [
    {
      "EffectID": "RestoreSANOnCombatEnd",
      "Level": 1,
      "Target": "Self",
      "Params": [2]
    }
  ]
}
```
