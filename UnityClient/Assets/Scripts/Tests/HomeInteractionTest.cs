using System.Collections.Generic;
using UnityEngine;

// Talking with Zero and touching her (docs/specs/2026-10-18-home-evening-thread.md, B): bond +1 for the day's first
// chat and the day's first welcome touch, her story told a piece a day as the bond allows, shying away at low SAN
// without losing bond, enough for today after a while, and talking counts toward 精神好.
public static class HomeInteractionTest {
    public static void Run() {
        Debug.Log("=== Running Home Interaction Test ===");

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();

        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;
        List<string> failures = new List<string>();
        void Check(bool condition, string message) {
            if (!condition) {
                failures.Add(message);
            }
        }

        player.CurrentDay = 5;
        doll.Bond.AffectionLevel = 0;
        doll.Bond.Points = 0;
        doll.Status.SAN_Current = doll.Status.SAN_Max;
        doll.Status.HP_Current = doll.Status.HP_Max;

        // The day's first chat: bond +1 and the first piece of her story.
        ZeroAnswer first = ZeroInteractionService.Chat(player, HomeRoom.Hall);
        Check(first.Bond != null && first.Bond.PointsAdded == ZeroInteractionService.ChatBond, "the first chat gave no bond");
        Check(first.IsLore && player.LoreHeard == 1 && first.Line.Contains("不记得"), "first chat story: " + first.Line);
        Check(player.TalkedDay == player.CurrentDay, "talking was not recorded for 精神好");
        ZeroAnswer second = ZeroInteractionService.Chat(player, HomeRoom.Hall);
        Check(second.Bond == null && !second.IsLore && !string.IsNullOrEmpty(second.Line), "a second chat gave bond or story: " + second.Line);
        HomeLine said = ConfigManager.HomeLines[ZeroInteractionService.ChatPool].Lines.Find(line => line.Text == second.Line);
        Check(said != null && second.Face == said.Face, $"the chat line came without its face ({second.Face})");

        // The next day: the second piece; the third waits for bond 1.
        player.CurrentDay = 6;
        Check(ZeroInteractionService.Chat(player, HomeRoom.Attic).IsLore && player.LoreHeard == 2, "the second piece was not told");
        doll.Bond.AffectionLevel = 0;
        player.CurrentDay = 7;
        ZeroAnswer waiting = ZeroInteractionService.Chat(player, HomeRoom.Attic);
        Check(!waiting.IsLore && player.LoreHeard == 2, "the story went on without the bond for it");
        doll.Bond.AffectionLevel = 1;
        player.CurrentDay = 8;
        Check(ZeroInteractionService.Chat(player, HomeRoom.Attic).IsLore && player.LoreHeard == 3, "the third piece was not told at bond 1+");

        // Touch: the day's first welcome touch gives bond, later ones only answer.
        ZeroAnswer pat = ZeroInteractionService.Touch(player, TouchRegion.Head);
        Check(pat.Accepted && pat.Reaction == ZeroReaction.Hop && pat.Bond != null, "a pat on the head was not welcome");
        Check(!string.IsNullOrEmpty(pat.Line), "no line for a pat");
        ZeroAnswer hand = ZeroInteractionService.Touch(player, TouchRegion.Hand);
        Check(hand.Accepted && hand.Bond == null, "a second touch gave bond again");

        // Low SAN: she shies away from a hand on her head; no bond lost.
        int before = BondService.Points(doll);
        doll.Status.SAN_Current = 1;
        player.CurrentDay = 9;
        ZeroAnswer shy = ZeroInteractionService.Touch(player, TouchRegion.Head);
        Check(!shy.Accepted && shy.Reaction == ZeroReaction.Flinch && shy.Line.Contains("不是你的手"), "low-SAN head touch: " + shy.Line);
        Check(BondService.Points(doll) == before, "shying away cost bond");
        doll.Status.SAN_Current = doll.Status.SAN_Max;

        // Bond 3: she leans in.
        doll.Bond.AffectionLevel = 3;
        player.CurrentDay = 10;
        ZeroAnswer lean = ZeroInteractionService.Touch(player, TouchRegion.Shoulder);
        Check(lean.Reaction == ZeroReaction.Lean, "no lean at bond " + BondService.Level(doll));

        // Enough for today.
        ZeroAnswer last = null;
        for (int i = 0; i < ZeroInteractionService.TouchLimit + 1; i++) {
            last = ZeroInteractionService.Touch(player, TouchRegion.Hand);
        }
        Check(last != null && !last.Accepted && last.Reaction == ZeroReaction.None && last.Line.Contains("今天"), "too many touches: " + last?.Line);

        // Saved.
        SaveFile restored = SaveService.Deserialize(SaveService.Serialize(player));
        Check(restored.Player.LoreHeard == player.LoreHeard && restored.Player.TouchBondDay == player.TouchBondDay, "interaction state lost in the save");

        if (failures.Count == 0) {
            Debug.Log("Home Interaction PASSED.");
        } else {
            Debug.LogError("Home Interaction FAILED: " + string.Join("; ", failures));
        }

        Debug.Log("=== Home Interaction Test Finished ===");
    }
}
