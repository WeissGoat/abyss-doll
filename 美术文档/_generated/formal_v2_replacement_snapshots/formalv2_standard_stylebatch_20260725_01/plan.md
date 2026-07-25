# Formal V2 主动迭代计划

> This is a generated replacement plan, not a second Manifest or progress table.

- GeneratedAt: `2026-07-25T23:50:51+08:00`
- BatchID: `formalv2_standard_stylebatch_20260725_01`
- Action: `visual_v2_replace`
- Provider: `agent_selected`
- Method: `agent_selected`
- Extracted: `2`
- Planned: `2`
- Prompt ready: `2`
- Asset classes: `{"background": 1, "icon": 1}`

## Rules

- Keep `VisualID`, `ApprovedPath`, `DisplaySpec`, `.meta`, and GUID unchanged.
- Choose the provider and generation method per run; this plan intentionally does not hard-code either.
- Publish a new numeric `processed/<n>` round and stop on failed or decision-required rounds.
- Do not sync Approved from this plan automatically.

## Items

| Order | VisualID | Class | Domain | Type | Prompt | Current | ApprovedPath |
|---:|---|---|---|---|---|---|---|
| 1 | `bg_combat_abyss` | background | background | background | ready | approved | UnityClient/Assets/Art/Approved/Backgrounds/Combat/bg_combat_abyss.png |
| 2 | `ui_icon_warning` | icon | ui | icon | ready | approved | UnityClient/Assets/Art/Approved/UI/ui_icon_warning.png |
