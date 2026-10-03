---
name: p3-playtest
description: Use when Project P3 needs to see the game actually running - enter Play Mode through Unity MCP, reach a screen by the player path or a debug route, take screenshots, read the console, review what a player would see, run a baseline walkthrough, reproduce a bug on screen, or do the light art check that confirms registered art shows correctly (runtime_validated). Automated test runs belong to p3-program-validation.
---

# P3 Playtest

用 Unity MCP 把游戏跑起来，像玩家一样看画面。用于基线检查、改动后的自测、在画面上复现 bug、确认素材上屏。

实操写法（截图会暂停编辑器、Game View 尺寸、C# 6 限制、点按钮 / 攻击 / 放战利品的代码）见 [mcp-recipes.md](references/mcp-recipes.md)，第一次走查前读一遍。

## 步骤

1. **准备**
   - 读 `mcpforunity://editor/state`：Editor 要在线，不在编译，不在 Play Mode。Editor 没开就停下，报告 `validation_limited:unity_editor_not_running`。
   - 有代码改动时，先 `refresh_unity`（`compile=request`），再用 `read_console` 看编译错误。
   - `manage_scene(action="get_loaded_scenes")`：场景有未保存的改动就先问用户。
   - 证据目录：`UnityClient/Logs/Playtest/<yyyyMMdd_HHmm>_<主题>/`（`Logs/` 不进 git）。
2. **进 Play Mode**：`manage_editor(action="play")`，等 editor state 显示已在运行、可以调用工具。`read_console` 记下进场时已有的报错，作为基线。
3. **到达目标界面**，按优先级：
   - 正常玩家路径：沿真实界面操作。UGUI 按钮可以用 `execute_code` 找到按钮后调用 `onClick.Invoke()`；序章对白点继续按钮推进。
   - 调试路径：调试菜单做好后优先用它。在那之前，用 `execute_code` 调 `GameFlowController.Instance` 的 `EnterWorkshop()`、`EnterDungeonMap()`、`EnterCombat()`、`DepartToDungeon()` 等方法；需要先准备数据的界面（战利品、结算、安全区、阶梯、工坊各面板）照 `UnityClient/Assets/Scripts/ArtAcceptance/ArtAcceptanceCaptureSteps.cs` 的写法准备。
   - 报告里写明走的是哪条路：调试路径只能证明画面能正确显示，不能证明玩家走得到。
4. **截图**：`manage_camera(action="screenshot", capture_source="game_view", output_folder="Logs/Playtest/<目录名>", screenshot_file_name="<界面>.png", include_image=true)`，不要传 `camera`（传了就拍不到 Screen Space Overlay 的 UI）。Game View 用 1920x1080；保存的 PNG 不是这个尺寸时在报告里注明。截图后编辑器可能被暂停，下一步之前先解除暂停。
5. **看图评审**：像第一次玩的玩家一样描述看到了什么——当前该做什么是否一目了然，按钮能不能点，文字有没有溢出或重叠，有没有调试文字、英文占位或原始 ID，素材有没有缺失、拉伸或错位，风格是否统一。和预期对比，列出问题。
6. **再读 console**：新增的 Error / Exception 记进报告。
7. **退出**：`manage_editor(action="stop")`。
8. **报告**：走过的路径、截图路径、按严重程度排列的问题、console 新错误。值得跟进的问题写进 `PROJECT_STATUS.md` 的「已知问题」。

## 美术轻量检查（登记的素材上屏）

确认指定 VisualID 在真实界面上显示正确。通过后可以声明这些 VisualID `runtime_validated`。

1. 绑定检查：`python tools/美术工具/check_art_binding.py --visual-id <id> [--visual-id <id> ...] --out <证据目录>/binding.json`。失败就先修导入或 Registry（`p3-art`）。
2. 按上面第 2–6 步到达用这些素材的界面并截图。界面还不存在时，停下并说明需要程序先接入。
3. 每个 VisualID 检查：位置、比例、裁切、层级正确，压在上面的文字仍然可读。
4. 在证据目录写 `review.md`：界面、到达路径、VisualID、看到了什么、结论（`passed` 或 `failed` 及原因）。console 没有新错误才算通过。
5. 通过后在 `PROJECT_STATUS.md` 相关行记一笔：日期、VisualID、证据目录。

## 硬规则

- 不保存为了到达或检查界面而做的改动：不写场景、Prefab、资源、配置或存档。原因：检查不能改变被检查的游戏。
- 截图保存在证据目录里才算证据；只出现在对话里的图只用于诊断。原因：证据要能在对话之外复查。
- 调试路径到达的界面只能证明显示正确，不能声明"玩家路径可达"。原因：这两件事需要不同的证据。
- 截图通过不代表程序正确，测试通过也不代表画面正确；两种结论分开写。原因：它们检查的是不同的东西。
