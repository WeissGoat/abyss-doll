# Evidence policy

- Passing focus target: one `final`.
- Issue without modification: one `issue`.
- Persisted iteration: `before` and `after`.
- T0 seal: every registered `seal`.
- Regression: the exact source RunID's report, UI/Registry snapshots, checklist, and only the PNG files regenerated under that run's `screenshots/` directory.

Prepare every formal capture with `p3_art_prepare_capture`; use the returned exact MCP arguments; finalize with `p3_art_finalize_capture`. Never import a user-supplied path.

Machine success leaves `ExternalReview=Required` and `ClaimCeiling=evidence_collected`.
