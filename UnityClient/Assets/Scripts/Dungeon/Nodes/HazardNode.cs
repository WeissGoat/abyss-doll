public class HazardNode : DungeonOutcomeNode {
    protected override string DefaultTitle => "危险房间";
    protected override string DefaultDescription => "房间中的污染机关造成了额外损耗。";
    protected override string RewardSourceType => "HazardNode";
}
