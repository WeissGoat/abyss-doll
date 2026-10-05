using System.Collections.Generic;
using UnityEngine;

// M3 batches 1-2 (docs/specs/2026-10-05-t0-first-loop.md): the first dive is a fixed three-step shallow-edge route
// (voice, one scavenger bug, the bring-back choice), and the return home closes the night and opens day 2.
public static class PrologueFirstLoopTest {
    public static void Run() {
        Debug.Log("=== Running Prologue First Loop Test ===");
        List<string> failures = new List<string>();
        void Check(bool condition, string message) {
            if (!condition) {
                failures.Add(message);
            }
        }

        // Listen to the voice, beat the bug, bring back the core dust, then care for her.
        CoreBackend core = Init();
        foreach (string sceneID in new[] { "t0_voice", "t0_voice_listen", "t0_mine_cart", "t0_head_home", "t0_return", "t0_care", "t0_care_core", "t0_notice", "t0_memory", "t0_loop_open" }) {
            Check(ConfigManager.Scenes.ContainsKey(sceneID), "missing scene " + sceneID);
        }

        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;
        List<PrologueBeatNode> beats = new List<PrologueBeatNode>();
        System.Action<PrologueBeatNode> onBeat = beats.Add;
        DungeonEventBus.OnPrologueBeatEntered += onBeat;
        try {
            int san = doll.Status.SAN_Current;
            Check(core.Dungeon.StartPrologueShallowRun() && core.Dungeon.IsPrologueRun, "the prologue run did not start");
            DungeonLayer layer = core.Dungeon.CurrentLayer;
            Check(layer.NodeRows.Count == 3 && layer.RootNode is PrologueVoiceNode, "the route is not voice -> fight -> cart");
            Check(PrologueShallowRun.BringBackOptions().Count == 2, "the memory shard is offered before listening");

            PrologueVoiceNode voice = layer.RootNode as PrologueVoiceNode;
            core.Dungeon.MoveToNode(voice);
            Check(beats.Contains(voice), "the voice beat did not reach the UI");
            voice.Choose(true);
            Check(doll.Status.SAN_Current <= san - PrologueShallowRun.ListenSanCost, "listening cost no SAN");
            Check(PrologueShallowRun.BringBackOptions().Contains(PrologueShallowRun.MemoryNoiseID), "listening did not offer the memory shard");

            CombatNode bug = voice.NextNodes[0] as CombatNode;
            core.Dungeon.MoveToNode(bug);
            Check(core.Combat.EnemyFaction.Fighters.Count == 2, "the fight is not two scavenger bugs");
            DollFighter fighter = core.Combat.PlayerFaction.Fighters[0] as DollFighter;
            fighter.RuntimeHP -= 12;
            doll.Status.HP_Current = fighter.RuntimeHP;
            bug.ResolveAfterVictory();

            PrologueBringBackNode cart = bug.NextNodes[0] as PrologueBringBackNode;
            core.Dungeon.MoveToNode(cart);
            Check(beats.Contains(cart), "the cart beat did not reach the UI");
            Check(cart.Choose(PrologueShallowRun.CoreDustID), "the core dust was not brought back");
            Check(player.IsNight && player.StashInventory.Exists(item => item.ConfigID == PrologueShallowRun.CoreDustID), "the run did not end in the night with the dust in the stash");
            Check(PrologueReturnHome.IsPending(core.Dungeon), "the return home is not pending");
            string recap = PrologueReturnHome.BuildRecap(player);
            Check(recap.Contains("带回：稳定核心碎屑") && recap.Contains("HP"), "recap: " + recap);

            Check(PrologueReturnHome.Care(player), "care did not use the core dust");
            Check(doll.Status.HP_Current == doll.Status.HP_Max && doll.Status.WearAndTear == 0f && !player.StashInventory.Exists(item => item.ConfigID == PrologueShallowRun.CoreDustID), "care did not restore her or keep the dust");
            int day = player.CurrentDay;
            PrologueReturnHome.Finish(player);
            Check(!player.IsNight && player.CurrentDay == day + 1 && !PrologueReturnHome.IsPending(core.Dungeon), "the night did not close into the next day");
            Check(DayCycleService.CanDive(player, out string reason), "day 2 cannot dive: " + reason);
        } finally {
            DungeonEventBus.OnPrologueBeatEntered -= onBeat;
        }

        // Follow the voice, bring back the scrap, read the notice: the scrap sells at the dump-box price.
        core = Init();
        player = core.CurrentPlayer;
        core.Dungeon.StartPrologueShallowRun();
        PrologueVoiceNode voice2 = core.Dungeon.CurrentLayer.RootNode as PrologueVoiceNode;
        core.Dungeon.MoveToNode(voice2);
        voice2.Choose(false);
        Check(!PrologueShallowRun.BringBackOptions().Contains(PrologueShallowRun.MemoryNoiseID), "the memory shard is offered without listening");
        CombatNode bug2 = voice2.NextNodes[0] as CombatNode;
        core.Dungeon.MoveToNode(bug2);
        bug2.ResolveAfterVictory();
        PrologueBringBackNode cart2 = bug2.NextNodes[0] as PrologueBringBackNode;
        core.Dungeon.MoveToNode(cart2);
        cart2.Choose(PrologueShallowRun.ScrapID);
        int money = player.Money;
        int income = PrologueReturnHome.ReadNotice(player);
        Check(income > 0 && player.Money == money + income && !player.StashInventory.Exists(item => item.ConfigID == PrologueShallowRun.ScrapID), $"the scrap did not sell: +{income}, money {money} -> {player.Money}");
        Check(!PrologueReturnHome.BroughtMemory, "the scrap counts as the memory shard");
        PrologueReturnHome.Finish(player);

        // A normal dive afterwards uses the random layer again.
        Check(core.Dungeon.StartRunAtLayer(1) && !core.Dungeon.IsPrologueRun && PrologueShallowRun.Current == null, "a normal dive still uses the prologue route");

        if (failures.Count == 0) {
            Debug.Log("Prologue First Loop PASSED.");
        } else {
            Debug.LogError("Prologue First Loop FAILED: " + string.Join("; ", failures));
        }

        GameRoot.Core = null;
        PrologueShallowRun.Reset();
        Debug.Log("=== Prologue First Loop Test Finished ===");
    }

    private static CoreBackend Init() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();
        DayCycleService.TakePendingReport();
        PrologueShallowRun.Reset();
        return core;
    }
}
