using System;

public enum ItemType {
    Weapon = 0,
    Armor = 1,
    Consumable = 2,
    Loot = 3,
    QuestItem = 4,
    Anchor = 5
}

public enum ItemRarity {
    Common = 1,
    Uncommon = 2,
    Rare = 3,
    Epic = 4,
    Cursed = 5
}

public enum TriggerType {
    Passive = 0,
    Manual = 1
}

public enum DamageType {
    None = 0,
    Physical = 1,
    Energy = 2,
    Shield = 3,
    Heal = 4,
    RestoreSAN = 5
}

public enum TargetDirection {
    Self = 0,
    Right = 1,
    Left = 2,
    Up = 3,
    Down = 4,
    AllAdjacent = 5,
    Global = 6
}
