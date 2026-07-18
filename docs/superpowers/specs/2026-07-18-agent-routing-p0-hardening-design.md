---
id: agent_routing_p0_hardening_design
title: Agent 路由 P0 治理加固设计
type: design
role: 知识库
domain: agent_workflow
status: draft
source_of_truth: false
last_verified: 2026-07-18
update_rule: PROJECT_STATUS 快照边界、状态 / View 校验规则或元数据补齐范围变化时更新本设计。
---

# Agent 路由 P0 治理加固设计

## 目标

完成上一轮 Role 渐进式披露分析中确认的三个 P0：

1. 把所有 Agent 必读的 `PROJECT_STATUS.md` 压缩为当前项目快照。
2. 让文档校验器自动阻止状态页和 Role View 退回长日志或结构缺失。
3. 补齐现有两份实施计划的 YAML 元数据，使知识库达到 `missing_metadata=0`。

## 非目标

- 不改变 active Role 词表、Owner 责任、执行 / 验收协议或项目阶段。
- 不引入 Role Registry、`affected_roles`、typed relations 或自动 backlinks。
- 不修改 GDD、配置源、程序业务代码、美术资产、生成物或当前并发美术状态。
- 不拆分 `PROJECT_STATUS.md`，也不新建历史状态文件；历史追溯继续使用事实文档、验收记录和 Git。

## PROJECT_STATUS 当前快照

保留现有 YAML 元数据和双向关系，正文统一为：

1. 最后更新
2. 当前阶段
3. 当前顶层目标
4. 当前优先级
5. 跨职能交接
6. 问题 / 阻塞
7. 下一步总建议
8. 关键入口

正文不得保留按日期无限增长的 `最近完成` 日志，也不得同时保留已被后续结论覆盖的通过 / 否决记录。当前仍有效的验收边界压缩进优先级、阻塞或交接；完整过程回到目标事实文档。

校验器按解析后的正文计算行数，`PROJECT_STATUS.md` 正文最多 80 行，不计算 YAML front matter。

## 状态页防回退规则

校验范围为以下 active 状态页：

- `agent_status/director.md`
- `agent_status/design.md`
- `agent_status/program.md`
- `agent_status/art.md`

每页必须包含：

- `## 最后更新`
- `## 当前关注`
- `## 最近完成`
- `## 下一步建议`
- `## 问题 / 阻塞`
- `## 关键证据入口`

正文最多 80 行，并禁止 `## YYYY-MM-DD ...` 日期日志标题。`agent_status/README.md` 和 archived `pm.md` 不适用该体量门禁。

## Role View 防回退规则

校验以下 active View：

- `知识库/views/director.md`
- `知识库/views/owner.md`
- `知识库/views/narrative.md`
- `知识库/views/design.md`
- `知识库/views/program.md`
- `知识库/views/art.md`

每页必须包含：

- `## 必读`
- `## 按任务读取`
- `## 验收 / 恢复时` 或 `## 验收 / 恢复时读取`

校验只确认结构存在，不限制各 Role 的专业内容和文档数量。

## 校验器结构

将 `validate_docs.py` 中新增规则拆为小型纯函数：

- 提取正文二级标题。
- 校验必须标题。
- 计算正文行数。
- 识别日期日志标题和日期日志条目。

新增 Python 单元测试覆盖：

- 合法状态快照通过。
- 缺少章节失败。
- 超过正文行数失败。
- 日期日志标题失败。
- 合法 Role View 通过。
- 缺少三层阅读章节失败。

现有索引新鲜度、元数据、Role 白名单和双向关系校验保持不变。

## 元数据补齐

以下文件只增加 YAML front matter，不修改实施计划正文：

- `docs/superpowers/plans/2026-07-18-unified-art-processing-rounds.md`
- `docs/superpowers/plans/2026-07-18-zero-dialogue-neutral-character-portrait-pilot.md`

两者使用 `type: plan`、`role: 美术`、`status: active`、`source_of_truth: false`，分别使用独立稳定 ID 和现有主题 domain。

## 验收标准

1. `PROJECT_STATUS.md` 正文不超过 80 行，且无日期完成日志。
2. 四个 active 状态页通过结构、体量和日期日志门禁。
3. 六个 active Role View 通过三层阅读结构门禁。
4. 新增校验单元测试通过。
5. `Validate-Docs.ps1` 输出 `missing_metadata=0`。
6. 本次提交不包含当前并发美术文件、生成物或 submodule 改动。
