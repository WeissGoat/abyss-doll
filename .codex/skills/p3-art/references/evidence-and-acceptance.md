# Raw 生成证据

## 工作区

P3 上游生产工作流通过 Manifest `ProductionProfile` Resolver 指定 raw 工作区。本 Skill 不自行推断 Profile：

```text
UnityClient/Assets/Art/_IncomingAI/<profile-directory>/<VisualID>/
  raw/
  manifest_snapshot.json
  generation.json
```

`<profile-directory>` 当前为 `standard_assets` 或 `character_portraits`。它只表达生产 Profile，不限制文生图、图生图、inpaint、导入或其他当前 / 未来图片能力。

探索性人设、三后端对比或专项试验可使用任务文档指定目录，但必须明确“不进入 Approved / Manifest / Registry”。

## generation.json 最低证据

记录：

- provider、model、capability。
- prompt 格式和 negative prompt。
- 参考图 / mask 来源。
- 请求次数、每次 `count=1`、seed、尺寸和输出格式。
- 每张输出路径、实际尺寸、错误和时间。

正式证据必须包含 `EvidenceMode=formal_v2`、`RequirementSnapshot`、`PromptRevisionID`、`PromptRevisionFingerprint`、`PromptRevisionSnapshot`、`PromptFormat` 和精确 `ProviderRequest`。当前生成器只接受发布的 PromptRevision；历史 evidence 保留用于审计，不可作为执行输入。

## 能力状态声明

| 证据 | 最多可声明 |
|---|---|
| provider 请求与错误记录 | 请求已执行 |
| 可解码 raw 图片 + generation.json | 图片候选生成 / 编辑完成 |

本 Skill 不声明 processed、selected、Approved、registered、runtime validated 或 player-path complete。

## 安全边界

- 不在日志、文档、状态页或提交中写真实 token。
- `config.local.yaml` 和 smoke 输出保持忽略。
- `_IncomingAI` 默认不提交。
- 图片含随机文字、水印、签名、错误角色数量或身份漂移时，应如实返回给上游评估，不能由本 Skill 推进正式状态。
