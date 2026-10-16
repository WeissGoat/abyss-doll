// The enemy turn resolves instantly, so the end-turn button is live again the moment the auto end (or a first click)
// fires. A click landing in that instant used to end the fresh player turn as well. Clicks within a short grace after a
// player turn starts are dropped.
public class EndTurnClickGuard {
    public const float GraceSeconds = 0.6f;

    private float _blockedUntil = float.NegativeInfinity;

    public void OnPlayerTurnStarted(float now) {
        _blockedUntil = now + GraceSeconds;
    }

    public bool AllowManualEnd(float now) {
        return now >= _blockedUntil;
    }
}
