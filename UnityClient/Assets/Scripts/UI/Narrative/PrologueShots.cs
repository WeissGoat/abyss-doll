using System.Collections.Generic;

// The prologue (T0) told as full-screen pictures, one per line (2026-10-25, replacing the comic pages that laid several
// 16:9 CGs out on a black page): each prologue visual lists the shot for its first, second, ... line; later lines keep
// the last shot. Visuals not listed here keep their old staging.
public static class PrologueShots {
    private const string Prefix = "cg_t0_01a_";

    private static readonly Dictionary<string, string[]> Shots = new Dictionary<string, string[]> {
        // 又到早上了。
        ["cg_t0_01a_black_wake"] = new[] { "p01_wake_pov" },
        // 他们连坏掉的东西也收。/ 不，他们最喜欢收坏掉的东西。
        ["cg_t0_01a_debt_notice"] = new[] { "p02_panel02_debt_notice_door", "p03_panel01_debt_notice_close" },
        // 父亲写字从不写“也许”。/ 所以浅层至少还有一点办法。
        ["cg_t0_01a_repair_note"] = new[] { "p04_panel01_repair_note_close", "p04_panel02_shallow_gate_glow" },
        // 卖掉它，够一天。/ 用掉它，也许够她醒一次。
        ["cg_t0_01a_core_shard"] = new[] { "p04_panel03_core_shard_box", "p02_panel03_no0_hand_cloth" },
        // 没有第二台了。/ 也没有第二个早上。/ (启动零号)
        ["cg_t0_01a_find_no0"] = new[] { "p05_panel01_broken_doll_parts", "p05_panel02_no0_half_reveal", "p05_panel03_no0_core_dim" },
        ["vfx_no0_core_start"] = new[] { "p06_panel02_core_insert", "p06_panel03_core_wake" },
        // Her first words, the status card and the wipe.
        ["stand_no0_weak_sitting"] = new[] { "p06_panel01_no0_close" },
        ["ui_status_card_prologue"] = new[] { "p06_panel01_no0_close" }
    };

    public static bool Has(string visualID) {
        return visualID != null && Shots.ContainsKey(visualID);
    }

    // The shot for this line of the visual: a new visual starts at its first shot, the same visual again moves on one.
    public static string Next(string visualID, ref string currentVisualID, ref int line) {
        if (!Has(visualID)) {
            return null;
        }

        if (visualID == currentVisualID) {
            line++;
        } else {
            currentVisualID = visualID;
            line = 0;
        }

        string[] shots = Shots[visualID];
        return Prefix + shots[line < shots.Length ? line : shots.Length - 1];
    }
}
