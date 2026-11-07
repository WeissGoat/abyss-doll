// The solvent spray (溶剂喷雾): melts every crystal in the backpack and washes the spores off every item (GDD_01 §6.2).
public class PurgeBackpackEffect : EffectBase {
    private static BackpackGrid Grid => GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;

    public override bool AllowsUse(ItemEntity provider, out string reason) {
        bool dirty = (Grid?.CrystalCells().Count ?? 0) > 0 || (GameRoot.Core?.Combat?.Items?.PollutedCount ?? 0) > 0;
        reason = dirty ? string.Empty : $"[{provider?.Name}] 背包里没有结晶，也没有孢子。";
        return dirty;
    }

    public override void ApplyOnUse(ItemUseContext context, ItemEntity provider) {
        Grid?.ClearCrystal();
        GameRoot.Core?.Combat?.Items?.ClearPollution();
        GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Interfere, Actor = context?.UserFighter, Label = "溶剂化开了结晶，洗掉了孢子" });
    }

    public override string Describe(ItemEntity provider, int rotation) {
        return "使用：化开背包里所有结晶，洗掉所有孢子污染";
    }
}
