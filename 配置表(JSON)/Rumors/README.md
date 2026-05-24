# Rumors Config

`Rumors/*.json` defines weekly economy rumors. Current runtime support applies item sell price multipliers and order tag weight boosts.

Required fields:
- `RumorID`: stable unique id.
- `RumorType`: enum value from `EconomyRumorType`.
- `Channel`: enum value from `EconomySellChannel`.
- `PriceMultiplier`: multiplier applied to matching item base value.
- `DurationDays`: active duration after weekly refresh.
- `Weight`: positive value for weekly rumor selection.

Matching:
- `TargetTags` applies price changes to items with any matching static or dynamic tag.
- Empty `TargetTags` applies to all items in the configured channel.
- `BoostedOrderTags` doubles effective order selection weight for matching order tags.
