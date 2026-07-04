---
id: art_live2d_character_animation_spec
title: Live2D角色动画资产接入规格
type: art
role: 美术
domain: live2d_character_animation
status: active
source_of_truth: true
related:
  - 美术文档/README.md
  - 美术文档/00_美术流水线总览.md
  - 美术文档/02_资源规格与接入规范.md
  - 美术文档/17_Agent原生动态立绘资产接入规格.md
  - 开发文档/rules/02_Unity表现层与编辑器构建规范.md
  - 开发文档/rules/03_视觉资源系统程序开发规范.md
  - 开发文档/17_Live2DSpine运行时接入评估.md
  - agent_status/art.md
  - agent_status/program.md
last_verified: 2026-06-13
update_rule: 修改Live2D/Spine外部导出兼容路线、专有runtime导入门禁或与Agent原生DollPuppet关系时同步本文档。
---

# Live2D角色动画资产接入规格

> 定位：本文定义 Cubism / Spine 外部导出资产进入 Project P3 的兼容接入边界。首版动态魔偶立绘主线已改为 [17_Agent原生动态立绘资产接入规格.md](17_Agent原生动态立绘资产接入规格.md) 的 `DollPuppet` 路线；本文只在项目获得 Cubism / Spine 工具链或外部 rigger 交付时生效，不改变当前 FormalV2 UI、Approved 静态素材、Manifest、Registry 或 ArtAcceptance 的既有门禁。

## 1. 结论

正式魔偶立绘动画不继续使用粗糙的 Unity 伪 Live2D 小 rig。当前路线分层如下：

1. 首版主线采用 `Agent-native DollPuppet`：由 agent 生成分层图、rig JSON、motion JSON、expression JSON，并由 Unity importer / runtime 生成可验收 Prefab。
2. `Spine JSON` 作为实验支线：技术上可由 agent 生成公开 JSON skeleton，但进入正式前必须确认 spine-unity 版本、兼容性和授权。
3. `Live2D Cubism` / `Spine Editor` 作为外部导出兼容支线：只有在对应工具链或外部 rigger 交付 runtime 包时接入。
4. AI 只作为美术生产辅助，用于补层、遮挡区域 inpaint、表情差分、局部修复和短 cut-in 的关键帧候选，不作为常驻待机动画的最终驱动。

本路线的目标不变：让玩家在工坊、魔偶房间、战斗 HUD 左侧和维护反馈中看到同一个魔偶的稳定动态立绘，而不是一组帧间身份漂移的 AI 序列。区别在于，首版不再把完成口径卡在 Cubism `.moc3` 或 Spine `.skel`。

## 2. 当前不做

为避免影响现有美术验收流程，本规格明确排除以下内容：

* 不把 Live2D 加入当前 FormalV2 ArtAcceptance 必过项。
* 不修改 `美术文档/ui_design/screen_layouts.json`。
* 不新增或修改 Approved 静态 PNG、Manifest 条目、VisualAssetRegistry、程序接入清单或 `_generated` 队列。
* 不修改 `UnityClient/Packages/manifest.json` 引入 Cubism / Spine 包。
* 不要求现有 `doll_proto_0_stand` 静态立绘升级为动态模型。
* 不使用 Unity 内部 Transform / Sprite 拼接方案冒充正式 Live2D。
* 不要求 agent 直接生成 Cubism `.moc3` 或 Spine `.skel` 二进制文件。
* 不用 NovelAI / Gemini 直接生成常驻待机帧动画序列。

## 3. 首个试点范围

首个正式试点只做一个魔偶：

```text
DollID: doll_proto_0
DynamicVisualID: doll_proto_0_live2d
FallbackSpriteID: doll_proto_0_stand
PrimaryScreens:
  - workshop_main
  - doll_room
  - combat_hud
```

首批动作集合：

| MotionID | 用途 | 播放方式 |
|---|---|---|
| `idle` | 常规呼吸、轻微重心和头发 / 装饰摆动 | 循环 |
| `low_san_idle` | 低 SAN 压力、呼吸变浅、核心灯不稳 | 循环 |
| `repair_react` | 维护 / 修复完成反馈 | 单次 |
| `hit_react` | 战斗受击或护盾冲击反馈 | 单次 |

首批表情集合：

| ExpressionID | 用途 |
|---|---|
| `normal` | 默认 |
| `blink` | 眨眼或自动叠加 |
| `low_san` | 低 SAN / 焦虑 |
| `hurt` | 受击 / 破损 |
| `relaxed` | 维护后放松 |

## 4. 美术生产流程

正式 Cubism / Spine 资产不从单张平面图直接进入 Unity。美术侧必须先完成可绑定素材：

```text
角色概念定稿
  -> 分层 PSD 或等价分层源文件
  -> 遮挡区域补层
  -> 表情差分
  -> Cubism / Spine 工具链或外部 rigger 绑定
  -> 动作和表情导出
  -> Unity Prefab 封装
  -> 独立 Live2D 验收
```

### 4.1 AI 的允许用途

NovelAI、Gemini inpaint 或其他图像工具只能用于以下辅助步骤：

* 根据正式角色图补全被头发、手臂、衣物或 UI 裁切遮挡的隐藏区域。
* 生成眼睛、眉、嘴、破损、低 SAN 压力等局部表情候选。
* 修复分层后的边缘、透明像素、断线、纹理空洞和光照不一致。
* 为 `repair_react`、`hit_react` 这类短 cut-in 提供 1 到 3 张关键帧参考。

AI 输出必须经过人工筛选和分层清理后才能进入绑定，不得把 raw 输出直接作为 Unity 运行时资产。

### 4.2 AI 的禁止用途

以下用法不作为正式路线：

* 用 AI 直接生成 24 到 60 帧待机序列。
* 用 AI 视频生成替代绑定、物理和表情控制。
* 用逐帧重绘解决眨眼、呼吸、发丝摆动等常驻动作。
* 把局部 inpaint 候选直接同步到 Approved 或 Manifest。

原因是常驻立绘需要身份、线稿、比例、阴影和装饰稳定；AI 帧动画在这些维度上风险较高，更适合短 cut-in 或宣传素材。

## 5. 目录约定

### 5.1 工作区

Live2D 工作区仍属于 `_IncomingAI`，但不进入现有静态图扫描队列：

```text
UnityClient/Assets/Art/_IncomingAI/DollsLive2D/<DollID>/
  source/
    concept.png
    source_layers.psd
  masks/
  inpaint_candidates/
  layered_psd/
  contact_sheet/
  notes.md
  generation.json
```

`generation.json` 记录 provider、prompt、mask、候选路径、筛选结论和人工修复说明。该文件只作为美术制作证据，不代表 Approved 入库。

### 5.2 正式区

只有完成外部绑定、动作、表情和 fallback 后，才允许进入 Cubism / Spine 兼容 Approved 区：

```text
UnityClient/Assets/Art/Approved/DollsLive2D/<DollID>/
  Runtime/
    <DollID>.model3.json
    <DollID>.moc3
    textures/
    motions/
    expressions/
    physics/
  Prefabs/
    DollLive2D_<DollID>.prefab
  SourceRefs/
    README.md
```

`SourceRefs/README.md` 只记录源文件、授权、生成批次和绑定工具版本；Unity 运行时不读取该文件。

## 6. VisualID 与资源契约

首个版本不要求扩展 `VisualAssetRegistry` 数据结构。Live2D 模型先作为 `GameObject Prefab` 注册，程序继续通过现有 `VisualAssetService.GetPrefab(visualID)` 获取。

推荐 ID：

```text
doll_proto_0_live2d
doll_proto_0_stand
```

`doll_proto_0_live2d` 是动态立绘 VisualID，不再强制表示 Cubism 模型。首版 `ModelKind` 以 `DollPuppet` 为准；当后续接入 Cubism / Spine runtime 包时，可以继续使用该 VisualID 或新增兼容 alias，但必须保留 `FallbackSpriteID`。

推荐资产说明字段如下。它可以先写在 Live2D 规格、试点交接文档或未来 Manifest 扩展中；在工具更新前不要求现有 Manifest 支持这些字段。

```json
{
  "VisualID": "doll_proto_0_live2d",
  "AssetKind": "Prefab",
  "ModelKind": "Live2D",
  "DollID": "doll_proto_0",
  "FallbackSpriteID": "doll_proto_0_stand",
  "PrefabPath": "UnityClient/Assets/Art/Approved/DollsLive2D/doll_proto_0/Prefabs/DollLive2D_doll_proto_0.prefab",
  "RuntimeRoot": "UnityClient/Assets/Art/Approved/DollsLive2D/doll_proto_0/Runtime",
  "MotionSet": ["idle", "low_san_idle", "repair_react", "hit_react"],
  "ExpressionSet": ["normal", "blink", "low_san", "hurt", "relaxed"]
}
```

## 7. Unity 接入边界

正式接入时，运行时 UI 仍保持纯 UGUI。Live2D 显示有两种允许方式：

1. 独立角色相机渲染到 `RenderTexture`，UGUI 用 `RawImage` 展示。
2. 在不破坏现有 Canvas、Raycast 和遮罩层级时，直接放置角色 Prefab 到受控表现层。

首个试点优先采用 `RenderTexture + RawImage`，因为它对现有 UGUI 层级、ArtAcceptance 截图和点击射线影响最小。

推荐新增表现层封装：

```csharp
public interface IDollLive2DPresenter
{
    void PlayIdle();
    void PlayLowSanIdle();
    void PlayRepairReact();
    void PlayHitReact();
    void SetExpression(string expressionID);
    void SetCoreGlow(float normalized);
    void SetSanStress(float normalized);
}
```

UI Controller 只传入领域状态和动作意图，不直接写 Cubism 参数、mesh、physics 或 motion 文件名。具体参数映射由 `DollLive2DPresenter` 或同等 Presenter 负责。

## 8. Fallback 规则

每个动态立绘必须有静态 fallback：

* Live2D prefab 缺失时显示 `FallbackSpriteID`。
* Cubism / Spine 包未安装时显示 `FallbackSpriteID`。
* motion 或 expression 缺失时回退 `idle` 和 `normal`，不阻断主流程。
* fallback 仍按现有静态立绘 DisplaySpec 和 ArtAcceptance 规则验收。

这保证 Live2D 资产不会阻断当前可玩 UI，也不会把资源导入问题扩大成玩法阻塞。

## 9. 独立验收门禁

### 9.1 首版 runner 输出

首版独立验收 runner 已按 `L2D-VAL-01` 落到程序侧，入口和输出如下：

```text
UnityClient/Logs/.live2d_acceptance_trigger = RUN_LIVE2D_ACCEPTANCE

UnityClient/Logs/Live2DAcceptance/latest/
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

美术验收读取 `report.json` 时必须区分：

- `PASSED`：动态 Prefab 和可接收 motion / expression intent 的 Presenter 均存在。
- `FALLBACK_ONLY`：runner 正常运行，但只捕获静态 fallback；这不是 Live2D 动态通过。
- `FAILED`：截图、fallback 或报告输出失败。

当前 `doll_proto_0_live2d` 尚无真实 Cubism / Spine 绑定包，因此补跑 runner 的合理预期是 fallback 或受限验证。不得把 `FALLBACK_ONLY`、静态立绘截图、浏览器预览或 Unity 自研伪 rig 作为正式 Live2D 动态资产验收通过。

Live2D 验收是新增的并行门禁，只在某个 Live2D 资产明确进入试点时运行。现有 FormalV2 ArtAcceptance 不因此新增失败条件。

首版验收证据可以是多帧截图，不要求视频：

```text
0s idle
1s idle
2s expression switch
3s low_san_idle
repair_react trigger
hit_react trigger
fallback sprite trigger
```

验收项：

| 类别 | 检查 |
|---|---|
| 资源 | Prefab 可通过 `VisualID` 获取，fallback sprite 存在 |
| 动作 | `idle`、`low_san_idle` 可循环播放 |
| 反馈 | `repair_react`、`hit_react` 可单次触发并回到 idle |
| 表情 | `normal`、`low_san`、`hurt`、`relaxed` 可切换 |
| UI | 不遮挡核心按钮，不破坏 UGUI 射线，不挤压现有布局 |
| 玩法边界 | Prefab 不保存 HP、SAN、金币、好感或背包等玩法状态 |
| 降级 | 缺包、缺 prefab、缺 motion 时显示静态 fallback |

## 10. 需要开发的功能

若后续采用 Cubism / Spine 外部导出路线，需要拆成以下开发项。首版 Agent-native 路线的任务拆分见 `17_Agent原生动态立绘资产接入规格.md`：

| 编号 | 功能 | 归属 | 完成判定 |
|---|---|---|---|
| L2D-ART-01 | `doll_proto_0` 分层源文件、补层、表情差分和绑定规范 | 美术 | 有源文件、contact sheet、人工筛选记录 |
| L2D-ART-02 | Cubism 绑定、motion、expression、physics 导出 | 美术 | Runtime 目录完整，Prefab 可打开 |
| L2D-PROG-01 | Cubism SDK 或 Spine runtime 引入评估 | 程序 | 明确包来源、版本、许可、导入路径和回滚方式 |
| L2D-PROG-02 | `DollLive2DPresenter` 与 fallback 加载 | 程序 | Prefab 可由 VisualID 加载，缺失时显示静态立绘 |
| L2D-PROG-03 | UGUI 显示桥接 | 程序 / UI | `RenderTexture + RawImage` 或受控表现层不破坏现有 UI |
| L2D-PROG-04 | 领域状态到动作 / 表情映射 | 程序 | SAN、维护、受击只传状态意图，不泄露玩法规则到 prefab |
| L2D-VAL-01 | 独立 Live2D 验收 runner | 程序 / 美术 | 输出多帧截图和 JSON 报告，不影响 FormalV2 ArtAcceptance |
| L2D-DOC-01 | 试点交接记录 | 美术 / 程序 | 记录 VisualID、路径、fallback、验收证据和限制 |

## 11. 与现有美术验收的关系

当前 FormalV2 验收继续以以下链路为准：

```text
active screen_layouts.json
  -> Approved 静态素材
  -> VisualAssetRegistry
  -> ArtAcceptance 截图
  -> 人工画面复核
```

Live2D 试点验收独立为：

```text
Live2D source / runtime package
  -> DollLive2D Prefab
  -> VisualID prefab lookup
  -> Live2D multi-frame capture
  -> fallback check
```

两条链路的交汇点只有 `VisualID` 和 Unity Prefab。除非某个界面明确改成动态魔偶显示，否则 Live2D 验收结果不改变该界面的 FormalV2 通过 / 不通过结论。

## 12. 推荐推进顺序

1. 首先按 `17_Agent原生动态立绘资产接入规格.md` 推进 `DollPuppet` 首版，避免卡在 Cubism / Spine GUI 工具。
2. 若后续获得 Cubism / Spine 工具链或外部 rigger 交付，再将同一套分层源和动作意图转为 Cubism / Spine runtime 包。
3. Cubism 路线必须由 Cubism Modeler / Editor 产出 `.moc3`；agent 不直接生成 `.moc3`。
4. Spine 路线优先验证 JSON skeleton；进入正式前确认 spine-unity 授权、版本和 Unity 兼容性。
5. Unity 只接一个动态 Prefab 和一个 fallback sprite，并先跑独立多帧截图验收。

## 13. 风险和控制

| 风险 | 控制方式 |
|---|---|
| AI 帧动画身份漂移 | 不用于常驻待机，只用于短 cut-in 参考 |
| 绑定成本失控 | 首批只做 1 个魔偶、4 个 motion、5 个 expression |
| 影响现有 UI 验收 | 独立 runner，默认不进入 FormalV2 ArtAcceptance |
| Unity 包导入污染工程 | 程序先做包评估，不在规格阶段改 package |
| 缺模型阻断主流程 | 所有动态立绘必须有 fallback sprite |
| Prefab 携带玩法状态 | Presenter 只接受状态快照和播放意图 |
| Cubism / Spine 工具链卡死首版 | 首版采用 Agent-native DollPuppet；Cubism / Spine 只作兼容支线 |

## 14. 完成口径

本文档完成只代表 Cubism / Spine 外部导出兼容边界已收束，不代表已经制作或接入任何 Cubism / Spine 资产。首版动态立绘完成口径以 `17_Agent原生动态立绘资产接入规格.md` 为准。

若后续要求 Cubism / Spine 兼容包正式完成，必须同时满足：

* `doll_proto_0_live2d` Prefab 已入库并可通过 VisualID 获取。
* `doll_proto_0_stand` fallback 存在并可显示。
* 独立 Live2D 验收输出多帧截图和报告。
* 至少一个正式界面能显示动态魔偶，且缺模型时回退静态立绘。
* 没有改变现有 FormalV2 ArtAcceptance 的通过条件。
