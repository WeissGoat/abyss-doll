# 美术文档索引

> **定位：** Project P3 美术生产、AI 素材生成、资源接入与 Manifest 管理的入口。
> **更新时间：** 2026-05-05

## 推荐阅读顺序

1. [00_美术流水线总览.md](00_美术流水线总览.md)：整体流程、职责边界和当前 MVP 优先级。
2. [01_Manifest规范.md](01_Manifest规范.md)：Manifest 字段结构，以及每一步应该填哪些内容。
3. [02_资源规格与接入规范.md](02_资源规格与接入规范.md)：目录、命名、素材规格和 Unity 接入边界。
4. [03_AI生成与筛选规范.md](03_AI生成与筛选规范.md)：AI 出图批次、预处理、筛选和状态回填。
5. [04_美术风格基准.md](04_美术风格基准.md)：地底奇幻冒险 + 蒸汽朋克的视觉基准。
6. [05_AI图片网关接入方案.md](05_AI图片网关接入方案.md)：将 `tools/ai-image-gateway` 接入 Manifest 批量跑图流程的开发方案。

## 机器生成文件

* [_generated/art_manifest.json](_generated/art_manifest.json)：机器可读 Manifest。
* [_generated/视觉资产Manifest.md](_generated/视觉资产Manifest.md)：脚本生成的 Manifest 摘要，方便快速查看。
* [_generated/AI绘图提示词清单.md](_generated/AI绘图提示词清单.md)：脚本补全后的提示词清单，供出图和审阅。

生成命令：

```powershell
.\tools\美术工具\Update-ArtManifest.ps1
.\tools\美术工具\Generate-ArtPrompts.ps1
```

## 外部契约

* [版本规划/01_MVP美术与UI需求清单.md](../版本规划/01_MVP美术与UI需求清单.md)：MVP UI 和交互表现需求。
* [开发文档/09_视觉资源系统程序开发规范.md](../开发文档/09_视觉资源系统程序开发规范.md)：程序侧 `VisualID -> VisualAssetRegistry -> Unity Asset` 契约。
* [tools/美术工具/README.md](../tools/美术工具/README.md)：美术流水线脚本说明。
