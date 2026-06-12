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
last_verified: 2026-06-13
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
- Latest ArtAcceptance available locally after art/UI direct runtime polish: `20260613_024312`, status `PASSED`, 21/21 captured.
- Latest Registry evidence: `EntryCount=278`, `MissingRequiredVisualIDs=0`, `missing_registry=0`, `missing_approved=0`, `missing_meta=0`.
- Current FormalV2 runtime status: `美术文档/_generated/FormalV2运行时验收状态.md`, gate=`no_program_integrate`; latest local proof is `ReportRunID=20260613_024312`.
- Runtime review priority queue: `美术文档/_generated/FormalV2运行时复验优先级清单.md`, current `review_queue=30`, all non-blocking watch items.
- Latest runtime screenshots for art review: `UnityClient/Logs/ArtAcceptance/latest/screenshots/`.
- Art-side registry action: the offline candidate `美术文档/_generated/VisualAssetRegistry.offline_candidate.asset` was copied into `UnityClient/Assets/Resources/VisualAssetRegistry.asset`, then ArtAcceptance was rerun successfully.

Art-side resource acceptance is complete for the current FormalV2 asset queue. Art/UI side has directly fixed the latest runtime screenshot contamination, P0 composition issues, modal isolation and dungeon map node/route integration. `20260613_024312` is the current FormalV2 runtime UI polish evidence: ArtAcceptance `PASSED`, 21/21 captured, `DataSourceSummary.RealGameplay=20`, `AcceptancePreview=1`, `FormalV1Template=0`. Manual screenshot review passes the current vertical-slice baseline. The shared workshop sub-panel treatment is still a quality iteration target, but it is no longer a FormalV2 runtime seal blocker.

## Current Evidence

1. `UnityClient/Logs/ArtAcceptance/latest/report.json`: `RunID=20260613_024312`, `Status=PASSED`, `Registry.EntryCount=278`, `MissingRequiredVisualIDs=[]`, warnings/errors=0, `DataSourceSummary.RealGameplay=20`, `AcceptancePreview=1`, `FormalV1Template=0`.
2. `美术文档/_generated/VisualAssetRegistry登记缺口清单.md/json`: `program_integrate=0`, `missing_registry=0`, `missing_approved=0`, `missing_meta=0`.
3. `美术文档/_generated/程序接入交接清单.md/json`: `program_integrate=0`, `add_capture=0`, `rerun_acceptance=0`.
4. Latest screenshots: `UnityClient/Logs/ArtAcceptance/latest/screenshots/`.
5. Latest screenshot directory: `UnityClient/Logs/ArtAcceptance/latest/screenshots/`.
6. `美术文档/09_运行时美术验收记录.md`: 2026-06-13 FormalV2 runtime UI direct polish evidence recorded; resource / Registry / tool gates pass, current vertical-slice baseline passes.

Follow-up now focuses on quality iteration, not registry or current seal blockers. `workshop_main`, `combat_hud`, `inventory_loot`, `settlement`, `dungeon_map`, modal panels and workshop sub-panels can serve as the current player-flow UI baseline. The shared sub-panel style can still be differentiated later by screen cluster.

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
- Latest ArtAcceptance no longer has old missing combat VisualID errors, cross-capture `ScenarioEventPanel_Runtime` pollution or `formal_v1_template` captures.
- Art-side manual judgment is now complete for `20260613_020257`: no new bulk asset generation is required; remaining issues are quality iteration items, not current runtime seal blockers.

## Pass Criteria

- Registry and ArtAcceptance no longer report missing currently required FormalV2 VisualIDs.
- Screenshots show approved assets rather than fallback/missing sprite for the handed-off VisualIDs.
- Backgrounds cover their containers without transparent holes or obvious stretching.
- Icons remain readable at their runtime display sizes.
- FormalV2 core composition is preserved: combat uses player-left/enemy-right stage, dungeon map centers route depth, workshop main feels like a calm home room, and town economy screens reduce button-pile density.

## Next Art Action

Do not start another bulk NovelAI pass. Continue direct art/UI runtime work in player-flow order: improve shared workshop sub-panels into more differentiated FormalV2 structures, then handle animation / VFX and small-size icon readability only when runtime screenshots prove a real issue. Single-VisualID replacement remains allowed only when the asset itself is weak after layout is fixed.

## 2026-06-13 Runtime UI Direct Polish

- Art/UI side directly handled the remaining runtime UI visual blockers in pure UGUI.
- `WorkshopFormalV1PanelController` now renders the 11 old-template screens through per-screen FormalV2 runtime structures: stronger backdrop, neutral cards, subtler outlines, lower-density text and real best-fit / wrap behavior.
- `WorkshopUIController` now gives `sell_panel` and `prosthetic_panel` independent modal isolation, solid runtime cards and real tinting for buttons.
- `DungeonMapUIController` now uses smaller map markers, shorter labels, depth-based node placement, route layering behind nodes and lower-saturation route / plate colors.
- Verification evidence: `dotnet build UnityClient/Assembly-CSharp.csproj --no-restore` passed with 0 warnings / 0 errors; `Validate-UIDesign.ps1` passed; latest ArtAcceptance `RunID=20260613_020257` passed with 21/21 captures, Registry 278, `MissingRequiredVisualIDs=0`, warnings/errors=0, `RealGameplay=21`, `FormalV1Template=0`.
- Art decision: FormalV2 runtime UI visual seal passes the current vertical-slice baseline. Shared workshop sub-panel differentiation remains a later quality iteration item.

## 2026-06-13 Runtime UI Direct Polish Follow-up

- Art/UI side directly refined `sell_panel` and `prosthetic_panel` in `WorkshopUIController`.
- `sell_panel` now reads as `Town Market`: a left-side market item list plus a right-side `Market Route` guidance panel. Item rows use fixed solid cards with separated icon, item/source text, value pill and staging pill.
- `prosthetic_panel` now reads as `Workshop Studio`: a left-side recipe list plus a right-side `Studio Bench` doll preview/status panel. Rows use fixed solid cards with prosthetic icon, readable cost text, state icon and action button.
- Recipe cost text now resolves item display names from `ConfigManager.Items` before falling back to config IDs, so runtime rows show `废旧齿轮` instead of `loot_gear_scrap`.
- Verification evidence: `dotnet build UnityClient/Assembly-CSharp.csproj --no-restore` passed with 0 warnings / 0 errors; `Validate-UIDesign.ps1` passed; `Validate-ArtGeneratedJson.ps1 -Strict` passed; `Invoke-P0Validation.ps1 -SkipUnity -SkipArtAcceptance:$false -ArtAcceptanceTimeoutSeconds 240` passed with ArtAcceptance `RunID=20260613_024312`, 21/21 captures, Registry 278, `MissingRequiredVisualIDs=0`, warnings/errors=0, `RealGameplay=20`, `AcceptancePreview=1`, `FormalV1Template=0`.
- Limitation: the P0 wrapper was run with `-SkipUnity`, so ConfigValidator and Unity smoke tests were intentionally skipped. ArtAcceptance was rerun and passed.
