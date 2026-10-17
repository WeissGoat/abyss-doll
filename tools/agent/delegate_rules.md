Project P3 is a Unity 2022.3 game; the Unity project is `UnityClient/`.

- Do not use the Unity Editor or Unity MCP unless the brief says so; only one agent may drive the Editor at a time.
- A running build is driven with `python tools/agent/p3cmd.py <command>` (send `help` for the list; `launch` starts
  the newest build under `Builds/Windows/`). Prefer text (`Describe`, `Occlusion`, `DragCheck`) over screenshots.
- `screenshot` takes an absolute path (a relative one lands in the build folder) and a width; keep it at 640.
- The build writes its log to `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Project P3\Player.log`.
- `配置表(JSON)/` is the config source; never edit `UnityClient/Assets/StreamingAssets/Configs` or any `_generated`
  folder, even as scratch.
- Quit any build you launched (`p3cmd.py quit`) before you answer.
