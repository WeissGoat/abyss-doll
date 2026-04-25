# Unity 表现层与编辑器构建规范 (UI & Editor Construction Guidelines)

> **定位说明：** 本文档旨在为客户端工程师确立表现层（UI）搭建与场景构建的工程规范。
> **核心开发理念：** 全面摒弃传统的手工预设拼接模式，采用自动化场景生成（Editor Scripting）与数据驱动的运行时 UI 实例化（Runtime Generation），确保客户端结构的高内聚与可复现性。

---

## 1. 场景构建原则 (Scene Construction Principles)

客户端基础场景骨架禁止手动操作 Hierarchy 进行层级拼装，必须通过编写具有幂等性的 C# 编辑器扩展脚本实现一键生成。

*   **自动化骨架构建 (`MVPEditorSetup.cs`)**：
    *   在顶部菜单 `Tools` 中调用自动化生成脚本。
    *   脚本需包含：生成 `[GameRoot]` 单例并挂载核心事件总线与 `FileLogger`。
    *   自动构建核心渲染画布（如 `InventoryCanvas`、HUD），以及带有 `GridLayoutGroup` 约束的网格容器。
    *   所有的配置参数（缩放、边距、锚点）硬编码于构建脚本中，确保环境重构的绝对一致性。

---

## 2. UI 技术栈统一与防干扰 (UI Technology Stack)

由于“背包网格系统”涉及复杂的不规则物理空间碰撞、脱离文档流的绝对定位交互与跨画布拖拽，项目表现层技术栈已在早期迭代中被彻底收束。

*   **全面采用纯 UGUI 架构**：
    *   为避免射线检测（Raycast）被不可预见的事件拦截面板遮挡，项目**已明确废弃 UI Toolkit 方案**。
    *   无论是复杂的战斗 HUD、深渊探索面板还是网格交互，均采用 UGUI 开发。
*   **数据驱动的动态实例化 (Data-Driven Instantiation)**：
    *   场景中不预先放置任何实体格或武器图标。
    *   **网格生成**：`GridGenerator` 需根据后端模型 `ChassisComponent` 提供的二维数组与死格掩码（GridMask），在 `Start()` 阶段动态双层遍历生成 `GridSlotUI`。
    *   **物品生成**：读取后端的 `ItemEntity` 属性，将其投射给实例化的 `DraggableItemUI`，动态赋予长宽尺寸与材质。

---

## 3. 交互逻辑与表现解耦 (Interaction & View Decoupling)

UI 脚本仅负责渲染呈现与捕捉外设输入，严禁承载任何游戏规则判定状态机。

*   **无状态前端 (Stateless View)**：
    *   诸如 `DraggableItemUI` 等表现层脚本禁止自行维护当前坐标合法性。必须将 UI 中心锚点（Pivot）对齐至首个单元格，以避免视觉偏移与坐标计算错位。
*   **请求与响应闭环 (Request-Response Flow)**：
    *   拖拽释放触发 `OnDrop` 时，UI 层仅作为请求发起者：
        ```csharp
        // 伪代码规范
        bool canPlace = CoreBackend.CurrentGrid.CanPlaceItem(draggedItem.Data, X, Y);
        ```
    *   若后端校验通过，UI 执行成功吸附音频播放与坐标位移；若后端驳回，必须触发 `ReturnToOriginalPosition()` 并重新向底层矩阵注册原坐标数据，以防出现状态断层引发的“视觉幽灵物品”。
*   **事件驱动更新 (Event-Driven Updates)**：
    *   AP、HP、SAN 等 HUD 状态变更，只允许通过监听 `GameEventBus` 广播更新。
    *   严禁 UI 脚本在 `Update()` 函数中高频轮询底层数据的脏读。

---

## 4. 表现队列规范 (Visual Queue)

由于后端的“一击致死”、“连击”及“网格触发特效”皆为毫秒级瞬间结算完毕的事务，为防止前端动画播放错乱或指令覆盖，需严格构建表现队列。

*   底层战斗与计算引擎不再直接抛出改变画面的立刻执行指令。
*   底层需向前端投递封装后的 `VisualCommand`（视觉表现指令对象）。
*   场景内的协程或全局管理器按入队顺序（FIFO）提取指令（如播放受击特效、伤害飘字动画），在上一指令的动画回调/延时结束后再执行下一指令。

---

## 5. 测试与启动沙盒 (Bootstrapping & Mock Testing)

*   **`MVPTester.cs`** 作为独立的胶水代码，仅用于研发环境中的闭环链路测试。
*   流程规范要求：在 `GameRoot.Awake` 的 JSON 异步反序列化与核心总线生成完毕后（可通过事件通知或延时调用），再向 `CoreBackend` 发起第一层深渊的加载请求（`LoadLayer`），并初始化玩家初始底盘资源。