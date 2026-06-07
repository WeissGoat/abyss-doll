---
id: dev_rules_readme
title: 程序开发规范目录入口
type: dev
role: 程序
domain: program_rules_index
status: active
source_of_truth: true
related:
  - 开发文档/README.md
  - 开发文档/00_程序开发大纲.md
  - 开发文档/rules/13_编程规范与架构约定.md
  - 开发文档/rules/00_客户端核心架构规范.md
  - 开发文档/rules/00_Unity表现层与编辑器构建规范.md
  - 开发文档/rules/00_自动化测试框架与流程指南.md
  - 开发文档/rules/09_视觉资源系统程序开发规范.md
last_verified: 2026-06-07
update_rule: 新增、移动或废弃程序规范文档时同步本文件。
---

# 程序开发规范目录入口

> `rules/` 只放硬性规范和协作约定，不放具体功能需求。具体系统怎么实现仍回到 `开发文档/` 根目录的系统文档和对应 GDD / 配置文档。

## 当前规范文档

| 文档 | 定位 |
|---|---|
| `13_编程规范与架构约定.md` | 程序开发硬约束：系统归属、状态归属、配置驱动、Validator、测试、UI 边界和协作规范。 |
| `00_客户端核心架构规范.md` | 客户端宏观架构：CoreBackend、领域层、事件总线、表现层隔离和工厂模式。 |
| `00_Unity表现层与编辑器构建规范.md` | Unity 表现层 / 编辑器构建规范：纯 UGUI、场景构建、表现队列和 UI 交互边界。 |
| `00_自动化测试框架与流程指南.md` | AutoTestDaemon、`.test_trigger`、Headless VisualQueue 和 smoke test 工作流。 |
| `09_视觉资源系统程序开发规范.md` | VisualID、VisualAssetRegistry、VisualAssetService、DisplaySpec 和美术资源程序接入契约。 |

## 使用规则

- 开发新功能前，先按 `13_编程规范与架构约定.md` 写清最小设计卡。
- 涉及 Unity UI / 美术接入时，同时读取 `00_Unity表现层与编辑器构建规范.md` 和 `09_视觉资源系统程序开发规范.md`。
- 涉及自动化验证、Unity smoke test 或一键验收时，读取 `00_自动化测试框架与流程指南.md`。
- 规范文档不替代系统需求。具体字段、API、状态机和验收样例必须写入对应系统开发文档或 GDD / 配置文档。
