# MVP 需要补充的配置调整

> 本文档用于整理 MVP 白盒试玩前的配置补充与数值收口。本文只讨论配置层，不包含具体代码实现。初始装备中临时加入较多物品，是为了测试方便，不视为正式体验配置问题。

## 1. 当前配置覆盖度

当前配置表已经基本覆盖《最小 MVP 体验闭环内容清单》的数量要求：

*   1 个人偶，含 2 级底盘。
*   2 个义体插件。
*   2 个深渊层级。
*   4 个怪物。
*   12 个局内物品，覆盖武器、防具、消耗品、战利品、核心材料。
*   2 个义体制造配方。

因此当前不建议继续大量加内容。优先工作是让已有配置更准确地服务 MVP 验证。

**2026-05-10 配置冻结原则：**

MVP 白盒试玩前不再优先新增大量物品、怪物或装备。配置侧只允许围绕以下目标调整：

*   保证关键成长材料稳定出现。
*   明确物品职责：卖钱、制造、保命、战斗、占格、施压。
*   拆分测试便利配置与正式试玩配置。
*   为程序消费规则补齐显式字段，例如 `GuaranteedLoot`、`CanSell`、`PassiveEffects`、`CarryEffects`、`PortraitID`。

---

## 2. P0：必须调整

### 2.1 一阶动力核心改为 Boss 保底配置

**当前问题：** 【一阶动力核心】位于 `elite_scrap_guard` 的普通 `LootPool` 中，不符合文档中的“必掉”定位。

**调整建议：**

```json
{
  "GuaranteedLoot": [
    {
      "ItemID": "mat_core_tier1",
      "Count": 1
    }
  ],
  "LootPool": [
    { "ItemID": "gear_tactical_blade", "Weight": 35 },
    { "ItemID": "loot_rusty_coil", "Weight": 80 },
    { "ItemID": "con_cheap_sedative", "Weight": 20 }
  ]
}
```

**目的：** 确保玩家只要打赢 1 层守门人，就一定能进入“带不带得走核心”的背包取舍，而不是被掉落随机性截断升级闭环。

### 2.2 区分纯卖钱战利品与制造材料

**当前问题：** 【废旧齿轮】既是 `Loot`，又是义体制造材料，还具备 100G 售价。这个设计可以成立，但必须显式表达“卖掉会损失成长资源”。

**推荐方案 A：材料不可一键出售**

```json
{
  "ConfigID": "loot_gear_scrap",
  "ItemType": "Loot",
  "BaseValue": 100,
  "Tags": ["Material", "Mechanical"],
  "CanSell": false
}
```

**推荐方案 B：材料可出售，但 UI 强提醒**

```json
{
  "ConfigID": "loot_gear_scrap",
  "ItemType": "Loot",
  "BaseValue": 100,
  "Tags": ["Material", "Mechanical"],
  "CanSell": true,
  "SellWarning": "该物品可用于制造义体，出售后会延缓成长。"
}
```

**MVP 建议：** 先采用方案 A，降低误操作风险。等经济系统更成熟后，再开放材料出售。

### 2.3 义体配置统一为 `PassiveEffects`

**当前问题：** 义体配置使用 `PassiveEffect` 简写字段，而运行时效果系统更适合消费 `EffectData` 列表。

**调整建议：**

【动力臂增幅插件】：

```json
{
  "ProstheticID": "pros_power_arm",
  "Name": "动力臂增幅插件",
  "Level": "Primary",
  "SlotType": "Arm",
  "PassiveEffects": [
    {
      "EffectID": "DamageMultiplier",
      "Level": 1,
      "Target": "Global",
      "Params": [0.1],
      "TargetTags": ["Melee"]
    }
  ]
}
```

【稳压散热插件】：

```json
{
  "ProstheticID": "pros_cooling_system",
  "Name": "稳压散热插件",
  "Level": "Primary",
  "SlotType": "Core",
  "PassiveEffects": [
    {
      "EffectID": "RestoreSANOnCombatEnd",
      "Level": 1,
      "Target": "Global",
      "Params": [2]
    }
  ]
}
```

**备注：** 如果程序暂不支持 `TargetTags`，也可以先让动力臂增幅所有武器，但文档上应标注为临时验证口径。

---

## 3. P1：建议调整

### 3.1 战术长刀回调到文档数值

**当前问题：** 文档写【战术长刀】为 2 AP / 35 伤害，实际配置为 2 AP / 50 伤害。50 伤害会显著抬高 1 层输出上限，并压低短剑存在感。

**调整建议：**

*   正式 MVP 白盒试玩：`BaseValue = 35`。
*   若仍需要测试便利，可保留一个测试专用武器，不混入正式掉落池。

**理由：** 长刀应该是“占 3 格换更高 AP 效率”的过渡装备，而不是直接变成 1 层答案。

### 3.2 初始装备拆成测试配置与正式配置

**当前情况：** 初始装备加入战术长刀、修复剂、镇静剂等，是为了测试方便。这个做法合理，但建议从配置命名上区分，避免后续试玩误用测试档。

**调整建议：**

*   `doll_proto_0_test.json`：保留当前测试便利配置。
*   `doll_proto_0_mvp.json`：正式 MVP 试玩配置。

**正式 MVP 初始装建议：**

*   【生锈短剑】x1
*   【木制小盾】x1
*   【便携修复剂】x1

**可选：**

*   若要验证 SAN 消耗品，可额外给【廉价镇静剂】x1。
*   不建议正式开局直接给【战术长刀】，否则 1 层装备成长感会变弱。

### 3.3 2 层补一个二阶素材

**当前问题：** 文档闭环写到“获得 2 阶素材”，但当前配置里没有二阶素材或二阶核心。2 层胜利后主要获得高价战利品和装备，缺少下一轮成长钩子。

**新增建议：**

```json
{
  "ConfigID": "mat_core_tier2",
  "Name": "二阶污染核心",
  "ItemType": "QuestItem",
  "Rarity": "Epic",
  "BaseValue": 0,
  "Grid": {
    "Shape": [[0,0], [0,1], [1,0], [1,1]],
    "GridCost": 4
  },
  "Combat": null,
  "Tags": ["CoreMaterial", "Toxic", "Heavy"]
}
```

**投放建议：**

*   放入 `elite_mutant_amalgam` 的保底掉落。
*   暂时不需要接配方用途，只要能进入仓库并被结算展示即可。

### 3.4 毒性战利品标注携带代价

**当前问题：** 【污染滤芯】有 `Toxic` 标签，但配置中没有明确毒性数值。后续程序即使消费标签，也需要知道每节点扣多少 SAN。

**调整建议：**

```json
{
  "ConfigID": "loot_toxic_filter",
  "Tags": ["Toxic", "Mechanical"],
  "CarryEffects": [
    {
      "EffectID": "SANDrainOnNodeEnter",
      "Params": [1]
    }
  ]
}
```

**MVP 口径：** 每携带 1 件 Toxic 物品，进入新节点时额外 -1 SAN。

### 3.5 怪物与界面视觉 ID 补齐

**当前情况：** 美术资源已经进入 `VisualAssetRegistry`，但怪物、节点、背景仍需要配置或代码侧明确 VisualID 规则。

**调整建议：**

```json
{
  "MonsterID": "mob_scavenger_bug",
  "PortraitID": "monster_mob_scavenger_bug_portrait",
  "CombatVisualID": "monster_mob_scavenger_bug_combat"
}
```

节点与背景在 MVP 阶段可以先采用程序默认规则：

*   `CombatNode` -> `node_combat_icon`
*   `SafeRoomNode` -> `node_safe_room_icon`
*   Boss 节点 -> `node_boss_icon`
*   工坊背景 -> `bg_workshop_day`
*   深渊地图背景 -> `bg_dungeon_map`
*   战斗背景 -> `bg_combat_abyss`

**MVP 口径：** 怪物头像建议显式写入 `PortraitID`；节点图标和背景可以先用固定默认 ID，等后续多层多主题时再配置化。

---

## 4. P2：暂缓补充

### 4.1 暂不增加新怪物

4 个怪物已经足够验证 1 层基础战斗、1 层守门、2 层干涉、2 层精英。当前更重要的是让酸液与寄生干涉实际生效。

### 4.2 暂不增加大量新装备

现有装备已经覆盖：

*   低 AP 短剑。
*   叠盾盾牌。
*   过渡长刀。
*   被动装甲。
*   高占地大剑。
*   邻接增幅手枪。

继续加装备会稀释 MVP 测试焦点。若需要测试更多掉落手感，优先调整掉落权重，而不是新增品类。

### 4.3 暂不接完整房租曲线

房租/账单属于长期经济压力系统。MVP 白盒试玩可以先用底盘升级和义体制造作为金币回流目标。等“卖出 -> 升级 -> 2 层验证”跑顺后，再加入账单压力。

---

## 5. 推荐配置调整顺序

### 批次 A：第一轮白盒试玩前

1.  `elite_scrap_guard`：拆出【一阶动力核心】保底掉落。
2.  `pros_power_arm` / `pros_cooling_system`：统一为 `PassiveEffects`。
3.  `loot_gear_scrap`：明确是否可售，MVP 推荐不可被一键出售。
4.  拆分测试初始装与正式 MVP 初始装。

### 批次 B：第二轮压力验证前

1.  `gear_tactical_blade`：正式数值回调到 35 伤害。
2.  新增 `mat_core_tier2`，放入 2 层 Boss 保底掉落。
3.  `loot_toxic_filter`：补充携带 SAN 代价字段。
4.  2 层怪物干涉相关物品补齐，例如诅咒废件。

### 批次 C：表现接线前

1.  4 个怪物补齐 `PortraitID`。
2.  确认背景、节点、义体、底盘、人偶素材的 VisualID 与 Registry 一致。
3.  缺失资源统一回退到 `ui_missing_sprite`。

完成以上调整后，MVP 配置侧的目标不是“内容更多”，而是每个配置都能承担清晰职责：赚钱、制造、保命、战斗、占格、制造压力。
