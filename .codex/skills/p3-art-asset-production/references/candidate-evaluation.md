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

## Decision thresholds

- `>= 88`, no hard failure, and lead over second place `>= 5`: auto-select.
- `78-87`: repair the best candidate or generate two targeted variants.
- `< 78`: revise prompt/reference/provider; do not select.
- Top candidates within 5 points and representing materially different directions: use the interaction policy.

In `auto` mode, break close ties by: active-fact consistency, identity, semantic correctness, engineering safety, style, composition, then decoration.

## Review record

Record for every valid candidate:

```json
{
  "path": "processed/example.png",
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

