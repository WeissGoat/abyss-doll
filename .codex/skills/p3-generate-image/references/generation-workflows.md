# 生成与调整工作流

## 新建设计图 / 概念图

1. 读取目标设计和 `美术文档/04_美术风格基准.md`。
2. 当前环境有 Codex `image_gen` 时优先使用。
3. 没有 `image_gen` 时直接改用 `openai_images`。
4. 结果进入探索目录或 `_IncomingAI`；未经筛选和同步不得进入 Approved。

## P3 Asset Contract 文生图

```powershell
.\tools\美术工具\Run-ArtGeneration.ps1 `
  -Config tools/ai-image-gateway/config.local.yaml `
  -Provider openai_images `
  -VisualID <VisualID> `
  -Variants 4 `
  -DelaySeconds 2 `
  -DryRun
```

确认 dry-run 后移除 `-DryRun`。`Variants=4` 表示串行调用四次，每次请求一张。输出 raw 和 generation evidence 后停止，把控制权交还 `p3-art-asset-production`。

## 文件夹图生图 / 差分

编辑：

```text
tools/ai-image-gateway/examples/run_batch_i2i_folder.py
```

设置：

```python
PROVIDER = "gemini_chat_image"
COUNT = 3
DRY_RUN = True
```

确认匹配文件后把 `DRY_RUN` 改为 `False`。每张源图循环 `COUNT` 次，每次只请求一张。

## NovelAI Inpaint

只在以下条件同时成立时使用：

- 有可用源图和 mask。
- 修改区域明确。
- 允许一定结果漂移。
- Gemini 图生图不合适，或需要 NovelAI 二次元风格 / negative tags。

不得把 inpaint 当成像素级可控修图工具。

## 能力完成边界

生成或编辑完成后仅返回 raw 图片、provider/model/prompt/seed/尺寸、参考图或 mask、错误和时间证据。预处理、筛选、Approved、`.meta` / GUID、Registry 和运行时验收由 `p3-art-asset-production` 继续执行。
