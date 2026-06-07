---
id: dev_05_view_event_bus
title: 表现层架构与事件总线 (View & EventBus System)
type: dev
role: 程序
domain: presentation_layer
status: active
source_of_truth: true
related:
  - 开发文档/rules/00_客户端核心架构规范.md
  - 开发文档/rules/13_编程规范与架构约定.md
  - 开发文档/01_核心数据与实体容器(CoreData).md
  - 开发文档/rules/00_Unity表现层与编辑器构建规范.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - 美术文档/ui_design/README.md
  - 美术文档/ui_design/handoff_checklist.md
  - 美术文档/archive/11_P0_UI骨架接入交付.md
  - 美术文档/archive/12_P1_UI骨架接入准备.md
last_verified: 2026-05-23
update_rule: 修改对应程序架构、接口契约、验证流程或 Unity 实现边界时同步本文件。
---

# 表现层架构与事件总线 (View & EventBus System)

> **定位：** 指导程序如何处理“纯C#业务逻辑”与“Unity画面表现”之间的关系。
> **核心原则：** 采用**数据驱动的 MVC + EventBus（事件总线）**。将客户端视为“伪服务端黑盒”与“纯展示前端”的结合体，彻底杜绝逻辑与表现的面条式耦合。

## 1. 唯一引擎入口：GameRoot (Bootstrapper)

业务逻辑（背包、战斗、养成）必须是纯 C# 的 `CoreBackend` 黑盒，绝不继承 `MonoBehaviour`。整个游戏场景中，只有一个入口类负责启动它们，并提供引擎的时间脉搏。

```csharp
using UnityEngine;

public class GameRoot : MonoBehaviour 
{
    // 全局唯一业务黑盒入口
    public static CoreBackend Core { get; private set; }

    private void Awake() 
    {
        // 1. 实例化纯 C# 的后端黑盒
        Core = new CoreBackend();
        // 2. 初始化所有系统模块（无缝解决依赖关系）
        Core.InitAllSystems();
    }

    private void Update() 
    {
        // 3. 将引擎的时间脉搏（DeltaTime）传递给不需要继承 MonoBehaviour 的 C# 系统
        Core.Tick(Time.deltaTime); 
    }
}

// 纯 C# 黑盒总线 (不继承 MonoBehaviour)
public class CoreBackend 
{
    public PlayerProfile Player { get; private set; }
    public CombatSystem Combat { get; private set; }
    public WorkshopSystem Workshop { get; private set; }
    
    public void InitAllSystems() {
        Player = new PlayerProfile();
        // 初始化其他系统...
    }
    
    public void Tick(float dt) {
        Combat?.Tick(dt); // 例如处理某些需要倒计时的局外事物
    }
}
```

## 2. 前后端交互闭环：API 调用与 EventBus

**铁律：** View层（UI脚本）只能“呼叫后端API”和“监听后端事件”，绝不允许 View 直接去修改底层的金币或背包数据。

### A. 前端发起请求 (Request)
UI 按钮被点击时，直接调用 `GameRoot.Core` 暴露的方法。
```csharp
// 挂载在 UI 上的 MonoBehaviour
public class BuyButtonUIView : MonoBehaviour {
    public void OnClickBuy() {
        // 向黑盒发送请求，不自己扣钱！
        bool success = GameRoot.Core.Workshop.RequestCraftProsthetic("pros_01");
        if(!success) {
            PlayErrorShakeAnimation(); // 前端处理失败表现
        }
    }
}
```

### B. 后端处理与事件抛出 (Event Publish)
在纯 C# 的 `WorkshopSystem` 中，瞬间处理完数据，然后通过 `EventBus` 广播事件。
```csharp
// WorkshopSystem.cs (纯C#)
public bool RequestCraftProsthetic(string id) {
    if(CanAfford(...)) {
        DeductCost(...);
        AddProsthetic(id);
        
        // 核心动作：数据落库完毕，向全宇宙广播事件
        EventBus.Publish(new OnGoldChangedEvent(Player.Money));
        EventBus.Publish(new OnProstheticCraftedEvent(id));
        return true;
    }
    return false;
}
```

### C. 前端监听与表现更新 (Event Subscribe)
各种 UI 脚本在 `Start` 时订阅自己关心的事件，在收到事件时再去修改画面。
```csharp
// TopBarUIView.cs (挂载在顶部金币栏的脚本)
private void Start() {
    EventBus.Subscribe<OnGoldChangedEvent>(OnGoldChanged);
}

private void OnGoldChanged(OnGoldChangedEvent e) {
    goldText.text = e.NewGold.ToString();
    PlayCoinDropSound();
}
```

### D. 深渊出发层级选择 UI

小镇中的“出发深渊”不应再直接写死调用 `LoadLayer(1)`。表现层应拆成两步：

1. 点击“出发深渊”打开 `DungeonStartLayerPanel`。
2. 玩家在面板中选择一个已解锁且存在配置的层级。
3. UI 调用 `GameFlowController.DepartToDungeon(selectedLayerID)`，再由后端校验并加载。

推荐最小结构：

* `DungeonStartLayerPanel`：独立弹窗，不嵌在小镇主面板旁边，避免后续层数增加后把小镇 UI 撑长。
* `DungeonStartLayerEntryUI`：单个层级按钮，显示层名、解锁状态、推荐标签和锁定原因。
* `Confirm` 按钮：调用 `DepartToDungeon(selectedLayerID)`；也可以点击条目后立即进入，但 MVP 建议保留确认按钮，方便玩家检查。
* `Close` 按钮：关闭弹窗并回到小镇，不改变任何后端状态。

层级选择面板只读以下信息：

* `ConfigManager.Dungeons`：用于列出存在配置的层级和显示层名。
* `PlayerProfile.HighestUnlockedDungeonLayer`：用于判断按钮可点击或显示锁定。
* `PlayerProfile.LastSelectedDungeonStartLayer`：用于默认高亮上次选择。

表现层边界：

* UI 不直接修改 `HighestUnlockedDungeonLayer`。
* UI 不直接调用 `DungeonManager.LoadLayer()` 绕过权限校验。
* UI 可以显示锁定原因，例如“通过第 1 层后解锁”。
* 如果玩家选择非法层，后端返回失败，UI 只负责提示，不自行兜底加载第 1 层。

事件建议：

* `OnDungeonStartLayerUnlockedEvent`：层级入口解锁时广播，用于弹出“已解锁第 N 层入口”提示或刷新小镇按钮状态。
* `OnDungeonRunStartedEvent`：从小镇正式开始一轮探索时广播，携带 `StartLayerID`，用于关闭小镇界面并打开深渊地图。
* `OnDungeonStartLayerRejectedEvent`：可选事件；当 UI 请求非法层时，用于显示失败原因。MVP 也可以直接用 `bool` 返回值和日志处理。

### E. 背包表现控制器边界

背包网格是高频交互系统，表现层必须有独立边界，不能继续把显示、同步、层级和丢弃表现堆在 `GameFlowController`。

当前统一入口：

* `InventoryInteractionService`：负责拿起、放置、旋转、恢复、暂存丢弃等背包规则请求。
* `InventoryPresentationController`：负责背包 UI 显示/隐藏、格子布局、物品 UI 同步、`InventoryItemLayer`、底盘面板和暂存丢弃 UI 清理。
* `DraggableItemUI` / `GridSlotUI`：只采集输入和展示反馈，真实规则必须调用 `InventoryInteractionService`。
* `GameFlowController`：只传入当前屏幕上下文，例如 Workshop、DungeonMap、Combat、CombatLoot、SafeRoom、Stairs。

协作边界：

* 功能开发 agent 修改背包规则时，优先改 `InventoryInteractionService`、`BackpackGrid`、测试和配置校验。
* UI/美术接入 agent 修改背包显示时，优先改 `InventoryPresentationController`、Prefab、VisualID 和 UGUI 层级。
* 不允许 UI/美术接入为了摆放或拖拽效果直接修改 `BackpackGrid` 真实状态。
* 不允许功能开发为了快速显示结果把物品 UI 生成逻辑塞回 `GameFlowController`。

## 3. 解决“时间的流逝”：表现队列 (Visual Queue)

由于后端的战斗结算（比如一刀砍死怪物）在毫秒内瞬间完成，而前端播放动画需要时间。必须引入 **“表现队列 (Command Pattern)”** 来防止数据错乱或动画鬼畜。

1. **后端瞬间结算：** `EnemyFaction` 执行完毕，玩家扣除 50 血。
2. **入队不执行：** 后端抛出的不再是直接的更新事件，而是将其包装为视觉指令 `VisualCommand` 塞入队列。
    *   `[播放怪物攻击动画]`
    *   `[等待 0.5 秒]`
    *   `[播放玩家受击特效, 飘字 -50]`
3. **前端异步播放：** 场景中的 `VisualPlayer` 协程按顺序从队列里拿出指令执行，播完上一个再播下一个。这样即使底层一回合瞬间算完了 10 只怪的攻击，画面上依然是行云流水地依次展现。

## 4. UI 技术选型策略 (Pure UGUI)

当前客户端表现层统一采用 `UGUI`，不再维护 `UI Toolkit`、`UXML` 或 `USS` 运行时界面。早期混合方案在背包网格、跨画布拖拽和射线检测中暴露出事件拦截风险，后续新增界面应遵循 `开发文档/rules/00_Unity表现层与编辑器构建规范.md` 的纯 UGUI 方案。

*   **常规面板（商店、对话框、属性界面）：使用 UGUI 预制体与控制器脚本。**
    *   面板骨架由编辑器构建脚本或稳定 Prefab 提供，避免手工重复搭建。
    *   文本、按钮、列表项和状态刷新由对应 Controller 监听后端事件后更新。
*   **核心玩法（背包网格、不规则物品拖拽）：使用 UGUI + 代码批量生成。**
    *   背包格、物品图标、深渊节点按钮等可复用元素统一放在 `UnityClient/Assets/Prefabs/` 下。
    *   运行时通过 `Instantiate` 生成格子、节点和条目，挂载 `IDragHandler` / `IPointerHandler` 等 UGUI 事件接口实现交互。

