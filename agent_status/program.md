---
id: agent_status_program
title: 程序 / Unity 状态
type: status
role: 程序
domain: unity_programming
status: active
source_of_truth: true
related:
  - 版本规划/09_正式版核心纵切开发路线.md
  - 版本规划/10_正式版核心纵切版本节点规划.md
  - agent_status/README.md
  - PROJECT_STATUS.md
  - 开发文档/00_客户端核心架构规范.md
  - 开发文档/13_编程规范与架构约定.md
  - 开发文档/00_程序开发大纲.md
  - 设计文档/GDD_00_系统关联总图.md
  - agent_status/design.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - agent_status/art.md
  - 知识库/views/program.md
last_verified: 2026-05-24
update_rule: 程序、Unity、验证或工程边界任务完成后更新本文件。
---

# 程序 / Unity 状态

## 最后更新

2026-05-24

## 当前关注

支撑正式版核心纵切 Alpha，让 Unity 运行时系统保持模块清晰、数据驱动、可测试，并与当前 GDD 规则一致。

PM 版本节点中，程序线当前落在 A1 核心边界收口：继续收口 `GameFlowController`、runtime fallback UI、配置校验和 agent 健康检查，为 A2 背包与战斗纵切降低冲突风险。

## 必读文件

- `知识库/views/program.md`
- `开发文档/00_程序开发大纲.md`
- `开发文档/00_客户端核心架构规范.md`
- `开发文档/00_Unity表现层与编辑器构建规范.md`
- `开发文档/12_程序开发优化建议与重构路线.md`
- `开发文档/13_编程规范与架构约定.md`
- 当前系统对应的 `开发文档/` 落地文档。

## 工作边界

- 运行时 UI 使用纯 UGUI。不要重新引入 UI Toolkit 运行时资产或代码路径。
- `配置表(JSON)` 是配置源；`UnityClient/Assets/StreamingAssets/Configs` 是生成副本。
- 游戏规则属于后端 / 领域服务，不属于 UI Controller。
- UI Controller 只调用后端 API 并监听事件。
- 移动 Unity 资产时保留并同步移动 `.meta` 文件。
- `tools/ai-image-gateway` 按 submodule 处理。

## 常用命令

```powershell
.\tools\config\Sync-Configs.ps1 -Clean
Set-Content -Path "UnityClient/Logs/.test_trigger" -Value "RUN_ALL_TESTS"
```

## 最近完成

- 已创建复制程序智能体使用的状态页。
- 运行时 Prefab 已移动到 `UnityClient/Assets/Prefabs`。
- 已新增并验证 `tools/config/Sync-Configs.ps1`。
- 项目结构整理时已移除旧的受跟踪 UI Toolkit 运行时资产。
- PM 已将程序线纳入 `版本规划/10_正式版核心纵切版本节点规划.md`：A1 收边界，A2 做背包与战斗正式纵切，A3 承接深渊与局外成长。
- Alpha 1A 背包交互继续收口：`DraggableItemUI` 已移除历史“失联物品自动修复”，点击使用不再由 UI 悄悄改写真实背包状态。
- Alpha 1A 背包表现规格继续收口：新增 `InventoryDisplaySpec`，统一格子尺寸、间距、物品占格尺寸、拖拽偏移和各界面背包布局 profile。
- `InventoryDisplaySpecSmokeTest` 已通过 Unity 自动测试守护执行，确认背包规格入口、布局 profile 和 GridLayoutGroup 应用结果一致。
- `GridGenerator` 与 `MVPEditorSetup` 已接入 `InventoryDisplaySpec.ApplyGridLayout()`，背包格运行时生成和编辑器骨架默认值不再各自维护尺寸常量。
- 美术侧已交付 `combat_hud` Formal V1 active 规格：敌人从卡片迁移为右侧战斗实体，背包底部居中，敌人血条贴脚下。

## 下一步建议

1. 按 A1 节点增加 `tools/agent` 启动与验证脚本，让复制智能体快速检查仓库健康状态。
2. 继续推进 Alpha 1A 背包交互正式化：检查正式 UI Prefab / 场景资产中的背包 GridLayoutGroup 默认值，必要时通过编辑器工具统一修正。
3. 接入 `combat_hud` Formal V1 时以 active `screen_layouts.json` 为准，敌人实体优先读 `MonsterEntity.CombatVisualID`，缺图再临时 fallback 到 `PortraitID`。
4. 增加配置校验工具，检查必填字段、唯一 ID、交叉引用和正式 UI 缺引用。

## 问题 / 阻塞

- 当前工作区已有其他 agent / 用户留下的 Unity UI 脚本脏文件，编辑前需要先检查并避免覆盖无关改动。
- `combat_hud` Formal V1 的 `monster_*_combat` 透明战斗实体素材尚未生成，程序接入阶段需要 fallback 或占位策略。
- `tools/ai-image-gateway` 子模块内部有未提交改动。

## 完成回写清单

- 更新本文件的 `最后更新`。
- 在 `最近完成` 记录简短事实。
- 如有变化，刷新 `当前关注`、`下一步建议` 和阻塞项。
- 如果需要美术或策划跟进，在 `PROJECT_STATUS.md` 增加跨职能交接。
