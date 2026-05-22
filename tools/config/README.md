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
