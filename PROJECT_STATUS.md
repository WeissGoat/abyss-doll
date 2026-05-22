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
  - 版本规划/09_正式版核心纵切开发路线.md
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

## 职能状态页

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

## 跨职能交接

- 美术/UI 与程序智能体都应以纯 UGUI 作为运行时 UI 目标。
- 策划与程序智能体做运行时验证前应先同步配置：`.\tools\config\Sync-Configs.ps1 -Clean`。
- 美术智能体刷新 Manifest 前应先同步配置，确保视觉需求跟随当前配置源。
- 任意智能体修改系统规则时，必须更新对应 GDD 或开发文档，不能只改代码或配置。

## 问题 / 阻塞

- 当前工作区已有前序 UI、美术、生成物和 submodule 相关脏文件。后续智能体暂存时需要严格收窄范围。
- `tools/ComfyUI_NAIDGenerator/` 当前未跟踪，后续需要决定它是 vendor 代码、submodule，还是本地专用工具。
- `tools/ai-image-gateway` 子模块内部有未提交改动；如需处理，应进入子模块内部单独处理。

## 下一步总建议

1. 增加 `tools/agent` 健康检查脚本，方便复制智能体开工前检查仓库状态。
2. 增加配置校验工具，检查 `配置表(JSON)` 的 ID、必填字段和交叉引用。
3. 为知识库索引增加可选的职能 / 领域过滤入口，方便复制智能体按任务快速定位文档。
4. 从 Alpha 优先级中选择下一个正式纵切系统，并通过职能状态页拆分给美术 / 策划 / 程序。
