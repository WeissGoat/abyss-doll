---
name: p3-narrative-cg-comic
description: Use when Project P3 work involves narrative CG backgrounds, comic-page playback, panel VisualIDs, character/scene/style consistency, runtime comic screenshots, or acceptance claims for CG sequences such as T0-01A.
---

# P3 叙事 CG / 漫画式播放

## 定位

把 Project P3 的叙事 CG 从“批量独立生图”收敛为“锚点锁定 -> 页级候选 -> 运行时验收”的可执行流程。

本 skill 不替代事实文档：

- 通用规格以 `美术文档/18_CG底图与漫画式播放演出工作流.md` 为准。
- 目标切片细案以对应 CG 细案为准，例如 `美术文档/19_T0-01序章CG细案.md`。
- AI 后端、prompt、图生图、inpaint 和 raw 生成证据使用 `.codex/skills/p3-generate-image/SKILL.md`（Skill 名 `generate-image`）。
- selected 晋级、Approved 同步、Unity 导入和 Registry 登记使用 `.codex/skills/p3-art-asset-production/SKILL.md`（Skill 名 `p3-art-asset-production`）。
- 运行时截图、T0 seal capture 和 `runtime_validated` 验收使用 `.codex/skills/p3-art-validation/SKILL.md`；旧 ArtAcceptance 全量回归只走其 `art_regression` Profile。

## 触发场景

遇到以下任务必须使用本 skill：

- 制作、重做、验收 CG 底图、叙事背景、漫画页或分格演出。
- 将一段剧情拆成 `PageID`、`LayoutPreset`、Panel VisualID、字幕条和 Action。
- 处理同一角色在多格 CG 中的脸、发型、服装、姿态、空间或风格一致性。
- 判断一批 CG 是否能进入 Approved、运行时漫画播放或最终验收。
- 用户反馈“风格不对”“一致性有问题”“像一次性生图”“不像漫画播放”。

## 开工读取

1. 读取 `AGENTS.md`、`PROJECT_STATUS.md`、`agent_status/art.md`。
2. 读取目标 CG 事实来源：
   - 通用流程：`美术文档/18_CG底图与漫画式播放演出工作流.md`
   - 目标细案：例如 `美术文档/19_T0-01序章CG细案.md`
   - 目标角色 / 场景 / UI / 剧情事实文档。
3. 需要实际出图、图生图、局部修图或后端排障时，读取并遵守 `.codex/skills/p3-generate-image/SKILL.md`（Skill 名 `generate-image`）。
4. 需要 selected 晋级、Approved 同步、Unity 导入或 Registry 登记时，读取并遵守 `.codex/skills/p3-art-asset-production/SKILL.md`。
5. 需要运行时截图或验收时，读取并遵守 `.codex/skills/p3-art-validation/SKILL.md`。

## 禁止直接全量生图

除非用户明确要求只做探索草图，否则不得直接对全套 Panel 做独立文生图。

正式 CG 生产前必须先完成三类锚点：

| 锚点 | 目的 | 通过标准 |
|---|---|---|
| 角色锚点 | 锁定同一角色身份、脸、发型、服装、关键道具和状态差异。 | 角色在后续多格中可以被识别为同一人；允许状态变化，不允许身份漂移。 |
| 场景锚点 | 锁定主要空间结构、光源、材质、镜头语言和可复用物件。 | 后续环境格像同一个地点的不同取景，而不是每张重造空间。 |
| 风格锚点 | 锁定线条、上色、噪声、留白、漫画页阅读感和项目风格。 | 比单张漂亮更重要；必须能指导整页画面统一。 |

锚点未通过时，只能声明 `锚点候选` 或 `锚点待定`，不能进入全量 Panel 正式生产。

## 标准执行流程

### 1. 证据审计

先列出已有资产和结论：

- 哪些 VisualID 已在 Approved。
- 哪些是旧 fallback、staging 合成或临时底图。
- 哪些已有 Registry、运行时截图或 ArtAcceptance。
- 哪些只是 `_IncomingAI` 候选或 contact sheet。

不得把旧运行时截图用于证明新覆盖素材的验收通过。

### 2. 分镜与页级设计

每页必须先写清：

- `PageID`
- `LayoutPreset`
- 读图目标
- Panel 列表和 reveal 顺序
- 字幕功能位
- Action 是否存在
- 禁止 UI / 禁止风格 / 禁止误读

如果某页包含同一角色两格以上，必须标记为 `character_consistency_required`。

如果多页发生在同一空间，必须标记为 `scene_consistency_required`。

### 3. 锚点生产

按“少量、可控、可比较”生产锚点：

- 每类锚点先生成 2-4 张候选。
- 制作锚点 contact sheet。
- 选择一个主锚点，最多两个辅助锚点。
- 记录每张锚点的用途：角色、场景、风格或构图。

角色锚点优先使用图生图或参考图约束后续角色格。场景锚点优先约束同一空间下的所有环境格。

### 4. 页级候选生产

按页推进，不按全套 Panel 一次铺量：

1. 选择一页。
2. 用该页需要的锚点生成每个 Panel。
3. 每个 Panel 至少 2-4 个候选，正式批量仍按单图串行。
4. 预处理后生成单 Panel contact sheet。
5. 再生成整页 mock / page contact sheet，检查页内阅读顺序和一致性。
6. 页级通过后才进入下一页。

不得只按单图好看筛选；必须按页判断。

### 5. 一致性检查

每页筛选至少检查：

- 角色身份：脸、发型、遮挡、服装、体型、关键道具是否连续。
- 场景空间：门、桌、床、光源、暗门、道具位置是否像同一地点。
- 风格语言：线条、明暗、色彩、噪声、留白和渲染密度是否统一。
- 叙事焦点：每格是否只有一个主要信息点。
- 构图安全：中心 4:3 是否保留关键主体，运行时 cover 不裁断重点。
- 文本安全：无烘焙对白、伪文字、UI、logo、水印。
- 项目风格：不得滑向黄铜暖灯蒸汽朋克、都市 noir、低多边形、写实照片或恐怖娃娃。

任一项失败，优先用图生图 / 局部修图 / 同锚点重跑修复，不直接进入 Approved。

### 6. Approved 与清单

只有页级通过后才允许同步 Approved。

Approved 同步、Unity 导入和 Registry 登记走 `p3-art-asset-production` 的 Approved gate（静态 Sprite 入口为 `tools/美术工具/Invoke-ArtApprovedUnityRegistration.ps1` 的 `Plan -> SyncApproved -> Finalize`）：由它保护 `.meta` / GUID，并在 Finalize 时刷新 `美术文档/_generated/可接入素材清单.md` / `.json` 和 `art_integration_snapshots/`。不要手工复制文件或手写这些生成物。

本 skill 额外要求：写明本轮是新图、同 VisualID 替换、图生图修正、inpaint，还是仅筛选。

### 7. 运行时验收

运行时验收必须用当前 Approved 版本截图，不得沿用旧素材截图。

至少检查：

- Panel 全部加载，无 missing sprite、蓝屏、空白。
- 黑色 gutter / 多格排版成立。
- reveal 顺序符合分镜。
- 字幕条在安全区且不遮挡脸、核心、关键道具。
- 只有目标页出现目标 Action。
- 漫画播放结束进入真实玩家流程。

T0-01A 必须按 `19` 的截图清单产出 `t0_comic_p01_black_wake.png` 到 `t0_comic_p06_start_no0.png`，或按目标细案的最新截图清单执行。

## 状态声明口径

按以下层级声明，不得越级：

| 层级 | 可声明内容 |
|---|---|
| `规格完成` | 分镜、Page、Panel、VisualID、验收口径已写入事实文档。 |
| `锚点完成` | 角色、场景、风格锚点已选定并可约束后续生产。 |
| `候选完成` | `_IncomingAI` 候选和 contact sheet 已生成。 |
| `页级通过` | 单页 mock / page contact sheet 阅读顺序和一致性通过。 |
| `素材完成` | Panel 已进入 Approved，清单和快照已刷新。 |
| `接入完成` | VisualID 已登记，运行时漫画页能加载和逐格显示。 |
| `验收通过` | 当前 Approved 的运行时截图 / Play 路径证明漫画页正确播放并进入真实流程。 |

如果只做了批量文生图，最多声明 `候选完成`。

如果只把图放入 Approved，最多声明 `素材完成`。

如果有旧运行时截图但后续覆盖过素材，只能声明 `旧版本条件证据存在`。

## 复盘要求

每轮实际生产或验收结束后，必须记录：

- 使用的后端和模型。
- 哪些图是纯文生图、图生图、inpaint 或同 VisualID 替换。
- 哪些锚点约束了哪些 Panel。
- 哪些图因一致性、构图、伪文字、风格漂移或主体裁切被拒绝。
- 当前最高可声明层级。
- 下一轮最小修复集。

状态回写到 `agent_status/art.md`。影响 T0 细案、实现切片或全局体验口径时，再同步对应版本规划 / director 状态页。
