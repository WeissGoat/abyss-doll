---
id: tools_config_readme
title: Config Tools
type: tool
role: 程序
domain: config_tooling
status: active
source_of_truth: false
related:
  - AGENTS.md
  - PROJECT_STATUS.md
last_verified: 2026-05-23
update_rule: 修改对应工具入口、参数或执行流程时同步本文件。
---

# Config Tools

> Purpose: keep the versioned config source in `配置表(JSON)` separate from Unity's runtime copy in `UnityClient/Assets/StreamingAssets/Configs`.

## Sync-Configs.ps1

Copies all config JSON folders from the repository source directory into Unity `StreamingAssets`, which is ignored by Git and treated as a generated runtime copy.

```powershell
.\tools\config\Sync-Configs.ps1 -Clean
```

Recommended flow before running Unity config loading, visual manifest scans, or automated smoke tests:

```powershell
.\tools\config\Sync-Configs.ps1 -Clean
.\tools\美术工具\Update-ArtManifest.ps1
```
