using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The enemy turn on the real combat screen: each enemy lunges and its hit lands (flash, shake, floating number)
// before the next one acts, and once the turn is over every fighter image is back at the place the HUD laid it out.
// Zero's own strike first (bare hands): with her pose set registered she snaps to the punch pose, the hit freezes the stage for
// a few frames and leaves a slash; hit by the enemies she shows hurt or guard, and ends back in her stance.
public static class CombatMotionPlayScenario {
    public static IEnumerator Run() {
        yield return PlayScenarioSupport.BootToWorkshop();
        P3DevTools.GoTo("combat");
        CombatMotionDirector director = null;
        yield return PlayScenarioSupport.Until(() => {
            director = Object.FindObjectOfType<CombatMotionDirector>();
            return director != null && GameRoot.Core.Combat.CurrentState == CombatState.PlayerTurn && VisualQueue.Count == 0;
        }, 10f, "no combat screen in a player turn");
        if (director == null) {
            yield break;
        }

        // The debug fight has one enemy; restart it with two of them so "one at a time" has something to measure.
        CombatSystem combat = GameRoot.Core.Combat;
        string monsterID = ((MonsterFighter)combat.EnemyFaction.Fighters[0]).DataRef.MonsterID;
        combat.StartCombat(new List<string> { monsterID, monsterID });
        yield return PlayScenarioSupport.Until(() => combat.CurrentState == CombatState.PlayerTurn && VisualQueue.Count == 0, 10f,
            "the two-enemy fight did not start");
        int enemies = combat.EnemyFaction.Fighters.Count;
        bool dollPoses = CombatMotionDirector.HasPose("doll_zero_combat", CombatPose.Punch);
        director.ResetRecording();
        if (ItemUseService.TryBeginUnarmedStrike(out string strikeFailure)) {
            ItemUseService.TryConfirmPendingTarget(combat.EnemyFaction.Fighters[0], out strikeFailure);
        }

        yield return PlayScenarioSupport.Until(() => VisualQueue.Count == 0, 10f, "Zero's strike did not finish playing");
        if (dollPoses && !director.PosesShown.Contains(CombatPose.Punch)) {
            Debug.LogError("[CombatMotion] Zero struck bare-handed but never showed her punch pose.");
        }

        if (director.HitstopsPlayed == 0 || director.VfxSpawned == 0) {
            Debug.LogError($"[CombatMotion] Zero's hit had no impact: {director.HitstopsPlayed} hitstop(s), {director.VfxSpawned} effect(s).");
        }

        director.ResetRecording();
        float started = Time.time;
        combat.EndPlayerTurn();
        HUDController turnHud = Object.FindObjectOfType<HUDController>();
        yield return new WaitForSeconds(0.2f);
        if (combat.CurrentState == CombatState.PlayerTurn && VisualQueue.Count > 0 && (turnHud.PlayerTurnShown || !HUDController.EnemyTurnShowing)) {
            Debug.LogError("[CombatMotion] The screen shows the player turn while the enemy turn is still playing.");
        }

        yield return PlayScenarioSupport.Until(() => combat.CurrentState != CombatState.EnemyTurn && VisualQueue.Count == 0, 20f,
            "the enemy turn did not finish playing");
        float turnSeconds = Time.time - started;
        yield return new WaitForSeconds(CombatMotionDirector.ShakeSeconds + 0.2f);

        List<float> strikeTimes = new List<float>();
        for (int i = 0; i < director.PlayedBeats.Count; i++) {
            CombatBeat beat = director.PlayedBeats[i];
            if ((beat.Kind == CombatBeatKind.Strike || beat.Kind == CombatBeatKind.Interfere) && beat.Actor is MonsterFighter) {
                strikeTimes.Add(director.PlayedBeatTimes[i]);
            }
        }

        if (strikeTimes.Count < 2) {
            Debug.LogError($"[CombatMotion] Fewer than two enemy actions played on screen ({director.PlayedBeats.Count} beats, {enemies} enemies).");
            yield break;
        }

        for (int i = 1; i < strikeTimes.Count; i++) {
            float gap = strikeTimes[i] - strikeTimes[i - 1];
            if (gap < 0.45f) {
                Debug.LogError($"[CombatMotion] Enemy actions {i - 1} and {i} started {gap:0.00}s apart; they should play one at a time.");
            }
        }

        if (director.MaxLungeOffset < 20f) {
            Debug.LogError($"[CombatMotion] Attackers barely moved (largest lunge {director.MaxLungeOffset:0.0}px).");
        }

        if (director.PopupsSpawned == 0) {
            Debug.LogError("[CombatMotion] No floating number appeared for the hits.");
        }

        HUDController hud = turnHud;
        if (!hud.PlayerTurnShown && combat.CurrentState == CombatState.PlayerTurn) {
            Debug.LogError("[CombatMotion] The enemy turn finished playing but the screen still does not show the player turn.");
        }

        foreach (Image image in hud.GetComponentsInChildren<Image>()) {
            Vector2 expected;
            if (image.name == "PlayerDoll_Image") {
                expected = new Vector2(0f, 54f);
            } else if (image.name == "EnemySprite_Image") {
                expected = HUDController.EnemySpriteRestPosition(image);
            } else {
                continue;
            }

            if ((image.rectTransform.anchoredPosition - expected).sqrMagnitude > 0.25f) {
                Debug.LogError($"[CombatMotion] {image.name} rests at {image.rectTransform.anchoredPosition}, not at its layout {expected}.");
            }
        }

        RectTransform stage = hud.transform.Find("StageRoot") as RectTransform;
        if (stage != null && stage.anchoredPosition.sqrMagnitude > 0.25f) {
            Debug.LogError($"[CombatMotion] The stage stayed shaken at {stage.anchoredPosition}.");
        }

        if (stage != null && Mathf.Abs(stage.localScale.x - 1f) > 0.001f) {
            Debug.LogError($"[CombatMotion] The stage stayed pushed in at scale {stage.localScale.x:0.000}.");
        }

        if (dollPoses) {
            bool zeroHit = director.PlayedBeats.Exists(b => b.Kind == CombatBeatKind.Hit && b.Target is DollFighter);
            if (zeroHit && !director.PosesShown.Contains(CombatPose.Hurt) && !director.PosesShown.Contains(CombatPose.Guard)) {
                Debug.LogError($"[CombatMotion] Zero was hit but showed neither hurt nor guard (poses: {string.Join(",", director.PosesShown)}).");
            }

            Image doll = hud.transform.Find("StageRoot/PlayerStageRoot/PlayerDoll_Image")?.GetComponent<Image>();
            VisualAssetService.TryGetSprite(CombatMotionDirector.PoseVisualID("doll_zero_combat", CombatPose.Idle), out Sprite stance);
            if (doll != null && combat.PlayerFaction.Fighters[0].RuntimeHP > 0 && doll.sprite != stance) {
                Debug.LogError($"[CombatMotion] After the turn Zero shows {doll.sprite?.name}, not her combat stance.");
            }
        }

        int zeroHits = director.PlayedBeats.FindAll(b => b.Kind == CombatBeatKind.Hit && b.Target is DollFighter).Count;
        Debug.Log($"[CombatMotion] {strikeTimes.Count} enemy action(s) over {turnSeconds:0.00}s, {director.PopupsSpawned} popup(s), lunge {director.MaxLungeOffset:0}px, Zero hit {zeroHits}x, poses [{string.Join(",", director.PosesShown)}].");
    }
}
