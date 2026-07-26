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
  - rules/README.md
  - rules/01_文档维护与新增控制规则.md
  - rules/02_智能体任务路由与完成协议.md
  - 版本规划/README.md
  - 版本规划/13_正式版全局体验总线与开放节奏.md
  - 版本规划/14_0-12小时候选主循环细案设计.md
  - 版本规划/09_正式版核心纵切开发路线.md
  - agent_status/director.md
  - agent_status/pm.md
  - agent_status/program.md
  - agent_status/design.md
  - 设计文档/README.md
  - 设计文档/delivery/00_策划文档开发交付审计.md
  - 设计文档/规则卡/01_局外时间与日程口径规则卡.md
  - 设计文档/规则卡/02_物品背包旋转与生命周期规则卡.md
  - 设计文档/规则卡/03_战斗回合与怪物意图规则卡.md
  - 设计文档/规则卡/04_小镇经济结算与压力链规则卡.md
  - 设计文档/规则卡/05_局外成长与维护规则卡.md
  - 设计文档/规则卡/06_标签与特质规则卡.md
  - 设计文档/规则卡/07_势力声望与订单规则卡.md
  - 设计文档/规则卡/08_人偶核心状态与好感双轨规则卡.md
  - 设计文档/规则卡/09_人偶交互事件与反馈规则卡.md
  - 设计文档/规则卡/10_剧本调度与事件队列规则卡.md
  - 设计文档/规则卡/11_人偶房间布局与视觉叙事规则卡.md
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
last_verified: 2026-07-26
update_rule: 项目阶段、总目标、跨职能交接或阻塞项变化时更新本文件。
---

# 项目状态

> 本页只保留所有 Agent 开工所需的当前项目快照。历史过程回到领域事实文档、验收记录或 Git，不在这里按日期追加。

## 最后更新

2026-07-26

## 当前阶段

正式版核心纵切。纵切代表开发顺序，不代表质量降级；被选中的系统按正式版标准完整处理。

```text
证据审计 -> 策划详细设计 -> 配置源落地 -> UI/UX 与美术设计 / 素材
-> 程序功能与素材接入 -> Owner 自验 -> 专业 Role 外部验收 -> 状态回写
```

## 当前顶层目标

- `13`：全游戏体验总线和全系统宏观大纲。
- `14`：0-12 小时 T0-T4 细案索引与承接规则。
- `09`：正式版纵切宏观路线。
- `11`：批次与详细需求文档门禁，不记录实现进度。

## 当前优先级

1. 按完整玩家结果推进 T0：`T0-01A` 收口开场到首潜许可，随后补 `T0-01B` 浅层遭遇与首件带回物、`T0-01C` 回城照看与下一轮目标。
2. `T0-01A` 已有运行时纵切和程序主链证据，但当前只能声明条件通过；完整 T0、商业化演出、最终 CG / UI / ArtAcceptance 和专业外部验收仍未封板。
3. T1 已有 30 分钟-2 小时设计稿；T2-T4 未完成详细设计前，不作为 Owner 正式实现依据。
4. 策划 C1-C3 配置源和当前程序支持已收口；当前继续 C4 局外成长及后续配置，只按真实缺口补齐、校准或修复。
5. 程序继续围绕正常 UI 可达、真实领域服务、状态真实变化和 Smoke / P0 证据处理缺口，不重复派发已通过的基础链路。
6. 美术生产已统一为数字处理轮次；Zero 的 `doll_zero_dialogue_neutral` 已完成 Approved、Unity 导入和 Registry 登记，其余 13 个静态立绘成员保持 selected，尚未获得 Approved 授权或运行时绑定。
7. 美术 Request Catalog 已完成 v2 PromptRevision 迁移：313 个 Requirement strict 通过，3 个 Pilot 为 `prompt_ready`、其余 310 个保持 `prompt_authoring_required`；`formalv2_standard_stylebatch_20260725_01` 已把 `bg_combat_abyss` 与 `ui_icon_warning` 的旧批量流真实推进到 Approved 同 VisualID 替换和 `registered`，本轮 PromptRevision 迁移本身未生图、未改 Approved / GUID / Registry。

## 跨职能交接

- 游戏导演维护 `13`、`14`、`09` 和全局完成口径；Owner 维护目标模块并按受影响 Role 条件展开。
- 剧情、策划、程序和美术分别维护专业事实；Owner 自验不替代专业 Role 外部验收。
- UI / 美术与程序统一使用纯 UGUI；程序只消费 active `screen_layouts.json`、Manifest 和正式交接入口。
- 配置事实来源是 `配置表(JSON)`；运行时验证前同步配置，不手写维护 StreamingAssets 副本。
- 美术执行交接统一使用 `RequestID + RequirementFingerprint + PromptRevisionID + PromptFormat`；`p3-art-asset-production` 负责编译 Requirement、Agent authoring/发布、准入和 Approved/Unity 交接，`generate-image` 只消费已发布 Variant、确定性序列化并记录 ProviderRequest。
- 长任务、恢复继续和跨会话拆分统一使用 P3 Mission；文档调整先遵守 `rules/01`。

## 问题 / 阻塞

- T0-01A 的漫画页排版、对白皮肤、半开放工坊、首潜许可卡和最终连续截图仍需收口。
- T0-01B / T0-01C 和 T2 详细设计尚未完成。
- Zero neutral 的素材接入链已到 `registered`，当前未完成的是 Prefab / UGUI 运行时消费、容器裁切和美术验收；其余 13 个成员仍在 Approved 授权边界。`registered -> runtime_validated` 的 ArtRun binding/finalize 机制已实现，但尚未对 neutral 执行正式 TargetID 运行时验收。
- 工作区存在并发美术、生成物和 submodule 改动；所有提交必须精确暂存。
- 本轮已完成真实 provider smoke、背景/透明图标批量生成、逐项视觉评审、Approved 同名覆盖与 live Unity Registry 登记；后续扩大批次仍需逐批 smoke、技术门禁和 Agent 视觉评审，不能把 Catalog `ready`、raw 或 `selected` 解释为 Approved / registered / runtime_validated。
- 严格美术生成物聚合校验中，Request Catalog 已通过；离线 Registry candidate 仍报告既有 `changed_existing=19`，不归因于本轮编译迁移，后续需单独收口。

## 下一步总建议

1. 先完成 T0-01A 的 `SEAL-DES / ART / FLOW / UI / VAL`，再新增 B / C 段 Owner 具体设计。
2. B / C 段收口后完善 T2，把出售、账单、维护、传闻 / 基础订单和成长目标组织成下一轮下潜理由。
3. 策划继续 C4 配置源落地；程序和美术只围绕当前 Owner 玩家结果处理真实缺口。
4. 以 Zero neutral 的真实接入证据为模板，按用户授权逐个推进其余 13 个 selected 成员；运行时绑定和 ArtAcceptance 留给后续独立任务。
5. 以已验证的 Request Catalog 和 `formalv2_standard_stylebatch_20260725_01` 为模板，继续扩大标准背景/图标小批次；UI nine-slice 与角色立绘继续使用各自独立 route，运行时绑定和 ArtAcceptance 单独安排。

## 关键入口

- 游戏导演 / Owner：`agent_status/director.md`、`知识库/views/director.md`、`知识库/views/owner.md`
- 剧情：`知识库/views/narrative.md`、`设计文档/剧情/README.md`
- 策划：`agent_status/design.md`、`知识库/views/design.md`
- 程序：`agent_status/program.md`、`知识库/views/program.md`
- 美术：`agent_status/art.md`、`知识库/views/art.md`
- 跨职能协议：`rules/02_智能体任务路由与完成协议.md`
