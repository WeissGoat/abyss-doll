// Where the prologue is, for the save (GDD_02 §5.3). The opening (black screen to the status card) is a few minutes of
// scenes and is not saved: quitting there starts the prologue again. From the half-open workshop on, a save continues
// at the stage it was made in: the half-open workshop, the first dive (a normal dive checkpoint), or the return home.
// Town autosaves wait until the stage is cleared, so a save made mid-prologue never comes back as a plain town day.
public static class PrologueProgress {
    public const string Opening = "Opening";
    public const string HalfOpen = "HalfOpen";
    public const string ReturnHome = "ReturnHome";

    public static bool InPrologue(PlayerProfile player) {
        return player != null && !string.IsNullOrEmpty(player.PrologueStage);
    }

    public static bool IsAt(PlayerProfile player, string stage) {
        return player != null && player.PrologueStage == stage;
    }

    // A save point can be written here (not during the opening).
    public static bool CanCheckpoint(PlayerProfile player) {
        return InPrologue(player) && player.PrologueStage != Opening;
    }

    public static void Reach(PlayerProfile player, string stage, NarrativeStateSnapshot narrative = null) {
        if (player == null) {
            return;
        }

        player.PrologueStage = stage;
        if (narrative != null) {
            player.PrologueNarrative = narrative;
        }
    }

    // The normal day loop opens.
    public static void Finish(PlayerProfile player) {
        if (player == null) {
            return;
        }

        player.PrologueStage = null;
        player.PrologueNarrative = null;
    }
}
