---
id: config_narrative_readme
title: Narrative 配置说明
type: config_readme
role: 策划
domain: narrative_config
status: active
source_of_truth: true
related:
  - 开发文档/18_全局叙事播放系统开发方案.md
  - 版本规划/0-12小时细案/T0-01A_开局人偶状态到首次下潜许可开发方案.md
  - agent_status/design.md
  - agent_status/program.md
last_verified: 2026-07-01
update_rule: 修改 Narrative 配置字段、Yarn 脚本源、触发条件或校验口径时同步本文件。
---

# Narrative 配置说明

`配置表(JSON)/Narrative` 是全局叙事播放系统的源配置目录。`UnityClient/Assets/StreamingAssets/Configs/Narrative` 只由同步脚本生成，不手写维护。

首批 T0-01A 配置包含：

- `source_tables/t0_01a_prologue.dialogue.csv`：文案编辑源和 line key 对照。
- `scripts/t0_01a_prologue.yarn`：Yarn / 等效运行时脚本源。
- `narrative_nodes.json`：节点清单、脚本来源、说话人、line key、VisualID / fallback 和命令声明。
- `narrative_triggers.json`：触发事件、条件、播放模式、优先级和完成旗标。
- `narrative_speakers.json`：说话人、显示名 key、默认头像 / fallback。
- `narrative_flags.json`：叙事旗标、写入时机和存档口径。
- `narrative_commands.json`：允许命令、参数数量和上下文授权。

按钮、状态卡、首潜确认面板等不应作为对白播放的 UI 文案，可以在 `source_tables/*.dialogue.csv` 中使用 `row_type=comment` 记录稳定 `line_key`，并在 Yarn 节点内用 `// ui_text ... #line:<key>` 注释保留审校入口。当前 P3 等效运行时会跳过注释行，Validator 仍能用这些 key 检查节点清单与源表闭合。

配置完成判定必须同时满足：源配置已落地、`Sync-Configs.ps1 -Clean` 成功、Narrative Validator 能检查 node / trigger / speaker / flag / command / VisualID fallback / line key 引用，并回写状态页证据。
