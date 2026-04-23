# MVP Unity 编辑器实操步骤指南 (Editor Workflow)

> **定位说明：** 本文档是写给负责在 Unity 中搭建表现层（UI）的客户端同学的**实操说明书**。
> **核心开发理念：** 彻底抛弃传统的“在 Hierarchy 里拖拽对齐像素”的手工作坊模式。我们将根据不同的 UI 场景需求，**结合 AI 辅助（如 Cursor）**，分类采用最适合的“代码驱动 UI”方案。

本指南将表现层开发明确划分为三种不同的落地流派，请严格对号入座。

---

## 流派一：编辑器自动化生成 (Editor Scripting)
**【适用场景】** 游戏启动时的底层骨架（GameRoot）、重复的预制体（Prefab）基础构建、固定结构的 Canvas 画布等。
**【核心思路】** 把 AI 当作自动化黑客。直接用纯 C# 编写编辑器脚本，一键构建场景。

**实操步骤：**
1. **不要手动新建任何物体。**
2. 在项目目录 `Assets/Scripts/Editor/` 下，我们已经编写了 `MVPEditorSetup.cs` 工具脚本。
3. **点击魔法菜单：** 等待 Unity 编译完成后，在顶部系统菜单栏点击 **`Tools -> 魔偶深渊 一键生成 MVP 场景骨架`**。
4. **见证生成：** 脚本会自动在场景中：
   * 生成 `[GameRoot]` 并挂载核心后端和 `FileLogger`。
   * 生成 `InventoryCanvas` 及其附带的 UGUI 缩放组件。
   * 自动生成背包的 `GridContainer`，并正确设置 `GridLayoutGroup` 的边距和约束。
   * 在后台悄悄生成最基础的 `SlotPrefab` (死格子) 和 `ItemPrefab_TestSword` (测试武器)，并保存到 `Assets/` 根目录下供后续代码使用。
5. **极客红利：** 以后不管是不小心删错了场景，还是想在新工程复现，永远只需点一下按钮，瞬间恢复完美状态。

---

## 流派二：传统 UGUI + 运行时动态实例化 (Runtime Generation)
**【适用场景】** 游戏最核心的玩法——**“背包网格”与“不规则形状物品的拖拽碰撞”**。
**【核心思路】** 这种强空间几何重叠、脱离文档流绝对定位的交互，强行用纯代码布局会极其痛苦。我们保留 UGUI 的物理坐标系，但**用代码在 `Start()` 时批量实例化网格**。

**实操步骤：**
1. **确认基础组件：** 确保刚才流派一生成的 `GridContainer` 挂载了我们写好的 `GridGenerator.cs` 脚本，且预制体插槽已关联。
2. **底层数据驱动生成：** 
   * `GridGenerator` 会读取后端 `ChassisComponent`（底盘数据）的长宽。
   * 用双层 `for` 循环瞬间 `Instantiate` 25 个小方块，并挂载 `GridSlotUI`。
   * 根据配置表的 `GridMask`，自动把不能放东西的死角染成深灰色。
3. **实现鼠标拖拽与射线检测：**
   * 物品上挂载的 `DraggableItemUI` 实现 `IBeginDragHandler` 和 `IDragHandler`。
   * **铁律：拖拽松手时 (`OnDrop`)，UI 绝不自己决定能不能放下！**
   * UI 必须向黑盒发请求：`bool canPlace = GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid.CanPlaceItem(...)`。
   * 只有后端返回 True，前端才播放“咔哒”吸附音效并触发 `EventBus.PublishItemPlaced`。

---

## 流派三：全面拥抱 UI Toolkit (类似 Web 前端)
**【适用场景】** 各种常规面板（血条 HUD、SAN值槽、结束回合按钮、商店列表、结算界面）。
**【核心思路】** 这种规整的数据展示面板，完全摒弃 UGUI。直接让 AI 帮你写类似 HTML/CSS 的 `UXML` 和 `USS`。

**实操步骤：**
1. **呼叫 AI 生成文本：**
   * 对 Cursor 提出需求：“我需要一个 UI Toolkit 的战斗HUD。左侧包含血条（ProgressBar），右下角是一个红色大按钮（名字叫 btn-end-turn）。请用 Flexbox 写 UXML 和 USS。”
2. **挂载运行：** 
   * 将生成的文本放入 Unity。
   * 在 Hierarchy 新建空物体 `HUD_Manager`，挂载 `UIDocument`，将 UXML 拖入，画面瞬间成型，且完美适配任何分辨率。
3. **编写前端粘合剂 (`HUDController.cs`)：**
   * 写一个极简的脚本挂在一起。它只负责**获取节点**和**监听事件总线**。
   ```csharp
   private void OnEnable() {
       var root = GetComponent<UIDocument>().rootVisualElement;
       
       // 1. 获取按钮，绑定 API 请求
       root.Q<Button>("btn-end-turn").clicked += () => {
           GameRoot.Core.Combat.EndPlayerTurn();
       };
       
       // 2. 监听后端的 EventBus 广播，单向修改画面
       GameEventBus.OnAPChanged += (id, current, max) => {
           // 刷新 AP 水晶的高亮数量
           UpdateAPCrystals(current);
       };
   }
   ```

---

## 🏁 终极验收缝合代码 (MVPTester)

这三种流派搭建完毕后，我们需要一条拉起深渊战斗的“导火索”。
请在刚才通过工具生成的 `MVP_Tester` 物体上，检查并运行这最后一段胶水代码：

```csharp
void Start() {
    // 延迟 1 秒，确保后端 JSON 全部读取入内存
    Invoke("StartMockRun", 1.0f);
}

void StartMockRun() {
    Debug.Log("[MVPTester] 开始启动 MVP 闭环测试...");

    // 1. 触发流派二：初始化UI网格
    ChassisComponent myChassis = GameRoot.Core.CurrentPlayer.ActiveDoll.Chassis;
    FindObjectOfType<GridGenerator>().GenerateGrid(myChassis);

    // 2. 在屏幕上生成一把测试用的长剑（DraggableItemUI）
    // 给它灌入从配置表中读出的真实的 ItemEntity 数据...

    // 3. 启动深渊与战斗状态机
    GameRoot.Core.Dungeon.LoadLayer(1); 
    
    // 4. 触发流派三：此时 CombatSystem 会抛出 OnTurnStart 事件，UI Toolkit 监听到后自动亮起 3 颗 AP 水晶。
}
```

当您在编辑器里按下 Play 键，画面瞬间生成完毕，拖动红色的武器放入网格，点击武器，看着怪物扣血、水晶变暗——这意味着，一套具备世界级扩展性的客户端架构，已经在您的手中诞生了！