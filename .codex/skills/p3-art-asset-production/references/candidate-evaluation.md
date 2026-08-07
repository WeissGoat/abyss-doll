# Candidate evaluation

## Deterministic hard gate

Reject before visual scoring when any of these is true:

- corrupt, empty, solid-color, or duplicate output;
- unrepairable dimensions/format/alpha;
- random text, signature, watermark, or baked UI copy;
- wrong subject count, asset type, or obvious identity;
- critical subject crop or safe-area violation;
- transparent-background contract cannot be met;
- forbidden content or direct contradiction with the Asset Contract;
- same-VisualID replacement would change path, `.meta`, GUID, DisplaySpec, or binding.

## Base scoring

Score valid candidates to 100:

| Dimension | Weight |
|---|---:|
| semantic/game recognition | 25 |
| style consistency | 20 |
| identity/subject consistency | 20 |
| composition and safe area | 15 |
| small-size readability | 10 |
| engineering usability | 10 |

Adjust weights by asset type:

- icon: increase recognition and small-size readability;
- character: increase identity and style;
- background: increase scene semantics, composition, and safe area;
- comic panel: increase narrative semantics, identity continuity, and reading composition;
- UI skin: increase stretchability, empty content area, and engineering usability.

## Character set consistency

For related `character_portrait_set` members, do not stop at isolated candidate scores. Compare the selected candidates against active character facts, design-layer anchors, their declared source relationships, and one another. Check at least:

- recognizable identity, face structure, hair, costume, and body proportions;
- head size, eye line, baseline, canvas position, and expected runtime crop;
- lighting, palette, rendering language, and outline treatment;
- whether the requested state is readable without unintended identity or direction change;
- whether rapid runtime switching produces visible jumps not required by the performance.

A member can pass its individual score and still fail set consistency. Return only the affected member to review or adjustment unless the shared anchor or art direction is invalid.

## Decision thresholds

- `>= 88`, no hard failure, and lead over second place `>= 5`: auto-select.
- `78-87`: repair the best candidate or generate two targeted variants.
- `< 78`: revise prompt/reference/provider; do not select.
- Top candidates within 5 points and representing materially different directions: use the interaction policy.

In `auto` mode, break close ties by: active-fact consistency, identity, semantic correctness, engineering safety, style, composition, then decoration.

## Technical evidence and replacement gates

The Agent may recommend a candidate, but the Registrar is the source of truth for deterministic technical status. It recomputes the review from the actual file and current Manifest Spec, then compares the submitted `technical_review_v2` payload and `ReviewFingerprint`. Handwritten `Status=passed` or a stale report is rejected. Character full-body negative space may produce the `high_occupied_bbox_transparency` warning; an actual enclosed transparent hole is the separate `unexpected_transparent_holes` hard failure.

Only an explicitly authorized heuristic override can change an automatic result. The current allow-list is `subject_outside_safe_canvas -> accept_as_warning`; decode, hash, dimensions, format, alpha-contract, and nine-slice structural failures remain hard failures. The effective decision records `AutomaticStatus` and `AppliedOverrides`.

For a same-VisualID replacement, compare the candidate against the execution-time `selected/` baseline, not a score copied from an older review. Require `SelectionMode=replacement`, matching baseline path and SHA, a strictly higher total score by at least `MinimumScoreDelta`, and no regression in each `ProtectedDimensions` entry. `-AllowSelectedOverwrite` authorizes the file copy only after those checks. Identical candidate and selected SHA returns `already_selected` without another copy.

For `character_portrait_set`, use `character_portrait_v3` instead of a generic score map. `Identity`, `Costume`, `Proportion`, `Framing`, `Technical`, and `TargetFit` each require an independent finding and evidence; the first five dimensions are mandatory protected dimensions. Missing six-dimensional baseline evidence fails closed as `replacement_baseline_review_required`. After the checks and immediately before overwrite, preserve the previous selected bytes and its review evidence in `replacement-baselines/<VisualID>/`.

Individual acceptance is not set acceptance. `Invoke-PortraitSetGate.ps1 -Phase Prepare` produces a current SHA-bound contact sheet and small-size strip. Finalize requires exact member SHA/group coverage and all group/cross-group checks; a new selected byte, group, or identity contract invalidates the prior `SetSnapshotFingerprint`.

## Review record

Record for every valid candidate:

```json
{
  "path": "processed/2/001.png",
  "hard_gate": "passed",
  "scores": {
    "semantic": 23,
    "style": 18,
    "identity": 20,
    "composition": 13,
    "small_size": 9,
    "engineering": 10,
    "total": 93
  },
  "risks": ["minor hand anatomy noise"],
  "recommended_action": "repair_local"
}
```

Use actual image inspection, contact sheets, small-size previews, and relevant identity/style anchors. Do not score from filenames or prompts alone.

The latest numeric round is authoritative. A single valid `passed` candidate may be resolved for downstream selection; multiple passed candidates require an explicit selection. A latest failed, decision-required, or legacy-unverified round blocks fallback to earlier rounds.
