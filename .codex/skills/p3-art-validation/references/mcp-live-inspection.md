# MCP live inspection

Use this order:

```text
set_active_instance
-> p3_validation_readiness
-> p3_art_open_target
-> p3_art_inspect_target
-> manage_camera screenshot, game_view, include_image=true, camera omitted
-> diagnose
```

If the target is unreachable, record `art_blocked:target_screen_unreachable` and hand off to program. Do not construct fake gameplay state.

For a formal capture, the Game View must use the target's registered reference size (currently `1920x1080`). The inline image may be downscaled by `max_resolution`; `p3_art_finalize_capture` validates the original PNG dimensions.
