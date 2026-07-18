---
id: agent_status_program
title: 程序 / Unity 状态
type: status
role: 程序
domain: unity_programming
status: active
source_of_truth: true
related:
  - 版本规划/0-12小时细案/T0-01_序章首次循环开发总方案.md
  - 版本规划/0-12小时细案/T0-01A_开局人偶状态到首次下潜许可开发方案.md
  - 版本规划/09_正式版核心纵切开发路线.md
  - agent_status/README.md
  - PROJECT_STATUS.md
  - 开发文档/rules/01_客户端分层与领域架构规范.md
  - 开发文档/rules/00_程序开发总规则.md
  - 开发文档/00_程序开发大纲.md
  - 开发文档/15_P0配置Validator与自动验收底座需求.md
  - 开发文档/16_程序主流程闭环与架构收口推进计划.md
  - 设计文档/GDD/GDD_00_系统关联总图.md
  - agent_status/design.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - agent_status/art.md
  - 知识库/views/program.md
  - 美术文档/16_Live2D角色动画资产接入规格.md
  - 美术文档/17_Agent原生动态立绘资产接入规格.md
  - 开发文档/17_Live2DSpine运行时接入评估.md
  - 开发文档/18_全局叙事播放系统开发方案.md
  - 配置表(JSON)/Narrative/README.md
last_verified: 2026-07-18
update_rule: 程序、Unity、验证或工程边界任务完成后更新本文档。
---

# 程序 / Unity 状态

## 最后更新

2026-07-18

## 当前关注

- 程序正式版按完整玩家结果推进，运行时 UI 保持纯 UGUI，游戏规则留在后端 / 领域服务，配置源来自 `配置表(JSON)`。
- `p3-program-validation`、`p3-art-validation`、`p3-release-validation` 已分离；程序验证只负责编译、配置、Smoke、P0 / T0 功能路径，不替代美术验收或发布聚合。
- T0-01A 的 NARR-00..04 和 T0-FLOW-01..05 已有程序承接与 headless / smoke 证据；当前重点是 `SEAL-FLOW`、`SEAL-UI`、FormalV2 工坊 zone、首潜许可卡和连续截图防回归。
- AI 图片网关 chat image provider 已支持默认关闭、请求级可覆盖的透明 SSE 增量读取；当前继续观察 Gemini 双参考图长耗时响应的上游完整性。

## 最近完成

- 全局叙事播放系统已接入配置加载、触发调度、状态回写、UGUI 对白层、过程 CG 容器和命令桥，运行时仍需按场景补验证证据。
- Unity MCP 验收编排已拆分为程序、美术和发布 lanes；程序 lane 不再自动启动 ArtAcceptance。
- T0-01A 的启动零号、苏醒对白、首潜确认和真实 `DungeonManager.StartRunAtLayer(1)` 链路已有实现 / smoke 证据，但不等于完整 T0 验收完成。
- `tools/ai-image-gateway` 已完成 `httpx-sse` 透明流式接入、同连接 JSON fallback、错误截断、断流无 buffered 二次请求和 secret-safe smoke CLI；目标测试 `35 passed`、全量 `121 passed`，真实 Gemini 文生图流式在 `109.672s` 成功。

## 下一步建议

- 按 T0-01A 开发方案补 `SEAL-FLOW-01`、`SEAL-UI-01..03` 和 `SEAL-VAL-01`，优先验证正常玩家 UI 可达、状态真实变化和固定截图语义。
- 继续保持程序验证与美术验证分离；需要运行时画面时使用对应的 `p3-art-validation` 或 `p3-release-validation` 路由。
- 修改系统契约时同步开发文档、Validator、测试和受影响状态页；不要把运行时副本或截图当作配置源。

## 问题 / 阻塞

- Unity Editor / MCP 现场验证仍可能受环境和既有 MCPForUnity 版本 warning 限制；受限时必须记录 `validation_limited:*`。
- T0-01A 的商业化连续画面、最终美术接入和专业 Role 外部验收仍未完成。
- Gemini 双参考图流式 smoke 在 `292.906s` 后遭遇上游 `incomplete chunked read`，没有可解码图片；当前为 `validation_limited:stream_request_failed_before_success_evidence`，不能声明双图端到端稳定。

## 关键证据入口

- `开发文档/00_程序开发大纲.md`
- `开发文档/18_全局叙事播放系统开发方案.md`
- `开发文档/19_UnityMCP验收编排层设计.md`
- `开发文档/20_UnityMCP验收编排层实现计划.md`
- `docs/superpowers/specs/2026-07-18-ai-image-gateway-transparent-streaming-design.md`
- `tools/ai-image-gateway/docs/openai_compatible_relay_integration.md`
- `版本规划/0-12小时细案/T0-01A_开局人偶状态到首次下潜许可开发方案.md`
- `配置表(JSON)/Narrative/README.md`
