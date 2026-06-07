---
id: dev_archive_readme
title: 开发文档归档目录入口
type: dev
role: 程序
domain: program_docs_archive
status: active
source_of_truth: true
related:
  - 开发文档/README.md
  - 开发文档/archive/06_架构评估与收口建议.md
last_verified: 2026-06-07
update_rule: 开发文档归档规则或归档清单变化时同步本文件。
---

# 开发文档归档目录入口

> 本目录保存历史评估、早期建议和已被当前事实文档吸收的旧方案。归档文档只作为背景参考，不作为当前开发事实源。

## 当前归档

| 文档 | 归档原因 | 当前替代入口 |
|---|---|---|
| `06_架构评估与收口建议.md` | 早期架构体检和收口建议已被正式纵切路线、程序规范和重构路线吸收。 | `开发文档/12_程序开发优化建议与重构路线.md`、`开发文档/rules/13_编程规范与架构约定.md`、`agent_status/program.md` |

## 归档规则

- 归档文档的 front matter `status` 应改为 `archived`，`source_of_truth` 应改为 `false`。
- 如果归档文档仍有历史参考价值，保留 `related` 指向当前替代入口。
- 新开发、配置、UI 或验收工作不得只引用归档文档作为依据；必须回到 active 文档或状态页确认当前口径。
