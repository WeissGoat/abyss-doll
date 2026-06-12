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
  - 版本规划/README.md
  - 版本规划/13_正式版全局体验总线与开放节奏.md
  - 版本规划/09_正式版核心纵切开发路线.md
  - agent_status/director.md
  - agent_status/pm.md
  - agent_status/program.md
  - agent_status/design.md
  - 设计文档/README.md
  - 设计文档/delivery/00_策划文档开发交付审计.md
  - 设计文档/rules/01_局外时间与日程口径规则卡.md
  - 设计文档/rules/02_物品背包旋转与生命周期规则卡.md
  - 设计文档/rules/03_战斗回合与怪物意图规则卡.md
  - 设计文档/rules/04_小镇经济结算与压力链规则卡.md
  - 设计文档/rules/05_局外成长与维护规则卡.md
  - 设计文档/rules/06_标签与特质规则卡.md
  - 设计文档/rules/07_势力声望与订单规则卡.md
  - 设计文档/rules/08_人偶核心状态与好感双轨规则卡.md
  - 设计文档/rules/09_人偶交互事件与反馈规则卡.md
  - 设计文档/rules/10_剧本调度与事件队列规则卡.md
  - 设计文档/rules/11_人偶房间布局与视觉叙事规则卡.md
  - 设计文档/content_packs/21_人偶成长修复内容包.md
  - 设计文档/content_packs/22_小镇经济月租内容包.md
  - 设计文档/content_packs/23_长期记忆剧情内容包.md
  - 设计文档/content_packs/24_势力订单声望内容包.md
  - 设计文档/content_packs/25_第三层路线侵蚀内容包.md
  - 设计文档/config/designs/31_第一层正式配置落地设计.md
  - 设计文档/config/designs/32_第二层正式配置落地设计.md
  - 设计文档/config/designs/33_第三层正式配置落地设计.md
  - 设计文档/config/tasks/34_前三层正式配置实现任务拆分.md
  - 设计文档/config/validation/35_前三层正式配置Validator与固定Seed验收样例.md
  - 设计文档/config/audits/36_局外成长正式配置承接审计.md
  - 设计文档/config/designs/37_局外成长正式配置落地设计.md
  - 设计文档/config/tasks/38_局外成长正式配置实现任务拆分.md
  - 设计文档/config/gates/39_局外成长正式配置ID锁定与冲突检查.md
  - 设计文档/config/designs/40_局外成长底盘正式配置落地设计.md
  - 设计文档/config/designs/41_局外成长效果特质正式配置落地设计.md
  - 设计文档/config/designs/42_局外成长义体正式配置落地设计.md
  - 设计文档/config/designs/43_局外成长制造维护正式配置落地设计.md
  - 设计文档/config/designs/44_局外成长人偶特质房间正式配置落地设计.md
  - 设计文档/config/validation/45_局外成长正式配置Validator与固定验收样例.md
  - 设计文档/config/audits/46_经济压力正式配置承接审计.md
  - 设计文档/config/designs/47_经济压力核心正式配置落地设计.md
  - 设计文档/config/designs/48_经济压力传闻正式配置落地设计.md
  - 设计文档/config/designs/49_经济压力势力正式配置落地设计.md
  - 设计文档/config/designs/50_经济压力订单正式配置落地设计.md
  - 设计文档/config/validation/51_经济压力正式配置Validator与固定验收样例.md
  - 设计文档/config/tasks/52_经济压力正式配置实现任务拆分.md
  - 设计文档/config/gates/53_经济压力正式配置ID锁定与冲突检查.md
  - 设计文档/config/gates/54_经济压力正式配置README字段口径检查.md
  - 设计文档/config/gates/55_经济压力订单ID最终锁定表.md
  - 设计文档/config/gates/56_正式配置源落地准入门禁.md
  - 开发文档/15_P0配置Validator与自动验收底座需求.md
  - agent_status/art.md
  - 知识库/views/director.md
  - 知识库/views/pm.md
  - 知识库/views/art.md
  - 知识库/views/design.md
  - 知识库/views/program.md
  - tools/agent/README.md
  - tools/p3-mission/README.md
last_verified: 2026-06-13
update_rule: 项目阶段、总目标、跨职能交接或阻塞项变化时更新本文件。
---

# 项目状态

> 项目级状态页。复制出来的智能体如果改变了当前里程碑、跨职能交接、阻塞项或下一步总优先级，需要更新这里。

## 最后更新

2026-06-13

## 当前阶段

正式版核心纵切。

项目已经从 MVP 闭环验证转入正式版纵切开发。这里的“纵切”是开发顺序，不是质量降级：被选中的系统按正式版标准完整处理。当前开发应按纵向链路推进：

```text
设计规则 -> 配置结构 -> 运行行为 -> UI/表现 -> 验证 -> 文档同步
```

## 当前顶层目标

`版本规划/13_正式版全局体验总线与开放节奏.md`

`版本规划/09_正式版核心纵切开发路线.md` 继续作为开发宏观路线；`版本规划/11_纵切批次与需求文档承接矩阵.md` 继续作为需求文档承接门禁。

## 当前版本规划入口

`版本规划/README.md`

## 当前优先级

1. 游戏导演 / 制作人侧已完成首批 Owner 启动判断：`13` 已从 8-12 小时候选主循环扩展为全系统体验归位，并补充全游戏模块宏观设计矩阵；现有 `GDD_01` 到 `GDD_12` 均保留，不砍正式系统；8-12 小时只作为候选主循环稳定门槛，不作为完整游戏范围上限。
2. 后续具体执行进入任务 Owner 纵切启动阶段：一个 agent 对一个玩家结果负责到底，覆盖规则 / 配置 / 程序 / UI / 美术接入 / 验证 / 状态回写；首批从 `T0-01 -> T0-02 -> T0-03` 核对 / 派发首次循环，并必须引用 `13` 的模块宏观设计行，职能 agent 默认作为主策、主程、主美验收，不再作为常规分包承接者。
3. C1-C3 配置程序支持已先收口：策划前三层配置源已完成，程序侧已完成当前可做的同步、静态审计和 P0 入口接入；后续只在 Unity runtime 补跑或人工体验发现真实阻断时按 bug 修复追加处理。
4. 继续围绕“玩家正常 UI 可达 + 调用真实领域服务 + 状态真实变化 + smoke / P0 证据”补主流程闭环；不再按程序 / 策划 / 美术横向拆任务，而按玩家结果拆任务 Owner 包。
5. UI / 美术接入跟随可玩闭环：有 Approved 资源和 active UI 规格时一起接入；ArtAcceptance、debug preview、只读快照或静态面板不能替代真实玩家结果。
6. 第四 / 五层、高层订单链、完整势力线、完整房间 / 日记 / 纪念物、完整好感交互和剧本事件队列已经纳入全游戏宏观大纲；实现上按玩家时长段逐步开放深度，不能因为宏观保留就一次性横向铺量。

当前程序正式版本完成口径：以 `agent_status/program.md`、开发文档、代码事实和验收证据为准。只读数据层、ArtAcceptance 截图、debug 入口或静态面板不能直接标记为可玩完成；服务存在但正常 UI 不可达，仍属于“功能未开放完”。本轮程序推进的执行顺序是 `CFG-01..02 -> FLOW-01..07 -> ARCH-01..05 -> REVIEW-01`。

## 职能状态页

- 游戏导演 / 制作人：`agent_status/director.md`
- 美术 / UI：`agent_status/art.md`
- 策划 / 数值：`agent_status/design.md`
- 程序 / Unity：`agent_status/program.md`
- 旧 PM 兼容入口：`agent_status/pm.md`

## 最近完成

本节只保留最近对全局阶段、跨职能交接或下一步总优先级有影响的摘要；详细执行记录回到对应职能状态页和事实文档。

- 游戏导演 / 制作人侧已在 `版本规划/13_正式版全局体验总线与开放节奏.md` 新增 `全游戏模块宏观设计矩阵`，并压缩重复的深渊五层表和 8-12 小时五幕表；`版本规划/09_正式版核心纵切开发路线.md` 已同步说明后续 Owner 包必须引用时长段、包 ID 和模块宏观设计行。
- 游戏导演 / 制作人侧已确认宏观设计足够支撑首批 Owner 开工：`T0-01 -> T0-02 -> T0-03` 进入启动队列；12 小时以后内容细案、完整美术风格包、完整数值曲线和全层深渊细案改为并行补强，不作为首批开工阻塞。
- 美术全局方向已纠偏：正式主轴为日系二次元地底奇幻冒险 + 低信息密度；黄铜 / 铜件 / 暖灯 / 蒸汽朋克不再作为全局风格关键词，也不作为 active UI 或正向素材提示词的默认材料 / 灯光方向。
- 0-12 小时候选主循环已在 `13` 拆成 `T0-01..T4-02` Owner 任务包矩阵；后续不按程序 / 策划 / 美术横切分包，而按玩家结果纵切派发。
- 美术 / UI 侧已直接接管 FormalV2 运行时视觉 / 布局精修，并确认纯 UGUI 表现层 polish 默认由美术侧闭环：最新 ArtAcceptance `RunID=20260613_020257` 工具层 `PASSED`、21/21 captured、Registry 278、`MissingRequiredVisualIDs=0`、`RealGameplay=21`、`FormalV1Template=0`；当前 FormalV2 runtime UI visual seal 通过纵切基线，后续共享子面板差异化、动画 / VFX 和小尺寸可读性属于美术 / UI 质量迭代。
- 策划 / 配置侧 C1-C3 前三层配置源已标记为 `配置完成` 并完成当前可做的同步 / 静态审计；后续只按真实缺口补齐、验收修复、字段迁移或数值校准处理，不重复派发基础配置。
- `p3-mission` 已收敛为当前唯一长期任务恢复协议；旧 PM / 版本规划路由、旧 `12` 长期节点页和 `misc/Missions` 只保留兼容或归档说明，不再作为 active 派发入口。

历史完成细项按职能追溯：游戏导演看 `agent_status/director.md`，美术 / UI 看 `agent_status/art.md`，策划 / 配置看 `agent_status/design.md` 与 `设计文档/config/26_正式配置设计与填充推进计划.md`，程序 / Unity 看 `agent_status/program.md`，知识库和旧入口迁移看 `DOCS_INDEX.md` 与 `版本规划/README.md`。
## 跨职能交接

本节只保留当前仍影响派发和验收的全局交接规则；具体执行细节回到对应职能状态页。

- 游戏导演 / 制作人维护 `13` 体验总线、`09` 开发路线、`PROJECT_STATUS.md` 和任务 Owner 工作流；变更阶段、开放节奏、优先级、里程碑或跨职能交接时同步目标状态页。
- 下一批实际执行按玩家结果派发任务 Owner，不按程序 / 策划 / 美术横切分包。Owner 包必须标注服务时长段、包 ID、`13` 模块宏观设计行、事实来源、允许改动范围、主职能验收和验证证据。
- 涉及美术 / UI 的 Owner 包必须按 `美术文档/04_美术风格基准.md` 和 Formal V2 当前规则验收：日系二次元地底奇幻冒险是主轴，不能把黄铜暖灯、棕金机械 UI 或蒸汽朋克当作默认世界气质或生图提示词基础。
- 美术 / UI 与程序侧统一使用纯 UGUI；程序接入只以 `美术文档/ui_design/screen_layouts.json` 当前 active 规格为准，draft 和 `versions/` 不作为程序接入口。
- FormalV2 当前工具层 ArtAcceptance 通过，核心 P0 UI 结构和当前纵切 UI 基线通过；后续视觉 / 布局精修由美术 / UI 侧直接处理，程序侧只在领域服务、Unity 工程约束、自动验收工具或 UGUI 底层能力出现阻断时介入，资源登记和批量补图不重开。
- 策划与程序做运行时验证前应先同步配置：`./tools/config/Sync-Configs.ps1 -Clean`；配置事实来源是 `配置表(JSON)`，不要手写维护 `UnityClient/Assets/StreamingAssets/Configs`。
- `11` 不记录实现进度；实现状态、证据入口和验收状态统一回到对应职能状态页和事实文档。字段说明、README、审计、任务拆分或 JSON 修改前设计不等于配置源完成。
- 长任务、恢复继续、跨会话拆分和 mission 请求统一使用 `.codex/skills/p3-mission/`；旧 `misc/Missions` 和旧 PM 路由只保留兼容 / 归档说明。
- 任意智能体新增或调整文档关联时，必须维护 `related` 双向互链，并运行 `./tools/docs/Validate-Docs.ps1`。
## 问题 / 阻塞

- 当前工作区已有前序 UI、美术、生成物和 submodule 相关脏文件。后续智能体开工前应先运行 `.\tools\agent\Invoke-AgentHealthCheck.ps1`，提交时严格收窄范围。
- `CombatLootDropTest.Run` 旧精英保底断言已按正式 Boss 保底口径修正；后续若 Unity runtime 补跑发现新阻断，再按程序 bug 追加处理。
- `tools/ComfyUI_NAIDGenerator/` 当前未跟踪，后续需要决定它是 vendor 代码、submodule，还是本地专用工具。
- `tools/ai-image-gateway` 子模块内部有未提交改动；如需处理，应进入子模块内部单独处理。

## 下一步总建议

1. 下一步开始首批 Owner：`T0-01 开局人偶状态到首次下潜许可`、`T0-02 首次下潜到第一笔战利品`、`T0-03 回城出售到维护 / 休整闭环`。如果已有完整证据，只派真实缺口补强、验收补强或运行时阻断修复。
2. 下一批实际执行不再按程序 / 策划 / 美术横向分包，改按具体时长段内的玩家结果派发任务 Owner；每个包必须写清服务哪个时长段、对应 `T0-01..T4-02` 或后续包 ID、引用 `13` 哪条模块宏观设计行、玩家结果、范围外、事实来源、允许改动范围、主职能验收和验证证据。
3. 程序侧已完成 `FLOW-01..FLOW-07` 一轮补强和 `ARCH-01..ARCH-05` 当前收口；后续只在 Unity runtime / 人工体验发现正常 UI 不可达、按钮未调用真实领域服务或状态未回写时，按真实缺口追加 bug 修复或验收补强。
4. UI / 美术侧已接管 FormalV2 运行时视觉 / 布局精修，并完成 ArtAcceptance `20260613_020257` 复验。本轮资源 / Registry / 工具门禁和当前纵切 UI 基线通过；下一步继续由美术 / UI 侧直接做共享子面板差异化、背景候选、动效 / VFX 和小尺寸可读性复查；程序侧只在真实领域服务、自动验收工具或 Unity 工程阻断时介入。任务 Owner 接入时仍必须同时满足玩家主流程入口和真实领域服务操作闭环，不能只做截图覆盖。
5. 策划 / 配置侧继续推进 C4 及后续配置；C1-C3 已配置完成并完成当前程序侧可做审计，后续只按真实缺口补齐、验收修复、字段迁移或数值校准。

