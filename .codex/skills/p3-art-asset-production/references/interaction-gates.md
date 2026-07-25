# Interaction and escalation gates

## Interactive mode pauses

Pause only when at least one applies:

1. Two valid candidates represent different art directions rather than different quality levels.
2. A first-time core anchor is about to be fixed: character master, scene style anchor, first UI skin family, key narrative CG, or promotional image.
3. The requested action would replace an already integrated shared asset and advance permission was not granted.
4. Candidate confidence is below threshold or identity/style cannot be judged reliably.
5. Active facts conflict or the requested direction changes a locked fact.
6. A character portrait set would lock or replace a shared identity/presentation anchor, or set members pass individually but represent incompatible character directions.

Routine icons, derived assets, high-confidence portrait members consistent with locked facts, and explicitly authorized same-VisualID replacements should not pause unnecessarily.

## Question format

Provide:

- current state and rounds completed;
- contact sheet or relevant evidence;
- 2-3 concrete options;
- recommended option first;
- score and fact-based reason;
- material tradeoff/risk;
- exact state to resume from.

Do not ask “你喜欢哪张？” without analysis.

## Pause record

```json
{
  "state": "decision_required",
  "decision_id": "choose_direction",
  "recommended_option": "candidate_a",
  "options": [],
  "evidence": [],
  "resume_from": "SELECTION_DECISION"
}
```

## Auto mode

Auto mode does not pause for subjective ties when a conservative fact-aligned choice exists. It still stops for contradictory facts, unsafe overwrite/path changes, missing authority, hard-gate failure with no valid candidate, credentials with no usable fallback, or legal/IP ambiguity that cannot be resolved from project facts.
