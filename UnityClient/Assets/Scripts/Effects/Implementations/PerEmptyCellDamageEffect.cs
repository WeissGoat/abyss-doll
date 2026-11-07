using UnityEngine;

// Negative space (孤高之刃, GDD_01 §5.2): Params[0] more damage for every empty usable cell in the backpack, counted
// at the moment of the strike. Every piece of loot picked up makes it weaker.
public class PerEmptyCellDamageEffect : EffectBase {
    private float _perCell;

    public override void Init(EffectData data) {
        base.Init(data);
        _perCell = data.Params != null && data.Params.Length > 0 ? data.Params[0] : 0f;
    }

    public static int EmptyCells() {
        BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        return grid == null ? 0 : Mathf.Max(0, grid.UsableCellCount() - grid.FilledCellCount());
    }

    public override float StrikeBonus(ItemEntity provider) {
        return _perCell * EmptyCells();
    }

    public override string Describe(ItemEntity provider, int rotation) {
        BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        string now = grid != null && grid.ContainedItems.Contains(provider) ? $"（现在空 {EmptyCells()} 格，+{Mathf.RoundToInt(StrikeBonus(provider))}）" : string.Empty;
        return $"背包里每空一格，伤害 +{Mathf.RoundToInt(_perCell)}{now}";
    }

    public override void ValidateConfig(ConfigValidationReport report, string ownerID, EffectData data) {
        if (data.Params == null || data.Params.Length == 0 || data.Params[0] <= 0f) {
            report.AddError($"[{ownerID}] PerEmptyCellDamage needs Params[0] > 0.");
        }
    }
}
