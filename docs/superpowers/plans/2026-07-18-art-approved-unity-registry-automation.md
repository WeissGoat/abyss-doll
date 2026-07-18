---
id: plan_art_approved_unity_registry_automation
title: Approved 到 Unity 导入与 Registry 登记自动化实施计划
type: plan
role: 美术
domain: art_asset_integration
status: active
source_of_truth: false
related: []
last_verified: 2026-07-18
update_rule: 修改 Approved 接入编排实现范围、文件结构、测试命令、Unity MCP 试点或完成口径时更新本文档。
---

# Approved 到 Unity 导入与 Registry 登记自动化 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 建立 `selected -> approved -> unity_imported -> registered` 的脚本 + Unity MCP 自动化链路，让 Agent 能安全同步 Approved、核验 live importer/Registry，并把完成状态写入现有 `RegistryStatus=registered`。

**Architecture:** 新增一个薄 PowerShell 入口和一个可测试的 Python 核心。Python 负责 run evidence、输入指纹、权限/路径预检、MCP 结果终验和 Manifest 写回；PowerShell 只负责调用现有 `Sync-ApprovedArt.ps1` 与 owning generator。Unity MCP 仍由 Agent 在 `SyncApproved` 和 `Finalize` 阶段之间调用，不新增 Editor C# 或 custom MCP tool。

**Tech Stack:** PowerShell 7、Python 3、`unittest`、Unity 2022.3.60f1、CoplayDev Unity MCP、现有 P3 Manifest/Approved/VisualAssetRegistry 工具。

## Global Constraints

- 正式文件复制必须继续由 `tools/美术工具/Sync-ApprovedArt.ps1` 完成。
- 不修改 `UnityClient/Assets/Scripts/Editor/VisualAssetRegistryEditorTools.cs`。
- Manifest 主 `Status` 保持 `approved`；接入完成写现有 `RegistryStatus=registered`。
- `unity_imported` 只存在于 ArtImportRun evidence，不新增 Manifest 字段。
- 不创建 Prefab / UGUI 绑定，不进入 PlayMode，不运行 ArtAcceptance、T0 或程序回归。
- 常开工具组保持 `core + testing + docs`；`scripting_ext` 仅在 live importer/Registry 快照阶段临时启用并恢复关闭。
- 工作区已有并发改动；每个提交只暂存本任务文件或本任务明确改动的 hunk。
- 首次核心角色 Approved、新 Approved 路径、覆盖已有 Approved、`.meta` / GUID 风险仍必须经过授权。

---

## File Structure

- Create `tools/美术工具/art_approved_unity_registration.py`: ArtImportRun schema、Plan、Approved 同步核验、MCP 终验、Manifest RegistryStatus 写回和 summary。
- Create `tools/美术工具/Invoke-ArtApprovedUnityRegistration.ps1`: `Plan`、`SyncApproved`、`Finalize` 三阶段稳定 CLI，复用现有脚本。
- Create `tools/美术工具/tests/test_art_approved_unity_registration.py`: Python 核心单元和临时目录集成测试。
- Modify `tools/美术工具/README.md`: 新入口、阶段命令、MCP 交接文件、恢复规则；同时修正现有 Sync processed Resolver 旧描述。
- Modify `.codex/skills/p3-art-asset-production/references/approved-and-unity.md`: Agent 的精确工具组和 MCP 调用顺序、evidence schema、终验门禁。
- Modify `.codex/skills/p3-art-asset-production/references/evidence-and-writeback.md`: 增加 `P3ArtImport/<ArtImportRunID>` 证据引用和 ProductionRun 关联规则。
- Modify `美术文档/00_美术流水线总览.md`: 将 Unity 导入 / Registry 阶段指向新编排入口，终点明确为 `registered`。
- Modify `美术文档/02_资源规格与接入规范.md`: 明确 live importer/Registry 证据与主 Status / RegistryStatus 分工。
- Modify `agent_status/art.md`: 实现完成后记录新入口、试点状态与下一步 Approved 授权门禁。
- Modify `agent_status/program.md`: 仅记录静态 Sprite 接入交接能力已自动化，不声明运行绑定完成。

---

### Task 1: ArtImport Plan 与输入指纹核心

**Files:**
- Create: `tools/美术工具/art_approved_unity_registration.py`
- Create: `tools/美术工具/tests/test_art_approved_unity_registration.py`

**Interfaces:**
- Consumes: `sync_approved_art.choose_source`, `sync_approved_art.repo_path`, `art_workspace.normalize_entry_workspace_paths`, `art_workspace.workspace_path`。
- Produces: `create_plan(manifest_path: Path, incoming_root: Path, approved_root: Path, evidence_root: Path, art_import_run_id: str, visual_ids: list[str], mode: str, unity_instance: str, permissions: dict[str, bool]) -> dict[str, Any]`、`load_run_document(run_path: Path, name: str, schema: str) -> dict[str, Any]`、CLI `plan`；后续任务依赖 `run_dir()`、`read_json()`、`write_json_atomic()`、`sha256_file()`、`parse_meta_guid()`。

- [ ] **Step 1: 写 Plan 失败测试**

在 `test_art_approved_unity_registration.py` 建立临时项目结构，写入一个 `character_portrait_set` Manifest entry、`selected/001.png` 和目标 OutputPath，测试：

```python
def test_create_plan_records_selected_target_and_expected_importer(self) -> None:
    plan = create_plan(
        manifest_path=self.manifest_path,
        incoming_root=self.incoming_root,
        approved_root=self.approved_root,
        evidence_root=self.evidence_root,
        art_import_run_id="art_import_test_01",
        visual_ids=["doll_zero_dialogue_neutral"],
        mode="interactive",
        unity_instance="UnityClient@test1234",
        permissions={
            "allow_approved_sync": False,
            "allow_existing_target_overwrite": False,
            "allow_new_approved_target": False,
        },
    )
    item = plan["items"][0]
    self.assertEqual(item["selected_source_kind"], "selected")
    self.assertEqual(item["selected_sha256"], sha256_file(self.selected_path))
    self.assertEqual(item["unity_asset_path"], "Assets/Art/Approved/Dolls/doll_zero_dialogue_neutral.png")
    self.assertEqual(item["expected_importer"]["max_texture_size"], 2048)
    self.assertTrue(item["authorization_required"])
```

再增加：VisualID 不存在、重复 Manifest entry、selected 缺失、OutputPath 不在 `UnityClient/Assets/Art/Approved`、run ID 重用但请求指纹变化时失败。

- [ ] **Step 2: 运行测试确认失败**

Run:

```powershell
python -m unittest tools/美术工具/tests/test_art_approved_unity_registration.py -v
```

Expected: FAIL，`ModuleNotFoundError: No module named 'art_approved_unity_registration'`。

- [ ] **Step 3: 实现基础 helper 和 Plan schema**

新增下列稳定接口：

```python
REQUEST_SCHEMA = "p3-art-import-request@1"
PLAN_SCHEMA = "p3-art-approved-plan@1"
SYNC_SCHEMA = "p3-art-approved-sync@1"
UNITY_IMPORT_SCHEMA = "p3-art-unity-import@1"
REGISTRY_SCHEMA = "p3-art-registry-result@1"
CONSOLE_SCHEMA = "p3-art-console-delta@1"
SUMMARY_SCHEMA = "p3-art-import-summary@1"


class ArtImportError(RuntimeError):
    pass


def run_dir(evidence_root: Path, art_import_run_id: str) -> Path:
    if not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9_.-]{2,127}", art_import_run_id):
        raise ArtImportError("invalid ArtImportRunID")
    return evidence_root / art_import_run_id


def parse_meta_guid(meta_path: Path) -> str:
    match = re.search(r"(?m)^guid:\s*([0-9a-fA-F]{32})\s*$", meta_path.read_text(encoding="utf-8-sig"))
    if match is None:
        raise ArtImportError(f"Unity meta has no guid: {meta_path}")
    return match.group(1).lower()


def expected_importer(entry: dict[str, Any]) -> dict[str, Any]:
    source_spec = entry.get("Spec", {}).get("SourceSpec", {})
    width = int(source_spec["Width"])
    height = int(source_spec["Height"])
    max_dimension = max(width, height, 512)
    max_texture_size = 1 << (max_dimension - 1).bit_length()
    return {
        "texture_type": "Sprite",
        "sprite_import_mode": "Single",
        "alpha_is_transparency": bool(source_spec.get("AlphaRequired", True)),
        "max_texture_size": min(max_texture_size, 8192),
        "filter_mode": "Bilinear",
        "mipmap_enabled": False,
    }
```

`create_plan` 必须：

1. 读取并归一化 Manifest entries；
2. 对 VisualID 做唯一匹配；
3. 使用现有 `choose_source` 解析实际 selected；
4. 限制 OutputPath 在 Approved 根下并转换为形如 `Assets/Art/Approved/Dolls/doll_zero_dialogue_neutral.png` 的 Unity 路径；
5. 记录 selected/Manifest/目标 PNG/目标 `.meta` SHA-256 和 GUID；
6. 记录 create/overwrite/noop 动作与权限需求；
7. 写 `request.json` 和 `approved-plan.json`，重复 Plan 仅在请求指纹一致时幂等成功。

- [ ] **Step 4: 实现全局 Approved basename 冲突检测**

增加：

```python
def find_approved_basename_collisions(approved_root: Path) -> dict[str, list[str]]:
    by_visual_id: dict[str, list[str]] = {}
    for path in sorted(approved_root.rglob("*")):
        if path.is_file() and path.suffix.lower() in IMAGE_EXTENSIONS:
            by_visual_id.setdefault(path.stem, []).append(path.as_posix())
    return {key: values for key, values in by_visual_id.items() if len(values) > 1}
```

Plan 遇到任意冲突时写入 `blocking_errors`，代码使用 `blocked:approved_visualid_collision`，后续 SyncApproved 必须拒绝。

- [ ] **Step 5: 运行 Plan 测试确认通过**

Run:

```powershell
python -m unittest tools/美术工具/tests/test_art_approved_unity_registration.py -v
```

Expected: Plan、路径、权限、冲突和幂等用例全部 PASS。

- [ ] **Step 6: 提交 Task 1**

```powershell
git add -- tools/美术工具/art_approved_unity_registration.py tools/美术工具/tests/test_art_approved_unity_registration.py
git commit -m "feat: add art import planning evidence"
```

---

### Task 2: Approved 同步编排与证据核验

**Files:**
- Modify: `tools/美术工具/art_approved_unity_registration.py`
- Create: `tools/美术工具/Invoke-ArtApprovedUnityRegistration.ps1`
- Modify: `tools/美术工具/tests/test_art_approved_unity_registration.py`

**Interfaces:**
- Consumes: Task 1 的 `approved-plan.json`、现有 `Sync-ApprovedArt.ps1`。
- Produces: `verify_sync_authorization(run_path: Path, authorize_approved_sync: bool) -> dict[str, Any]`、`record_approved_sync(run_path: Path) -> dict[str, Any]`、CLI `verify-sync` / `record-sync`；PowerShell `-Phase SyncApproved`。

- [ ] **Step 1: 写权限和同步核验失败测试**

增加：

```python
def test_verify_sync_requires_explicit_gate_and_new_target_permission(self) -> None:
    self.create_default_plan()
    with self.assertRaisesRegex(ArtImportError, "approved_authorization_required"):
        verify_sync_authorization(self.run_path, authorize_approved_sync=False)
    with self.assertRaisesRegex(ArtImportError, "new Approved target"):
        verify_sync_authorization(self.run_path, authorize_approved_sync=True)
```

覆盖场景：

- `AuthorizeApprovedSync` 缺失；
- 新目标没有 `allow_new_approved_target`；
- 已有目标没有 `allow_existing_target_overwrite`；
- Plan 有 blocking errors；
- Plan 后 selected hash 或 Manifest hash 变化；
- 同 VisualID 替换后 `.meta` 字节或 GUID 变化。

- [ ] **Step 2: 运行测试确认失败**

Run:

```powershell
python -m unittest tools/美术工具/tests/test_art_approved_unity_registration.py -v
```

Expected: FAIL，缺少 `verify_sync_authorization` / `record_approved_sync`。

- [ ] **Step 3: 实现 SyncApproved 前后核验**

新增：

```python
def verify_sync_authorization(run_path: Path, *, authorize_approved_sync: bool) -> dict[str, Any]:
    request = load_run_document(run_path, "request.json", REQUEST_SCHEMA)
    plan = load_run_document(run_path, "approved-plan.json", PLAN_SCHEMA)
    if plan["blocking_errors"]:
        raise ArtImportError(plan["blocking_errors"][0]["code"])
    if not authorize_approved_sync or not request["permissions"]["allow_approved_sync"]:
        raise ArtImportError("blocked:approved_authorization_required")
    # Recompute manifest and selected fingerprints before returning the plan.
    return plan


def record_approved_sync(run_path: Path) -> dict[str, Any]:
    plan = load_run_document(run_path, "approved-plan.json", PLAN_SCHEMA)
    items: list[dict[str, Any]] = []
    for planned in plan["items"]:
        target = PROJECT_ROOT / planned["output_path"]
        if not target.exists():
            raise ArtImportError(f"failed:approved_sync target missing: {target}")
        approved_sha = sha256_file(target)
        if approved_sha != planned["selected_sha256"]:
            raise ArtImportError(f"failed:approved_sync hash mismatch: {planned['visual_id']}")
        meta_path = target.with_name(target.name + ".meta")
        meta_guid = parse_meta_guid(meta_path) if meta_path.exists() else None
        if planned["target_meta_sha256"] and sha256_file(meta_path) != planned["target_meta_sha256"]:
            raise ArtImportError(f"failed:approved_sync meta changed: {planned['visual_id']}")
        items.append({
            "visual_id": planned["visual_id"],
            "approved_sha256": approved_sha,
            "meta_guid": meta_guid,
            "meta_status": "preserved" if meta_guid else "awaiting_unity_import",
            "status": "passed",
        })
    result = {"schema": SYNC_SCHEMA, "items": items, "completed_at": now_iso()}
    write_json_atomic(run_path / "approved-sync.json", result)
    return result
```

新目标允许 Unity 尚未生成 `.meta`，`approved-sync.json` 记录 `meta_status=awaiting_unity_import`；已有目标必须保留同步前 `.meta` SHA-256 和 GUID。

- [ ] **Step 4: 实现 PowerShell 三阶段外壳的 Plan / SyncApproved**

参数固定为：

```powershell
param(
    [ValidateSet("Plan", "SyncApproved", "Finalize")]
    [string]$Phase,
    [Parameter(Mandatory = $true)]
    [string]$ArtImportRunID,
    [string[]]$VisualID = @(),
    [ValidateSet("interactive", "auto")]
    [string]$Mode = "interactive",
    [string]$UnityInstance = "",
    [string]$ManifestPath = "",
    [string]$IncomingRoot = "",
    [string]$ApprovedRoot = "",
    [string]$EvidenceRoot = "",
    [switch]$AuthorizeApprovedSync,
    [switch]$AllowExistingTargetOverwrite,
    [switch]$AllowNewApprovedTarget,
    [switch]$RefreshProgramHandoff
)
```

`Plan` 调用 Python CLI 并透传权限；`SyncApproved`：

1. 调用 `verify-sync`；
2. 读取 plan 中 VisualIDs；
3. 调用同目录 `Sync-ApprovedArt.ps1 -VisualID $ids`，存在覆盖动作时加 `-Overwrite`；
4. 调用 `record-sync`；
5. 任一步失败立即退出非零。

不得在新脚本中复制 PNG 或改写 `.meta`。

- [ ] **Step 5: 运行测试与 PowerShell 语法检查**

Run:

```powershell
python -m unittest tools/美术工具/tests/test_art_approved_unity_registration.py -v
[System.Management.Automation.Language.Parser]::ParseFile(
  (Resolve-Path '.\tools\美术工具\Invoke-ArtApprovedUnityRegistration.ps1'),
  [ref]$null,
  [ref]$null
) | Out-Null
```

Expected: Python 全部 PASS；PowerShell parser 无 error。

- [ ] **Step 6: 提交 Task 2**

```powershell
git add -- tools/美术工具/art_approved_unity_registration.py tools/美术工具/Invoke-ArtApprovedUnityRegistration.ps1 tools/美术工具/tests/test_art_approved_unity_registration.py
git commit -m "feat: orchestrate approved art synchronization"
```

---

### Task 3: MCP 证据终验、RegistryStatus 写回与生成清单

**Files:**
- Modify: `tools/美术工具/art_approved_unity_registration.py`
- Modify: `tools/美术工具/Invoke-ArtApprovedUnityRegistration.ps1`
- Modify: `tools/美术工具/tests/test_art_approved_unity_registration.py`

**Interfaces:**
- Consumes: `approved-sync.json`、Agent 生成的 `unity-import.json`、`registry-result.json`、`console-delta.json`。
- Produces: `stage_finalize(run_path: Path, manifest_path: Path) -> dict[str, Any]`、`complete_finalize(run_path: Path, generated_paths: list[Path]) -> dict[str, Any]`、Manifest `RegistryStatus=registered`、`generated-writeback.json`、`summary.json`。

- [ ] **Step 1: 写 Finalize 失败测试**

增加 helper 写三份 MCP 证据，并测试以下拒绝条件：

```python
def test_stage_finalize_rejects_registry_guid_mismatch(self) -> None:
    self.prepare_synced_run()
    self.write_valid_unity_import()
    self.write_registry_result(sprite_guid="f" * 32)
    self.write_valid_console_delta()
    with self.assertRaisesRegex(ArtImportError, "registry_asset_mismatch"):
        stage_finalize(self.run_path, manifest_path=self.manifest_path)
```

必须覆盖：

- schema 错误；
- ArtImportRunID / unity instance / VisualID 集合不一致；
- MCP observed_at 早于 approved-sync completed_at；
- Texture2D / Sprite 缺失；
- importer 任一字段不匹配；
- Registry `match_count != 1`；
- `TryGetEntry=false`；
- Registry Sprite path 或 GUID 错；
- Console 新增 errors 或 target warnings；
- `.meta` 当前 GUID 与 Unity evidence 不一致。

- [ ] **Step 2: 运行测试确认失败**

Run:

```powershell
python -m unittest tools/美术工具/tests/test_art_approved_unity_registration.py -v
```

Expected: FAIL，缺少 `stage_finalize`。

- [ ] **Step 3: 实现 MCP evidence 深度校验**

证据最小结构固定为：

```json
{
  "schema": "p3-art-unity-import@1",
  "art_import_run_id": "art_import_test_01",
  "unity_instance": "UnityClient@test1234",
  "observed_at": "2026-07-18T12:00:00+08:00",
  "items": [
    {
      "visual_id": "doll_zero_dialogue_neutral",
      "asset_path": "Assets/Art/Approved/Dolls/doll_zero_dialogue_neutral.png",
      "asset_database_guid": "0123456789abcdef0123456789abcdef",
      "main_asset_type": "UnityEngine.Texture2D",
      "sprite_loaded": true,
      "source_width": 1024,
      "source_height": 1536,
      "importer": {
        "texture_type": "Sprite",
        "sprite_import_mode": "Single",
        "alpha_is_transparency": true,
        "max_texture_size": 2048,
        "filter_mode": "Bilinear",
        "mipmap_enabled": false
      },
      "status": "passed"
    }
  ]
}
```

Registry evidence 使用：

```json
{
  "schema": "p3-art-registry-result@1",
  "art_import_run_id": "art_import_test_01",
  "unity_instance": "UnityClient@test1234",
  "observed_at": "2026-07-18T12:01:00+08:00",
  "registry_asset_path": "Assets/Resources/VisualAssetRegistry.asset",
  "items": [
    {
      "visual_id": "doll_zero_dialogue_neutral",
      "match_count": 1,
      "try_get_entry": true,
      "sprite_loaded": true,
      "sprite_asset_path": "Assets/Art/Approved/Dolls/doll_zero_dialogue_neutral.png",
      "sprite_guid": "0123456789abcdef0123456789abcdef",
      "status": "passed"
    }
  ]
}
```

Console evidence 使用 `errors=[]`、`target_warnings=[]`、`global_preexisting_warnings=[]`，只有前两项必须为空。

- [ ] **Step 4: 实现 Manifest 幂等写回**

`stage_finalize` 验证全部证据后：

```python
def mark_registry_status(manifest: dict[str, Any], visual_ids: set[str]) -> bool:
    changed = False
    for entry in manifest["Entries"]:
        if entry.get("VisualID") not in visual_ids:
            continue
        if entry.get("RegistryStatus") != "registered":
            entry["RegistryStatus"] = "registered"
            changed = True
    return changed
```

使用原子替换写 Manifest，保持 `Status=approved`，不得追加重复 Notes。写 `finalize-stage.json` 记录 manifest before/after hash、VisualIDs 和状态。

- [ ] **Step 5: 在 PowerShell Finalize 中刷新 owning outputs**

顺序固定：

```powershell
python $pythonScript stage-finalize --art-import-run-id $ArtImportRunID --manifest-path $ManifestPath --evidence-root $EvidenceRoot
& $integrationScript -Snapshot -SnapshotTag $ArtImportRunID
& $handoffScript -Snapshot -SnapshotTag $ArtImportRunID
& $gapScript -Snapshot -SnapshotTag $ArtImportRunID
python $pythonScript complete-finalize --art-import-run-id $ArtImportRunID --evidence-root $EvidenceRoot --generated-path $integrationJson --generated-path $handoffJson --generated-path $gapJson
```

`complete-finalize` 检查 latest JSON/Markdown 存在且时间不早于 `finalize-stage.json`，写 `generated-writeback.json` 与 `summary.json`。summary claim 固定为 `registered`，scope exclusions 原样保留。

- [ ] **Step 6: 运行全部新增测试**

Run:

```powershell
python -m unittest tools/美术工具/tests/test_art_approved_unity_registration.py -v
```

Expected: Plan、Sync、Finalize、幂等和失败分类全部 PASS。

- [ ] **Step 7: 提交 Task 3**

```powershell
git add -- tools/美术工具/art_approved_unity_registration.py tools/美术工具/Invoke-ArtApprovedUnityRegistration.ps1 tools/美术工具/tests/test_art_approved_unity_registration.py
git commit -m "feat: finalize live unity art registration"
```

---

### Task 4: Skill 与事实文档接入

**Files:**
- Modify: `.codex/skills/p3-art-asset-production/references/approved-and-unity.md`
- Modify: `.codex/skills/p3-art-asset-production/references/evidence-and-writeback.md`
- Modify: `tools/美术工具/README.md`
- Modify: `美术文档/00_美术流水线总览.md`
- Modify: `美术文档/02_资源规格与接入规范.md`
- Modify: `agent_status/art.md`
- Modify: `agent_status/program.md`

**Interfaces:**
- Consumes: Tasks 1-3 的 CLI、evidence schema 和失败代码。
- Produces: Agent 可直接执行的标准调用说明和项目状态回写。

- [ ] **Step 1: 更新 Skill 的 Approved / Unity 顺序**

在 `approved-and-unity.md` 明确：

```text
Invoke-ArtApprovedUnityRegistration.ps1 -Phase Plan
-> Approved 用户门禁
-> -Phase SyncApproved
-> set_active_instance
-> Console baseline
-> refresh_unity + Wait-UnityIdle
-> scripting_ext on
-> importer pre-snapshot
-> Rebuild Approved Sprite Registry
-> refresh + Wait-UnityIdle
-> importer post-snapshot + live Registry snapshot
-> Validate Approved Display Specs
-> Console delta
-> scripting_ext off
-> -Phase Finalize
```

写清菜单成功不等于状态完成、runtime scope 明确排除、恢复点与失败代码。

- [ ] **Step 2: 更新 evidence 关系**

在 `evidence-and-writeback.md` 增加：

```text
UnityClient/Logs/P3ArtImport/<ArtImportRunID>/
```

ProductionRun 只引用 ArtImportRunID；ArtImport evidence 不是第二套进度表。完成 claim 为 `registered`，运行绑定继续由后续任务负责。

- [ ] **Step 3: 更新工具 README**

新增三阶段命令、各阶段写入文件、权限 switches 和恢复示例。修正 `Sync-ApprovedArt` 旧描述为：

```text
Manifest.SelectedPath -> selected/ 第一张兼容 -> 最新数字轮次唯一 passed
```

最新轮次失败、多候选或证据不完整时不得回退。

- [ ] **Step 4: 更新美术事实文档和状态页**

`00` / `02` 写入新入口、`Status=approved` / `RegistryStatus=registered` 分工和 MCP live 证据。`agent_status/art.md` 记录自动化已落地但 Zero 首次 Approved 尚待授权；`agent_status/program.md` 只记录 Registry 登记交接自动化，不声明 runtime binding。

- [ ] **Step 5: 运行文档生成与校验**

Run:

```powershell
.\tools\docs\Generate-DocsIndex.ps1
.\tools\docs\Validate-Docs.ps1
```

Expected: `validation passed`，无缺失元数据或单向 related。

- [ ] **Step 6: 精确提交 Task 4**

先检查每个 dirty 文件已有用户改动，只暂存本任务 hunk：

```powershell
git diff -- .codex/skills/p3-art-asset-production/references/approved-and-unity.md
git diff -- tools/美术工具/README.md
git diff -- 美术文档/00_美术流水线总览.md 美术文档/02_资源规格与接入规范.md
git commit -m "docs: route art assets through unity registration automation"
```

---

### Task 5: 完整验证与首个 dry-run 试点

**Files:**
- Test: `tools/美术工具/tests/test_art_approved_unity_registration.py`
- Evidence only: `UnityClient/Logs/P3ArtImport/<ArtImportRunID>/request.json`
- Evidence only: `UnityClient/Logs/P3ArtImport/<ArtImportRunID>/approved-plan.json`

**Interfaces:**
- Consumes: 完成的 CLI、当前 `zero_dialogue_portrait_v1` selected 资产和 live Unity MCP。
- Produces: 单元测试结果、Plan evidence、Unity 只读可达性证据；不写 Approved。

- [ ] **Step 1: 运行美术工具相关回归**

Run:

```powershell
python -m unittest discover -s tools/美术工具/tests -p "test_art_*.py" -v
python -m unittest tools/美术工具/tests/test_sync_approved_art.py tools/美术工具/tests/test_generate_art_integration_candidates.py -v
```

Expected: 全部 PASS，现有数字轮次 Resolver 行为不回归。

- [ ] **Step 2: 运行新入口 Plan 试点**

选择 `doll_zero_dialogue_neutral`，只运行：

```powershell
.\tools\美术工具\Invoke-ArtApprovedUnityRegistration.ps1 `
  -Phase Plan `
  -ArtImportRunID art_import_zero_dialogue_neutral_plan_20260718_01 `
  -VisualID doll_zero_dialogue_neutral `
  -UnityInstance UnityClient@c0741596
```

Expected:

- 写 request / approved-plan；
- `authorization_required=true`；
- selected hash、OutputPath 和 expected importer 正确；
- 不创建 Approved PNG，不修改 `.meta` / Registry / Manifest 状态。

- [ ] **Step 3: 运行 Unity MCP 只读能力检查**

1. 确认工具组为 core + testing + docs，`scripting_ext=false`。
2. 锁定 `UnityClient@c0741596`。
3. 读取 editor_state、menu-items 和现有 Approved 测试资产 `ui_missing_sprite` 的 manage_asset info。
4. 临时启用 `scripting_ext`，只读查询该既有资产的 importer 和 Registry entry。
5. 关闭 `scripting_ext` 并再次列出工具组。

Expected: live 读取成功，工具组恢复，不执行 Registry 菜单，不修改项目资产。

- [ ] **Step 4: 运行严格完成检查**

Run:

```powershell
.\tools\agent\Invoke-AgentHealthCheck.ps1
git diff --check
git status --short
```

Expected: 本任务测试和文档通过；工作区仍可能有既有 WARN，但没有本任务新增的未解释失败或误暂存。

- [ ] **Step 5: 停在首次核心角色 Approved 门禁**

向用户报告 Plan evidence、将创建的 Approved 路径、是否覆盖、`.meta` 状态和 Unity MCP 可达性。只有用户明确授权后，才运行 `-Phase SyncApproved` 并继续真实 `unity_imported -> registered` 试点。

---

## Plan Self-Review

- Spec coverage: Plan、Approved gate、Sync、Unity importer、Registry、Console、writeback、工具组恢复、失败恢复和 dry-run 试点均有对应 Task。
- Scope: 没有 Prefab / UGUI 绑定、PlayMode、ArtAcceptance 或程序回归步骤。
- Type consistency: 三阶段 CLI、schema 名称、`RegistryStatus=registered` 和 ArtImportRunID 在所有 Task 中一致。
- Safety: 所有正式 PNG 仍由现有 Sync 工具写入；首次 Zero Approved 在 Task 5 明确停问。
