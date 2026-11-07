// A trigger chain's start with nothing of its own (火花电池, GDD_01 §5.1A): using it only sets off what its out ports
// link to, so it cannot be used while nothing is linked or Zero is jammed.
public class ChainStarterEffect : EffectBase {
    public override bool AllowsUse(ItemEntity provider, out string reason) {
        reason = string.Empty;
        BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        if (TriggerChain.State?.Jammed == true) {
            reason = $"[{provider?.Name}] 卡壳中，这回合链路不通。";
            return false;
        }

        if (grid != null && TriggerChain.Workers(provider, grid) == 0) {
            reason = $"[{provider?.Name}] 出口没有接上能带动的模块。";
            return false;
        }

        return true;
    }

    public override string Describe(ItemEntity provider, int rotation) {
        return "使用：只负责起链，带动出口接上的模块";
    }
}
