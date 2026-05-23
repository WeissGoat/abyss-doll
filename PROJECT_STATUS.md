---
id: project_status
title: 项目状态
type: status
role: 全局
domain: project_status
status: active
source_of_truth: true
related:
  - AGENTS.md
  - GEMINI.md
  - 版本规划/09_正式版核心纵切开发路线.md
  - agent_status/pm.md
  - agent_status/program.md
  - agent_status/design.md
  - agent_status/art.md
  - 知识库/views/pm.md
  - 知识库/views/art.md
  - 知识库/views/design.md
  - 知识库/views/program.md
last_verified: 2026-05-23
update_rule: 项目阶段、总目标、跨职能交接或阻塞项变化时更新本文件。
---

# 项目状态

> 项目级状态页。复制出来的智能体如果改变了当前里程碑、跨职能交接、阻塞项或下一步总优先级，需要更新这里。

## 最后更新

2026-05-23

## 当前阶段

正式版核心纵切 Alpha。

项目已经从 MVP 闭环验证转入正式版纵切开发。当前开发应按纵向链路推进：

```text
设计规则 -> 配置结构 -> 运行行为 -> UI/表现 -> 验证 -> 文档同步
```

## 当前顶层目标

`版本规划/09_正式版核心纵切开发路线.md`

## 当前优先级

1. 背包与物品生命周期正式化。
2. 战斗与怪物机制框架正式化。
3. 深渊探索、层级入口、撤离与失败节奏正式化。
4. 局外成长、义体、底盘、配方和材料缺口形成正式构筑方向。
5. UI / 美术资源接入流程稳定化，运行时截图验收逐步成为质量门禁。
6. UI 从 MVP Baseline 迁移到 Formal V1 正式结构，优先重审战斗界面。

## 职能状态页

- PM / 版本规划：`agent_status/pm.md`
- 美术 / UI：`agent_status/art.md`
- 策划 / 数值：`agent_status/design.md`
- 程序 / Unity：`agent_status/program.md`

## 最近完成

- 项目结构整理已提交：`d5434b8 chore: tidy project structure`。
- `tools/ai-image-gateway` 已登记为 submodule。
- 运行时 Prefab 已移动到 `UnityClient/Assets/Prefabs`。
- 已从受版本控制的 Unity 工程路径中移除旧 UI Toolkit 运行时资产。
- 已新增 `tools/config/Sync-Configs.ps1`，用于把 `配置表(JSON)` 同步到 Unity `StreamingAssets`。
- `AGENTS.md` 已扩展为复制智能体的统一入口。
- 已新增项目总状态页和美术 / 策划 / 程序三个职能状态页。
- 知识库智能体已完成轻量知识库首版：元数据规范、核心文档元数据头、索引生成和校验脚本。
- 已为当前索引内全部 Markdown 文档补齐元数据头，并重新生成 `DOCS_INDEX.md` 与 `docs_index.json`。
- 已把文档 `related` 元数据升级为双向关系网，让美术、程序、策划、配置、数值和版本路线文档形成可校验互链。
- 知识库校验已补上索引新鲜度检查，会扫描当前 Markdown 清单并与 `docs_index.json` 比对，避免新增文档未入索引时误通过。
- 已新增 `知识库/views/art.md`、`知识库/views/design.md`、`知识库/views/program.md`，作为复制智能体按职能开工的阅读入口。
- 已明确 `UnityClient/Assets/StreamingAssets/Configs` 是由 `配置表(JSON)` 同步生成的运行时副本，不作为知识库事实来源。
- 美术智能体已建立 UI 设计版本化流程：MVP Baseline 冻结、Formal V1 结构设计、active `screen_layouts.json` 确认后更新、再进入素材生成和程序接入。
- P0 UI MVP Baseline 已运行时验收通过；下一步不是继续美化 MVP 结构，而是先重审 Formal V1 战斗/工坊/拾取等正式结构。
- PM / 版本规划智能体已建立状态页和阅读入口，用于维护正式版核心纵切 Alpha 的阶段判断、里程碑、优先级和跨职能交接。

## 跨职能交接

- 美术/UI 与程序智能体都应以纯 UGUI 作为运行时 UI 目标。
- 策划与程序智能体做运行时验证前应先同步配置：`.\tools\config\Sync-Configs.ps1 -Clean`。
- 美术智能体刷新 Manifest 前应先同步配置，确保视觉需求跟随当前配置源。
- 美术 / UI 的程序接入只以 `美术文档/ui_design/screen_layouts.json` 当前 active 规格为准；`versions/` baseline 和 candidate 不作为程序接入口。
- Formal V1 UI 文档由美术侧先提出，用户确认后再更新 active 规格、生成素材并交给程序接入。
- 任意智能体修改系统规则时，必须更新对应 GDD 或开发文档，不能只改代码或配置。
- 任意智能体新增或调整文档关联时，必须维护 `related` 双向互链，并运行 `.\tools\docs\Validate-Docs.ps1`。
- 复制智能体需要快速定位上下文时，优先读取 `知识库/views/` 下对应职能入口，再进入事实来源文档。
- 配置相关问题以 `配置表(JSON)` 为源；不要手写修改 `UnityClient/Assets/StreamingAssets/Configs` 运行时副本。
- PM / 版本规划智能体负责维护 `09` 路线、`PROJECT_STATUS.md` 和跨职能版本拆分；变更阶段、优先级、里程碑或交接时必须同步目标职能状态页。

## 问题 / 阻塞

- 当前工作区已有前序 UI、美术、生成物和 submodule 相关脏文件。后续智能体暂存时需要严格收窄范围。
- `tools/ComfyUI_NAIDGenerator/` 当前未跟踪，后续需要决定它是 vendor 代码、submodule，还是本地专用工具。
- `tools/ai-image-gateway` 子模块内部有未提交改动；如需处理，应进入子模块内部单独处理。

## 下一步总建议

1. 增加 `tools/agent` 健康检查脚本，方便复制智能体开工前检查仓库状态。
2. 增加配置校验工具，检查 `配置表(JSON)` 的 ID、必填字段和交叉引用。
3. 为知识库索引增加可选的职能 / 领域 / 关联深度过滤入口，方便复制智能体按任务快速定位文档。
4. 美术侧先完成 `combat_hud` Formal V1 结构确认和 active 规格更新，再交给 UI 程序侧迁移。
5. PM 先从 Alpha 优先级中选择下一个正式纵切系统，并通过职能状态页拆分给美术 / 策划 / 程序。
