# Unity MCP 实操写法

2026-10-04 基线走查时验证过的写法。调试菜单做好之前，靠这些在 Play Mode 里推进游戏。

## 环境注意

- **截图会暂停编辑器。** `manage_camera` 截完图后 `EditorApplication.isPaused` 可能变成 `true`，之后游戏不再走帧（`Time.frameCount` 不动）。每次截图后、下一步操作前先解除暂停：
  ```csharp
  if (UnityEditor.EditorApplication.isPaused) UnityEditor.EditorApplication.isPaused = false;
  ```
- **Game View 尺寸。** 用户布局可能是 Free Aspect（例如 754x726），界面会被挤压。走查前切到 Full HD，结束后切回原来的序号：
  ```csharp
  var asm = typeof(UnityEditor.Editor).Assembly;
  var gvType = asm.GetType("UnityEditor.GameView");
  var gv = UnityEditor.EditorWindow.GetWindow(gvType, false, null, false);
  var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic;
  var prop = gvType.GetProperty("selectedSizeIndex", flags);
  // 先读 prop.GetValue(gv, null) 记下原值；Standalone 组里 3 = "Full HD (1920x1080)"
  prop.SetValue(gv, 3, null);
  ```
- **`execute_code` 用 CodeDom 编译（C# 6）。** 不能用本地函数、`out var`、`is` 模式匹配；改用 `System.Func<...>` lambda 和先声明的变量。可以直接引用游戏类型（`GameRoot.Core`、`ItemUseService` 等），不需要反射。
- **序章叙事触发是一次性的。** 同一次 Play Mode 里重放开场不会再触发；要重看就退出 Play Mode 重进。

## 常用片段

查看当前界面和可点按钮（排除对白层）：

```csharp
var canvas = UnityEngine.GameObject.Find("InventoryCanvas").transform;
var overlay = canvas.Find("P3DialogueOverlay_Runtime");
var sb = new System.Text.StringBuilder();
foreach (UnityEngine.Transform t in canvas) if (t.gameObject.activeSelf) sb.AppendLine("panel: " + t.name);
foreach (var b in UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Button>()) {
    if (!b.gameObject.activeInHierarchy || !b.interactable || b.transform.IsChildOf(overlay)) continue;
    var txt = b.GetComponentInChildren<UnityEngine.UI.Text>();
    sb.AppendLine(b.transform.parent.name + "/" + b.name + " '" + (txt ? txt.text.Replace("\n", " ") : "") + "'");
}
return sb.ToString();
```

按文字点按钮（相当于玩家点击）：在上面的循环里找到 `txt.text.Contains("开始下潜")` 的按钮，调用 `b.onClick.Invoke()`。地图节点是 `MapLayout` 下的 `NodeButtonPrefab(Clone)`，文字形如"战斗 中险""营地 低险"。

用武器攻击（相当于"点武器，再点敌人"）：

```csharp
var combat = GameRoot.Core.Combat;
var grid = GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid as BackpackGrid;
ItemEntity weapon = null;
foreach (var it in grid.ContainedItems) if (it.ItemType == "Weapon") weapon = it;
FighterEntity enemy = combat.EnemyFaction.Fighters.Find(f => f.RuntimeHP > 0);
string reason;
ItemUseService.TryUseItem(weapon, out reason);
ItemUseService.TryConfirmPendingTarget(enemy, out reason);
```

盾牌和药剂直接 `ItemUseService.TryUseItem(item, out reason)`。回合结束点"结束回合"按钮；最后一个敌人倒下后也要再点一次，战斗才会结束。

把战利品放进背包（相当于拖放）：掉落物由 `DraggableItemUI` 显示，物品不在 `grid.ContainedItems` 里的就是待拾取的。

```csharp
var ctx = InventoryInteractionContext.FromCurrentDoll("Playtest");
string reason;
bool placed = InventoryInteractionService.RequestPlace(item, x, y, rotation, ctx, out reason);
```

物品形状坐标是 `[x, y]`，`[[0,0],[0,1]]` 是竖着的 1x2。放完点"确认拾取并继续"。

读状态：`GameRoot.Core.CurrentPlayer`（`Money`、`StashInventory`、`ActiveDoll.Status.HP_Current / SAN_Current`、`ActiveDoll.RuntimeGrid`），`GameRoot.Core.Combat.CurrentState`。
