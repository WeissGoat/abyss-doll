using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class MonsterActionContext {
    public CombatSystem Combat;
    public MonsterFighter Actor;
    public CombatFaction PlayerFaction;
    public CombatFaction EnemyFaction;
    public DollEntity ActiveDoll;
    public MonsterActionRuntimeState RuntimeState;
    public MonsterCombatModifierSystem RuntimeModifiers;
}

public enum MonsterAISelectorType {
    WeightedRandom
}

public enum MonsterActionType {
    DamageTarget,
    ReduceWeaponDamage,
    AddCursedItem
}

public enum MonsterTargetType {
    FirstAlivePlayer,
    RandomPlayer,
    LowestHpPlayer,
    RandomPlayerWeapon,
    PlayerGridFirstFit
}

public enum MonsterActionConditionType {
    Always,
    PlayerHasWeapon,
    PlayerGridHasSpace
}

public static class MonsterActionConfigParser {
    public static bool TryParseSelector(string selectorID, out MonsterAISelectorType selectorType) {
        string normalized = string.IsNullOrEmpty(selectorID) ? nameof(MonsterAISelectorType.WeightedRandom) : selectorID;
        return Enum.TryParse(normalized, true, out selectorType);
    }

    public static bool TryParseActionType(string actionTypeID, out MonsterActionType actionType) {
        actionType = default;
        return !string.IsNullOrEmpty(actionTypeID) && Enum.TryParse(actionTypeID, true, out actionType);
    }

    public static bool TryParseTarget(string targetID, MonsterTargetType defaultType, out MonsterTargetType targetType) {
        string normalized = string.IsNullOrEmpty(targetID) ? defaultType.ToString() : targetID;
        return Enum.TryParse(normalized, true, out targetType);
    }

    public static bool TryParseCondition(string conditionID, out MonsterActionConditionType conditionType) {
        string normalized = string.IsNullOrEmpty(conditionID) ? nameof(MonsterActionConditionType.Always) : conditionID;
        return Enum.TryParse(normalized, true, out conditionType);
    }

    public static MonsterTargetType GetDefaultTarget(MonsterActionType actionType) {
        switch (actionType) {
            case MonsterActionType.ReduceWeaponDamage:
                return MonsterTargetType.RandomPlayerWeapon;
            case MonsterActionType.AddCursedItem:
                return MonsterTargetType.PlayerGridFirstFit;
            case MonsterActionType.DamageTarget:
            default:
                return MonsterTargetType.FirstAlivePlayer;
        }
    }

    public static bool IsTargetCompatible(MonsterActionType actionType, MonsterTargetType targetType) {
        switch (actionType) {
            case MonsterActionType.DamageTarget:
                return targetType == MonsterTargetType.FirstAlivePlayer
                    || targetType == MonsterTargetType.RandomPlayer
                    || targetType == MonsterTargetType.LowestHpPlayer;
            case MonsterActionType.ReduceWeaponDamage:
                return targetType == MonsterTargetType.RandomPlayerWeapon;
            case MonsterActionType.AddCursedItem:
                return targetType == MonsterTargetType.PlayerGridFirstFit;
            default:
                return false;
        }
    }
}

public class MonsterActionRunner {
    public bool ExecuteTurn(MonsterFighter actor, MonsterActionContext context) {
        if (actor == null || actor.RuntimeHP <= 0) {
            return false;
        }

        if (context == null) {
            context = new MonsterActionContext();
        }

        context.Actor = actor;
        if (context.RuntimeState == null) {
            context.RuntimeState = new MonsterActionRuntimeState();
        }

        if (context.RuntimeModifiers == null) {
            context.RuntimeModifiers = new MonsterCombatModifierSystem();
        }

        MonsterAIConfig ai = actor.DataRef?.AI;
        if (ai == null || ai.Actions == null || ai.Actions.Count == 0) {
            Debug.LogError($"[MonsterActionAI] Monster [{actor.Name}] has no AI.Actions config.");
            return false;
        }

        IMonsterActionSelector selector = MonsterActionSelectorFactory.Create(ai.Selector);
        if (selector == null) {
            Debug.LogError($"[MonsterActionAI] Monster [{actor.Name}] uses unknown selector [{ai.Selector}].");
            return false;
        }

        MonsterActionConfig selected = selector.SelectAction(context, ai);
        if (selected == null) {
            Debug.LogWarning($"[MonsterActionAI] Monster [{actor.Name}] has no executable action this turn.");
            return false;
        }

        MonsterActionBase action = MonsterActionFactory.Create(selected);
        if (action == null) {
            Debug.LogError($"[MonsterActionAI] Action [{selected.ActionID}] type [{selected.ActionType}] is not implemented.");
            return false;
        }

        bool executed = action.Execute(context);
        if (executed) {
            context.RuntimeState.MarkUsed(actor, selected);
        }

        return executed;
    }
}

public class MonsterActionRuntimeState {
    private readonly Dictionary<string, int> _cooldowns = new Dictionary<string, int>();
    private readonly Dictionary<string, int> _uses = new Dictionary<string, int>();

    public void Reset() {
        _cooldowns.Clear();
        _uses.Clear();
    }

    public void AdvanceCooldownsAtEnemyTurnStart() {
        List<string> keys = new List<string>(_cooldowns.Keys);
        foreach (string key in keys) {
            _cooldowns[key]--;
            if (_cooldowns[key] <= 0) {
                _cooldowns.Remove(key);
            }
        }
    }

    public bool IsActionAvailable(MonsterFighter actor, MonsterActionConfig action) {
        if (actor == null || action == null) {
            return false;
        }

        string key = BuildKey(actor, action);
        if (_cooldowns.TryGetValue(key, out int remainingCooldown) && remainingCooldown > 0) {
            return false;
        }

        if (action.UsesPerCombat > 0 && _uses.TryGetValue(key, out int usedCount) && usedCount >= action.UsesPerCombat) {
            return false;
        }

        return true;
    }

    public void MarkUsed(MonsterFighter actor, MonsterActionConfig action) {
        if (actor == null || action == null) {
            return;
        }

        string key = BuildKey(actor, action);
        if (!_uses.ContainsKey(key)) {
            _uses[key] = 0;
        }
        _uses[key]++;

        if (action.CooldownTurns > 0) {
            _cooldowns[key] = action.CooldownTurns;
        }
    }

    private string BuildKey(MonsterFighter actor, MonsterActionConfig action) {
        string actorKey = !string.IsNullOrEmpty(actor.RuntimeID) ? actor.RuntimeID : actor.DataRef?.MonsterID ?? actor.Name;
        string actionKey = !string.IsNullOrEmpty(action.ActionID) ? action.ActionID : action.ActionType;
        return $"{actorKey}:{actionKey}";
    }
}

public class MonsterCombatModifierSystem {
    private readonly List<MonsterRuntimeItemModifier> _itemModifiers = new List<MonsterRuntimeItemModifier>();

    public void Clear() {
        _itemModifiers.Clear();
    }

    public void AddWeaponDamageMultiplier(ItemEntity item, float multiplier, int durationPlayerTurns, string sourceActionID) {
        if (item == null || string.IsNullOrEmpty(item.InstanceID) || durationPlayerTurns <= 0) {
            return;
        }

        MonsterRuntimeItemModifier existing = _itemModifiers.Find(modifier =>
            modifier.ItemInstanceID == item.InstanceID &&
            string.Equals(modifier.SourceActionID, sourceActionID, StringComparison.OrdinalIgnoreCase));

        if (existing != null) {
            existing.Multiplier = multiplier;
            existing.RemainingPlayerTurns = durationPlayerTurns;
            return;
        }

        _itemModifiers.Add(new MonsterRuntimeItemModifier {
            ItemInstanceID = item.InstanceID,
            Multiplier = multiplier,
            RemainingPlayerTurns = durationPlayerTurns,
            SourceActionID = sourceActionID
        });
    }

    public void ApplyToGrid(BackpackGrid grid) {
        if (grid == null || grid.ContainedItems == null || _itemModifiers.Count == 0) {
            return;
        }

        RemoveMissingItems(grid);

        foreach (MonsterRuntimeItemModifier modifier in _itemModifiers) {
            ItemEntity item = grid.ContainedItems.Find(candidate => candidate.InstanceID == modifier.ItemInstanceID);
            if (!MonsterTargetSelector.IsWeaponItem(item)) {
                continue;
            }

            item.Combat.RuntimeDamage = Mathf.Max(0, Mathf.RoundToInt(item.Combat.RuntimeDamage * modifier.Multiplier));
        }
    }

    public void AdvancePlayerTurnEnd() {
        for (int i = _itemModifiers.Count - 1; i >= 0; i--) {
            _itemModifiers[i].RemainingPlayerTurns--;
            if (_itemModifiers[i].RemainingPlayerTurns <= 0) {
                _itemModifiers.RemoveAt(i);
            }
        }
    }

    private void RemoveMissingItems(BackpackGrid grid) {
        for (int i = _itemModifiers.Count - 1; i >= 0; i--) {
            bool exists = grid.ContainedItems.Exists(item => item.InstanceID == _itemModifiers[i].ItemInstanceID);
            if (!exists) {
                _itemModifiers.RemoveAt(i);
            }
        }
    }
}

public class MonsterRuntimeItemModifier {
    public string ItemInstanceID;
    public float Multiplier;
    public int RemainingPlayerTurns;
    public string SourceActionID;
}

public interface IMonsterActionSelector {
    MonsterActionConfig SelectAction(MonsterActionContext context, MonsterAIConfig aiConfig);
}

public static class MonsterActionSelectorFactory {
    public static IMonsterActionSelector Create(string selectorID) {
        if (!MonsterActionConfigParser.TryParseSelector(selectorID, out MonsterAISelectorType selectorType)) {
            return null;
        }

        switch (selectorType) {
            case MonsterAISelectorType.WeightedRandom:
                return new WeightedRandomMonsterActionSelector();
            default:
                return null;
        }
    }

    public static bool IsSelectorRegistered(string selectorID) {
        return MonsterActionConfigParser.TryParseSelector(selectorID, out _);
    }
}

public class WeightedRandomMonsterActionSelector : IMonsterActionSelector {
    public MonsterActionConfig SelectAction(MonsterActionContext context, MonsterAIConfig aiConfig) {
        List<MonsterActionConfig> candidates = new List<MonsterActionConfig>();
        int totalWeight = 0;

        foreach (MonsterActionConfig actionConfig in aiConfig.Actions) {
            if (actionConfig == null || actionConfig.Weight <= 0) {
                continue;
            }

            if (context.RuntimeState != null && !context.RuntimeState.IsActionAvailable(context.Actor, actionConfig)) {
                continue;
            }

            if (!MonsterActionConditionEvaluator.Evaluate(actionConfig.Condition, context, actionConfig)) {
                continue;
            }

            MonsterActionBase action = MonsterActionFactory.Create(actionConfig);
            if (action == null || !action.CanExecute(context)) {
                continue;
            }

            candidates.Add(actionConfig);
            totalWeight += actionConfig.Weight;
        }

        if (candidates.Count == 0 || totalWeight <= 0) {
            return null;
        }

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cursor = 0;
        foreach (MonsterActionConfig candidate in candidates) {
            cursor += candidate.Weight;
            if (roll < cursor) {
                return candidate;
            }
        }

        return candidates[candidates.Count - 1];
    }
}

public abstract class MonsterActionBase {
    protected MonsterActionConfig Config { get; private set; }
    protected MonsterActionParamReader Params { get; private set; }

    protected MonsterActionBase(MonsterActionConfig config) {
        Config = config;
        Params = new MonsterActionParamReader(config);
    }

    public virtual bool CanExecute(MonsterActionContext context) {
        return true;
    }

    public abstract bool Execute(MonsterActionContext context);
}

public static class MonsterActionFactory {
    public static MonsterActionBase Create(MonsterActionConfig config) {
        if (config == null || !MonsterActionConfigParser.TryParseActionType(config.ActionType, out MonsterActionType actionType)) {
            return null;
        }

        switch (actionType) {
            case MonsterActionType.DamageTarget:
                return new DamageTargetAction(config);
            case MonsterActionType.ReduceWeaponDamage:
                return new ReduceWeaponDamageAction(config);
            case MonsterActionType.AddCursedItem:
                return new AddCursedItemAction(config);
            default:
                return null;
        }
    }

    public static bool IsActionRegistered(string actionType) {
        return MonsterActionConfigParser.TryParseActionType(actionType, out _);
    }
}

public class DamageTargetAction : MonsterActionBase {
    public DamageTargetAction(MonsterActionConfig config) : base(config) {}

    public override bool CanExecute(MonsterActionContext context) {
        return Params.GetInt("Damage", 0) > 0 && MonsterTargetSelector.SelectPlayerTarget(context, Config.Target) != null;
    }

    public override bool Execute(MonsterActionContext context) {
        int damage = Params.GetInt("Damage", 0);
        int repeatCount = Mathf.Max(1, Params.GetInt("RepeatCount", 1));
        if (damage <= 0) {
            Debug.LogError($"[MonsterActionAI] DamageTarget action [{Config.ActionID}] has invalid Damage [{damage}].");
            return false;
        }

        bool executed = false;
        for (int i = 0; i < repeatCount; i++) {
            FighterEntity target = MonsterTargetSelector.SelectPlayerTarget(context, Config.Target);
            if (target == null) {
                break;
            }

            context.Actor.DealActionDamage(target, damage, Config.ActionID);
            executed = true;

            if (context.PlayerFaction != null && context.PlayerFaction.IsWipedOut()) {
                break;
            }
        }

        return executed;
    }
}

public class ReduceWeaponDamageAction : MonsterActionBase {
    public ReduceWeaponDamageAction(MonsterActionConfig config) : base(config) {}

    public override bool CanExecute(MonsterActionContext context) {
        return MonsterTargetSelector.SelectPlayerWeapon(context, Config.Target) != null;
    }

    public override bool Execute(MonsterActionContext context) {
        ItemEntity weapon = MonsterTargetSelector.SelectPlayerWeapon(context, Config.Target);
        if (weapon == null) {
            Debug.LogWarning($"[MonsterActionAI] ReduceWeaponDamage action [{Config.ActionID}] found no weapon target.");
            return false;
        }

        float multiplier = Params.GetFloat("Multiplier", 1f);
        int durationPlayerTurns = Params.GetInt("DurationPlayerTurns", 1);
        if (multiplier <= 0f || durationPlayerTurns <= 0) {
            Debug.LogError($"[MonsterActionAI] ReduceWeaponDamage action [{Config.ActionID}] has invalid params.");
            return false;
        }

        context.RuntimeModifiers.AddWeaponDamageMultiplier(weapon, multiplier, durationPlayerTurns, Config.ActionID);
        GameEventBus.PublishAttackAction(context.Actor.Name, weapon.Name, Config.ActionID);
        Debug.Log($"[MonsterActionAI] {context.Actor.Name} reduced [{weapon.Name}] damage by multiplier {multiplier} for {durationPlayerTurns} player turn(s).");

        if (context.ActiveDoll != null) {
            GridSolver.RecalculateAllEffects(context.ActiveDoll);
        }

        return true;
    }
}

public class AddCursedItemAction : MonsterActionBase {
    public AddCursedItemAction(MonsterActionConfig config) : base(config) {}

    public override bool CanExecute(MonsterActionContext context) {
        string itemID = Params.GetString("ItemID", string.Empty);
        if (string.IsNullOrEmpty(itemID) || !ConfigManager.Items.ContainsKey(itemID)) {
            return false;
        }

        ItemEntity previewItem = ConfigManager.CreateItem(itemID);
        return previewItem != null && MonsterTargetSelector.CanPlaceItemInPlayerGrid(context, previewItem);
    }

    public override bool Execute(MonsterActionContext context) {
        string itemID = Params.GetString("ItemID", string.Empty);
        if (string.IsNullOrEmpty(itemID) || !ConfigManager.Items.ContainsKey(itemID)) {
            Debug.LogError($"[MonsterActionAI] AddCursedItem action [{Config.ActionID}] references missing ItemID [{itemID}].");
            return false;
        }

        BackpackGrid grid = MonsterTargetSelector.GetPlayerGrid(context);
        if (grid == null) {
            Debug.LogWarning($"[MonsterActionAI] AddCursedItem action [{Config.ActionID}] found no player grid.");
            return false;
        }

        ItemEntity item = ConfigManager.CreateItem(itemID);
        ApplyOverrides(item);

        if (!grid.TryPlaceFirstAvailable(item, out int x, out int y)) {
            Debug.LogWarning($"[MonsterActionAI] AddCursedItem action [{Config.ActionID}] could not place [{itemID}] in player grid.");
            return false;
        }

        GameEventBus.PublishAttackAction(context.Actor.Name, item.Name, Config.ActionID);
        GameEventBus.PublishItemPlaced(item.InstanceID, x, y);
        if (context.ActiveDoll != null) {
            GridSolver.RecalculateAllEffects(context.ActiveDoll);
        }

        Debug.Log($"[MonsterActionAI] {context.Actor.Name} added cursed item [{item.Name}] at ({x},{y}).");
        return true;
    }

    private void ApplyOverrides(ItemEntity item) {
        if (item == null) {
            return;
        }

        List<string> overrideTags = Params.GetStringList("OverrideTags");
        if (overrideTags.Count > 0) {
            if (item.Tags == null) {
                item.Tags = new List<string>();
            }

            foreach (string tag in overrideTags) {
                if (!item.Tags.Exists(existing => string.Equals(existing, tag, StringComparison.OrdinalIgnoreCase))) {
                    item.Tags.Add(tag);
                }
            }
        }

        if (Params.Has("OverrideValue")) {
            item.BaseValue = Mathf.Max(0, Params.GetInt("OverrideValue", item.BaseValue));
        }
    }
}

public static class MonsterActionConditionEvaluator {
    public static bool Evaluate(string condition, MonsterActionContext context, MonsterActionConfig actionConfig) {
        if (!MonsterActionConfigParser.TryParseCondition(condition, out MonsterActionConditionType conditionType)) {
            Debug.LogWarning($"[MonsterActionAI] Unknown action condition [{condition}].");
            return false;
        }

        switch (conditionType) {
            case MonsterActionConditionType.Always:
                return true;
            case MonsterActionConditionType.PlayerHasWeapon:
                return MonsterTargetSelector.HasPlayerWeapon(context);
            case MonsterActionConditionType.PlayerGridHasSpace:
                return HasSpaceForConfiguredItem(context, actionConfig);
            default:
                return false;
        }
    }

    public static bool IsConditionRegistered(string condition) {
        return MonsterActionConfigParser.TryParseCondition(condition, out _);
    }

    private static bool HasSpaceForConfiguredItem(MonsterActionContext context, MonsterActionConfig actionConfig) {
        MonsterActionParamReader reader = new MonsterActionParamReader(actionConfig);
        string itemID = reader.GetString("ItemID", string.Empty);
        if (string.IsNullOrEmpty(itemID) || !ConfigManager.Items.ContainsKey(itemID)) {
            return false;
        }

        ItemEntity item = ConfigManager.CreateItem(itemID);
        return item != null && MonsterTargetSelector.CanPlaceItemInPlayerGrid(context, item);
    }
}

public static class MonsterTargetSelector {
    public static FighterEntity SelectPlayerTarget(MonsterActionContext context, string targetSelector) {
        if (context?.PlayerFaction?.Fighters == null) {
            return null;
        }

        List<FighterEntity> aliveTargets = new List<FighterEntity>();
        foreach (FighterEntity fighter in context.PlayerFaction.Fighters) {
            if (fighter != null && fighter.RuntimeHP > 0) {
                aliveTargets.Add(fighter);
            }
        }

        if (aliveTargets.Count == 0) {
            return null;
        }

        if (!MonsterActionConfigParser.TryParseTarget(targetSelector, MonsterTargetType.FirstAlivePlayer, out MonsterTargetType targetType)) {
            Debug.LogWarning($"[MonsterActionAI] Unknown player target selector [{targetSelector}].");
            return null;
        }

        switch (targetType) {
            case MonsterTargetType.RandomPlayer:
                return aliveTargets[UnityEngine.Random.Range(0, aliveTargets.Count)];
            case MonsterTargetType.LowestHpPlayer:
                FighterEntity lowest = aliveTargets[0];
                foreach (FighterEntity candidate in aliveTargets) {
                    if (candidate.RuntimeHP < lowest.RuntimeHP) {
                        lowest = candidate;
                    }
                }
                return lowest;
            case MonsterTargetType.FirstAlivePlayer:
                return aliveTargets[0];
            default:
                Debug.LogWarning($"[MonsterActionAI] Target selector [{targetSelector}] is not a player target selector.");
                return null;
        }
    }

    public static ItemEntity SelectPlayerWeapon(MonsterActionContext context, string targetSelector) {
        BackpackGrid grid = GetPlayerGrid(context);
        if (grid == null || grid.ContainedItems == null) {
            return null;
        }

        List<ItemEntity> weapons = new List<ItemEntity>();
        foreach (ItemEntity item in grid.ContainedItems) {
            if (IsWeaponItem(item)) {
                weapons.Add(item);
            }
        }

        if (weapons.Count == 0) {
            return null;
        }

        if (!MonsterActionConfigParser.TryParseTarget(targetSelector, MonsterTargetType.RandomPlayerWeapon, out MonsterTargetType targetType)) {
            Debug.LogWarning($"[MonsterActionAI] Unknown weapon target selector [{targetSelector}].");
            return null;
        }

        switch (targetType) {
            case MonsterTargetType.RandomPlayerWeapon:
                return weapons[UnityEngine.Random.Range(0, weapons.Count)];
            default:
                Debug.LogWarning($"[MonsterActionAI] Target selector [{targetSelector}] is not a weapon target selector.");
                return null;
        }
    }

    public static bool HasPlayerWeapon(MonsterActionContext context) {
        return SelectPlayerWeapon(context, nameof(MonsterTargetType.RandomPlayerWeapon)) != null;
    }

    public static bool CanPlaceItemInPlayerGrid(MonsterActionContext context, ItemEntity item) {
        BackpackGrid grid = GetPlayerGrid(context);
        return grid != null && item != null && grid.TryFindFirstAvailable(item, out _, out _);
    }

    public static BackpackGrid GetPlayerGrid(MonsterActionContext context) {
        DollEntity doll = context?.ActiveDoll ?? GameRoot.Core?.CurrentPlayer?.ActiveDoll;
        return doll?.RuntimeGrid as BackpackGrid;
    }

    public static bool IsWeaponItem(ItemEntity item) {
        if (item?.Combat == null || item.ItemType != nameof(ItemType.Weapon)) {
            return false;
        }

        return item.Combat.DamageType == nameof(DamageType.Physical)
            || item.Combat.DamageType == nameof(DamageType.Energy);
    }

    public static bool IsTargetRegistered(string targetSelector) {
        return MonsterActionConfigParser.TryParseTarget(targetSelector, MonsterTargetType.FirstAlivePlayer, out _);
    }
}

public class MonsterActionParamReader {
    private readonly MonsterActionConfig _config;

    public MonsterActionParamReader(MonsterActionConfig config) {
        _config = config;
    }

    public bool Has(string key) {
        return _config?.Params != null && _config.Params.ContainsKey(key);
    }

    public int GetInt(string key, int defaultValue = 0) {
        JToken token = GetToken(key);
        if (token == null) {
            return defaultValue;
        }

        try {
            return token.Value<int>();
        } catch {
            return defaultValue;
        }
    }

    public float GetFloat(string key, float defaultValue = 0f) {
        JToken token = GetToken(key);
        if (token == null) {
            return defaultValue;
        }

        try {
            return token.Value<float>();
        } catch {
            return defaultValue;
        }
    }

    public string GetString(string key, string defaultValue = "") {
        JToken token = GetToken(key);
        if (token == null) {
            return defaultValue;
        }

        try {
            return token.Value<string>() ?? defaultValue;
        } catch {
            return defaultValue;
        }
    }

    public List<string> GetStringList(string key) {
        List<string> values = new List<string>();
        JToken token = GetToken(key);
        if (token == null) {
            return values;
        }

        if (token.Type == JTokenType.Array) {
            foreach (JToken child in token.Children()) {
                string value = child.Value<string>();
                if (!string.IsNullOrEmpty(value)) {
                    values.Add(value);
                }
            }
            return values;
        }

        string single = token.Value<string>();
        if (!string.IsNullOrEmpty(single)) {
            values.Add(single);
        }

        return values;
    }

    private JToken GetToken(string key) {
        if (string.IsNullOrEmpty(key) || _config?.Params == null) {
            return null;
        }

        return _config.Params.TryGetValue(key, out JToken token) ? token : null;
    }
}
