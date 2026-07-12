---
name: p3-art-validation
description: Diagnose, capture, iterate, and externally review Project P3 runtime UI and art through registered Unity targets and allowlisted UGUI or Approved VisualID changes. Use for ArtAcceptance, runtime screenshots, UI polish, art iteration, and T0 visual seals; never use for full P0 or domain-rule mutation.
---
# P3 Art Validation

Read `agent_status/art.md`, select a Profile using [profile-routing.md](references/profile-routing.md), create an ArtRunID, check readiness, open only a registered target, capture before evidence, diagnose, optionally apply one typed allowlisted change, capture after evidence, collect Console delta, merge, then request external art review.

If a target is unreachable, stop visual mutation and hand off `art_blocked:target_screen_unreachable` to program. Follow [iteration-boundaries.md](references/iteration-boundaries.md).
