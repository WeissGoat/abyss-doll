---
id: art_character_zero_portrait_p0_20260711_review
title: 零号P0身份锚点Gemini首批筛选记录
type: art
role: 美术
domain: character_portrait_generation_review
status: active
source_of_truth: false
related:
  - 美术文档/人设/05_零号立绘素材设计与交付清单.md
last_verified: 2026-07-11
update_rule: 本批新增候选、改变采用版本、进入透明清理或改变入库判断时更新本记录。
---

# 零号 P0 身份锚点 Gemini 首批筛选记录

## 1. 批次范围

- Provider：`gemini_chat_image`
- Model：`gemini-3.1-flash-image`
- 工作方式：使用既有 Gemini 动作候选做单图参考，逐张图生图并人工复核。
- 输出范围：正面常态、3/4 对话、维护坐姿三张身份锚点。
- 当前阶段：`selected candidate`，不是 Approved、Manifest、Registry、DollPuppet 或运行时验收素材。

## 2. 采用版本

| AssetID | 采用文件 | 结论 |
|---|---|---|
| `zero_stand_neutral` | `selected/zero_stand_neutral_gemini_v2_crop.png` | 采用。第二轮去除胸前系结，补长散发，披肩与内衬裙可分。 |
| `zero_dialogue_neutral` | `selected/zero_dialogue_neutral_gemini_v2_crop.png` | 采用。第二轮去除披肩长侧片，恢复短披肩加独立内衬裙。 |
| `zero_maintenance_sit` | `selected/zero_maintenance_sit_gemini_v3_crop.png` | 采用。第二轮修正为双膝并拢正向坐姿，第三轮去除腮红并收短披肩。 |

对照图：`selected/contact_sheet_zero_portrait_p0_anchor.jpg`。

## 3. 淘汰原因

| 输出 | 淘汰原因 |
|---|---|
| `zero_stand_neutral/..._00.jpg` | 胸前有系结，发长和关节细节不足。 |
| `zero_dialogue_neutral/..._00.jpg` | 灰披肩向下延伸为外套裙，破坏披肩 / 内衬裙分层。 |
| `zero_maintenance_sit/..._00.jpg` | 侧坐交叠腿、绑发、披肩拖尾，成熟感偏强。 |
| `zero_maintenance_sit_v2/..._00.jpg` | 坐姿已成立，但腮红明显，披肩后侧仍有长尾。 |

## 4. 处理说明

Gemini 当前网关在请求 `1024x1536` 时仍返回横版 `1536x768`。`selected/*.png` 采用中心 `512x768` 区域裁切，再以 Lanczos 放大到 `1024x1536`；没有重绘、透明化或局部修图。因此这些文件只用于方向确认和后续透明母图输入。

## 5. 后续门禁

1. 三张候选统一做同脸、披肩剪裁和脚部比例复核。
2. 生成或清理真正透明的 `1024x1536` 母图，去除白边和 JPEG 痕迹。
3. 在工坊、房间、维护和战斗 HUD 实际容器中检查缩放可读性。
4. 主美确认后才允许拆分眼罩、嘴型、红光、披肩、磨损和核心仓图层。
5. 完成 metadata、contact sheet、人工结论与流水线刷新后，才讨论进入 Approved。
