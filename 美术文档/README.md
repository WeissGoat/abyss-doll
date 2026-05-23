---
id: art_readme
title: 美术文档索引
type: art
role: 美术
domain: art_pipeline
status: active
source_of_truth: true
related:
  - GEMINI.md
  - 开发文档/14_Unity运行时美术自动验收方案.md
  - 开发文档/09_视觉资源系统程序开发规范.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - 美术文档/00_美术流水线总览.md
  - 美术文档/ui_design/README.md
  - agent_status/art.md
  - tools/美术工具/README.md
  - 知识库/views/art.md
last_verified: 2026-05-23
update_rule: 美术文档结构、推荐阅读顺序或外部契约变化时同步本文件。
---

# 美术文档索引

> **定位：** Project P3 美术生产、AI 素材生成、资源接入与 Manifest 管理的入口。
> **更新时间：** 2026-05-22

## 推荐阅读顺序

0. [../知识库/views/art.md](../知识库/views/art.md)：美术智能体开工导航，只做上下文路由。
1. [10_正式版核心纵切美术路线.md](10_正式版核心纵切美术路线.md)：正式版核心纵切 Alpha 的美术/UI 路线，明确 UI 骨架按正式版设计、素材内容逐步迭代。
2. [00_美术流水线总览.md](00_美术流水线总览.md)：整体流程、职责边界和当前 Alpha 优先级。
3. [ui_design/README.md](ui_design/README.md)：UI 设计系统、组件目录、界面布局和程序交付校验。
4. [01_Manifest规范.md](01_Manifest规范.md)：Manifest 字段结构，以及每一步应该填哪些内容。
5. [02_资源规格与接入规范.md](02_资源规格与接入规范.md)：目录、命名、素材规格和 Unity 接入边界。
6. [03_AI生成与筛选规范.md](03_AI生成与筛选规范.md)：AI 出图批次、预处理、筛选和状态回填。
7. [04_美术风格基准.md](04_美术风格基准.md)：地底奇幻冒险 + 蒸汽朋克的视觉基准。
8. [05_AI图片网关接入方案.md](05_AI图片网关接入方案.md)：将 `tools/ai-image-gateway` 接入 Manifest 批量跑图流程的开发方案。
9. [06_MVP素材接入状态同步.md](06_MVP素材接入状态同步.md)：MVP 阶段素材接入状态，后续作为历史基线和缺口参考。
10. [07_MVP_UI重新设计同步.md](07_MVP_UI重新设计同步.md)：MVP UI 重设计同步，后续作为历史方案参考，不再作为正式 UI 目标。
11. [08_Unity运行时美术验收工具需求.md](08_Unity运行时美术验收工具需求.md)：给程序侧实现运行时截图、UI 状态导出和美术接入验收的工具需求。
12. [09_运行时美术验收记录.md](09_运行时美术验收记录.md)：记录 Unity 运行时截图验收结论、返修项和下一批补素材需求。
13. [11_Alpha_P0_UI骨架接入交付.md](11_Alpha_P0_UI骨架接入交付.md)：P0 UI 骨架交给程序侧接入的本轮执行文档。
14. [12_Alpha_P1_UI骨架接入准备.md](12_Alpha_P1_UI骨架接入准备.md)：P1 UI 骨架草案和后续接入准备。
15. [art_requirements_seed.json](art_requirements_seed.json)：配置表无法扫出的 preset 资产种子，例如 UI 皮肤、背景、程序缺口反馈。

## 机器生成文件

* [_generated/art_manifest.json](_generated/art_manifest.json)：机器可读 Manifest。
* [_generated/视觉资产Manifest.md](_generated/视觉资产Manifest.md)：脚本生成的 Manifest 摘要，方便快速查看。
* [_generated/AI绘图提示词清单.md](_generated/AI绘图提示词清单.md)：脚本补全后的提示词清单，供出图和审阅。
* [ui_design/_generated/ui_design_handoff.md](ui_design/_generated/ui_design_handoff.md)：UI 设计校验后生成的程序交付摘要。

生成命令：

```powershell
.\tools\美术工具\Update-ArtManifest.ps1
.\tools\美术工具\Generate-ArtPrompts.ps1
.\tools\美术工具\Validate-UIDesign.ps1
```

## 外部契约

* [版本规划/09_正式版核心纵切开发路线.md](../版本规划/09_正式版核心纵切开发路线.md)：当前正式版核心纵切 Alpha 的顶层路线，以此为准。
* [版本规划/01_MVP美术与UI需求清单.md](../版本规划/01_MVP美术与UI需求清单.md)：MVP UI 和交互表现需求。
* [开发文档/09_视觉资源系统程序开发规范.md](../开发文档/09_视觉资源系统程序开发规范.md)：程序侧 `VisualID -> VisualAssetRegistry -> Unity Asset` 契约。
* [tools/美术工具/README.md](../tools/美术工具/README.md)：美术流水线脚本说明。
