# Profile routing

| Intent | Profile | Unity evidence | External review |
|---|---|---|---|
| Focused runtime smoke or regression | `smoke_focus` | Console delta, smoke report | None by default |
| Runtime UI/art review | `art_runtime` | Console delta, ArtAcceptance report, screenshots | Art required |
| T0 seal candidate | `t0_seal` | Static gates, smoke, T0 report, screenshots | Art and director required |
| Full P0 automation | `p0_full` | Config report, Console delta, smoke, ArtAcceptance summary | As reported |

Do not combine Profiles to hide a failed required step. Start separate RunIDs when two Profiles are genuinely required.

Pass these merge defaults unless stronger evidence exists:

| Profile | OwnerValidation | ExternalReview |
|---|---|---|
| `smoke_focus` | `NotStarted` | `NotRequired` |
| `art_runtime` | `NotStarted` | `Required` |
| `t0_seal` | `NotStarted` | `Required` |
| `p0_full` | `NotStarted` | `NotRequired` unless the produced report requires review |
