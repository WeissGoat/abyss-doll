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

支撑正式版核心纵切，让 Unity 运行时系统保持模块清晰、数据驱动、可测试，并与当前 GDD 规则一致。

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
- 接入新 Approved 美术素材前，先查看 `美术文档/_generated/可接入素材清单.md`；`program_integrate` 是当前可登记 / 可接入队列。
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
- A1A 背包交互继续收口：`DraggableItemUI` 已移除历史“失联物品自动修复”，点击使用不再由 UI 悄悄改写真实背包状态。
- A1A 背包表现规格继续收口：新增 `InventoryDisplaySpec`，统一格子尺寸、间距、物品占格尺寸、拖拽偏移和各界面背包布局 profile。
- `InventoryDisplaySpecSmokeTest` 已通过 Unity 自动测试守护执行，确认背包规格入口、布局 profile 和 GridLayoutGroup 应用结果一致。
- `GridGenerator` 与 `MVPEditorSetup` 已接入 `InventoryDisplaySpec.ApplyGridLayout()`，背包格运行时生成和编辑器骨架默认值不再各自维护尺寸常量。
- 新增 `InventoryGridLayoutAssetValidator` Editor 工具与 smoke test，自动检查 Prefab / Scene 中背包 GridLayoutGroup 默认值是否符合 `InventoryDisplaySpec`。
- 新增 `tools/agent/Invoke-UnitySmokeTests.ps1`，复制程序智能体可一键触发背包交互服务、DisplaySpec 与 GridLayoutGroup 资产布局 smoke test。
- 美术侧已交付 `combat_hud` Formal V1 active 规格：敌人从卡片迁移为右侧战斗实体，背包底部居中，敌人血条贴脚下。
- 美术侧已入库 `combat_hud` Formal V1 所需第一批战斗资源：4 个 `monster_*_combat`、`ui_combat_entity_shadow` 和 `ui_combat_target_ring`。
- UI 程序侧已接入 `combat_hud` Formal V1：`HUDController` 生成左玩家 / 右敌人实体舞台、敌人脚下血条、目标光环和底部居中战斗背包，敌人实体优先读取 `MonsterEntity.CombatVisualID`。
- 已刷新 `VisualAssetRegistry` 并登记 4 个 `monster_*_combat`、`ui_combat_entity_shadow`、`ui_combat_target_ring`；`VisualAssetSmokeTest.Run`、`InventoryDisplaySpecSmokeTest.Run`、`ConfigValidationSmokeTest.Run` 均通过。
- ArtAcceptance `20260524_043441` 已通过，`combat_hud` 截图无 warnings / errors，可交美术侧做正式视觉验收。
- 美术侧已提供 `美术文档/_generated/可接入素材清单.md/json`，程序侧可按 `program_integrate` 条目自助发现待接入素材。
- A2 背包旋转规则已配置化：`Grid.CanRotate` / `Grid.RotationSteps` 纳入 `ItemGridComponent`、`BackpackGrid`、`InventoryInteractionService`、`ConfigValidator` 和 `InventoryInteractionServiceSmokeTest`，方向型相邻效果会随物品当前旋转后的朝向重新计算。

## 下一步建议

1. 继续推进 A2 背包交互正式化：补 UI 层拖拽取消、战利品拾取共用规则和非战斗整理路径的回归测试。
2. 继续收口物品生命周期：使用、消耗、出售、制造消耗、撤离和战败损失都应走领域服务并补 smoke test。
3. 等美术侧基于 ArtAcceptance 最新截图验收 `combat_hud` Formal V1，如需微调敌人站位、血条层级或目标光环显示，再回到 HUD 表现层处理。
4. 接入美术新素材时优先读取 `美术文档/_generated/可接入素材清单.md`，先处理 `program_integrate` 队列，再回到运行时截图验收。
5. 增加配置校验工具，检查必填字段、唯一 ID、交叉引用和正式 UI 缺引用。

## 问题 / 阻塞

- 当前工作区已有其他 agent / 用户留下的 Unity UI 脚本脏文件，编辑前需要先检查并避免覆盖无关改动。
- `tools/ai-image-gateway` 子模块内部有未提交改动。

## 完成回写清单

- 更新本文件的 `最后更新`。
- 在 `最近完成` 记录简短事实。
- 如有变化，刷新 `当前关注`、`下一步建议` 和阻塞项。
- 如果需要美术或策划跟进，在 `PROJECT_STATUS.md` 增加跨职能交接。

