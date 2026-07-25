# Formal V2 主动迭代计划

> This is a generated replacement plan, not a second Manifest or progress table.

- GeneratedAt: `2026-07-19T16:45:45+08:00`
- BatchID: `formalv2_ui_button_primary_repair_20260719_01`
- Action: `visual_v2_replace`
- Provider: `agent_selected`
- Method: `agent_selected`
- Extracted: `1`
- Planned: `1`
- Prompt ready: `1`
- Asset classes: `{"ui_skin": 1}`

## Rules

- Keep `VisualID`, `ApprovedPath`, `DisplaySpec`, `.meta`, and GUID unchanged.
- Choose the provider and generation method per run; this plan intentionally does not hard-code either.
- Publish a new numeric `processed/<n>` round and stop on failed or decision-required rounds.
- Do not sync Approved from this plan automatically.

## Items

| Order | VisualID | Class | Domain | Type | Prompt | Current | ApprovedPath |
|---:|---|---|---|---|---|---|---|
| 1 | `ui_button_primary` | ui_skin | ui | button | ready | approved | UnityClient/Assets/Art/Approved/UI/ui_button_primary.png |
