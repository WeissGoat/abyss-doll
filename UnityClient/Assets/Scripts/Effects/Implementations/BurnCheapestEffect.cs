using System.Collections.Generic;
using UnityEngine;

// The trash cannon (粉碎炮, GDD_01 §5.2): each strike burns the cheapest piece of loot in the backpack (monster junk
// included) and adds Params[0] damage per cell it took up. Nothing to burn, no shot. The burnt item is gone for good.
public class BurnCheapestEffect : EffectBase {
    private float _perCell;

    public override void Init(EffectData data) {
        base.Init(data);
        _perCell = data.Params != null && data.Params.Length > 0 ? data.Params[0] : 0f;
    }

    public static ItemEntity Ammo(ItemEntity provider, BackpackGrid grid) {
        ItemEntity best = null;
        foreach (ItemEntity item in grid?.ContainedItems ?? new List<ItemEntity>()) {
            if (item == null || item == provider || item.ItemType != nameof(ItemType.Loot)) {
                continue;
            }

            if (best == null || item.BaseValue < best.BaseValue || (item.BaseValue == best.BaseValue && Cells(item) < Cells(best))) {
                best = item;
            }
        }

        return best;
    }

    private static int Cells(ItemEntity item) {
        return item?.Grid?.Shape?.Length ?? 1;
    }

    private static BackpackGrid Grid => GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;

    public override bool AllowsUse(ItemEntity provider, out string reason) {
        bool any = Ammo(provider, Grid) != null;
        reason = any ? string.Empty : $"[{provider?.Name}] 背包里没有能烧的战利品。";
        return any;
    }

    public override float StrikeBonus(ItemEntity provider) {
        ItemEntity ammo = Ammo(provider, Grid);
        return ammo == null ? 0f : _perCell * Cells(ammo);
    }

    // After the shot: the round it fired is burnt.
    public override void OnItemUsed(ItemUseOutcome outcome, ItemEntity provider) {
        ItemEntity ammo = Ammo(provider, Grid);
        if (ammo == null) {
            return;
        }

        if (ItemLifecycleService.TryConsumeBackpackItem(GameRoot.Core?.CurrentPlayer, ammo, "ScrapCannon", out _)) {
            GameEventBus.PublishItemRemoved(ammo.InstanceID);
            GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Interfere, Actor = outcome.User, Label = $"烧掉 {ammo.Name}" });
        }
    }

    public override string Describe(ItemEntity provider, int rotation) {
        BackpackGrid grid = Grid;
        ItemEntity ammo = Ammo(provider, grid);
        string next = ammo != null && grid != null && grid.ContainedItems.Contains(provider)
            ? $"（下一发烧 {ammo.Name}，+{Mathf.RoundToInt(StrikeBonus(provider))}）"
            : string.Empty;
        return $"每发烧掉背包里最便宜的战利品，它占几格伤害就加几个 {Mathf.RoundToInt(_perCell)}{next}";
    }

    public override void ValidateConfig(ConfigValidationReport report, string ownerID, EffectData data) {
        if (data.Params == null || data.Params.Length == 0 || data.Params[0] <= 0f) {
            report.AddError($"[{ownerID}] BurnCheapest needs Params[0] > 0 (damage per cell burnt).");
        }
    }
}
