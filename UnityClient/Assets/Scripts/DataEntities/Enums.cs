using System;

public enum ItemType {
    Weapon = 0,
    Armor = 1,
    Consumable = 2,
    Loot = 3,
    QuestItem = 4,
    Anchor = 5
}

public enum ItemOwnerScope {
    Unknown = 0,
    Run = 1,
    Workshop = 2,
    Shop = 3,
    Consumed = 4,
    Lost = 5
}

public enum ItemContainerType {
    Unknown = 0,
    Generated = 1,
    Inbox = 2,
    Backpack = 3,
    SafeBox = 4,
    GroundInventory = 5,
    ShopBin = 6,
    Consumed = 7,
    Lost = 8,
    Sold = 9
}

public enum ItemLifecycleTransition {
    None = 0,
    GenerateToInbox = 1,
    InboxToBackpack = 2,
    BackpackToGroundInventory = 3,
    BackpackToLost = 4,
    BackpackToConsumed = 5,
    BackpackToSold = 6,
    GroundInventoryToConsumed = 7,
    GroundInventoryToSold = 8,
    ActiveDiscardToLost = 9,
    ShopBinToSold = 10,
    InboxToLost = 11
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

public enum CombatEventType {
    None = 0,
    OnTurnStart = 1,
    OnTurnEnd = 2,
    OnAttack = 3,
    OnTakeDamage = 4,
    OnCombatEnd = 5
}

public enum EffectTriggerType {
    None = 0,
    OnDungeonMoveCost = 1
}

public enum EffectResourceType {
    None = 0,
    SAN = 1,
    HP = 2,
    Money = 3,
    AP = 4
}

public enum EffectModifierOperation {
    AddFlat = 0,
    AddPercent = 1,
    Multiply = 2
}
