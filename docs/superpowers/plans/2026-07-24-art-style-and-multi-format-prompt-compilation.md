# 美术风格目录与多格式生成请求编译 Implementation Plan

> For agentic workers: REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox syntax for tracking.

**Goal:** 为 P3 Manifest 建立可 fingerprint 的分层美术风格合同、一次编译多种 Prompt 格式的持久化生成请求，并让标准批量流和角色立绘套组流消费同一份编译请求而不互相误路由。

**Architecture:** 保留现有 Manifest 作为规范化需求与状态快照，新增纯 Python 编译模块生成 CanonicalVisualBrief、natural_language_v1、danbooru_tags_v1 和结构化 TechnicalRequest，输出 美术文档/_generated/art_generation_requests.json。标准资产批量脚本只消费已编译 Request；角色立绘通过独立 AssetSet 编排器按成员依赖消费 Request。provider adapter 只负责选择已有格式、序列化后端参数并写入运行证据。

**Tech Stack:** Python 3、stdlib json/hashlib/unittest/argparse、Pillow、PowerShell wrappers、现有 tools/ai-image-gateway API、现有 Manifest/processed round/selection 工具。

## Global Constraints

- VisualIntent 是最终资产需求；稳定合同不得写入 OpenAI、Gemini、NovelAI、文生图、图生图、inpaint 或模型名称。
- ArtStyleCatalog 是 Manifest 中的生成快照；事实继续来自 active 美术文档、Token、Seed、配置和角色专项事实。
- natural_language_v1 默认用于 OpenAI/GPT image edit 和 Gemini image/chat image；danbooru_tags_v1 默认用于 NovelAI。
- 生成请求必须持久化到 美术文档/_generated/art_generation_requests.json，脚本不得在执行时请求 Agent 临时编译 Prompt。
- character_portrait_set 必须被标准批量执行器硬阻断，使用独立 AssetSet 编排。
- raw、processed/<正整数>、selected、approved、registered、runtime_validated 是不同声明；新编译不改变已发布资产状态、GUID、Registry 或 Approved hash。
- 不回滚或覆盖工作区已有用户/其他 Agent 改动；每个任务只暂存本任务文件。
- 所有新增 Python 代码必须有 focused unit tests；所有迁移必须先 dry-run，再允许写回生成物。

## File Map

### Create

- tools/美术工具/art_style_catalog.py：Catalog 规范化、fingerprint、StyleRef 解析和 brief 层继承。
- tools/美术工具/art_prompt_compiler.py：CanonicalVisualBrief、两种 Prompt Variant、TechnicalRequest 和 Request Catalog 写入。
- tools/美术工具/compile_art_generation_requests.py：读取 Manifest、编译全部/指定 VisualID、写 Request Catalog 和迁移报告。
- tools/美术工具/Compile-ArtGenerationRequests.ps1：公开 PowerShell 入口。
- tools/美术工具/validate_art_generation_requests.py：Request Catalog/schema/fingerprint/coverage/迁移不变量验证。
- tools/美术工具/Run-CharacterPortraitSet.ps1：角色套组编排入口。
- tools/美术工具/run_character_portrait_set.py：AssetSet 依赖排序、Request 校验、成员执行计划和停止门禁。
- tools/美术工具/tests/test_art_style_catalog.py
- tools/美术工具/tests/test_art_prompt_compiler.py
- tools/美术工具/tests/test_compile_art_generation_requests.py
- tools/美术工具/tests/test_validate_art_generation_requests.py
- tools/美术工具/tests/test_run_character_portrait_set.py

### Modify

- 美术文档/04_美术风格基准.md、ui_design/design_tokens.json、01_Manifest规范.md、00_美术流水线总览.md、README.md：记录机器可执行的风格语义、Manifest 字段和编译流程。
- tools/美术工具/update_art_manifest.py、generate_art_prompts.py：输出新字段并把现有 Domain 词典作为迁移适配器。
- tools/美术工具/run_art_generation.py：读取 Request Catalog，选择 PromptFormat，记录实际请求。
- tools/美术工具/generate_formal_v2_replacement_plan.py、run_art_production_batch.py：计划与批量执行改为消费 RequestID/fingerprint。
- tools/美术工具/validate_art_generated_json.py、zero_portrait_master_batch.py 及相关测试：接入新验证和兼容路由。
- .codex/skills/p3-art-asset-production、.codex/skills/p3-generate-image、agent_status/art.md、PROJECT_STATUS.md：实施完成后同步边界和证据。

---

### Task 1: 锁定风格源合同

**Files:**
- Modify: 美术文档/04_美术风格基准.md
- Modify: 美术文档/ui_design/design_tokens.json
- Modify: 美术文档/01_Manifest规范.md
- Test: tools/美术工具/tests/test_art_style_catalog.py

**Interfaces:** 产生 p3_global_v1、ui_v1、character_portrait_v1、ui_button_core_v1、primary、none、MoonWhiteTrim、PrimaryActionCrimson、RuneActive 等稳定语义名。

- [ ] Step 1: 在 test_art_style_catalog.py 写最小 Catalog fixture，并断言缺少 resolver 时测试失败。

~~~python
CATALOG = {
    "Version": 1,
    "GlobalStyle": {"ID": "p3_global_v1", "PositiveEN": ["Japanese anime-inspired 2D game art"]},
    "Profiles": {
        "ui_v1": {"PositiveEN": ["clean hand-painted fantasy game UI"]},
        "character_portrait_v1": {"PositiveEN": ["full-body anime game character portrait"]},
    },
    "Families": {
        "ui_button_core_v1": {
            "Profile": "ui_v1",
            "PositiveEN": ["single horizontal button skin"],
            "Roles": {"primary": {"PositiveEN": ["crimson primary action surface"]}},
        }
    },
    "ContextAccents": {"none": {"PositiveEN": []}},
}
~~~

- [ ] Step 2: 在 04_美术风格基准.md 增加 Global/Profile/Family/Role/ContextAccent 语义说明，并在 design_tokens.json 增加 ColorRoleAliases：

~~~json
"ColorRoleAliases": {
  "MoonWhiteTrim": "TextPrimary",
  "PrimaryActionCrimson": "CoralAccent",
  "RuneActive": "RuneActive"
}
~~~

明确主行动颜色来自 Role，不得来自 VisualID；浅蓝只用于激活、选中和能量反馈。

- [ ] Step 3: 在 01_Manifest规范.md 增加 ArtStyleCatalog、StyleRef、VisualIntent、AssetSets、CompiledRequest、RequestFingerprint、PromptVariants、SemanticCoverage 字段规则；说明旧 Prompt 字段只是兼容输出。

- [ ] Step 4: 运行文档索引和校验：

~~~powershell
.\tools\docs\Generate-DocsIndex.ps1
.\tools\docs\Validate-Docs.ps1
~~~

Expected: validation passed。

- [ ] Step 5: Commit：

~~~powershell
git add -- 美术文档/04_美术风格基准.md 美术文档/ui_design/design_tokens.json 美术文档/01_Manifest规范.md tools/美术工具/tests/test_art_style_catalog.py
git commit -m "docs: define art style catalog source contract"
~~~

---

### Task 2: 实现 Catalog 规范化和 StyleRef 解析

**Files:**
- Create: tools/美术工具/art_style_catalog.py
- Test: tools/美术工具/tests/test_art_style_catalog.py

**Interfaces:**

~~~python
canonical_json(value: Any) -> str
sha256_json(value: Any) -> str
build_catalog_snapshot(project_root: Path, existing: dict[str, Any] | None = None) -> dict[str, Any]
validate_catalog(catalog: dict[str, Any]) -> list[str]
resolve_style_ref(catalog: dict[str, Any], style_ref: dict[str, Any]) -> dict[str, Any]
resolve_entry_layers(entry: dict[str, Any], catalog: dict[str, Any], asset_sets: dict[str, Any]) -> dict[str, Any]
~~~

- [ ] Step 1: 实现排序 key、紧凑 JSON、UTF-8 SHA-256；不把 GeneratedAt、输出路径、provider、seed、variants 或运行状态放入输入 fingerprint。
- [ ] Step 2: 实现 Catalog 校验，返回 style_* 错误码：global/profile/family/role 缺失、Family/Profile 不匹配、ContextAccent 不允许。
- [ ] Step 3: 实现 Global → Profile → Family → Role → ContextAccent 解析；失败抛出 StyleResolutionError，不能静默回退。
- [ ] Step 4: 实现标准资产和角色资产解析；角色追加 AssetSet IdentityLocks、SetRole、SourceAssets；绝不从 VisualID 猜 Family/Role。
- [ ] Step 5: 添加 test_primary_role_resolves_to_crimson_semantic、test_visual_id_does_not_infer_role、test_family_role_mismatch_is_blocked、test_portrait_layers_include_identity_locks、test_fingerprint_ignores_generated_at。
- [ ] Step 6: Run and commit：

~~~powershell
python -m unittest tools/美术工具/tests/test_art_style_catalog.py -v
git add -- tools/美术工具/art_style_catalog.py tools/美术工具/tests/test_art_style_catalog.py
git commit -m "feat: add art style catalog resolver"
~~~

---

### Task 3: 实现多格式 Prompt 编译器

**Files:**
- Create: tools/美术工具/art_prompt_compiler.py
- Test: tools/美术工具/tests/test_art_prompt_compiler.py

**Interfaces:**

~~~python
build_canonical_visual_brief(entry: dict[str, Any], catalog: dict[str, Any], asset_sets: dict[str, Any]) -> dict[str, Any]
serialize_natural_language(brief: dict[str, Any]) -> dict[str, Any]
serialize_danbooru_tags(brief: dict[str, Any]) -> dict[str, Any]
compile_generation_request(entry: dict[str, Any], catalog: dict[str, Any], asset_sets: dict[str, Any], *, compiler_version: int = 1) -> dict[str, Any]
~~~

- [ ] Step 1: 写标准按钮和角色差分 fixture，断言 brief 含 Style、Subject、Composition、Required、Forbidden、Preserve、RequiredChanges。
- [ ] Step 2: 合并 StyleRef、VisualIntent、Spec、AssetSet locks、SetRole、SourceAssets，生成稳定语义 ID。
- [ ] Step 3: 实现 natural_language_v1，按 Global → Profile → Family → Role → Accent → Subject → Appearance → Composition → Preserve → Changes 拼接并去重；记录 SemanticCoverage。
- [ ] Step 4: 实现 danbooru_tags_v1，输出结构化 Tag/Weight；保持 provider-neutral；无法表达的必需语义标记 unsupported。
- [ ] Step 5: 将尺寸、Alpha、参考图、mask/edit intent、NineSlice 能力、身份锁和差分变化放入 TechnicalRequest/PreservationContract。
- [ ] Step 6: 添加 test_button_compiles_natural_language_and_tags、test_portrait_preserve_and_required_changes_are_structured、test_tag_weights_are_structured_not_provider_syntax、test_unsupported_semantic_marks_variant_not_ready、test_same_input_produces_same_variant_fingerprint。
- [ ] Step 7: Run and commit：

~~~powershell
python -m unittest tools/美术工具/tests/test_art_prompt_compiler.py -v
git add -- tools/美术工具/art_prompt_compiler.py tools/美术工具/tests/test_art_prompt_compiler.py
git commit -m "feat: compile art requests into multiple prompt formats"
~~~

---

### Task 4: 编译 Request Catalog 并迁移 Manifest

**Files:**
- Create: tools/美术工具/compile_art_generation_requests.py
- Create: tools/美术工具/Compile-ArtGenerationRequests.ps1
- Create: tools/美术工具/tests/test_compile_art_generation_requests.py
- Modify: tools/美术工具/update_art_manifest.py
- Modify: tools/美术工具/Update-ArtManifest.ps1
- Modify: tools/美术工具/generate_art_prompts.py
- Modify: related Manifest/prompt tests

**Interfaces:**

~~~text
Compile-ArtGenerationRequests.ps1
  -ManifestPath <path>
  -OutputPath <path>
  -VisualID <id[]>
  -DryRun
  -Overwrite
~~~

~~~python
compile_manifest_requests(manifest: dict[str, Any], *, project_root: Path, compiler_version: int = 1, visual_ids: set[str] | None = None) -> dict[str, Any]
~~~

- [ ] Step 1: 写一标准 Entry 和一角色 Entry 迁移测试，验证新字段出现且 Status、SelectedPath、ApprovedPath、RegistryStatus 不变。
- [ ] Step 2: 扩展 update_art_manifest 默认字段，并增加 --dry-run。新 Entry 获得确定性默认 Profile 和空 CompiledRequest；不能判断时写 style_resolution_required，不能猜测。--dry-run 只输出变更摘要，不写 Manifest。
- [ ] Step 3: 将现有 Domain 词典转为 VisualIntent 迁移适配器；旧 Prompt 字段继续兼容输出。
- [ ] Step 4: 从现有 AssetSetID/SetRole/SourceAssets 构建 AssetSets；为 Zero dialogue set 补身份来源和锁，不改变成员状态。
- [ ] Step 5: 输出 美术文档/_generated/art_generation_requests.json；Entry 只写 RequestID、RequestFingerprint、CompileStatus 引用。自然语言 Variant ready 时回填兼容 Prompt。
- [ ] Step 6: 实现 dry-run 报告，统计 ready、style_resolution_required、unsupported、invalid、unchanged published entries；dry-run 不写 Manifest/Request 文件。
- [ ] Step 7: Run and commit：

~~~powershell
python -m unittest tools/美术工具/tests/test_compile_art_generation_requests.py tools/美术工具/tests/test_update_art_manifest.py tools/美术工具/tests/test_generate_art_prompts.py -v
git add -- tools/美术工具/compile_art_generation_requests.py tools/美术工具/Compile-ArtGenerationRequests.ps1 tools/美术工具/update_art_manifest.py tools/美术工具/Update-ArtManifest.ps1 tools/美术工具/generate_art_prompts.py tools/美术工具/tests/test_compile_art_generation_requests.py tools/美术工具/tests/test_update_art_manifest.py tools/美术工具/tests/test_generate_art_prompts.py
git commit -m "feat: compile persisted art generation requests"
~~~

---

### Task 5: 增加严格 Request Catalog 验证

**Files:**
- Create: tools/美术工具/validate_art_generation_requests.py
- Create: tools/美术工具/tests/test_validate_art_generation_requests.py
- Modify: tools/美术工具/validate_art_generated_json.py

**Interfaces:**

~~~python
validate_request_catalog(catalog: dict[str, Any], manifest: dict[str, Any], *, strict: bool = False) -> list[str]
~~~

- [ ] Step 1: 写失败测试：RequestID 缺失、fingerprint 不匹配、Catalog 过期、Variant 缺失、Coverage<1、非法 tag weight、有效双格式请求。
- [ ] Step 2: 校验 Catalog version、Request 唯一性、Manifest pointer、canonical fingerprint、CompileStatus/Variant 状态组合，返回稳定错误码。
- [ ] Step 3: 比较迁移前后 VisualID、OutputPath、Status、RegistryStatus、ApprovedPath、selected/Approved hash；首版不提供绕过参数。
- [ ] Step 4: 为 validate_art_generated_json.py 增加可选 --request-catalog 和 --manifest；显式提供 Catalog 时严格失败。
- [ ] Step 5: Run and commit：

~~~powershell
python -m unittest tools/美术工具/tests/test_validate_art_generation_requests.py -v
git add -- tools/美术工具/validate_art_generation_requests.py tools/美术工具/tests/test_validate_art_generation_requests.py tools/美术工具/validate_art_generated_json.py
git commit -m "feat: validate compiled art generation requests"
~~~

---

### Task 6: 将单资产生成切换到编译 Request

**Files:**
- Modify: tools/美术工具/run_art_generation.py
- Create: tools/美术工具/tests/test_run_art_generation_requests.py

**Interfaces:**

新增 CLI：--request-catalog、--request-id、--prompt-format auto|natural_language_v1|danbooru_tags_v1。

~~~python
select_compiled_request(catalog: dict[str, Any], manifest_entry: dict[str, Any], *, request_id: str = "", prompt_format: str = "auto") -> tuple[dict[str, Any], str]
serialize_provider_prompt(variant: dict[str, Any], prompt_format: str) -> tuple[str, str]
~~~

- [ ] Step 1: 测试 auto/explicit format selection、stale/missing preflight failure、portrait workspace preservation。
- [ ] Step 2: 实现 Catalog loading/freshness；有指针但过期时失败，不能回退旧 Manifest Prompt。
- [ ] Step 3: 实现双格式序列化；tags 在 adapter 层转换为后端字符串，结构化 tags 保留到证据。
- [ ] Step 4: 改造 make_request，从 compiled request 和 selected Variant 读取 Prompt、尺寸和格式；PromptFormat 写入 Gateway extra。
- [ ] Step 5: generation.json 写入 RequestID、fingerprint、PromptFormat、RequestSnapshot、ProviderRequest、Provider、Model、outputs、errors。
- [ ] Step 6: Run and commit：

~~~powershell
python -m unittest tools/美术工具/tests/test_run_art_generation_requests.py -v
git add -- tools/美术工具/run_art_generation.py tools/美术工具/tests/test_run_art_generation_requests.py
git commit -m "feat: generate art from persisted prompt variants"
~~~

---

### Task 7: 切换 Formal V2 计划和标准批量执行

**Files:**
- Modify: tools/美术工具/generate_formal_v2_replacement_plan.py
- Modify: tools/美术工具/run_art_production_batch.py
- Modify: corresponding tests

计划项必须包含 VisualID、RequestID、RequestFingerprint、PromptFormats、ProductionProfile。

- [ ] Step 1: 测试 ready/stale/missing/portrait 计划；角色项在生成分组前阻断。
- [ ] Step 2: 用 Request readiness 替代 PromptReady 作为执行门；PromptReady 兼容字段由 Catalog 和目标格式计算。
- [ ] Step 3: grouping 增加 PromptFormat；命令传递 Request Catalog、RequestID、PromptFormat。
- [ ] Step 4: 返回 route_mismatch:character_portrait_set_requires_portrait_set_orchestration；allow-blocked 只能报告不能执行。
- [ ] Step 5: NineSlice 项继续要求显式 ui_skin capability；Prompt ready 不能绕过技术门禁。
- [ ] Step 6: Run and commit：

~~~powershell
python -m unittest tools/美术工具/tests/test_run_art_production_batch.py tools/美术工具/tests/test_generate_formal_v2_replacement_plan.py -v
git add -- tools/美术工具/generate_formal_v2_replacement_plan.py tools/美术工具/run_art_production_batch.py tools/美术工具/tests/test_generate_formal_v2_replacement_plan.py tools/美术工具/tests/test_run_art_production_batch.py
git commit -m "feat: route art batches through compiled requests"
~~~

---

### Task 8: 实现独立角色立绘套组路由

**Files:**
- Create: tools/美术工具/run_character_portrait_set.py
- Create: tools/美术工具/Run-CharacterPortraitSet.ps1
- Create: tools/美术工具/tests/test_run_character_portrait_set.py
- Modify: tools/美术工具/zero_portrait_master_batch.py

**Interfaces:**

~~~python
load_asset_set(manifest: dict[str, Any], asset_set_id: str) -> dict[str, Any]
order_portrait_members(asset_set: dict[str, Any], entries: list[dict[str, Any]]) -> list[dict[str, Any]]
build_portrait_set_plan(manifest: dict[str, Any], request_catalog: dict[str, Any], asset_set_id: str, *, prompt_format: str = "auto") -> dict[str, Any]
~~~

- [ ] Step 1: 测试 AssetSet 缺失、重复成员、母版/参考源缺失、依赖顺序、stale Request、有效 Zero dialogue set。
- [ ] Step 2: 要求 Profile、身份来源/锁、成员 AssetSetID、SetRole、SourceAssets；不得从文件名猜身份。
- [ ] Step 3: 使用 SourceAssets 显式边排序；循环/缺源返回 decision_required，不执行任何成员。
- [ ] Step 4: 每成员附 RequestID、fingerprint、PromptFormat、reference、preservation contract、workspace；不固定生成方法。
- [ ] Step 5: 只执行下一个 ready 成员或显式授权子集；failed/decision_required 时停止。
- [ ] Step 6: Zero 专用脚本增加 compiled Request/参考图入口；旧 Prompt 只能走 --legacy-prompt 并标记 legacy_unverified。
- [ ] Step 7: Run and commit：

~~~powershell
python -m unittest tools/美术工具/tests/test_run_character_portrait_set.py -v
git add -- tools/美术工具/run_character_portrait_set.py tools/美术工具/Run-CharacterPortraitSet.ps1 tools/美术工具/zero_portrait_master_batch.py tools/美术工具/tests/test_run_character_portrait_set.py
git commit -m "feat: add independent character portrait set route"
~~~

---

### Task 9: 接入验证器、Skill 和交接文档

**Files:**
- Modify: tools/美术工具/validate_art_generated_json.py
- Modify: .codex/skills/p3-art-asset-production
- Modify: .codex/skills/p3-generate-image
- Modify: 美术文档/00_美术流水线总览.md、README.md

- [ ] Step 1: 明确 art-asset-production 负责准入、Catalog readiness、路由、Approved 和 Unity handoff；generate-image 消费已编译 Variant、选择 provider、记录证据。
- [ ] Step 2: 文档化格式映射：OpenAI/GPT image edit 与 Gemini image/chat image 使用 natural_language_v1；NovelAI 使用 danbooru_tags_v1。
- [ ] Step 3: handoff 暴露 RequestID、PromptFormat、CompileStatus、route mismatch，不建立第二套进度表。
- [ ] Step 4: Run：

~~~powershell
.\tools\docs\Generate-DocsIndex.ps1
.\tools\docs\Validate-Docs.ps1
python -m unittest discover -s tools/美术工具/tests -p "test_*.py" -v
~~~

- [ ] Step 5: Commit：

~~~powershell
git add -- .codex/skills/p3-art-asset-production/SKILL.md .codex/skills/p3-art-asset-production/references/modes-and-input.md .codex/skills/p3-art-asset-production/references/workspace-profiles-and-character-portraits.md .codex/skills/p3-generate-image/SKILL.md .codex/skills/p3-generate-image/references/provider-selection.md 美术文档/00_美术流水线总览.md 美术文档/README.md tools/美术工具/validate_art_generated_json.py
git commit -m "docs: document compiled art request consumers"
~~~

---

### Task 10: 执行 313 Entry 迁移和 dry-run 验收

**Files:**
- Modify generated: 美术文档/_generated/art_manifest.json
- Create generated: 美术文档/_generated/art_generation_requests.json
- Create generated: 美术文档/_generated/art_generation_request_migration.json
- Modify generated: 美术文档/_generated/AI绘图提示词清单.md
- Modify: agent_status/art.md、PROJECT_STATUS.md

- [ ] Step 1: 保存迁移前快照：Entry 数量、VisualID 集合、Status、RegistryStatus、OutputPath、ApprovedPath、SelectedPath 和现有文件 hash。
- [ ] Step 2: dry-run Manifest/Request 编译：

~~~powershell
powershell -File tools/美术工具/Update-ArtManifest.ps1 -ManifestPath 美术文档/_generated/art_manifest.json -DryRun
powershell -File tools/美术工具/Compile-ArtGenerationRequests.ps1 -ManifestPath 美术文档/_generated/art_manifest.json -DryRun
~~~

Expected: no image generation, no Approved/Registry writes。

- [ ] Step 3: 写入并严格验证：

~~~powershell
powershell -File tools/美术工具/Compile-ArtGenerationRequests.ps1 -ManifestPath 美术文档/_generated/art_manifest.json
python tools/美术工具/validate_art_generation_requests.py --manifest 美术文档/_generated/art_manifest.json --request-catalog 美术文档/_generated/art_generation_requests.json --strict
~~~

- [ ] Step 4: 标准批量 dry-run 覆盖 bg_workshop_day、bg_combat_abyss、ui_icon_warning、ui_button_primary；断言只引用 Request，不发生 provider 调用。
- [ ] Step 5: mocked Gateway 验证 OpenAI/Gemini 选择 natural language，NovelAI 选择 tags，并检查 generation.json ProviderRequest 快照。
- [ ] Step 6: 角色套组 dry-run：

~~~powershell
powershell -File tools/美术工具/Run-CharacterPortraitSet.ps1 -AssetSetID zero_dialogue_portrait_v1 -ManifestPath 美术文档/_generated/art_manifest.json -RequestCatalogPath 美术文档/_generated/art_generation_requests.json -DryRun
~~~

断言母版/anchor 在差分前，标准批量对同一成员返回 route_mismatch。

- [ ] Step 7: 比较迁移前后快照；VisualID、OutputPath、Status、RegistryStatus、ApprovedPath、selected/Approved hash、.meta GUID 和 Registry 必须不变。
- [ ] Step 8: 在 agent_status/art.md 与 PROJECT_STATUS.md 写入实际 ready/blocked/unsupported 数量和验证命令。
- [ ] Step 9: 最终验证：

~~~powershell
.\tools\agent\Invoke-AgentHealthCheck.ps1 -Strict
.\tools\docs\Generate-DocsIndex.ps1
.\tools\docs\Validate-Docs.ps1
python -m unittest discover -s tools/美术工具/tests -p "test_*.py" -v
git status --short
~~~

- [ ] Step 10: 检查 generated/status 文件已有的并发改动，只暂存本次迁移新增的 hunks；然后提交本次迁移文件：

~~~powershell
git add -- 美术文档/_generated/art_manifest.json 美术文档/_generated/art_generation_requests.json 美术文档/_generated/art_generation_request_migration.json 美术文档/_generated/AI绘图提示词清单.md agent_status/art.md PROJECT_STATUS.md
git commit -m "feat: migrate manifest to persisted multi-format art requests"
~~~

## Self-Review Checklist

- [ ] Catalog/source contract、Manifest schema、CanonicalVisualBrief、自然语言、Danbooru tags、TechnicalRequest、Request persistence、fingerprint/stale、标准批量、角色套组、provider evidence、迁移、文档和测试均有对应任务。
- [ ] 每个跨任务函数名和 CLI flag 已定义，没有让实现者自行猜接口。
- [ ] 批次执行不会调用 Agent 临时编译；只有 Task 4/10 编译或刷新 Request Catalog。
- [ ] character_portrait_set 不能进入 run_art_production_batch.py。
- [ ] 编译不改变 Approved、Registry、GUID 或公开状态。
- [ ] 现有 Domain 词典作为迁移适配器保留，首版不强制删除。
- [ ] Provider-specific tag 语法只在 adapter 和 Run Evidence 出现，不进入稳定 Request Catalog。
