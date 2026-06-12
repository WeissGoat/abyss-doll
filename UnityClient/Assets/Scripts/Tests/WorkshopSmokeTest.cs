using UnityEngine;
using UnityEngine.UI;

public static class WorkshopSmokeTest {
    public static void Run() {
        try {
            Debug.Log("=== Running Workshop Smoke Test ===");

            CoreBackend core = new CoreBackend();
            core.InitAllSystems();

            GameRoot.Core = core;

            var player = core.CurrentPlayer;
            var doll = player.ActiveDoll;

            if (doll == null || doll.Chassis == null) {
                Debug.LogError("Bootstrap failed: ActiveDoll or Chassis is null.");
                return;
            }

            BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;
            ItemEntity sellTarget = grid?.ContainedItems.Find(item => item != null && item.ConfigID == "gear_wooden_shield");
            if (grid == null || sellTarget == null) {
                Debug.LogError("Backpack sell bootstrap failed: missing runtime grid or wooden shield.");
                return;
            }

            Debug.Log($"[Before] Chassis: {doll.Chassis.ChassisID} (Grid: {doll.Chassis.GridWidth}x{doll.Chassis.GridHeight}), Money: {player.Money}");

            int initialBackpackCount = grid.ContainedItems.Count;
            int initialMoney = player.Money;
            int[] soldPosition = sellTarget.Grid?.CurrentPos != null && sellTarget.Grid.CurrentPos.Length >= 2
                ? new[] { sellTarget.Grid.CurrentPos[0], sellTarget.Grid.CurrentPos[1] }
                : null;
            EconomySellLine expectedSellLine = TownEconomyService.CalculateItemSellValue(player, sellTarget, EconomySellChannel.DumpBox);
            EconomySellReport sellReport = TownEconomyService.SellItems(player, new[] { sellTarget }, EconomySellChannel.DumpBox);
            bool sold = sellReport.Success && sellReport.SoldItems.Count == 1;

            if (sold && player.Money == initialMoney + expectedSellLine.FinalValue) {
                Debug.Log("Single Item Sell PASSED.");
            } else {
                Debug.LogError($"Single Item Sell FAILED. Expected money {initialMoney + expectedSellLine.FinalValue}, got {player.Money}, Sold={sold}, Reason={sellReport.Reason}");
            }

            bool soldOriginCleared = soldPosition == null || grid.GetItemAt(soldPosition[0], soldPosition[1]) == null;
            if (grid.ContainedItems.Count == initialBackpackCount - 1 && soldOriginCleared) {
                Debug.Log("Single Item Backpack Removal PASSED.");
            } else {
                Debug.LogError($"Single Item Backpack Removal FAILED. Expected backpack count {initialBackpackCount - 1}, got {grid.ContainedItems.Count}, OriginCleared={soldOriginCleared}");
            }

            player.Money = 1500;
            var coreMaterial = ConfigManager.CreateItem("mat_core_tier1");
            if (coreMaterial == null) {
                Debug.LogError("Config 'mat_core_tier1' not found. Ensure the JSON exists.");
                return;
            }
            player.StashInventory.Add(coreMaterial);

            ChassisUpgradeResult upgradeResult = core.Workshop.UpgradeDollChassis(doll);

            if (player.Money == 500) {
                Debug.Log("Money Deduction PASSED.");
            } else {
                Debug.LogError($"Money Deduction FAILED. Expected 500, got {player.Money}");
            }

            if (player.StashInventory.Count == 0) {
                Debug.Log("Material Deduction PASSED.");
            } else {
                Debug.LogError($"Material Deduction FAILED. Expected 0 items, got {player.StashInventory.Count}");
            }

            if (doll.Chassis.ChassisID == "chassis_lv2_expanded" && doll.Chassis.GridWidth == 5 && doll.Chassis.GridHeight == 5) {
                Debug.Log("Chassis Upgrade PASSED.");
            } else {
                Debug.LogError($"Chassis Upgrade FAILED. Current Chassis: {doll.Chassis.ChassisID} ({doll.Chassis.GridWidth}x{doll.Chassis.GridHeight})");
            }

            bool upgradeResultPassed = upgradeResult.Success
                && upgradeResult.PreviousChassisID == "chassis_lv1_basic"
                && upgradeResult.NewChassisID == "chassis_lv2_expanded"
                && upgradeResult.MoneySpent == 1000
                && upgradeResult.ConsumedItems.Count == 1
                && upgradeResult.RuntimeGridRebuilt;
            if (upgradeResultPassed) {
                Debug.Log("Chassis Upgrade Result DTO PASSED.");
            } else {
                Debug.LogError($"Chassis Upgrade Result DTO FAILED. Success={upgradeResult.Success}, From={upgradeResult.PreviousChassisID}, To={upgradeResult.NewChassisID}, Money={upgradeResult.MoneySpent}, Items={upgradeResult.ConsumedItems.Count}, GridRebuilt={upgradeResult.RuntimeGridRebuilt}, Reason={upgradeResult.Reason}");
            }

            RunProstheticCraftAndEffectTest(core);
            RunWorkshopSellPanelUITest(core);
            RunWorkshopProstheticPanelUITest(core);
            RunWorkshopProstheticLockedRowsUITest(core);
            RunWorkshopFormalV2HubLayoutTest(core);

            Debug.Log("=== Workshop Smoke Test Finished ===");
        } catch (System.Exception ex) {
            Debug.LogError($"[WorkshopSmokeTest Crash] {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static void RunWorkshopSellPanelUITest(CoreBackend core) {
        GameObject canvasObj = new GameObject("WorkshopSellUITestCanvas");
        canvasObj.AddComponent<Canvas>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject workshopObj = new GameObject("WorkshopPanel");
        workshopObj.transform.SetParent(canvasObj.transform, false);
        workshopObj.AddComponent<RectTransform>();
        WorkshopUIController controller = workshopObj.AddComponent<WorkshopUIController>();
        controller.moneyText = CreateTestText(workshopObj.transform);
        controller.chassisInfoText = CreateTestText(workshopObj.transform);

        core.CurrentPlayer.ActiveDoll.RuntimeGrid = new BackpackGrid(core.CurrentPlayer.ActiveDoll.Chassis);
        core.CurrentPlayer.ActiveRumors.Clear();
        core.CurrentPlayer.Money = 10;
        core.CurrentPlayer.CurrentDay = 1;
        ItemEntity sellTarget = ConfigManager.CreateItem("loot_gear_scrap");
        ((BackpackGrid)core.CurrentPlayer.ActiveDoll.RuntimeGrid).PlaceItem(sellTarget, 0, 0);
        TownEconomyService.RefreshWeeklyEconomy(core.CurrentPlayer, 42);

        controller.RefreshUI();
        controller.OpenSellPanel();

        bool panelIsSeparate = controller.sellPanel != null && controller.sellPanel.transform.parent == canvasObj.transform;
        bool panelOpened = controller.sellPanel != null && controller.sellPanel.activeSelf;
        bool listBuilt = controller.stashListParent != null && controller.stashListParent.childCount > 0;
        bool readableBackdrop = HasReadableModalBackdrop(controller.sellPanel);
        bool stableRowLayout = listBuilt && HasPreferredRowHeight(controller.stashListParent.GetChild(0), 100f);
        bool compactListLane = HasCompactRectMaskedListLane(controller.sellPanel, "SellList_Scroll", 900f, 500f);
        bool prostheticPanelClosed = controller.prostheticPanel != null && !controller.prostheticPanel.activeSelf;
        Button directSellButton = controller.sellPanel != null ? FindButton(controller.sellPanel, "Sell_Button") : null;
        Button shopStagingButton = controller.sellAllBtn;
        string marketText = CollectText(canvasObj);

        int beforeMoney = core.CurrentPlayer.Money;
        int beforeBackpackCount = ((BackpackGrid)core.CurrentPlayer.ActiveDoll.RuntimeGrid).ContainedItems.Count;
        shopStagingButton?.onClick.Invoke();
        WorkshopFormalV1PanelController formalController = controller.GetComponent<WorkshopFormalV1PanelController>();
        bool shopStagingOpened = formalController != null && formalController.CurrentScreenID == "shop_staging";
        bool economySellNotApplied = core.CurrentPlayer.Money == beforeMoney
            && ((BackpackGrid)core.CurrentPlayer.ActiveDoll.RuntimeGrid).ContainedItems.Contains(sellTarget)
            && ((BackpackGrid)core.CurrentPlayer.ActiveDoll.RuntimeGrid).ContainedItems.Count == beforeBackpackCount;
        bool marketPreviewText = marketText.Contains("Town Market Preview")
            && marketText.Contains("Shop staging");

        if (panelIsSeparate && panelOpened && listBuilt && readableBackdrop && stableRowLayout && compactListLane && prostheticPanelClosed && directSellButton == null && shopStagingOpened && economySellNotApplied && marketPreviewText) {
            Debug.Log("Workshop Market Preview Panel UI PASSED.");
        } else {
            Debug.LogError($"Workshop Market Preview Panel UI FAILED. Separate={panelIsSeparate}, Opened={panelOpened}, Rows={controller.stashListParent?.childCount ?? 0}, Backdrop={readableBackdrop}, RowLayout={stableRowLayout}, CompactLane={compactListLane}, ProstheticPanelClosed={prostheticPanelClosed}, DirectSellButton={directSellButton != null}, ShopStagingOpened={shopStagingOpened}, EconomyUnchanged={economySellNotApplied}, Text={marketPreviewText}, Money={core.CurrentPlayer.Money}");
        }

        controller.CloseSellPanel();
        Object.DestroyImmediate(canvasObj);
    }

    private static void RunWorkshopProstheticPanelUITest(CoreBackend core) {
        GameObject canvasObj = new GameObject("WorkshopProstheticUITestCanvas");
        canvasObj.AddComponent<Canvas>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject workshopObj = new GameObject("WorkshopPanel");
        workshopObj.transform.SetParent(canvasObj.transform, false);
        workshopObj.AddComponent<RectTransform>();
        WorkshopUIController controller = workshopObj.AddComponent<WorkshopUIController>();
        controller.moneyText = CreateTestText(workshopObj.transform);
        controller.chassisInfoText = CreateTestText(workshopObj.transform);

        core.CurrentPlayer.ActiveDoll.EquippedProsthetics.Clear();
        core.CurrentPlayer.StashInventory.Clear();
        core.CurrentPlayer.Money = 2000;
        AddStashItems(core.CurrentPlayer, "loot_gear_scrap", 4);

        controller.RefreshUI();
        controller.OpenProstheticPanel();

        bool panelIsSeparate = controller.prostheticPanel != null && controller.prostheticPanel.transform.parent == canvasObj.transform;
        bool panelOpened = controller.prostheticPanel != null && controller.prostheticPanel.activeSelf;
        bool listBuilt = controller.prostheticListParent != null && controller.prostheticListParent.childCount > 0;
        bool readableBackdrop = HasReadableModalBackdrop(controller.prostheticPanel);
        bool stableRowLayout = listBuilt && HasPreferredRowHeight(controller.prostheticListParent.GetChild(0), 120f);
        bool compactListLane = HasCompactRectMaskedListLane(controller.prostheticPanel, "ProstheticList_Scroll", 980f, 500f);
        bool sellPanelClosed = controller.sellPanel != null && !controller.sellPanel.activeSelf;
        bool studioSwitchesPresent = controller.prostheticPanel != null
            && FindButton(controller.prostheticPanel, "StudioMaintenance_Button") != null
            && FindButton(controller.prostheticPanel, "StudioChassis_Button") != null;
        bool recipeRowsHaveMaterialIcon = listBuilt
            && controller.prostheticListParent.GetChild(0).Find("MaterialNeedIcon_Image") != null;
        Button craftButton = listBuilt
            ? controller.prostheticListParent.GetChild(0).Find("Craft_Button")?.GetComponent<Button>()
            : null;
        bool craftButtonWasPresent = craftButton != null;
        bool craftButtonInteractable = craftButtonWasPresent && craftButton.interactable;

        int beforeMoney = core.CurrentPlayer.Money;
        int beforeStashCount = core.CurrentPlayer.StashInventory.Count;
        craftButton?.onClick.Invoke();
        bool craftApplied = craftButtonWasPresent
            && craftButtonInteractable
            && core.CurrentPlayer.ActiveDoll.EquippedProsthetics.Count == 1
            && core.CurrentPlayer.Money < beforeMoney
            && core.CurrentPlayer.StashInventory.Count < beforeStashCount
            && controller.prostheticSummaryText != null
            && controller.prostheticSummaryText.text.Contains("Crafted and equipped");

        if (panelIsSeparate && panelOpened && listBuilt && readableBackdrop && stableRowLayout && compactListLane && sellPanelClosed && studioSwitchesPresent && recipeRowsHaveMaterialIcon && craftApplied) {
            Debug.Log("Workshop Prosthetic Panel UI PASSED.");
        } else {
            Debug.LogError($"Workshop Prosthetic Panel UI FAILED. Separate={panelIsSeparate}, Opened={panelOpened}, ProstheticRows={controller.prostheticListParent?.childCount ?? 0}, Backdrop={readableBackdrop}, RowLayout={stableRowLayout}, CompactLane={compactListLane}, SellPanelClosed={sellPanelClosed}, StudioSwitches={studioSwitchesPresent}, MaterialIcon={recipeRowsHaveMaterialIcon}, CraftButton={craftButtonWasPresent}, Interactable={craftButtonInteractable}, CraftApplied={craftApplied}, Money={core.CurrentPlayer.Money}, Stash={core.CurrentPlayer.StashInventory.Count}, Summary={controller.prostheticSummaryText?.text}");
        }

        controller.CloseProstheticPanel();
        Object.DestroyImmediate(canvasObj);
    }

    private static void RunWorkshopProstheticLockedRowsUITest(CoreBackend core) {
        GameObject canvasObj = new GameObject("WorkshopProstheticLockedUITestCanvas");
        canvasObj.AddComponent<Canvas>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject workshopObj = new GameObject("WorkshopPanel");
        workshopObj.transform.SetParent(canvasObj.transform, false);
        workshopObj.AddComponent<RectTransform>();
        WorkshopUIController controller = workshopObj.AddComponent<WorkshopUIController>();
        controller.moneyText = CreateTestText(workshopObj.transform);
        controller.chassisInfoText = CreateTestText(workshopObj.transform);

        core.CurrentPlayer.ActiveDoll.EquippedProsthetics.Clear();
        core.CurrentPlayer.StashInventory.Clear();
        core.CurrentPlayer.Money = 0;

        controller.RefreshUI();
        controller.OpenProstheticPanel();

        bool listBuilt = controller.prostheticListParent != null && controller.prostheticListParent.childCount > 0;
        Transform firstRow = listBuilt ? controller.prostheticListParent.GetChild(0) : null;
        Button craftButton = firstRow != null ? firstRow.Find("Craft_Button")?.GetComponent<Button>() : null;
        bool passed = controller.prostheticPanel != null
            && controller.prostheticPanel.activeSelf
            && firstRow != null
            && HasPreferredRowHeight(firstRow, 120f)
            && firstRow.Find("LockedIcon_Image") != null
            && firstRow.Find("MaterialNeedIcon_Image") != null
            && craftButton != null
            && !craftButton.interactable
            && CollectText(canvasObj).Contains("Locked");

        if (passed) {
            Debug.Log("Workshop Prosthetic Locked Row UI PASSED.");
        } else {
            Debug.LogError($"Workshop Prosthetic Locked Row UI FAILED. Rows={controller.prostheticListParent?.childCount ?? 0}, LockedIcon={firstRow?.Find("LockedIcon_Image") != null}, MaterialIcon={firstRow?.Find("MaterialNeedIcon_Image") != null}, Button={craftButton != null}, Interactable={craftButton?.interactable}, Text={CollectText(canvasObj)}");
        }

        controller.CloseProstheticPanel();
        Object.DestroyImmediate(canvasObj);
    }

    private static void RunWorkshopFormalV2HubLayoutTest(CoreBackend core) {
        GameObject canvasObj = new GameObject("WorkshopFormalV2HubCanvas");
        canvasObj.AddComponent<Canvas>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject workshopObj = new GameObject("WorkshopPanel");
        workshopObj.transform.SetParent(canvasObj.transform, false);
        workshopObj.AddComponent<RectTransform>();
        WorkshopUIController controller = workshopObj.AddComponent<WorkshopUIController>();
        controller.moneyText = CreateTestText(workshopObj.transform);
        controller.chassisInfoText = CreateTestText(workshopObj.transform);
        controller.departBtn = CreateTestButton(workshopObj.transform, "Depart_Button");
        controller.upgradeBtn = CreateTestButton(workshopObj.transform, "Upgrade_Button");

        core.CurrentPlayer.CurrentDay = 5;
        core.CurrentPlayer.CurrentMonth = 1;
        core.CurrentPlayer.CurrentMonthDay = 5;
        core.CurrentPlayer.Money = 345;

        controller.RefreshUI();

        Transform status = workshopObj.transform.Find("LightStatusStrip");
        Transform doll = workshopObj.transform.Find("DollDisplay");
        Transform abyss = workshopObj.transform.Find("AbyssDoorPanel");
        Transform studio = workshopObj.transform.Find("WorkshopEntryPanel");
        Transform ledger = workshopObj.transform.Find("LedgerCornerPanel");
        RectTransform background = FindRectTransform(workshopObj, "WorkshopBackground_Image");
        RectTransform studioRect = studio as RectTransform;
        RectTransform dollStatus = workshopObj.transform.Find("DollStatusPlate") as RectTransform;
        Button[] activeButtons = canvasObj.GetComponentsInChildren<Button>(false);
        bool compactStatus = controller.moneyText != null
            && controller.moneyText.transform.parent == status
            && controller.moneyText.rectTransform.sizeDelta.x <= 620f;
        bool oldBulkHidden = controller.upgradeBtn != null
            && !controller.upgradeBtn.gameObject.activeSelf
            && controller.openMaintenancePanelBtn != null
            && !controller.openMaintenancePanelBtn.gameObject.activeSelf
            && controller.openOrderBoardPanelBtn != null
            && !controller.openOrderBoardPanelBtn.gameObject.activeSelf;
        bool hubButtonsReduced = activeButtons.Length <= 3;
        bool backgroundStretches = background != null
            && background.anchorMin == Vector2.zero
            && background.anchorMax == Vector2.one;
        bool workshopMainKeepsBackpackOutOfHub = !CollectText(canvasObj).Contains("Backpack");
        bool studioIsOffBackpackLane = studioRect != null
            && studioRect.anchorMin.y >= 0.45f
            && studioRect.sizeDelta.x <= 330f;
        bool dollStatusBelowDollLegs = dollStatus != null
            && dollStatus.anchoredPosition.y <= 50f
            && dollStatus.sizeDelta.y <= 120f;
        bool labelsUpdated = CollectText(canvasObj).Contains("Descend")
            && CollectText(canvasObj).Contains("Studio")
            && CollectText(canvasObj).Contains("Market");

        if (status != null && doll != null && abyss != null && studio != null && ledger != null && compactStatus && oldBulkHidden && hubButtonsReduced && backgroundStretches && workshopMainKeepsBackpackOutOfHub && studioIsOffBackpackLane && dollStatusBelowDollLegs && labelsUpdated) {
            Debug.Log("Workshop FormalV2 Hub Layout PASSED.");
        } else {
            Debug.LogError($"Workshop FormalV2 Hub Layout FAILED. Status={status != null}, Doll={doll != null}, Abyss={abyss != null}, Studio={studio != null}, Ledger={ledger != null}, Compact={compactStatus}, OldBulkHidden={oldBulkHidden}, ActiveButtons={activeButtons.Length}, Background={backgroundStretches}, BackpackHidden={workshopMainKeepsBackpackOutOfHub}, StudioLane={studioIsOffBackpackLane}, DollStatus={dollStatusBelowDollLegs}, Text={CollectText(canvasObj)}");
        }

        Object.DestroyImmediate(canvasObj);
    }

    private static void RunProstheticCraftAndEffectTest(CoreBackend core) {
        var player = core.CurrentPlayer;
        var doll = player.ActiveDoll;
        doll.RuntimeGrid = new BackpackGrid(doll.Chassis);
        BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;

        ItemEntity meleeWeapon = ConfigManager.CreateItem("gear_rusty_dagger");
        grid.PlaceItem(meleeWeapon, 0, 0);

        player.Money = 2000;
        AddStashItems(player, "loot_gear_scrap", 3);

        ProstheticCraftingResult powerArmResult = core.Workshop.CraftAndEquipProstheticWithResult("craft_pros_power_arm", doll);
        bool craftedPowerArm = powerArmResult.Success;
        bool hasPowerArm = doll.EquippedProsthetics.Contains("pros_power_arm");
        bool damageBuffed = meleeWeapon.Combat.RuntimeDamage > meleeWeapon.Combat.BaseValue;

        if (craftedPowerArm && hasPowerArm && damageBuffed && powerArmResult.MoneySpent == 300 && powerArmResult.ConsumedItems.Count == 2 && powerArmResult.Equipped && powerArmResult.EffectsRecalculated) {
            Debug.Log("Prosthetic Craft Damage Effect PASSED.");
        } else {
            Debug.LogError($"Prosthetic Craft Damage Effect FAILED. Crafted={craftedPowerArm}, Equipped={hasPowerArm}, Base={meleeWeapon.Combat.BaseValue}, Runtime={meleeWeapon.Combat.RuntimeDamage}, Money={powerArmResult.MoneySpent}, Items={powerArmResult.ConsumedItems.Count}, Effects={powerArmResult.EffectsRecalculated}, Reason={powerArmResult.Reason}");
        }

        player.Money = 2000;
        AddStashItems(player, "loot_gear_scrap", 2);
        ProstheticCraftingResult coolingResult = core.Workshop.CraftAndEquipProstheticWithResult("craft_pros_cooling_system", doll);
        bool craftedCooling = coolingResult.Success;
        doll.Status.SAN_Current = 10;
        int beforeSAN = doll.Status.SAN_Current;

        CombatFaction faction = new CombatFaction { Type = FactionType.Player };
        DollFighter fighter = new DollFighter(doll, faction);
        faction.Fighters.Add(fighter);
        CombatEventBus.Publish(CombatEventType.OnCombatEnd, faction);
        fighter.Cleanup();

        bool restoredSAN = doll.Status.SAN_Current == beforeSAN + 2;
        if (craftedCooling && doll.EquippedProsthetics.Contains("pros_cooling_system") && restoredSAN && coolingResult.MoneySpent == 1000 && coolingResult.ConsumedItems.Count == 2 && coolingResult.Equipped && coolingResult.EffectsRecalculated) {
            Debug.Log("Prosthetic Combat End SAN Effect PASSED.");
        } else {
            Debug.LogError($"Prosthetic Combat End SAN Effect FAILED. Crafted={craftedCooling}, Equipped={doll.EquippedProsthetics.Contains("pros_cooling_system")}, SAN={doll.Status.SAN_Current}, Expected={beforeSAN + 2}, Money={coolingResult.MoneySpent}, Items={coolingResult.ConsumedItems.Count}, Effects={coolingResult.EffectsRecalculated}, Reason={coolingResult.Reason}");
        }
    }

    private static void AddStashItems(PlayerProfile player, string configID, int count) {
        for (int i = 0; i < count; i++) {
            ItemEntity item = ConfigManager.CreateItem(configID);
            if (item != null) {
                player.StashInventory.Add(item);
            }
        }
    }

    private static Text CreateTestText(Transform parent) {
        GameObject obj = new GameObject("TestText");
        obj.transform.SetParent(parent, false);
        return obj.AddComponent<Text>();
    }

    private static Button CreateTestButton(Transform parent, string objectName) {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<Image>();
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        Text label = textObj.AddComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = objectName;
        label.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        return obj.AddComponent<Button>();
    }

    private static Button FindButton(GameObject root, string buttonName) {
        if (root == null) {
            return null;
        }

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++) {
            Button button = buttons[i];
            if (button != null && button.name == buttonName) {
                return button;
            }
        }

        return null;
    }

    private static bool HasReadableModalBackdrop(GameObject panel) {
        Image image = panel != null ? panel.GetComponent<Image>() : null;
        if (image == null) {
            return false;
        }

        Color color = image.color;
        return color.a >= 0.82f
            && color.a <= 0.96f
            && color.r <= 0.08f
            && color.g <= 0.08f
            && color.b <= 0.08f;
    }

    private static bool HasPreferredRowHeight(Transform row, float minHeight) {
        if (row == null) {
            return false;
        }

        LayoutElement layoutElement = row.GetComponent<LayoutElement>();
        RectTransform rect = row as RectTransform;
        return layoutElement != null
            && rect != null
            && layoutElement.preferredHeight >= minHeight
            && rect.sizeDelta.y >= minHeight;
    }

    private static bool HasCompactRectMaskedListLane(GameObject panel, string scrollName, float maxWidth, float maxHeight) {
        RectTransform scrollRect = FindRectTransform(panel, scrollName);
        if (scrollRect == null) {
            return false;
        }

        RectTransform viewport = FindRectTransform(scrollRect.gameObject, "Viewport");
        return scrollRect.sizeDelta.x <= maxWidth
            && scrollRect.sizeDelta.y <= maxHeight
            && viewport != null
            && viewport.GetComponent<RectMask2D>() != null;
    }

    private static RectTransform FindRectTransform(GameObject root, string objectName) {
        if (root == null) {
            return null;
        }

        RectTransform[] rects = root.GetComponentsInChildren<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++) {
            if (rects[i] != null && rects[i].name == objectName) {
                return rects[i];
            }
        }

        return null;
    }

    private static string CollectText(GameObject root) {
        if (root == null) {
            return string.Empty;
        }

        Text[] texts = root.GetComponentsInChildren<Text>(true);
        string combined = string.Empty;
        for (int i = 0; i < texts.Length; i++) {
            if (texts[i] == null) {
                continue;
            }

            if (!string.IsNullOrEmpty(combined)) {
                combined += "\n";
            }

            combined += texts[i].text;
        }

        return combined;
    }
}
