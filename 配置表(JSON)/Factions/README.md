# Factions Config

`Factions/*.json` defines town economy factions used by orders, reputation, trust, and black-market hooks.

Required fields:
- `FactionID`: stable unique id.
- `DisplayName`: player-facing faction name.
- `VisibleOrderSlots`: how many weekly order candidates this faction exposes.
- `MaxActiveOrders`: accepted/in-progress order cap for this faction.

Optional fields:
- `IsBlackMarket`: marks black-market factions for future betrayal and risk rules.
- `FactionTags`: tags for design filtering.
- `ReputationRanks`: threshold table for later unlock checks.
