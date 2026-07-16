---
id: art_character_design_readme
title: 人设文档入口
type: art
role: 美术
domain: character_design
status: active
source_of_truth: true
related:
  - 美术文档/README.md
  - 美术文档/人设/01_人设参考获取规则.md
  - 美术文档/人设/02_零号原型参考_失明少女.md
  - 美术文档/人设/03_零号初版人设方案.md
  - 美术文档/人设/04_零号AI后端出图提示词对比.md
  - 美术文档/人设/05_零号立绘素材设计与交付清单.md
  - 美术文档/04_美术风格基准.md
  - 美术文档/17_Agent原生动态立绘资产接入规格.md
  - tools/美术工具/README.md
  - agent_status/art.md
last_verified: 2026-07-11
update_rule: 新增或调整人设 Owner 工作流、参考获取规则、角色交付包入口或 DollPuppet 人设承接规则时同步本文件。
---

# 人设文档入口

> 定位：本目录记录 Project P3 美术侧的人设 Owner 工作流、参考获取规则、角色交付包设计入口和后续动态立绘承接规则。生成报告、raw JSON、contact sheet 等流水线产物仍放在对应 `_generated` 或 `_IncomingAI` 目录，不在本目录手写维护。

## 当前入口

| 文档 | 用途 |
|---|---|
| [01_人设参考获取规则.md](01_人设参考获取规则.md) | 记录 Danbooru / 其他二次元角色参考的获取、过滤、归并、中文名补全和报告生成规则。 |
| [02_零号原型参考_失明少女.md](02_零号原型参考_失明少女.md) | 记录用户指定的《漆黑的子弹》失明少女原型资料、外部图片链接、可转译设计点和禁止照搬项。 |
| [03_零号初版人设方案.md](03_零号初版人设方案.md) | 记录用户确认的零号初版外貌与性格基线：白布遮眼、灰披肩、超短内衬裙、裸腿裸足、核心仓隐藏和初始三无。 |
| [04_零号AI后端出图提示词对比.md](04_零号AI后端出图提示词对比.md) | 记录 ChatGPT / Gemini(nanobanana) / NovelAI 三个后端的差异化提示词、工具入口和输出批次。 |
| [05_零号立绘素材设计与交付清单.md](05_零号立绘素材设计与交付清单.md) | 以 Gemini 版本为视觉母版，定义静态立绘、状态差分、特殊 cut-in、DollPuppet 分层和生产优先级。 |

## 当前角色重点

首个角色交付包目标是 `doll_proto_0 / 原型机·零 / 零号`。

当前初版方向已收束为：外貌参考失明少女的遮眼、披肩、轻薄脆弱轮廓，但性格不参考原角色；零号初始为三无 / 冷感，接近 2B 式克制。视觉基线为白布遮眼、灰披肩、简单超短内衬裙、裸腿裸足、轻微灰尘 / 磨损 / 细小人偶关节，核心仓平时被披肩遮住，仅维护或演出时可见。

2026-07-11 起，后续立绘素材以 Gemini 版本为视觉母版：Gemini 锁身体比例、长发、白布、灰披肩和动作轮廓；ChatGPT 只补披肩体块与概念完成度；NovelAI 只作结构备选。具体生产清单以 `05` 为准。

## 生成物入口

当前 Danbooru 参考榜单生成物位于：

```text
美术文档/_generated/danbooru_character_reference/
  zero_doll_reference_report.md
  zero_doll_reference_raw.json
```

这些文件是脚本输出，只作为参考证据和复查材料，不等于人设定稿、Approved 素材、Manifest 条目、DollPuppet 包或运行时验收通过。
