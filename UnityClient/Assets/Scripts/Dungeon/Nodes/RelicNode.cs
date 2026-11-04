using System.Collections.Generic;

// 遗物祭台 (GDD_06 §3.2C): three relics to pick one from, or none. The pick goes into the backpack like loot, so it
// costs room, and works for this dive only (it crumbles at the exit, DungeonManager). The entry's SceneID gives the
// lines; the choices are rolled on entering, from the node's seed, so a resumed save offers the same three.
public class RelicNode : EventNode {
    public const int Offered = 3;

    protected override string DefaultTitle => "遗物祭台";
    protected override string DefaultDescription => "她把手从祭台上收了回来。";
    protected override string RewardSourceType => "RelicNode";

    private SceneConfig _scene;

    public override SceneConfig Scene => _scene;

    public override void OnEnterNode() {
        RollOffer();
        base.OnEnterNode();
    }

    // Rolls the three relics from the current random state (DungeonManager seeds it from the node).
    public void RollOffer() {
        _scene = BuildScene();
    }

    public override void Choose(string choiceID) {
        if (ConfigManager.Items.TryGetValue(choiceID ?? string.Empty, out ItemEntity template) && DungeonManager.IsRelic(template)) {
            ItemEntity relic = ConfigManager.CreateItem(choiceID);
            Resolve(null, null, FindChoice(choiceID)?.Result, new List<ItemEntity> { relic });
            return;
        }

        base.Choose(choiceID);
    }

    private SceneConfig BuildScene() {
        SceneConfig template = base.Scene;
        SceneConfig scene = new SceneConfig {
            SceneID = template?.SceneID ?? "relic_altar",
            Title = template?.Title ?? DefaultTitle,
            Lines = template?.Lines ?? new List<SceneLine>()
        };

        List<ItemEntity> pool = new List<ItemEntity>();
        foreach (ItemEntity item in ConfigManager.Items.Values) {
            if (DungeonManager.IsRelic(item)) {
                pool.Add(item);
            }
        }

        pool.Sort((a, b) => string.CompareOrdinal(a.ConfigID, b.ConfigID));
        for (int i = 0; i < Offered && pool.Count > 0; i++) {
            ItemEntity relic = pool[GameRandom.Range(0, pool.Count)];
            pool.Remove(relic);
            scene.Choices.Add(new SceneChoice {
                ID = relic.ConfigID,
                Label = relic.Name,
                Hint = $"{DungeonManager.ItemCells(relic)} 格 · {relic.Description}",
                Result = $"她把{relic.Name}收进背包。它在里面微微发热。"
            });
        }

        // The way out stays last, as in every event.
        SceneChoice leave = template?.Choices?.Find(choice => choice.ID == "leave");
        scene.Choices.Add(leave ?? new SceneChoice { ID = "leave", Label = "都不拿", Hint = "什么都不拿", Result = DefaultDescription });
        return scene;
    }
}
