---
id: agent_status_art
title: 美术 / UI 状态
type: status
role: 美术
domain: art_pipeline
status: active
source_of_truth: true
related:
  - AGENTS.md
  - 美术文档/README.md
last_verified: 2026-05-23
update_rule: 美术或 UI 视觉流水线任务完成后更新本文件。
---

# 美术 / UI 状态

## 最后更新

2026-05-23

## 当前关注

支撑正式版核心纵切 Alpha，让 UI 与视觉资源清晰表达当前玩法规则，同时保持 Manifest 驱动的美术流水线可靠。

## 必读文件

- `美术文档/README.md`
- `美术文档/10_正式版核心纵切美术路线.md`
- `美术文档/00_美术流水线总览.md`
- `美术文档/ui_design/README.md`
- `美术文档/01_Manifest规范.md`
- `开发文档/09_视觉资源系统程序开发规范.md`
- `开发文档/14_Unity运行时美术自动验收方案.md`

## 工作边界

- 运行时 UI 目标是纯 UGUI。不要新增 UI Toolkit、UXML、USS 或 `UIDocument` 运行时流程。
- `美术文档/art_requirements_seed.json` 维护配置表无法扫描出的视觉需求。
- `美术文档/_generated` 与 `美术文档/ui_design/_generated` 是生成输出。
- 正式运行时资源放在 `UnityClient/Assets/Art/Approved`。
- AI 出图工作区 `UnityClient/Assets/Art/_IncomingAI` 保持忽略。

## 常用命令

```powershell
.\tools\config\Sync-Configs.ps1 -Clean
.\tools\美术工具\Update-ArtManifest.ps1
.\tools\美术工具\Generate-ArtPrompts.ps1
.\tools\美术工具\Validate-UIDesign.ps1
```

## 最近完成

- 已创建复制美术智能体使用的状态页。
- 项目已增加 Manifest 扫描前的配置同步步骤。
- 项目结构整理时已移除旧的受跟踪 UI Toolkit 运行时资产。

## 下一步建议

1. 新增美术工作前，先检查当前已有的美术文档和生成 Manifest 脏文件。
2. 判断新的 Alpha UI 交付应更新 `美术文档/11_Alpha_P0_UI骨架接入交付.md` 还是 `美术文档/12_Alpha_P1_UI骨架接入准备.md`。
3. 视觉需求变化后，保持 `art_manifest.json`、提示词清单和 UI 设计交付摘要一致。

## 问题 / 阻塞

- 当前工作区已有多个美术文档和生成文件处于脏状态，后续美术智能体编辑前需要先阅读差异。
- `tools/ComfyUI_NAIDGenerator/` 未跟踪，需要决定是否纳入正式美术流水线。

## 完成回写清单

- 更新本文件的 `最后更新`。
- 在 `最近完成` 记录简短事实。
- 如有变化，刷新 `当前关注`、`下一步建议` 和阻塞项。
- 如果需要程序或策划跟进，在 `PROJECT_STATUS.md` 增加跨职能交接。
