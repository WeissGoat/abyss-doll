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

## 3. 解决“时间的流逝”：表现队列 (Visual Queue)

由于后端的战斗结算（比如一刀砍死怪物）在毫秒内瞬间完成，而前端播放动画需要时间。必须引入 **“表现队列 (Command Pattern)”** 来防止数据错乱或动画鬼畜。

1. **后端瞬间结算：** `EnemyFaction` 执行完毕，玩家扣除 50 血。
2. **入队不执行：** 后端抛出的不再是直接的更新事件，而是将其包装为视觉指令 `VisualCommand` 塞入队列。
    *   `[播放怪物攻击动画]`
    *   `[等待 0.5 秒]`
    *   `[播放玩家受击特效, 飘字 -50]`
3. **前端异步播放：** 场景中的 `VisualPlayer` 协程按顺序从队列里拿出指令执行，播完上一个再播下一个。这样即使底层一回合瞬间算完了 10 只怪的攻击，画面上依然是行云流水地依次展现。

## 4. UI 技术选型策略 (AI 辅助降维打击)

为了避免在 Unity 中繁琐的拖拽对齐，同时最大化利用 AI（如 Cursor）的纯文本代码生成能力，我们采用以下混合选型：

*   **常规面板（商店、对话框、属性界面）：全面拥抱 `UI Toolkit`。**
    *   使用类 Web 前端的 `UXML` (结构) 和 `USS` (样式/Flexbox布局)。
    *   **开发流：** 直接让 AI 生成 `UXML/USS` 文本，挂到 `UIDocument` 上瞬间成型。不需要手拖一个像素。
*   **核心玩法（背包网格、不规则物品拖拽）：保留 `UGUI` + 代码批量生成。**
    *   因为 UI Toolkit 做脱离文档流的自由坐标物理碰撞较为痛苦。
    *   **开发流：** 在 Unity 里只做一个极其简单的「基础格子 Prefab」和「物品图标 Prefab」。让 AI 写一个脚本，在游戏启动时用代码 `Instantiate` 出 5x5 的阵列。然后挂载 `IDragHandler` 配合射线检测（Raycast）实现网格吸附与碰撞计算。