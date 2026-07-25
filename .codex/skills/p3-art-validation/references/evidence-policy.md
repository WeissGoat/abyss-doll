# Evidence policy

- Passing focus target: one `final`.
- Issue without modification: one `issue`.
- Persisted iteration: `before` and `after`.
- T0 seal: every registered `seal`.
- Regression: the exact source RunID's report, UI/Registry snapshots, checklist, and only the PNG files regenerated under that run's `screenshots/` directory.

Prepare every formal capture with `p3_art_prepare_capture`; use the returned exact MCP arguments; finalize with `p3_art_finalize_capture`. Never import a user-supplied path.

`p3_art_inspect_target` records the live bounded inspection. Its binding evidence must show an active UGUI node consuming the required VisualID, an `Assets/Art/Approved/` path, a non-empty GUID, and the same Sprite path/GUID returned by `VisualAssetRegistry`.

After display and Console review, record an explicit Agent review and call `finalize_runtime_validated`. Finalization writes `runtime-validation.json` with the internal checks and `claim=runtime_validated`; it does not modify Manifest or Registry status. A failed review leaves the public state at `registered`.

Profile completion records the technical result and its evidence package. `player_path_verified` and `regression_passed` require their own evidence and are not implied by runtime validation.
