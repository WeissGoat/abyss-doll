# Formal V2 主动迭代计划

> This is a generated replacement plan, not a second Manifest or progress table.

- GeneratedAt: `2026-07-24T01:37:07+08:00`
- BatchID: `formalv2_replacement_20260719_01`
- Action: `visual_v2_replace`
- Provider: `agent_selected`
- Method: `agent_selected`
- Extracted: `35`
- Planned: `32`
- Prompt ready: `32`
- Asset classes: `{"background": 5, "icon": 7, "standard_asset": 8, "ui_skin": 12}`

## Rules

- Keep `VisualID`, `ApprovedPath`, `DisplaySpec`, `.meta`, and GUID unchanged.
- Choose the provider and generation method per run; this plan intentionally does not hard-code either.
- Publish a new numeric `processed/<n>` round and stop on failed or decision-required rounds.
- Do not sync Approved from this plan automatically.

## Items

| Order | VisualID | Class | Domain | Type | Prompt | Current | ApprovedPath |
|---:|---|---|---|---|---|---|---|
| 1 | `bg_combat_abyss` | background | background | background | ready | approved | UnityClient/Assets/Art/Approved/Backgrounds/Combat/bg_combat_abyss.png |
| 2 | `bg_dungeon_map` | background | background | background | ready | approved | UnityClient/Assets/Art/Approved/Backgrounds/Dungeon/bg_dungeon_map.png |
| 3 | `bg_settlement_defeat` | background | background | background | ready | formal_ai_v2 | UnityClient/Assets/Art/Approved/Backgrounds/Settlement/bg_settlement_defeat.png |
| 4 | `bg_settlement_victory` | background | background | background | ready | formal_ai_v2 | UnityClient/Assets/Art/Approved/Backgrounds/Settlement/bg_settlement_victory.png |
| 5 | `bg_workshop_day` | background | background | background | ready | approved | UnityClient/Assets/Art/Approved/Backgrounds/Workshop/bg_workshop_day.png |
| 6 | `ui_button_danger` | ui_skin | ui | button | ready | approved | UnityClient/Assets/Art/Approved/UI/ui_button_danger.png |
| 7 | `ui_button_primary` | ui_skin | ui | button | ready | approved | UnityClient/Assets/Art/Approved/UI/ui_button_primary.png |
| 8 | `ui_button_secondary` | ui_skin | ui | button | ready | approved | UnityClient/Assets/Art/Approved/UI/ui_button_secondary.png |
| 9 | `ui_combat_feedback_hit` | standard_asset | ui | effect_overlay | ready | formal_ai_v2 | UnityClient/Assets/Art/Approved/UI/ui_combat_feedback_hit.png |
| 10 | `ui_combat_feedback_shield_break` | standard_asset | ui | effect_overlay | ready | formal_ai_v2 | UnityClient/Assets/Art/Approved/UI/ui_combat_feedback_shield_break.png |
| 11 | `ui_combat_grid_lock_marker` | standard_asset | ui | marker | ready | formal_ai_v2 | UnityClient/Assets/Art/Approved/UI/ui_combat_grid_lock_marker.png |
| 12 | `ui_combat_junk_preview_marker` | standard_asset | ui | marker | ready | formal_ai_v2 | UnityClient/Assets/Art/Approved/UI/ui_combat_junk_preview_marker.png |
| 13 | `ui_combat_status_bar_hp` | ui_skin | ui | bar | ready | approved | UnityClient/Assets/Art/Approved/UI/ui_combat_status_bar_hp.png |
| 14 | `ui_combat_status_bar_shield` | ui_skin | ui | bar | ready | approved | UnityClient/Assets/Art/Approved/UI/ui_combat_status_bar_shield.png |
| 15 | `ui_dungeon_node_plate` | standard_asset | ui | frame | ready | formal_ai_v2 | UnityClient/Assets/Art/Approved/UI/ui_dungeon_node_plate.png |
| 16 | `ui_dungeon_route_line` | standard_asset | ui | divider | ready | formal_ai_v2 | UnityClient/Assets/Art/Approved/UI/ui_dungeon_route_line.png |
| 17 | `ui_icon_equipped` | icon | ui | icon | ready | formal_ai_v2 | UnityClient/Assets/Art/Approved/UI/ui_icon_equipped.png |
| 18 | `ui_icon_faction` | icon | ui | icon | ready | formal_ai_v2 | UnityClient/Assets/Art/Approved/UI/ui_icon_faction.png |
| 19 | `ui_icon_locked` | icon | ui | icon | ready | formal_ai_v2 | UnityClient/Assets/Art/Approved/UI/ui_icon_locked.png |
| 20 | `ui_icon_money` | icon | ui | icon | ready | approved | UnityClient/Assets/Art/Approved/UI/ui_icon_money.png |
| 21 | `ui_icon_order` | icon | ui | icon | ready | formal_ai_v2 | UnityClient/Assets/Art/Approved/UI/ui_icon_order.png |
| 22 | `ui_icon_rumor` | icon | ui | icon | ready | formal_ai_v2 | UnityClient/Assets/Art/Approved/UI/ui_icon_rumor.png |
| 23 | `ui_icon_warning` | icon | ui | icon | ready | approved | UnityClient/Assets/Art/Approved/UI/ui_icon_warning.png |
| 24 | `ui_list_row_normal` | ui_skin | ui | panel | ready | formal_ai_v2 | UnityClient/Assets/Art/Approved/UI/ui_list_row_normal.png |
| 25 | `ui_list_row_selected` | ui_skin | ui | panel | ready | formal_ai_v2 | UnityClient/Assets/Art/Approved/UI/ui_list_row_selected.png |
| 26 | `ui_loot_drop_zone` | standard_asset | ui | panel | ready | approved | UnityClient/Assets/Art/Approved/UI/ui_loot_drop_zone.png |
| 27 | `ui_loot_pickup_panel` | ui_skin | ui | panel | ready | approved | UnityClient/Assets/Art/Approved/UI/ui_loot_pickup_panel.png |
| 28 | `ui_panel_info` | ui_skin | ui | panel | ready | approved | UnityClient/Assets/Art/Approved/UI/ui_panel_info.png |
| 29 | `ui_panel_main` | ui_skin | ui | panel | ready | formal_ai_v2 | UnityClient/Assets/Art/Approved/UI/ui_panel_main.png |
| 30 | `ui_settlement_defeat_panel` | ui_skin | ui | panel | ready | formal_ai_v2 | UnityClient/Assets/Art/Approved/UI/ui_settlement_defeat_panel.png |
| 31 | `ui_settlement_victory_panel` | ui_skin | ui | panel | ready | formal_ai_v2 | UnityClient/Assets/Art/Approved/UI/ui_settlement_victory_panel.png |
| 32 | `ui_title_divider` | standard_asset | ui | divider | ready | formal_ai_v2 | UnityClient/Assets/Art/Approved/UI/ui_title_divider.png |

## Skipped

- `ui_inventory_cell`: manifest_missing
- `ui_inventory_cell_selected`: manifest_missing
- `ui_modal_frame`: manifest_missing
