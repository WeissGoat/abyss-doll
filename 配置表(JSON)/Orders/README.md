# Orders Config

`Orders/*.json` defines weekly town orders. Runtime state is stored as order instances on `PlayerProfile`.

Required fields:
- `OrderID`: stable unique id.
- `FactionID`: must reference `Factions/*.json`.
- `OrderType`: enum value from `EconomyOrderType`.
- `Requirement.RequiredCount`: number of matching items to deliver.
- `Weight`: positive value for weekly order selection.

Requirement matching:
- `RequiredItemIDs` restricts delivery to specific item configs.
- `RequiredTags` requires all listed tags on each delivered item.
- `ForbiddenTags` blocks matching items.
- `MinGridCost` requires item grid cost to be at least the configured value.
- `MinLayer` requires the player's highest unlocked dungeon layer.

Rewards:
- `FixedGold` is paid directly.
- `RewardID` optionally references `Rewards/*.json`.
- `ReputationDelta` and `TrustDelta` modify faction runtime state on completion.
