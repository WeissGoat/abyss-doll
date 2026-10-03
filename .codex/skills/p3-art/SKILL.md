---
name: p3-art
description: Use for any Project P3 art work - concept or placeholder images, formal runtime assets (icons, backgrounds, UI images, UI skins), character portrait sets and differences, narrative CG and comic pages, image generation or editing through the AI image gateway or a host image tool, candidate review and selection, and taking assets into Approved, Unity import and the VisualAssetRegistry. Checking that registered art shows correctly on screen belongs to p3-playtest.
---

# P3 美术生产

一条路：**生成 → 挑选 → 入库**（Approved → Unity 导入 → Registry）。素材在真实界面上显示得对不对，交给 `p3-playtest` 的美术轻量检查。

## 先判断是哪一种

| 类型 | 怎么做 | 参考 |
|---|---|---|
| 探索：概念图、方向对比、占位图 | 直接出图，存到 `tmp/art_explore/<日期>_<主题>/`（不进 git），给用户看。不进 Approved，不写 Manifest，没有门禁，结论只算探索结论。 | [image-generation.md](references/image-generation.md) |
| 标准资产：图标、背景、普通 UI 图片 | 走脚本流水线的快速道：编译需求 → 出图 → 一次性评审整批 → 选定 → 入库。 | [production-pipeline.md](references/production-pipeline.md) |
| 身份类资产：角色立绘套组、叙事 CG / 漫画页、UI 九宫格皮肤、首个风格锚点、替换界面上已在用的资产 | 先定角色、场景、风格锚点，再按套 / 按页生产，走完整道。 | [narrative-cg.md](references/narrative-cg.md)、[production-pipeline.md](references/production-pipeline.md) |

美术方向在 M4 锁定（暂定对标《火山的女儿》《来自深渊》，见 `PROJECT_STATUS.md`）。在那之前，正式生产只做 Demo 必需、和方向无关的素材；身份类资产和新的风格锚点等到 M4。

## 出图

- 新概念图：优先用宿主自带的生图工具（例如 Codex 的 `image_gen`）；没有就用 AI 图片网关的 `openai_images`。
- 已有图只改局部，或做角色 / 状态 / 姿态差分：`gemini_chat_image` 图生图。
- 需要二次元特化或 Danbooru tag 控制：`novelai`。
- 不同后端的 Prompt 格式不同（自然语言或 tag）；批量请求一张一张串行发。
- 正式批量前，先对目标后端做最小 smoke：`.\tools\美术工具\Test-AIImageBackends.ps1 -Backend <chatgpt|gemini|novelai> -Attempts 1`。
- 细节：[image-generation.md](references/image-generation.md)、[provider-selection.md](references/provider-selection.md)、[generation-workflows.md](references/generation-workflows.md)。

## 挑选和入库

脚本在 `tools/美术工具/`，参数见它的 `README.md`。门禁由脚本执行，看它们的拒绝信息即可，不用手工复查。

| 步骤 | 命令 |
|---|---|
| 编译需求 | `Compile-ArtGenerationRequests.ps1 -VisualID <ids>`，再 `validate_art_generation_requests.py --strict` |
| 撰写 Prompt | `Export-ArtPromptAuthoringPackage.ps1 -VisualID <ids>` → 撰写 → `Publish-ArtPromptRevision.ps1 -RevisionPath <file>` |
| 批量计划 | `Generate-ArtBatchPlan.ps1`；同 VisualID 替换用 `Generate-FormalV2ReplacementPlan.ps1` |
| 出图和处理 | 标准资产 `Run-ArtProductionBatch.ps1 -PlanPath <plan> -VisualID <ids> -Route <class>=<capability>`；立绘套组 `Run-CharacterPortraitSet.ps1`（`-Resume` 继续） |
| 选定 | `Select-ArtCandidate.ps1 -VisualID <id> -ReviewPath <review>`；立绘套组另过 `Invoke-PortraitSetGate.ps1 -Phase Prepare / Finalize / Check` |
| 入库 | `Invoke-ArtApprovedUnityRegistration.ps1 -Phase Plan`、`SyncApproved`、`Finalize`，用同一个 `-ArtImportRunID` |

按阶段需要再读：状态机 [state-machine.md](references/state-machine.md)，候选评审 [candidate-evaluation.md](references/candidate-evaluation.md)，入库 [approved-and-unity.md](references/approved-and-unity.md)，证据 [evidence-and-writeback.md](references/evidence-and-writeback.md)，什么时候停下来问用户 [interaction-gates.md](references/interaction-gates.md)。

同步 Approved 前要用户同意（可以一次批准一整批）。

## 硬规则

- 只有脚本写 `processed/` 轮次、`selected/`、Approved、Manifest 和 Registry；不手工复制候选，不覆盖已发布的轮次。原因：脚本让哈希、`.meta` 和证据保持一致，手工复制会悄悄破坏它们。
- 同 VisualID 替换保留 VisualID、Approved 路径、DisplaySpec、`.meta` 和 GUID。原因：Prefab 和 Registry 按 GUID 引用，GUID 一变，所有用到它的界面都会断。
- Prefab、Registry 和运行时代码不引用 `_IncomingAI`。原因：它不进 git，换台机器或打包就丢。
- 不提交 API key、`*.local.yaml`、smoke 测试图或 `_IncomingAI` 原始候选。原因：密钥泄露无法撤回；候选图体积大，也不是正式资产。
- 后端报错、限流、超时或返回 0 张可用图时，如实告诉用户，记录 `validation_limited:provider_unavailable:<provider>`，不悄悄换后端，也不说成出图成功。原因：请求发出去不等于拿到了图。
- 出图只代表有了候选。`selected`、`approved`、`registered`、`runtime_validated` 各要各的证据，不越级声明。原因：每一级检查的东西不同。
