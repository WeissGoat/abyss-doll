# Formal V2 Prompt Readiness

GeneratedAt: `2026-06-09T09:00:42+08:00`

## Summary

- ProgramIntegrateVisuals: `77`
- PromptReady: `77`
- PromptBlocked: `0`
- GenericPromptRemaining: `0`
- CjkPromptViolations: `0`
- DomainCounts: `{'item': 13, 'monster': 34, 'background': 6, 'ui': 3, 'memento': 7, 'order': 6, 'rumor': 8}`

## Rerun Commands

These commands are dry-run verified in the current toolchain shape only after `Run-ArtGeneration.ps1 -DryRun` is executed separately. Use them after setting `NAI_ACCESS_TOKEN`; keep `-Concurrency 1 -DelaySeconds 1`.

### background

```powershell
$runArtGeneration = Get-ChildItem -Path .\tools -Recurse -Filter Run-ArtGeneration.ps1 | Select-Object -First 1 -ExpandProperty FullName
$artToolDir = Split-Path -Parent $runArtGeneration
$config = Join-Path $artToolDir 'ai_image_gateway.local.yaml'
& $runArtGeneration -Config $config -Provider novelai -Status approved -VisualID bg_dungeon_layer_3,bg_workshop_home_room,bg_workshop_studio,bg_daily_bill,bg_shop_staging,bg_town_shop -Variants 1 -Concurrency 1 -DelaySeconds 1 -BatchID nai_formalv2_program_integrate_prompt_specific_20260609_01_background -PreserveStatus
```

### item

```powershell
$runArtGeneration = Get-ChildItem -Path .\tools -Recurse -Filter Run-ArtGeneration.ps1 | Select-Object -First 1 -ExpandProperty FullName
$artToolDir = Split-Path -Parent $runArtGeneration
$config = Join-Path $artToolDir 'ai_image_gateway.local.yaml'
& $runArtGeneration -Config $config -Provider novelai -Status approved -VisualID item_con_anchor_charm_icon,item_con_purifying_salt_icon,item_con_solvent_spray_icon,item_con_stabilizer_ampoule_icon,item_gear_chain_hook_icon,item_gear_corroded_bulwark_icon,item_gear_mycelium_cloak_icon,item_gear_spore_lance_icon,item_gear_vein_sickle_icon,item_loot_acid_gland_icon,item_loot_corroded_nerve_icon,item_loot_crystal_scale_icon,item_loot_living_mycelium_icon -Variants 1 -Concurrency 1 -DelaySeconds 1 -BatchID nai_formalv2_program_integrate_prompt_specific_20260609_01_item -PreserveStatus
```

### memento

```powershell
$runArtGeneration = Get-ChildItem -Path .\tools -Recurse -Filter Run-ArtGeneration.ps1 | Select-Object -First 1 -ExpandProperty FullName
$artToolDir = Split-Path -Parent $runArtGeneration
$config = Join-Path $artToolDir 'ai_image_gateway.local.yaml'
& $runArtGeneration -Config $config -Provider novelai -Status approved -VisualID memento_blackmarket_letter,memento_debt_shadow_window,memento_first_return_tag,memento_low_san_blanket,memento_pawn_empty_tag,memento_truth_red_thread,memento_wall_crack_first_defeat -Variants 1 -Concurrency 1 -DelaySeconds 1 -BatchID nai_formalv2_program_integrate_prompt_specific_20260609_01_memento -PreserveStatus
```

### monster

```powershell
$runArtGeneration = Get-ChildItem -Path .\tools -Recurse -Filter Run-ArtGeneration.ps1 | Select-Object -First 1 -ExpandProperty FullName
$artToolDir = Split-Path -Parent $runArtGeneration
$config = Join-Path $artToolDir 'ai_image_gateway.local.yaml'
& $runArtGeneration -Config $config -Provider novelai -Status approved -VisualID monster_boss_gatekeeper_mk1_combat,monster_boss_mycelium_oracle_combat,monster_boss_spore_foundry_combat,monster_elite_crystal_bulwark_combat,monster_elite_spore_matriarch_combat,monster_elite_vein_knight_combat,monster_mob_acid_slime_mature_combat,monster_mob_crystal_guard_combat,monster_mob_echo_pilgrim_combat,monster_mob_lost_miner_echo_combat,monster_mob_mycelium_crawler_combat,monster_mob_nerve_midge_swarm_combat,monster_mob_rust_cultivator_combat,monster_mob_rust_hound_combat,monster_mob_shell_grafter_combat,monster_mob_soul_midge_swarm_combat,monster_mob_spore_archer_combat,monster_boss_gatekeeper_mk1_portrait,monster_boss_mycelium_oracle_portrait,monster_boss_spore_foundry_portrait,monster_elite_crystal_bulwark_portrait,monster_elite_spore_matriarch_portrait,monster_elite_vein_knight_portrait,monster_mob_acid_slime_mature_portrait,monster_mob_crystal_guard_portrait,monster_mob_echo_pilgrim_portrait,monster_mob_lost_miner_echo_portrait,monster_mob_mycelium_crawler_portrait,monster_mob_nerve_midge_swarm_portrait,monster_mob_rust_cultivator_portrait,monster_mob_rust_hound_portrait,monster_mob_shell_grafter_portrait,monster_mob_soul_midge_swarm_portrait,monster_mob_spore_archer_portrait -Variants 1 -Concurrency 1 -DelaySeconds 1 -BatchID nai_formalv2_program_integrate_prompt_specific_20260609_01_monster -PreserveStatus
```

### order

```powershell
$runArtGeneration = Get-ChildItem -Path .\tools -Recurse -Filter Run-ArtGeneration.ps1 | Select-Object -First 1 -ExpandProperty FullName
$artToolDir = Split-Path -Parent $runArtGeneration
$config = Join-Path $artToolDir 'ai_image_gateway.local.yaml'
& $runArtGeneration -Config $config -Provider novelai -Status approved -VisualID order_black_contested_core_buyout_icon,order_blackmarket_relic_box_icon,order_crystal_scale_batch_icon,order_mage_oracle_sample_icon,order_spore_cage_procurement_icon,order_workshop_spore_core_frame_icon -Variants 1 -Concurrency 1 -DelaySeconds 1 -BatchID nai_formalv2_program_integrate_prompt_specific_20260609_01_order -PreserveStatus
```

### rumor

```powershell
$runArtGeneration = Get-ChildItem -Path .\tools -Recurse -Filter Run-ArtGeneration.ps1 | Select-Object -First 1 -ExpandProperty FullName
$artToolDir = Split-Path -Parent $runArtGeneration
$config = Join-Path $artToolDir 'ai_image_gateway.local.yaml'
& $runArtGeneration -Config $config -Provider novelai -Status approved -VisualID rumor_acid_gland_shortage_icon,rumor_crystal_scale_contract_icon,rumor_filter_shortage_icon,rumor_living_mycelium_shortage_icon,rumor_miner_lamps_icon,rumor_old_blades_icon,rumor_oracle_fossil_collector_icon,rumor_spore_amber_jeweler_icon -Variants 1 -Concurrency 1 -DelaySeconds 1 -BatchID nai_formalv2_program_integrate_prompt_specific_20260609_01_rumor -PreserveStatus
```

### ui

```powershell
$runArtGeneration = Get-ChildItem -Path .\tools -Recurse -Filter Run-ArtGeneration.ps1 | Select-Object -First 1 -ExpandProperty FullName
$artToolDir = Split-Path -Parent $runArtGeneration
$config = Join-Path $artToolDir 'ai_image_gateway.local.yaml'
& $runArtGeneration -Config $config -Provider novelai -Status approved -VisualID ui_combat_feedback_echo_fade,ui_combat_feedback_scrap_break,ui_combat_feedback_slime_pop -Variants 1 -Concurrency 1 -DelaySeconds 1 -BatchID nai_formalv2_program_integrate_prompt_specific_20260609_01_ui -PreserveStatus
```

## Items

| Ready | Priority | VisualID | Domain | Type | ConfigID | Issues | Warnings | PromptPreview |
|---|---|---|---|---|---|---|---|---|
| yes | P0 | `item_con_anchor_charm_icon` | item | icon | con_anchor_charm | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, game item icon, anchor charm con |
| yes | P0 | `item_con_purifying_salt_icon` | item | icon | con_purifying_salt | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, game item icon, purifying salt i |
| yes | P0 | `item_con_solvent_spray_icon` | item | icon | con_solvent_spray | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, game item icon, solvent spray co |
| yes | P0 | `item_con_stabilizer_ampoule_icon` | item | icon | con_stabilizer_ampoule | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, game item icon, stabilizer ampou |
| yes | P0 | `item_gear_chain_hook_icon` | item | icon | gear_chain_hook | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, game item icon, chain hook gear  |
| yes | P0 | `item_gear_corroded_bulwark_icon` | item | icon | gear_corroded_bulwark | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, game item icon, corroded bulwark |
| yes | P0 | `item_gear_mycelium_cloak_icon` | item | icon | gear_mycelium_cloak | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, game item icon, mycelium cloak i |
| yes | P0 | `item_gear_spore_lance_icon` | item | icon | gear_spore_lance | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, game item icon, spore lance weap |
| yes | P0 | `item_gear_vein_sickle_icon` | item | icon | gear_vein_sickle | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, game item icon, vein sickle weap |
| yes | P0 | `item_loot_acid_gland_icon` | item | icon | loot_acid_gland | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, game item icon, acid gland loot  |
| yes | P0 | `item_loot_corroded_nerve_icon` | item | icon | loot_corroded_nerve | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, game item icon, corroded nerve l |
| yes | P0 | `item_loot_crystal_scale_icon` | item | icon | loot_crystal_scale | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, game item icon, crystal scale lo |
| yes | P0 | `item_loot_living_mycelium_icon` | item | icon | loot_living_mycelium | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, game item icon, living mycelium  |
| yes | P0 | `monster_boss_gatekeeper_mk1_combat` | monster | combat_sprite | boss_gatekeeper_mk1 | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, full-body creature illustration, |
| yes | P0 | `monster_boss_mycelium_oracle_combat` | monster | combat_sprite | boss_mycelium_oracle | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, full-body creature illustration, |
| yes | P0 | `monster_boss_spore_foundry_combat` | monster | combat_sprite | boss_spore_foundry | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, full-body creature illustration, |
| yes | P0 | `monster_elite_crystal_bulwark_combat` | monster | combat_sprite | elite_crystal_bulwark | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, full-body creature illustration, |
| yes | P0 | `monster_elite_spore_matriarch_combat` | monster | combat_sprite | elite_spore_matriarch | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, full-body creature illustration, |
| yes | P0 | `monster_elite_vein_knight_combat` | monster | combat_sprite | elite_vein_knight | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, full-body creature illustration, |
| yes | P0 | `monster_mob_acid_slime_mature_combat` | monster | combat_sprite | mob_acid_slime_mature | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, full-body creature illustration, |
| yes | P0 | `monster_mob_crystal_guard_combat` | monster | combat_sprite | mob_crystal_guard | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, full-body creature illustration, |
| yes | P0 | `monster_mob_echo_pilgrim_combat` | monster | combat_sprite | mob_echo_pilgrim | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, full-body creature illustration, |
| yes | P0 | `monster_mob_lost_miner_echo_combat` | monster | combat_sprite | mob_lost_miner_echo | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, full-body creature illustration, |
| yes | P0 | `monster_mob_mycelium_crawler_combat` | monster | combat_sprite | mob_mycelium_crawler | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, full-body creature illustration, |
| yes | P0 | `monster_mob_nerve_midge_swarm_combat` | monster | combat_sprite | mob_nerve_midge_swarm | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, full-body creature illustration, |
| yes | P0 | `monster_mob_rust_cultivator_combat` | monster | combat_sprite | mob_rust_cultivator | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, full-body creature illustration, |
| yes | P0 | `monster_mob_rust_hound_combat` | monster | combat_sprite | mob_rust_hound | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, full-body creature illustration, |
| yes | P0 | `monster_mob_shell_grafter_combat` | monster | combat_sprite | mob_shell_grafter | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, full-body creature illustration, |
| yes | P0 | `monster_mob_soul_midge_swarm_combat` | monster | combat_sprite | mob_soul_midge_swarm | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, full-body creature illustration, |
| yes | P0 | `monster_mob_spore_archer_combat` | monster | combat_sprite | mob_spore_archer | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, full-body creature illustration, |
| yes | P0 | `monster_boss_gatekeeper_mk1_portrait` | monster | portrait | boss_gatekeeper_mk1 | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, creature portrait, massive mecha |
| yes | P0 | `monster_boss_mycelium_oracle_portrait` | monster | portrait | boss_mycelium_oracle | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, creature portrait, mycelium orac |
| yes | P0 | `monster_boss_spore_foundry_portrait` | monster | portrait | boss_spore_foundry | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, creature portrait, large spore-f |
| yes | P0 | `monster_elite_crystal_bulwark_portrait` | monster | portrait | elite_crystal_bulwark | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, creature portrait, elite crystal |
| yes | P0 | `monster_elite_spore_matriarch_portrait` | monster | portrait | elite_spore_matriarch | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, creature portrait, elite spore m |
| yes | P0 | `monster_elite_vein_knight_portrait` | monster | portrait | elite_vein_knight | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, creature portrait, elite vein kn |
| yes | P0 | `monster_mob_acid_slime_mature_portrait` | monster | portrait | mob_acid_slime_mature | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, creature portrait, mature acid s |
| yes | P0 | `monster_mob_crystal_guard_portrait` | monster | portrait | mob_crystal_guard | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, creature portrait, crystal guard |
| yes | P0 | `monster_mob_echo_pilgrim_portrait` | monster | portrait | mob_echo_pilgrim | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, creature portrait, echo pilgrim  |
| yes | P0 | `monster_mob_lost_miner_echo_portrait` | monster | portrait | mob_lost_miner_echo | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, creature portrait, lost miner ec |
| yes | P0 | `monster_mob_mycelium_crawler_portrait` | monster | portrait | mob_mycelium_crawler | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, creature portrait, mycelium craw |
| yes | P0 | `monster_mob_nerve_midge_swarm_portrait` | monster | portrait | mob_nerve_midge_swarm | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, creature portrait, nerve midge s |
| yes | P0 | `monster_mob_rust_cultivator_portrait` | monster | portrait | mob_rust_cultivator | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, creature portrait, rust cultivat |
| yes | P0 | `monster_mob_rust_hound_portrait` | monster | portrait | mob_rust_hound | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, creature portrait, rust hound mo |
| yes | P0 | `monster_mob_shell_grafter_portrait` | monster | portrait | mob_shell_grafter | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, creature portrait, shell grafter |
| yes | P0 | `monster_mob_soul_midge_swarm_portrait` | monster | portrait | mob_soul_midge_swarm | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, creature portrait, soul midge sw |
| yes | P0 | `monster_mob_spore_archer_portrait` | monster | portrait | mob_spore_archer | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, creature portrait, spore archer  |
| yes | P1 | `bg_dungeon_layer_3` | background | background | layer_3 | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, environment background, vast und |
| yes | P1 | `bg_workshop_home_room` | background | background | workshop_home_room | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, environment background, peaceful |
| yes | P1 | `bg_workshop_studio` | background | background | workshop_studio | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, environment background, mechanic |
| yes | P1 | `ui_combat_feedback_echo_fade` | ui | icon | combat_feedback_echo_fade | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, small 2D interface icon sprite,  |
| yes | P1 | `ui_combat_feedback_scrap_break` | ui | icon | combat_feedback_scrap_break | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, small 2D interface icon sprite,  |
| yes | P1 | `ui_combat_feedback_slime_pop` | ui | icon | combat_feedback_slime_pop | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, small 2D interface icon sprite,  |
| yes | P2 | `bg_daily_bill` | background | background | daily_bill | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, environment background, quiet to |
| yes | P2 | `bg_shop_staging` | background | background | shop_staging | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, environment background, small to |
| yes | P2 | `bg_town_shop` | background | background | town_shop | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, environment background, cozy und |
| yes | P2 | `memento_blackmarket_letter` | memento | icon | memento_blackmarket_letter | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, small room keepsake prop, sealed |
| yes | P2 | `memento_debt_shadow_window` | memento | icon | memento_debt_shadow_window | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, small room keepsake prop, small  |
| yes | P2 | `memento_first_return_tag` | memento | icon | memento_first_return_tag | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, small room keepsake prop, first  |
| yes | P2 | `memento_low_san_blanket` | memento | icon | memento_low_san_blanket | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, small room keepsake prop, folded |
| yes | P2 | `memento_pawn_empty_tag` | memento | icon | memento_pawn_empty_tag | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, small room keepsake prop, empty  |
| yes | P2 | `memento_truth_red_thread` | memento | icon | memento_truth_red_thread | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, small room keepsake prop, truth  |
| yes | P2 | `memento_wall_crack_first_defeat` | memento | icon | memento_wall_crack_first_defeat | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, small room keepsake prop, first  |
| yes | P2 | `order_black_contested_core_buyout_icon` | order | icon | order_black_contested_core_buyout | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, compact contract emblem, contest |
| yes | P2 | `order_blackmarket_relic_box_icon` | order | icon | order_blackmarket_relic_box | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, compact contract emblem, black-m |
| yes | P2 | `order_crystal_scale_batch_icon` | order | icon | order_crystal_scale_batch | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, compact contract emblem, crystal |
| yes | P2 | `order_mage_oracle_sample_icon` | order | icon | order_mage_oracle_sample | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, compact contract emblem, oracle  |
| yes | P2 | `order_spore_cage_procurement_icon` | order | icon | order_spore_cage_procurement | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, compact contract emblem, spore c |
| yes | P2 | `order_workshop_spore_core_frame_icon` | order | icon | order_workshop_spore_core_frame | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, compact contract emblem, worksho |
| yes | P2 | `rumor_acid_gland_shortage_icon` | rumor | icon | rumor_acid_gland_shortage | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, small market rumor emblem, acid  |
| yes | P2 | `rumor_crystal_scale_contract_icon` | rumor | icon | rumor_crystal_scale_contract | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, small market rumor emblem, cryst |
| yes | P2 | `rumor_filter_shortage_icon` | rumor | icon | rumor_filter_shortage | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, small market rumor emblem, filte |
| yes | P2 | `rumor_living_mycelium_shortage_icon` | rumor | icon | rumor_living_mycelium_shortage | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, small market rumor emblem, livin |
| yes | P2 | `rumor_miner_lamps_icon` | rumor | icon | rumor_miner_lamps | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, small market rumor emblem, miner |
| yes | P2 | `rumor_old_blades_icon` | rumor | icon | rumor_old_blades | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, small market rumor emblem, old b |
| yes | P2 | `rumor_oracle_fossil_collector_icon` | rumor | icon | rumor_oracle_fossil_collector | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, small market rumor emblem, oracl |
| yes | P2 | `rumor_spore_amber_jeweler_icon` | rumor | icon | rumor_spore_amber_jeweler | - | - | subterranean fantasy adventure, whimsical yet ominous, steampunk machinery, brass and copper details, worn metal, soft painterly 2D game asset, warm lamplight, subtle bioluminescent glow, small market rumor emblem, spore |

## Source Files

- Manifest: `美术文档/_generated/art_manifest.json`
- Program handoff: `美术文档/_generated/程序接入交接清单.json`
