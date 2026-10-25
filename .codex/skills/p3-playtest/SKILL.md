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
| `Occlusion()` | 被别的东西盖住的物品和可点按钮（按 UGUI 绘制顺序算，不用截图）；弹窗后面的按钮被盖是正常的 |
| `LayoutCheck()` | 当前画面的布局问题：超出屏幕、溢出所在面板、按钮互相重叠或压在别的窗口上、文字被后画的面板盖住或被截断、图片被拉变形（按矩形算，跳过遮罩和弹窗背景下面的东西） |
| `DragCheck()` | 像玩家一样拿起一件物品（优先拾取栏里的）停在背包上方，报告有没有东西画在它上面、落点提示是绿还是红，然后放回原处 |
| `ShowCG(id)` / `HideCG()` / `MotionCG()` | 在对话层显示一张 CG 或序章漫画页（例如 `vfx_no0_core_start`）；`MotionCG()` 报告画面上动态 CG 的视频播到第几帧、是否停在最后一帧 |

游戏内按 F1 打开同一套功能的开发者面板（只在 Editor 和开发版里有）。

打包版可以像 Editor 一样一步步操作：`python tools/agent/p3cmd.py launch [包目录]` 以 `-p3cmd` 启动（窗口 1280x720、不读写存档、跳过序章，加 `--prologue` 播放），之后每条 `python tools/agent/p3cmd.py <命令>` 在游戏主线程执行一个 `P3DevTools` 方法（`Describe`、`Click 继续`、`RunDive 40 true`、`Occlusion`），另有 `screenshot [路径] [宽度，默认 640]`、`wait <秒>`、`quit`、`help`。只监听本机，不带参数启动时不开。长走查可以写成任务说明交给 `tools/agent/delegate.py`，只看它的报告。

打包版的画面检查用 `tools/build/Test-WindowsBuild.ps1`：包自己截序章每一句、工坊、手账、补给铺、维护、整备、战斗、营业和结尾，连跑 3 次下潜，每张截图跑一次 `LayoutCheck()`，有布局问题就 FAIL，结果和截图在 `UnityClient/Logs/Playtest/<时间>_build_smoke/`。只在 Editor 里看过的画面不能代表打包版：画布查找、对象顺序在打包版里会不一样。截图全是同一张空白图时，多半是电脑锁屏或窗口没在渲染（Player.log 里会有 FMOD 找不到音频设备），自检的逻辑结果仍然有效，截图要等解锁后重跑。

Editor 里默认不播序章：进 Play Mode 直接到工坊，背包里是初始装备。要检查序章时，用菜单 `Tools/P3/启动时播放序章`、F1 面板或 `SetPrologueOnStartup(true)` 打开，下次进 Play Mode 生效；看完记得关掉。这个开关存在本机的 EditorPrefs 里，打出来的包总是播放序章。

## 交给子 agent 的测试

画面和流程的大部分检查不用自己一步步走，交给子 agent（`tools/agent/delegate.py`，DeepSeek 对制作人不限量），只看报告。三层，各管一类问题：

| 测什么 | 怎么跑 | 谁 | 抓得到 / 抓不到 |
|---|---|---|---|
| 布局（确定性） | 打包自检 `Test-WindowsBuild.ps1`，每张截图跑 `LayoutCheck()` | 程序 | 出屏、重叠、截断、拉伸；抓不到"难看、看不懂" |
| 画面（看图） | 自检之后 `python tools/agent/visual_review.py`：把自检截图按每组 12 张分给视觉 agent 并行看，汇总到 `tmp/agent_reports/*_visual_review_summary.md` | `codex/gemini`（备用 `codex/deepseek`，两者都能看图；`hermes/*` 不能） | 半成品感、对比度、弹窗没遮罩、两个气泡、风格不统一；会看错，每条先打开对应截图核实再修 |
| 流程（像玩家一样玩） | `python tools/agent/delegate.py run playthrough tools/agent/briefs/playthrough.md --lock build --timeout 150` | `codex/deepseek` | 卡住、看不懂的按钮和数字、节奏（第几天通关）、日志里的异常；只读文字，看不到画面 |

- 不要让视觉 agent 自己操作打包版再截图：每一步要几分钟（80 分钟只截了 15 张）。截图交给自检，agent 只看图。
- 一组超过约 15 张截图会超过中转的 32MB 请求上限（413）。
- 打包和视觉检查不需要 Editor；同一时间只让一个 agent 驱动打包版（`--lock build`）。
- 改了界面以后：打包 → 自检 PASS → `visual_review.py` → 核实、修 → 再打包；改了流程或数值以后再加一轮 playthrough。

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
