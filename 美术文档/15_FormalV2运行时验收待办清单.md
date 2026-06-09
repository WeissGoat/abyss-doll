---
id: art_formal_v2_runtime_acceptance_checklist
title: FormalV2 Runtime Acceptance Checklist
type: art
role: 美术
domain: art_acceptance
status: active
source_of_truth: true
related:
  - 美术文档/README.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - 美术文档/09_运行时美术验收记录.md
  - 美术文档/_generated/程序接入交接清单.md
  - 美术文档/_generated/可接入素材清单.md
  - 美术文档/_generated/FormalV2运行时复验优先级清单.md
  - agent_status/art.md
  - agent_status/program.md
last_verified: 2026-06-09
update_rule: FormalV2 runtime acceptance gate, evidence, or review focus changes should update this file.
---
# FormalV2 Runtime Acceptance Checklist

> Generated/maintained by art agent for the FormalV2 full-asset iteration. This checklist is not a Unity integration source; it is the art-side review contract after program registration and ArtAcceptance rerun.

## Current Gate

- Latest generated handoff: `美术文档/_generated/程序接入交接清单.md`
- Current program queue: `program_integrate=87`
- Current missing art generation queue: `generate_needed=0`
- Current quality backlog: `technical_fix=0`, `visual_v2_replace=0`, `spec_review=0`
- Current requirement candidate scan: `new_candidate=0`, `deferred_candidate=29`, `ignored_candidate=26`
- Latest ArtAcceptance available locally: `20260606_230523`, status `FAILED`, older than current FormalV2 asset queue.
- Runtime review priority queue: `美术文档/_generated/FormalV2运行时复验优先级清单.md`, current `review_queue=30`, all non-blocking watch items.

Art-side runtime acceptance must wait until program registers the current 87 VisualIDs and reruns ArtAcceptance / VisualAsset checks.

## Required Program Evidence

1. `UnityClient/Logs/ArtAcceptance/latest/report.json` with a new RunID later than `20260606_230523`.
2. `UnityClient/Logs/ArtAcceptance/latest/registry_snapshot.json` showing the newly registered FormalV2 VisualIDs.
3. `UnityClient/Logs/ArtAcceptance/latest/screenshots/` refreshed for the 16 rerun screens in the handoff queue.
4. No `MissingRequiredVisualIDs` for the currently handed-off FormalV2 assets.

## Art Review Focus

| Area | Screens | Review Focus |
|---|---|---|
| Core combat flow | `combat_hud`, `inventory_loot`, `settlement` | Combat stage composition, enemy silhouettes, bottom inventory readability, reward scatter clarity, settlement outcome panels and background fit. |
| Dungeon navigation | `dungeon_map`, `layer_select`, `safe_room`, `stairs_room` | Map depth and node readability, background cover behavior, safe/stairs room opacity, route/node integration. |
| Workshop and growth | `workshop_main`, `maintenance_panel`, `prosthetic_panel`, `chassis_upgrade_panel`, `doll_room`, `doll_interaction` | Home-room calmness, workshop-studio panel framing, doll/prosthetic/chassis icon readability, memento display clarity. |
| Town economy | `shop_staging`, `business_settlement`, `daily_bill_report`, `order_board`, `rumor_board`, `faction_shop`, `sell_panel` | Town/shop background reuse, order/rumor/faction icon semantics, list-row density, low-clutter FormalV2 direction. |
| Event layer | `scenario_event` | Background/content separation and panel skin readability. |

## Known Watch Items

- `program_integrate=87` currently includes 23 item icons, 34 monster combat/portrait assets, 6 backgrounds, 7 mementos, 6 order icons, 8 rumor icons, and 3 combat feedback UI assets.
- The runtime review priority queue currently contains 30 watch items, mainly item icon semantics, monster portrait small-size readability, memento readability, order/rumor icon semantics, and two combat feedback overlays.
- Monster combat sprites and portraits passed static checks, but runtime scale and small-size readability still need screenshot review.
- Item/order/rumor icons passed static checks, but semantic distinction must be judged in actual list/grid usage.
- Existing latest ArtAcceptance failure still mentions old missing combat VisualIDs and missing combat HUD structure; do not use it as current FormalV2 pass/fail evidence after the 87 assets are registered.

## Pass Criteria

- Registry and ArtAcceptance no longer report missing currently required FormalV2 VisualIDs.
- Screenshots show approved assets rather than fallback/missing sprite for the handed-off VisualIDs.
- Backgrounds cover their containers without transparent holes or obvious stretching.
- Icons remain readable at their runtime display sizes.
- FormalV2 core composition is preserved: combat uses player-left/enemy-right stage, dungeon map centers route depth, workshop main feels like a calm home room, and town economy screens reduce button-pile density.

## Next Art Action

After program reruns ArtAcceptance, review `latest/contact_sheet.png`, per-screen screenshots, `report.json`, `registry_snapshot.json`, and `ui_snapshot.json`; then update `美术文档/09_运行时美术验收记录.md`, refresh generated queues, and create second-pass NovelAI replacement tasks only for VisualIDs proven weak in runtime screenshots.
