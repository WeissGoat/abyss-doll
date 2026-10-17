using System;
using System.Collections.Generic;
using System.Linq;

// One step of a dive as the journal draws it: the node's icon and kind, and whether Zero got hurt there.
[Serializable]
public class DiveLogStep {
    public string Kind;
    public string IconID;
    public bool Hurt;
    public bool Boss;
    public bool Elite;
}

// What happened on one dive, for the journal page at the door and the night talk (docs/specs/2026-10-17-home-loop.md).
// Written by DiveLogService from the backend as the run goes; kept on the player (saved) as the journal.
[Serializable]
public class DiveLog {
    public int Day;
    public int LayerID;
    public bool Prologue;
    public bool Defeated;
    public string DefeatedBy;
    public int HPStart;
    public int HPEnd;
    public int HPMax;
    public int LowestHP;
    public int SANStart;
    public int SANEnd;
    public int SANMax;
    public List<DiveLogStep> Steps = new List<DiveLogStep>();
    public List<string> DefeatedEnemies = new List<string>();
    public string BossName;
    public bool BossDefeated;
    public bool EliteFought;
    public bool EliteBypassed;
    public int LeftBehind;
    public int BroughtOut;
    public int BroughtOutValue;
    public List<string> BroughtOutNames = new List<string>();
    public List<string> BroughtOutIDs = new List<string>();
    public int Lost;
    public List<string> LostNames = new List<string>();
    // Bond 3: she held on once against a lethal blow (one HP left).
    public bool HeldOn;
    // 精神好: she set out cared for and talked to; the first fight's first turn gets +1 AP (SpiritUsed once it has).
    public bool Spirited;
    public bool SpiritUsed;
    // A wish this dive fulfilled: what she says at the door instead.
    public string WishDoneLine;
    // The arrival page has been shown (it opens once, when Zero comes through the door).
    public bool Shown;
}

public enum DiveHighlight {
    Ordinary = 0,
    WishDone = 1,
    LeftBehind = 2,
    EliteBypassed = 3,
    BossDefeated = 4,
    NearlyFell = 5,
    Defeated = 6
}

public static class DiveLogService {
    public const int JournalLimit = 40;
    // A step counts as "hurt" when it cost at least this share of Zero's HP; the page marks it with a red cross.
    public const float HurtShare = 0.2f;
    public const float NearlyFellShare = 0.2f;

    private static DiveLog _current;
    private static int _hpBeforeStep;

    public static DiveLog Current => _current;

    // The page Finish just wrote, for the day's report (the night talk reads it).
    public static DiveLog LastFinished { get; private set; }

    public static void Begin(PlayerProfile player, int layerID, bool prologue) {
        DollEntity doll = player?.ActiveDoll;
        _current = new DiveLog {
            Day = player?.CurrentDay ?? 0,
            LayerID = layerID,
            Prologue = prologue,
            HPStart = doll?.Status.HP_Current ?? 0,
            HPMax = doll?.Status.HP_Max ?? 0,
            LowestHP = doll?.Status.HP_Current ?? 0,
            SANStart = doll?.Status.SAN_Current ?? 0,
            SANMax = doll?.Status.SAN_Max ?? 0
        };
        _hpBeforeStep = _current.HPStart;
        _current.Spirited = !prologue && HomeEveningService.IsSpirited(player);
    }

    public static void RecordNode(NodeBase node, string iconID) {
        if (_current == null || node == null) {
            return;
        }

        CombatNode combat = node as CombatNode;
        _current.Steps.Add(new DiveLogStep {
            Kind = KindOf(node),
            IconID = iconID,
            Boss = combat != null && combat.MonsterIDs.Any(id => id != null && id.StartsWith("boss_")),
            Elite = combat != null && combat.MonsterIDs.Any(id => id != null && id.StartsWith("elite_"))
        });
        _hpBeforeStep = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.Status.HP_Current ?? _hpBeforeStep;
    }

    public static void RecordCombat(CombatOutcomeReport report) {
        if (_current == null || report == null) {
            return;
        }

        DiveLogStep step = _current.Steps.Count > 0 ? _current.Steps[_current.Steps.Count - 1] : null;
        int hp = report.ActiveDollHP;
        _current.LowestHP = Math.Min(_current.LowestHP, hp);
        // Whole HP points: 0.2f of 100 is a hair over 20 in float, so a 20-point hit would not count.
        if (step != null && _current.HPMax > 0 && _hpBeforeStep - hp >= Math.Max(1, (int)Math.Round(_current.HPMax * HurtShare))) {
            step.Hurt = true;
        }
        _hpBeforeStep = hp;

        if (step != null && step.Elite) {
            _current.EliteFought = true;
        }

        if (report.OutcomeType == CombatOutcomeType.Victory) {
            foreach (CombatOutcomeFighterSnapshot enemy in report.EnemyFighters) {
                if (enemy != null && !string.IsNullOrEmpty(enemy.Name)) {
                    _current.DefeatedEnemies.Add(enemy.Name);
                }
            }

            if (step != null && step.Boss) {
                _current.BossDefeated = true;
                _current.BossName = report.EnemyFighters.FirstOrDefault()?.Name;
            }
        } else if (report.OutcomeType == CombatOutcomeType.Defeat) {
            _current.DefeatedBy = report.EnemyFighters.FirstOrDefault(enemy => enemy != null && enemy.IsAlive)?.Name;
        }
    }

    public static void RecordLeftBehind(int count) {
        if (_current != null && count > 0) {
            _current.LeftBehind += count;
        }
    }

    // The run is settled: fill in what came home and keep the page in the player's journal.
    public static DiveLog Finish(PlayerProfile player, DungeonSettlementResult result, bool defeated, DungeonLayer layer) {
        DiveLog log = _current;
        _current = null;
        LastFinished = log;
        if (log == null || player == null) {
            return log;
        }

        DollEntity doll = player.ActiveDoll;
        log.Defeated = defeated;
        log.HPEnd = doll?.Status.HP_Current ?? 0;
        log.SANEnd = doll?.Status.SAN_Current ?? 0;
        log.LowestHP = Math.Min(log.LowestHP, log.HPEnd);
        log.BroughtOut = result?.BroughtOutCount ?? 0;
        log.BroughtOutValue = result?.BroughtOutEstimatedValue ?? 0;
        log.BroughtOutNames = result?.BroughtOutNames != null ? new List<string>(result.BroughtOutNames) : new List<string>();
        log.BroughtOutIDs = result?.BroughtOutIDs != null ? new List<string>(result.BroughtOutIDs) : new List<string>();
        log.Lost = result?.LostCount ?? 0;
        log.LostNames = result?.LostNames != null ? new List<string>(result.LostNames) : new List<string>();
        log.EliteBypassed = !log.EliteFought && BypassedAnElite(layer);
        log.Shown = log.Prologue;

        player.DiveJournal = player.DiveJournal ?? new List<DiveLog>();
        player.DiveJournal.Add(log);
        if (player.DiveJournal.Count > JournalLimit) {
            player.DiveJournal.RemoveAt(0);
        }

        return log;
    }

    // The page waiting at the door: the latest dive, if its arrival has not been shown yet.
    public static DiveLog PendingArrival(PlayerProfile player) {
        List<DiveLog> journal = player?.DiveJournal;
        DiveLog last = journal != null && journal.Count > 0 ? journal[journal.Count - 1] : null;
        return last != null && !last.Shown ? last : null;
    }

    public static DiveHighlight Highlight(DiveLog log) {
        if (log == null) {
            return DiveHighlight.Ordinary;
        }

        if (log.Defeated) {
            return DiveHighlight.Defeated;
        }

        if (log.HPMax > 0 && log.LowestHP > 0 && log.LowestHP < log.HPMax * NearlyFellShare) {
            return DiveHighlight.NearlyFell;
        }

        if (log.BossDefeated) {
            return DiveHighlight.BossDefeated;
        }

        if (log.EliteBypassed) {
            return DiveHighlight.EliteBypassed;
        }

        if (log.LeftBehind > 0) {
            return DiveHighlight.LeftBehind;
        }

        return DiveHighlight.Ordinary;
    }

    // Facts a line can ask for (Home/zero_lines_*.json "Tags"): the highlight plus a few plain observations.
    public static HashSet<string> Facts(DiveLog log) {
        HashSet<string> facts = new HashSet<string>();
        if (log == null) {
            return facts;
        }

        facts.Add(HighlightTag(Highlight(log)));
        facts.Add("layer" + log.LayerID);
        if (log.BroughtOut == 0) {
            facts.Add("empty_handed");
        } else if (log.BroughtOutValue >= 500) {
            facts.Add("good_haul");
        }

        if (log.HPMax > 0 && log.HPEnd >= log.HPMax) {
            facts.Add("unhurt");
        }

        if (log.HeldOn) {
            facts.Add("held_on");
        }

        return facts;
    }

    public static string HighlightTag(DiveHighlight highlight) {
        switch (highlight) {
            case DiveHighlight.Defeated: return "defeated";
            case DiveHighlight.NearlyFell: return "nearly_fell";
            case DiveHighlight.BossDefeated: return "boss_defeated";
            case DiveHighlight.EliteBypassed: return "elite_bypassed";
            case DiveHighlight.LeftBehind: return "left_behind";
            case DiveHighlight.WishDone: return "wish_done";
            default: return "ordinary";
        }
    }

    public static string KindOf(NodeBase node) {
        if (node is CombatNode combat) {
            if (combat.MonsterIDs.Any(id => id != null && id.StartsWith("boss_"))) {
                return "首领";
            }

            return combat.MonsterIDs.Any(id => id != null && id.StartsWith("elite_")) ? "精英" : "战斗";
        }

        if (node is SafeRoomNode) return "安全屋";
        if (node is StairsNode) return "阶梯";
        if (node is TreasureNode) return "宝藏";
        if (node is RestStopNode) return "营地";
        if (node is EventNode) return "事件";
        if (node is HazardNode) return "危险";
        return "未知";
    }

    private static bool BypassedAnElite(DungeonLayer layer) {
        if (layer == null) {
            return false;
        }

        int furthestRow = -1;
        int eliteRow = int.MaxValue;
        foreach (NodeBase node in layer.NodeRows.SelectMany(row => row)) {
            if (node == null) {
                continue;
            }

            if (node.IsVisited) {
                furthestRow = Math.Max(furthestRow, node.MapRow);
            }

            if (node is CombatNode combat && combat.MonsterIDs.Any(id => id != null && id.StartsWith("elite_"))) {
                eliteRow = Math.Min(eliteRow, node.MapRow);
            }
        }

        return eliteRow != int.MaxValue && furthestRow > eliteRow;
    }
}
