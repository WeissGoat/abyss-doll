---
id: art_agent_native_doll_puppet_spec
title: Agent原生动态立绘资产接入规格
type: art
role: 美术
domain: dynamic_doll_puppet
status: active
source_of_truth: true
related:
  - 美术文档/README.md
  - 美术文档/16_Live2D角色动画资产接入规格.md
  - 开发文档/17_Live2DSpine运行时接入评估.md
  - 开发文档/rules/02_Unity表现层与编辑器构建规范.md
  - 开发文档/rules/03_视觉资源系统程序开发规范.md
  - agent_status/art.md
  - agent_status/program.md
last_verified: 2026-06-14
update_rule: 修改动态立绘主路线、DollPuppet目录结构、JSON契约、Unity接入边界或验收口径时同步本文档。
---

# Agent原生动态立绘资产接入规格

> 定位：本文定义 Project P3 首版正式动态魔偶立绘的 agent-native 制作与接入路线。它以项目自有 `DollPuppet` 资产格式为主线，视觉目标对标 Live2D，但不把首版完成口径绑定到 Cubism / Spine GUI 工具或专有导出文件。

## 1. 结论

首版动态魔偶立绘采用 `Agent-native Doll Puppet first`：

1. 主线：`DollPuppet`，由 agent 生成分层图、rig JSON、motion JSON、expression JSON，Unity 侧 importer / runtime 生成可验收 Prefab。
2. 实验支线：`Spine JSON`，仅在授权、runtime 和兼容性确认后用于验证 agent 生成公开 skeleton JSON 的可行性。
3. 外部兼容支线：`Live2D Cubism` / `Spine Editor`，只在有对应工具或外部 rigger 交付时接入；不要求 agent 直接生成 `.moc3` 或 `.skel`。
4. AI 视频 / 插帧工具只用于短 cut-in、演出参考或动作意图参考，不作为常驻动态立绘运行时资产。

本路线保留现有原则：不继续推进粗糙的 Unity 伪 Live2D 小 rig；首版必须是数据驱动、可版本管理、可自动验收、可 fallback 的正式动态立绘系统。

## 2. 首个试点范围

首个试点仍只做一个魔偶：

```text
DollID: doll_proto_0
DynamicVisualID: doll_proto_0_live2d
ModelKind: DollPuppet
FallbackSpriteID: doll_proto_0_stand
PrimaryScreens:
  - workshop_main
  - doll_room
  - combat_hud
```

`DynamicVisualID` 暂沿用 `doll_proto_0_live2d`，因为程序侧已有 fallback resolver 和验收 runner 合同；但其 `ModelKind` 变更为 `DollPuppet`，不再表示必须是 Cubism `.moc3` 模型。

首批动作集合：

| MotionID | 用途 | 播放方式 |
|---|---|---|
| `idle` | 常规呼吸、轻微重心、头发 / 装饰摆动、核心灯弱脉冲 | 循环 |
| `low_san_idle` | 低 SAN 压力、呼吸变浅、核心灯不稳、轻微抖动 | 循环 |
| `repair_react` | 维护 / 修复完成反馈，核心灯稳定回亮 | 单次 |
| `hit_react` | 战斗受击或护盾冲击反馈 | 单次 |

首批表情集合：

| ExpressionID | 用途 |
|---|---|
| `normal` | 默认 |
| `blink` | 眨眼或自动叠加 |
| `low_san` | 低 SAN / 焦虑 |
| `hurt` | 受击 / 破损 |
| `relaxed` | 维护后放松 |

## 3. 当前不做

为避免扩大范围，本规格明确不做以下内容：

- 不要求导出 Cubism `.moc3`、`.model3.json` 或 Spine `.skel`。
- 不直接生成 24-60 帧常驻待机帧动画。
- 不把 AI 视频、浏览器预览、静态立绘或空 Prefab 当作正式动态立绘通过。
- 不修改当前 FormalV2 ArtAcceptance 必过条件。
- 不修改 `美术文档/ui_design/screen_layouts.json`。
- 不要求首版接入所有正式界面；先用独立验收 runner 证明动态资产，再接目标界面。
- 不让动态立绘 Prefab 保存 HP、SAN、金币、背包、好感等玩法状态。

## 4. 资产目录

### 4.1 工作区

AI 生成、补层、候选筛选和分层清理都发生在 `_IncomingAI` 工作区，不进入 Manifest 或程序交接清单：

```text
UnityClient/Assets/Art/_IncomingAI/DollPuppets/doll_proto_0/
  source/
    concept.png
    base_flat.png
  masks/
  layer_candidates/
  expression_candidates/
  contact_sheet/
  notes.md
  generation.json
```

`generation.json` 必须记录 provider、prompt、seed、mask、候选路径、人工筛选结论和后续修复要求。它只作为制作证据，不代表 Approved 入库。

### 4.2 正式区

只有分层图、rig、motion、expression、fallback 和来源记录齐备后，才允许进入 Approved：

```text
UnityClient/Assets/Art/Approved/DollPuppets/doll_proto_0/
  SourceRefs/
    README.md
  Textures/
    base.png
    layers/
      body.png
      head.png
      hair_back.png
      hair_front.png
      eye_l.png
      eye_r.png
      brow_l.png
      brow_r.png
      mouth.png
      arm_l.png
      arm_r.png
      core_glow.png
      accessory_*.png
  Rig/
    doll_proto_0.rig.json
    doll_proto_0.mesh.json
  Motions/
    idle.motion.json
    low_san_idle.motion.json
    repair_react.motion.json
    hit_react.motion.json
  Expressions/
    normal.expression.json
    blink.expression.json
    low_san.expression.json
    hurt.expression.json
    relaxed.expression.json
  Prefabs/
    DollPuppet_doll_proto_0.prefab
```

`SourceRefs/README.md` 记录源图、生成批次、筛选结论、授权、AI provider、人工修复说明和不可直接复用限制。Unity 运行时不读取该文件。

## 5. DollPuppet JSON契约

### 5.1 `rig.json`

`rig.json` 描述分层结构、父子关系、pivot、绘制顺序和可动画参数。示例字段：

```json
{
  "schema": "p3.doll_puppet.rig.v1",
  "dollId": "doll_proto_0",
  "visualId": "doll_proto_0_live2d",
  "fallbackSpriteId": "doll_proto_0_stand",
  "canvas": { "width": 1024, "height": 1536, "pixelsPerUnit": 100 },
  "layers": [
    {
      "id": "body",
      "path": "Textures/layers/body.png",
      "parent": "root",
      "pivot": [0.5, 0.42],
      "drawOrder": 10,
      "deformMode": "transform"
    },
    {
      "id": "core_glow",
      "path": "Textures/layers/core_glow.png",
      "parent": "body",
      "pivot": [0.5, 0.5],
      "drawOrder": 40,
      "deformMode": "material"
    }
  ],
  "parameters": [
    { "id": "breath", "type": "float", "min": 0, "max": 1, "default": 0 },
    { "id": "san_stress", "type": "float", "min": 0, "max": 1, "default": 0 },
    { "id": "core_glow", "type": "float", "min": 0, "max": 1, "default": 0.4 }
  ]
}
```

首版允许 `deformMode=transform` / `material` / `sprite_skin` 三类。`sprite_skin` 只有在 Unity 2D Animation package 已确认可用时启用；否则不写入首版必过项。

### 5.2 `mesh.json`

`mesh.json` 只描述需要局部形变的层。没有形变需求的层可以省略。首版推荐只对头发、衣摆、核心灯周边使用有限网格，避免过度绑定。

```json
{
  "schema": "p3.doll_puppet.mesh.v1",
  "meshes": [
    {
      "layerId": "hair_front",
      "vertices": [[0, 0], [1, 0], [1, 1], [0, 1]],
      "triangles": [0, 1, 2, 0, 2, 3],
      "weights": []
    }
  ]
}
```

`mesh.json` 是可选增强，不是首版阻塞。若使用 Unity SpriteSkin，权重必须由 importer 或 Unity 侧工具生成并可复现。

### 5.3 `motion.json`

`motion.json` 是可 diff 的曲线动画，不保存玩法状态：

```json
{
  "schema": "p3.doll_puppet.motion.v1",
  "motionId": "idle",
  "loop": true,
  "duration": 3.2,
  "tracks": [
    {
      "target": "layer:body",
      "property": "scaleY",
      "keys": [[0, 1.0], [1.6, 1.015], [3.2, 1.0]]
    },
    {
      "target": "parameter:core_glow",
      "property": "value",
      "keys": [[0, 0.35], [1.6, 0.55], [3.2, 0.35]]
    }
  ]
}
```

支持的首版属性：

- `localX` / `localY`
- `rotationZ`
- `scaleX` / `scaleY`
- `alpha`
- `material.emission`
- `parameter.value`

### 5.4 `expression.json`

`expression.json` 负责层显示、透明度、局部 offset 和参数覆盖：

```json
{
  "schema": "p3.doll_puppet.expression.v1",
  "expressionId": "low_san",
  "overrides": [
    { "target": "layer:eye_l", "property": "localY", "value": -2.0 },
    { "target": "layer:mouth", "property": "alpha", "value": 0.85 },
    { "target": "parameter:san_stress", "property": "value", "value": 0.75 }
  ]
}
```

表情可以与 motion 叠加，但不能触发玩法计算；SAN、受击、维护等只作为表现意图输入。

## 6. AI生产职责

AI 工具可用于：

- 重制或补强 `doll_proto_0` 正式立绘。
- 补全被头发、手臂、衣物遮挡的隐藏区域。
- 生成眼、眉、嘴、破损、低 SAN、relaxed 等表情候选。
- 修复分层边缘、透明像素、断线和光照不一致。
- 为 `repair_react` / `hit_react` 提供 1-3 张关键帧参考。

AI 工具不可用于：

- 直接生成常驻待机帧序列并入库。
- 用视频输出替代可控 rig。
- 把 raw 输出直接同步到 Approved。
- 自动决定角色身份变化或风格大改。

每个 AI 批次必须留下 `generation.json`、contact sheet 和人工筛选结论。

## 7. Unity接入架构

首版复用既有程序底座，但把命名从 `Live2D` 概念逐步抽象到动态立绘：

| 组件 | 职责 |
|---|---|
| `DollDynamicVisualResolver` | 解析 `doll_proto_0_live2d`，优先找动态 Prefab，失败时返回 `doll_proto_0_stand` fallback。 |
| `IDollDynamicPresenter` | 表现层接口，覆盖 idle、low_san_idle、repair_react、hit_react、expression、core_glow、san_stress。 |
| `DollPuppetAsset` | 运行时读取后的 rig / mesh / motion / expression 数据容器。 |
| `DollPuppetImporter` | Editor 侧把 Approved DollPuppet 包转换为 Unity Prefab。 |
| `DollPuppetRuntime` | 管理 layer GameObject、参数、表达式和 motion 叠加。 |
| `DollPuppetAnimator` | 播放 motion curves，处理 loop、单次反馈和回到 idle。 |
| `DollPuppetUGUIBridge` | 使用角色相机 + RenderTexture + UGUI RawImage 显示，保证不阻挡射线。 |

现有 `DollLive2DVisualResolver`、`IDollLive2DPresenter`、`DollLive2DUGUIBridge` 可作为兼容层保留；正式文档和后续接口应逐步迁移到 `DollDynamic*` / `DollPuppet*` 命名，避免把 DollPuppet 误判为 Cubism 资产。

## 8. VisualID与Manifest关系

首版不要求扩展 `VisualAssetRegistry` 数据结构。动态立绘作为 Prefab 注册，静态 fallback 继续作为 Sprite 注册：

```json
{
  "VisualID": "doll_proto_0_live2d",
  "AssetKind": "Prefab",
  "ModelKind": "DollPuppet",
  "DollID": "doll_proto_0",
  "FallbackSpriteID": "doll_proto_0_stand",
  "PrefabPath": "UnityClient/Assets/Art/Approved/DollPuppets/doll_proto_0/Prefabs/DollPuppet_doll_proto_0.prefab",
  "RuntimeRoot": "UnityClient/Assets/Art/Approved/DollPuppets/doll_proto_0",
  "MotionSet": ["idle", "low_san_idle", "repair_react", "hit_react"],
  "ExpressionSet": ["normal", "blink", "low_san", "hurt", "relaxed"]
}
```

在 Manifest 工具支持 Prefab 前，该说明只写在本规格和交接文档中，不强行塞入现有静态 PNG Manifest。

## 9. 验收runner

既有 Live2D 独立验收 runner 应升级为动态立绘验收 runner，或在报告中明确 `ModelKind=DollPuppet`：

```text
UnityClient/Logs/.doll_puppet_acceptance_trigger = RUN_DOLL_PUPPET_ACCEPTANCE

UnityClient/Logs/DollPuppetAcceptance/latest/
  report.json
  notes.txt
  screenshots/
    idle_0s.png
    idle_1s.png
    expression_2s.png
    low_san_idle_3s.png
    repair_react.png
    hit_react.png
    fallback.png
```

报告状态：

| Status | 含义 |
|---|---|
| `PASSED` | DollPuppet Prefab 存在，motion / expression 可播放，fallback 可切换，截图齐备。 |
| `FALLBACK_ONLY` | runner 正常，但只捕获静态 fallback；不能标记动态资产通过。 |
| `FAILED` | Prefab、截图、motion、expression、fallback 或报告输出失败。 |

该 runner 不进入当前 FormalV2 ArtAcceptance 必过项，除非某个正式界面明确切到动态魔偶显示。

## 10. 与Cubism / Spine的关系

### 10.1 Cubism

Cubism 只作为外部导出兼容路线。`.moc3` 由 Cubism Modeler / Editor 输出，不作为 agent 直接生成目标。若后续获得 Cubism 绑定包，可以用同一个 `DynamicVisualID` 替换 ModelKind 或新增 alias，但必须保留 fallback。

### 10.2 Spine JSON

Spine JSON 是可读、可编辑、runtime 可加载的公开格式，可作为实验支线验证：

```text
DollPuppet layers / rig / motions
  -> Spine JSON skeleton
  -> atlas + png
  -> spine-unity runtime smoke
```

进入正式前必须确认 spine-unity 版本、Unity 兼容性、runtime 授权和回滚策略。未确认前不得修改 `UnityClient/Packages/manifest.json`。

### 10.3 AI视频 / 插帧

AI 视频、LivePortrait、ToonCrafter 或同类工具只用于参考或短 cut-in，不作为可组合状态机动态立绘主线。

## 11. 完成口径

首个 DollPuppet 试点完成必须同时满足：

- `doll_proto_0_live2d` Prefab 已入库并可通过 VisualID 获取。
- `doll_proto_0_stand` fallback 存在并可显示。
- Approved DollPuppet 包包含 SourceRefs、Textures、Rig、Motions、Expressions 和 Prefab。
- `idle`、`low_san_idle` 可循环播放。
- `repair_react`、`hit_react` 可单次触发并回到 idle。
- `normal`、`blink`、`low_san`、`hurt`、`relaxed` 可切换。
- 独立 DollPuppet 验收输出多帧截图和 `report.json`。
- 至少一个正式界面可显示动态魔偶，且缺动态资产时回退静态立绘。
- 不改变现有 FormalV2 ArtAcceptance 通过条件。
- Prefab 不保存玩法状态。

## 12. 任务拆分建议

| 编号 | 功能 | 归属 | 完成判定 |
|---|---|---|---|
| DP-GATE-01 | 迁移动态立绘主路线门禁 | 全局 | `16` / `17` / 程序评估 / 状态页口径一致。 |
| DP-ART-01 | 正式立绘与 fallback 源锁定 | 美术 | 有可追溯 base、fallback、generation.json、contact sheet 入口和 provider 限制记录。 |
| DP-ART-02 | 分层与表情候选包 | 美术 | 有真实 masks、layer candidates、expression candidates、contact sheet 和筛选结论。 |
| DP-DATA-01 | DollPuppet JSON 契约 | 程序 | rig / mesh / motion / expression schema 可解析并拒绝玩法状态字段。 |
| DP-ART-03 | Approved DollPuppet 素材包 | 美术 / 程序 | SourceRefs、Textures、Rig、Motions、Expressions 齐备且 JSON 通过校验。 |
| DP-PROG-01 | DollDynamic / DollPuppet 命名兼容 | 程序 | `DollDynamic*` 合同落地，旧 `DollLive2D*` 作为兼容包装保留。 |
| DP-PROG-02 | DollPuppet importer / runtime / prefab | 程序 | Prefab 可由 Approved 包生成，fallback 可显示，motion / expression 可播放。 |
| DP-UI-01 | UGUI 显示桥接升级 | UI程序 | RenderTexture + RawImage 或受控 fallback 容器不阻挡核心按钮，并有正式界面证据。 |
| DP-VAL-01 | DollPuppet 独立验收 runner | 程序 / 美术 | 输出多帧截图和 JSON 报告，不影响 FormalV2 ArtAcceptance。 |
| DP-DOC-01 | 交接收口 | 全局 | VisualID、路径、fallback、验收证据、限制和后续 Spine / Cubism 兼容策略记录完整。 |

## 13. 2026-06-14 当前交接状态

本轮 P3 mission 的可用结论如下：

- 已确认主路线：首版采用 `Agent-native DollPuppet first`，Cubism / Spine 保留为外部兼容或实验支线。
- 已锁定试点 ID：`DollID=doll_proto_0`、`DynamicVisualID=doll_proto_0_live2d`、`FallbackSpriteID=doll_proto_0_stand`、`ModelKind=DollPuppet`。
- 已建立工作区：`UnityClient/Assets/Art/_IncomingAI/DollPuppets/doll_proto_0/`，当前只有静态 `source/base_flat.png`、请求包 README 和 `generation.json`，不等于可入库分层源。
- 已完成程序契约：`DollPuppetJsonContract`、`DollPuppetJsonValidator`、`DollDynamicVisualResolver`、`IDollDynamicPresenter` 和旧 `DollLive2D*` 兼容包装已编译通过。
- 当前主要阻塞：缺真实 masks、分层候选、表情候选、人工清理后的层图、Approved DollPuppet 包、动态 Prefab、Unity UI 运行态证据和 `DollPuppetAcceptance` 多帧截图报告。
- 验收边界：FormalV2 静态 UI / ArtAcceptance 条件不因本试点扩大；静态 fallback、触发文件、编译通过或旧 Live2D runner 都不能标记为 DollPuppet 动态资产通过。

## 14. 风险和控制

| 风险 | 控制方式 |
|---|---|
| 回退成粗糙小 rig | 使用正式 JSON schema、Approved 包、Prefab、runner 和截图验收作为完成门槛。 |
| agent 分层质量不稳定 | 每批保留 contact sheet、人工筛选结论和 SourceRefs，不让 raw 输出入库。 |
| 动态效果不如 Cubism | 首版只承诺 4 个 motion、5 个 expression；复杂物理留给后续 Cubism / Spine 或 SpriteSkin 增强。 |
| 与现有 UI 验收互相污染 | 独立 DollPuppetAcceptance；FormalV2 ArtAcceptance 不新增必过条件。 |
| Spine 授权不清 | Spine JSON 只作为实验支线，授权确认前不导入 runtime。 |
| Prefab 携带玩法状态 | Presenter 只接受状态快照和播放意图，Prefab 不保存领域数据。 |
