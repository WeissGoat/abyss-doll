---
id: art_archive_readme
title: 美术归档文档
type: art
role: 美术
domain: art_archive
status: active
source_of_truth: true
related:
  - 美术文档/README.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - 美术文档/ui_design/README.md
  - 美术文档/archive/08_Unity运行时美术验收工具需求.md
  - 美术文档/archive/10_美术验收截图优化与真实数据驱动演进方案.md
  - 美术文档/archive/15_FormalV2运行时验收待办清单.md
  - 美术文档/archive/04_零号AI后端出图提示词对比.md
last_verified: 2026-08-02
update_rule: 新增、恢复或移动归档文档时同步本文件。
---

# 美术归档文档

> **定位：** 保存已经被当前流程替代的 MVP 记录和批次交付快照。归档文档只用于追溯，不作为当前规划、程序接入或素材生产入口。

## 当前归档

| 文档 | 归档原因 | 当前替代入口 |
|---|---|---|
| [06_MVP素材接入状态同步.md](06_MVP素材接入状态同步.md) | MVP 阶段素材接入状态，已被 latest 可接入素材清单替代。 | [../_generated/可接入素材清单.md](../_generated/可接入素材清单.md) |
| [07_MVP_UI重新设计同步.md](07_MVP_UI重新设计同步.md) | MVP UI 重设计同步，已被 Formal V1 UI 版本流替代。 | [../ui_design/README.md](../ui_design/README.md) |
| [11_P0_UI骨架接入交付.md](11_P0_UI骨架接入交付.md) | P0 批次交付快照，当前接入以 active JSON 和 handoff 为准。 | [../ui_design/screen_layouts.json](../ui_design/screen_layouts.json)、[../ui_design/_generated/ui_design_handoff.md](../ui_design/_generated/ui_design_handoff.md) |
| [12_P1_UI骨架接入准备.md](12_P1_UI骨架接入准备.md) | P1 批次交付快照，当前接入以 active JSON 和 handoff 为准。 | [../ui_design/screen_layouts.json](../ui_design/screen_layouts.json)、[../ui_design/_generated/ui_design_handoff.md](../ui_design/_generated/ui_design_handoff.md) |
| [08_Unity运行时美术验收工具需求.md](08_Unity运行时美术验收工具需求.md) | 旧 ArtAcceptance 工具需求和交付说明，日常验收已改为 MCP live-first。 | [../../开发文档/19_UnityMCP验收编排层设计.md](../../开发文档/19_UnityMCP验收编排层设计.md)、[../../开发文档/14_Unity运行时美术自动验收方案.md](../../开发文档/14_Unity运行时美术自动验收方案.md) |
| [10_美术验收截图优化与真实数据驱动演进方案.md](10_美术验收截图优化与真实数据驱动演进方案.md) | 已完成的一次性截图真实数据驱动演进方案，旧 Runner 现只作全量回归后端。 | [../../开发文档/19_UnityMCP验收编排层设计.md](../../开发文档/19_UnityMCP验收编排层设计.md) |
| [15_FormalV2运行时验收待办清单.md](15_FormalV2运行时验收待办清单.md) | 2026-06-13 FormalV2 批次门禁快照，继续 active 会形成第二套状态表。 | [../ui_design/ui_iteration_process.md](../ui_design/ui_iteration_process.md)、[../../agent_status/art.md](../../agent_status/art.md) |
| [04_零号AI后端出图提示词对比.md](04_零号AI后端出图提示词对比.md) | 2026-07 的三后端提示词实验与母版筛选记录；机械提示词和批次命令已不再是当前生产入口。 | [../../.codex/skills/p3-generate-image/SKILL.md](../../.codex/skills/p3-generate-image/SKILL.md)、[../人设/05_零号立绘素材设计与交付清单.md](../人设/05_零号立绘素材设计与交付清单.md) |

## 归档规则

1. 当前规划、当前 active UI 规格、当前素材生产流程不能放在 archive。
2. 已被新流程替代的阶段同步、批次交付、旧方案可以归档。
3. 归档文档的 `status` 应为 `historical`，`source_of_truth` 应为 `false`。
4. 如果归档文档仍被当前文档引用，引用文字必须明确“历史参考”。
5. 恢复归档文档为当前流程前，必须先更新 [../README.md](../README.md) 和对应状态页。
