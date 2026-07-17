---
id: agent_status_art
title: 美术 / UI 状态
type: status
role: 美术
domain: art_pipeline
status: active
source_of_truth: true
related:
  - 版本规划/09_正式版核心纵切开发路线.md
  - agent_status/README.md
  - PROJECT_STATUS.md
  - 开发文档/00_程序开发大纲.md
  - 开发文档/14_Unity运行时美术自动验收方案.md
  - 开发文档/rules/03_视觉资源系统程序开发规范.md
  - agent_status/program.md
  - 设计文档/GDD/GDD_00_系统关联总图.md
  - agent_status/design.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - 美术文档/05_AI图片网关接入方案.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
  - 美术文档/ui_design/ui_iteration_process.md
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/README.md
  - 知识库/views/art.md
  - 美术文档/16_Live2D角色动画资产接入规格.md
  - 美术文档/17_Agent原生动态立绘资产接入规格.md
  - 美术文档/18_CG底图与漫画式播放演出工作流.md
  - 美术文档/19_T0-01序章CG细案.md
  - 美术文档/20_GIF小循环人物替换工作流.md
  - .codex/skills/p3-generate-image/SKILL.md
  - .codex/skills/p3-art-asset-production/SKILL.md
  - .codex/skills/p3-narrative-cg-comic/SKILL.md
  - 版本规划/0-12小时细案/T0-01_序章首次循环开发总方案.md
  - 美术文档/人设/README.md
  - 美术文档/人设/01_人设参考获取规则.md
  - 美术文档/人设/02_零号原型参考_失明少女.md
  - 美术文档/人设/03_零号初版人设方案.md
  - 美术文档/人设/04_零号AI后端出图提示词对比.md
  - 美术文档/人设/05_零号立绘素材设计与交付清单.md
  - 开发文档/17_Live2DSpine运行时接入评估.md
  - 开发文档/18_全局叙事播放系统开发方案.md
last_verified: 2026-07-18
update_rule: 美术或 UI 视觉流水线任务完成后更新本文件。
---

# 美术 / UI 状态

## 2026-07-18 GIF 小循环人物替换流水线设计

- 最近完成：用户已批准 `8-30` 帧小循环 GIF 人物替换流程，独立设计文档为 `美术文档/20_GIF小循环人物替换工作流.md`。首版采用 `0-N` 张参考图 + 必填文字描述，文字优先级最高；默认用 `gemini_chat_image` 整帧图生图，各帧完全独立生成，通过身份帧 / 动作帧双预审后才运行完整批次，并保留拆帧、请求、重试、质量风险和 GIF 重编码证据。
- 当前关注：设计已通过复核，逐任务 TDD 实现计划位于 `docs/superpowers/plans/2026-07-18-gif-character-replacement.md`；尚未新增 `Invoke-GifCharacterReplace.ps1`、时间轴解析、独立逐帧生成、风险检测或编码实现，也未执行真实 GIF smoke。
- 下一步建议：按实现计划选择 subagent-driven 或 inline execution；实现应位于 `tools/美术工具/`，复用图片网关而不把 P3 工作区和 GIF 编排逻辑写入 `tools/ai-image-gateway` 子模块。
- 问题 / 阻塞：无实现阻塞；透明 GIF 的新人物轮廓可能超出原 alpha，首版只能报告 `transparent_silhouette_limited`，不能保证复杂遮挡下的像素级轮廓。

## 2026-07-14 p3-art-asset-production 新 Skill

- 最近完成：新增 `.codex/skills/p3-art-asset-production/`，并完成美术生产文档职责重构：`00` 保留端到端阶段与三轴完成状态，`03` 改为稳定质量 / 筛选准入事实，`05` 收窄为图片生成能力与网关契约，`01/02/04` 补齐 SourceType / Operation、Asset Contract、正式 SelectedPath、`.meta` / GUID、Unity live import 和 provider 语义边界；图片能力 Skill 已以 `generate-image` 收口。
- 当前关注：正式美术生产由 `p3-art-asset-production` 编排并调用 `generate-image` 和 `p3-art-validation`。默认 `interactive + auto_until_decision`；用户明确授权全自动后，可在锁定事实和权限范围内自动 selected、同步 Approved、接入 Unity并完成运行时证据。
- 下一步建议：用一个低风险图标或同 VisualID 替换任务试跑新 Skill，验证 ProductionRun、评分决策、正式 SelectedPath、Approved 门禁、Unity 导入和 focused ArtRun 证据是否按文档闭环；不要直接从核心角色母版开始首跑。
- 问题 / 阻塞：本轮只迭代 Skill 与事实文档并归档旧验收入口，没有执行具体资产生产、修改 Manifest / Approved / Registry 或产生新的运行时验收结论。

## 2026-07-12 p3-art-validation 删除人工主美状态

- 最近完成：按用户决定删除 Art Profile、ArtRun、Skill 和发布聚合中的 `ExternalReview`、`ReviewRequired`、主美批准与外部复核状态；美术验收只输出运行时技术结果和证据。
- 当前关注：保留 MCP live inspection、bounded UGUI 诊断、capture ticket、before/after、seal 和 regression，不建立人工审核状态机。
- 下一步建议：直接根据实时画面和 ArtRun 证据继续修改或结束本次美术任务；需要永久修改 UI 时再由程序补目标 persist adapter。
- 问题 / 阻塞：当前无人工状态阻塞；具体 TargetID 仍未注册业务持久化 adapter。

## 2026-07-12 零号高频对话差分 Gemini 首批

- 最近完成：以既有 `zero_dialogue_neutral` 作为唯一参考母版，使用 `gemini_chat_image` 串行生成高频对话差分；新增说话、接受指令、疑惑、思考、信任柔和、冷淡、疲惫 `7` 张成功候选，与中性母版组成 `8` 格对话对照。采用文件、提示词和 contact sheet 位于 `美术文档/人设/AI出图/zero_dialogue_differences_20260712_01/`，筛选证据为同批次 `selection_review.md`。
- 当前关注：八格差分已经能区分基本对话节奏，但仍是白底 RGB 候选；`trust_soft` 手指需要清理，`cold` 与中性差异偏弱，所有图仍需统一头部尺寸、披肩下摆和人物占比。Gemini 成功输出为 `1376x768` 横图，selected 版本仅中心裁切并缩放到 `1024x1536`。
- 下一步建议：先修复 `trust_soft` 手部并增强 `cold` 疏离朝向；通道恢复后单图补跑 `zero_low_san`，再补 `zero_alert` 与 `zero_hurt`。日常对话差分通过主美筛选后再进入透明底、嘴型 / 手势分层和实际 UI 容器检查。
- 问题 / 阻塞：`zero_low_san` 使用完整提示词两次、低分辨率参考图加短提示词一次，三次均在约 125 秒返回 Gemini 中转站 `HTTP 524`，本轮记录为 `validation_limited:gemini_524`。本批未修改 Approved / Manifest / Registry，也没有 ArtAcceptance 或 DollPuppetAcceptance 证据。

## 2026-07-12 叙事 CG / 漫画页生产 skill

- 最近完成：新增 `.codex/skills/p3-narrative-cg-comic/SKILL.md`，把 `美术文档/18_CG底图与漫画式播放演出工作流.md` 的执行部分收敛为项目级 skill：先锁角色 / 场景 / 风格三类锚点，再按页生成候选、做 page contact sheet、一致性检查、Approved 同步和运行时漫画截图验收；同时在 `AGENTS.md` 美术 / UI 路由中登记该 skill。
- 当前关注：该 skill 明确禁止未锁锚点时直接全量独立文生图，并要求记录每张图是纯文生图、图生图、inpaint 还是同 VisualID 替换；实际出图仍调用 `.codex/skills/p3-generate-art/SKILL.md`，运行时截图验收仍调用 `.codex/skills/p3-validation/SKILL.md`。
- 下一步建议：后续继续 T0-01A 封板 CG 时，先按新 skill 建立零号角色锚点、破败工坊场景锚点和漫画页风格锚点，再重做 p02-p05 页级候选；不要直接用散点 Panel 批量覆盖 Approved。
- 问题 / 阻塞：本轮只新增 skill 和路由口径，没有生成新图片、修改 Approved / Manifest / Registry、刷新 `_generated` 或产生 ArtAcceptance / 运行时验收结论。

## 2026-07-11 零号 P0 身份锚点 Gemini 首批

- 最近完成：使用 `gemini_chat_image` 与既有 Gemini 动作候选逐张图生图，完成 `zero_stand_neutral`、`zero_dialogue_neutral`、`zero_maintenance_sit` 三张身份锚点首批筛选；最终采用文件和对照图位于 `美术文档/人设/AI出图/zero_portrait_p0_20260711_01/selected/`，筛选证据为同批次 `selection_review.md`。
- 当前关注：三张采用图已修正胸前系结、披肩罩裙化、维护侧坐 / 腮红 / 披肩长尾等明显漂移，但仍是白底候选。Gemini 网关请求 `1024x1536` 时连续返回 `1536x768` 横图，selected 版本仅中心裁切并缩放为 `1024x1536`，不等于原生竖版透明母图。
- 下一步建议：先对三张候选做同脸、披肩剪裁和脚部比例复核，再生产透明母图；通过实际 UI 容器尺寸检查后，继续制作 `zero_dialogue_command_ready`、`zero_low_san` 和 `zero_hurt`，最后进入 DollPuppet 分层。
- 问题 / 阻塞：尚未生成透明底、未去除 JPEG 痕迹、未进入 Approved / Manifest / Registry，也没有 Unity ArtAcceptance 或 DollPuppetAcceptance 证据。

## 2026-07-11 P3 AI 生图统一 skill 与规范 V2

- 最近完成：新增 `.codex/skills/p3-generate-art/`，将设计图 / 概念图、Manifest 文生图、Gemini 图生图差分、NovelAI inpaint、三后端提示词格式、单图循环和证据边界收敛为统一 Agent 入口；`美术文档/05_AI图片网关接入方案.md` 已升级为 V2，并同步 `00`、`02`、`03`、美术 README、Formal V2 总方案 / README / concepts 入口、`AGENTS.md`、`GEMINI.md`、美术工具 README 和三后端配置模板。
- 当前关注：设计图 / 概念图默认优先 Codex `image_gen`，不可用时自动转 `openai_images`；已有图整体成立但局部有问题或需要差分时优先 `gemini_chat_image` 图生图；NovelAI inpaint 只用于有 mask、允许一定随机性且指向性要求不强的局部重绘。
- 下一步建议：后续 AI 美术任务先触发 `$p3-generate-art`，按 skill 读取目标资产事实来源、执行配置检查 / smoke，并按 `raw -> processed -> selected -> Approved -> Registry -> ArtAcceptance` 留证据。
- 问题 / 阻塞：本轮只迭代 skill、规范和配置模板，没有生成新图片、修改 Approved / Manifest / Registry 或产生运行时验收结论。

## 2026-07-11 零号 Gemini 母版立绘素材设计

- 最近完成：按用户确认将零号后续立绘改为以 Gemini 版本为视觉母版；新增 `美术文档/人设/05_零号立绘素材设计与交付清单.md`，定义 3 个基础姿势、P0 六态静态立绘、P1 关系 / 恢复差分、P2 特殊 cut-in、状态组合矩阵、DollPuppet 分层和生产验收顺序。
- 当前关注：现有 Gemini 图只锁身份与轮廓，不直接入库；正式母图必须放大到画布高度 `85%–90%`，修正披肩罩裙化风险，并输出 `1024x1536` 透明立绘。现有 `doll_proto_0_stand` 继续作为 fallback VisualID，不在本轮新增 Manifest 条目。
- 下一步建议：先重绘 Gemini 正面母版、3/4 对话母版和维护坐姿母版，再补 P0 六态；静态尺寸验收后才进入 DollPuppet 分层，不从当前 AI JPG raw 直接拆层。
- 问题 / 阻塞：当前只是素材设计完成，未生成新的透明母图、Approved 素材、Manifest / Registry 条目、DollPuppet 包或运行时验收证据。

## 2026-07-11 AI 后端连通性复测工具

- 最近完成：新增 `tools/美术工具/Test-AIImageBackends.ps1` 与 `tools/美术工具/test_ai_image_backends.py`，用于按 `tools/ai-image-gateway/config.local.yaml` 对 `chatgpt` / `openai_images`、`gemini` / `gemini_chat_image`、`novelai` 三个后端做最小真实出图 smoke；同步更新 `tools/美术工具/README.md` 用法。
- 当前关注：工具默认输出到系统临时目录 `P3BackendSmoke`，生成 `summary.json`、`config_summary.json`、`provider.log` 和最小样图，不写入 `_IncomingAI`、Approved、Manifest、Registry、`screen_layouts.json` 或 ArtAcceptance 结论。
- 下一步建议：更换 token、代理地址或模型名后，先运行 `.\tools\美术工具\Test-AIImageBackends.ps1 -CheckConfigOnly` 确认配置脱敏摘要，再运行完整 smoke；NovelAI 限流时使用 `-Backend novelai -Attempts 3 -RetryDelaySeconds 30` 单独复测。
- 问题 / 阻塞：新工具实跑时 ChatGPT 与 Gemini 通过；NovelAI 当前连续限流，工具正确返回失败码并把 provider 详细日志写入 `provider.log`。

## 2026-07-11 AI 出图三后端可用性复测

- 最近完成：按用户更换 ChatGPT token 后的要求，重新实测 `novelai`、`openai_images` / ChatGPT 和 `gemini_chat_image` 三个 AI 出图后端。`openai_images` 使用 `gpt-image-2` 首次生成成功；`gemini_chat_image` 使用 `gemini-3.1-flash-image` 首次生成成功；`novelai` 使用 `nai-diffusion-4-5-full` 首次遇到限流，第二次重试生成成功。
- 当前关注：三后端当前均可用，但 NovelAI 仍有明显限流波动，批量任务需要保留重试、串行节流或更长 backoff；本轮只做后端 smoke，不新增 Approved 素材、Manifest、Registry、`screen_layouts.json` 或 ArtAcceptance 结论。
- 下一步建议：后续大批量角色 / CG 出图优先把 ChatGPT 与 Gemini 作为稳定通道，NovelAI 用于风格补充或小批量重试；若 NovelAI 连续限流，先降并发与单批数量，再决定是否更换 token / 账号。
- 问题 / 阻塞：无 ChatGPT token 阻塞；NovelAI 当前状态为 `usable_with_rate_limit_risk`。
- 关键证据：复测摘要 `C:\Users\WhiteSheep\AppData\Local\Temp\P3BackendSmoke\20260711_113054_full_retest\summary.json`；输出样例 `chatgpt_attempt1.png`、`gemini_attempt1.jpg`、`novelai_attempt2.png` 位于同目录。

## 2026-07-11 T0-01A 封板 CG Panel 审计与后端 smoke

- 最近完成：已按 `18_CG底图与漫画式播放演出工作流.md`、`19_T0-01序章CG细案.md` 和 `T0-PRE-02..06` 重审 15 个 Approved Panel。结论为 10 个需重制、5 个保留候选；逐 Panel 原因已写入 `美术文档/19_T0-01序章CG细案.md` 第 11 节。
- 当前关注：按页重制 `p02 -> p03 -> p04 -> p05_panel01`，每页先进 `_IncomingAI`、预处理和 contact sheet，人工选定后才允许同 VisualID 覆盖 Approved。
- 问题 / 阻塞：已确认美术流水线必须显式使用 `tools/ai-image-gateway/config.local.yaml`；指定该配置后 Gemini 生成成功。p02 首批 9 张候选已完成，但债务纸仍有伪文字，零号手部仍过度骨架机械化，因此本批 `0/3` Panel 可覆盖 Approved。
- 关键证据：`art_integration_snapshots/20260711_133613_generation_t0_01a_seal_p02_gemini_v1_20260711.md`、`20260711_133744_processed_candidate_t0_01a_seal_p02_gemini_v1_20260711.md`；页级审核结论见 `美术文档/19_T0-01序章CG细案.md` 第 11.1 节。

## 2026-07-11 T0-PRE 商业化基线设计补齐

- 最近完成：`T0-PRE` 商业化基线已写入 `版本规划/0-12小时细案/T0-01_序章首次循环开发总方案.md` 第 3.1 节。美术 / UI 后续承接完整 T0 时，以 16 张目标截图作为画面基线：黑屏醒来、破败工坊、债务 / 手记 / 核心碎片、发现并启动零号、苏醒照看、半开放工坊、首潜许可、浅缘短遭遇、首件带回物、回城照看和下一轮目标。
- 当前关注：本轮只完成目标画面设计，不新增 Approved 素材、Manifest、Registry、`screen_layouts.json` 或 ArtAcceptance 证据。后续 A 段封板候选仍需处理漫画页排版、对白 UI 皮肤、启动仪式画面、半开放工坊和首潜许可卡；B/C 段需新增浅缘场景、带回物和回城照看画面。
- 下一步建议：美术 / UI 承接 T0 时先按 `T0-PRE-01..16` 判断每张画面的主视觉、允许 UI 和禁止 UI，再决定是否补 CG、背景、结果卡、许可卡或 FormalV2 工坊子态。
- 问题 / 阻塞：无新增素材阻塞；但不能把现有 T0-01A 条件通过截图视为完整 T0 商业化美术封板。

## 2026-07-11 T0-01A 序章 CG 按 18/19 验收复核

- 最近完成：按 `美术文档/18_CG底图与漫画式播放演出工作流.md` 与 `美术文档/19_T0-01序章CG细案.md` 复核当前 T0-01A 序章 CG。结论：`规格完成=通过`；`素材完成=通过`，p02-p06 共 15 张 Panel 已入 `UnityClient/Assets/Art/Approved/NarrativeCG/T0/`，p02-p06 均为 `1920x1080`，p01 为 `1376x768` 临时醒来底图；`接入完成=条件通过`，VisualID 已登记且 `P3DialogueOverlayController` 有 p02-p06 漫画页定义；`验收通过=未通过/未完成`。
- 当前关注：7 月 8 日 `UnityClient/Logs/T0Validation/t0_val_01_final_contact_sheet_latest.png` 可证明旧版本 10 个语义节点能走完，但它早于 7 月 10 日风格纠偏覆盖，不能证明当前最新 Approved 面板的最终播放效果。`UnityClient/Logs/T0Validation/` 仍缺少 19 第 8 节要求的 `t0_comic_p01_black_wake.png` 到 `t0_comic_p06_start_no0.png` 六张专门漫画页验收截图。
- 下一步建议：优先重跑当前 Approved 版本的运行时漫画页截图，逐张检查黑色 gutter、多格排版、逐格 reveal、字幕安全区、p06 单一 `启动人偶` 动作，以及播放结束是否回到真实 T0 流程；通过后再标记 `验收通过`。
- 问题 / 阻塞：当前画面质量仍有美术风险，p03/p04 的债务、手记、浅层暗门和核心碎片仍偏地底洞窟奇幻插画，弱于“破败但干净的手作工坊 + 安静压力”；p05/p06 零号风格已明显纠偏，但最终人设未完全锁定，仍只能作为 T0 序章 CG 纠偏素材，不是最终角色母版。
- 关键证据：Approved 汇总图 `UnityClient/Assets/Art/_IncomingAI/_page_reviews/t0_01a_acceptance_current_approved_contact_20260710.png`；风格纠偏 review `UnityClient/Assets/Art/_IncomingAI/_page_reviews/t0_01a_style_correction_review_20260710.md`；接入快照 `美术文档/_generated/art_integration_snapshots/20260710_235213_t0_01a_cg_acceptance_20260710.md`；旧运行时条件截图 `UnityClient/Logs/T0Validation/t0_val_01_final_contact_sheet_latest.png`。

## 2026-07-08 T0-VAL-01 FormalV2 运行时纵切条件通过

- 最近完成：`T0-VAL-01` 在第 15 节人工否决后完成正式版运行时重做，当前美术 / UI 口径为 `conditional_pass:t0_val_01_formal_v2_runtime_slice`。最新 final capture report 为 `UnityClient/Logs/T0Validation/t0_val_01_final_capture_report.txt`，`captured_at=2026-07-08 00:46:12`、`count=10`、`semantic_failed=False`；contact sheet 为 `UnityClient/Logs/T0Validation/t0_val_01_final_contact_sheet_latest.png`。
- 当前关注：本结论只证明从开局人偶状态、过程 CG / 对白、发现并启动零号、苏醒、半开放工坊、首潜许可卡到第一层地图的运行时纵切已可见；不等于完整 T0 序章、最终 CG / Live2D、ArtAcceptance 或主美商业化封板完成。
- 下一步建议：若继续追最终商业化封板，优先处理漫画页黑场 / 多格排版、对白框 `AUTO / LOG` 旧 UI 感、半开放工坊最终视觉、首潜许可卡仪式感，以及 ArtAcceptance / 主美人工截图验收。
- 问题 / 阻塞：当前不再是“无序章 / 无工坊 / 无许可”的 P0 主链阻塞；剩余风险集中在美术 polish 与最终验收封板，不能把条件通过扩大为美术最终通过。

## 2026-07-08 T0-01A 序章 CG Approved 首版素材完成

- 最近完成：在用户确认允许使用项目 AI 图片网关与 fallback provider 后，恢复 T0-01A 序章 CG 制作；已用 `gemini_chat_image` 追加修正版候选，并将 p02-p06 共 15 张漫画 Panel 以及 p01 醒来底图整理到 `UnityClient/Assets/Art/Approved/NarrativeCG/T0/`。本轮重点替换了 p02 工坊远景、p05 零号半身 / 核心近景、p06 零号近景 / 核心接入 / 核心唤醒等弱格，严格保留 Unity `.meta`。
- 当前关注：该成果可标记为 `素材完成`，不等于 `接入完成` 或 `运行时验收通过`；当前 `Generate-ArtIntegrationCandidates` 仍显示 `program_integrate=0`、`acceptance_needed=290`，后续需要程序 / UI 侧把漫画页 Page、VisualID、逐格显现和字幕条接入运行时。
- 下一步建议：进入 T0-01A 漫画式播放接入任务，按 `美术文档/19_T0-01序章CG细案.md` 第 8 节生成 `t0_comic_p01_black_wake.png` 到 `t0_comic_p06_start_no0.png` 六张运行时截图，并检查 `启动人偶` 页是否只有单一动作按钮。
- 问题 / 阻塞：无美术出图阻塞；剩余风险是 p03 抵扣器械阴影较抽象、零号最终人设未完全锁死，后续角色定稿后可能需要同 VisualID 质量替换。
- 关键证据：最终总览图 `UnityClient/Assets/Art/_IncomingAI/_page_reviews/t0_01a_approved_contact_20260708.png`；美术 review `UnityClient/Assets/Art/_IncomingAI/_page_reviews/t0_01a_prologue_cg_approved_review_20260708.md`；快照 `美术文档/_generated/art_integration_snapshots/20260708_004354_approved_sync_candidate_t0_01a_cg_fix_gemini_20260708_02.md` 与 `20260708_004816_approved_sync_candidate_t0_01a_cg_fix_gemini_20260708_03.md`。

## 2026-07-07 T0-01A 序章 CG p02 页级候选筛选

- 最近完成：按 B 方案运行时漫画页口径，复核了 p02 三个现有 `_IncomingAI` Gemini 候选：`cg_t0_01a_p02_panel01_workshop_wide`、`cg_t0_01a_p02_panel02_debt_notice_door`、`cg_t0_01a_p02_panel03_no0_hand_cloth`。页级 review 已写入 `UnityClient/Assets/Art/_IncomingAI/_page_reviews/t0_01a_p02_workshop_establish_review_20260707.md`。
- 当前关注：p02 三格阅读顺序成立，债务纸和零号手部为强候选；工坊远景可作临时气氛候选，但更像洞穴工坊，生活压力和破败室内感不足，进入 Approved 前需要同 VisualID 追加变体。
- 下一步建议：如用户确认允许 fallback 出图后端，优先补 `cg_t0_01a_p02_panel01_workshop_wide` 的 2-3 张变体，并补 p03-p06 剩余 11 个 Panel 候选；之后再做页级 contact sheet、selected 拷贝和 Approved 同步。
- 问题 / 阻塞：当前 Codex 未暴露内置 `image_gen`；按项目美术规则，未获用户确认前不能自动切到 NovelAI / AI 图片网关 / mock。`ART-CG-04` 与 `ART-CG-05` 已在本地 mission 记录为 blocked；没有新增 Approved、Registry 或运行时验收结论。
- 关键证据：`美术文档/_generated/art_generation_plan_snapshots/20260707_010311_t0_01a_cg_followup_candidates_20260707.md` 显示剩余候选计划 `planned=11`、`prompt_ready=11`；p02 review 文件记录三格保留/重跑判断。

## 最后更新

2026-07-12

## 当前关注

支撑正式版核心纵切。按最新 `09` 路线，美术 / UI 作为 P5 表现支撑，只围绕当前 P0-P4 功能纵切补表达、资源和截图验收，不继续横向铺所有界面。

T0-01A 叙事播放 UI 当前已有 `NARR-03` 纯 UGUI 对白层代码入口，并已补第一版演出合成层。2026-07-08 美术侧已完成 T0-01A 序章 CG 首版 Approved 素材：p01 醒来底图与 p02-p06 共 15 张漫画 Panel 已位于 `UnityClient/Assets/Art/Approved/NarrativeCG/T0/`，最终总览图与美术 review 已写入 `_IncomingAI/_page_reviews/`。当前可声明 `素材完成`；`T0-VAL-01` 运行时纵切则已进入 `conditional_pass:t0_val_01_formal_v2_runtime_slice`，证据为 10 张 final capture 与 contact sheet。该条件通过不等于完整 T0 序章、最终 CG / Live2D、ArtAcceptance 或主美商业化封板完成。

2026-07-11 `T0-01A` Owner 设计已升级为封板候选 V5。美术 / UI 下一轮不是继续做散点 polish，而是按 `T0-PRE-01..11` 整体交付：重排 p02-p05 漫画页，制作启动零号近景仪式，收敛正式对白皮肤，将半开放工坊落到 FormalV2 `workshop_main` 同一房间，并把许可文件、暗门/井口和零号组成首潜许可仪式画面。
2026-07-07 已按用户确认的 B 方案新增 `美术文档/18_CG底图与漫画式播放演出工作流.md`，并新增 `美术文档/19_T0-01序章CG细案.md`。2026-07-08 的新增变化是素材层已从 `规格完成` 推进到 `素材完成`，运行时纵切已从第 15 节硬失败修复到条件通过；后续仍需按最终商业化封板要求继续处理漫画页排版、字幕条、专属对白 UI 皮肤和 ArtAcceptance / 主美验收。
`T0-FLOW-01` 开场前半段已由程序侧接入 Narrative 自动推进：黑屏、债务纸、修复手记、核心碎片和发现零号通过 VisualID / fallback 表现意图进入播放链路。当前已有正式 T0-01A CG Approved 素材可供继续替换和 polish；最新 T0-VAL-01 只证明运行时纵切成立，不把序章画面质量标记为最终通过。
`T0-FLOW-02` 程序侧已接入 `启动人偶` 单按钮、零号苏醒、状态小卡和 `擦去核心仓灰尘` 的 Narrative 表现命令链路：状态卡仍复用 `ui_status_card_prologue` / `ui_panel_main` fallback，启动 VFX 复用 `vfx_no0_core_start` / `ui_core_glow` fallback。2026-07-05 已产出启动按钮与擦灰动作 live 截图；本轮没有新增 Approved 素材、Manifest、Registry 或 active `screen_layouts.json`，因此美术侧只记录 staging 可见，不标记商业画面验收通过。
`T0-FLOW-03` 程序侧已接入半开放工坊与浅层入口主行动：工坊使用现有 UGUI 面板和按钮皮肤进入 `PrologueHalfOpen` 状态，隐藏市场、完整维护、义体、底盘、订单、传闻、势力等入口，只保留 `浅层入口` 主行动、零号状态提示和压力文案。2026-07-05 已产出半开放工坊 live 截图；本轮没有新增 Approved 素材、Manifest、Registry 或 active `screen_layouts.json`，因此美术侧只记录 staging 可见，不标记商业画面验收通过。

`T0-FLOW-04` 程序侧已接入首潜第一层确认态：复用现有 `layer_select` / `DungeonStartLayerUIController` UGUI 面板，只显示第一层，按钮改为 `出发 / 再看她一眼`，并显示许可通过或阻断的玩家可读中文文案。2026-07-05 已产出允许态与磨损极高阻断态 live 截图；本轮没有新增 Approved 素材、Manifest、Registry 或 active `screen_layouts.json`，因此美术侧只记录 staging 可见，不标记商业画面验收通过。

PM 版本节点中，美术线当前 21 个界面都已具备 Formal V1 active 规格。美术侧已主动触发 latest ArtAcceptance `20260527_002436`，工具层 21/21 captured、`PASSED`、Registry 191、MissingRequiredVisualIDs=0、UI snapshot risks=0；latest 程序交接清单为 `program_integrate=0`、`add_capture=0`、`rerun_acceptance=0`。本轮人工验收结论是“资源接入通过、画面不完全通过”：`combat_hud`、`dungeon_map`、`safe_room`、`stairs_room`、`sell_panel`、`prosthetic_panel` 需要程序侧清理截图状态或补有效展示数据后重跑；其余界面多为通过或条件通过，后续继续 Visual V2 同名替换。

2026-06-07 已由美术侧直接修复 ArtAcceptance 程序截图问题：每个截图点前新增清场钩子，`combat_hud` 改为重开验收层并直接绑定 CombatNode 进入战斗，`ui_snapshot` 改为只记录当前可见/有效 UI 元素。修复已提交为 `771149e fix art acceptance capture cleanup`，C# 编译通过；本地 Unity Editor 未消费 `RUN_ART_ACCEPTANCE` 触发，仍需下次 Unity 重跑确认。当前预期剩余阻塞是 3 个怪物战斗图缺失：`monster_boss_gatekeeper_mk1_combat`、`monster_mob_lost_miner_echo_combat`、`monster_mob_rust_hound_combat`。

当前新增重点是 Formal V2 UX/UI 重构。Formal V1 证明了功能区域、VisualID 和截图链路可运行，但整体体验仍偏按钮菜单 / debug 面板。Formal V2 先作为 draft 设计层推进，不修改 active `screen_layouts.json`，不触发素材生成，也不要求程序接入；V2-A 优先重审 `workshop_main`、`combat_hud`、`inventory_loot`、`dungeon_map`、`settlement` 五个核心主流程界面。

2026-06-08 用户已评审并认可 `美术文档/ui_design/formal_v2/concepts/review_index.md` 当前 Formal V2 概念方向。Formal V2 进入 V2-A active 迁移准备阶段：下一步按 P3 mission `ART-V2-02` 先迁移 `workshop_main` / `workshop_studio`、`combat_hud`、`inventory_loot`、`dungeon_map` 和 `settlement` 到 active `screen_layouts.json`；V2-B / V2-C 暂不作为本批次程序接入口。

2026-06-08 V2-A active 迁移已完成：`workshop_main`、`combat_hud`、`inventory_loot`、`dungeon_map`、`settlement` 已在 `美术文档/ui_design/screen_layouts.json` 标记为 `StructureVersion=FormalV2`，并通过 `Validate-UIDesign.ps1`；`美术文档/ui_design/_generated/ui_design_handoff.md` 已刷新。`workshop_main` 已移除主界面直接背包组件依赖，背包 / 底盘 / 义体 / 维护后续由 `workshop_studio` 方向承接。

2026-06-08 已执行 V2-A active 后的生成物刷新：`Update-ArtManifest.ps1` 后 Manifest 为 226 条，`Generate-ArtPrompts.ps1` 更新 29 条提示词，`Generate-ArtIntegrationCandidates.ps1` 输出 `program_integrate=0`、`generate_needed=29`、`acceptance_needed=191`；`Generate-FormalV1AcceptanceQueue.ps1` 现在只统计 16 个仍为 FormalV1 的 active 屏，`Generate-ArtProgramHandoff.ps1` 输出 `program_integrate=0`、`add_capture=0`、`rerun_acceptance=16`。

2026-06-08 已按 FormalV2 全量素材实际生产口径推进 NovelAI 批次，而不是停留在规划：缺图批次 `nai_formalv2_missing_20260608_01` 已以 NovelAI 串行方式生成 `58/58` 张真实图片，预处理 `58/58`，并同步到 Approved；latest 清单刷新为 `program_integrate=58`、`generate_needed=0`。其中新增运行时资源包含 23 个物品图标、34 个怪物 combat / portrait 资源和 `bg_dungeon_layer_3`。2026-06-09 已补齐这 58 个新缺图正式资产的 Manifest 质量账：全部标为 `QualityTier=formal_ai_v2`、`ReplacementBatchID=nai_formalv2_missing_20260608_01`，程序交接清单中 58 项质量列均为 `formal_ai_v2`。首批质量替换批次 `nai_formalv2_quality_20260608_01_p1_combat_readability` 已以 `concurrency=1`、`delay=1` 串行生成 10 张候选图，预处理后用 `meta_guard=strict` 同名替换 Approved，10 个战斗可读性 UI 资源已标为 `QualityTier=formal_ai_v2`，程序侧无需重新登记这些同名替换项。

2026-06-09 已完成 P1 核心背景质量替换批次 `nai_formalv2_quality_20260608_02_p1_core_backgrounds`：`bg_safe_room` 和 `bg_stairs_room` 各生成 4 张 NovelAI 候选，筛选后分别选用 `20260609_004.png` 与 `20260609_002.png`，预处理后以 `meta_guard=strict` 同名同步到 Approved，均为 `1920x1080` 且 alpha 全为 255，修复旧背景透明 / 半透明导致黑底穿透的技术风险。

2026-06-09 已完成 P1/P2 UI skin core 正式替换批次 `nai_formalv2_quality_20260609_01_p1_p2_ui_skin_core`：15 个 UI 面板 / 列表 / 节点盘 / 路线 / 结算徽记资源以 NovelAI 串行生成 `60/60` 张候选，预处理后人工筛选 15 张，并用 `meta_guard=strict` 同名替换 Approved；Manifest 均为 `QualityTier=formal_ai_v2`、`ReplacementBatchID=nai_formalv2_quality_20260609_01_p1_p2_ui_skin_core`、`CandidateBatchID=None`，程序侧无需重新登记这些同名替换项。

2026-06-09 已完成 P2 shared UI icons 正式替换批次 `nai_formalv2_quality_20260609_02_p2_shared_ui_icons`：31 个共享 UI 图标以 NovelAI 串行生成 `124/124` 张候选，预处理后人工筛选 31 张，并用 `meta_guard=strict` 同名替换 Approved；Manifest 均为 `QualityTier=formal_ai_v2`、`ReplacementBatchID=nai_formalv2_quality_20260609_02_p2_shared_ui_icons`、`CandidateBatchID=None`。本批次运行中 NovelAI 曾返回服务端限流，脚本按串行请求自动等待后完成，不是并发跑图。

2026-06-09 已完成 P2 scene backgrounds 正式替换批次 `nai_formalv2_quality_20260609_03_p2_scene_backgrounds`：`bg_doll_room_attic`、`bg_layer_select`、`bg_settlement_defeat`、`bg_settlement_victory` 各生成 4 张 NovelAI 候选，预处理后人工筛选并以 `meta_guard=strict` 同名替换 Approved；四张背景均为 `1920x1080`，alpha 全不透明，Manifest 均为 `QualityTier=formal_ai_v2`、`CandidateBatchID=None`。

2026-06-09 已完成 P2 economy / order / rumor / faction icons 正式替换批次 `nai_formalv2_quality_20260609_04_p2_economy_social_icons`：42 个经济、订单、传闻和势力图标以 NovelAI 串行生成 `168/168` 张候选，预处理后人工筛选并以 `meta_guard=strict` 同名替换 Approved；Manifest 均为 `QualityTier=formal_ai_v2`、`ReplacementBatchID=nai_formalv2_quality_20260609_04_p2_economy_social_icons`、`CandidateBatchID=None`。

2026-06-09 已完成 P2 growth / chassis / prosthetic / room memento assets 正式替换批次 `nai_formalv2_quality_20260609_05_p2_growth_room_assets`：17 个成长、底盘、义体和房间纪念物资源以 NovelAI 串行生成候选，预处理后人工筛选并以 `meta_guard=strict` 同名替换 Approved；部分条目因 NovelAI 临时断线 / 限流只有 1-3 张候选，但均有可用正式图。该批次已提交为 `0cfc13c art: replace FormalV2 growth room assets`。

2026-06-09 已完成 FormalV2 怪物头像 alpha 技术修复：新增 `tools/美术工具/fix_opaque_art_alpha.py`，按 `素材质量替换清单.json` 处理 `technical_fix` 队列，将 17 张 `AlphaRequired=false` 的 monster portrait Approved PNG alpha 通道统一修为 255，保持尺寸、Approved 路径和 Unity `.meta` 不变。刷新后 `素材质量替换清单` 为 `technical_fix=0`、`visual_v2_replace=0`、`spec_review=0`，`VisualV2生成计划` 为 `planned=0`、`prompt_ready=0`。

截至 2026-06-12，Formal V2 全局风格口径已纠偏为“日系二次元地底奇幻冒险 + 低信息密度”。机械、工坊和旧工具只作为维护、义体、底盘等局部系统语义；黄铜、铜件、暖灯和蒸汽朋克不再作为全局风格关键词，也不作为 active UI 或正向生图提示词的默认材料 / 灯光方向。2026-06-07 重出的概念图暂保留为结构和氛围参考，但其中棕金机械和黄铜暖灯倾向不再作为后续全局美术依据。`workshop_main` 仍保持魔偶中心安心房间，`combat_hud` 保持左人偶 / 右敌方 / 底部背包结构，`dungeon_map` 继续要求底图、路线和节点融合，避免海面底图或悬浮节点。概念图位于 `美术文档/ui_design/formal_v2/concepts/`，只用于结构和氛围评审，不作为 Approved 运行时素材、Manifest 条目或程序接入口。设计图 / 概念图默认必须用 Codex 内置 `image_gen`；若当前工具环境没有暴露 `image_gen`，美术智能体必须先提醒用户并等待确认，不能自动切到 NovelAI、AI 图片网关、mock 或本地脚本。

V2-B 七个局外功能界面已补齐详细草案，并按最新反馈修正语义：`maintenance_panel`、`prosthetic_panel`、`chassis_upgrade_panel` 归入 `workshop_studio` 内的可切换子面板或弹出窗口，不再作为独立大场景；`sell_panel` 改作小镇商店 / 市场交易界面，工坊卖出和出货分配由 `shop_staging` 承接。旧维护、义体、底盘升级和旧 `sell_panel` 概念图已归档到 `concepts/archive/2026-06-01_workshop_studio_and_shop_semantics/`；2026-06-06 已用内置 imagegen 按新语义重出 4 张概念图。

Formal V2 数量账：active UI 规格共有 21 个界面；21 个 active 界面的 V2 草案已全部补齐，并额外保留 `workshop_studio` 作为 `workshop_main` 的拆分方案。结构设计图已生成 22 张，位于 `美术文档/ui_design/formal_v2/design_boards/`，覆盖 21 个 active 界面和 `workshop_studio`；AI 概念参考图也已补齐 22 张，位于 `美术文档/ui_design/formal_v2/concepts/`。概念图评审索引和 4 张 contact sheet 已补到 `美术文档/ui_design/formal_v2/concepts/review_index.md` 与 `concepts/contact_sheets/`。这些设计图都不进入 Approved、Manifest 或程序交接清单。

美术侧已把“可接入覆盖”、“缺图生成”和“视觉质量替换”拆开：`可接入素材清单.md` 给程序看，`缺图生成计划.md` 给美术侧执行 `generate_needed` 新素材跑图，`素材质量替换清单.md` 给美术侧执行 local_v0 / placeholder 同名替换和技术修复。当前 `generate_needed=0`、`art_select=0`、`art_process=0`，新增资源交接为 `program_integrate=77`；质量清单为 `technical_fix=0`、`visual_v2_replace=0`、`spec_review=0`。历史本地生成 Approved 的 Visual V2 同名替换、怪物头像 alpha 技术修复和本轮 29 个新准入素材 Approved 入库均已完成，后续转入程序登记后的运行时截图验收。

美术侧已新增“需求候选扫描”前哨：`Scan-ArtRequirementCandidates.ps1` 会扫描最新设计文档、配置表、版本规划和 active UI 文档，生成 `美术文档/_generated/美术需求候选清单.md/json`。当前 latest 为 `new_candidate=55`、`approved_without_manifest=0`、`seed_only=0`、`manifest_managed=244`；本轮已从候选中准入 29 个 FormalV2 新素材需求，裁决见 `美术文档/14_FormalV2素材候选审查记录.md`。这些候选只用于人工审查，确认后才写入 `art_requirements_seed.json` 或等待正式配置字段落地，不自动进入 Manifest。

P1 战斗可读性 active 合同已补齐：`combat_hud` 现在包含怪物意图图标、战斗状态图标、命中 / 破盾反馈、封格 / 塞包 overlay。P1 结算结果 active 合同已补齐：`settlement` 现在包含胜利、HP 战败、SAN 崩溃、HP+SAN 复合战败和队伍溃败五态结果徽记。P3 底盘升级 active 合同已补齐：`chassis_upgrade_panel` 包含当前底盘、下一底盘、容量变化、材料缺口、蓝图前置和升级确认。P3 维护可读性 active 合同已补齐：`maintenance_panel` 现在把磨损修复、侵蚀净化和下潜许可拆成独立 VisualID。P3 Room Memento 已按 `44_局外成长人偶特质房间正式配置落地设计.md` 前置补齐 8 个房间纪念物 VisualID。P4 营业结算 active 合同已补齐：`business_settlement` 位于 `shop_staging` 和 `daily_bill_report` 之间，承接顾客流、成交反馈、未售出 / 黑市风险摘要和进入账单动作。P4 每日账单 active 合同已补齐：`daily_bill_report` 现在把收入、支出和月租债务拆成独立 VisualID。P4 经济压力传闻 / 势力 / 订单已按 `48_经济压力传闻正式配置落地设计.md`、`49_经济压力势力正式配置落地设计.md` 和 `50_经济压力订单正式配置落地设计.md` 前置补齐 42 个 VisualID。58 个 V2-A 缺图项已生成 Approved PNG，构成为 23 个物品图标、34 个怪物战斗 / 头像资源和 1 个背景；后续又从 FormalV2 候选准入中补齐 29 个新增 Approved PNG。latest 可接入清单当前为 `program_integrate=77`、`acceptance_needed=191`、`generate_needed=0`，质量替换和技术修复队列已清空。

Visual V2 执行入口已补齐：`美术文档/_generated/VisualV2生成计划.md/json` 当前 `planned=0`、`PromptReadyItems=0`，说明本轮 local_v0 / placeholder 的 Visual V2 同名替换已全部完成。NovelAI token 链路已完成 P0 新节点图标、P1 核心战斗意图图标、P1 首批战斗反馈 / 标记、本轮 P1 combat readability 10 项、P1 core backgrounds 2 项、P1/P2 UI skin core 15 项、P2 shared UI icons 31 项、P2 scene backgrounds 4 项、P2 economy / order / rumor / faction icons 42 项和 P2 growth / chassis / prosthetic / room memento assets 17 项替换；后续若再跑图仍继续串行生成，脚本每次请求 1 张图，图间隔 1 秒。

Visual V2 工具链已补齐安全替换流程：已接入素材可用 `Run-ArtGeneration.ps1 -PreserveStatus` 生成候选，用 `CandidateBatchID` 只预处理本批 raw，再用 `Sync-ApprovedArt.ps1 -QualityTier formal_ai_v2 -ClearCandidate` 同名替换 Approved。该流程不会把原 `approved` / `registered` / `validated` 状态回退到 `generated`。`Sync-ApprovedArt.ps1 -CandidateBatchID` 默认启用严格 `.meta` guard：目标 PNG 和目标 `.meta` 必须已存在，且同步前后 `.meta` 字节必须一致，确保正式图替换只改 PNG 内容，不要求程序侧重新登记同一资产。

美术文档已收敛为四层入口：`README.md` 只做导航，`10_正式版核心纵切美术路线.md` 作为当前规划入口，`00_美术流水线总览.md` 作为端到端资产生产工作流入口，`ui_design/README.md` 作为 UI 版本和 active 规格入口。`archive/` 保存 MVP 记录和旧批次交付快照。

Formal V1 / FormalV2 运行时验收已工具化：`Generate-FormalV1AcceptanceQueue.ps1` 会从 active `screen_layouts.json`、Manifest、latest ArtAcceptance 和 Registry 快照生成 `美术文档/ui_design/_generated/FormalV1验收队列.md/json`。程序侧交接已进一步收敛到 `美术文档/_generated/程序接入交接清单.md/json`，该清单合并 `program_integrate`、截图覆盖和 ArtAcceptance 重跑队列。当前 latest 程序交接为 `program_integrate=77`、`add_capture=0`、`rerun_acceptance=16`；本轮 29 个新准入素材已完成 NovelAI 生成、预处理、筛选 / fallback、Approved 同步和 `.meta` 补齐，可进入程序登记队列。

## 必读文件

- `知识库/views/art.md`
- `美术文档/README.md`
- `美术文档/10_正式版核心纵切美术路线.md`
- `美术文档/00_美术流水线总览.md`
- `美术文档/ui_design/README.md`
- `美术文档/ui_design/ui_iteration_process.md`
- `美术文档/ui_design/formal_v1/screen_structure_review.md`
- `美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md`
- `美术文档/01_Manifest规范.md`
- `开发文档/rules/03_视觉资源系统程序开发规范.md`
- `开发文档/14_Unity运行时美术自动验收方案.md`

## 工作边界

- 运行时 UI 目标是纯 UGUI。不要新增 UI Toolkit、UXML、USS 或 `UIDocument` 运行时流程。
- `美术文档/ui_design/screen_layouts.json` 是当前 active UI 对接规格；程序接入、素材生成和可接入素材清单只认 active。
- MVP UI 设计已冻结到 `美术文档/ui_design/versions/mvp_baseline_2026-05-22/`，作为历史基线和回退参考。
- Formal V1 先写在 `美术文档/ui_design/formal_v1/`；用户确认后再逐界面修改 active。
- Formal V2 先写在 `美术文档/ui_design/formal_v2/`；用户确认并写入 active 前，只是 UX/UI 设计草案，不作为程序接入口或素材生成入口。
- Formal V2 概念图放在 `美术文档/ui_design/formal_v2/concepts/`，只作为评审参考，不进入 `UnityClient/Assets/Art/Approved`、Manifest 或程序接入清单。设计图 / 概念图默认用 Codex 内置 `image_gen` 生成；如果当前工具环境没有暴露 `image_gen`，必须先提醒用户并等待确认，不能自动改用 NovelAI、AI 图片网关、mock 或本地脚本。
- `versions/formal_v1_candidate/` 是复杂界面的可选暂存区，不是必经流程。
- `美术文档/art_requirements_seed.json` 维护配置表无法扫描出的视觉需求。
- `美术文档/_generated` 与 `美术文档/ui_design/_generated` 是生成输出。
- 正式运行时资源放在 `UnityClient/Assets/Art/Approved`。
- AI 出图工作区 `UnityClient/Assets/Art/_IncomingAI` 保持忽略。
- 每次 AI 出图、预处理或 Approved 同步完成后，都必须维护一版可接入素材清单：`美术文档/_generated/可接入素材清单.md/json` 作为 latest，`美术文档/_generated/art_integration_snapshots/` 作为历史快照。只有明确说明“不更新交付清单”的本地调试才允许使用 `-SkipIntegrationCandidates`。

## 常用命令

```powershell
.\tools\config\Sync-Configs.ps1 -Clean
.\tools\美术工具\Update-ArtManifest.ps1
.\tools\美术工具\Scan-ArtRequirementCandidates.ps1
.\tools\美术工具\Generate-ArtPrompts.ps1
.\tools\美术工具\Validate-UIDesign.ps1
.\tools\美术工具\Scan-UIIterationCandidates.ps1
.\tools\美术工具\Generate-ArtIntegrationCandidates.ps1
.\tools\美术工具\Generate-ArtBatchPlan.ps1
.\tools\美术工具\Generate-ArtQualityBacklog.ps1
.\tools\美术工具\Generate-VisualV2Plan.ps1
.\tools\美术工具\Generate-LocalV0Art.ps1
.\tools\美术工具\Generate-FormalV1AcceptanceQueue.ps1
.\tools\美术工具\Generate-ArtProgramHandoff.ps1
```

## 最近完成

- 2026-07-11：`T0-01` 文档体系已新增完整开发总入口 `版本规划/0-12小时细案/T0-01_序章首次循环开发总方案.md`。美术 / UI 侧后续按 A/B/C 分段承接：A 段继续处理漫画页、对白皮肤、启动仪式、半开放工坊和许可卡；B 段另行承接浅缘场景、短遭遇和首件带回物；C 段另行承接回城照看、结果三行结构和下一轮目标画面。`T0-VAL-01` 只作为 A 段运行时验收记录，本轮不新增 Approved、Manifest、Registry 或 ArtAcceptance 结论。
- 2026-07-08：`T0-VAL-01` 已从第 15 节人工否决后的硬失败修复到 `conditional_pass:t0_val_01_formal_v2_runtime_slice`。最新 `UnityClient/Logs/T0Validation/t0_val_01_final_capture_report.txt` 为 `captured_at=2026-07-08 00:46:12`、`count=10`、`semantic_failed=False`，contact sheet 为 `UnityClient/Logs/T0Validation/t0_val_01_final_contact_sheet_latest.png`。美术 / UI 当前可承认运行时纵切已可见：过程 CG / 对白、启动零号、半开放工坊、首潜许可和进入第一层均有截图链；但不能声明完整 T0 序章、最终 CG / Live2D、ArtAcceptance 或主美商业化封板完成。
- 2026-07-07：`T0-01A` 序章 CG 已整理出首批真实候选：`cg_t0_01a_p02_panel01_workshop_wide`、`cg_t0_01a_p02_panel02_debt_notice_door`、`cg_t0_01a_p02_panel03_no0_hand_cloth`、`cg_t0_01a_p03_panel01_debt_notice_close` 各有 1 张 raw / processed / contact sheet。粗审结论：门缝催缴纸、零号手部和债务纸特写可保留为候选；工坊远景气氛可用但洞穴感偏强、工坊生活感偏弱，需补更多备选。当前没有同步 Approved，也没有程序可接入素材。
- 2026-07-07：`T0-01A` 序章 CG Panel 提示词 / Spec 已补到可真实出图状态：`generate_art_prompts.py` 新增 `narrative_cg` 专用提示词分支、15 个 Panel 的中英文视觉描述、负面词和 `1920x1080 / opaque_environment / center_4_3 safe area` 规格；`Generate-ArtPrompts.ps1` 更新后 15 个 Panel 均为 `prompted` 且不再使用 `single readable game asset` / 图标规格。缺图计划显示 `planned=15`、`prompt_ready=15`、`Size counts={"1920x1080":15}`。
- 2026-07-07：`T0-01A` 序章 CG Panel 已进入 seed / Manifest 准入：`art_requirements_seed.json` 当前包含 15 个 `cg_t0_01a_p*_panel*` 条目，Manifest 已刷新为 299 条；15 个 Panel 均为 `SourceType=preset`、`PresetCategory=narrative_cg_panel`、`AssetType=cg_panel`，缺图计划显示 `planned=15`、可接入清单显示 `generate_needed=15`、程序交接仍为 `program_integrate=0`。当前只是素材需求准入，不代表图片已生成或可接入。
- 2026-07-07：新增 `美术文档/19_T0-01序章CG细案.md`，把 T0-01A 序章从通用 CG 工作流落到美术侧具体分镜：6 个漫画页、15 个 Panel VisualID、零号表现要求、构图 / 风格禁区、出图规格和 6 张运行时截图验收清单。本文只声明 `规格完成`，未新增 `_IncomingAI`、Approved、Manifest、Registry、`screen_layouts.json` 或运行时截图证据。
- 2026-07-07：新增 `美术文档/18_CG底图与漫画式播放演出工作流.md`，把用户确认的 B 方案沉淀为正式规格：采用运行时漫画页拼装，不做整页带字烘焙；定义 Page / Panel / LayoutPreset / Caption / Action 数据口径、Panel VisualID 命名、Manifest `narrative_cg_panel` 分类、美术生产 8 步、T0-01A 首批 6 页样板和 15 个初始 Panel VisualID。本文只声明规格完成，未新增 `_IncomingAI`、Approved、Manifest、Registry、`screen_layouts.json` 或运行时截图证据。
- 2026-07-06：用户人工验收再次否决 `T0-VAL-01`，美术 / UI 侧当前口径改为 `validation_failed:t0_val_01_commercial_prologue_not_passed`。最新 `UnityClient/Logs/T0Validation/t0_val_01_final_capture_report.txt` 虽为 `semantic_failed=False`，但只能证明 8 个状态截图存在；画面仍被判定为半成品：没有真正序章、启动零号缺少仪式感、半开放工坊像调试 UI、整体棕金机械 / 厚金边旧 RPG 感不适配 FormalV2 和当前日系二次元地底奇幻方向。复核记录见 `版本规划/0-12小时细案/T0-VAL-01_最终实际效果优化文档.md` 第 15 节。
- 2026-07-05：`T0-VAL-01` 正式版视觉 / UI 优化方案已重设并写入 `版本规划/0-12小时细案/T0-VAL-01_最终实际效果优化文档.md` 第 13 节。美术 / UI 侧下一轮按正式版处理：对白框参考用户图一的“角色居中 + 底部对白 + AUTO/LOG 辅助但默认手动点击”体验；半开放工坊直接接入 FormalV2 `workshop_main` 的 `home_room_scene`、`light_status_strip`、`doll_center`、`abyss_door`、`workshop_entry`、`ledger_corner`；过程 CG、启动零号仪式、状态小卡、首潜许可卡都按正式分镜和低信息密度重做。当前仍是 `validation_failed:t0_val_01_commercial_effect_not_passed`，不是美术 / UI 通过。
- 2026-07-05：用户人工验收否决 `T0-VAL-01`，美术 / UI 侧同步撤回“staging 纵切验收通过”口径。当前不是 P2 小修，而是 P0 商业化序章效果不成立：开场过程 CG 像背景 + 道具卡 + 对白框，`启动人偶` 缺少仪式感，半开放工坊证据缺失，首潜确认仍像通用层列表 / 调试面板，整体棕金机械 / 暖灯 / 厚金边 UI 也偏离当前日系二次元地底奇幻冒险方向。详细问题写入 `版本规划/0-12小时细案/T0-VAL-01_最终实际效果优化文档.md` 第 12 节。
- 2026-07-05：`T0-VAL-01` 最终实际效果验收已完成两轮通过。美术 / UI 口径：当前 staging 纵切版本的 8 张固定截图已能覆盖黑屏醒来、债务纸特写、发现零号与 `启动人偶`、零号苏醒、状态卡与擦灰、半开放工坊、首潜确认和第一层地图；subagent 未发现阻断完成的问题。后续保留两个 P2 优化项：债务纸需要补可读的催缴 / 债务 / 抵押文本，首潜确认需要从通用列表感继续强化为“首次下潜许可卡”。这两项不阻断当前 T0-VAL-01 staging 验收，也不代表正式 CG / 动态立绘 / ArtAcceptance 封版已完成。
- 2026-07-05：`T0-VAL-01` live Unity MCP 连续画面证据已补齐到 Owner 自验层级，并在 15:12-15:20 重新抓取有效截图，避免旧截图误截纯色过渡帧。允许路径截图覆盖 `启动人偶`、`擦去核心仓灰尘`、半开放工坊、第一层确认和进入 `DungeonMap`：`UnityClient/Logs/T0Validation/t0_val_01_livefix_01_start_doll_action.png`、`_02_wipe_action.png`、`_03_half_open_workshop.png`、`_04_layer_confirm.png`、`_05_final_dungeon_map.png`；阻断态截图 `t0_val_01_rerun_07_blocked_extreme_wear.png` 显示磨损极高时首潜确认不可点击，强制点击也未触发 `StartRunAtLayer`。美术 / UI 结论：序章关键玩家路径画面已可见，但仍是复用素材与 UGUI staging 合成，不是正式 CG / Live2D / 商业演出验收通过。
- 2026-07-04：按 `T0-VAL-01` 复验 live Unity 玩家画面，结论为美术 / UI 验收未通过。证据截图 `UnityClient/Logs/T0Validation/t0_val_01_recheck_01_opening.png` 与 `UnityClient/Logs/T0Validation/t0_val_01_recheck_02_after_wait.png` 均停在 `cg_t0_01a_debt_notice` 债务纸对白；画面没有可见 `继续`、`启动人偶` 或其他动作按钮，Overlay 仍阻塞输入。开发方案要求的 `启动人偶` 单按钮态、状态小卡、半开放工坊、第一层确认允许态 / 阻断态截图均未出现，因此不能把当前 T0-01A 记为序章画面或玩家体验验收通过。
- 2026-07-04 `T0-01A` 序章演出合成 MVP 已由程序侧接入到 `P3DialogueOverlayController`：`cg_t0_01a_debt_notice`、`cg_t0_01a_repair_note`、`cg_t0_01a_core_shard`、`cg_t0_01a_find_no0`、`vfx_no0_core_start`、`stand_no0_weak_sitting` 和 `ui_status_card_prologue` 会在缺正式 CG 时映射到现有 Approved 工坊背景、道具卡和零号静态立绘；`NarrativeOverlaySmokeTest` 已新增工坊背景、债务纸和人偶显示断言。验证证据：`dotnet build UnityClient/Assembly-CSharp.csproj --no-restore` 0 warning / 0 error；`dotnet build UnityClient/Assembly-CSharp-Editor.csproj --no-restore` 0 warning / 0 error；`UnityClient/Logs/TestReport.json` 中 `PrologueFirstDivePermissionSmokeSuite.Run` 为 `PASSED`；`PlayModeRecoveryTools.ReportRuntimeState` 显示 `visual=cg_t0_01a_debt_notice` 且 stage 为 `bg_workshop_home_room` + `memento_debt_shadow_window`。本轮未新增 Approved、Manifest、Registry 或 `screen_layouts.json`，也未产出连续截图 / ArtAcceptance，因此只承认“可运行演出合成 MVP”，不承认最终视觉通过。
- `T0-VAL-01` 美术 / UI 侧状态回写已更新：7 月 5 日截图已补到 `启动人偶`、擦灰、半开放工坊、首潜确认允许态 / 阻断态和第一层进入；仍缺的是正式黑屏字幕 / 过程 CG 资产、出发切黑专门截图、ArtAcceptance 旁路和主美人工复核。因此美术侧当前承认“staging 画面可见 / 玩家路径可验证”，不承认“商业序章画面验收通过”。
- `T0-FLOW-04` 首潜第一层确认 UI 程序侧已新增：`DungeonStartLayerUIController.PresentFirstDive()` 复用现有层选择面板，固定 `layerID=1`，只展示 `第一层  旧矿井浅缝`，按钮为 `出发 / 再看她一眼`，许可通过和磨损等阻断态都以中文玩家文案显示；`再看她一眼` 返回半开放工坊。该状态由 Narrative `open_layer_confirm` 的 FirstDive 请求或工坊 `OpenFirstDiveLayerConfirmPanel()` 打开，不新增素材、不改 Manifest / Registry / `screen_layouts.json`。
- `T0-FLOW-04` 美术 / UI 验证限制已被 7 月 5 日补强：Unity AutoTest smoke `PrologueFirstDiveLayerConfirmSmokeTest.Run` 已通过，且 T0-VAL-01 已补首潜确认允许态 / 阻断态 live 截图；剩余限制是缺正式美术规格和 ArtAcceptance 旁路。
- `T0-FLOW-03` 半开放工坊 UI 程序侧已新增：`WorkshopUIController.EnterPrologueHalfOpen()` 复用当前 `workshop_main` UGUI 结构，将主按钮改为 `浅层入口`，灰态表达未开放系统，并保留零号状态提示。该状态由 Narrative `unlock_ui shallow_gate` 驱动，不新增素材、不改 Manifest / Registry / `screen_layouts.json`。
- `T0-FLOW-03` 美术 / UI 验证限制已被 7 月 5 日补强：Unity batchmode smoke `PrologueHalfOpenFlowEditorRunner.RunFromBatchmode` 已通过，且 T0-VAL-01 已补半开放工坊 live 截图；剩余限制是缺正式美术规格和 ArtAcceptance 旁路。
- `NARR-03` 纯 UGUI 叙事 overlay 程序侧已新增：`P3DialogueOverlayController` 使用 UGUI 构建黑屏层、VisualID 表现容器、对白框、说话人 / 正文、继续按钮和单按钮动作；VisualID 只通过 `VisualAssetService` / fallback 解析，不在对白表或 UI 代码中写资源路径。本轮未新增 Approved 素材、未修改 Manifest / Registry / `screen_layouts.json`，也未生成截图。
- `NARR-03` 美术 / UI 验证限制已被 7 月 5 日补强：当前已有 Unity runtime smoke、PlayMode 状态和 T0-VAL-01 连续截图证据；剩余限制是正式黑屏字幕 / 过程 CG 资产、ArtAcceptance 旁路和主美人工复核尚未完成，因此仍不能声明最终画面质量通过。

- 已迭代 `T0-01A_开局人偶状态到首次下潜许可开发方案.md` 的美术 / UI 交付口径：T0-01A 现在明确对白表 / Yarn / Narrative JSON 不写真实资源路径，只写 VisualID、角色表现参数或表现意图；首版建议复用或登记 `cg_t0_01a_black_wake`、`cg_t0_01a_debt_notice`、`cg_t0_01a_repair_note`、`cg_t0_01a_core_shard`、`cg_t0_01a_find_no0`、`no0 expression=weak pose=sitting`、`vfx_no0_core_start`、`vfx_shallow_gate_wake` 等表现意图，并要求 `locked_for_implementation` 前关键项有 Approved 素材、动态 Prefab 映射或明确 fallback。本轮只更新开发方案，未新增素材需求、未改 Approved、未刷新 Manifest。
- 已同步全局叙事播放系统的美术素材绑定口径到 `开发文档/18_全局叙事播放系统开发方案.md`：对白表 / Yarn 不写真实资源路径，只写 VisualID、角色表现参数或表现意图；`narrative_nodes.json.visual_intents` 汇总 CG、静态立绘 / 头像、Live2D / Spine / DollPuppet、VFX / 音效、对话 UI 皮肤和 fallback 需求；`locked_for_implementation` 前关键 VisualID 必须有 Approved 素材、动态 Prefab 映射或明确 fallback。本轮只补开发方案口径，未新增素材需求、未改 Approved、未刷新 Manifest。
- 2026-06-13 美术 / UI 侧确认后续涉及纯 UGUI 的布局、视觉层级、皮肤绑定、VisualID 表现、截图验收和非玩法 UI polish 默认由美术侧直接闭环；只有领域服务、后端规则、Unity 工程约束、自动验收工具或 UGUI 底层能力阻断时再交给程序侧。
- 2026-06-13 美术 / UI 侧继续直接处理 FormalV2 运行时 UGUI 精修并完成当前纵切基线封版：`dungeon_map` 节点缩小、标签缩短、路线降噪并按纵深重新布局；`sell_panel` / `prosthetic_panel` modal tint 和遮罩层级更干净；11 个旧模板屏已补独立薄 Controller 路径并不再走 `formal_v1_template` 降级截图。最终 ArtAcceptance `RunID=20260613_020257` 工具层 `PASSED`、21/21 captured、Registry 278、`MissingRequiredVisualIDs=0`、warnings/errors=0、`RealGameplay=21`、`FormalV1Template=0`；当前 FormalV2 runtime UI visual seal 通过纵切基线，后续共享子面板差异化属于质量迭代。证据入口：`UnityClient/Logs/ArtAcceptance/latest/report.json`、`UnityClient/Logs/ArtAcceptance/latest/contact_sheet_formalv2_20260613_020257_codex.png`、`美术文档/09_运行时美术验收记录.md`。
- 2026-06-12 美术 / UI 侧已直接完成一轮 FormalV2 运行时 UGUI 精修和验收工具修复：ArtAcceptance `RunID=20260612_231356` 工具层 `PASSED`、21/21 captured、Registry 278、`MissingRequiredVisualIDs=0`、warnings/errors=0；`ScenarioEventPanel_Runtime` 跨截图污染已清除，只保留在 `scenario_event` 自身截图。核心 P0 UI 结构条件通过：`workshop_main` 不再常驻背包格，`inventory_loot` 改为战斗场景半透明拾取叠层，`combat_hud` 状态文字与条形控件不再明显压叠，`settlement` 标题 / 摘要 / 三列结构可读。证据入口：`UnityClient/Logs/ArtAcceptance/latest/report.json`、`UnityClient/Logs/ArtAcceptance/latest/contact_sheet_formalv2_20260612_231356_codex.png`、`美术文档/09_运行时美术验收记录.md`。
- 已完成美术全局方向纠偏：`美术文档/04_美术风格基准.md`、`美术文档/03_AI生成与筛选规范.md`、Formal V2 总览 / README / concepts 评审说明、`formal_v2/01..21` active 界面草案、`美术文档/ui_design/design_tokens.json`、`美术文档/ui_design/screen_layouts.json` 和 `tools/美术工具/generate_art_prompts.py` 已同步：全局主轴为日系二次元地底奇幻冒险，active UI 与正向素材提示词不再默认使用黄铜 / 铜件 / 暖灯 / 蒸汽朋克；旧词只允许出现在历史纠偏说明、禁止项、负面词或明确物品语义中。本轮不重跑概念图，不改 Approved 运行时图片。
- 程序/UI 侧已完成 FormalV2 运行时结构重排后的截图缺口修复，并产出美术侧下一轮人工复验入口：latest ArtAcceptance `RunID=20260611_014057`、`Status=PASSED`、21/21 captured、Registry 278、`MissingRequiredVisualIDs=0`、warnings/errors=0；证据入口为 `UnityClient/Logs/ArtAcceptance/latest/report.json`、`UnityClient/Logs/ArtAcceptance/latest/screenshots/` 和 `UnityClient/Logs/ArtAcceptance/latest/contact_sheet_formalv2_20260611_014057.png`。本轮程序侧自查结论：`sell_panel` / `prosthetic_panel` 不再是空黑框，`workshop_main`、`combat_hud`、`dungeon_map`、`inventory_loot`、`settlement` 均有最新截图覆盖；但 FormalV2 UI 画面是否通过仍待美术侧按截图人工复验，不得仅凭 ArtAcceptance `PASSED` 标记为视觉通过。
- 已按用户要求创建本地 P3 mission：`.mission/20260608_002315-Formal-V2-UI-active规格迁移与美术交接落地.csv`，并拆分为 V2 评审冻结、V2-A active 迁移、Manifest / handoff 刷新、素材缺口拆分、程序交接和首批运行时素材批次规划 6 个任务。`ART-V2-01` 已记录用户认可 `concepts/review_index.md`，Formal V2 当前进入 V2-A active 迁移阶段。
- 已完成 P3 mission `ART-V2-02`：V2-A 五个核心屏幕 active 规格已迁移到 FormalV2；同步调整 `component_catalog.json` 的屏幕组件适用关系；`Validate-UIDesign.ps1` 通过并刷新 `美术文档/ui_design/_generated/ui_design_handoff.md`。
- 已执行 P3 mission `ART-V2-03` 的生成链路：同步配置、刷新 Manifest、Prompt、UI handoff、FormalV1 验收队列、程序交接清单和可接入素材清单；当前 V2-A 相关新增缺口集中体现为 `generate_needed=29`，程序登记队列仍为 0。
- 已完成 P3 mission `ART-V2-04` / `ART-V2-05` 的交接整理：`美术文档/13_正式纵切UI与素材覆盖矩阵.md` 已记录 V2-A 五屏 active、`generate_needed=29` 构成和程序侧交接口径；`agent_status/program.md` 和 `PROJECT_STATUS.md` 已同步 `program_integrate=0`、`rerun_acceptance=16` 的当前判断。
- 已完成 P3 mission `ART-V2-06` 的首批运行时素材规划：缺图跑图批次为 `nai_v2a_runtime_missing_20260608_01`，包含 29 个已具备 Prompt / Spec 的运行时内容素材；Visual V2 质量替换批次为 `nai_v2a_runtime_quality_20260608_01`，包含 121 个已接入 local_v0 / placeholder 的同名替换项。本轮只完成批次计划和文档交接，尚未实际调用 NovelAI 跑图。
- 已完成 FormalV2 全量美术迭代 mission `ART-FV2-01`：`00_formal_v2_ux_ui_overview.md` 新增 Formal V2 美术系统标准、`local_v0` / `formal_ai_v2` / `final_polish` 质量层级，以及概念图 / 结构图 / 运行时素材边界；`formal_v2/README.md` 已指向该总规范。已用 `py tools/docs/validate_docs.py --index docs_index.json` 验证通过。
- 已完成 FormalV2 语义高风险素材二次替换的提示词准备：针对 `item_con_purifying_salt_icon`、`item_gear_corroded_bulwark_icon`、`item_gear_mycelium_cloak_icon`、`item_gear_spore_lance_icon`、`item_loot_acid_gland_icon`、`item_mat_core_tier2_fragment_icon`、`monster_boss_spore_foundry_portrait`、`monster_mob_acid_slime_mature_portrait`、`monster_mob_echo_pilgrim_portrait` 补入明确英文视觉描述和专用负面词，避免继续生成灯具 / 黄铜容器 / 黑底小主体。`Run-ArtGeneration.ps1 -Status approved -PreserveStatus -DryRun` 已确认批次 `nai_formalv2_semantic_fix_20260609_01` 可选中 9 项，尺寸为 6 个 `512x512` 图标和 3 个 `1024x1024` 头像；当前未实际调用 NovelAI。
- 已完成 58 个 FormalV2 `program_integrate` 待接入资产的提示词具体化：Manifest 中这 58 项的 `PromptEN` 不再含 `single readable game asset` 通用模板，已覆盖 1 个 layer3 背景、23 个物品图标和 34 个怪物战斗 / 头像资源。`Run-ArtGeneration.ps1 -Status approved -PreserveStatus -DryRun` 已确认批次 `nai_formalv2_program_integrate_prompt_specific_20260609_01` 可选中 58 项；当前仍未实际调用 NovelAI。
- 已新增 FormalV2 Prompt Readiness 门禁报告：`美术文档/_generated/formal_v2_prompt_readiness/formal_v2_prompt_readiness.md/json`，当前结论为 `ProgramIntegrateVisuals=58`、`PromptReady=58`、`PromptBlocked=0`、`GenericPromptRemaining=0`、`CjkPromptViolations=0`。报告内按 `background` / `item` / `monster` 输出不含中文路径的可复制 NovelAI 串行重跑命令。
- 已整理本轮待提交美术工作区变更：Approved 图标 / 纪念物 / 势力 / 订单 / 传闻 `.meta` 导入上限按运行时用途提升到 1024 或 2048；补提交美术流水线历史快照与 `美术风格参考/` 参考图，并为参考图补 LFS 规则；同步收口 `dungeon_map` Formal V2 为“可推进大地图 + 独立生态层”方向。`tools/ComfyUI_NAIDGenerator/` 仍按第三方工具候选留在未跟踪状态，待确认 vendor / submodule / 本地工具口径。
- 已完成 P0 UI 运行时验收：`workshop_main`、`combat_hud`、`inventory_loot` 当前 MVP Baseline 为 `validated`。
- 已建立 Formal V1 UI 结构设计层：`combat_hud`、`workshop_main`、`inventory_loot`、`dungeon_map`、`settlement`。
- 已冻结 MVP UI Baseline：`美术文档/ui_design/versions/mvp_baseline_2026-05-22/`。
- 已建立 UI 设计迭代流程：Formal V1 文档确认后修改 active `screen_layouts.json`，再生成素材和交给程序接入。
- PM 已将美术线纳入 `版本规划/09_正式版核心纵切开发路线.md`：A1 聚焦 `combat_hud` Formal V1，A2/A3 依次承接拾取、战斗、地图、工坊和结算界面。
- 已把 `combat_hud` active 规格切到 Formal V1：底部居中背包、左玩家/右敌人实体舞台、敌人脚下血条、中央 `vfx_space`。
- 已扩展 Manifest 扫描与提示词生成：`MonsterEntity.CombatVisualID` 会扫出 `monster_*_combat` 透明战斗实体素材需求，并新增 `ui_combat_entity_shadow` / `ui_combat_target_ring`。
- 已完成 `combat_hud` Formal V1 第一批战斗资源入库：`monster_mob_scavenger_bug_combat`、`monster_mob_acid_slime_combat`、`monster_elite_scrap_guard_combat`、`monster_elite_mutant_amalgam_combat`、`ui_combat_entity_shadow`、`ui_combat_target_ring`，并补齐 Unity `.meta`。
- 已新增 `Scan-UIIterationCandidates.ps1`，可自动扫描 active UI 规格、Manifest、Approved 资源和最新 ArtAcceptance 输出，生成 UI 迭代候选报告。
- 当前扫描报告的旧截图早于 2026-05-24 active 规格；后续需要用最新 ArtAcceptance 逐屏覆盖当前 Formal V1 结构。
- 已新增 `Generate-ArtIntegrationCandidates.ps1`，生成 `美术文档/_generated/可接入素材清单.md/json`；`Sync-ApprovedArt.ps1` 非 DryRun 同步后会默认刷新清单，程序侧可按 `program_integrate` 自助接入。
- 已把可接入素材清单升级为 `latest + snapshot` 机制：`Run-ArtGeneration.ps1`、`Optimize-ArtAssets.ps1`、`Sync-ApprovedArt.ps1` 非 DryRun 后默认刷新 latest，并在 `美术文档/_generated/art_integration_snapshots/` 留一份阶段快照；新增 `art_process` 状态表示 raw 已生成但还需预处理。
- UI 程序侧已接入 `combat_hud` Formal V1，并通过 ArtAcceptance `20260524_043441`：左玩家、右敌方实体、底部居中背包、敌人脚下血条和目标光环均已出现在运行时截图中，报告无 warnings / errors。
- 已把 `workshop_main` / `inventory_loot` active 规格切到 Formal V1，并通过 `Validate-UIDesign.ps1`；程序交付摘要 `ui_design_handoff.md` 已重新生成。
- 已把 P1 首批五个界面 `dungeon_map`、`settlement`、`layer_select`、`sell_panel`、`prosthetic_panel` 切到 Formal V1 active 规格，补齐对应结构文档和迁移记录；程序侧可按 active 规格分批接入。
- 已把 A3 房间节点 `safe_room`、`stairs_room` 切到 Formal V1 active 规格，补齐结构文档、迁移记录和 `13_正式纵切UI与素材覆盖矩阵.md`；程序侧可按 active 规格分批接入。
- 已把 `maintenance_panel`、`daily_bill_report` 切到 Formal V1 active 规格，补齐结构文档、迁移记录和覆盖矩阵；当前 active Formal V1 UI 覆盖增至 12 个界面。
- 已新增 `ui_icon_maintenance`、`ui_icon_bill`、`ui_icon_warning` 三个 preset UI 图标需求，并补齐英文绘图提示词、负面提示词和结构化 Spec。
- 已明确美术侧生成交付协议：每轮生成、预处理或 Approved 同步后都维护 latest 可接入素材清单，并留下 snapshot，供程序侧按 `program_integrate` 自助接入。
- 已将可接入素材清单维护规则同步到 `AGENTS.md` 美术智能体入口：AI 出图、预处理、Approved 同步三类动作都必须刷新 latest 并留 snapshot。
- 已按 NovelAI 串行生成、预处理并同步 `ui_icon_maintenance`、`ui_icon_bill`、`ui_icon_warning` 到 `UnityClient/Assets/Art/Approved/UI/`，并补齐 Unity `.meta`。
- 已留档 P1 图标可接入历史快照：`美术文档/_generated/art_integration_snapshots/20260524_075054_p1_ui_icons_approved_ready_repo_state.*`。
- 已把 A4 三个界面 `shop_staging`、`order_board`、`rumor_board` 切到 Formal V1 active 规格，补齐结构文档、迁移记录和覆盖矩阵。
- 已新增 `ui_icon_shop_channel`、`ui_icon_black_market`、`ui_icon_order`、`ui_icon_faction`、`ui_icon_deadline`、`ui_icon_rumor`、`ui_icon_price_up`、`ui_icon_price_down` 八个 preset UI 图标需求，并补齐英文绘图提示词、负面提示词和结构化 Spec。
- 已尝试用 NovelAI 串行生成 A4 八个 UI 图标，失败原因为 HTTP 402：Anlas 余额不足，不是并发或 rate limit；已留档 `美术文档/_generated/art_integration_snapshots/20260524_084327_generation_nai_a4_ui_icons_20260524_01.*`。
- 已用 local_v0 方式补齐 A4 八个 UI 图标的透明 PNG 可接入临时版，并同步到 `UnityClient/Assets/Art/Approved/UI/`，同时补齐 Unity `.meta`：`ui_icon_shop_channel`、`ui_icon_black_market`、`ui_icon_order`、`ui_icon_faction`、`ui_icon_deadline`、`ui_icon_rumor`、`ui_icon_price_up`、`ui_icon_price_down`。
- 已刷新 A4 local_v0 图标可接入历史快照：`美术文档/_generated/art_integration_snapshots/20260524_085725_a4_ui_icons_local_v0_approved_repo_state.*`。
- UI 程序侧已按当时 latest `program_integrate` 队列接入一批 Approved VisualID，并绑定 `combat_hud`、`maintenance_panel`、`daily_bill_report`、`shop_staging`、`order_board`、`rumor_board`；ArtAcceptance `20260524_212423` 通过，`MissingRequiredVisualIDs=0`，无 missing sprite 命中。
- 已完成美术文档高内聚整理：`README.md` 明确规划层 / 工作流层 / 契约与数据层 / 交付与历史层；`00_美术流水线总览.md` 收敛为资产生产流程；`10_正式版核心纵切美术路线.md` 收敛为当前规划和近期顺序；`13_正式纵切UI与素材覆盖矩阵.md` 作为 active Formal V1 覆盖和素材批次入口；`ui_design/README.md` 明确程序只接 active `screen_layouts.json`。
- 已将 `06_MVP素材接入状态同步.md`、`07_MVP_UI重新设计同步.md`、`11_P0_UI骨架接入交付.md`、`12_P1_UI骨架接入准备.md` 归档到 `美术文档/archive/`，并新增 `archive/README.md` 说明归档规则和替代入口。
- 已将 `ui_design/ui_iteration_process.md` 升级为独立 UI 版本迭代工作流：baseline -> design draft -> 用户确认 -> active -> handoff -> seed/Manifest/Prompt/Spec -> Approved -> 可接入清单 -> 程序接入 -> ArtAcceptance -> validated。
- 已复核最新 `版本规划/09`、`11`，并将美术侧推进口径同步为：P5 不独立铺量，只服务 P0-P4 当前纵切；`知识库/views/art.md`、`10_正式版核心纵切美术路线.md`、`13_正式纵切UI与素材覆盖矩阵.md` 和 UI versions 入口已更新。
- 已将 `doll_room` 从 Formal V1 草案推进到 active `screen_layouts.json` 规格，补齐 `bg_doll_room_attic`、`ui_icon_diary`、`ui_room_memento_slot` 三个 preset 资产需求、Manifest / Prompt / Spec 和 local_v0 Approved PNG。
- 已刷新 latest 可接入素材清单并留档 `美术文档/_generated/art_integration_snapshots/20260525_003240_formal_v1_19_active_doll_room_ready.*`；当前清单显示 `program_integrate=12`、`acceptance_needed=82`、`generate_needed=0`。
- 已同步美术路线、UI 覆盖矩阵、UI 设计入口、Formal V1 总览、迁移日志和知识库美术入口：当前 active Formal V1 覆盖为 20 个界面，暂无剩余 draft UI 队列。
- 已新增 `Generate-ArtQualityBacklog.ps1` / `generate_art_quality_backlog.py`，自动从 Manifest 和 Approved PNG 生成 `美术文档/_generated/素材质量替换清单.md/json`，并在 `art_quality_snapshots/` 留历史快照。
- 已将质量替换队列接入 `README.md`、`00_美术流水线总览.md`、`10_正式版核心纵切美术路线.md`、`13_正式纵切UI与素材覆盖矩阵.md` 和 `tools/美术工具/README.md`；后续程序接入看 `可接入素材清单`，美术精修看 `素材质量替换清单`。
- 已修复 4 张怪物头像 Approved PNG 的半透明边缘技术风险：`monster_mob_scavenger_bug_portrait`、`monster_mob_acid_slime_portrait`、`monster_elite_scrap_guard_portrait`、`monster_elite_mutant_amalgam_portrait` 现在符合 `AlphaRequired=false`。
- 已刷新质量清单快照 `美术文档/_generated/art_quality_snapshots/20260525_004827_monster_portrait_alpha_fixed_visual_v2_backlog.*`；当前 `technical_fix=0`、`visual_v2_replace=20`、`spec_review=0`。
- 已补齐 Visual V2 安全替换工具链：`Run-ArtGeneration.ps1` 支持 `-PreserveStatus`，`Optimize-ArtAssets.ps1` 支持 `-CandidateBatchID`，`Sync-ApprovedArt.ps1` 支持 `-CandidateBatchID` / `-QualityTier` / `-ClearCandidate`；Python 端兼容 `utf-8-sig` Manifest。
- 已用临时 Manifest 和 mock provider 验证 Visual V2 流程：`ui_button_primary` 测试样本保持 `Status=approved`，同步后写入 `QualityTier=formal_ai_v2` / `ReplacementBatchID`，并清理候选字段，不污染正式 Manifest 和 Approved 目录。
- 已刷新质量清单快照 `美术文档/_generated/art_quality_snapshots/20260525_011427_visual_v2_tooling_ready.*`；当前仍为 `technical_fix=0`、`visual_v2_replace=20`、`spec_review=0`，本轮没有真实素材替换。
- 已新增 `Generate-VisualV2Plan.ps1` / `generate_visual_v2_plan.py`，把 `visual_v2_replace` 队列转成 NovelAI 可执行批次计划；latest 为 `美术文档/_generated/VisualV2生成计划.md/json`，历史快照在 `美术文档/_generated/visual_v2_plan_snapshots/`。
- 已生成 Visual V2 执行计划 `nai_visual_v2_20260525_01`：计划替换 20 个 local_v0 资源，全部具备英文提示词、负面提示词和结构化 Spec，尺寸分布为 `512x512=19`、`1920x1080=1`。
- 已用 NovelAI 对 `ui_icon_diary` 做串行生成探测，失败原因为 HTTP 402：单张 512x512 需要 5 Anlas，当前仅 2 Anlas；未生成图片，也未把 mock 或失败输出同步为正式资源。
- 已刷新质量清单快照 `美术文档/_generated/art_quality_snapshots/20260525_013017_nai_visual_v2_probe_anlas_blocked.*`；当前仍为 `technical_fix=0`、`visual_v2_replace=20`、`spec_review=0`。
- 已把 P1 战斗可读性增补写入 `combat_hud` active 规格、组件目录、Formal V1 文档和迁移记录：新增 `Combat.IntentIcon`、`Combat.StatusIcon`、`Combat.HitFeedback`、`Combat.ShieldBreakFeedback`、`Combat.GridLockMarker`、`Combat.JunkPreviewMarker` 六类组件。
- 已在 `art_requirements_seed.json` 和提示词生成脚本中补齐 17 个战斗可读性 VisualID 的英文绘图提示词、中文说明、负面提示词和结构化 Spec；图标 / 反馈 / 背包格 overlay 尺寸分别按 `512x512`、`512x512`、`256x256` 管理。
- 已刷新 Manifest / Prompt / UI handoff / 可接入清单 / 质量清单并留档 `20260525_020335_p1_combat_readability_contract_prompt_fix.*` 与 `20260525_020357_p1_combat_readability_contract_prompt_fix.*`；当前 `generate_needed=21`、`technical_fix=0`、`visual_v2_replace=20`。
- 已新增 `Generate-ArtBatchPlan.ps1` / `generate_art_batch_plan.py`，把 `可接入素材清单` 中的 `generate_needed` 队列转成 NovelAI 可执行缺图跑图计划；latest 为 `美术文档/_generated/缺图生成计划.md/json`，历史快照在 `美术文档/_generated/art_generation_plan_snapshots/`。
- 已建立早期缺图执行计划并跑通 local_v0 兜底：当时 21 个缺失素材均具备英文提示词、负面提示词和结构化 Spec，构成为 P0 新节点图标 4 个、P1 战斗可读性 UI 素材 17 个；后续 P0 新节点图标和部分 P1 战斗素材已被 NovelAI 正式图替换。
- 已新增 `Generate-LocalV0Art.ps1` / `generate_local_v0_art.py`，在 NovelAI Anlas 不足时为已有 Prompt / Spec 的缺图项生成明确标记的 local_v0 Approved 素材，不冒充正式 AI 产物。
- 已用 `local_v0_missing_assets_20260525_01` 补齐 21 个缺图 Approved PNG 和 Unity `.meta`：4 个新地图节点图标、17 个 P1 战斗可读性 UI 素材。latest 可接入清单已刷新为 `program_integrate=33`、`acceptance_needed=82`、`generate_needed=0`。
- 已刷新质量清单与 Visual V2 计划：当前 `technical_fix=0`、`visual_v2_replace=41`、`spec_review=0`，正式 AI 替换批次为 `nai_visual_v2_20260525_02`。
- 已将 `chassis_upgrade_panel` 从 Formal V1 草案推进到 active `screen_layouts.json` 规格，补齐 `ui_icon_chassis_upgrade`、`ui_icon_blueprint`、`ui_icon_material_need` 三个 preset UI 图标需求、Manifest / Prompt / Spec 和 local_v0 Approved PNG。
- 已刷新 latest 可接入素材清单、缺图生成计划、质量清单和 Visual V2 计划：当前 `program_integrate=36`、`acceptance_needed=82`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=44`、`spec_review=0`，正式 AI 替换批次为 `nai_visual_v2_20260525_03`。
- 已将 `business_settlement` 作为 P4 经济压力链中 `shop_staging -> business_settlement -> daily_bill_report` 的中间营业反馈界面写入 active `screen_layouts.json`，补齐 `business_settlement_v1.md`、组件目录、迁移记录、路线文档和覆盖矩阵。
- 已补齐 `ui_icon_customer`、`ui_icon_sale_spark`、`ui_icon_business_settlement` 三个 preset UI 图标需求、英文提示词、中文说明、负面提示词和结构化 Spec，并用 `local_v0_business_settlement_20260525_01` 生成 Approved PNG 和 Unity `.meta`。
- 已刷新 latest 可接入素材清单、缺图生成计划、质量清单和 Visual V2 计划：当前 `program_integrate=39`、`acceptance_needed=82`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=47`、`spec_review=0`，正式 AI 替换批次为 `nai_visual_v2_20260525_04`。
- 已补齐 `maintenance_panel` 维护可读性增量：新增 `ui_icon_wear_repair`、`ui_icon_corruption_purify`、`ui_icon_dive_permit` 三个 preset UI 图标需求、英文提示词、中文说明、负面提示词和结构化 Spec，并用 `local_v0_maintenance_readability_20260525_01` 生成 Approved PNG 和 Unity `.meta`。
- 已刷新 latest 可接入素材清单、缺图生成计划、质量清单和 Visual V2 计划：当前 `program_integrate=42`、`acceptance_needed=82`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=50`、`spec_review=0`，正式 AI 替换批次为 `nai_visual_v2_20260525_05`。
- 已补齐 `daily_bill_report` 每日账单经济可读性增量：新增 `ui_icon_income`、`ui_icon_expense`、`ui_icon_debt_rent` 三个 preset UI 图标需求、英文提示词、中文说明、负面提示词和结构化 Spec，并用 `local_v0_daily_bill_readability_20260525_01` 生成 Approved PNG 和 Unity `.meta`。
- 已刷新 latest 可接入素材清单、缺图生成计划、质量清单和 Visual V2 计划：当前 `program_integrate=45`、`acceptance_needed=82`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=53`、`spec_review=0`，正式 AI 替换批次为 `nai_visual_v2_20260525_06`。
- 已新增 `Generate-FormalV1AcceptanceQueue.ps1` / `generate_formal_v1_acceptance_queue.py`，从 active UI 规格、Manifest、latest ArtAcceptance 和 Registry 快照生成 Formal V1 运行时美术验收队列；当前 `screens=21`、`captured=15`、`program_register_visuals=10`、`review_previous_screenshot=11`，最新快照为 `美术文档/ui_design/_generated/formal_v1_acceptance_snapshots/20260525_235820_local_v0_quality_normalized.*`。
- 已新增 `Generate-ArtProgramHandoff.ps1` / `generate_art_program_handoff.py`，把 `可接入素材清单` 与 `FormalV1验收队列` 合成为程序侧一站式交接清单；latest 为 `美术文档/_generated/程序接入交接清单.md/json`，当前汇总 `program_integrate=45`、`add_capture=6`、`rerun_acceptance=15`，快照为 `美术文档/_generated/art_program_handoff_snapshots/20260525_045747_program_handoff_20260525_01.*`。
- 已按 `40_局外成长底盘正式配置落地设计.md` 和 `42_局外成长义体正式配置落地设计.md` 前置补齐 9 个正式成长 VisualID：`chassis_standard_frame_icon`、`chassis_compact_raider_icon`、`chassis_bulwark_carrier_icon`、`prosthetic_focus_lens_icon`、`prosthetic_anchor_left_arm_icon`、`prosthetic_charge_coil_arm_icon`、`prosthetic_san_regulator_core_icon`、`prosthetic_salvage_fingertips_icon`、`prosthetic_mender_spine_icon`；已补 seed、Prompt / Spec、local_v0 Approved PNG 和 Unity `.meta`。
- 已刷新 latest 可接入清单、缺图生成计划、质量清单、Visual V2 计划、Formal V1 验收队列和程序交接清单：当前 `program_integrate=54`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=62`，正式 AI 替换批次为 `nai_visual_v2_20260525_07`。
- 已按 `44_局外成长人偶特质房间正式配置落地设计.md` 前置补齐 8 个 Room Memento VisualID：`memento_first_repair_patch`、`memento_first_chassis_frame`、`memento_first_prosthetic_case`、`memento_san_collapse_blanket`、`memento_miracle_burn_mark`、`memento_boss1_lamp`、`memento_layer2_corrosion_vial`、`memento_return_mark`；已补 seed、Prompt / Spec、local_v0 Approved PNG 和 Unity `.meta`。
- 已刷新 latest 可接入清单、缺图生成计划、质量清单、Visual V2 计划、Formal V1 验收队列和程序交接清单：当前 `program_integrate=62`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=70`，正式 AI 替换批次为 `nai_visual_v2_20260525_08`。
- 已按 `48_经济压力传闻正式配置落地设计.md` 和 `49_经济压力势力正式配置落地设计.md` 前置补齐 13 个 P4 经济压力 VisualID：8 个传闻图标与 5 个势力徽章；已补 seed、Prompt / Spec、local_v0 Approved PNG 和 Unity `.meta`。
- 已对 13 个 P4 经济压力图标做 focused verification：PNG 均为 `512x512`，有 alpha，主体 bbox 居中；Manifest 均为 `Status=approved`、`QualityTier=local_v0`，`PromptEN` 和结构化 `Spec.SourceSpec=512x512` 齐全。
- 已按 `50_经济压力订单正式配置落地设计.md` 前置补齐 29 个 P4 经济压力订单 VisualID：19 个订单模板图标与 10 个订单类型 / 状态复用图标；已补 seed、Prompt / Spec、local_v0 Approved PNG 和 Unity `.meta`。
- 已对 29 个 P4 经济压力订单图标做 focused verification：PNG 均为 `512x512` RGBA，含 alpha，主体 bbox 居中；Manifest 均为 `Status=approved`、`QualityTier=local_v0`，`PromptEN`、`NegativePromptEN` 和结构化 `Spec.SourceSpec=512x512` 齐全。
- 已在 P4 经济压力传闻 / 势力 / 订单批次后刷新 latest 可接入清单、缺图生成计划、质量清单、Visual V2 计划、Formal V1 验收队列和程序交接清单。
- 已把 `settlement` 升级为 CombatOutcomeReport 五态结果报告 active 合同：新增胜利、HP 战败、SAN 崩溃、HP+SAN 复合战败和队伍溃败五个结果徽记 VisualID，并补齐 seed、Prompt / Spec、local_v0 Approved PNG 和 Unity `.meta`。
- 已在 `settlement` 五态结果徽记批次后刷新 latest 可接入清单、缺图生成计划、质量清单、Visual V2 计划、Formal V1 验收队列和程序交接清单。
- 已新增 `Scan-ArtRequirementCandidates.ps1` / `scan_art_requirement_candidates.py`，从设计文档、配置表、版本规划和 active UI 文档扫描潜在美术需求候选；latest 输出 `new_candidate=36`、`approved_without_manifest=0`、`seed_only=0`、`manifest_managed=158`，快照为 `美术文档/_generated/art_requirement_candidate_snapshots/20260525_074546_art_requirement_candidates_v2_20260525.*`。
- 已新增 `Normalize-ArtQualityTier.ps1` / `normalize_art_quality_tier.py`，把历史本地生成 Approved 资产规范化为 `QualityTier=local_v0`：本轮识别 `legacy_candidates=132`，实际补标 35 条，正式质量层级 `formal_ai_v2/final/production` 不会被降级。
- 已验证 NovelAI token 链路并完成首个正式 AI 替换：`node_eventnode_icon` 已生成、预处理并同步到 `UnityClient/Assets/Art/Approved/Nodes/Icons/node_eventnode_icon.png`，Manifest 中 `QualityTier=formal_ai_v2`、`ReplacementBatchID=nai_visual_v2_probe_20260525_02`。
- 已完成 P0 新节点图标正式 AI 替换：`node_hazardnode_icon`、`node_reststopnode_icon`、`node_treasurenode_icon` 已通过 NovelAI 串行生成、预处理和 Approved 同名替换；连同 `node_eventnode_icon`，4 个新节点图标均为 `QualityTier=formal_ai_v2`。
- 已刷新 latest 队列：`program_integrate=0`、`acceptance_needed=191`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=128`；Visual V2 下一执行批次为 `nai_visual_v2_20260526_02`，`PromptReadyItems=128`。
- 已完成 P1 核心战斗意图图标正式 AI 替换：`ui_combat_intent_attack`、`ui_combat_intent_defend`、`ui_combat_intent_buff`、`ui_combat_intent_debuff` 均已串行生成、预处理、人工筛选并同步为 `QualityTier=formal_ai_v2`；`debuff` 已因首轮语义偏差修正提示词模板和专用负面词。
- 已刷新 latest 队列：`program_integrate=0`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=124`，Visual V2 下一执行批次为 `nai_visual_v2_20260526_03`；Formal V1 验收队列显示 21 个 active 界面均已 captured，当前进入美术截图验收。
- 已加固 Visual V2 Approved 同名替换工具：`Sync-ApprovedArt.ps1 -CandidateBatchID` 默认拒绝创建新 Approved 路径，要求目标 PNG / `.meta` 已存在并校验 `.meta` 同步前后不变；`-AllowNewTargetWithCandidate` 仅用于明确创建新资产路径，不用于已接入素材的正式图替换。已用临时 Manifest 验证 strict dry-run、缺目标默认失败和显式放行新目标三种路径。
- 已完成 P1 首批战斗反馈 / 标记正式 AI 替换：`ui_combat_feedback_hit`、`ui_combat_feedback_shield_break`、`ui_combat_grid_lock_marker` 已通过 NovelAI 串行生成、预处理、contact sheet 人工筛选，并以 `meta_guard=strict` 同名同步为 `QualityTier=formal_ai_v2`；Approved PNG 尺寸 / alpha 校验通过，Unity `.meta` 未改变。
- 已刷新 latest 队列：`program_integrate=0`、`generate_needed=29`、`technical_fix=0`、`visual_v2_replace=121`；当前缺图批次为 `nai_v2a_runtime_missing_20260608_01`，Visual V2 质量替换批次为 `nai_v2a_runtime_quality_20260608_01`。
- 已主动触发并验收程序接入后的 latest ArtAcceptance：RunID=`20260527_002436`，21/21 截图、`PASSED`、Registry EntryCount=191、MissingRequiredVisualIDs=0、UI snapshot risks=0；已刷新 Formal V1 验收队列和程序交接清单，当前 `program_integrate=0`、`add_capture=0`、`rerun_acceptance=0`。
- 已把本轮人工验收写入 `美术文档/09_运行时美术验收记录.md`：`inventory_loot`、`settlement` 通过；`layer_select`、`maintenance_panel`、`daily_bill_report`、`business_settlement`、`chassis_upgrade_panel`、`doll_interaction`、`doll_room`、`order_board`、`rumor_board`、`faction_shop`、`scenario_event`、`shop_staging`、`workshop_main` 条件通过；`combat_hud`、`dungeon_map`、`safe_room`、`stairs_room`、`sell_panel`、`prosthetic_panel` 不通过，需程序侧返修截图状态或补有效列表数据后重跑。
- 已建立 Formal V2 UX/UI 重构设计层：新增 `美术文档/ui_design/formal_v2/README.md`、`00_formal_v2_ux_ui_overview.md` 和 V2-A 五个核心界面设计入口，明确 Formal V2 先解决按钮堆叠、主次行动不清、场景隐喻不足和正式感不足。
- 已将 V2-A 五个核心界面从入口占位推进为可评审草案：`01_workshop_main_v2.md`、`02_combat_hud_v2.md`、`03_inventory_loot_v2.md`、`04_dungeon_map_v2.md`、`05_settlement_v2.md` 均已覆盖 Formal V1 问题、玩家目标、主结构、信息层级、行动层级、程序迁移、素材变化和 UX 验收标准。
- 已在 Formal V2 总方案补充工具策略：Figma、Unity MCP、截图标注和 PlayMode 布局扫描只作为设计 / 验收辅助，不替代 active `screen_layouts.json`、Manifest、程序交接清单和 ArtAcceptance。
- 已生成 V2-A 五个核心界面概念参考图并归档到 `美术文档/ui_design/formal_v2/concepts/`：`workshop_main`、`combat_hud`、`inventory_loot`、`dungeon_map`、`settlement`。这些图只用于结构、氛围和视觉重心评审，不作为运行时素材。
- 已按用户反馈替换 Formal V2 概念图风格和结构：`workshop_main` 改为魔偶中心安心房间，新增 `workshop_studio` 工作室参考图；`inventory_loot` 改为半透明战斗场景清点层；`dungeon_map` 改为地图 / 节点视觉中心且暂不保留常驻节点详情；`combat_hud`、`settlement` 统一降低硬核感和信息密度。
- 已补齐 Formal V2-B 七个局外功能界面详细草案：`06_maintenance_panel_v2.md`、`07_prosthetic_panel_v2.md`、`08_chassis_upgrade_panel_v2.md`、`09_sell_panel_v2.md`、`10_shop_staging_v2.md`、`11_business_settlement_v2.md`、`12_daily_bill_report_v2.md`；并同步 `formal_v2/README.md`、`00_formal_v2_ux_ui_overview.md` 和对应 Formal V1 文档的双向关系。
- 已生成 Formal V2-B 局外功能概念参考图；其中维护 / 义体 / 底盘升级和旧 `sell_panel` 概念图在用户反馈后归档，2026-06-06 已用内置 imagegen 按 `workshop_studio` 子面板和小镇商店语义重出 4 张新图。所有图片仅用于设计评审，不进入 Approved、Manifest 或程序交接清单。
- 已按用户反馈修正 V2-B 语义并整理概念图目录：维护 / 义体 / 底盘升级改为 `workshop_studio` 子面板，旧三张独立大场景图归档；`sell_panel` 改为小镇商店 / 市场交易，旧工坊估价柜台图归档；当前 Formal V2 active 界面草案和 AI 概念图均已补齐。
- 已补齐 Formal V2-C 九个剩余 active 界面详细草案：`13_layer_select_v2.md`、`14_safe_room_v2.md`、`15_stairs_room_v2.md`、`16_order_board_v2.md`、`17_rumor_board_v2.md`、`18_faction_shop_v2.md`、`19_doll_interaction_v2.md`、`20_scenario_event_v2.md`、`21_doll_room_v2.md`。至此 21 个 active UI 界面均已有 Formal V2 可评审方案。
- 已新增 `Generate-FormalV2DesignBoards.ps1` / `generate_formal_v2_design_boards.py`，生成确定性 Formal V2 结构设计图；当前 `design_boards/` 已覆盖 21 个 active 界面和 `workshop_studio`，共 22 张 PNG，并生成 `formal_v2_design_boards.json` 和目录说明。
- 已明确区分 `concepts/` 与 `design_boards/`：前者是 AI 氛围概念图，后者是结构 layout board；两者都不是 Approved 运行时素材、Manifest 条目或程序接入口。2026-06-06 已用内置 imagegen 补齐 13 张缺口概念图，当前 AI 概念图总数为 22 张。
- 已新增 Formal V2 概念图评审索引和 contact sheet：`concepts/review_index.md` 记录评审顺序与口径，`concepts/contact_sheets/` 生成 V2-A、V2-B、V2-C 和全部 22 张概念图总览，方便用户横向审风格统一性和信息密度。
- 历史 2026-06-07 曾按“日系二次元地底奇幻 + 轻蒸汽工艺”重出 `workshop_main`、`combat_hud`、`dungeon_map` 概念图；该口径现已被 2026-06-12 纠偏覆盖。设计图 / 概念图默认仍用 Codex 内置 `image_gen`，无 `image_gen` 时必须先提醒用户，不能自动切换到 NovelAI 或其他生图渠道。先前误生成的 NovelAI 临时概念图目录已清理，未进入正式 `concepts/`。
- 已在当前工具环境试通 Codex 内置 `image_gen`，并按新风格同名替换 `workshop_main_formal_v2_concept.png`、`combat_hud_formal_v2_concept.png`、`dungeon_map_formal_v2_concept.png`；旧图归档到 `concepts/archive/2026-06-07_anime_style_regen/`，四张 contact sheet 已刷新。
- 已补充层地图设计规则：`dungeon_map` 不是单屏静态节点板，而是可推进大地图；地图需要前景 / 中景 / 远景纵深，支持玩家沿路线前进和镜头前移；每层可以有独立生态主题，例如地底草原、地下森林、晶洞、遗迹、雾谷、矿坑或湿地。新版 `dungeon_map_formal_v2_concept.png` 已按该规则重出，旧图归档到 `concepts/archive/2026-06-07_layer_map_depth_regen/`。
- 已直接处理程序侧 ArtAcceptance 验收工具问题并提交 `771149e`：截图点前清理跨界面残留，`combat_hud` 进入独立战斗状态，`ui_snapshot` 过滤不可见残留元素；下一轮需要 Unity 重跑验收确认截图和报告。
- 已创建并推进本地 P3 mission `.mission/20260608_232238-FormalV2-全量素材实际生产迭代.csv`：`ARTPROD-V2-01..05` 已完成，真实 NovelAI 缺图生成 `58/58`、缺图预处理 `58/58`、Approved 同步 `58/58`、首批 P1 combat readability 质量替换生成 / 预处理 / 同步 `10/10`；验证通过 `validate_ui_design.py`、`tools/docs/validate_docs.py --index docs_index.json` 和 mission validate。
- 已刷新 FormalV2 运行时素材队列：`美术文档/_generated/可接入素材清单.md/json` 当前 `program_integrate=58`、`generate_needed=0`；`美术文档/_generated/素材质量替换清单.md/json` 当前 `technical_fix=17`、`visual_v2_replace=0`；`美术文档/_generated/VisualV2生成计划.md/json` 当前 `planned=0`、`prompt_ready=0`。Visual V2 同名替换层已清空，下一步转入怪物头像 alpha 技术修复。
- 已完成 P1 core backgrounds 正式替换：`bg_safe_room`、`bg_stairs_room` 均已同步为 `QualityTier=formal_ai_v2`，Approved PNG 为 `1920x1080` 且 alpha 全不透明；这两个同名替换保持原 VisualID / Approved 路径 / `.meta`，程序侧无需重新登记。
- 已完成 P1/P2 UI skin core 正式替换：`ui_panel_main`、5 个 settlement outcome 徽记、`ui_dungeon_node_plate`、`ui_dungeon_route_line`、`ui_icon_sale_spark`、`ui_list_row_normal`、`ui_list_row_selected`、`ui_room_memento_slot`、`ui_settlement_defeat_panel`、`ui_settlement_victory_panel`、`ui_title_divider` 均已同步为 `QualityTier=formal_ai_v2`，保持原 VisualID / Approved 路径 / `.meta`。
- 已完成 P2 shared UI icons 正式替换：31 个共享 UI 图标均已同步为 `QualityTier=formal_ai_v2`，保持原 VisualID / Approved 路径 / `.meta`；其中 `ui_icon_chassis_upgrade` 和 `ui_icon_order` 语义可用但偏弱，后续运行时截图如小尺寸不清晰，可进入二次质量替换。
- 已完成 P2 scene backgrounds 正式替换：`bg_doll_room_attic`、`bg_layer_select`、`bg_settlement_defeat`、`bg_settlement_victory` 均已同步为 `QualityTier=formal_ai_v2`，Approved PNG 为 `1920x1080` 且 alpha 全不透明；这四个同名替换保持原 VisualID / Approved 路径 / `.meta`。
- 已完成 P2 economy / order / rumor / faction icons 正式替换：42 个经济、订单、传闻和势力图标均已同步为 `QualityTier=formal_ai_v2`，保持原 VisualID / Approved 路径 / `.meta`；本批次已通过 512x512 尺寸、Manifest 字段和队列刷新验证。
- 已完成 P2 growth / chassis / prosthetic / room memento assets 正式替换：17 个成长、底盘、义体和房间纪念物资源均已同步为 `QualityTier=formal_ai_v2`，保持原 VisualID / Approved 路径 / `.meta`；提交为 `0cfc13c art: replace FormalV2 growth room assets`。
- 已完成怪物 portrait alpha 技术修复：新增并执行 `tools/美术工具/fix_opaque_art_alpha.py`，将 17 张 `monster_*_portrait` 的 alpha 修为全 255；刷新 `可接入素材清单`、`素材质量替换清单` 和 `VisualV2生成计划` 后，当前 `generate_needed=0`、`technical_fix=0`、`visual_v2_replace=0`、`spec_review=0`、`planned=0`。
- 已补齐 `nai_formalv2_missing_20260608_01` 58 个新缺图正式资产的 Manifest 质量字段，并刷新 `程序接入交接清单`：当前程序侧应登记 58 个 `formal_ai_v2` VisualID，且机器核对 PNG / `.meta` / QualityTiers 全通过。

- 已新增并执行 `Generate-FormalV2AssetReview.ps1` / `generate_formal_v2_asset_review.py`，对 latest `program_integrate=58` 的 Formal V2 Approved 素材做程序登记前静态预验收；输出位于 `美术文档/_generated/formal_v2_asset_review/`，快照为 `美术文档/_generated/formal_v2_asset_review_snapshots/20260609_041011_formalv2_program_integrate_58_precheck/`。结论：`reviewed=58`、`pass=58`、`fail=0`、`warn=0`，其中 item=23、monster=34、background=1，PNG / `.meta` / `formal_ai_v2` / 尺寸 / alpha 均符合 Manifest SourceSpec。
- 已补充 `美术文档/_generated/formal_v2_asset_review/visual_semantic_review.md/json` 和快照 `formalv2_program_integrate_58_semantic_review`：技术预检仍允许程序接入 58 个 VisualID，但美术侧标出物品图标同质化和怪物 portrait 小尺寸可读性风险；当前记录 `secondary_replacement_candidate=13`、`watch_runtime_readability=19`、`ok_for_current_v2=26`，后续二次 NovelAI 替换应以运行时截图证明为准。

2026-06-09 added FormalV2 prompt-ready generation executor: `Run-FormalV2PromptReadyGeneration.ps1` / `run_formal_v2_prompt_ready_generation.py`. Verification: `python -m py_compile` passed; dry-run passed for `item` limit 2 and full 58 prompt-ready `program_integrate` assets, split as item=23, monster=34, background=1, all routed through `Run-ArtGeneration.ps1 -Provider novelai -Status approved -PreserveStatus -Concurrency 1 -DelaySeconds 1 -DryRun`. Current shell still has `NAI_ACCESS_TOKEN=NOT_SET`, so this round did not call NovelAI and did not replace Approved PNGs.
2026-06-09 已完成 FormalV2 新一轮素材候选准入审查：基于候选快照 `formalv2_goal_resume_20260609` 从 `new_candidate=84` 中确认 29 个正式需求写入 `art_requirements_seed.json`，包括 5 个 FormalV2 场景背景、3 个配置直引战斗反馈、7 个房间纪念物、6 个订单图标和 8 个传闻图标；已新增 `美术文档/14_FormalV2素材候选审查记录.md` 记录准入 / 暂缓 / 误报裁决。刷新后 Manifest `Entries=284`，候选清单为 `new_candidate=55`、`seed_only=0`、`manifest_managed=244`，缺图生成计划曾为 `planned=29`、`prompt_ready=29`，批次 `nai_formalv2_candidate_triage_20260609_01`。
2026-06-09 已完成 FormalV2 候选准入 29 个新素材的真实 NovelAI 入库：先补强 `generate_art_prompts.py` 中 29 个新增 ID 的具体 PromptEN，针对 6 个语义跑偏风险项执行二次修复批次 `nai_formalv2_candidate_semantic_fix_20260609_01`，串行生成 `24/24` 张候选并人工筛选 6 张；随后将原批次剩余 23 项以 processed fallback 同步 Approved。新增 29 个 PNG 均已补齐 Unity `.meta`，Manifest 均为 `QualityTier=formal_ai_v2`；最终 `Generate-FormalV2AssetReview.ps1 -SnapshotTag formalv2_candidate_triage_after_meta_fix` 结果为 `reviewed=77`、`pass=77`、`fail=0`、`warn=0`。latest 队列为 `program_integrate=77`、`generate_needed=0`、`art_select=0`、`technical_fix=0`、`visual_v2_replace=0`、`spec_review=0`。
- 已新增 `美术文档/15_FormalV2运行时验收待办清单.md`，把当前 FormalV2 运行时验收门禁从聊天结论收敛为文档：当前必须等待程序登记 latest `program_integrate=77` 并产出晚于 `20260606_230523` 的 ArtAcceptance RunID；美术侧后续按该清单复核 `report.json`、`registry_snapshot.json`、`ui_snapshot.json`、contact sheet 和 16 个需重跑截图，再决定是否发起二次 NovelAI 替换。
- 已完成一轮 FormalV2 运行时验收前复查：本地 latest ArtAcceptance 仍为旧 RunID `20260606_230523` 且 `FAILED`，早于当前 77 个待登记素材，不能作为当前 V2 通过 / 失败证据；已刷新 latest 队列和快照，当前仍为 `program_integrate=77`、`rerun_acceptance=16`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=0`、`spec_review=0`、`new_candidate=0`，`FormalV2AssetReview reviewed=77/pass=77`，`PromptReadiness prompt_ready=77/prompt_blocked=0`。
- 已新增 `Validate-ArtGeneratedJson.ps1` / `validate_art_generated_json.py`，作为 FormalV2 美术交接的 UTF-8 JSON 可读性门禁；当前严格校验通过，确认 `可接入素材清单`、`程序接入交接清单`、`素材质量替换清单`、`美术需求候选清单`、`formal_v2_asset_review` 和 `formal_v2_prompt_readiness` 均能按 UTF-8 解析，关键计数为 `program_integrate=77`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=0`、`new_candidate=0`、`reviewed=77/pass=77`、`prompt_ready=77/prompt_blocked=0`。
- 已新增 `Generate-ArtRegistryGapChecklist.ps1` / `generate_art_registry_gap_checklist.py`，将当前 FormalV2 程序接入队列进一步压成 `VisualAssetRegistry登记缺口清单.md/json`；本轮核对 `UnityClient/Assets/Resources/VisualAssetRegistry.asset` 后确认 `program_integrate=77`、`missing_registry=77`、`missing_approved=0`、`missing_meta=0`。程序侧可直接在 Unity Editor 执行 `Tools/P3 Art/Rebuild Approved Sprite Registry`，保存 Registry 后重跑 ArtAcceptance。
- 已新增 `Generate-FormalV2RuntimeAcceptanceStatus.ps1` / `generate_formal_v2_runtime_acceptance_status.py`，把 latest ArtAcceptance、Registry 缺口和程序 handoff 聚合为 `FormalV2运行时验收状态.md/json`；当前 gate 为 `waiting_registry`，证据为 `program_integrate=77`、`missing_registry=77`、latest ArtAcceptance `RunID=20260606_230523` / `Status=FAILED`。该状态报告用于后续自动判断何时进入美术侧逐屏截图验收。
- 已新增并执行 `Generate-FormalV2VisualSemanticReview.ps1` / `generate_formal_v2_visual_semantic_review.py`，把 FormalV2 77 个待登记素材的运行时语义风险从旧 58 项人工记录升级为可复跑报告；输出位于 `美术文档/_generated/formal_v2_asset_review/visual_semantic_review.md/json`，快照为 `formalv2_program_integrate_77_semantic_review`。当前结论：`reviewed=77`、`ProgramIntegrationDecision=allow_program_integrate`、`ok_for_current_v2=41`、`watch_runtime_readability=25`、`style_mismatch_watch=2`、`secondary_replacement_candidate=9`。这些风险不阻塞程序登记，只作为后续 ArtAcceptance 运行时截图复核和二次 NovelAI 同名替换的优先级依据；`Validate-ArtGeneratedJson.ps1 -Strict` 已纳入该 JSON 并通过。
- 已完成 FormalV2 语义二次正式替换批次 `nai_formalv2_semantic_replace_20260609_02`：针对 `visual_semantic_review` 标出的 9 个 `secondary_replacement_candidate` 串行调用 NovelAI 生成 33 张候选，人工筛选 9 张并以 strict meta guard 同名同步 Approved；替换项为 `item_con_purifying_salt_icon`、`item_gear_corroded_bulwark_icon`、`item_gear_mycelium_cloak_icon`、`item_gear_spore_lance_icon`、`item_loot_acid_gland_icon`、`item_loot_corroded_nerve_icon`、`monster_boss_spore_foundry_portrait`、`monster_mob_acid_slime_mature_portrait`、`monster_mob_echo_pilgrim_portrait`。本批次没有改变 VisualID、Approved 路径或 Unity `.meta` / GUID；3 张头像同步后带轻微半透明 alpha，已用 `fix_opaque_art_alpha.py` 修为完全不透明。刷新后 `FormalV2AssetReview reviewed=77/pass=77`、`素材质量替换清单 technical_fix=0/visual_v2_replace=0/spec_review=0`、`visual_semantic_review secondary_replacement_candidate=0`、`ProgramIntegrationDecision=allow_program_integrate`。

## 下一步建议

1. 程序侧不再需要登记资源：latest `program_integrate=0`、`missing_registry=0`、ArtAcceptance `20260613_020257 PASSED`、Registry 278、`MissingRequiredVisualIDs=0`、`RealGameplay=21`、`FormalV1Template=0`。
2. FormalV2 UI 后续运行时视觉 / 布局精修由美术 / UI 侧直接处理，仍遵守纯 UGUI、active `screen_layouts.json`、真实玩家流程和 ArtAcceptance 证据口径；只有领域服务、工具链、Unity 工程约束或测试底座问题再交给程序侧。
3. `T0-VAL-01` 当前以条件通过作为运行时纵切证据收口；若继续追最终商业化封板，下一步聚焦漫画页排版 / 字幕条、对白 UI 皮肤、半开放工坊最终视觉、首潜许可卡专属视觉，以及 ArtAcceptance / 主美人工截图验收。
4. FormalV2 运行时 UI 当前已通过资源 / Registry / ArtAcceptance 工具门禁和当前纵切基线封版；下一轮美术 / UI 侧按玩家流程继续做质量迭代，优先区分共享工坊子面板的专屏结构，其次再处理背景候选、动画 / VFX、图标语义和真实玩家流程中的小尺寸可读性复查。
4. 当前不要重开批量资源登记或批量 NovelAI 补图；如果下一轮截图证明某个 VisualID 在真实尺寸下语义或可读性不足，再做单项 NovelAI 同名替换。仍必须单图串行：`-Concurrency 1 -DelaySeconds 1`，已接入同名替换必须使用 strict meta guard。
5. 后续每次刷新美术 `_generated` 关键 JSON 后，运行 `Validate-ArtGeneratedJson.ps1 -Strict`，确认程序交接和美术验收依赖的 JSON 在 UTF-8 读取下可解析。

## 问题 / 阻塞

- `T0-VAL-01` 当前项目结论为 `conditional_pass:t0_val_01_formal_v2_runtime_slice`。美术 / UI 侧不再把它记为“没有序章 / 美术不适配 / 工坊像调试 UI”的 P0 主链阻塞，但必须保留边界：这不是完整 T0 序章、最终 CG / Live2D、ArtAcceptance 或主美商业化封板。
- 当前工作区仍有大量程序、策划、Unity 资产和知识库生成物处于脏状态；美术侧提交时只纳入本轮美术流水线相关文件。
- P0 的 `validated` 是 MVP Baseline 骨架验收通过，不代表 Formal V1 / FormalV2 正式视觉完成；当前必须以 latest ArtAcceptance、逐屏截图复验和本状态页结论为准。
- `NARR-03` 已有 Unity runtime smoke、PlayMode 状态和 T0-VAL-01 连续截图证据；T0-01A 序章 CG 首版 Approved Panel 也已完成。当前仍缺最终画面级 ArtAcceptance 旁路、主美人工复核和必要的同 VisualID polish，因此不能证明最终画面质量通过。
- `T0-FLOW-03` 已有半开放工坊 live 截图；当前仍缺正式 Panel / 工坊过渡素材、Manifest / Registry 接入和 ArtAcceptance 旁路，因此只能证明 staging 画面可见，不能证明商业画面验收通过。
- `T0-FLOW-04` 已有首潜第一层确认允许态 / 阻断态 live 截图；当前仍缺正式首潜许可卡视觉补强、Manifest / Registry 接入和 ArtAcceptance 旁路，因此只能证明 staging 画面可见，不能证明商业画面验收通过。
- 本轮已从外部 `F:\my_project\new\tags_machine\novelai\client.py` 只读提取 NovelAI token 到当前进程并真实调用 NovelAI；未提交 token，未使用 mock / local_v0 冒充正式图。后续跑图仍必须单图串行：`-Concurrency 1 -DelaySeconds 1`。
- FormalV2 资源登记门禁已清空，最新 ArtAcceptance `20260613_020257` 工具层通过且 `FormalV1Template=0`；当前 FormalV2 runtime UI visual seal 已通过纵切基线。共享工坊子面板仍需后续专屏差异化，但不再作为当前封版阻塞。
- FormalV2 V2-A 已进入 active 规格；V2-B / V2-C 仍按 draft 管理，未写入 active 前不作为程序接入口。
- Formal V2 概念图含 AI 伪文字和局部装饰噪声，只能作为结构参考；正式接入前仍需将控件、文本、图标和面板皮肤拆回可实现规格。
- Formal V2 结构设计图是确定性 layout board，只用于评审结构和迁移顺序；不能被当作最终视觉稿或程序接入规格。
- `tools/ComfyUI_NAIDGenerator/` 未跟踪，需要决定是否纳入正式美术流水线。

## 完成回写清单

- 更新本文件的 `最后更新`。
- 在 `最近完成` 记录简短事实。
- 如有变化，刷新 `当前关注`、`下一步建议` 和阻塞项。
- 如果需要程序或策划跟进，在 `PROJECT_STATUS.md` 增加跨职能交接。
- 美术侧每轮实际交付完成后，提交本轮美术相关改动；提交范围必须排除程序、策划、子模块或本地工具无关改动。

## 2026-06-09 FormalV2 Requirement Candidate Decisions

- Added machine-readable candidate review decisions at `美术文档/art_requirement_candidate_decisions.json` and wired them into `Scan-ArtRequirementCandidates.ps1` / `scan_art_requirement_candidates.py`.
- Latest requirement scan snapshot `formalv2_candidate_decisions_20260609`: `new_candidate=0`, `deferred_candidate=29`, `ignored_candidate=26`, `manifest_managed=244`, `approved_without_manifest=0`, `seed_only=0`.
- Refreshed current art queues after decisions: `program_integrate=77`, `generate_needed=0`, `art_select=0`, `art_process=0`; quality backlog remains `technical_fix=0`, `visual_v2_replace=0`, `spec_review=0`; FormalV2 static asset review remains `reviewed=77`, `pass=77`.
- Verification passed: `python -m py_compile tools\美术工具\scan_art_requirement_candidates.py`; `Validate-UIDesign.ps1`; `python tools\docs\validate_docs.py --index docs_index.json`; `git diff --check` reported only line-ending warnings on unrelated dirty files and touched art scripts.

## 2026-06-09 FormalV2 Style Feedback Replacement

- Completed the remaining `style_mismatch_watch` cleanup for `ui_combat_feedback_echo_fade` and `ui_combat_feedback_slime_pop`: refreshed prompt rules, generated two serial NovelAI candidate batches with `-Concurrency 1 -DelaySeconds 1`, then applied controlled color postprocess so the final Approved sprites fit the Japanese subterranean fantasy V2 palette.
- Synced both replacements with strict meta guard and preserved VisualID / Approved path / Unity `.meta` GUID: `UnityClient/Assets/Art/Approved/UI/ui_combat_feedback_echo_fade.png` and `UnityClient/Assets/Art/Approved/UI/ui_combat_feedback_slime_pop.png`. Both are `512x512` RGBA with transparent alpha.
- Refreshed handoff, registry gap, FormalV2 static review, semantic review, quality backlog, runtime acceptance status and snapshots. Current gate remains `waiting_registry`: `program_integrate=77`, `missing_registry=77`, `missing_approved=0`, `missing_meta=0`, latest ArtAcceptance still `RunID=20260606_230523 / FAILED`.
- Verification passed: image size / alpha inspection; `Validate-ArtGeneratedJson.ps1 -Strict`; `Validate-UIDesign.ps1`; `python tools\docs\validate_docs.py --index docs_index.json`; `git diff --check` reported only CRLF warnings on existing dirty p3-mission files and touched art scripts.

## 2026-06-09 FormalV2 Remaining Item Icon Replacement

- Added `-ConfigID` filtering to `Run-ArtGeneration.ps1` / `run_art_generation.py` so art generation can target a specific config row when several config entries share one VisualID; dry-run confirmed the remaining item pass selects 13 config entries instead of 16 mixed shared-VisualID entries.
- Strengthened `generate_art_prompts.py` for the remaining item icons with concrete English visual descriptions and config-specific negative prompts that exclude lantern / lamp / jar / bottle / vessel drift. `Generate-ArtPrompts.ps1` refreshed the affected Manifest prompt fields.
- Ran real NovelAI batch `nai_formalv2_remaining_items_20260609_01` serially with `-Concurrency 1 -DelaySeconds 1`: 13 entries, 52 generated candidates, 52 raw images, no generation errors. Manually selected one candidate each for `item_loot_rusty_coil_icon`, `item_loot_toxic_filter_icon`, `item_mat_core_tier1_icon`, `item_loot_spore_amber_icon`, `item_loot_vein_plate_icon`, `item_loot_warped_plate_icon`, `item_mat_core_tier2_fragment_icon`, `item_mat_core_tier3_seed_icon`, `item_order_contested_spore_core_icon`, `item_order_live_spore_cage_icon`, `item_trade_luminous_fungus_icon`, `item_trade_sealed_relic_box_icon`, and `item_trade_singing_fossil_icon`.
- Synced the 13 selected candidates into `UnityClient/Assets/Art/Approved/Items/Icons/` with strict `.meta` guard and `QualityTier=formal_ai_v2`; VisualID, Approved path and Unity `.meta` GUIDs were preserved. Image inspection confirmed all 13 Approved PNGs are `512x512` RGBA with valid transparent alpha and existing `.meta`.
- Refreshed handoff, registry gap, FormalV2 asset review, visual semantic review, prompt readiness and runtime acceptance status. Current evidence: `program_integrate=87`, `missing_registry=87`, `missing_approved=0`, `missing_meta=0`, `FormalV2AssetReview reviewed=87/pass=87`, `visual_semantic_review secondary_replacement_candidate=0`, `decision=allow_program_integrate`, gate remains `waiting_registry` until Unity registry rebuild and a fresh ArtAcceptance run.
- Verification passed: `python -m py_compile tools\美术工具\generate_art_prompts.py`; `python -m py_compile tools\美术工具\run_art_generation.py`; `Validate-ArtGeneratedJson.ps1 -Strict`; `Validate-UIDesign.ps1`; `python tools\docs\validate_docs.py --index docs_index.json`; `git diff --check` reported only CRLF warnings on unrelated dirty p3-mission files and touched art scripts.

## 2026-06-09 FormalV2 Runtime Review Queue

- Added `Generate-FormalV2RuntimeReviewQueue.ps1` / `generate_formal_v2_runtime_review_queue.py` to turn `visual_semantic_review` into an art-side screenshot review queue after program registry rebuild and ArtAcceptance rerun.
- Generated `美术文档/_generated/FormalV2运行时复验优先级清单.md/json` and snapshot `formalv2_waiting_registry_87_20260609`: current gate is `waiting_registry`, `program_integrate=87`, `missing_registry=87`, `review_queue=30`.
- The queue groups the 30 non-blocking watch items into `item_icon_semantics=7`, `monster_portrait_readability=15`, `memento_icon_semantics=2`, `order_icon_semantics=1`, `rumor_icon_semantics=3`, and `ui_feedback_readability=2`; primary review screens are `combat_hud`, `order_board`, `rumor_board`, `inventory_loot`, `safe_room`, and `stairs_room`.
- Updated `美术文档/15_FormalV2运行时验收待办清单.md` from 77 to 87 current program-integrate VisualIDs and linked the generated runtime review priority queue as the next art review entrance.
- Extended `Validate-ArtGeneratedJson.ps1 -Strict` coverage to include the runtime review queue. Verification passed: `python -m py_compile tools\美术工具\generate_formal_v2_runtime_review_queue.py tools\美术工具\validate_art_generated_json.py`; `Validate-ArtGeneratedJson.ps1 -Strict`; `Validate-UIDesign.ps1`; `python tools\docs\validate_docs.py --index docs_index.json`.

## 2026-06-09 FormalV2 Registry Rebuild Attempt

- Art-side attempted to unblock `waiting_registry` directly by running Unity `2022.3.60f1` batchmode with `-executeMethod VisualAssetRegistryEditorTools.RebuildApprovedSpriteRegistryFromApprovedFolder`.
- Unity exited before project load with code `199`: `UnityClient/Logs/rebuild_registry_batch.log` shows `LicensingClient` IPC timeout after 60 seconds. `UnityClient/Assets/Resources/VisualAssetRegistry.asset` remained unchanged at `2026-05-26 00:11:19`, and latest ArtAcceptance remained `RunID=20260606_230523 / FAILED`.
- Updated `美术文档/15_FormalV2运行时验收待办清单.md` with this evidence and clarified that program side should run the Registry rebuild from a licensed Unity Editor session if batchmode is unavailable. Gate remains `waiting_registry`.

## 2026-06-09 FormalV2 Offline Registry Candidate

- Added `tools/美术工具/Generate-VisualAssetRegistryOfflineCandidate.ps1` as a conservative fallback aid for the current Unity licensing blocker. It reads `VisualAssetRegistry.asset`, `VisualAssetRegistry登记缺口清单.json`, and each Approved PNG `.meta`, then writes an offline candidate instead of touching the live Unity registry.
- Generated `美术文档/_generated/VisualAssetRegistry.offline_candidate.asset` and `美术文档/_generated/VisualAssetRegistry离线候选报告.md`. Verification: candidate has `278` unique entries, adds the current `87` missing VisualIDs, preserves `MissingSprite`, and leaves `UnityClient/Assets/Resources/VisualAssetRegistry.asset` unchanged.
- This candidate is only a program-side review/diff aid. FormalV2 runtime gate remains `waiting_registry` until the real Unity `VisualAssetRegistry.asset` is rebuilt/saved and ArtAcceptance produces a fresh run.

## 2026-06-10 FormalV2 Registry Applied and Runtime Review

- Art side applied `美术文档/_generated/VisualAssetRegistry.offline_candidate.asset` to `UnityClient/Assets/Resources/VisualAssetRegistry.asset`, preserving `ui_missing_sprite` and expanding the runtime registry to `EntryCount=278`.
- Refreshed registry, handoff, FormalV2 runtime status and review queues. Current generated evidence: `program_integrate=0`, `missing_registry=0`, `missing_approved=0`, `missing_meta=0`, `add_capture=0`, `rerun_acceptance=0`, `generate_needed=0`.
- Added batchmode support for ArtAcceptance follow-through: `ArtAcceptanceRunner.CompleteRun()` exits the Editor in batchmode when `autoExitPlayMode=true`; `ArtAcceptanceEditorDaemon.RunFromBatchmode()` opens `Assets/Scenes/SampleScene.unity` before PlayMode.
- Ran Unity ArtAcceptance batchmode successfully. Latest evidence: `RunID=20260610_003347`, `Status=PASSED`, 21/21 captured, `RegistryEntryCount=278`, `MissingRequiredVisualIDs=0`, warnings/errors=0. Contact sheet: `UnityClient/Logs/ArtAcceptance/latest/contact_sheet_formalv2_20260610.png`.
- Updated `美术文档/09_运行时美术验收记录.md` and `美术文档/15_FormalV2运行时验收待办清单.md`. Art conclusion: resource/registry/tool gates pass, but FormalV2 runtime UI is not visually accepted. Most remaining issues are program/UI layout realization: `workshop_main` remains button-heavy, `prosthetic_panel` / `sell_panel` are empty black-panel states, and many V2-B/V2-C screens still use the old three-column template.

## 2026-06-11 FormalV2 Program Reflow Handoff

- Program/UI side repaired the post-tool-pass screenshot gaps and reran current-workspace ArtAcceptance. Latest evidence: `RunID=20260611_014057`, `Status=PASSED`, 21/21 captured, `RegistryEntryCount=278`, `MissingRequiredVisualIDs=0`, warnings/errors=0.
- Final evidence bundle for art review: `UnityClient/Logs/ArtAcceptance/latest/report.json`, `UnityClient/Logs/ArtAcceptance/latest/screenshots/`, and `UnityClient/Logs/ArtAcceptance/latest/contact_sheet_formalv2_20260611_014057.png`.
- Program-side screenshot self-review confirms `sell_panel` and `prosthetic_panel` now show real rows/icons/states rather than empty black frames, while `workshop_main`, `combat_hud`, `dungeon_map`, `inventory_loot`, and `settlement` have fresh screenshots after the FormalV2 reflow. Art-side manual screenshot acceptance is still required before marking FormalV2 UI visuals as passed.

## 2026-06-11 FormalV2 Program Reflow Art Acceptance

- Art-side reviewed `UnityClient/Logs/ArtAcceptance/latest/contact_sheet_formalv2_20260611_014057.png` and key screenshots. Resource / Registry / ArtAcceptance tool gates pass: 21/21 captured, Registry 278, `MissingRequiredVisualIDs=0`, warnings/errors=0.
- FormalV2 runtime visual acceptance does not pass. Main issues: `workshop_main` has non-cover background, overlapping backpack / status UI and clipped text; `inventory_loot` is a blue standalone panel instead of a translucent combat overlay; `settlement` title / summary / columns overlap; `dungeon_map` nodes float over the background and labels are unreadable; `sell_panel` / `prosthetic_panel` now contain rows but modal masking, frame alignment and text hierarchy are still insufficient; V2-B / V2-C screens still mostly read as old three-column templates.
- Current art-side decision: no bulk NovelAI pass and no resource registration task. Route the next step to program/UI layout polish, then rerun ArtAcceptance for another art manual review.

## 2026-06-12 Expression Preview Prototype

- 最近完成：新增一个不进入正式美术流水线的表现小样：Unity 运行时原型 `P3ExpressionPreviewController` 和本地浏览器预览 `tools/p3_expression_preview.html`，用于验证“伪 Live2D 魔偶呼吸 / SAN veil / 核心灯 + 维护火花 + 战斗命中特效”的风格方向。
- 当前关注：本轮没有新增 Approved 图片、Manifest 条目、VisualID 或资源登记队列；浏览器预览只服务方向评审，不能替代正式 UI / VFX / Live2D 接入规格。
- 下一步建议：若用户认可风格，先把表现拆成维护、战斗命中、护盾破裂和魔偶待机四个最小 VFX / 动画规格，再决定是否进入 `screen_layouts.json`、Manifest、Prefab 和 ArtAcceptance 验收。
- 问题 / 阻塞：Unity MCP 连接被工程授权撤销，本轮无法直接产出 Unity 编辑器内截图；已用本地浏览器预览截图保存到 `UnityClient/Logs/ExpressionPreview/p3_expression_preview_browser.png` 作为方向参考。
- 关键证据：Unity 程序和 Editor 工程编译均通过；预览页已在 `http://127.0.0.1:3000/p3_expression_preview.html` 打开并可循环播放。

## 2026-06-12 Expression Preview Approved Asset Pass

- Recently completed: refreshed the isolated expression preview to use existing Approved assets for the doll stand, workshop background, rust hound combat art, hit feedback and shield break feedback. The older geometric doll/enemy placeholders are now only fallback behavior.
- Current focus: this remains a direction preview only; no Approved PNG, Manifest row, VisualID, Registry entry or formal UI contract was added or modified in this pass.
- Next suggestion: if the direction is accepted, split it into the smallest formal deliverables: doll idle motion, repair pulse/sparks, combat hit feedback and shield break feedback, then define the real Prefab/VFXID handoff and ArtAcceptance evidence.
- Blockers: Unity MCP is still unavailable in this session, so the review evidence is the local browser screenshot at `UnityClient/Logs/ExpressionPreview/p3_expression_preview_formal_stage.png`.

## 2026-06-13 Live2D角色动画规格收束

- 最近完成：新增 `美术文档/16_Live2D角色动画资产接入规格.md`，把正式魔偶立绘动画路线收束为 Live2D Cubism 优先、Spine 备选、AI 仅用于补层 / 表情差分 / 短 cut-in 辅助，不继续推进 Unity 自研伪 Live2D 小 rig。
- 当前关注：该规格是未来动态立绘试点契约；本轮没有新增 Approved 图片、Manifest 条目、VisualID、Registry 登记、`screen_layouts.json` 变更或 FormalV2 ArtAcceptance 必过项。
- 下一步建议：若进入试点，先只做 `doll_proto_0_live2d`，要求有 `doll_proto_0_stand` fallback、分层源文件、Cubism motion / expression、Prefab 封装和独立 Live2D 多帧截图验收，再决定是否接入 `workshop_main` / `doll_room` / `combat_hud`。
- 问题 / 阻塞：当前尚未引入 Cubism / Spine 包，也未启动正式绑定资产制作；不得把现有表达预览或静态立绘替代为正式 Live2D 资产验收。

## 2026-06-13 Live2D试点P3 mission规划

- 最近完成：基于 `美术文档/16_Live2D角色动画资产接入规格.md` 新增本地 P3 mission `.mission/20260613_Live2D角色动画试点规划.csv`，拆成门禁确认、分层源与 fallback、补层 / 表情差分、Cubism 绑定、程序 runtime 评估、Prefab / Presenter、UGUI 桥接、独立验收和交接回写。
- 当前关注：该 mission 是本地恢复队列，不是新的美术事实来源；当前仍未新增 Approved、Manifest、VisualID、Registry、`screen_layouts.json` 或 FormalV2 ArtAcceptance 条件。
- 下一步建议：若继续执行，先跑 `L2D-GATE-01`，确认试点不影响 FormalV2 后再进入 `doll_proto_0` 分层源与 fallback 锁定。

## 2026-06-13 Live2D试点门禁确认

- 最近完成：`L2D-GATE-01` 门禁核对通过，可作为本地试点继续推进；依据为 `美术文档/16_Live2D角色动画资产接入规格.md` 已标记 active / source_of_truth，且明确 Live2D 试点独立于当前 FormalV2 静态资源验收。
- 当前关注：本轮仍不新增 Approved 图片、Manifest 条目、VisualID、Registry、`screen_layouts.json` 或 `_generated` 队列，也不把 Live2D 纳入 FormalV2 ArtAcceptance 必过项。
- 下一步建议：进入 `L2D-ART-01` 时先锁定 `doll_proto_0_stand` 静态 fallback、可绑定分层源缺口和 SourceRefs 记录；没有正式分层源前，不得把浏览器表现小样或静态立绘标记为正式 Live2D 资产。
- 问题 / 阻塞：当前工作区已有大量无关脏文件，Live2D 试点执行时必须严格限缩到 `_IncomingAI/DollsLive2D` 工作区、独立交接文档和状态页；FormalV2 美术验收流程继续按原链路推进。

## 2026-06-13 Live2D分层源与fallback锁定

- 最近完成：`L2D-ART-01` 已锁定 `doll_proto_0_stand` 作为首个 Live2D 试点静态 fallback，并在 `UnityClient/Assets/Art/_IncomingAI/DollsLive2D/doll_proto_0/notes.md` 与 `generation.json` 记录分层制作清单、遮挡补层清单、表情差分清单、动作目标和不入库边界。
- 当前关注：现有 `doll_proto_0_stand` 只有 flatten PNG、Approved sprite、`.meta` 和 Registry 条目，可用于降级显示；当前没有 PSD / Cubism / Spine / motion / expression / physics 文件，不能标记为可绑定源或正式 Live2D 资产。
- 下一步建议：`L2D-ART-02` 只允许围绕补层、表情差分和 contact sheet 做候选，不允许生成常驻待机帧动画；候选必须先人工清理成分层源，再进入 Cubism / Spine 绑定。
- 问题 / 阻塞：旧 `doll_proto_0_stand` 生成记录含黄铜 / 暖灯 / 蒸汽朋克倾向；后续 inpaint 和表情候选必须改用当前“日系二次元地底奇幻冒险 + 低信息密度”的风格基准。

## 2026-06-13 Live2D补层与表情候选请求包

- 最近完成：`L2D-ART-02` 已在 `UnityClient/Assets/Art/_IncomingAI/DollsLive2D/doll_proto_0/` 建立补层 / 表情候选请求包：`candidate_requests.md`、`generation.json`、`masks/README.md`、`inpaint_candidates/README.md` 和 `contact_sheet/README.md`。请求项覆盖 hair / face hidden fill、body overlap、hand / leg joints、core glow masks，以及 blink / low_san / hurt / relaxed 表情差分。
- 当前关注：本轮没有运行 NovelAI、Gemini inpaint 或其他 provider，真实候选图数量为 0；记录为 `validation_limited:provider_not_run`。这不是 Approved 素材、不是 Manifest 生成项，也不会刷新可接入素材清单。
- 下一步建议：如果后续选择 provider，应按请求项单图串行生成到 Live2D 专用子目录，人工筛选后再更新 contact sheet；仍不得生成常驻 24-60 帧待机序列。
- 问题 / 阻塞：缺少经人工确认的遮罩文件和可用 provider 输出，Cubism / Spine 绑定不能从这一行直接启动。

## 2026-06-13 Live2D Cubism绑定交接阻塞

- 最近完成：`L2D-ART-03` 已完成交接包核对，结论为阻塞而非完成：`UnityClient/Assets/Art/Approved/DollsLive2D/doll_proto_0/` 不存在，工程内没有 `doll_proto_0` 的 `.model3.json`、`.moc3`、motion、expression、physics 或 `DollLive2D_doll_proto_0.prefab`。
- 当前关注：不得创建空 Runtime / Prefabs 目录或用 Unity 伪 rig 冒充 Cubism 交接包；没有真实分层源和绑定输出前，`doll_proto_0_live2d` 不能进入 Approved、Manifest、Registry 或程序接入。
- 下一步建议：先完成真实分层源、遮罩、inpaint / 表情候选和人工清理，再由 Cubism / Spine 绑定工具产出 Runtime、motions、expressions、physics 和 Prefab。
- 问题 / 阻塞：`blocked:missing_layered_source_and_cubism_binding_output`。该阻塞不影响 FormalV2 静态 UI / ArtAcceptance，静态 fallback 仍使用 `doll_proto_0_stand`。

## 2026-06-13 Live2D绑定阻塞复核

- 最近完成：复核 `L2D-ART-03` 阻塞仍成立：`Approved/DollsLive2D` 不存在，`_IncomingAI/DollsLive2D/doll_proto_0/source` 与 `layered_psd` 为空，仓库内未找到 `.psd` / `.psb` / `.cmo3` / `.can3` / `.model3.json` / `.moc3` / `.motion3.json` / `.exp3.json` / `.physics3.json` / Spine `.skel` / `.atlas` 等源或运行时文件。
- 当前关注：本机未发现 Cubism Editor 或 Spine 可执行工具；当前只能保留补层 / 表情候选请求包和静态 fallback，不能推进正式绑定交接。
- 下一步建议：外部补齐经人工清理的分层源、遮罩、候选筛选结论和 Cubism / Spine 绑定输出后，再重新打开 `L2D-ART-03`。
- 问题 / 阻塞：仍为 `blocked:missing_layered_source_and_cubism_binding_output`；不得用空目录、静态图、浏览器预览或 Unity 伪 rig 关闭该阻塞。

## 2026-06-13 Agent原生动态立绘规格迭代

- 最近完成：新增 `美术文档/17_Agent原生动态立绘资产接入规格.md`，将首版动态魔偶立绘主线调整为 `Agent-native DollPuppet first`：分层 PNG、rig JSON、motion JSON、expression JSON、Unity importer / runtime、Prefab、fallback 和独立验收。
- 当前关注：`美术文档/16_Live2D角色动画资产接入规格.md` 已转为 Cubism / Spine 外部导出兼容路线，不再把 `.moc3` / `.skel` 作为首版阻塞项；Spine JSON 只作为授权与 runtime 兼容确认后的实验支线。
- 下一步建议：后续重新拆 DollPuppet 任务时，先做 `doll_proto_0` 正式立绘 / 分层源 / 表情差分，再产出 Approved DollPuppet 包与独立多帧验收，不把 AI 视频或静态 fallback 写成动态通过。
- 问题 / 阻塞：当前仍未制作 `doll_proto_0` DollPuppet Approved 包；本轮是规格迭代，不代表动态立绘资产已交付。

## 2026-06-13 Live2D试点交接收口

- 最近完成：本轮试点已把路线、限制和程序接入基础设施收口：`美术文档/16_Live2D角色动画资产接入规格.md` 定义 Cubism first / Spine fallback 和 AI 辅助边界；`_IncomingAI/DollsLive2D/doll_proto_0/` 记录 fallback、补层 / 表情候选请求包；程序侧完成 Prefab fallback、Presenter、UGUI bridge 和独立验收 runner。
- 当前关注：当前没有正式分层 PSD、Cubism / Spine Runtime、motion、expression、physics 或 `DollLive2D_doll_proto_0.prefab`；`doll_proto_0_stand` 只是静态 fallback，不得标记为正式 Live2D 资产。
- 下一步建议：美术侧后续先产出正式角色定稿或确认现有立绘可用，再做遮挡补层、表情差分、人工清理和 Cubism / Spine 绑定输出；绑定包完整后再交给程序跑独立 Live2D 多帧验收。
- 问题 / 阻塞：`blocked:missing_layered_source_and_cubism_binding_output` 仍成立；该阻塞不影响当前 FormalV2 静态资源验收链路。

## 2026-06-13 Live2D独立验收runner美术口径

- 最近完成：`L2D-VAL-01` 已新增独立 Live2D 验收 runner；预期输出为 `UnityClient/Logs/Live2DAcceptance/latest/report.json` 和 7 张截图：`idle_0s`、`idle_1s`、`expression_2s`、`low_san_idle_3s`、`repair_react`、`hit_react`、`fallback`。
- 当前关注：该 runner 不进入 FormalV2 ArtAcceptance 必过项，不修改 Approved、Manifest、Registry、`screen_layouts.json` 或 `_generated` 美术队列；当前没有真实 `doll_proto_0_live2d` 绑定包，合理结果应是 `FALLBACK_ONLY` / 受限验证，而非动态资产通过。
- 下一步建议：待 Cubism / Spine 绑定包和 Prefab 交付后，美术验收再看多帧截图中的身份稳定、表情差分、低 SAN 状态、受击 / 维护反馈和 fallback 切换。
- 问题 / 阻塞：不得把静态 fallback 截图、组件编译通过、浏览器预览或 Unity 自研伪 rig 当作正式 Live2D 动态立绘验收。

## 2026-06-13 Live2D UGUI桥接美术边界

- 最近完成：程序侧已补独立 `DollLive2DUGUIBridge`，后续可用 `RenderTexture + RawImage` 显示动态魔偶，缺动态源时显示静态 fallback。
- 当前关注：桥接尚未挂入 `workshop_main` / `doll_room` 正式界面，也没有改变 active `screen_layouts.json`、FormalV2 ArtAcceptance 或美术验收条件。
- 下一步建议：美术侧后续验收应看独立 Live2D 多帧截图和目标界面射线 / 遮挡证据，不能只凭组件可编译标记动态立绘画面通过。

## 2026-06-13 FormalV2运行时UI直管精修

- 最近完成：美术 / UI 侧直接处理 FormalV2 运行时 UI 剩余视觉阻塞：11 个旧 FormalV1 模板屏改为逐屏 FormalV2 运行时结构，`sell_panel` / `prosthetic_panel` 改为独立 modal，`dungeon_map` 节点 / 路线降低悬浮 UI 感；同时确认后续纯 UGUI 表现层 polish 由美术侧直接闭环。
- 当前关注：资源 / Registry / ArtAcceptance 工具门禁通过，本轮直接精修通过；最新证据为 ArtAcceptance `RunID=20260613_020257`、`PASSED`、21/21 captured、Registry 278、`MissingRequiredVisualIDs=0`、warnings/errors=0、`RealGameplay=21`、`FormalV1Template=0`。FormalV2 runtime UI visual seal 已通过当前纵切基线。
- 下一步建议：继续由美术 / UI 侧直接做质量迭代，优先把共享工坊子面板按界面簇区分为更明确的 FormalV2 专屏结构；资源登记、批量补图、modal 隔离、地图节点融合和旧 `formal_v1_template` 降级路径不再是当前阻塞。

## 2026-06-13 FormalV2 Town Market / Workshop Studio polish

- 最近完成：美术 / UI 侧继续直接处理纯 UGUI 运行时表现，聚焦 `sell_panel` 与 `prosthetic_panel`。`sell_panel` 已改成 `Town Market` 两栏结构：左侧货物列表，右侧 `Market Route` 引导；行内拆出图标、物品 / 来源、价格 pill 和 staging pill。`prosthetic_panel` 已改成 `Workshop Studio` 两栏结构：左侧义体配方，右侧 `Studio Bench` 人偶预览 / 状态说明；行内拆出义体图标、成本、状态图标和动作按钮。配方成本现在优先从 `ConfigManager.Items` 解析显示名，避免继续暴露 `loot_gear_scrap` 这类配置 ID。
- 当前关注：这轮不新增 Approved、Manifest、Registry 或 `screen_layouts.json`，只处理 Unity 运行时 UI 表现层；最新证据为 ArtAcceptance `RunID=20260613_024312`、`PASSED`、21/21 captured、Registry 278、`MissingRequiredVisualIDs=0`、warnings/errors=0、`RealGameplay=20`、`AcceptancePreview=1`、`FormalV1Template=0`。`sell_panel` / `prosthetic_panel` 通过当前纵切基线，后续仍可做更高品质插画化 / 动效化。
- 下一步建议：UI 直管流程继续有效。下一批优先按玩家流程看 `maintenance_panel`、`chassis_upgrade_panel`、`shop_staging` / `daily_bill_report` 等共享子面板是否需要从“可用结构”提升为更有场景语义的专屏结构；只有玩法服务、测试底座或 Unity 工程约束问题再交给程序侧。
- 验证证据：`dotnet build UnityClient/Assembly-CSharp.csproj --no-restore` 0 warning / 0 error；`Validate-UIDesign.ps1` 通过；`Validate-ArtGeneratedJson.ps1 -Strict` 通过；`Invoke-P0Validation.ps1 -SkipUnity -SkipArtAcceptance:$false -ArtAcceptanceTimeoutSeconds 240` 通过。限制：本轮 P0 wrapper 使用 `-SkipUnity`，因此 ConfigValidator 与 Unity smoke tests 未执行，ArtAcceptance 已重新运行并通过。

## 2026-06-13 AI图片网关NovelAI inpaint候选闭环

- 最近完成：`tools/ai-image-gateway` 已补 NovelAI inpaint 多候选能力、固定 seed、P3 Live2D 候选 runner 和 mock dry-run；runner 只写 `UnityClient/Assets/Art/_IncomingAI/DollsLive2D/<DollID>/inpaint_candidates/` 与对应 `generation.json` 证据字段，不同步 Approved、Manifest、Registry 或 FormalV2 队列。
- 当前关注：`doll_proto_0` 请求包 dry-run 可读到 `doll_proto_0_stand` fallback，但 8 个候选请求仍因缺 mask 或缺 `MaskTarget` 被阻塞；真实候选图数量仍为 0，不能进入 Cubism / Spine 绑定。
- 下一步建议：先人工补齐 `masks/hair_face_hidden_fill.png`、`body_overlap_fill.png`、`hand_leg_joint_fill.png`、`core_glow_masks.png`，并为表情差分请求补 MaskTarget 后，再用 NovelAI provider 串行生成候选和 contact sheet。
- 验证证据：`pytest tests -q` 于 `tools/ai-image-gateway` 通过，60 passed；P3 dry-run 报告 processed=8、blocked=8、failed=0，未写入候选图。

## 2026-06-13 DollPuppet试点P3 mission设计

- 最近完成：基于 `美术文档/17_Agent原生动态立绘资产接入规格.md` 新增本地 P3 mission `.mission/20260613_232104-基于-Agent-native-DollPuppet-规格推进-doll-proto-0-首版动.csv`，拆成门禁、正式立绘与 fallback、分层 / 表情候选、Approved DollPuppet 包、程序 schema / importer / runtime、UGUI 桥接、独立验收和交接复核。
- 当前关注：该 mission 只是本地执行队列，不是新的美术事实来源；当前仍未制作 `UnityClient/Assets/Art/Approved/DollPuppets/doll_proto_0/`，也没有 DollPuppet Prefab、Manifest / Registry 登记或 FormalV2 ArtAcceptance 条件变更。
- 下一步建议：若继续执行，先跑 `DP-GATE-01` 核对 `16` / `17` / 程序评估和状态页口径一致，再进入 `DP-ART-01` 正式立绘源与 fallback 锁定；不得复用旧 Cubism 阻塞项作为 DollPuppet 主线阻塞。
- 验证证据：`Test-P3Mission.ps1 -Strict` 通过，mission 当前 `tasks=0/10 done`，下一行是 `DP-GATE-01`。

## 2026-06-13 DollPuppet主路线门禁确认

- 最近完成：`DP-GATE-01` 已完成门禁核对：`美术文档/17_Agent原生动态立绘资产接入规格.md` 是首版动态魔偶立绘主线，`美术文档/16_Live2D角色动画资产接入规格.md` 只保留 Cubism / Spine 外部导出兼容边界，程序评估也明确首版优先 `Agent-native DollPuppet`。
- 当前关注：本行没有新增 Approved 图片、Manifest 条目、Registry、`screen_layouts.json`、DollPuppet Prefab 或 FormalV2 ArtAcceptance 必过项；旧 `L2D-ART-03` 的 Cubism 绑定阻塞不再阻断 DollPuppet 主线，但仍不代表 DollPuppet 资产已交付。
- 下一步建议：进入 `DP-ART-01` 时应在 `_IncomingAI/DollPuppets/doll_proto_0/` 建立正式立绘源 / fallback 记录，而不是继续写入旧 `_IncomingAI/DollsLive2D` 工作区。
- 验证证据：`Test-P3Mission.ps1 -Strict` 通过；门禁源文档、程序评估和 art/program 状态页口径一致。

## 2026-06-13 DollPuppet正式立绘与fallback源锁定

- 最近完成：`DP-ART-01` 已建立 `UnityClient/Assets/Art/_IncomingAI/DollPuppets/doll_proto_0/` 工作区，并将现有 `doll_proto_0_stand` 复制为 `source/base_flat.png`，同时新增 `notes.md`、`generation.json` 和 `contact_sheet/README.md` 记录 DynamicVisualID、ModelKind、fallback、风格修正、正式 repaint prompt 草案和 provider 限制。
- 当前关注：本行没有生成新 AI 图；当前 Codex 工具环境未暴露内置 `image_gen`，且未获授权切到 NovelAI / AI 图片网关 / CLI fallback / mock。本行证据等级为 `validation_limited:provider_unavailable`，不能标记为正式新立绘或可分层源已完成。
- 下一步建议：`DP-ART-02` 可以在该 DollPuppet 工作区继续补 masks、layer candidate requests、expression candidate requests 和 contact sheet 结构；真正产出候选图前仍需要可用 provider 或人工绘制 / 清理输入。
- 验证证据：`generation.json` 可被 `ConvertFrom-Json` 解析；`base_flat.png` 为 `1024x1536`；`Validate-Docs.ps1` 通过，且未修改 Approved、Manifest、Registry、`screen_layouts.json` 或 FormalV2 ArtAcceptance。

## 2026-06-13 DollPuppet分层与表情候选包阻塞

- 最近完成：`DP-ART-02` 已补齐 DollPuppet 候选请求包结构：`masks/README.md`、`layer_candidates/README.md`、`expression_candidates/README.md`、`contact_sheet/README.md` 和 `generation.json` 的 `candidatePackage`，覆盖 body / head / hair / arms / legs / core / accessory 以及 `blink`、`low_san`、`hurt`、`relaxed` 表情差分请求。
- 当前关注：该行按规格标记为阻塞而非完成：当前没有真实 mask PNG、没有 provider 输出、没有 layer candidate 图、没有 expression candidate 图，也没有 contact sheet 缩略图；不能进入 Approved DollPuppet 包或 schema / rig 资产制作。
- 下一步建议：先补 `masks/hair_face_hidden_fill.png`、`body_overlap_fill.png`、`hand_leg_joint_fill.png`、`core_glow_masks.png`，并在可用 `image_gen` 或用户明确批准的 provider 下串行生成候选，再更新 contact sheet 和人工筛选结论。
- 问题 / 阻塞：`blocked:missing_provider_and_masks`。这不影响 FormalV2 静态 UI / ArtAcceptance，也不要求回到 Cubism / Spine 路线。

## 2026-06-14 DollPuppet Approved包组装阻塞

- 最近完成：`DP-ART-03` 已完成入库前核对，结论为阻塞而非完成：`UnityClient/Assets/Art/Approved/DollPuppets/doll_proto_0/` 不存在，`_IncomingAI/DollPuppets/doll_proto_0/` 目前只有静态 `source/base_flat.png`、请求包 README 和 `generation.json`，没有真实 mask PNG、分层候选、表情候选、人工清理后的层图、rig / mesh / motion / expression JSON。
- 当前关注：不得创建空 Approved 包、不得把 raw request / README 当成正式层图，也不得刷新 Manifest、Registry、`screen_layouts.json` 或 FormalV2 ArtAcceptance 条件来“通过”动态立绘。
- 下一步建议：先补真实 masks 和 provider / 人工绘制输出，完成分层候选筛选与边缘清理后，再重新组装 `SourceRefs`、`Textures/layers`、`Rig`、`Motions` 和 `Expressions`。
- 问题 / 阻塞：`blocked:missing_cleaned_layered_source_and_candidate_outputs`。静态 fallback 仍使用 `doll_proto_0_stand`，当前阻塞不影响 FormalV2 静态 UI / ArtAcceptance。

## 2026-06-14 DollPuppet UGUI桥接美术边界

- 最近完成：同步 `DP-UI-01` 程序侧核对结论：现有 UGUI 桥接底座可继续作为 `DollDynamic` / DollPuppet 显示基础，但当前没有动态 Prefab、正式界面接入截图或 Unity 射线验证。
- 当前关注：美术验收仍不能把静态 fallback、独立桥接组件、编译通过或旧 Live2D 命名兼容写成 `doll_proto_0_live2d` 动态画面已通过；FormalV2 静态 UI / ArtAcceptance 条件不因该试点扩大。
- 下一步建议：先补齐 Approved DollPuppet 包和程序生成 Prefab，再看 `workshop_main` / `doll_room` 多帧动态截图、fallback 切换和 UI 遮挡 / 射线证据。
- 问题 / 阻塞：`blocked:missing_dynamic_prefab_and_unity_ui_evidence`。当前动态立绘美术验收仍停在制作输入和运行态证据缺失。

## 2026-06-14 AI图片网关NovelAI真实inpaint业务测试

- 最近完成：已补 `UnityClient/Assets/Art/_IncomingAI/DollsLive2D/doll_proto_0/masks/hair_face_hidden_fill.png`，并用 AI 图片网关 NovelAI provider 对 `l2d_fill_hair_face_01` 跑真实 inpaint 出图 1 张：`UnityClient/Assets/Art/_IncomingAI/DollsLive2D/doll_proto_0/inpaint_candidates/hair_face/l2d_fill_hair_face_01_00_seed20260614.png`。`generation.json` 已记录 `GeneratedCount=1`、provider=`novelai`、model=`nai-diffusion-4-5-full-inpainting`。
- 当前关注：该输出是真实 provider 业务链路证据，但画面 mask 区域出现大块灰色填充，不能作为可清理分层源、不能进入 Approved、Manifest、Registry、FormalV2 队列或 Cubism / Spine / DollPuppet 绑定。
- 下一步建议：后续先缩小 / 重画 face-only mask，并在限流窗口外继续串行试跑；同时补齐 `body_overlap_fill.png`、`hand_leg_joint_fill.png`、`core_glow_masks.png` 与表情差分 `MaskTarget` 后再生成 contact sheet 和人工筛选结论。
- 问题 / 阻塞：真实出图链路已验证，但候选质量未通过；第二次 `--no-add-original-image` 参数试跑被 NovelAI 429 限流，没有第二张输出。动态立绘资产仍是 `blocked:missing_cleaned_layered_source_and_candidate_outputs`。
- 验证证据：`tools/ai-image-gateway` 执行 `python -m pytest tests -q` 通过，65 passed；`git diff --check` 仅输出既有 CRLF 提示，无 whitespace error；`generation.json` 可解析，mask 为 `1024x1536`，真实输出为 `832x1216`。

## 2026-06-14 NovelAI 4.5 inpaint gateway fix

- Recently completed: fixed `tools/ai-image-gateway` NovelAI inpaint payload sizing for 4.5. The provider now applies the free-tier size limit before encoding `parameters.image` and `parameters.mask`, flattens RGBA source images to RGB for inpaint upload, avoids double `-inpainting` model suffixes, and passes V4/V4.5 `strength`.
- Current focus: a real NovelAI 4.5 run for `l2d_fill_hair_face_01` produced `UnityClient/Assets/Art/_IncomingAI/DollsLive2D/doll_proto_0/inpaint_candidates/hair_face/l2d_fill_hair_face_01_00_seed20260631.png`. The previous broad gray block regression is fixed, but the result is still `reviewed_not_approved` because the face area becomes a large pale cyan visor-like patch.
- Next suggestion: keep using narrower masks or a cleaner formal source repaint before assembling Live2D / DollPuppet layers; do not sync either raw output to Approved, Manifest, Registry, FormalV2 queues, Cubism, Spine or DollPuppet packages.
- Evidence: `tools/ai-image-gateway` `python -m pytest tests -q` passed with 68 tests; real NovelAI output was generated with model `nai-diffusion-4-5-full-inpainting`; comparison image is `UnityClient/Assets/Art/_IncomingAI/DollsLive2D/doll_proto_0/inpaint_candidates/hair_face/compare_old_fixed_seed20260631.png`. Face-crop grayish ratio dropped from about `0.474` to `0.047`.

## 2026-06-14 NovelAI 4.5 ANR-style inpaint reconnect

- Recently completed: reconnected `tools/ai-image-gateway` NovelAI 4.5 inpaint to the `F:\my_project\Auto-NovelAI-Refactor` request shape: img2img base parameters plus `action=infill`, full-size binary mask PNG, `inpaintImg2ImgStrength`, `strength`, `noise`, `extra_noise_seed`, `color_correct=false`, and default `add_original_image=false`; the Live2D runner now keeps manual-cleanup outputs in `generated_with_review_required` instead of plain `generated`.
- Current focus: real outputs `UnityClient/Assets/Art/_IncomingAI/DollsLive2D/doll_proto_0/inpaint_candidates/hair_face/l2d_fill_hair_face_01_00_seed20260632.png` and `UnityClient/Assets/Art/_IncomingAI/DollsLive2D/doll_proto_0/inpaint_candidates/hair_face/l2d_fill_hair_face_01_00_seed20260633.png` now behave as local inpaint rather than full mask repaint. They still remain raw candidates / `reviewed_not_approved`; do not sync to Approved, Manifest, Registry, FormalV2 queues, Cubism, Spine, or DollPuppet packages.
- Next suggestion: continue art-side screening with tighter masks or a cleaner formal source repaint before layer extraction; keep raw NovelAI candidates in `_IncomingAI` only.
- Evidence: comparison images `UnityClient/Assets/Art/_IncomingAI/DollsLive2D/doll_proto_0/inpaint_candidates/hair_face/compare_old_wrong_anr_seed20260632.png` and `UnityClient/Assets/Art/_IncomingAI/DollsLive2D/doll_proto_0/inpaint_candidates/hair_face/compare_anr_seed20260633.png`; `generation.json` records seed `20260633`, provider `novelai`, model `nai-diffusion-4-5-full-inpainting`, `AddOriginalImage=false`, provider mask white ratio `0.042839`, and review-required status.

## 2026-06-14 DollPuppet独立验收runner美术边界

- 最近完成：同步 `DP-VAL-01` 核对结论：已写入 `UnityClient/Logs/.doll_puppet_acceptance_trigger = RUN_DOLL_PUPPET_ACCEPTANCE`，但当前没有 `DollPuppetAcceptance/latest/report.json`、没有 7 张截图，也没有动态 Prefab。
- 当前关注：美术侧验收必须等待真实 `DollPuppetAcceptance` 报告；不能把触发文件、旧 Live2D runner、fallback-only 预期或编译通过写成 DollPuppet 多帧动态验收通过。
- 下一步建议：动态 Prefab 可用后，验收截图至少覆盖 `idle_0s`、`idle_1s`、`expression_2s`、`low_san_idle_3s`、`repair_react`、`hit_react` 和 `fallback`，并明确区分 `PASSED` / `FALLBACK_ONLY` / `FAILED`。
- 问题 / 阻塞：`blocked:missing_dollpuppet_acceptance_runner_output_and_unity_runtime`。该独立验收不进入 FormalV2 21 屏 ArtAcceptance 必过条件。

## 2026-06-14 DollPuppet试点交接收口

- 最近完成：本轮已把 DollPuppet 首版试点的 VisualID、路径、fallback、程序合同、验收限制和 FormalV2 不受影响边界收口到 `美术文档/17_Agent原生动态立绘资产接入规格.md`、`开发文档/17_Live2DSpine运行时接入评估.md`、`agent_status/program.md` 和本状态页。
- 当前关注：美术侧真正缺口仍是正式 masks、分层候选、表情候选、人工清理后的层图、SourceRefs 和 Approved DollPuppet 包；当前 `_IncomingAI/DollPuppets/doll_proto_0/` 的请求包与静态 fallback 不等于动态资产。
- 下一步建议：先完成 DollPuppet 专用工作区的真实候选与人工筛选，再进入 Approved 包组装；程序侧随后才能生成 Prefab、接 UI 和跑 `DollPuppetAcceptance`。
- 验证证据：`Validate-Docs.ps1` 通过；mission strict 通过，当前为 `tasks=5/10 done, blocked=5`。FormalV2 静态 UI / ArtAcceptance 不因本试点改变。

## 2026-06-14 DollPuppet NovelAI请求入口准备

- 最近完成：用户已明确批准使用 NovelAI / inpaint 补 DollPuppet 素材；本轮在 `UnityClient/Assets/Art/_IncomingAI/DollPuppets/doll_proto_0/` 下补齐 4 张专用 mask：`hair_face_hidden_fill.png`、`body_overlap_fill.png`、`hand_leg_joint_fill.png`、`core_glow_masks.png`，并新增 `source/inpaint_source_opaque.png` 作为 NovelAI inpaint 输入，避免继续复用旧 Live2D 工作区作为主线。
- 当前关注：`generation.json` 已从旧 `provider_unavailable` 更新为本轮 `approved_for_this_followup`，并补齐 7 个 DollPuppet `CandidateRequests`，覆盖 body/head/hair、body overlap、arms/legs/joints、core glow、blink、low_san、relaxed。网关 dry-run 结果为 `processed=7`、`blocked=0`、`failed=0`、`outputs=7 planned`。
- 下一步建议：继续执行 `DP-ART-05`，用 NovelAI 串行生成真实候选；如遇 429 或灰块 / 身份漂移，应保留失败证据并进入参数 / mask 调整，而不是把 raw 输出写入 Approved。
- 问题 / 阻塞：当前只完成 mask 与请求入口，真实候选图数量仍为 0；`Approved/DollPuppets/doll_proto_0/`、Prefab、DollPuppetAcceptance 和 FormalV2 条件仍未改变。
- 验证证据：`generation.json` 可解析，`CandidateRequests=7`、`MaskPackage.masks=4`；`python tools/ai-image-gateway/examples/p3_live2d_inpaint.py --generation-json UnityClient/Assets/Art/_IncomingAI/DollPuppets/doll_proto_0/generation.json --provider novelai --source-image UnityClient/Assets/Art/_IncomingAI/DollPuppets/doll_proto_0/source/inpaint_source_opaque.png --dry-run` 返回 planned 且无 blocked / failed。

## 2026-06-14 DollPuppet NovelAI真实候选与质检

- 最近完成：`DP-ART-05` 已对 DollPuppet 专用工作区运行 NovelAI inpaint。第一轮 broad mask 生成 `6/7`，`dp_layer_hair_face_01` 因 429 限流失败；第二轮 tight mask 生成 `7/7`。当前共有 13 张真实 NovelAI 候选，路径位于 `layer_candidates/` 与 `expression_candidates/`，并生成 contact sheet：`contact_sheet/dollpuppet_novelai_candidates_20260614_round1_round2.png`。
- 当前关注：`DP-ART-06` 质检结论为 `reviewed_not_approved`：`dp_layer_core_glow_02` 和 `dp_expr_relaxed_02` 可作为参考，`dp_expr_low_san_02` 与 `dp_expr_relaxed_01` 仅部分可参考；身体补层、手脚关节、眨眼、头脸补层候选存在灰块、身份漂移、发光误生成或局部破坏，不能进入 Approved。
- 下一步建议：不要从这批 raw inpaint 图直接组装 `Approved/DollPuppets/doll_proto_0/`。下一步应补一个正式源图重绘 / img2img 候选批次，先得到更干净的全身中性源图，再重新做分层补区和人工清理。
- 问题 / 阻塞：`Approved/DollPuppets/doll_proto_0/`、清理后的 layer PNG、rig / motion / expression JSON、Prefab 和 `DollPuppetAcceptance` 仍未解锁；当前只是“NovelAI 真实候选已生成并质检完成”，不是动态立绘资产完成。
- 验证证据：`generation.json` 可被 `ConvertFrom-Json` 解析；`GeneratedAssets=13`、`CandidateRequests=14`、`ApprovedReadyCount=0`、`ReferenceOnlyCount=4`、`RejectedCount=9`；contact sheet README 已记录候选决策。
## 2026-06-14 DollPuppet NovelAI 4.5 local inpaint rerun

- Recently completed: ran real NovelAI 4.5 inpaint through `tools/ai-image-gateway` for DollPuppet expression deltas in `UnityClient/Assets/Art/_IncomingAI/DollPuppets/doll_proto_0/`. `_05` broad/feature masks generated real outputs but still pasted visible face-material patches after local composite; `_06` line-mask rerun generated `dp_expr_blink_06`, `dp_expr_low_san_06`, `dp_expr_relieved_06`, and `dp_expr_angry_06`.
- Current focus: `_06` is the current business evidence for local inpaint, not an Approved-ready art package. Pixel audit passed for all four `_06` outputs (`outside_changed=False`), but visual review still shows unnatural eyelid/brow/eye-line fragments, especially low_san and angry.
- Next suggestion: keep `_06` as cleanup/reference material only. Do not sync raw candidates to Approved, Manifest, Registry, FormalV2 queues, or a final DollPuppet package; next art pass should hand-clean the line deltas or use a cleaner source face and tighter masks.
- Evidence: `generation.json` now records `LatestBusinessAcceptance.status=generated_with_review_required`, `approvedReadyCount=0`; contact sheets are `contact_sheet/dollpuppet_expression_line_mask_06_20260614.png` and `contact_sheet/dollpuppet_expression_line_mask_06_head_20260614.png`. `dp_expr_relieved_06` hit NovelAI rate limit on the first pass and succeeded after one bounded retry; token source remains local `F:\my_project\new\tags_machine\novelai\client.py` with no token persisted.

## 2026-06-21 T0-01A 首次下潜许可 UI / 美术承接回退

- 最近完成：已新增 `T0-01A_开局人偶状态到首次下潜许可开发方案.md`，其中美术 / UI 侧明确首版用 UGUI 分层和轻 CG 合成黑屏、工坊全景、债务纸、手记、工具匣、发现零号、核心启动、状态小卡、半开放工坊和第一层确认；新增 VisualID 不是硬前置。当前未生成新图、未改 Approved、Manifest、Registry 或 `screen_layouts.json`。
- 当前关注：后续进入落地时，先按开发方案复用 `workshop_main`、`layer_select`、`doll_proto_0_stand`、现有按钮和信息面板皮肤；缺图只记录为后续补强。
- 下一步建议：开发落地后补关键截图：过程 CG 合成态、`启动人偶` 单按钮态、状态小卡、半开放工坊、第一层确认允许态 / 阻断态。

## 2026-07-01 T0-FLOW-01 开场前半段程序接入

- 最近完成：程序侧已新增 `PrologueOpeningNarrativeFlow` 与 `PrologueFirstDiveController`，按 Narrative 节点播放黑屏、债务纸、修复手记、核心碎片和发现零号；当前只消费 VisualID / fallback，不新增或替换正式素材。
- 当前关注：本轮证据是 Unity batchmode smoke，不是截图验收。黑屏、工坊特写和发现零号画面仍需要后续 ArtAcceptance / 人工截图复核。
- 下一步建议：后续 `T0-FLOW` 继续推进到 `启动人偶` 单按钮与状态小卡时，一并补过程 CG 合成态、发现零号、单按钮态和状态小卡截图。
- 验证证据：`UnityClient/Logs/prologue_opening_flow_editor_runner_codex_20260701.log` 显示 `Prologue Opening Narrative Flow Smoke PASSED`；受限项为 `validation_limited:screenshots_not_captured_for_T0_FLOW_01`。

## 2026-06-14 DollPuppet inpaint art-direction correction

- Feedback recorded: the current expression and action diff outputs should be treated as basically unusable for production DollPuppet diff work. They prove the NovelAI 4.5 gateway path and mask containment, but they do not satisfy the art goal.
- Current focus: do not continue hard-running batches on the same source/mask setup. The failure mode is visual/art-direction quality: face material patches, eye/glow artifacts, black-frame motion output, and expression deltas that are either too subtle or require manual repaint.
- Next suggestion: stop before further batch generation unless the source repaint, mask strategy, or manual cleanup plan changes. Current outputs remain failure/reference evidence only.

## 2026-06-21 AI图片网关OpenAI兼容中转接入

- 最近完成：`tools/ai-image-gateway` 已新增并补强 OpenAI-compatible 接入：`openai_images` 走 `/v1/images/generations`；`openai_chat_image` / `gemini_chat_image` / `grok_chat_image` 走 `/v1/chat/completions`。配置样例和 README 已补 `base_url`、`api_key`、`model`、`response_format`、`edit_endpoint`、`quality`、`output_format`、`temperature`、`timeout` 和 `retry`，并记录不跨 endpoint fallback 的规则。初版曾将 `/v1/images/edits` 误写为 masked edit / inpaint，已在 2026-06-22 修正为独立 `image_to_image` 语义。
- 当前关注：本轮只新增网关 provider、路由注册、配置样例、README 和单元测试；没有真实出图，没有写入 `_IncomingAI`、Approved、Manifest、Registry、`screen_layouts.json` 或 FormalV2 ArtAcceptance 队列。
- 下一步建议：接入真实中转站前先配置 `AI_IMAGE_PROXY_KEY` 与目标 `base_url/model`，分别用 `openai_images` 生图、`openai_images` 图生图 / reference edit、`gemini_chat_image` 生图、`grok_chat_image` 生图做单张 smoke test 确认返回格式；通过后再接入美术候选 runner。
- 问题 / 阻塞：当前尚未获得中转站真实请求 / 响应样例和密钥，不能声明 GPT / Gemini / Grok 真实 provider 出图链路已通过；只完成标准 OpenAI-compatible 适配层与 mock HTTP 验证。
- 验证证据：`tools/ai-image-gateway` 执行 `python -m pytest tests/test_openai_compatible_provider.py -q` 通过，7 passed；执行 `python -m pytest tests -q` 通过，82 passed。

## 2026-06-22 AI图片网关图生图语义拆分

- 最近完成：`tools/ai-image-gateway` 已新增 `Capability.IMAGE_TO_IMAGE`、`ImageToImageRequest`、`ImageService.image_to_image()` 和 `batch_image_to_image()`；`openai_images` 将 `/v1/images/edits` 映射为参考图编辑 / 图生图，不再声明 `Capability.INPAINT`；chat 兼容 provider 支持 text + `image_url` content array 的参考图请求。
- 当前关注：`InpaintRequest` / `Capability.INPAINT` 继续只代表带 mask 的局部重绘，当前由 NovelAI / Live2D inpaint 工作流使用；本轮没有真实出图，没有写入 `_IncomingAI`、Approved、Manifest、Registry、`screen_layouts.json` 或 FormalV2 ArtAcceptance 队列。
- 下一步建议：真实中转站 smoke test 按 `generate`、`image_to_image`、chat `generate`、chat `image_to_image` 分开验证；只有显式 mask 局部重绘流程才走 `inpaint`。
- 问题 / 阻塞：仍缺少中转站真实密钥和响应样例，不能声明 GPT / Gemini / Grok 真实出图链路已通过。
- 验证证据：`tools/ai-image-gateway` 执行 `python -m pytest tests/test_openai_compatible_provider.py tests/test_schema.py tests/test_mock_provider.py tests/test_config.py -q` 通过，38 passed；执行 `python -m pytest tests -q` 通过，90 passed。

## 2026-06-22 AI图片网关OpenAI兼容稳健性增强

- 最近完成：继续参考 `F:\my_project\image-gen`、`F:\my_project\infinite-canvas` 和 `F:\my_project\gpt-image-linux`，为 `tools/ai-image-gateway` 增强 OpenAI-compatible 适配稳健性：chat 响应现在可解析嵌套 JSON、SSE `data:` 事件、Markdown 图片、data URL 和裸 HTTP(S) 图片 URL；新增 `image_inputs` 工具用于把 bytes、本地路径、HTTP(S) URL 和 `data:image/...` 归一化为参考图 bytes 与 MIME 元数据。
- 当前关注：本轮只提升网关解析和 runner 输入前处理能力，没有真实出图，没有写入 `_IncomingAI`、Approved、Manifest、Registry、`screen_layouts.json` 或 FormalV2 ArtAcceptance 队列。
- 下一步建议：接入真实中转站时优先收集每个渠道的原始响应样例；若返回格式仍不匹配，再补解析 fixture，而不是改 provider endpoint 语义。
- 问题 / 阻塞：仍缺少真实中转站密钥和响应样例，不能声明 GPT / Gemini / Grok 真实出图链路已通过。
- 验证证据：`tools/ai-image-gateway` 执行 `python -m pytest tests/test_openai_compatible_provider.py tests/test_image_inputs.py -q` 通过，19 passed；执行 `python -m pytest tests -q` 通过，99 passed。

## 2026-06-22 AI图片网关中转站真实smoke

- 最近完成：使用用户提供的中转站凭据做临时内存 smoke，未写入仓库配置。`/v1/models` 返回 3 个模型：`gpt-image-2`、`gemini-3.1-flash-image`、`grok-imagine-image-lite`。文生图通路验证通过：`gpt-image-2` 走 `/v1/images/generations` 返回 1 张图；`gemini-3.1-flash-image` 走 `/v1/chat/completions` 返回 1 张图；`grok-imagine-image-lite` 走 `/v1/chat/completions` 返回 1 张图。
- 当前关注：本轮没有保存 smoke 输出图片，没有写入 `_IncomingAI`、Approved、Manifest、Registry、`screen_layouts.json` 或 FormalV2 ArtAcceptance 队列。为适配该中转站，chat provider 已改为默认不发送 `n`，也不转发 Images API 字符串 `response_format=b64_json`；否则 Grok / chat relay 会返回服务端错误。
- 下一步建议：当前可用路由应配置为：`generate: openai_images` 用 `gpt-image-2`，Gemini / Grok 单独用 `gemini_chat_image`、`grok_chat_image`。图生图当前只把 `gemini_chat_image` 标为已验证可用；`openai_images` 的 `/v1/images/edits` 在标准 multipart `image` / `image[]` 下均返回上游文件名过长错误，`image_data_url[]` 返回缺 `image`；`grok_chat_image` 参考图请求返回 SSE server_error。
- 问题 / 阻塞：该中转站 GPT 图生图 `/v1/images/edits` 和 Grok 参考图模式暂不可标记为可用；若要启用，需要中转站提供实际支持的 edits / 参考图请求格式或修复上游兼容。
- 验证证据：`tools/ai-image-gateway` 执行 `python -m pytest tests/test_openai_compatible_provider.py tests/test_image_inputs.py -q` 通过，22 passed；执行 `python -m pytest tests -q` 通过，102 passed。
## 2026-06-25 OpenAI-compatible relay disk smoke rerun

- Recently completed: reran the OpenAI-compatible relay routes with disk output enabled. Added a local smoke runner at `tools/ai-image-gateway/examples/smoke_openai_relay_to_disk.py` and fixed `openai_images` provider error extraction so provider JSON error payloads no longer become `AttributeError`.
- Current focus: all six relay routes were actually requested and wrote evidence under `UnityClient/Assets/Art/_IncomingAI/OpenAICompatibleRelaySmoke/20260625_232449/`: `gpt_images_generate`, `gemini_chat_generate`, `grok_chat_generate`, `gpt_images_image_to_image`, `gemini_chat_image_to_image`, and `grok_chat_image_to_image`. No provider-generated image was returned this run. Only `reference_fallback.png` was locally created so image-to-image routes could still be exercised.
- Blocking / issue: `/v1/models` currently returned only `gpt-image-2`; Gemini and Grok routes returned HTTP 503 "no available channel / distributor". GPT Images minimal requests also returned `openai_error / bad_response_status_code` after about 300 seconds, including minimal payloads without `response_format`, with `response_format=b64_json`, and with `n=1`.
- Evidence: manifest and raw response evidence are in `UnityClient/Assets/Art/_IncomingAI/OpenAICompatibleRelaySmoke/20260625_232449/manifest.json` and `raw_minimal/*.json`; targeted gateway tests passed with `python -m pytest tests/test_openai_compatible_provider.py tests/test_image_inputs.py -q` -> `23 passed`.

## 2026-06-26 AI image gateway local credential config

- Recently completed: added local-only `tools/ai-image-gateway/config.local.yaml` for the relay key and NovelAI provider settings, and ignored it via the submodule `.gitignore`; the config uses `openai_images` / `gemini_chat_image` / `grok_chat_image` for relay routes and `novelai` for true masked inpaint.
- Current focus: `examples/smoke_openai_relay_to_disk.py` now supports `--config config.local.yaml` and `--out-dir`, so smoke tests no longer depend on manually setting environment variables in the active shell.
- Evidence: `load_config('config.local.yaml')` resolves all enabled providers and `resolve_novelai_access_token(client_py_path=...)` returns a token; `python -m pytest tests/test_openai_compatible_provider.py tests/test_image_inputs.py -q` passed with `23 passed`; `config.local.yaml` is ignored and must not be committed.

## 2026-06-26 GPT image2 relay compare smoke

- Recently completed: tested `gpt-image-2` only against both relay bases. `https://jiuuij.de5.net/v1` is currently usable for `/v1/images/generations` and returned one real image saved at `UnityClient/Assets/Art/_IncomingAI/OpenAICompatibleRelaySmoke/gpt_image2_compare_20260626_223403/jiuuij_de5/gpt_image2_generation_00.png`.
- Current focus: `https://api.7r.fit/v1` can list models and includes `gpt-image-2`, `gpt-image-1`, and `gpt-image-1.5`, but `/v1/images/generations` returned HTTP 403 `Image generation is not enabled for this group`; do not route generation to this provider until the account/group permission changes.
- Evidence: comparison manifest and raw responses are in `UnityClient/Assets/Art/_IncomingAI/OpenAICompatibleRelaySmoke/gpt_image2_compare_20260626_223403/manifest.json`; the saved `jiuuij_de5` output was visually inspected as a valid blue crystal compass icon.

## 2026-06-26 Folder batch image-to-image template

- Recently completed: added `tools/ai-image-gateway/examples/batch_image_to_image_folder.py` and `tools/ai-image-gateway/docs/batch_image_to_image_folder_template.md` so a shared prompt can be applied to every image in a folder through `ImageService.image_to_image()`.
- Current focus: default usage targets `openai_images` / `gpt-image-2` because the old relay now passes `/v1/images/edits`; `examples/run_batch_i2i_folder.py` provides an edit-at-top Python runner for artists who prefer changing script variables instead of command-line arguments.
- Evidence: `python -m py_compile examples/batch_image_to_image_folder.py` passed; `python -m py_compile examples/run_batch_i2i_folder.py` passed; dry-run wrote `UnityClient/Assets/Art/_IncomingAI/OpenAICompatibleRelaySmoke/batch_i2i_template_dryrun/manifest.json`; targeted provider tests passed with `23 passed`.

## 2026-07-01 Batch text-to-image template

- Recently completed: added `tools/ai-image-gateway/examples/run_batch_generate.py` and `tools/ai-image-gateway/docs/batch_generate_template.md` for edit-at-top text-to-image batches where the user changes `PROMPT`, `COUNT`, `WIDTH`, `HEIGHT`, `PROVIDER`, and `OUTPUT_ROOT` directly in Python.
- Current focus: default usage targets `openai_images` / `gpt-image-2`; each run writes generated images, per-image metadata JSON, and a root `manifest.json` under `UnityClient/Assets/Art/_IncomingAI/TextToImageRuns/batch_generate_<timestamp>/`.
- Evidence: `python -m py_compile examples/run_batch_generate.py` passed; targeted provider tests passed with `python -m pytest tests/test_openai_compatible_provider.py tests/test_image_inputs.py -q` -> `23 passed`.

## 2026-07-05 零号人设 Danbooru 参考榜单工具与报告

- 最近完成：新增 `tools/美术工具/danbooru_character_reference.py` 与 `tools/美术工具/Generate-DanbooruCharacterReference.ps1`，参考 `F:\ThreeState\scrapu_db.py` 的 Danbooru tag / posts 拉取思路，改为只抓公开元数据、不下载图片的角色参考研究流水线；已生成 `美术文档/_generated/danbooru_character_reference/zero_doll_reference_report.md` 与 raw JSON。
- 当前关注：报告覆盖 `doll_joints`、`android`、`robot_girl`、`joints`、`mechanical_halo`、`mechanical_arms` 等零号重构关键词，包含关键词体量、总角色投稿榜、主题搜索组、角色 / 作品共现和 prompt 慎用项；所有默认主题搜索组均带 `1girl rating:g`，并已对可参考角色候选榜做二次过滤：已知男性 / 非少女对象排除，服装、形态和机器人 / 人类形态变体归并到 canonical 角色。
- 下一步建议：基于该报告再整理正式 `零号完整人设交付包 V1`，把参考榜单转成母图 prompt、三视图要求、表情差分、DollPuppet 分层清单和禁止照搬规则。
- 问题 / 阻塞：本轮未下载 Danbooru 图片，未新增 Approved 素材、Manifest、Registry、`screen_layouts.json`、DollPuppet 包或 ArtAcceptance 条件；报告是参考资料，不是素材交付或商业画面验收。
- 关键证据：脚本运行输出 `tag_counts=12`、`top_character_tags=40`、`search_groups=6`、`aggregate_characters=201`；报告与 raw JSON 已包含中文角色名 / 中文作品名字段，候选榜含 `合并来源` 列，生成物位于 `美术文档/_generated/danbooru_character_reference/`。

## 2026-07-05 人设 Owner 文档入口与参考获取规则

- 最近完成：新增 `美术文档/人设/README.md` 与 `美术文档/人设/01_人设参考获取规则.md`，把零号参考人设获取规则沉淀为人设 Owner 事实文档，并接入 `美术文档/README.md`、`04_美术风格基准.md`、`17_Agent原生动态立绘资产接入规格.md` 和 `tools/美术工具/README.md`。
- 当前关注：该规则只定义“怎么找参考、怎么过滤、怎么解释报告”，不定义零号最终造型；默认要求 `1girl rating:g`，去除 `blue_eyes`、`white_hair`、`white_dress` 等强外观预设，候选榜必须做男性 / 非目标对象过滤、同角色变体归并和中文角色名 / 作品名字段补全。
- 下一步建议：在该规则基础上整理 `零号完整人设交付包 V1`，明确母图方向稿、三视图、表情差分、DollPuppet 分层清单、可转译参考和禁止照搬项。
- 问题 / 阻塞：当前仍是规则文档与参考报告阶段，未产生人设定稿、Approved 素材、Manifest、Registry、DollPuppet 包或 ArtAcceptance 证据。

## 2026-07-05 零号指定原型参考：失明少女

- 最近完成：按用户指定，新增 `美术文档/人设/02_零号原型参考_失明少女.md`，整理《漆黑的子弹》失明少女的中文 / 英文资料、外部图片链接、外观与性格符号、转译到零号的方向和禁止照搬项。
- 当前关注：根据用户修正，该角色仅作为零号人物外貌原型，不继承性格；保留遮眼、披肩、轻薄脆弱轮廓等外观参考，外部图片只保存链接与描述，未下载、未复制、未进入 Approved、Manifest、Registry、`_IncomingAI` 或 DollPuppet 包。
- 下一步建议：基于该原型文档进入 `零号完整人设交付包 V1`，先出 2-3 个方向稿，测试银发 / 非银发、遮眼方式、维修标签、核心仓和临时罩布的原创组合。
- 问题 / 阻塞：当前仍是外部参考收集与转译规则阶段，不能声明零号人设定稿或商业素材可用；后续 prompt 必须显式禁止 `Black Bullet`、`Blind Girl`、`pink cape`、`begging sign` 等照搬词。

## 2026-07-05 零号初版人设方案

- 最近完成：新增 `美术文档/人设/03_零号初版人设方案.md`，按用户最新口径确定零号初版：外貌参考失明少女，性格不参考；眼部遮挡参考 2B 式识别度但改为白布，服装为灰披肩 + 简单超短内衬裙 + 裸腿 + 裸足，下半身留白，胸前无装饰。
- 当前关注：方案已明确轻微灰尘、磨损和细小人偶关节；核心仓平时被披肩遮住，只有维护、受损、启动或特殊演出时才可见；性格初始为三无 / 冷感，接近 2B 式克制，后续随养成变化。
- 下一步建议：基于该方案生成 2-3 张母图方向候选，再由用户 / 主美确认发型、灰披肩剪裁、内衬裙比例、遮眼白布固定方式和裸足下半身留白是否成立。
- 问题 / 阻塞：当前仍是人设方案文档，尚未生成母图、三视图、表情差分、DollPuppet 分层图、Approved 素材、Manifest、Registry 或 ArtAcceptance 证据。

## 2026-07-05 零号三后端 AI 出图批次

- 最近完成：新增 `美术文档/人设/04_零号AI后端出图提示词对比.md`、`tools/美术工具/zero_prototype_backend_batch.py` 和 `Generate-ZeroPrototypeBackendBatch.ps1`，分别针对 ChatGPT / Gemini(nanobanana) / NovelAI 编写差异化提示词，并已通过 `tools/ai-image-gateway/config.local.yaml` 实跑出图。
- 当前关注：实跑输出位于 `UnityClient/Assets/Art/_IncomingAI/CharacterDesign/zero_prototype_ai_backend_compare/zero_v1_backend_compare_20260705_234834/`；最终数量为 ChatGPT 3 张、Gemini 3 张、NovelAI 3 张，并生成 `contact_sheet_zero_v1_backend_compare.jpg` 与 `final_summary.json`。
- 下一步建议：优先从 NovelAI 组提取白布遮眼、裸足、人偶关节和简洁轮廓结构，从 ChatGPT 组提取灰披肩和工坊氛围；Gemini 组作为构图和场景参考，暂不直接作为母图首选。
- 问题 / 阻塞：本批次仍是 `_IncomingAI` 候选图，未进入 Approved、Manifest、Registry、DollPuppet 包或 ArtAcceptance；首次批量请求中 ChatGPT / Gemini 未足量返回，已通过单张补跑补齐。

## 2026-07-07 零号三后端提示词调整重跑

- 最近完成：按用户反馈更新零号人设要求和三后端提示词：以第一轮 ChatGPT 组立绘为概念图基准；NovelAI 提示词修正为完整灰披肩覆盖双肩、娇小可爱小体型、非儿童化、禁止露肩和成熟高挑；Gemini 提示词改为白背景纯人设图。新图已按要求输出到 `美术文档/人设/AI出图/zero_v1_rerun_20260707_01/`。
- 当前关注：本轮 ChatGPT 成功 3 张，Gemini 成功 3 张，NovelAI 于 2026-07-08 00:03 补跑成功 3 张，并已刷新 `contact_sheet_zero_v1_rerun_20260707_01.jpg` 与 `final_summary.json`；Gemini 背景干净但角色偏小，NovelAI 更小体型但灰披肩更像一体式罩裙。
- 下一步建议：先从新 ChatGPT 组选择母图概念基准；Gemini 若继续使用，下一轮应追加“larger character, fills most of canvas”；NovelAI 可作为小体型、白布遮眼和简化轮廓参考，但披肩剪裁仍需以 ChatGPT 组为准继续收束。
- 问题 / 阻塞：NovelAI 昨日曾在首次 `-Count 3`、两次单张补跑和 2026-07-07 00:23 追加 `-Count 3` 中返回 `[novelai] Rate limited`；2026-07-08 重试已解除限流并生成 3 张。本批仍是人设候选图，不是 Approved、Manifest、Registry、DollPuppet 包或 ArtAcceptance 证据。

## 2026-07-08 零号三视图三后端批次

- 最近完成：按用户要求将零号发型锁为银白 / 灰白长发散发，不扎发；新增 `Generate-ZeroPrototypeTurnaroundBatch.ps1` / `zero_prototype_turnaround_batch.py`，三后端分别设计三视图 prompt。输出目录为 `美术文档/人设/AI出图/zero_v1_turnaround_20260708_01/`，并生成 `contact_sheet_zero_v1_turnaround.jpg` 与 `final_summary.json`。
- 当前关注：Gemini 三视图成功 3 张，长发散发、正侧背关系和白背景模型表最清楚；NovelAI 三视图成功 3 张，但披肩偏罩裙化，脚部和比例有漂移。ChatGPT 组当前 0 张。
- 下一步建议：先以 Gemini 三视图作为结构主参考，继续要求灰披肩按 ChatGPT 母图的披肩体块收束；待 `openai_images` 凭证修复后只补跑 ChatGPT 三视图。
- 问题 / 阻塞：ChatGPT / `openai_images` 返回 `HTTP 401: Invalid token`，本机无 `OPENAI_API_KEY` 可绕过；本批仍是人设候选图，不是 Approved、Manifest、Registry、DollPuppet 包或 ArtAcceptance 证据。

## 2026-07-10 零号红眼设定与单张动作图批次

- 最近完成：按用户补充设定，将零号眼罩后方真实眼部锁为红眼；常态仍由白布遮眼，启动、低 SAN、受损或维护半掀时才允许红光透布或短暂露出。新增并继续完善 `Generate-ZeroPrototypePoseActionBatch.ps1` / `zero_prototype_pose_action_batch.py`，用于生成单张单视角 / 单动作人设图；2026-07-11 已继续补跑 ChatGPT 与 NovelAI，并恢复三后端总览。
- 当前关注：`zero_v1_pose_actions_20260710_01` 当前共有 30 张候选文件：ChatGPT 10 张 / 7 个动作，Gemini 12 张 / 8 个动作，NovelAI 8 张 / 8 个动作。ChatGPT 是当前审美主线；Gemini 人物偏小；NovelAI 罩裙化、橙红眼罩和动作精度偏差明显，只作结构备选。
- 下一步建议：停止整批铺量，先由用户 / 主美从 ChatGPT 组确认灰披肩剪裁、体型和长发轮廓；启动准备可暂用 Gemini / NovelAI 候选表达动作，再在 ChatGPT 网关稳定时单张补图。
- 问题 / 阻塞：ChatGPT `pose_06_activation_ready` 在原 prompt、降质量、降尺寸和短 prompt 下多次于约 120 秒返回 `HTTP 502 Bad Gateway`；最小 512 smoke 第二次可成功，说明 token 可用但竖图链路波动。本批仍只是人设候选图，不是 Approved、Manifest、Registry、DollPuppet 包或 ArtAcceptance 证据。`Validate-ArtGeneratedJson.ps1 -Strict` 的唯一失败是既有 `offline_registry_candidate`（`has_missing_sprite=True`、`changed_existing=18`），记录为 `validation_limited:existing_offline_registry_candidate`，与本批无关。
- 关键证据：`美术文档/人设/AI出图/zero_v1_pose_actions_20260710_01/contact_sheet_zero_v1_pose_actions.jpg`、同目录 `final_summary.json`；可接入快照 `美术文档/_generated/art_integration_snapshots/20260711_143616_zero_pose_actions_20260711.md` 显示 `program_integrate=0`、`generate_needed=0`，本批未进入正式接入队列。
## 2026-07-10 T0-01A 序章 CG 风格纠偏启动

- 最近完成：根据用户反馈“美术风格不对”，已新增本轮风格纠偏执行清单 `UnityClient/Assets/Art/_IncomingAI/_page_reviews/t0_01a_style_correction_plan_20260710.md`。本轮判断上一版主要偏差不是素材数量，而是风格锚点偏向通用洞穴冒险插画，工坊生活压力、日系二次元故事面板低噪声、零号一致性和启动仪式感不足。
- 当前关注：先不全量重跑 15 张，优先重做六个风格锚点：`cg_t0_01a_p02_panel01_workshop_wide`、`cg_t0_01a_p05_panel02_no0_half_reveal`、`cg_t0_01a_p05_panel03_no0_core_dim`、`cg_t0_01a_p06_panel01_no0_close`、`cg_t0_01a_p06_panel02_core_insert`、`cg_t0_01a_p06_panel03_core_wake`。三后端策略为 OpenAI / Gemini / Grok 都尝试，NovelAI 仅作为必要备选或风格补充。
- 下一步建议：先完成三后端 smoke，再以 `t0_01a_cg_style_fix_20260710` 批次生成候选；筛选通过后才允许同 VisualID 覆盖 Approved，并继续只声明 `风格纠偏素材完成`，不声明运行时漫画页验收。
- 问题 / 阻塞：暂无美术决策阻塞；剩余风险是三后端输出稳定性和零号最终人设仍未完全锁死。
## 2026-07-10 T0-01A 序章 CG 风格纠偏素材替换完成

- 最近完成：根据用户反馈“美术风格不对”，已将 `narrative_cg` 生成口径从通用地底洞穴风格改为专用的日式 2D 故事漫画面板风格，并重跑 6 个 T0-01A 风格锚点候选。
- 当前关注：有效产出为 `gemini_chat_image` 6 张、`novelai` 6 张、Gemini 追加细化 3 张；`openai_images` 全部返回 HTTP 504，`grok_chat_image` 全部返回 HTTP 503 无可用渠道。筛选替换建议为 p02 `20260710_001`、p05 半揭示 `20260710_001`、p05 核心 `20260710_003`、p06 近景 `20260710_004`、p06 接入 `20260710_001`、p06 唤醒 `20260710_001`。评审证据为 `UnityClient/Assets/Art/_IncomingAI/_page_reviews/t0_01a_style_correction_review_20260710.md`、`t0_01a_style_correction_contact_20260710.png` 和 `美术文档/_generated/art_integration_snapshots/20260710_012453_t0_01a_style_fix_20260710_verify.md`。
- 下一步建议：后续进入运行时漫画页播放截图验收时，继续使用 `美术文档/19_T0-01序章CG细案.md` 的 6 张截图清单；本轮只声明 `风格纠偏素材替换完成`，不声明运行时漫画播放、VisualAssetRegistry 新登记或最终商业验收通过。
- 问题 / 阻塞：OpenAI / Grok 仍不可作为本轮稳定生产后端；NovelAI 本轮多张输出为纯色/无效图，筛选时只采用有效 Gemini 候选。`Validate-ArtGeneratedJson.ps1` 当前失败仅来自既有 `offline_registry_candidate`：`has_missing_sprite=True`、`changed_existing=18`，因此本轮不声明 Registry / 运行时漫画播放验收通过。
# 2026-07-12 MCP 美术验收 V2 已落地

- 最近完成：日常美术验收已切换为 `live-first, capture-on-decision`。默认通过 MCP 直接查看当前 Game View，并结合注册目标的 RectTransform / Graphic / CanvasGroup / CanvasScaler 等 bounded snapshot 诊断；截图只在问题、before/after、final 或 seal 决策时生成。
- 关键证据：`UnityClient/Logs/P3Validation/art-runs/art_v2_focus_final_20260712/screenshots/workshop_main/final.png` 为标准 `manage_camera(capture_source=game_view, camera omitted)` 经 ticket finalize 的 1920x1080 正式图，SHA-256 为 `f0f637a2c43774e00a6014459d2392b15a12f7f814c2cfe1f85482cb92279a73`；该 ArtRun 记录技术结果与证据。
- 最近完成：`art_regression` 已直接运行旧 `ArtAcceptanceRunner` 并导入 `art_v2_regression_final_20260712`。适配器锁定本次 source RunID `20260712_164511`，只接收本次 `latest/screenshots/` 的 21 张图、report、UI snapshot、Registry snapshot 和 checklist；日常 `art_focus/art_runtime/art_iteration/t0_art_seal` 不启动完整 runner。
- 补充证据：`art_v2_runtime_final_20260712` 已产出日常巡检 final；`art_v2_t0_seal_final_20260712` 已产出 `t0_prologue` seal；`art_v2_iteration_blocked_final_20260712` 已证明无注册 adapter 时会阻断，而不是借用测试 Fake、任意 C# 或资产路径继续。
- 当前关注：美术可以用 preview 快速试方向，但 preview 永远不能满足正式验收。正式 iteration 必须有注册 persist adapter、before/after、退出重进 PlayMode 和持久化后重新 inspect；当前目标尚未注册安全业务 adapter，因此缺 adapter 时明确阻断，不绕过。
- 下一步建议：需要迭代的具体界面先锁定可持久化事实来源，再由 UI Owner 增加对应 adapter；不需要持久化修改时可直接使用 live inspection 与 capture ticket。
- 问题 / 阻塞：`validation_limited:subagent forward-testing prohibited by user`；当前具体 TargetID 尚无业务持久化 adapter。

## 设计来源（已执行）

- 最近完成：用户批准美术验收与程序自动化测试完全分开。后续由 `p3-art-validation` 承接 `art_focus`、`art_runtime`、`art_iteration`、`t0_art_seal`，支持 MCP 驱动的运行时截图、层级诊断、Approved VisualID 接入和受控 UGUI before/after 迭代。
- 最近完成：新的 `开发文档/20_UnityMCP验收编排层实现计划.md` 已为美术侧拆出只读诊断、截图与 ArtAcceptance、受控 iteration 白名单、Profile orchestrator 和 Skill 任务；美术修改能力只有在拒绝任意 C#/菜单/资产路径的测试通过后才允许启用。
- 当前关注：美术 Skill 不运行完整 P0、不修改领域规则，也不维护人工主美判断状态。设计事实来源为 `开发文档/19_UnityMCP验收编排层设计.md`。
# 2026-07-12 P3 美术验收与迭代分离

- 最近完成：独立 ArtRunID、注册目标诊断/截图、受限 UGUI 迭代包与仅含技术结果的 Art Profile。
- 当前关注：美术只判断界面和表现，可在 MCP 下迭代；不执行全量 P0、不修改玩法规则。
- 下一步建议：真实 `art_focus` 与 `art_regression` 已产出；后续针对具体界面先注册安全 persist adapter，再生成 `art_iteration` before/after 包。
- 问题 / 阻塞：运行时截图链已验证；当前剩余限制是具体 TargetID 尚无业务持久化 adapter，不能把 preview 作为正式修改证据。
- 关键证据：真实 ArtRun 位于 `UnityClient/Logs/P3Validation/art-runs/`；发布聚合改为按两侧技术结果与输入指纹生成 `Passed/Failed/Blocked/Limited`。
