# 核心数据容器系统 (Core Data System)

> **定位：** 本文档指导程序如何实现 MVP 闭环所需的基础数据结构。
> **架构原则：** 采用“数据与表现分离”的纯 C# CBA (基于组件的架构)。核心数据**坚决不继承** `MonoBehaviour`，仅作为纯内存对象运行，方便后续的存档、反序列化和热更。

## 1. 全局单例与玩家档案 (PlayerProfile)

玩家的资产和解锁进度是全局存活的。

```csharp
[System.Serializable]
public class PlayerProfile {
    public string UID;
    public int Money;
    public int WorkshopLevel;
    
    // 当前出战的魔偶数据
    public DollEntity ActiveDoll;
    
    // 局外大仓库（不考虑网格，只存实体对象）
    public List<ItemEntity> StashInventory = new List<ItemEntity>();
}

// 游戏总控，挂载在常驻场景的空GameObject上
public class GameManager : MonoBehaviour {
    public static GameManager Instance;
    public PlayerProfile CurrentPlayer;
    
    void Awake() {
        // 单例初始化与存档读取逻辑
    }
}
```

## 2. 物品实体与组件化 (ItemEntity & Components)

**这是项目的灵魂。** 不再有 `class Sword : Weapon`，所有的物品都是一个包含字典（或固定字段）的基类。

```csharp
[System.Serializable]
public class ItemEntity {
    public string InstanceID; // 唯一实例ID (uuid)
    public string ConfigID;   // 关联配置表的ID (决定图标、名字)
    public ItemType Type;     // Weapon, Armor, Consumable, Loot
    public int BaseValue;     // 售卖基准价值

    // --- 组件 ---
    public ItemGridComponent GridComp;     // 网格组件 (必带)
    public ItemCombatComponent CombatComp; // 战斗组件 (仅武器/防具/消耗品有)
    public List<string> Tags;              // 标签列表，如 "Melee", "Toxic"
}

[System.Serializable]
public class ItemGridComponent {
    public Vector2Int[] Shape; // 形状坐标，如 L 型
    public int Rotation;       // 当前旋转角度(0, 90, 180, 270)
    public Vector2Int CurrentPos; // 当前放置在背包里的左上角原点坐标
}

[System.Serializable]
public class EffectData {
    public string EffectID;       // 效果ID，用于交给效果工厂实例化
    public int Level;             // 效果等级，支持升级
    public TargetDirection Target;// 生效方向(如 Right, Self)
    public float[] Params;        // 参数列表
}

[System.Serializable]
public class ItemCombatComponent {
    public TriggerType Trigger;   // 触发条件 (Manual, Passive 等)
    public int BaseValue;         // 基础伤害/治疗量
    public List<EffectData> Effects; // 物品效果列表(统合了连结与自身Buff)
}
```

## 3. 人偶实体与底层状态 (DollEntity)

魔偶是承载“血条”、“理智”以及“当前可用网格”的核心。

```csharp
[System.Serializable]
public class DollEntity {
    public string DollID;
    
    // 生存组件
    public int HP_Current;
    public int HP_Max;
    public int SAN_Current;
    public int SAN_Max;
    
    // 属性组件
    public int Power;
    public int Compute;
    public int Charm;
    
    // 当前安装的底盘组件
    public ChassisComponent Chassis;
    
    // 局内/当前生成的战斗网格实例
    [System.NonSerialized] 
    public BackpackGrid RuntimeGrid; 

    // 安装的局外义体
    public List<string> EquippedProsthetics = new List<string>();
}

[System.Serializable]
public class ChassisComponent {
    public string ChassisID;
    public int GridWidth;
    public int GridHeight;
    public bool[][] GridMask; // true为可用，false为不可用的死角
}
```

## 4. 数据配置表解析流程建议

1. 游戏启动时，由一个 `DataManager` 或 `ConfigManager` 读取我们在 `配置表(JSON)` 目录下生成的所有 JSON 文件。
2. 将解析出的模板数据存入全局 Dictionary 缓存。例如 `Dictionary<string, ItemConfigTemplate> ItemConfigs`。
3. 当玩家获得一件新物品时，调用工厂方法 `ItemFactory.CreateItem("gear_tactical_blade")`，根据 JSON 模板深拷贝生成一个独立的 `ItemEntity` 放入 `PlayerProfile.StashInventory` 或局内背包中。