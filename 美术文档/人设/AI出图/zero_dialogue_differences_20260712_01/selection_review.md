---
id: art_character_zero_dialogue_differences_20260712_review
title: 零号高频对话差分Gemini首批筛选记录
type: art
role: 美术
domain: character_dialogue_portrait_review
status: active
source_of_truth: false
related:
  - 美术文档/人设/05_零号立绘素材设计与交付清单.md
last_verified: 2026-07-12
update_rule: 本批新增差分、改变采用版本、补齐低SAN、进入透明清理或改变入库判断时更新本记录。
---

# 零号高频对话差分 Gemini 首批筛选记录

## 1. 批次范围

- Provider：`gemini_chat_image`
- Model：`gemini-3.1-flash-image`
- Reference：`zero_portrait_p0_20260711_01/selected/zero_dialogue_neutral_gemini_v2_crop.png`
- 方法：每个差分一次独立 `image_to_image` 请求，`count=1`，串行执行。
- 当前阶段：白底 selected candidate，不是透明母图、Approved、Manifest、Registry 或运行时验收素材。

## 2. 当前采用集合

| AssetID | 文件 | 当前判断 |
|---|---|---|
| `zero_dialogue_neutral` | `selected/zero_dialogue_neutral.png` | 对话母版基准。 |
| `zero_dialogue_talk_small` | `selected/zero_dialogue_talk_small.png` | 小开口与低位手势成立，可用于短句对白。 |
| `zero_dialogue_command_ready` | `selected/zero_dialogue_command_ready.png` | 手收披肩、姿态收紧成立，可用于接受指令。 |
| `zero_dialogue_confused` | `selected/zero_dialogue_confused.png` | 掌心上翻与轻微开口成立，手势略大但可辨识。 |
| `zero_dialogue_thoughtful` | `selected/zero_dialogue_thoughtful.png` | 手靠近锁骨与闭口停顿成立，与 command-ready 接近，后续需拉开头部角度。 |
| `zero_trust_soft` | `selected/zero_trust_soft.png` | 轻微笑意与开放手势成立，双手指形需要清理。 |
| `zero_cold` | `selected/zero_cold.png` | 手臂收后与紧嘴成立，但与 neutral 差异偏弱，后续需加强疏离朝向。 |
| `zero_tired` | `selected/zero_tired.png` | 低头、垂肩、低位合手成立，可直接表达疲惫。 |

统一对照图：`selected/contact_sheet_zero_dialogue_differences.jpg`。

## 3. 失败与限制

`zero_low_san` 共调用三次：

1. 原始 `1024x1536` 参考图与完整提示词。
2. 原始参考图与完整提示词原样重试。
3. `512x768` JPEG 参考图与缩短提示词。

三次都在约 125 秒返回 `HTTP 524: A timeout occurred`，没有产生图片。该结果说明当前阻塞发生在 Gemini 中转链路，不足以判断低 SAN 设计方向是否可生成。本批状态为 `validation_limited:gemini_524`，后续应在通道恢复后单图补跑，不继续无上限重试。

## 4. 预处理说明

Gemini 成功输出均为 `1376x768` 横图。selected 版本采用中心 `512x768` 区域裁切，再以 Lanczos 放大到 `1024x1536`。此处理只用于统一评审画布，没有透明化、同脸修正、手部修复或局部重绘。

## 5. 后续门禁

1. 统一八张的头部尺寸、脸型、披肩下摆和脚部比例。
2. 优先修复 `trust_soft` 手指与 `cold` 辨识度。
3. Gemini 通道稳定后补跑 `zero_low_san`；随后补 `zero_hurt` 与 `zero_alert`。
4. 选择一套稳定差分后再做透明底和嘴型 / 手势分层，不从当前 JPEG 衍生图直接拆 DollPuppet。
5. 进入 Approved 前必须完成实际对话 UI 容器缩放检查和主美人工验收。
