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
last_verified: 2026-06-12
update_rule: FormalV2 runtime acceptance gate, evidence, or review focus changes should update this file.
---
# FormalV2 Runtime Acceptance Checklist

> Generated/maintained by art agent for the FormalV2 full-asset iteration. This checklist is not a Unity integration source; it is the art-side review contract after program registration and ArtAcceptance rerun.

## Current Gate

- Latest generated handoff: `美术文档/_generated/程序接入交接清单.md`
- Current program queue: `program_integrate=0`
- Current missing art generation queue: `generate_needed=0`
- Current quality backlog: `technical_fix=0`, `visual_v2_replace=0`, `spec_review=0`
- Current requirement candidate scan: `new_candidate=0`, `deferred_candidate=29`, `ignored_candidate=26`
- Latest ArtAcceptance available locally after art/UI direct runtime polish: `20260612_231356`, status `PASSED`, 21/21 captured.
- Latest Registry evidence: `EntryCount=278`, `MissingRequiredVisualIDs=0`, `missing_registry=0`, `missing_approved=0`, `missing_meta=0`.
- Current FormalV2 runtime status: `美术文档/_generated/FormalV2运行时验收状态.md`, gate=`no_program_integrate`. This means the asset registration queue is clear; it does not mean FormalV2 UI layout is visually accepted.
- Runtime review priority queue: `美术文档/_generated/FormalV2运行时复验优先级清单.md`, current `review_queue=30`, all non-blocking watch items.
- Latest runtime contact sheet for art review: `UnityClient/Logs/ArtAcceptance/latest/contact_sheet_formalv2_20260612_231356_codex.png`.
- Art-side registry action: the offline candidate `美术文档/_generated/VisualAssetRegistry.offline_candidate.asset` was copied into `UnityClient/Assets/Resources/VisualAssetRegistry.asset`, then ArtAcceptance was rerun successfully.

Art-side resource acceptance is complete for the current FormalV2 asset queue. Art/UI side has directly fixed the latest runtime screenshot contamination and P0 composition issues. Manual screenshot review of `20260612_231356` is complete: core FormalV2 P0 structure is **conditionally passed**, while full FormalV2 visual acceptance is **not sealed**. Remaining work is UI layout / hierarchy polish and selected background replacement, not resource registration.

## Current Evidence

1. `UnityClient/Logs/ArtAcceptance/latest/report.json`: `RunID=20260612_231356`, `Status=PASSED`, `Registry.EntryCount=278`, `MissingRequiredVisualIDs=[]`, warnings/errors=0.
2. `美术文档/_generated/VisualAssetRegistry登记缺口清单.md/json`: `program_integrate=0`, `missing_registry=0`, `missing_approved=0`, `missing_meta=0`.
3. `美术文档/_generated/程序接入交接清单.md/json`: `program_integrate=0`, `add_capture=0`, `rerun_acceptance=0`.
4. Latest screenshots: `UnityClient/Logs/ArtAcceptance/latest/screenshots/`.
5. Latest contact sheet: `UnityClient/Logs/ArtAcceptance/latest/contact_sheet_formalv2_20260612_231356_codex.png`.
6. `美术文档/09_运行时美术验收记录.md`: 2026-06-12 美术侧直接运行时精修验收已记录; resource / Registry / tool gates pass, core FormalV2 P0 structure conditionally passes, full FormalV2 visual seal remains pending.

Follow-up now focuses on art/UI direct runtime polish, not registry. `workshop_main`, `combat_hud`, `inventory_loot`, and `settlement` can serve as the current player-flow UI base; `dungeon_map`, modal hierarchy, old FormalV1 template replacement, and `bg_workshop_day` background quality remain open.

## Art Review Focus

| Area | Screens | Review Focus |
|---|---|---|
| Core combat flow | `combat_hud`, `inventory_loot`, `settlement` | Combat stage composition, enemy silhouettes, bottom inventory readability, reward scatter clarity, settlement outcome panels and background fit. |
| Dungeon navigation | `dungeon_map`, `layer_select`, `safe_room`, `stairs_room` | Map depth and node readability, background cover behavior, safe/stairs room opacity, route/node integration. |
| Workshop and growth | `workshop_main`, `maintenance_panel`, `prosthetic_panel`, `chassis_upgrade_panel`, `doll_room`, `doll_interaction` | Home-room calmness, workshop-studio panel framing, doll/prosthetic/chassis icon readability, memento display clarity. |
| Town economy | `shop_staging`, `business_settlement`, `daily_bill_report`, `order_board`, `rumor_board`, `faction_shop`, `sell_panel` | Town/shop background reuse, order/rumor/faction icon semantics, list-row density, low-clutter FormalV2 direction. |
| Event layer | `scenario_event` | Background/content separation and panel skin readability. |

## Known Watch Items

- The previous `program_integrate=87` queue has been registered and is now clear.
- The runtime review priority queue currently contains 30 watch items, mainly item icon semantics, monster portrait small-size readability, memento readability, order/rumor icon semantics, and two combat feedback overlays.
- Monster combat sprites and portraits passed static checks, but runtime scale and small-size readability still need screenshot review.
- Item/order/rumor icons passed static checks, but semantic distinction must be judged in actual list/grid usage.
- Latest ArtAcceptance no longer has old missing combat VisualID errors or cross-capture `ScenarioEventPanel_Runtime` pollution.
- Art-side manual judgment is now complete for `20260612_231356`: no new bulk asset generation is required; remaining issues are P1/P2 UI polish and selected same-path background replacement.

## Pass Criteria

- Registry and ArtAcceptance no longer report missing currently required FormalV2 VisualIDs.
- Screenshots show approved assets rather than fallback/missing sprite for the handed-off VisualIDs.
- Backgrounds cover their containers without transparent holes or obvious stretching.
- Icons remain readable at their runtime display sizes.
- FormalV2 core composition is preserved: combat uses player-left/enemy-right stage, dungeon map centers route depth, workshop main feels like a calm home room, and town economy screens reduce button-pile density.

## Next Art Action

Do not start another bulk NovelAI pass. Continue direct art/UI runtime polish on `dungeon_map`, modal panels and old FormalV1 template screens. Only create single-VisualID replacement tasks when a runtime screenshot proves the asset itself is weak after layout is fixed.
