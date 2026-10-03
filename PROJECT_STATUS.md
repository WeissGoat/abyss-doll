# 项目状态

> 唯一的状态页。开工先读；做完一件事或发现问题时，更新对应的行。历史交给 git log。

最后更新：2026-10-03

## 目标：1–2 小时的 Demo

范围（2026-10-03 制作人确认）：

- **做：** 序章（T0：开场到首次下潜、浅层第一次遭遇、回城照看）；深渊第 1–2 层；背包格子战斗和搜打撤；回城出售、维修和基础成长（底盘、义体）；天数推进和房租压力；照看零号（HP、SAN、磨损及其反馈）。
- **Demo 之后：** 势力、订单与声望，黑市，传闻、顾客、橱窗等经营扩展，人偶房间与日记，剧本调度的长线事件，第 3–5 层。相关设计文档保留，作为正式版的远期参考。
- **美术方向：** 到 M4 再定，暂定对标《火山的女儿》和《来自深渊》。

## 里程碑

### M1 循环能跑（进行中）

- [ ] 基线检查：编译，跑全量测试，在 Play Mode 里从开局走一遍并给每个界面截图。
- [ ] 处理 10 个已知失败的测试：修好，或确认断言已过时后更新或删除。
- [ ] 调试菜单：跳过序章、跳到任意界面、发钱发物品。进各界面的现成写法见 `UnityClient/Assets/Scripts/ArtAcceptance/ArtAcceptanceCaptureSteps.cs`。
- [ ] 查清并修好"下潜一次后就没有可用武器、出售为 0"，让"下潜 → 回城 → 出售 → 维修 / 升级 → 再下潜"能连续走 3–5 轮；把跑局模拟改成平衡回归测试。
- [ ] 删除已被 `run_tests` 取代的旧验证框架：`P3ValidationCore`、P3 自定义 MCP 工具、`tools/agent/p3-validation-core/`，以及 ArtAcceptance 运行时里调试菜单用不到的部分。
- [ ] 打第一个 Windows 包；制作人连续试玩 30–60 分钟并记下问题。

### M2 核心 15 分钟好玩

- [ ] 背包和战斗：加深格子位置与形状带来的取舍，让战利品和装备抢空间，让怪物意图逼人做选择。
- [ ] 地图：让"撤还是继续"成为真正的决定。
- [ ] 经济：第 3–5 天就能感到房租压力。
- [ ] 数值全部走配置，用跑局模拟当平衡仪表盘。

### M3 完整的前 30 分钟（T0）

- [ ] 用 M2 的循环接上已有的序章 A 段（`版本规划/0-12小时细案/T0-01A_*`）。
- [ ] 补 B 段（浅层第一次遭遇、带回第一件东西）和 C 段（回城照看，引出下一轮目标），设计各写一页。
- [ ] 收尾 T0-01A 的遗留项：漫画页排版、对白皮肤、半开放工坊、首潜许可卡。

### M4 锁定美术方向

- [ ] 定一套风格，写一页美术方向说明（对标《火山的女儿》《来自深渊》）。
- [ ] 画一张零号标准设定图，所有画面统一按它来。
- [ ] 建 UI 主题层：颜色、字体、边框、间距集中定义，以后换皮只改一处。
- [ ] 接入已登记但还没上界面的素材，替换棕金蒸汽朋克边框和开发面板。

### Demo 收尾（M4 之后再细化）

- T1（30 分钟到 2 小时，已有设计稿 `版本规划/0-12小时细案/T1_第一层搜打撤成形.md`）、第 2 层内容、完整试玩和打磨。

## 现在

- 刚完成：工作流减负（2026-10-03，入口改为 `AGENTS.md`，状态只记本页，新增 `p3-playtest` / `p3-content`，美术 Skill 合并为 `p3-art`）。
- 下一步：M1 基线检查，需要制作人先打开 Unity Editor。

## 已知问题

- **10 个 smoke 用例失败**（最早见于 2026-08-02）：
  - Edit Mode 下调用 `Destroy`：`PrologueFirstDiveDepartureSmokeTest`、`PrologueFirstDiveLayerConfirmSmokeTest`、`PrologueFirstDivePermissionSmokeSuite`、`PrologueHalfOpenNarrativeFlowSmokeTest`。
  - 断言和当前 UI、文案或数值对不上：`CombatHUDIntentBindingSmokeTest`、`CombatOutcomeReportSmokeTest`、`CombatReadabilityTextServiceSmokeTest`、`MonsterIntentPreviewServiceSmokeTest`、`WorkshopSmokeTest`。
  - 验证命令被拒时打出 Error 日志：`NarrativeCommandBridgeSmokeTest`。
- **循环可能断了**：跑局模拟（`MVPAutoplaytestTest`，2026-10-02 的报告 `UnityClient/Logs/MVPAutoplaytestReport.json`）30 局全部只下潜一次，其中 22 局停在"背包中没有可用武器"；出售、底盘升级、义体制造、进入第二层都是 0。机器人代码最后改于 5 月，可能已经过期，也可能循环真断了。`版本规划/09` 里"自动化试玩已证明核心闭环没有系统性断点"的说法目前不成立。
- **画面风格割裂**：序章 CG 是青蓝洞穴风；战斗和拾取界面是棕金蒸汽朋克边框；工坊主界面是开发面板，露着英文调试文字和原始 ID；零号在不同画面至少有 4 种长相。M4 统一处理。
- **UI 全在代码里拼**，没有主题层；`WorkshopUIController.cs` 有 2667 行。碰到时按界面拆分，不做大重构。
- **旧验证框架**里有压缩成一两行的代码，它的测试还会往 `UnityClient/Logs/P3Validation/` 写假截图。M1 删除。

## 暂停项（Demo 需要时再拿起）

- 美术：Zero 立绘套组（14 张）的新 hurt 待同步 Approved / Unity / Registry；Request Catalog 的 308 项都需要重新撰写 PromptRevision；离线 Registry candidate 有 `changed_existing=22` 待刷新。M4 一并处理，细节见 `docs/archive/workflow-2026-10/agent_status/art.md`。
- 配置：C4 局外成长配置只补 Demo 用得到的部分。
- 设计：T2–T4 细案暂停；Demo 只需要 T0 和 T1。

## 决定记录

- 2026-10-03：
  - Demo 范围按上文，美术方向在 M4 定。
  - 先证明核心循环好玩，再给序章封板。
  - 工作流减负：角色、执行道和分角色状态页归档到 `docs/archive/workflow-2026-10/`，状态只记在本页，入口见 `AGENTS.md`。
