using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ArtAcceptanceRunner 的分步截图实现。
/// 包含 21 个 Capture 方法，每个方法驱动一个验收截图点的界面切换、稳定等待和截图触发。
/// </summary>
public partial class ArtAcceptanceRunner {

    // ──────────────────────────────────────────
    // Category A: 真实游戏流截图
    // ──────────────────────────────────────────

    private IEnumerator CaptureWorkshopMain() {
        Debug.Log("[ArtAcceptance] Capturing workshop_main...");
        ArtAcceptanceCaptureRecord capture = BeginCapture("workshop_main", "screenshots/workshop_main.png");

        if (!RequireRuntimeCore(capture) || !RequireFlowController(capture)) {
            CompleteSkipped(capture);
            yield break;
        }

        GameFlowController.Instance.EnterWorkshop();
        yield return WaitForVisualStable();
        yield return CaptureCurrentScreen(capture);
    }

    private IEnumerator CaptureSellPanel() {
        Debug.Log("[ArtAcceptance] Capturing sell_panel...");
        ArtAcceptanceCaptureRecord capture = BeginCapture("sell_panel", "screenshots/sell_panel.png");

        if (!RequireRuntimeCore(capture) || !RequireFlowController(capture)) {
            CompleteSkipped(capture);
            yield break;
        }

        GameFlowController.Instance.EnterWorkshop();
        yield return WaitForVisualStable();

        WorkshopUIController workshopController = FindObjectOfType<WorkshopUIController>();
        if (workshopController == null) {
            capture.Warnings.Add("WorkshopUIController not found for sell panel capture.");
            CompleteSkipped(capture);
            yield break;
        }

        workshopController.OpenSellPanel();
        yield return WaitForVisualStable();
        yield return CaptureCurrentScreen(capture);
        CloseWorkshopAcceptanceOverlays(workshopController);
    }

    private IEnumerator CaptureProstheticPanel() {
        Debug.Log("[ArtAcceptance] Capturing prosthetic_panel...");
        ArtAcceptanceCaptureRecord capture = BeginCapture("prosthetic_panel", "screenshots/prosthetic_panel.png");

        if (!RequireRuntimeCore(capture) || !RequireFlowController(capture)) {
            CompleteSkipped(capture);
            yield break;
        }

        GameFlowController.Instance.EnterWorkshop();
        yield return WaitForVisualStable();

        WorkshopUIController workshopController = FindObjectOfType<WorkshopUIController>();
        if (workshopController == null) {
            capture.Warnings.Add("WorkshopUIController not found for prosthetic panel capture.");
            CompleteSkipped(capture);
            yield break;
        }

        workshopController.OpenProstheticPanel();
        yield return WaitForVisualStable();
        yield return CaptureCurrentScreen(capture);
        CloseWorkshopAcceptanceOverlays(workshopController);
    }

    private IEnumerator CaptureLayerSelect() {
        Debug.Log("[ArtAcceptance] Capturing layer_select...");
        ArtAcceptanceCaptureRecord capture = BeginCapture("layer_select", "screenshots/layer_select.png");

        if (!RequireRuntimeCore(capture) || !RequireFlowController(capture) || !RequireDungeon(capture)) {
            CompleteSkipped(capture);
            yield break;
        }

        int previousHighestUnlockedLayer = GameRoot.Core.CurrentPlayer.HighestUnlockedDungeonLayer;
        UnlockConfiguredLayersForAcceptance();
        GameFlowController.Instance.EnterWorkshop();
        yield return WaitForVisualStable();

        WorkshopUIController workshopController = FindObjectOfType<WorkshopUIController>();
        if (workshopController == null) {
            GameRoot.Core.CurrentPlayer.HighestUnlockedDungeonLayer = previousHighestUnlockedLayer;
            capture.Warnings.Add("WorkshopUIController not found for layer select capture.");
            CompleteSkipped(capture);
            yield break;
        }

        workshopController.OpenDungeonStartLayerPanel();
        yield return WaitForVisualStable();
        yield return CaptureCurrentScreen(capture);
        CloseWorkshopAcceptanceOverlays(workshopController);
        GameRoot.Core.CurrentPlayer.HighestUnlockedDungeonLayer = previousHighestUnlockedLayer;
    }

    private IEnumerator CaptureDungeonMap() {
        Debug.Log("[ArtAcceptance] Capturing dungeon_map...");
        ArtAcceptanceCaptureRecord capture = BeginCapture("dungeon_map", "screenshots/dungeon_map.png");

        if (!RequireRuntimeCore(capture) || !RequireFlowController(capture) || !RequireDungeon(capture)) {
            CompleteSkipped(capture);
            yield break;
        }

        int layerID = ResolveAcceptanceLayerID();
        if (layerID <= 0) {
            capture.Warnings.Add("No unlocked dungeon layer config found.");
            CompleteSkipped(capture);
            yield break;
        }

        bool started = GameRoot.Core.Dungeon.StartRunAtLayer(layerID);
        if (!started) {
            capture.Warnings.Add($"Dungeon.StartRunAtLayer({layerID}) returned false.");
            CompleteSkipped(capture);
            yield break;
        }

        // Switch the visible screen to DungeonMap panel.
        // Without this, the workshopPanel remains active and the screenshot captures workshop instead.
        GameFlowController.Instance.EnterDungeonMap();
        yield return WaitForVisualStable();
        DungeonMapUIController mapController = FindObjectOfType<DungeonMapUIController>();
        if (mapController != null) {
            mapController.RefreshMap();
        }

        yield return WaitForVisualStable();
        yield return CaptureCurrentScreen(capture);
    }

    private IEnumerator CaptureCombatHud() {
        Debug.Log("[ArtAcceptance] Capturing combat_hud...");
        ArtAcceptanceCaptureRecord capture = BeginCapture("combat_hud", "screenshots/combat_hud.png");

        if (!RequireRuntimeCore(capture) || !RequireFlowController(capture) || GameRoot.Core?.Combat == null) {
            if (GameRoot.Core?.Combat == null) {
                capture.Errors.Add("CombatSystem is missing.");
                AddError($"Capture [{capture.ScreenTag}] requires CombatSystem.");
            }
            CompleteSkipped(capture);
            yield break;
        }

        List<string> monsterIDs = ResolveAcceptanceMonsterIDs();
        if (monsterIDs.Count == 0) {
            capture.Warnings.Add("No monster config found for combat HUD capture.");
            CompleteSkipped(capture);
            yield break;
        }

        GameRoot.Core.Combat.StartCombat(monsterIDs);
        GameFlowController.Instance.EnterCombat();
        yield return WaitForVisualStable();
        yield return CaptureCurrentScreen(capture);
    }

    // ──────────────────────────────────────────
    // Category B: 验收专用构造 payload 截图
    // ──────────────────────────────────────────

    private IEnumerator CaptureSafeRoom() {
        Debug.Log("[ArtAcceptance] Capturing safe_room...");
        ArtAcceptanceCaptureRecord capture = BeginCapture("safe_room", "screenshots/safe_room.png", "acceptance_preview");

        if (!RequireRuntimeCore(capture) || !RequireFlowController(capture)) {
            CompleteSkipped(capture);
            yield break;
        }

        SafeRoomNode node = new SafeRoomNode {
            NodeID = "art_acceptance_safe_room_preview"
        };

        GameFlowController.Instance.EnterSafeRoom(node);
        yield return WaitForVisualStable();
        yield return CaptureCurrentScreen(capture);
    }

    private IEnumerator CaptureStairsRoom() {
        Debug.Log("[ArtAcceptance] Capturing stairs_room...");
        ArtAcceptanceCaptureRecord capture = BeginCapture("stairs_room", "screenshots/stairs_room.png", "acceptance_preview");

        if (!RequireRuntimeCore(capture) || !RequireFlowController(capture) || !RequireDungeon(capture)) {
            CompleteSkipped(capture);
            yield break;
        }

        EnsureAcceptanceDungeonLayer();
        int layerID = GameRoot.Core?.Dungeon?.CurrentLayer?.LayerID ?? ResolveAcceptanceLayerID();
        StairsNode node = new StairsNode {
            NodeID = "art_acceptance_stairs_room_preview",
            LayerID = Mathf.Max(1, layerID)
        };

        GameFlowController.Instance.EnterStairs(node);
        yield return WaitForVisualStable();
        yield return CaptureCurrentScreen(capture);
    }

    private IEnumerator CaptureInventoryLoot() {
        Debug.Log("[ArtAcceptance] Capturing inventory_loot...");
        ArtAcceptanceCaptureRecord capture = BeginCapture("inventory_loot", "screenshots/inventory_loot.png", "acceptance_preview");

        if (!RequireRuntimeCore(capture) || !RequireFlowController(capture)) {
            CompleteSkipped(capture);
            yield break;
        }

        CombatLootPickupResult lootResult = BuildAcceptanceLootResult();
        if (lootResult == null || lootResult.OfferedItems.Count == 0) {
            capture.Warnings.Add("No item config found for inventory loot capture.");
            CompleteSkipped(capture);
            yield break;
        }

        Debug.Log("[ArtAcceptance] inventory_loot uses an acceptance-only loot payload; real reward settlement is not invoked.");
        GameFlowController.Instance.EnterCombatLoot(lootResult);
        yield return WaitForVisualStable();
        yield return CaptureCurrentScreen(capture);
    }

    private IEnumerator CaptureSettlement() {
        Debug.Log("[ArtAcceptance] Capturing settlement...");
        ArtAcceptanceCaptureRecord capture = BeginCapture("settlement", "screenshots/settlement.png", "acceptance_preview");

        if (!RequireRuntimeCore(capture) || !RequireFlowController(capture)) {
            CompleteSkipped(capture);
            yield break;
        }

        DungeonSettlementResult settlementResult = BuildAcceptanceSettlementResult();
        if (settlementResult == null) {
            capture.Warnings.Add("Failed to build settlement preview payload.");
            CompleteSkipped(capture);
            yield break;
        }

        GameFlowController.Instance.EnterSettlementPreview(settlementResult);
        yield return WaitForVisualStable();
        yield return CaptureCurrentScreen(capture);
    }

    // ──────────────────────────────────────────
    // Category C: Formal V1 模板面板截图
    // 降级兜底机制 (Degraded Fallback):
    //   每个面板先检测是否存在独立 Controller；
    //   存在 → 走 real_gameplay 路径；
    //   不存在 → 降级到 formal_v1_template 模板渲染。
    // ──────────────────────────────────────────

    private IEnumerator CaptureMaintenancePanel() {
        yield return CaptureWorkshopFormalV1Panel("maintenance_panel", "screenshots/maintenance_panel.png", "MaintenancePanelController");
    }

    private IEnumerator CaptureDailyBillReport() {
        yield return CaptureWorkshopFormalV1Panel("daily_bill_report", "screenshots/daily_bill_report.png", "DailyBillReportController");
    }

    private IEnumerator CaptureShopStaging() {
        yield return CaptureWorkshopFormalV1Panel("shop_staging", "screenshots/shop_staging.png", "ShopStagingController");
    }

    private IEnumerator CaptureOrderBoard() {
        yield return CaptureWorkshopFormalV1Panel("order_board", "screenshots/order_board.png", "OrderBoardController");
    }

    private IEnumerator CaptureRumorBoard() {
        yield return CaptureWorkshopFormalV1Panel("rumor_board", "screenshots/rumor_board.png", "RumorBoardController");
    }

    private IEnumerator CaptureBusinessSettlement() {
        yield return CaptureWorkshopFormalV1Panel("business_settlement", "screenshots/business_settlement.png", "BusinessSettlementController");
    }

    private IEnumerator CaptureChassisUpgradePanel() {
        yield return CaptureWorkshopFormalV1Panel("chassis_upgrade_panel", "screenshots/chassis_upgrade_panel.png", "ChassisUpgradeController");
    }

    private IEnumerator CaptureDollInteraction() {
        yield return CaptureWorkshopFormalV1Panel("doll_interaction", "screenshots/doll_interaction.png", "DollInteractionController");
    }

    private IEnumerator CaptureDollRoom() {
        yield return CaptureWorkshopFormalV1Panel("doll_room", "screenshots/doll_room.png", "DollRoomController");
    }

    private IEnumerator CaptureFactionShop() {
        yield return CaptureWorkshopFormalV1Panel("faction_shop", "screenshots/faction_shop.png", "FactionShopController");
    }

    private IEnumerator CaptureScenarioEvent() {
        yield return CaptureWorkshopFormalV1Panel("scenario_event", "screenshots/scenario_event.png", "ScenarioEventController");
    }

    /// <summary>
    /// V1 降级兜底截图方法。
    /// 优先检测 formalControllerName 对应的独立 Controller；
    /// 若存在，通过反射调用其 Open 方法，标记 DataSource 为 "real_gameplay"；
    /// 若不存在，降级到通用 WorkshopFormalV1PanelController 模板渲染，标记 "formal_v1_template"。
    /// </summary>
    private IEnumerator CaptureWorkshopFormalV1Panel(string screenTag, string relativeScreenshotPath, string formalControllerName = null) {
        Debug.Log($"[ArtAcceptance] Capturing {screenTag}...");

        // 前置检查：运行时核心和 FlowController 必须就绪
        bool coreReady = _runtimeCoreReady ||
            (GameRoot.Core != null && GameRoot.Core.CurrentPlayer != null && GameRoot.Core.CurrentPlayer.ActiveDoll != null);
        bool flowReady = GameFlowController.Instance != null;

        if (!coreReady || !flowReady) {
            ArtAcceptanceCaptureRecord skipCapture = BeginCapture(screenTag, relativeScreenshotPath, "formal_v1_template");
            if (!coreReady) {
                skipCapture.Errors.Add($"Runtime core is missing. {BuildRuntimeReadinessSummary()}");
                AddError($"Capture [{screenTag}] requires runtime core.");
            }
            if (!flowReady) {
                skipCapture.Errors.Add("GameFlowController.Instance is missing.");
                AddError($"Capture [{screenTag}] requires GameFlowController.");
            }
            CompleteSkipped(skipCapture);
            yield break;
        }

        GameFlowController.Instance.EnterWorkshop();
        yield return WaitForVisualStable();

        // 尝试独立 Controller 路径
        bool usedFormalController = false;
        if (!string.IsNullOrEmpty(formalControllerName)) {
            MonoBehaviour formalController = TryFindFormalController(formalControllerName);
            if (formalController != null) {
                ArtAcceptanceCaptureRecord capture = BeginCapture(screenTag, relativeScreenshotPath, "real_gameplay");
                Debug.Log($"[ArtAcceptance] {screenTag}: Found independent controller {formalControllerName}, using real_gameplay path.");

                // 通过反射调用 Open 方法 (约定: 独立 Controller 提供 Open<PanelName>View() 或 Open() 方法)
                var openMethod = formalController.GetType().GetMethod("Open",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                if (openMethod != null) {
                    openMethod.Invoke(formalController, null);
                }

                yield return WaitForVisualStable();
                yield return CaptureCurrentScreen(capture);
                usedFormalController = true;
            }
        }

        // 降级到 V1 模板渲染
        if (!usedFormalController) {
            ArtAcceptanceCaptureRecord capture = BeginCapture(screenTag, relativeScreenshotPath, "formal_v1_template");

            WorkshopUIController workshopController = FindObjectOfType<WorkshopUIController>();
            if (workshopController == null) {
                capture.Warnings.Add($"WorkshopUIController not found for {screenTag} capture.");
                CompleteSkipped(capture);
                yield break;
            }

            workshopController.OpenFormalV1Panel(screenTag);
            yield return WaitForVisualStable();
            yield return CaptureCurrentScreen(capture);
            CloseWorkshopAcceptanceOverlays(workshopController);
        }
    }

    /// <summary>
    /// 按类名动态查找独立 Controller。使用 FindObjectsOfType 遍历场景中所有 MonoBehaviour，
    /// 匹配类名。这避免了硬编码 Type 引用，支持未来增量添加新 Controller。
    /// </summary>
    private MonoBehaviour TryFindFormalController(string controllerTypeName) {
        if (string.IsNullOrEmpty(controllerTypeName)) {
            return null;
        }

        MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours) {
            if (behaviour != null &&
                behaviour.isActiveAndEnabled &&
                behaviour.GetType().Name == controllerTypeName) {
                return behaviour;
            }
        }

        return null;
    }
}
