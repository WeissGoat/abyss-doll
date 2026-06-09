# VisualAssetRegistry 登记缺口清单

> 美术侧生成的程序登记核对清单。它不修改 Unity 资产，只对比 `程序接入交接清单` 和当前 `VisualAssetRegistry.asset`。

- GeneratedAt: `2026-06-10T00:19:53+08:00`
- ProgramIntegrateVisualCount: `87`
- RegistryExistingCount: `278`
- MissingRegistryCount: `0`
- AlreadyRegisteredCount: `87`
- MissingApprovedFileCount: `0`
- MissingMetaFileCount: `0`

## 程序侧动作

1. 在 Unity Editor 中执行 `Tools/P3 Art/Rebuild Approved Sprite Registry`。
2. 保存 `UnityClient/Assets/Resources/VisualAssetRegistry.asset`。
3. 重跑 VisualAsset / ArtAcceptance 验收，并把新的 `UnityClient/Logs/ArtAcceptance/latest` 交给美术侧验收。

## 缺口明细

| VisualID | Priority | Domain | Type | Registered | Approved | Meta | Path |
|---|---|---|---|---|---|---|---|
