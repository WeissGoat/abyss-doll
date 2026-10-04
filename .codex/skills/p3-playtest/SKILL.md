---
name: p3-playtest
description: Use when Project P3 needs to see the game actually running - enter Play Mode through Unity MCP, reach a screen by the player path or a debug route, take screenshots, read the console, review what a player would see, run a baseline walkthrough, reproduce a bug on screen, or do the light art check that confirms registered art shows correctly (runtime_validated). Automated test runs belong to p3-program-validation.
---

# P3 Playtest

用 Unity MCP 把游戏跑起来，像玩家一样看画面。用于基线检查、改动后的自测、在画面上复现 bug、确认素材上屏。

在游戏里的操作都用 `P3DevTools`，每个方法一行调用、返回几行文字报告（`execute_code`：`return P3DevTools.Describe();`）。先读文字，只有需要判断画面时才截图。环境注意事项（截图会暂停编辑器、Game View 尺寸、C# 6 限制）见 [mcp-recipes.md](references/mcp-recipes.md)。

| 调用 | 作用 |
|---|---|
| `Describe()` | 当前界面、序章台词、天数 / 金钱 / HP / SAN、背包、战斗状态和敌人意图、玩家此刻能点的按钮 |
| `Click("开始下潜")` | 按文字点玩家此刻能点的按钮（序章挡着时只能点序章的按钮） |
| `RunDive()` | 从工坊像玩家一样跑一次下潜（选层 → 整备里「带上上次的装备」→ 出发），到安全区或阶梯撤离、回工坊；提示"call again"时再调一次（有些界面下一帧才响应；战斗结束后要等攻击动画播完，结果界面才出来） |
| `AutoCombat()`、`TakeLoot()` | 打完当前战斗；把战利品放进背包并确认 |
| `SkipPrologue()`、`GoTo("map")` 等 | 调试路径：`workshop`、`sell`、`prosthetic`、`layers`、`loadout`、`map`、`combat`、`saferoom`、`stairs` |
| `AddMoney(500)`、`AddItem("gear_rusty_dagger")`、`Heal()` | 改状态（调试用） |
| `SetPrologueOnStartup(true)` | 下次进 Play Mode 时播放序章 |

游戏内按 F1 打开同一套功能的开发者面板（只在 Editor 和开发版里有）。

打包版的画面检查用 `tools/build/Test-WindowsBuild.ps1`：包自己截序章、工坊、整备、账本，连跑 3 次下潜，结果和截图在 `UnityClient/Logs/Playtest/<时间>_build_smoke/`。只在 Editor 里看过的画面不能代表打包版：画布查找、对象顺序在打包版里会不一样。截图全是同一张空白图时，多半是电脑锁屏或窗口没在渲染（Player.log 里会有 FMOD 找不到音频设备），自检的逻辑结果仍然有效，截图要等解锁后重跑。

Editor 里默认不播序章：进 Play Mode 直接到工坊，背包里是初始装备。要检查序章时，用菜单 `Tools/P3/启动时播放序章`、F1 面板或 `SetPrologueOnStartup(true)` 打开，下次进 Play Mode 生效；看完记得关掉。这个开关存在本机的 EditorPrefs 里，打出来的包总是播放序章。

## 步骤

1. **准备**
   - 读 `mcpforunity://editor/state`：Editor 要在线，不在编译，不在 Play Mode。Editor 没开就停下，报告 `validation_limited:unity_editor_not_running`。
   - 有代码改动时，先 `refresh_unity`（`compile=request`），再用 `read_console` 看编译错误。
   - `manage_scene(action="get_loaded_scenes")`：场景有未保存的改动就先问用户。
   - 证据目录：`UnityClient/Logs/Playtest/<yyyyMMdd_HHmm>_<主题>/`（`Logs/` 不进 git）。
2. **进 Play Mode**：`manage_editor(action="play")`，等 editor state 显示已在运行、可以调用工具。`read_console` 记下进场时已有的报错，作为基线。
3. **到达目标界面**，按优先级：
   - 正常玩家路径：`P3DevTools.Click(...)`、`RunDive()`，只点玩家此刻能点的按钮。
   - 调试路径：`P3DevTools.SkipPrologue()`、`GoTo(...)`、`AddMoney` 等。
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
