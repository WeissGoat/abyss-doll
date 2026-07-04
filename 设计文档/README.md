---
id: design_docs_readme
title: 设计文档阅读入口
type: entry
role: 策划
domain: design_delivery
status: active
source_of_truth: true
related:
  - PROJECT_STATUS.md
  - rules/01_文档维护与新增控制规则.md
  - agent_status/design.md
  - 知识库/views/design.md
  - 版本规划/09_正式版核心纵切开发路线.md
  - 版本规划/11_纵切批次与需求文档承接矩阵.md
  - 设计文档/GDD/GDD_00_系统关联总图.md
  - 设计文档/参考/README.md
  - 设计文档/参考/01_This_Is_the_Police_演出参考.md
  - 设计文档/剧情/README.md
  - 设计文档/剧情/00_剧情大纲.md
  - 设计文档/剧情/01_序章演出与对话节奏.md
  - 设计文档/delivery/00_策划文档开发交付审计.md
  - 设计文档/delivery/12_策划交付落地矩阵.md
  - 设计文档/content_packs/18_正式版内容生产规格.md
  - 设计文档/config/26_正式配置设计与填充推进计划.md
  - 设计文档/config/audits/27_Items正式配置承接审计.md
  - 设计文档/config/audits/28_Monsters正式配置承接审计.md
  - 设计文档/config/audits/29_Dungeons正式配置承接审计.md
  - 设计文档/config/audits/30_Rewards正式配置承接审计.md
  - 设计文档/config/audits/36_局外成长正式配置承接审计.md
  - 设计文档/config/audits/46_经济压力正式配置承接审计.md
  - 设计文档/config/designs/31_第一层正式配置落地设计.md
  - 设计文档/config/designs/32_第二层正式配置落地设计.md
  - 设计文档/config/designs/33_第三层正式配置落地设计.md
  - 设计文档/config/designs/37_局外成长正式配置落地设计.md
  - 设计文档/config/designs/40_局外成长底盘正式配置落地设计.md
  - 设计文档/config/designs/41_局外成长效果特质正式配置落地设计.md
  - 设计文档/config/designs/42_局外成长义体正式配置落地设计.md
  - 设计文档/config/designs/43_局外成长制造维护正式配置落地设计.md
  - 设计文档/config/designs/44_局外成长人偶特质房间正式配置落地设计.md
  - 设计文档/config/designs/47_经济压力核心正式配置落地设计.md
  - 设计文档/config/designs/48_经济压力传闻正式配置落地设计.md
  - 设计文档/config/designs/49_经济压力势力正式配置落地设计.md
  - 设计文档/config/designs/50_经济压力订单正式配置落地设计.md
  - 设计文档/config/tasks/34_前三层正式配置实现任务拆分.md
  - 设计文档/config/tasks/38_局外成长正式配置实现任务拆分.md
  - 设计文档/config/tasks/52_经济压力正式配置实现任务拆分.md
  - 设计文档/config/validation/35_前三层正式配置Validator与固定Seed验收样例.md
  - 设计文档/config/validation/45_局外成长正式配置Validator与固定验收样例.md
  - 设计文档/config/validation/51_经济压力正式配置Validator与固定验收样例.md
  - 设计文档/config/gates/39_局外成长正式配置ID锁定与冲突检查.md
  - 设计文档/config/gates/53_经济压力正式配置ID锁定与冲突检查.md
  - 设计文档/config/gates/54_经济压力正式配置README字段口径检查.md
  - 设计文档/config/gates/55_经济压力订单ID最终锁定表.md
  - 设计文档/config/gates/56_正式配置源落地准入门禁.md
  - 设计文档/_archive/content_backlog/26_第四层组合压力内容包.md
last_verified: 2026-06-13
update_rule: 调整设计文档目录结构、阅读顺序、文档分层或内容包优先级时同步本文件。
---

# 设计文档阅读入口

> 本文件只说明 `设计文档/` 的目录结构、文档类型和阅读顺序。具体规则仍以对应 GDD、规则卡、内容包、配置设计或验收规格为准。策划业务规则目录为 `设计文档/规则卡/`；项目根目录 `rules/` 专指 agent 必读规则。

## 1. 目录结构

`设计文档/` 按用途分区，不再把所有文件平铺在根目录。

| 目录 | 内容 | 用途 |
|---|---|---|
| `GDD/` | `GDD_00` 到 `GDD_12` | 系统设计母文档，定义体验目标、系统边界、核心循环和跨系统关系。 |
| `规则卡/` | `01` 到 `11` 规则卡 | 可开发裁决层，定义状态机、算法、边界、字段口径和验收样例。 |
| `参考/` | 外部作品、演出方法和体验参照 | 只承接“可以借鉴什么”，不承接本项目正式规则或需求。 |
| `剧情/` | 剧情大纲与后续剧情填充文档 | 叙事母版层，说明主线、世界观、主角 / 零号魔偶、记忆拼图和结局方向。 |
| `delivery/` | `00`、`12` 到 `17` | 策划交付与验收承接层，说明需求如何交给配置、程序、UI / 美术和自动验收。 |
| `content_packs/` | `18` 到 `25` | 内容生产规格和首批正式内容包，说明具体物品、怪物、订单、事件、房间记忆等内容。 |
| `config/` | `26` 以及配置审计、设计、任务、验收、门禁 | 正式配置设计链路，服务 JSON 修改前设计、实现派发和验收准入。 |
| `_archive/` | 归档草案 | 暂不进入当前优先级的旧内容或后续层草案。 |

## 2. 文档类型词典

| 类型 | 不是 | 是 | 典型产出 |
|---|---|---|---|
| GDD | 不是任务拆分，也不是字段表 | 系统设计源头 | 体验目标、核心循环、跨系统关系、设计边界 |
| 规则卡 | 不是新系统设定 | 开发和验收遇到边界时的裁决依据 | 状态机、算法、字段、边界、失败判定 |
| 演出参考 | 不是正式需求书，也不是题材复制清单 | 外部作品的方法论参照 | 节奏、镜头、声音、信息密度、剧情与玩法编排方式 |
| 剧情大纲 | 不是剧本表、对话树或配置完成证明 | 主线叙事和世界观母版 | 核心主题、角色关系、记忆拼图、剧情节奏、结局方向 |
| 交付矩阵 / 承接清单 | 不是玩法正文 | 跨职能交接导航 | 配置字段、表现需求、Validator、验收路径 |
| 内容包 | 不是顶层路线 | 具体内容填充资料 | 物品、怪物、订单、事件、房间记忆、联合验收路径 |
| 配置承接审计 | 不是“只列缺字段”，也不是配置完成 | 配置源现状与正式设计之间的缺口审计 | 字段缺口、内容缺口、ID / 引用缺口、README / Validator / 验收缺口 |
| 正式配置落地设计 | 不是已修改 JSON | JSON 修改前配置设计清单 | 目标 ID、字段和值域、跨表引用、内容职责、验收样例 |
| 配置实现任务拆分 | 不是程序进度表 | 配置源实现的派发和证据要求 | 任务顺序、依赖、交付证据、禁止误标完成条件 |
| Validator / 固定样例 | 不是 Validator 程序实现 | 验收规格 | 检查项、固定 seed、失败信号、状态回写证据 |
| 准入门禁 | 不是配置完成证明 | 进入 JSON 修改前的检查标准 | 设计准入、实现证据、同步 / 校验 / 回写要求 |

## 3. 配置链路说明

正式配置工作按以下顺序承接：

```text
GDD / 规则卡 / 内容包
  -> 配置承接审计
  -> 正式配置落地设计
  -> 配置实现任务拆分
  -> ID 锁定 / README 字段口径 / 准入门禁
  -> 配置源 JSON 实现
  -> 同步运行时副本
  -> Validator / 固定 seed / 人工复核
  -> agent_status/design.md 回写
```

关键判定：

1. `配置承接审计` 是缺口报告，不等于配置字段已经补完。
2. `正式配置落地设计` 是 JSON 修改前的策划清单，接近“正式配置清单”，但不等于配置源完成。
3. `实现任务拆分`、`ID 锁定`、`README 字段口径检查`、`Validator 样例` 和 `准入门禁` 都只是配置实施证据链的一部分。
4. 只有配置源 JSON 已落地、同步通过、关键引用 error 为 0，并有 Validator / 固定样例 / 人工复核证据，才能在 `agent_status/design.md` 标记 `配置完成`。

## 4. 推荐阅读顺序

### 判断当前开发方向

1. `PROJECT_STATUS.md`
2. `版本规划/09_正式版核心纵切开发路线.md`
3. `版本规划/11_纵切批次与需求文档承接矩阵.md`
4. `agent_status/design.md`
5. 本文件

### 开发一个系统

1. `GDD/` 中对应系统 GDD。
2. `规则卡/` 中对应规则卡。
3. 如涉及叙事包装、开场节奏或演出方式，先读 `参考/` 与 `剧情/` 中对应文档。
4. `delivery/12_策划交付落地矩阵.md`。
5. `delivery/15` / `16` / `17` 中对应承接清单。
6. 已进入批次的 `content_packs/` 内容包。
7. 如涉及配置实现，再进入 `config/` 链路。

### 做剧情或演出设计

1. `剧情/00_剧情大纲.md`。
2. `剧情/01_序章演出与对话节奏.md` 或对应阶段剧情文档。
3. `参考/` 中相关外部作品演出参考。
4. `版本规划/13_正式版全局体验总线与开放节奏.md`。
5. `版本规划/14_0-12小时候选主循环细案设计.md` 与目标时长段细案。

### 新增或补齐一批配置内容

1. `content_packs/18_正式版内容生产规格.md`。
2. `config/26_正式配置设计与填充推进计划.md`。
3. `config/audits/` 中对应配置承接审计。
4. `config/designs/` 中对应 JSON 修改前配置设计。
5. `config/tasks/` 中对应配置实现任务拆分。
6. `config/gates/56_正式配置源落地准入门禁.md`。
7. `config/validation/` 中对应 Validator / 固定样例规格。

## 5. 当前 active 文件地图

### GDD

- `GDD/GDD_00_系统关联总图.md`
- `GDD/GDD_01_背包战斗与局内网格机制.md`
- `GDD/GDD_02_深渊地图遍历与搜打撤抉择.md`
- `GDD/GDD_03_人偶实体对象与好感双轨机制.md`
- `GDD/GDD_04_小镇循环与经济物价波浪模型.md`
- `GDD/GDD_05_剧本调度引擎与世界观封装逻辑.md`
- `GDD/GDD_06_物品系统与物品生命周期.md`
- `GDD/GDD_07_时间日程系统与天数轮转机制.md`
- `GDD/GDD_08_人偶养成子模块全案.md`
- `GDD/GDD_09_标签与特质系统.md`
- `GDD/GDD_10_势力声望与订单系统.md`
- `GDD/GDD_11_人偶房间与视觉叙事系统.md`
- `GDD/GDD_12_人偶交互管理器.md`

### 规则卡

- `规则卡/01_局外时间与日程口径规则卡.md`
- `规则卡/02_物品背包旋转与生命周期规则卡.md`
- `规则卡/03_战斗回合与怪物意图规则卡.md`
- `规则卡/04_小镇经济结算与压力链规则卡.md`
- `规则卡/05_局外成长与维护规则卡.md`
- `规则卡/06_标签与特质规则卡.md`
- `规则卡/07_势力声望与订单规则卡.md`
- `规则卡/08_人偶核心状态与好感双轨规则卡.md`
- `规则卡/09_人偶交互事件与反馈规则卡.md`
- `规则卡/10_剧本调度与事件队列规则卡.md`
- `规则卡/11_人偶房间布局与视觉叙事规则卡.md`

### 交付与内容包

- `参考/README.md`
- `参考/01_This_Is_the_Police_演出参考.md`
- `剧情/README.md`
- `剧情/00_剧情大纲.md`
- `剧情/01_序章演出与对话节奏.md`
- `delivery/00_策划文档开发交付审计.md`
- `delivery/12_策划交付落地矩阵.md`
- `delivery/13_策划跨系统验收场景矩阵.md`
- `delivery/14_策划配置表现验收承接规格.md`
- `delivery/15_P0主干配置表现验收承接清单.md`
- `delivery/16_P1人偶成长情感承接清单.md`
- `delivery/17_P2长期循环叙事承接清单.md`
- `content_packs/18_正式版内容生产规格.md`
- `content_packs/19_第一层正式核心内容包.md`
- `content_packs/20_第二层背包压力内容包.md`
- `content_packs/21_人偶成长修复内容包.md`
- `content_packs/22_小镇经济月租内容包.md`
- `content_packs/23_长期记忆剧情内容包.md`
- `content_packs/24_势力订单声望内容包.md`
- `content_packs/25_第三层路线侵蚀内容包.md`

### 配置链路

- `config/26_正式配置设计与填充推进计划.md`
- `config/audits/27_Items正式配置承接审计.md`
- `config/audits/28_Monsters正式配置承接审计.md`
- `config/audits/29_Dungeons正式配置承接审计.md`
- `config/audits/30_Rewards正式配置承接审计.md`
- `config/audits/36_局外成长正式配置承接审计.md`
- `config/audits/46_经济压力正式配置承接审计.md`
- `config/designs/31_第一层正式配置落地设计.md`
- `config/designs/32_第二层正式配置落地设计.md`
- `config/designs/33_第三层正式配置落地设计.md`
- `config/designs/37_局外成长正式配置落地设计.md`
- `config/designs/40_局外成长底盘正式配置落地设计.md`
- `config/designs/41_局外成长效果特质正式配置落地设计.md`
- `config/designs/42_局外成长义体正式配置落地设计.md`
- `config/designs/43_局外成长制造维护正式配置落地设计.md`
- `config/designs/44_局外成长人偶特质房间正式配置落地设计.md`
- `config/designs/47_经济压力核心正式配置落地设计.md`
- `config/designs/48_经济压力传闻正式配置落地设计.md`
- `config/designs/49_经济压力势力正式配置落地设计.md`
- `config/designs/50_经济压力订单正式配置落地设计.md`
- `config/tasks/34_前三层正式配置实现任务拆分.md`
- `config/tasks/38_局外成长正式配置实现任务拆分.md`
- `config/tasks/52_经济压力正式配置实现任务拆分.md`
- `config/validation/35_前三层正式配置Validator与固定Seed验收样例.md`
- `config/validation/45_局外成长正式配置Validator与固定验收样例.md`
- `config/validation/51_经济压力正式配置Validator与固定验收样例.md`
- `config/gates/39_局外成长正式配置ID锁定与冲突检查.md`
- `config/gates/53_经济压力正式配置ID锁定与冲突检查.md`
- `config/gates/54_经济压力正式配置README字段口径检查.md`
- `config/gates/55_经济压力订单ID最终锁定表.md`
- `config/gates/56_正式配置源落地准入门禁.md`

## 6. 系统需求文档门禁

后续任何系统进入开发、配置、表现或自动验收前，不能只停留在 `09` 路线、状态页、优先级列表或聊天结论中的一句话。策划智能体必须先确认它已经有详细需求文档承接。

详细需求文档至少覆盖：

1. 设计目标和玩家可见体验。
2. 范围外和暂不处理内容。
3. 核心规则、状态机、算法或生成约束。
4. 关键配置字段、枚举、ID 引用和默认值。
5. 跨系统关系和数据流。
6. UI / 美术表现需求。
7. Validator、自动验收或手动验收样例。
8. 完成判定和失败 / 阻塞条件。
