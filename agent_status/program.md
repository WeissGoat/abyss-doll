---
id: agent_status_program
title: 程序 / Unity 状态
type: status
role: 程序
domain: unity_programming
status: active
source_of_truth: true
related:
  - 版本规划/09_正式版核心纵切开发路线.md
  - agent_status/README.md
  - PROJECT_STATUS.md
  - 开发文档/rules/01_客户端分层与领域架构规范.md
  - 开发文档/rules/00_程序开发总规则.md
  - 开发文档/00_程序开发大纲.md
  - 开发文档/15_P0配置Validator与自动验收底座需求.md
  - 开发文档/16_程序主流程闭环与架构收口推进计划.md
  - 设计文档/GDD/GDD_00_系统关联总图.md
  - agent_status/design.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - agent_status/art.md
  - 知识库/views/program.md
  - 美术文档/15_FormalV2运行时验收待办清单.md
last_verified: 2026-05-27
update_rule: 程序、Unity、验证或工程边界任务完成后更新本文件。
---

# 程序 / Unity 状态

## 最后更新

2026-06-09

## 当前关注

支撑正式版核心纵切，让 Unity 运行时系统保持模块清晰、数据驱动、可测试，并与当前 GDD 规则一致。

程序侧完成项以本状态页、开发文档、代码事实和验收证据为准；`版本规划/09_正式版核心纵切开发路线.md` 只提供宏观优先级、长期节点和防重复派发口径。当前正式完成口径只把 P0 自动验收底座、P1 背包 / 物品生命周期标记为程序完成；战斗已开始补正式意图可读数据层，深渊地图、局外成长、经济压力和 P5 表现支撑仍按进行中或待正式验收处理，不得因历史原型或局部接入误判为正式完成。

## 必读文件

- `知识库/views/program.md`
- `开发文档/00_程序开发大纲.md`
- `开发文档/rules/01_客户端分层与领域架构规范.md`
- `开发文档/rules/02_Unity表现层与编辑器构建规范.md`
- `开发文档/12_程序开发优化建议与重构路线.md`
- `开发文档/rules/00_程序开发总规则.md`
- 当前系统对应的 `开发文档/` 落地文档。

## 工作边界

- 运行时 UI 使用纯 UGUI。不要重新引入 UI Toolkit 运行时资产或代码路径。
- `配置表(JSON)` 是配置源；`UnityClient/Assets/StreamingAssets/Configs` 是生成副本。
- 游戏规则属于后端 / 领域服务，不属于 UI Controller。
- UI Controller 只调用后端 API 并监听事件。
- 接入新 Approved 美术素材前，先查看 `美术文档/_generated/可接入素材清单.md`；`program_integrate` 是当前可登记 / 可接入队列。
- 移动 Unity 资产时保留并同步移动 `.meta` 文件。
- `tools/ai-image-gateway` 按 submodule 处理。

## 常用命令

```powershell
.\tools\config\Sync-Configs.ps1 -Clean
Set-Content -Path "UnityClient/Logs/.test_trigger" -Value "RUN_ALL_TESTS"
```

## 最近完成
- `REVIEW-01` mission outcome 审查已完成：`.mission/20260608_012621-Program-C1-C3-MainLoop-Architecture.csv` 的 14 个 TASK 均为 `DONE`，strict 校验通过；本轮原始目标“三段推进”已有对应证据：`CFG-01..02` 覆盖 C1-C3 配置程序支持，`FLOW-01..07` 覆盖主流程可玩闭环一轮补强，`ARCH-01..05` 覆盖架构审计、服务化、快照 / 操作配对、P0 报告契约和防重复派发规则。剩余风险不改写为完成：Unity Editor 未运行导致 runtime smoke 仍是 `validation_limited:UnityEditorNotRunning`，ArtAcceptance latest 仍有既有 combat HUD / 怪物战斗图失败。
- `ARCH-05` 文档和状态防重复派发收口已完成：`开发文档/16_程序主流程闭环与架构收口推进计划.md` 新增程序完成状态标签和防重复派发规则，统一区分 `服务完成`、`UI可达`、`展示已接入`、`可操作闭环完成`、`验收受限`、`未开放` 和 `架构收口`；`版本规划/09_正式版核心纵切开发路线.md` 同步宏观派发口径。后续已标为 `可操作闭环完成` 的基础能力不得以同名功能重开，只能按 bug、验收补强、表现补强或配置补齐处理；`validation_limited:*` 只能说明验证受限，不能当作通过证据。验证证据：`.\tools\docs\Validate-Docs.ps1` 通过；mission strict 通过，`tasks=14/14 done`。
- `ARCH-04` Validator / P0 / smoke 报告契约首批收口已完成：`tools/agent/Invoke-P0Validation.ps1` 新增 `BlockedCount`、`LimitationCount`、`ValidationLimitations`、`SmokeTestRegistry`、`MainFlowSmokeTests` 和 step 级 `StatusCode`，让 `Failed`、`Blocked`、warning 与 `validation_limited:*` 不再混在同一个错误口径里。Unity Editor 未运行时，`ConfigValidator` / `UnitySmokeTests` 现在记录为 `Blocked` 和 `validation_limited:UnityEditorNotRunning`，不再混入 `ErrorCount`；ArtAcceptance latest 的真实失败仍保留为 `Failed`。`开发文档/15_P0配置Validator与自动验收底座需求.md`、`开发文档/16_程序主流程闭环与架构收口推进计划.md`、`开发文档/rules/04_自动化测试与验收流程规范.md` 和 `tools/agent/README.md` 已同步字段与状态语义。验证证据：`.\tools\agent\Invoke-P0Validation.ps1 -TimeoutSeconds 180` 产出 RunID=`20260608_044305`，报告为 `Failed`，其中 `ErrorCount=17` 来自既有 ArtAcceptance latest 失败，`BlockedCount=2` / `LimitationCount=2` 来自 Unity Editor 未运行，`MainFlowSmokeTests` 列出 9 个主流程 smoke；`git diff --check` 通过；`.\tools\docs\Validate-Docs.ps1` 通过。
- 已同步项目级程序推进口径到 `PROJECT_STATUS.md`：当前程序优先级不再只写 C1-C3 配置支持，而是明确为 `CFG-01..02 -> FLOW-01..07 -> ARCH-01..05 -> REVIEW-01`。其中第二优先级是继续复核并补强“功能未开放完”的主流程可玩闭环，第三优先级是继续推进 Validator / P0 / smoke / 总控和 UI 职责边界的架构收口。
- `ARCH-03` 可读快照与可操作服务配对收口已完成：`开发文档/16_程序主流程闭环与架构收口推进计划.md` 已新增 `ARCH-03` 配对矩阵，按 Combat / Dungeon / Economy / Growth / Doll / Workshop Formal V1 面板标明 `可操作闭环完成`、`展示已接入`、`仅导航`、`未开放` 四类状态。主要结论：战斗 HUD、层选择 / 地图节点、阶梯 / 撤离、小镇出售、工坊维护、底盘升级、工坊义体制造、人偶触摸 / 对话已有真实服务证据；战斗结果结算、账单 / 经营摘要多为展示 + 导航；订单、传闻、势力商店、月租决策、典当、剧情事件、人偶赠礼仍不能用只读面板冒充可玩完成。本轮代码侧同步修正 `WorkshopFormalV1PanelBindingService.BuildChassisUpgradeBinding()` 的过期文案，并在 `WorkshopFormalV1PanelBindingSmokeTest.Run` 中断言底盘面板先展示 `ChassisUpgradeService` 已接入，再点击 `Upgrade_Button` 走真实升级服务。验证证据：`git diff --check` 通过；`dotnet build UnityClient/Assembly-CSharp.csproj --no-restore` 通过，0 warning / 0 error；`.\tools\docs\Validate-Docs.ps1` 通过；mission strict 校验通过；P0 RunID=`20260608_042204` 中 `ConfigSync` / `UIDesignValidation` 通过，`ConfigValidator` / `UnitySmokeTests` 因 Unity Editor 未运行 blocked，`ArtAcceptanceLatest` 仍读取既有旧失败 `20260606_230523`。
- `ARCH-02` 首批架构收口已完成：新增 `ChassisUpgradeService` / `ChassisUpgradeResult` 和 `ProstheticCraftingService` / `ProstheticCraftingResult`，把底盘升级、义体制造、同槽替换、成本扣除、效果重算和反馈文本从旧 `WorkshopSystem` 混合入口中拆出；`WorkshopSystem` 保留为兼容 facade，但不再承载这些领域操作细节。`WorkshopUIController` 的底盘升级按钮和义体制造行按钮改为消费 Result DTO，`WorkshopFormalV1PanelActionService` 的 `chassis_upgrade_panel/Upgrade_Button` 不再用前后 ID 猜成功，改为消费 `ChassisUpgradeResult`。测试侧补强 `WorkshopSmokeTest` 的底盘 / 义体 Result DTO 断言、义体面板按钮真实制造链路断言，并在 `WorkshopFormalV1PanelBindingSmokeTest` 中新增 Formal V1 底盘升级按钮真实后端操作断言。验证证据：`git diff --check` 通过；`dotnet build UnityClient/Assembly-CSharp.csproj --no-restore` 通过，0 warning / 0 error；`.\tools\docs\Validate-Docs.ps1` 通过；mission strict 校验通过；P0 RunID=`20260608_040615` 中 `ConfigSync` / `UIDesignValidation` 通过，`ConfigValidator` / `UnitySmokeTests` 因 Unity Editor 未运行记录为 `validation_limited:UnityEditorNotRunning`，`ArtAcceptanceLatest` 仍为既有战斗 HUD / 怪物 combat VisualID 旧失败。
- `ARCH-01` 主流程打通后的架构缝隙审计已完成并回写到 `开发文档/16_程序主流程闭环与架构收口推进计划.md`：审计对象覆盖 `GameFlowController`、`WorkshopUIController`、`WorkshopFormalV1PanelActionService`、`WorkshopSystem`、`DungeonManager`、`ConfigValidator` 和黄金路径 smoke，形成 12 条真实耦合点清单。主要结论：`GameFlowController` 仍持有 debug 资源注入、回城 HP/SAN 恢复、缺 UI 自动确认和运行时 fallback 面板构建；工坊底盘升级 / 义体制造此前仍通过旧 `WorkshopSystem` 直接扣费和改状态，缺 Result DTO；`DungeonManager` 同时承载事件订阅、地图推进、撤离 / 战败结算和战利品账本；`ConfigValidator` 已成为多域单文件；黄金路径 smoke 需要后续抽测试夹具。底盘升级 / 义体制造首批服务化已在 `ARCH-02` 处理，后续继续收口 `GameFlowController` fallback、`DungeonManager` 结算职责、Validator 和测试入口。
- `FLOW-07` 黄金路径 smoke / P0 摘要已落地当前可验证版本：新增 `MainFlowGoldenPathSmokeTest.Run`，把小镇准备、L1 下潜、地图寻路、战斗节点战利品拾取、阶梯解锁、撤离回城、战利品出售、工坊维护恢复下潜许可、再次进入 L2 串成一条可重复主流程验收路径；测试路线选择已调整为优先使用包含 `CombatNode` 的阶梯路径，避免只跑到无战斗阶梯路线后误判“没有战利品”。P0 默认 Unity smoke 列表已纳入 `MainFlowGoldenPathSmokeTest.Run`，`tools/agent/README.md` 已同步说明黄金路径覆盖范围。验证证据：`git diff --check -- UnityClient/Assets/Scripts/Tests/MainFlowGoldenPathSmokeTest.cs tools/agent/Invoke-P0Validation.ps1 tools/agent/README.md` 通过；`dotnet build UnityClient/Assembly-CSharp.csproj --no-restore` 通过，0 warning / 0 error；`.\tools\docs\Validate-Docs.ps1` 通过；`.\tools\p3-mission\Test-P3Mission.ps1 -Path ".mission\20260608_012621-Program-C1-C3-MainLoop-Architecture.csv" -Strict` 通过；`.\tools\agent\Invoke-P0Validation.ps1 -TimeoutSeconds 180` 产出 RunID=`20260608_033958`，`ConfigSync` 与 `UIDesignValidation` 通过，`smoke_tests.json` 已列出 `MainFlowGoldenPathSmokeTest.Run`，但 `ConfigValidator` / `UnitySmokeTests` 因 Unity Editor 未运行记为 `validation_limited:UnityEditorNotRunning`，`ArtAcceptanceLatest` 仍为既有 combat_hud / 怪物战斗图 / 过期截图验收问题。
- `FLOW-06` 工坊成长操作闭环第一条真实链路已纳入主流程验收：现有 `WorkshopFormalV1PanelActionService` 的 `maintenance_panel` 按钮链路已确认走 `core.Workshop.ApplyMaintenance()` / `MaintenanceService`，不由 UI 直接改写 HP、金币、材料或磨损；本轮补强 `WorkshopFormalV1PanelBindingSmokeTest.Run`，让 `FullRepair_Button` 在正常工坊 Formal V1 面板中证明维护前 `DiveReadinessService` 因极端磨损阻断下潜，点击后扣金币、消耗 `loot_gear_scrap`、降低磨损、补满 HP，并让下潜许可重新通过。P0 默认 Unity smoke 列表已纳入 `MaintenanceServiceSmokeTest.Run`、`GrowthFeedbackServiceSmokeTest.Run` 与 `WorkshopFormalV1PanelBindingSmokeTest.Run`，用于覆盖维护服务、成长反馈和玩家 UI 可操作入口。验证证据：`git diff --check -- UnityClient/Assets/Scripts/Tests/WorkshopFormalV1PanelBindingSmokeTest.cs tools/agent/Invoke-P0Validation.ps1 tools/agent/README.md` 通过；`.\tools\docs\Validate-Docs.ps1` 通过；`.\tools\p3-mission\Test-P3Mission.ps1 -Path ".mission\20260608_012621-Program-C1-C3-MainLoop-Architecture.csv" -Strict` 通过；`.\tools\agent\Invoke-P0Validation.ps1 -TimeoutSeconds 180` 产出 RunID=`20260608_032327`，`ConfigSync` 与 `UIDesignValidation` 通过，`smoke_tests.json` 已列出新增三项工坊成长测试，但 `ConfigValidator` / `UnitySmokeTests` 因 Unity Editor 未运行记为 `validation_limited:UnityEditorNotRunning`，`ArtAcceptanceLatest` 仍为既有 combat_hud / 怪物战斗图 / 过期截图验收问题。
- `FLOW-05` 小镇经济操作闭环第一条真实链路已接入：`WorkshopUIController` 的出售面板现在不再调用旧 `WorkshopSystem.SellItem/SellAllStashItems`，而是统一调用 `TownEconomyService.SellItems(..., EconomySellChannel.DumpBox)`；单件出售和全部出售都会走传闻倍率估值、金币增加、背包 / 仓库物品移除和背包效果重算，出售行显示 `BaseValue -> FinalValue`，统计值也改为 `TownEconomyService.CalculateItemSellValue()` 的最终售价。`WorkshopSmokeTest.Run` 已补充小镇 UI 出售按钮链路断言：设置 `rumor_mechanical_price_up` 后从背包出售 `loot_gear_scrap`，期望金币按 1.5x 增加且物品从背包移除；P0 默认 Unity smoke 列表已纳入 `TownEconomyServiceSmokeTest.Run` 与 `WorkshopSmokeTest.Run`。验证证据：`.\tools\agent\Invoke-AgentHealthCheck.ps1` 通过但提示无关 submodule / 本地工具脏项；`git diff --check -- UnityClient/Assets/Scripts/UI/Workshop/WorkshopUIController.cs UnityClient/Assets/Scripts/Tests/WorkshopSmokeTest.cs tools/agent/Invoke-P0Validation.ps1 tools/agent/README.md` 通过；`.\tools\docs\Validate-Docs.ps1` 通过；`.\tools\p3-mission\Test-P3Mission.ps1 -Path ".mission\20260608_012621-Program-C1-C3-MainLoop-Architecture.csv" -Strict` 通过；`.\tools\agent\Invoke-P0Validation.ps1 -TimeoutSeconds 180` 产出 RunID=`20260608_031810`，`ConfigSync` 与 `UIDesignValidation` 通过，`smoke_tests.json` 已列出新增小镇经济测试，但 `ConfigValidator` / `UnitySmokeTests` 因 Unity Editor 未运行记为 `validation_limited:UnityEditorNotRunning`，`ArtAcceptanceLatest` 仍为既有 combat_hud / 怪物战斗图 / 过期截图验收问题。
- `FLOW-04` 阶梯 / 安全区 / 撤离 / 进入下一层 / 层级解锁闭环已补齐程序侧验收覆盖：`DungeonStairsProgressionTest.Run` 新增 `Stairs Multi Layer Progression` smoke，用配置生成的 `StairsNode` 从 L1 首条路线推进到阶梯，验证解锁 L2、进入 L2 后入口节点可点击，再从 L2 阶梯解锁 L3、进入 L3 后入口节点可点击；测试同时覆盖 `StairsNode.CanEnterNextLayer()`、`EnterNextLayer()`、`DungeonManager.CanMoveToNode()` 和 `CanStartAtLayer(3)` 的组合链路，未恢复 Boss 胜利自动回城逻辑，也未写死生产阶梯节点。验证证据：`git diff --check -- UnityClient/Assets/Scripts/Tests/DungeonStairsProgressionTest.cs` 通过；`.\tools\docs\Validate-Docs.ps1` 通过；`.\tools\p3-mission\Test-P3Mission.ps1 -Path ".mission\20260608_012621-Program-C1-C3-MainLoop-Architecture.csv" -Strict` 通过；`.\tools\agent\Invoke-P0Validation.ps1 -TimeoutSeconds 180` 产出 RunID=`20260608_030542`，`ConfigSync` 与 `UIDesignValidation` 通过，`UnitySmokeTests` 因 Unity Editor 未运行记为 `validation_limited:UnityEditorNotRunning` 且 `smoke_tests.json` 已列出 `DungeonStairsProgressionTest.Run`，`ArtAcceptanceLatest` 仍为既有 combat_hud / 怪物战斗图 / 过期截图验收问题。
- `FLOW-03` 地图节点 / 战斗 / 非战斗节点 / 战利品 / 返回地图闭环已完成程序侧补强：`DungeonOutcomeNode` 的非战斗奖励现在与战斗战利品对齐物品生命周期，奖励生成时标为 `Inbox + Run`，确认时已放入背包的标为 `Backpack + Run`，未放入的标为 `Lost`；`CombatLootDropTest` 已从旧的 `elite_scrap_guard` 保底断言改为正式 `boss_gatekeeper_mk1` 保底 `mat_core_tier1` 口径；`DungeonNodeTypesSmokeTest` 新增三段主流程 smoke，覆盖从地图进入战斗节点并拾取后返回地图、从地图进入宝箱奖励节点并放弃后返回地图、从地图进入纯结果节点并结算后返回地图，且断言下一节点重新可点击。P0 默认 Unity smoke 列表已纳入 `CombatLootDropTest.Run` 与 `DungeonNodeTypesSmokeTest.Run`，阻断报告也会列出计划测试。验证证据：`git diff --check` 通过；`.\tools\docs\Validate-Docs.ps1` 通过；`.\tools\p3-mission\Test-P3Mission.ps1 -Path ".mission\20260608_012621-Program-C1-C3-MainLoop-Architecture.csv" -Strict` 通过；`.\tools\agent\Invoke-P0Validation.ps1 -TimeoutSeconds 180` 产出 RunID=`20260608_025532`，`ConfigSync` 与 `UIDesignValidation` 通过，`UnitySmokeTests` 因 Unity Editor 未运行记为 `validation_limited:UnityEditorNotRunning`，但 `smoke_tests.json` 已列出新增两个主流程测试，`ArtAcceptanceLatest` 仍为既有 combat_hud / 怪物战斗图验收问题。
- `FLOW-02` 层选择 / 下潜许可 / 地图进入路径已补齐玩家可读反馈：`DungeonStartLayerUIController` 现在用 `DiveReadinessService.Evaluate()` 作为每一层行状态和顶部摘要的唯一来源，未解锁层显示短中文原因（如“未解锁：先通过上一层”），人偶磨损 / 侵蚀 / 底盘 / 背包 / 义体等阻断会在当前选择摘要中展示完整 `DiveReadinessResult.BuildSummary()`；选择 fallback 改为优先最深可下潜层，若全部被状态阻断则保留已解锁层用于展示真实阻断原因。新增 `DungeonStairsProgressionTest` 的 `Dungeon Start Layer UI Readiness Reasons` smoke 用例，覆盖锁层原因可见、极端磨损原因可见、确认按钮随 readiness 禁用；同一测试类已有层进入、二层地图节点 / 连线布局和移动规则覆盖。验证证据：`git diff --check` 通过；`.\tools\agent\Invoke-P0Validation.ps1 -TimeoutSeconds 180` 产出 RunID=`20260608_024005`，`ConfigSync` 与 `UIDesignValidation` 通过，`ConfigValidator` / `UnitySmokeTests` 因 Unity Editor 未运行记为 `validation_limited:UnityEditorNotRunning`，`ArtAcceptanceLatest` 仍为既有 stale / combat_hud 缺失问题。
- `FLOW-01` 玩家主流程缺口审计已完成并形成实现顺序：当前小镇 -> 层选择 -> 地图 -> 战斗 / 非战斗节点 -> 战利品 -> 阶梯 / 安全区 -> 撤离 / 下一层路径已有真实领域链路，战利品拾取和阶梯继续深入不再是最高阻断；主要缺口按阻断级别排序为：`高` 小镇经济 UI 仍走旧 `WorkshopSystem.SellItem/SellAllStashItems`，没有消费 `TownEconomyService.SellItems` 的渠道 / 传闻倍率 / 报告链路；`高` Formal V1 `order_board`、`rumor_board`、`shop_staging`、`faction_shop` 多数仍是只读或 unsupported，玩家无法从正常 UI 接单 / 交付 / 使用传闻价格链；`中` 层选择失败原因只显示通用“通过上一层后解锁”，没有展示 `DiveReadinessService` 的完整失败原因；`中` 工坊成长只有维护和人偶触摸 / 对话已调用真实服务，底盘升级仍走旧 `WorkshopSystem.UpgradeDollChassis` 直接入口，制造 / 装备 / 配方选择未形成正式可玩闭环；`中` 还缺一条小镇 -> 深渊 -> 战斗 -> 战利品 -> 阶梯 / 撤离 -> 回城 -> 出售 / 维护 -> 再下潜的黄金路径 smoke / P0 摘要。验证证据：静态核验 `WorkshopUIController`、`WorkshopFormalV1PanelActionService`、`DungeonStartLayerUIController`、`DungeonManager`、`SafeRoomUIController`、`CombatNode`、`DungeonOutcomeNode` 和 `CombatLootUIController`；`.\tools\agent\Invoke-P0Validation.ps1 -TimeoutSeconds 180` 产出 RunID=`20260608_023254`，`ConfigSync` 与 `UIDesignValidation` 通过，`ConfigValidator` / `UnitySmokeTests` 因 Unity Editor 未运行记为 `validation_limited:UnityEditorNotRunning`，`ArtAcceptanceLatest` 仍为既有 stale / combat_hud 缺失问题。
- `CFG-01` C1-C3 配置程序支持审计已完成：`.\tools\config\Sync-Configs.ps1 -Clean` 通过；`.\tools\agent\Invoke-P0Validation.ps1 -TimeoutSeconds 180` 产出 `UnityClient/Logs/P0Validation/latest/report.json`，其中 ConfigValidator / Unity smoke 因 Unity Editor 未运行记为 `Blocked`，UI 规格校验通过，ArtAcceptance latest 为既有表现 / 缺图 / stale 问题；补充静态交叉审计报告 `UnityClient/Logs/P0Validation/latest/c1_c3_static_config_audit.json` / `.md`，覆盖 Items=40、Rewards=41、Monsters=21、Dungeons=3、Orders=8、Rumors=4、Factions=4，审计 C1-C3 硬运行时引用 error=0、warning=0。
- `CFG-02` 已按证据关闭且未做空代码改动：复跑 `.\tools\config\Sync-Configs.ps1 -Clean` 通过；复跑 `.\tools\agent\Invoke-P0Validation.ps1 -TimeoutSeconds 180` 产出 RunID=`20260608_022356`，失败项仍为 `validation_limited:UnityEditorNotRunning` 的 ConfigValidator / UnitySmokeTests 以及既有 ArtAcceptance stale / 缺图 / combat_hud 截图验收问题；结合 `CFG-01` 静态审计 error=0、warning=0，本轮未发现需要立即修复的 C1-C3 阻断性解析、Validator 或 smoke 程序缺口，下一步进入 `FLOW-01`。
- 已同步修正 `版本规划/09_正式版核心纵切开发路线.md` 的程序近期顺序：从旧的“战斗 UI / 经济 UI / 工坊 UI / C1-C3”改为用户最新三段优先级：1）C1-C3 配置程序支持；2）继续开发“功能未开放完”的主流程闭环；3）程序架构优化 / 收口。
- 已补全正式程序推进细化文档：`开发文档/16_程序主流程闭环与架构收口推进计划.md`。该文档现在明确本轮不是只做 C1-C3 配置审计，而是三段连续工作：1）C1-C3 配置程序支持；2）继续开发“功能未开放完”的主流程闭环；3）程序架构优化 / 收口。
- `开发文档/16_程序主流程闭环与架构收口推进计划.md` 已把主流程闭环拆成 `FLOW-01..FLOW-07`：玩家路径缺口审计、层选择 / 下潜 / 地图进入、地图节点 / 战斗 / 战利品、阶梯 / 安全区 / 撤离 / 层级解锁、小镇经济操作、工坊成长操作、整条黄金路径 smoke / P0 摘要。
- `开发文档/16_程序主流程闭环与架构收口推进计划.md` 已把架构优化 / 收口拆成 `ARCH-01..ARCH-05`：职责边界审计、流程编排与领域操作分离、只读快照与可操作服务配对、Validator / P0 / smoke 入口收口、文档和状态防重复收口。
- 已按用户最新优先级建立并补全本地程序推进 mission：`.mission/20260608_012621-Program-C1-C3-MainLoop-Architecture.csv`。新顺序为 `CFG-01..02 -> FLOW-01..07 -> ARCH-01..05 -> REVIEW-01`；mission 作为后续持续推进的本地恢复计划，不纳入提交。
- 开发文档/rules/00_程序开发总规则.md 已补充模块职责 / 代码归属 / hardcode 红线：新增代码必须进入拥有业务事实的模块，找不到归属先补服务、工厂、Action、Effect、Reward 或接口，不得塞进总控、流程或 UI Controller；4_自动化测试与验收流程规范.md 已补充测试粒度原则，要求测试围绕业务闭环、玩家可见结果、配置契约和验收场景，避免针对内部实现碎片写过细测试。
- `开发文档/rules/` 已重构为 `00-04` 顺序结构：`00_程序开发总规则.md` 只保留开工门禁与通用红线，`01` 负责客户端分层 / 领域架构，`02` 负责 Unity UGUI / Editor 构建，`03` 负责 VisualID / VisualAssetService 资源契约，`04` 负责自动化测试与验收流程；已同步跨文档引用、知识库索引和双向 related，`Generate-DocsIndex.ps1` / `Validate-Docs.ps1` 验证通过，indexed=217、missing_metadata=0。

- 开发文档结构已按当前项目阶段重整：新增 `开发文档/README.md`、`开发文档/rules/README.md`、`开发文档/archive/README.md`；将架构 / 编码 / UGUI / 自动化测试 / 视觉资源程序接入规范迁入 `开发文档/rules/`；将早期 `06_架构评估与收口建议.md` 归档到 `开发文档/archive/` 并标记 `archived`；同步更新跨文档引用、知识库入口和索引，`Generate-DocsIndex.ps1` 与 `Validate-Docs.ps1` 已通过，indexed=221。
- P0 验收口径与报告采集已修复：`RewardSystemSmokeTest.Run` 改为验证 `reward_boss_gatekeeper_mk1` 保底 `mat_core_tier1`，并确认 `reward_monster_elite_scrap_guard` 不承担 Boss 保底；`MonsterActionAITest.Run` 改为按怪物 AI 配置的 `Damage * RepeatCount` 计算期望 HP；`Invoke-UnitySmokeTests.ps1` 现在可输出匹配的稳定 `TestReport` 副本，`Invoke-P0Validation.ps1` 改为读取该副本，避免抢读全局 `UnityClient/Logs/TestReport.json` 时误判 `Blocked`。最新 `Invoke-P0Validation.ps1 -TimeoutSeconds 180` 通过，RunID=`20260527_013319`，Errors=0，Warnings=17。
- 已创建复制程序智能体使用的状态页。
- 运行时 Prefab 已移动到 `UnityClient/Assets/Prefabs`。
- 已新增并验证 `tools/config/Sync-Configs.ps1`。
- 项目结构整理时已移除旧的受跟踪 UI Toolkit 运行时资产。
- PM 已将程序线纳入 `版本规划/09_正式版核心纵切开发路线.md`：A1 收边界，A2 做背包与战斗正式纵切，A3 承接深渊与局外成长。
- A1A 背包交互继续收口：`DraggableItemUI` 已移除历史“失联物品自动修复”，点击使用不再由 UI 悄悄改写真实背包状态。
- A1A 背包表现规格继续收口：新增 `InventoryDisplaySpec`，统一格子尺寸、间距、物品占格尺寸、拖拽偏移和各界面背包布局 profile。
- `InventoryDisplaySpecSmokeTest` 已通过 Unity 自动测试守护执行，确认背包规格入口、布局 profile 和 GridLayoutGroup 应用结果一致。
- `GridGenerator` 与 `MVPEditorSetup` 已接入 `InventoryDisplaySpec.ApplyGridLayout()`，背包格运行时生成和编辑器骨架默认值不再各自维护尺寸常量。
- 新增 `InventoryGridLayoutAssetValidator` Editor 工具与 smoke test，自动检查 Prefab / Scene 中背包 GridLayoutGroup 默认值是否符合 `InventoryDisplaySpec`。
- 新增 `tools/agent/Invoke-UnitySmokeTests.ps1`，复制程序智能体可一键触发背包交互服务、DisplaySpec 与 GridLayoutGroup 资产布局 smoke test。
- 已新增 `开发文档/15_P0配置Validator与自动验收底座需求.md`，明确 P0 配置校验、统一验收命令、报告格式、门禁等级和 seed 回归需求，后续程序可按该文档扩 `ConfigValidator` 和 `Invoke-P0Validation.ps1`。
- 美术侧已交付 `combat_hud` Formal V1 active 规格：敌人从卡片迁移为右侧战斗实体，背包底部居中，敌人血条贴脚下。
- 美术侧已入库 `combat_hud` Formal V1 所需第一批战斗资源：4 个 `monster_*_combat`、`ui_combat_entity_shadow` 和 `ui_combat_target_ring`。
- UI 程序侧已接入 `combat_hud` Formal V1：`HUDController` 生成左玩家 / 右敌人实体舞台、敌人脚下血条、目标光环和底部居中战斗背包，敌人实体优先读取 `MonsterEntity.CombatVisualID`。
- 已刷新 `VisualAssetRegistry` 并登记 4 个 `monster_*_combat`、`ui_combat_entity_shadow`、`ui_combat_target_ring`；`VisualAssetSmokeTest.Run`、`InventoryDisplaySpecSmokeTest.Run`、`ConfigValidationSmokeTest.Run` 均通过。
- ArtAcceptance `20260524_043441` 已通过，`combat_hud` 截图无 warnings / errors，可交美术侧做正式视觉验收。
- 美术侧已提供 `美术文档/_generated/可接入素材清单.md/json`，程序侧可按 `program_integrate` 条目自助发现待接入素材。
- A2 背包旋转规则已配置化：`Grid.CanRotate` / `Grid.RotationSteps` 纳入 `ItemGridComponent`、`BackpackGrid`、`InventoryInteractionService`、`ConfigValidator` 和 `InventoryInteractionServiceSmokeTest`，方向型相邻效果会随物品当前旋转后的朝向重新计算。
- 已按 `美术文档/_generated/可接入素材清单.md` 的 `program_integrate` 区块登记 17 个 Approved VisualID：4 个 `monster_*_combat`、`ui_combat_entity_shadow`、`ui_combat_target_ring` 和 11 个维护 / 账单 / A4 界面图标。
- 已重建 `VisualAssetRegistry` 到 191 个 Approved Sprite 条目，并把深渊 L1 / L2 的 Treasure / Event / Rest / Hazard 节点 `NodeIconID` 对齐到正式 VisualID；`VisualAssetSmokeTest.Run`、`ConfigValidationSmokeTest.Run`、`DungeonNodeTypesSmokeTest.Run` 顺序 batchmode 验证通过。
- 已新增 `WorkshopFormalV1PanelController`，通过 `WorkshopUIController.OpenFormalV1Panel()` 接入 `maintenance_panel`、`daily_bill_report`、`shop_staging`、`order_board`、`rumor_board` 的 UGUI 预览面板和 Sprite 绑定，不写入玩法 / 经济规则。
- 已扩展 `VisualAssetSmokeTest`、`ConfigValidator` 和 `ArtAcceptanceRunner` 覆盖本轮新增 VisualID 与 5 个 Formal V1 面板；`VisualAssetSmokeTest.Run`、`ConfigValidationSmokeTest.Run`、`Validate-UIDesign.ps1` 均通过。
- ArtAcceptance `20260524_212423` 已通过，15 个截图点均 captured，`MissingRequiredVisualIDs=0`，UI snapshot 未发现 missing sprite；新增截图包含 `maintenance_panel`、`daily_bill_report`、`shop_staging`、`order_board`、`rumor_board`。
- Formal V1 运行时美术验收覆盖继续补齐：`ArtAcceptanceRunner` 新增 `business_settlement`、`chassis_upgrade_panel`、`doll_interaction`、`doll_room`、`faction_shop`、`scenario_event` 6 个截图入口；`WorkshopFormalV1PanelController` 已用 `VisualAssetService` 绑定对应 Approved VisualID，不写入玩法状态。
- 本轮校验：`VisualAssetSmokeTest.Run` 顺序 batchmode 通过；`ArtAcceptanceSmokeTest.Run` 首次因 Unity 工程锁被拦截，释放后单独重跑通过。完整运行时截图验收仍需在 Unity Editor 触发 ArtAcceptance 自动跑一轮。
- P0B 统一验收入口已落地：新增 `tools/agent/Invoke-P0Validation.ps1`，串联配置同步、`ConfigValidationSmokeTest.Run`、核心 Unity smoke tests、UI 规格校验和 ArtAcceptance latest 摘要，并生成 `UnityClient/Logs/P0Validation/latest/report.json` / `report.md`。
- 已修正 P0 阻断测试口径：`InventoryGridLayoutAssetValidatorTest` 避免在 PlayMode 中误调用编辑器场景 API；`DungeonStairsProgressionTest` 按正式地图的节点按钮 + 路线线段结构校验二层地图布局。
- `Invoke-P0Validation.ps1 -TimeoutSeconds 180` 已通过非 Strict 验证：配置同步、ConfigValidator、Unity smoke tests、UI 规格校验、ArtAcceptance latest 均通过；当前仍有 ConfigValidator 元数据标签 warning 和锁层路径预期 warning。
- 版本规划已补“程序功能开发完成”识别口径：`09` 增加完成项识别入口和防重复派发规则，`11` 保持需求承接门禁；程序实际完成状态仍回到本状态页、开发文档、代码事实和验收证据。
- P2 深渊地图正式网络基础已落地：`DungeonLayer` 按 `RowCount` / 宽度 / seed 生成多行节点网络，`DungeonMapUIController` 改为展示多路线节点和连线，`DungeonManager.CanMoveToNode()` 在领域层限制入口 / 后继节点移动；`DungeonStairsProgressionTest.Run` 与 `ConfigValidationSmokeTest.Run` 已通过。
- P2 非战斗节点基础已落地：新增 `DungeonOutcomeNode`、`TreasureNode`、`EventNode`、`RestStopNode`、`HazardNode`，支持 `Title` / `Description` / `OutcomeEffects` / `RewardID` 配置，结果节点和节点奖励分别接入节点结果 UI 与战利品拾取流；`DungeonNodeTypesSmokeTest.Run`、`DungeonStairsProgressionTest.Run`、`ConfigValidationSmokeTest.Run` 已通过。
- P2 路线风险表达与战争迷雾基础已落地：新增 `DungeonMapVisibilityService`，支持 `FogProfile`、`NodeRevealDepth`、`NodePreviewDepth`、`RiskLevel`、`RiskHint` 配置，地图 UI 按 `Revealed` / `Preview` / `Hidden` 展示节点与路线；`DungeonMapVisibilitySmokeTest.Run`、`DungeonStairsProgressionTest.Run`、`ConfigValidationSmokeTest.Run` 已通过。
- P2 层间安全区节奏已落地：新增 `DungeonSafeZoneService`，`StairsNode` 进入时自动全恢复 HP / SAN，`SafeRoomNode.Rest()` 复用同一领域服务；`DungeonStairsProgressionTest.Run` 已覆盖恢复不清空本轮战利品账本。
- P2 固定 seed 验收底座已落地：新增 `DungeonSeedAcceptanceService`，可输出层级 / seed / MapProfile 的稳定摘要、Boss / Stairs 可达性和失败 issue；`DungeonSeedAcceptanceSmokeTest.Run`、`DungeonStairsProgressionTest.Run`、`ConfigValidationSmokeTest.Run` 已通过。
- P3 下潜许可服务已落地：新增 `DiveReadinessService`，统一检查层级解锁、出战人偶、极端磨损 / 侵蚀、底盘网格、运行时背包网格和义体引用合法性；`DungeonManager.StartRunAtLayer()` 已接入正式预检；`DiveReadinessSmokeTest.Run`、`DungeonStairsProgressionTest.Run`、`ConfigValidationSmokeTest.Run` 已通过。
- P3 维护真实服务已落地：新增 `MaintenanceService`、`WorkshopCostService`、`MaintenanceConfig`、`Maintenance` 配置域和 `MaintenanceServiceSmokeTest.Run`，支持金币 / 材料成本、磨损 / 侵蚀降低、HP / SAN 恢复，并由 `DiveReadinessService` 重新判断下潜许可。
- P3 材料缺口 / 成长反馈后端服务已落地：新增 `GrowthFeedbackService`，统一输出制造、维护、下潜许可的可执行状态、金币缺口、材料缺口和推荐动作；`GrowthFeedbackServiceSmokeTest.Run`、`MaintenanceServiceSmokeTest.Run`、`DiveReadinessSmokeTest.Run`、`ConfigValidationSmokeTest.Run` 已通过。
- P3 成长反馈可读性占位文本层已落地：新增 `GrowthReadabilityTextService`，把下潜许可、维护 / 制造缺口和建议行动整理为 UI 可直接展示的只读文本快照；`GrowthReadabilityTextServiceSmokeTest.Run` 已通过。
- P3 人偶核心状态可读性占位文本层已落地：新增 `DollCoreStateReadabilityService`，把 HP、SAN、情绪、维护风险、Bond、底盘、义体和特质整理为 UI 可直接展示的只读文本快照；`DollCoreStateReadabilityServiceSmokeTest.Run` 已通过。
- P3 人偶基础交互领域服务已落地：新增 `DollInteractionService` 和按日运行时计数，支持触摸、对话、赠礼的场景权限、每日上限、防刷、低 SAN 压力反馈和赠礼接受才消耗；`DollInteractionServiceSmokeTest.Run` 已通过。
- P3 人偶交互可读性占位文本层已落地：新增 `DollInteractionReadabilityTextService`，把单次触摸 / 对话 / 赠礼结果和当日交互计数整理为 UI 可直接展示的只读文本快照；`DollInteractionReadabilityTextServiceSmokeTest.Run` 已通过。
- P4 月租 / 账单压力链后端底座已落地：新增 `EconomyConfig`、`TownEconomyService`、`Economy` 配置域和 `TownEconomyServiceSmokeTest.Run`，支持日结报告、月租支付、轻度欠账、玩家选择典当补足和保护物不典当；`TownEconomyServiceSmokeTest.Run`、`ConfigValidationSmokeTest.Run` 已通过。
- P4 订单 / 声望 / 传闻价格波后端链路已落地：新增 `Factions`、`Orders`、`Rumors` 配置域和运行时状态，`TownEconomyService` 支持每周刷新、接单、交付、奖励、声望 / 信任变更、传闻出售倍率和通用出售结算；`TownEconomyServiceSmokeTest.Run`、`ConfigValidationSmokeTest.Run` 已通过。
- P4 小镇经济概览数据层已落地：新增 `TownEconomyOverviewService`，只读汇总可售物各渠道估值、订单进度、传闻、势力摘要、月租压力和典当候选，供占位 UI 或正式 UI 后续消费；`TownEconomyOverviewServiceSmokeTest.Run`、`TownEconomyServiceSmokeTest.Run`、`ConfigValidationSmokeTest.Run` 已通过。
- P4 小镇经济可读性占位文本层已落地：新增 `TownEconomyReadabilityTextService`，把日历、金币、月租压力、出售候选、订单、传闻、势力和典当候选整理为 UI 可直接展示的只读文本快照；`TownEconomyReadabilityTextServiceSmokeTest.Run`、`TownEconomyOverviewServiceSmokeTest.Run`、`TownEconomyServiceSmokeTest.Run` 已通过。
- P1 怪物意图只读预览数据层已落地：新增 `MonsterIntentPreviewService`，从当前战斗上下文输出怪物 HP / Shield、候选行动、可执行状态、阻塞原因、攻击 / 腐蚀武器 / 塞污染物的类型化意图数据，供占位 UI 或正式 UI 后续消费；`MonsterIntentPreviewServiceSmokeTest.Run` 覆盖预览不修改背包状态。
- P1 回合意图锁定已落地：`CombatSystem.StartPlayerTurn()` 会锁定每个存活怪物本轮行动，`MonsterIntentPreviewService` 优先显示锁定行动，`MonsterActionRunner.ExecuteTurn()` 优先执行同一行动；若锁定行动变得不可执行则本轮失败不重选；`MonsterActionAITest.Run` 已覆盖预览与执行一致性。
- P1 战斗结果报告数据层已落地：新增 `CombatOutcomeReport` / `CombatOutcomeReportService` / `CombatEventBus.OnCombatOutcomePrepared`，胜利、HP 战败和 SAN 崩溃均可输出 UI 可消费快照；`CombatOutcomeReportSmokeTest.Run` 在 Unity Editor 日志中三项用例通过。
- P1 SAN 崩溃战败判定已落地：新增 `CombatDefeatConditionService`，`CombatSystem` 和结果报告共用 HP / SAN / 阵营全灭判定；`MonsterActionAITest.Run`、`MonsterIntentPreviewServiceSmokeTest.Run`、`ConfigValidationSmokeTest.Run` 命令行通过。
- P1 战斗复盘时间线数据层已落地：新增 `CombatTimelineRecorder`，`CombatOutcomeReport.TimelineEvents` 可输出战斗开始、回合、伤害、物品干涉和胜负结算事件；`CombatOutcomeReportSmokeTest.Run` 在 Unity Editor 日志中通过时间线断言。
- P1 战斗可读性占位文本层已落地：新增 `CombatReadabilityTextService`，把怪物意图、战斗者状态和最近战斗记录整理为 UI 可直接展示的只读文本快照；`CombatReadabilityTextServiceSmokeTest.Run` 已通过。
- P1 战斗 HUD 意图消费第一段已落地：`HUDController` 在敌人槽中消费 `CombatReadabilityTextService.BuildIntentSnapshot()`，按 `MonsterFighter.RuntimeID` 显示锁定怪物意图、可执行状态和伤害描述，缺失快照时回退到 HP 文案；新增 `CombatHUDIntentBindingSmokeTest.Run` 覆盖 HUD 文本中可见“敌方意图 / 攻击 / 造成约 10 伤害”，单测已通过。
- P1 战斗结算 UI 消费第一段已落地：`SettlementUIController` 新增 `Present(CombatOutcomeReport, Action)` 只读展示入口，复用 Formal V1 结算皮肤显示战斗标题、胜负摘要、魔偶 HP / SAN、敌我存活和最近时间线；`CombatOutcomeReportSmokeTest.Run` 新增 `Combat Outcome Settlement UI Binding` 覆盖，直接 Unity 触发验证通过。
- P1 战斗结算主流程绑定已落地：`GameFlowController` 在失败结算时优先消费 `Combat.LastOutcomeReport` 的 Defeat 报告，撤离 / 胜利结算仍走 `DungeonSettlementResult`；`CombatOutcomeReportSmokeTest.Run` 新增 `Combat Outcome GameFlow Settlement Binding`，RED 证明旧流程仍显示副本失败结算，GREEN 直接 Unity 触发验证 `Status=PASSED`、`GameFlowBinding=PASSED`。
- Formal V1 工坊面板只读数据绑定第一版已落地：新增 `WorkshopFormalV1PanelBindingService`，`WorkshopFormalV1PanelController` 改为从成长、人偶、经济和交互只读快照服务取文案，不在 UI Controller 内持有玩法规则或改写状态；`WorkshopFormalV1PanelBindingSmokeTest.Run` 已通过，覆盖维护面板和日账单面板真实绑定与经济状态不变性。
- 已补充视觉 / UI 接入完成口径：`program_integrate=0` 和 ArtAcceptance 截图通过只代表资源登记与运行时截图覆盖，不等于玩家可玩接入；Formal V1 界面后续必须同时满足玩家主流程可达和真实后端 / 领域服务操作闭环，才能标记为可玩接入完成。
- Formal V1 工坊可玩接入第一段已落地：`WorkshopUIController` 新增玩家主流程入口区，可打开维护、账单、商店、订单、传闻、经营结算、底盘、人偶互动、人偶房间、势力商店和剧情事件面板；新增 `WorkshopFormalV1PanelActionService`，维护按钮调用 `MaintenanceService`，人偶互动按钮调用 `DollInteractionService`，账单 / 经营按钮仅做真实导航或关闭，不伪造未完成业务结果；`WorkshopFormalV1PanelBindingSmokeTest.Run`、`MaintenanceServiceSmokeTest.Run`、`DollInteractionServiceSmokeTest.Run` 已通过。
- 美术侧已把 V2-A 五个核心流程界面迁移为 active FormalV2 规格：`workshop_main`、`combat_hud`、`inventory_loot`、`dungeon_map`、`settlement`。程序侧下一轮 UI 表现工作可直接读取 `美术文档/ui_design/screen_layouts.json`，不要再按 `formal_v2/*.md` 草案接入。FormalV2 最新美术 handoff 已刷新：缺图批次 `nai_formalv2_missing_20260608_01` 已真实 NovelAI 生成并同步 58 个新增 Approved VisualID，后续 FormalV2 候选准入批次再同步 29 个新增 Approved VisualID；当前程序侧需要按 `美术文档/_generated/程序接入交接清单.md` 或 `美术文档/_generated/可接入素材清单.md` 登记 / 接入 latest `program_integrate=77`。美术侧已补齐全部新增 PNG 的 Unity `.meta`，并通过 `Generate-FormalV2AssetReview.ps1 -SnapshotTag formalv2_candidate_triage_after_meta_fix` 静态预验收：`reviewed=77`、`pass=77`、`fail=0`、`warn=0`。VisualV2 质量替换、technical_fix 和缺图生成队列均已清空；同名替换项保持原 VisualID / 路径 / `.meta`，程序侧无需重新登记。

## 下一步建议

1. 第一优先级 C1-C3 配置程序支持已完成当前可做审计和无阻断收口；后续只在 Unity runtime 补跑或人工体验发现真实阻断时按 bug 修复追加处理，不继续占用主线。
2. 第二优先级主流程可玩闭环已完成一轮程序补强批次：`FLOW-01..FLOW-07` 覆盖玩家路径缺口审计、层选择 / 下潜、地图节点、战斗 / 非战斗节点、战利品、阶梯 / 撤离、小镇出售、工坊维护和黄金路径 smoke / P0 摘要。后续重点不是重开基础线，而是做 Unity runtime / 人工体验复核；若发现正常 UI 不可达、按钮未调用真实服务或状态未回写，按对应 `FLOW-*` 追加 bug 修复或验收补强。
3. 主流程闭环的完成口径是“玩家正常 UI 可达 + 调用真实领域服务 + 状态真实变化 + 有 smoke / P0 证据”，不能用只读快照、debug preview、ArtAcceptance 截图或静态面板代替；服务已存在但 UI 不可达，仍然只能算“功能未开放完”。
4. 第三优先级程序架构优化 / 收口已完成 `ARCH-01..ARCH-05` 当前批次收口；`REVIEW-01` 已确认本轮 mission 的 TASK 证据完整。后续若继续处理 `GameFlowController` fallback UI、`DungeonManager` 结算职责或测试入口夹具，应作为新一轮架构任务单独派发，不重开已完成基础能力。
5. UI / 美术接入跟随上述功能闭环推进：有 Approved 资源和 active UI 规格时一起接入；没有资源或规格不适配时记录清楚缺口，不让美术返修压过 C1-C3 程序支持和主流程开放。

## 问题 / 阻塞

- 当前工作区仍有未纳入本次提交的无关改动：`tools/ai-image-gateway` 子模块内部 `implementation_plan.md` 已修改，`tools/ComfyUI_NAIDGenerator/` 为未跟踪本地工具目录；后续提交前继续严格收窄暂存范围。
- 当前环境 Unity Editor 未运行，`Invoke-P0Validation.ps1` 中 ConfigValidator 和 Unity smoke 只能记录为 `validation_limited:UnityEditorNotRunning`；这不是 C1-C3 JSON 硬断链或 `FLOW-05` 代码运行失败证据，后续打开 Unity 后应补跑 runtime 验证。
- 当前 ArtAcceptance latest `20260606_230523` 失败且早于 active UI / art specs；先前缺失的 `monster_boss_gatekeeper_mk1_combat`、`monster_mob_lost_miner_echo_combat`、`monster_mob_rust_hound_combat` 等战斗图已经由美术侧生成并进入 latest `program_integrate=77` 待登记队列，但尚未由程序登记进最新 Registry。程序登记后需要重跑 ArtAcceptance / VisualAsset 验收；旧失败不再代表当前 Approved 素材缺失事实。
- `FLOW-03` / `FLOW-04` / `FLOW-07` 新增或扩展的 `CombatLootDropTest.Run`、`DungeonNodeTypesSmokeTest.Run`、`DungeonStairsProgressionTest.Run` 与 `MainFlowGoldenPathSmokeTest.Run` 已接入 P0 默认 smoke 列表，但当前环境没有 Unity Editor，尚未取得运行态通过证据；后续打开 Unity 后需补跑 P0 或单独触发这些测试。
- 美术验收发现 latest ArtAcceptance 虽然工具层 `PASSED`，但部分截图存在跨界面残留和空列表状态，不能作为 Formal V1 画面通过证据；程序侧需按 `美术文档/09_运行时美术验收记录.md` 的 2026-05-27 条目返修并重跑。
- `tools/ai-image-gateway` 子模块内部有未提交改动。

## 完成回写清单

- 更新本文件的 `最后更新`。
- 在 `最近完成` 记录简短事实。
- 如有变化，刷新 `当前关注`、`下一步建议` 和阻塞项。
- 如果需要美术或策划跟进，在 `PROJECT_STATUS.md` 增加跨职能交接。
