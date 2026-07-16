using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class CombatLootDropTest {
    private static CombatLootPickupResult _preparedLootResult;
    private static CombatLootCollectionResult _lastCollectionResult;
    private static bool _nodeSettlementCompleted;

    public static void Run() {
        Debug.Log("=== Running Combat Loot Drop Test ===");

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;

        var doll = core.CurrentPlayer.ActiveDoll;
        doll.RuntimeGrid = new BackpackGrid(doll.Chassis);

        CombatNode combatNode = new CombatNode {
            NodeID = "test_combat_loot"
        };
        combatNode.MonsterIDs.Add("mob_scavenger_bug");
        combatNode.NextNodes.Add(new SafeRoomNode { NodeID = "test_next_node" });

        core.Dungeon.CurrentLayer = new DungeonLayer {
            LayerID = 1,
            RootNode = combatNode,
            CurrentNode = combatNode
        };

        _preparedLootResult = null;
        _lastCollectionResult = null;
        _nodeSettlementCompleted = false;
        DungeonEventBus.OnCombatLootPrepared += HandleCombatLootPrepared;
        DungeonEventBus.OnCombatLootCollected += HandleCombatLootCollected;
        DungeonEventBus.OnNodeSettlementCompleted += HandleNodeSettlementCompleted;

        int beforeCount = ((BackpackGrid)doll.RuntimeGrid).ContainedItems.Count;
        combatNode.ResolveAfterVictory();
        int afterPrepareCount = ((BackpackGrid)doll.RuntimeGrid).ContainedItems.Count;

        if (_preparedLootResult != null &&
            _preparedLootResult.OfferedItems.Count == 1 &&
            afterPrepareCount == beforeCount &&
            !_nodeSettlementCompleted) {
            Debug.Log("Combat Loot Preparation PASSED.");
        } else {
            Debug.LogError($"Combat Loot Preparation FAILED. Offered={_preparedLootResult?.OfferedItems.Count ?? 0}, BackpackCount={afterPrepareCount}, SettlementCompleted={_nodeSettlementCompleted}");
        }

        ItemEntity acceptedOfferedItem = null;
        bool placedOfferedItem = false;
        if (_preparedLootResult != null) {
            acceptedOfferedItem = _preparedLootResult.OfferedItems[0];
            placedOfferedItem = PlaceViaInventoryService(acceptedOfferedItem, 0, 0, "CombatLootAccept", out string placeReason);
            if (!placedOfferedItem) {
                Debug.LogError($"Combat Loot Confirmation setup FAILED. Could not place offered item through InventoryInteractionService: {placeReason}");
            }
            combatNode.ConfirmLootCollection();
        }

        int afterConfirmCount = ((BackpackGrid)doll.RuntimeGrid).ContainedItems.Count;
        bool acceptedCollectionResult = _lastCollectionResult != null
            && acceptedOfferedItem != null
            && _lastCollectionResult.AcceptedItems.Contains(acceptedOfferedItem)
            && _lastCollectionResult.DiscardedItems.Count == 0;
        if (placedOfferedItem && afterConfirmCount == beforeCount + 1 && _nodeSettlementCompleted && acceptedCollectionResult) {
            Debug.Log("Combat Loot Confirmation PASSED.");
        } else {
            Debug.LogError($"Combat Loot Confirmation FAILED. PlacedByService={placedOfferedItem}, Expected backpack count {beforeCount + 1}, got {afterConfirmCount}, SettlementCompleted={_nodeSettlementCompleted}, AcceptedResult={acceptedCollectionResult}");
        }

        CombatNode bossNode = new CombatNode {
            NodeID = "test_boss_reward_loot"
        };
        bossNode.MonsterIDs.Add("boss_gatekeeper_mk1");
        core.Dungeon.CurrentLayer = new DungeonLayer {
            LayerID = 1,
            RootNode = bossNode,
            CurrentNode = bossNode
        };

        _preparedLootResult = null;
        _nodeSettlementCompleted = false;
        int beforeBossCount = ((BackpackGrid)doll.RuntimeGrid).ContainedItems.Count;
        bossNode.ResolveAfterVictory();
        int afterBossPrepareCount = ((BackpackGrid)doll.RuntimeGrid).ContainedItems.Count;

        if (_preparedLootResult != null &&
            _preparedLootResult.OfferedItems.Count >= 2 &&
            HasOfferedItem(_preparedLootResult, "mat_core_tier1") &&
            afterBossPrepareCount == beforeBossCount &&
            !_nodeSettlementCompleted) {
            Debug.Log("Combat RewardSystem Integration PASSED.");
        } else {
            Debug.LogError($"Combat RewardSystem Integration FAILED. Offered={_preparedLootResult?.OfferedItems.Count ?? 0}, HasCore={HasOfferedItem(_preparedLootResult, "mat_core_tier1")}, BackpackCount={afterBossPrepareCount}, SettlementCompleted={_nodeSettlementCompleted}");
        }

        RunCombatLootConfirmationRequiresBackpackPlacement(core);

        DungeonEventBus.OnCombatLootPrepared -= HandleCombatLootPrepared;
        DungeonEventBus.OnCombatLootCollected -= HandleCombatLootCollected;
        DungeonEventBus.OnNodeSettlementCompleted -= HandleNodeSettlementCompleted;

        RunCombatLootBackpackDiscardUITest(core);
        RunCombatLootFormalV2LayoutTest(core);

        Debug.Log("=== Combat Loot Drop Test Finished ===");
    }

    private static void RunCombatLootFormalV2LayoutTest(CoreBackend core) {
        GameObject canvasObj = new GameObject("CombatLootFormalV2LayoutTestCanvas");
        canvasObj.AddComponent<Canvas>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panelObj = new GameObject("CombatLootFormalV2LayoutPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        panelObj.AddComponent<RectTransform>();
        panelObj.AddComponent<Image>();
        CombatLootUIController controller = panelObj.AddComponent<CombatLootUIController>();

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        controller.titleText = CreateTestText("Title_Text", panelObj.transform, font);
        controller.summaryText = CreateTestText("Summary_Text", panelObj.transform, font);
        GameObject lootArea = new GameObject("LootArea");
        lootArea.transform.SetParent(panelObj.transform, false);
        lootArea.AddComponent<RectTransform>();
        controller.lootParent = lootArea.transform;
        controller.continueBtn = CreateTestButton("Continue_Button", panelObj.transform, font);

        GameObject itemPrefab = CreateLootItemPrefab();
        CombatLootPickupResult result = new CombatLootPickupResult {
            NodeID = "formal_v2_layout",
            TotalEstimatedValue = 42
        };
        result.OfferedItems.Add(ConfigManager.CreateItem("loot_gear_scrap"));
        result.OfferedItems.Add(ConfigManager.CreateItem("loot_toxic_filter"));
        result.OfferedItems.Add(ConfigManager.CreateItem("mat_core_tier1"));

        controller.Present(result, itemPrefab, null);

        RectTransform lootRect = controller.lootParent as RectTransform;
        RectTransform buttonRect = controller.continueBtn.GetComponent<RectTransform>();
        RectTransform pickupPanelRect = FindRectRecursive(panelObj.transform, "PickupPanel");
        RectTransform dropZoneRect = FindRectRecursive(panelObj.transform, "LootDropZone");
        InventoryLayoutProfile combatLootProfile = InventoryDisplaySpec.ResolveLayoutProfile(InventoryPresentationMode.CombatLoot);
        Image pickupPanelImage = pickupPanelRect != null ? pickupPanelRect.GetComponent<Image>() : null;

        int lootVisualCount = 0;
        bool rewardsAvoidBackpackCenter = true;
        foreach (Transform child in controller.lootParent) {
            RectTransform itemRect = child as RectTransform;
            if (itemRect == null) {
                continue;
            }

            lootVisualCount++;
            Vector2 position = itemRect.anchoredPosition;
            bool inBackpackCenter = Mathf.Abs(position.x) < 360f && Mathf.Abs(position.y) < 250f;
            rewardsAvoidBackpackCenter &= !inBackpackCenter;
        }

        bool passed = lootRect != null
            && lootRect.sizeDelta.x >= 1400f
            && lootRect.sizeDelta.y >= 700f
            && Mathf.Abs(lootRect.anchoredPosition.x) < 0.1f
            && Mathf.Abs(combatLootProfile.AnchoredPosition.x) < 0.1f
            && Mathf.Abs(buttonRect.anchoredPosition.x) < 0.1f
            && pickupPanelRect != null
            && pickupPanelRect.sizeDelta.x <= 1300f
            && pickupPanelImage != null
            && pickupPanelImage.color.a <= 0.4f
            && dropZoneRect != null
            && Mathf.Abs(dropZoneRect.anchoredPosition.x) < 0.1f
            && lootVisualCount == result.OfferedItems.Count
            && rewardsAvoidBackpackCenter;

        if (passed) {
            Debug.Log("Combat Loot FormalV2 Layout PASSED.");
        } else {
            Debug.LogError($"Combat Loot FormalV2 Layout FAILED. LootArea={lootRect?.sizeDelta.ToString() ?? "null"} Pos={lootRect?.anchoredPosition.ToString() ?? "null"}, Profile={combatLootProfile.AnchoredPosition}, ButtonX={buttonRect.anchoredPosition.x}, Pickup={pickupPanelRect?.sizeDelta.ToString() ?? "null"} Alpha={pickupPanelImage?.color.a ?? -1f}, DropZone={dropZoneRect?.anchoredPosition.ToString() ?? "null"}, LootVisuals={lootVisualCount}/{result.OfferedItems.Count}, AvoidCenter={rewardsAvoidBackpackCenter}");
        }

        UnityEngine.Object.DestroyImmediate(itemPrefab);
        UnityEngine.Object.DestroyImmediate(canvasObj);
    }

    private static void RunCombatLootBackpackDiscardUITest(CoreBackend core) {
        GameObject canvasObj = new GameObject("CombatLootDiscardUITestCanvas");
        canvasObj.AddComponent<Canvas>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameFlowController previousFlow = GameFlowController.Instance;
        GameObject flowObj = new GameObject("GameFlowController");
        GameFlowController flow = flowObj.AddComponent<GameFlowController>();
        SetGameFlowInstance(flow);
        try {
            SetGameFlowScreen(flow, "CombatLoot");
            InventoryPresentationController presentation = flowObj.AddComponent<InventoryPresentationController>();
            presentation.SetContext(InventoryPresentationMode.CombatLoot);

            var doll = core.CurrentPlayer.ActiveDoll;
            doll.RuntimeGrid = new BackpackGrid(doll.Chassis);
            BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;
            ItemEntity backpackItem = ConfigManager.CreateItem("loot_gear_scrap");
            string placeReason = string.Empty;
            bool placed = grid != null
                && backpackItem != null
                && PlaceViaInventoryService(backpackItem, 0, 0, "CombatLootDiscardUITestSetup", out placeReason);
            if (!placed) {
                Debug.LogError($"Combat Loot Backpack Discard UI FAILED. Could not place test backpack item through InventoryInteractionService: {placeReason}");
                return;
            }

            GameObject slotObj = new GameObject("Slot_0_0");
            slotObj.transform.SetParent(canvasObj.transform, false);
            slotObj.AddComponent<RectTransform>();

            GameObject itemObj = new GameObject("BackpackItemUI");
            itemObj.transform.SetParent(canvasObj.transform, false);
            itemObj.AddComponent<RectTransform>();
            itemObj.AddComponent<Image>();
            itemObj.AddComponent<CanvasGroup>();
            DraggableItemUI itemUI = itemObj.AddComponent<DraggableItemUI>();
            itemUI.SetupData(backpackItem);
            itemUI.SnapToSlot(slotObj.transform, 0, 0);

            PointerEventData eventData = new PointerEventData(null) {
                position = new Vector2(32f, 32f)
            };

            itemUI.OnBeginDrag(eventData);
            itemUI.OnEndDrag(eventData);

            bool canStage = flow.CanStageRemovedBackpackItems();
            bool stagedForDiscard = itemUI != null && itemUI.IsPendingDiscard;
            bool removedFromGrid = placed && grid != null && !grid.ContainedItems.Contains(backpackItem);

            InvokeDiscardDetachedBackpackItems(flow);
            bool queuedOrDestroyedAfterDiscard = Application.isPlaying || itemUI == null;

            if (canStage && stagedForDiscard && removedFromGrid && queuedOrDestroyedAfterDiscard) {
                Debug.Log("Combat Loot Backpack Discard UI PASSED.");
            } else {
                Debug.LogError($"Combat Loot Backpack Discard UI FAILED. CanStage={canStage}, Pending={stagedForDiscard}, RemovedFromGrid={removedFromGrid}, QueuedOrDestroyed={queuedOrDestroyedAfterDiscard}");
            }
        } finally {
            SetGameFlowInstance(previousFlow);
            UnityEngine.Object.DestroyImmediate(canvasObj);
            UnityEngine.Object.DestroyImmediate(flowObj);
        }
    }

    private static void RunCombatLootConfirmationRequiresBackpackPlacement(CoreBackend core) {
        var doll = core.CurrentPlayer.ActiveDoll;
        doll.RuntimeGrid = new BackpackGrid(doll.Chassis);
        BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;

        CombatNode discardNode = new CombatNode {
            NodeID = "test_unplaced_loot_discard"
        };
        discardNode.MonsterIDs.Add("mob_scavenger_bug");
        core.Dungeon.CurrentLayer = new DungeonLayer {
            LayerID = 1,
            RootNode = discardNode,
            CurrentNode = discardNode
        };

        _preparedLootResult = null;
        _lastCollectionResult = null;
        _nodeSettlementCompleted = false;

        int beforeCount = grid?.ContainedItems.Count ?? -1;
        discardNode.ResolveAfterVictory();
        int afterPrepareCount = grid?.ContainedItems.Count ?? -1;
        ItemEntity offeredItem = _preparedLootResult?.OfferedItems.Count > 0
            ? _preparedLootResult.OfferedItems[0]
            : null;

        discardNode.ConfirmLootCollection();
        int afterConfirmCount = grid?.ContainedItems.Count ?? -1;
        bool discardedCollectionResult = _lastCollectionResult != null
            && offeredItem != null
            && _lastCollectionResult.AcceptedItems.Count == 0
            && _lastCollectionResult.DiscardedItems.Contains(offeredItem);

        if (_preparedLootResult != null
            && offeredItem != null
            && afterPrepareCount == beforeCount
            && afterConfirmCount == beforeCount
            && _nodeSettlementCompleted
            && discardedCollectionResult) {
            Debug.Log("Combat Loot Unplaced Confirmation PASSED.");
        } else {
            Debug.LogError($"Combat Loot Unplaced Confirmation FAILED. Offered={_preparedLootResult?.OfferedItems.Count ?? 0}, Before={beforeCount}, AfterPrepare={afterPrepareCount}, AfterConfirm={afterConfirmCount}, SettlementCompleted={_nodeSettlementCompleted}, DiscardedResult={discardedCollectionResult}");
        }
    }

    private static void SetGameFlowScreen(GameFlowController controller, string screenName) {
        Type screenType = typeof(GameFlowController).GetNestedType("GameScreenState", BindingFlags.NonPublic);
        object state = Enum.Parse(screenType, screenName);
        typeof(GameFlowController)
            .GetField("_currentScreen", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, state);
    }

    private static void SetGameFlowInstance(GameFlowController controller) {
        typeof(GameFlowController)
            .GetProperty("Instance", BindingFlags.Static | BindingFlags.Public)
            .GetSetMethod(true)
            .Invoke(null, new object[] { controller });
    }

    private static void InvokeDiscardDetachedBackpackItems(GameFlowController controller) {
        typeof(GameFlowController)
            .GetMethod("DiscardDetachedBackpackItems", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(controller, null);
    }

    private static Text CreateTestText(string objectName, Transform parent, Font font) {
        GameObject textObj = new GameObject(objectName);
        textObj.transform.SetParent(parent, false);
        textObj.AddComponent<RectTransform>();
        Text text = textObj.AddComponent<Text>();
        text.font = font;
        return text;
    }

    private static Button CreateTestButton(string objectName, Transform parent, Font font) {
        GameObject buttonObj = new GameObject(objectName);
        buttonObj.transform.SetParent(parent, false);
        buttonObj.AddComponent<RectTransform>();
        buttonObj.AddComponent<Image>();
        Button button = buttonObj.AddComponent<Button>();
        Text label = CreateTestText("Text", buttonObj.transform, font);
        label.text = objectName;
        return button;
    }

    private static GameObject CreateLootItemPrefab() {
        GameObject prefab = new GameObject("LootItemPrefab_Test");
        RectTransform rect = prefab.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(96f, 96f);
        prefab.AddComponent<Image>();
        prefab.AddComponent<CanvasGroup>();
        prefab.AddComponent<DraggableItemUI>();
        return prefab;
    }

    private static RectTransform FindRectRecursive(Transform root, string objectName) {
        if (root == null) {
            return null;
        }

        if (root.name == objectName) {
            return root as RectTransform;
        }

        for (int i = 0; i < root.childCount; i++) {
            RectTransform match = FindRectRecursive(root.GetChild(i), objectName);
            if (match != null) {
                return match;
            }
        }

        return null;
    }

    private static bool PlaceViaInventoryService(ItemEntity item, int x, int y, string source, out string reason) {
        InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll(source);
        return InventoryInteractionService.RequestPlace(item, x, y, context, out reason);
    }

    private static bool HasOfferedItem(CombatLootPickupResult result, string configID) {
        if (result?.OfferedItems == null) {
            return false;
        }

        foreach (ItemEntity item in result.OfferedItems) {
            if (item != null && item.ConfigID == configID) {
                return true;
            }
        }

        return false;
    }

    private static void HandleCombatLootPrepared(CombatLootPickupResult result) {
        _preparedLootResult = result;
        Debug.Log($"Received combat loot payload. OfferedCount={result?.OfferedItems.Count ?? 0}");
    }

    private static void HandleCombatLootCollected(CombatLootCollectionResult result) {
        _lastCollectionResult = result;
        Debug.Log($"Received combat loot collection. Accepted={result?.AcceptedItems.Count ?? 0}, Discarded={result?.DiscardedItems.Count ?? 0}");
    }

    private static void HandleNodeSettlementCompleted() {
        _nodeSettlementCompleted = true;
        Debug.Log("Received OnNodeSettlementCompleted event.");
    }
}
