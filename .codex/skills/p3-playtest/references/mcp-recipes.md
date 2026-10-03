# Unity MCP 实操注意

2026-10-04 走查时踩到的环境问题。游戏内操作用 `P3DevTools`（见 `SKILL.md`），不再手写大段代码。

- **截图会暂停编辑器。** `manage_camera` 截完图后 `EditorApplication.isPaused` 可能变成 `true`，之后游戏不再走帧（`Time.frameCount` 不动）。截图后、下一步操作前先解除暂停：
  ```csharp
  if (UnityEditor.EditorApplication.isPaused) UnityEditor.EditorApplication.isPaused = false;
  ```
- **一次 `execute_code` 只跑在一帧里。** 有些界面（阶梯撤离、结算）下一帧才响应；`P3DevTools.RunDive()` 发现状态没变化会停下并提示再调一次。自己写的连续操作也要分成多次调用。
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
- **`execute_code` 用 CodeDom 编译（C# 6）。** 不能用本地函数、`out var`、`is` 模式匹配；改用 `System.Func<...>` lambda 和先声明的变量。可以直接引用游戏类型（`GameRoot.Core`、`P3DevTools` 等）。
- **序章叙事触发是一次性的。** 同一次 Play Mode 里重放开场不会再触发；要重看就退出 Play Mode 重进。
- **开发者面板。** 按 F1 打开；用 MCP 时可以直接把 `P3DevPanel_Runtime/DevCanvas` 设为激活来截图。
