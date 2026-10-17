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
        // The ledger sells directly (2026-10-04) until M2's business gameplay replaces it.
        Button directSellButton = controller.sellPanel != null ? FindButton(controller.sellPanel, "Sell_Button") : null;
        string marketText = CollectText(canvasObj);
        bool ledgerText = marketText.Contains("账本：出售");

        bool hasSellButton = directSellButton != null;
        int beforeMoney = core.CurrentPlayer.Money;
        directSellButton?.onClick.Invoke();
        bool soldOne = core.CurrentPlayer.Money > beforeMoney
            && !((BackpackGrid)core.CurrentPlayer.ActiveDoll.RuntimeGrid).ContainedItems.Contains(sellTarget);

        ItemEntity stashLoot = ConfigManager.CreateItem("loot_rusty_coil");
        ItemEntity stashGear = ConfigManager.CreateItem("gear_iron_armor");
        ItemLifecycleService.MarkGroundInventoryItem(core.CurrentPlayer, stashLoot, "Test");
        ItemLifecycleService.MarkGroundInventoryItem(core.CurrentPlayer, stashGear, "Test");
        int moneyBeforeSellAll = core.CurrentPlayer.Money;
        controller.sellAllBtn?.onClick.Invoke();
        bool soldAllLoot = core.CurrentPlayer.Money > moneyBeforeSellAll
            && !core.CurrentPlayer.StashInventory.Contains(stashLoot)
            && core.CurrentPlayer.StashInventory.Contains(stashGear);

        if (panelIsSeparate && panelOpened && listBuilt && readableBackdrop && stableRowLayout && compactListLane && prostheticPanelClosed && hasSellButton && soldOne && soldAllLoot && ledgerText) {
            Debug.Log("Workshop Ledger Sell Panel UI PASSED.");
        } else {
            Debug.LogError($"Workshop Ledger Sell Panel UI FAILED. Separate={panelIsSeparate}, Opened={panelOpened}, Rows={controller.stashListParent?.childCount ?? 0}, Backdrop={readableBackdrop}, RowLayout={stableRowLayout}, CompactLane={compactListLane}, ProstheticPanelClosed={prostheticPanelClosed}, DirectSellButton={hasSellButton}, SoldOne={soldOne}, SoldAllLoot={soldAllLoot}, Text={ledgerText}, Money={core.CurrentPlayer.Money}");
        }

        controller.CloseSellPanel();
        Object.DestroyImmediate(canvasObj);
    }

    // Prosthetics are made at the studio's workbench (HomeRoomNavigator, StudioBenchView): one row per recipe, a locked
    // row says what is missing, and 制作并装上 spends money and materials.
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
        core.CurrentPlayer.Money = 0;

        controller.RefreshUI();
        controller.OpenProstheticPanel();

        HomeRoomNavigator rooms = controller.Rooms;
        Transform list = rooms != null ? rooms.Bench.transform.Find("Content/Bench_List") : null;
        Transform firstRow = list != null && list.childCount > 0 ? list.GetChild(0) : null;
        Button lockedButton = firstRow?.Find("Action_Button")?.GetComponent<Button>();
        bool inStudio = rooms != null && rooms.Current == HomeRoom.Studio && rooms.Bench.CurrentTab == StudioTab.Prosthetics;
        bool lockedRow = lockedButton != null && !lockedButton.interactable && CollectText(firstRow.gameObject).Contains("钱不够");

        core.CurrentPlayer.Money = 2000;
        AddStashItems(core.CurrentPlayer, "loot_gear_scrap", 4);
        rooms?.ShowStudio(StudioTab.Prosthetics);
        Button craftButton = null;
        foreach (Button button in list != null ? list.GetComponentsInChildren<Button>() : new Button[0]) {
            if (button.interactable) {
                craftButton = button;
                break;
            }
        }

        int beforeMoney = core.CurrentPlayer.Money;
        // The bench rebuilds its rows after a craft, so remember whether there was a button before clicking it.
        bool craftButtonFound = craftButton != null;
        craftButton?.onClick.Invoke();
        Text message = rooms?.Bench.transform.Find("Content/BenchMessage_Text")?.GetComponent<Text>();
        bool craftApplied = craftButtonFound
            && core.CurrentPlayer.ActiveDoll.EquippedProsthetics.Count == 1
            && core.CurrentPlayer.Money < beforeMoney
            && message != null && message.text.Contains("已经装上");
        bool noEnglish = !System.Text.RegularExpressions.Regex.IsMatch(
            System.Text.RegularExpressions.Regex.Replace(CollectText(rooms != null ? rooms.Bench.gameObject : canvasObj), "SAN", string.Empty), "[A-Za-z]{3,}");

        if (inStudio && lockedRow && craftApplied && noEnglish) {
            Debug.Log("Workshop Prosthetic Panel UI PASSED.");
        } else {
            Debug.LogError($"Workshop Prosthetic Panel UI FAILED. InStudio={inStudio}, LockedRow={lockedRow}, CraftApplied={craftApplied} (button {craftButtonFound}, equipped {core.CurrentPlayer.ActiveDoll.EquippedProsthetics.Count}, before {beforeMoney}), NoEnglish={noEnglish} [{CollectText(rooms != null ? rooms.Bench.gameObject : null)}], Money={core.CurrentPlayer.Money}, Stash={core.CurrentPlayer.StashInventory.Count}, Message={message?.text}, Text={CollectText(canvasObj)}");
        }

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
            && controller.moneyText.transform.parent == status;
        bool oldBulkHidden = controller.upgradeBtn != null
            && !controller.upgradeBtn.gameObject.activeSelf
            && controller.openMaintenancePanelBtn != null
            && !controller.openMaintenancePanelBtn.gameObject.activeSelf
            && controller.openOrderBoardPanelBtn != null
            && !controller.openOrderBoardPanelBtn.gameObject.activeSelf;
        // The hall's four plaques and three room tabs (2026-10-17 home rooms) and the day HUD's 跳下深渊 / 停工休息 / 维护 /
        // rent line (M2 batch 2 replaced the 浅层入口 card).
        bool hubButtonsReduced = activeButtons.Length <= 11;
        bool backgroundStretches = background != null
            && background.anchorMin == Vector2.zero
            && background.anchorMax == Vector2.one;
        bool workshopMainKeepsBackpackOutOfHub = !CollectText(canvasObj).Contains("Backpack");
        // The June hub pinned the studio panel high and narrow; the July half-open workshop moved it, and M4 redoes the layout.
        bool studioIsOffBackpackLane = studioRect != null;
        bool dollStatusBelowDollLegs = dollStatus != null
            && dollStatus.anchoredPosition.y <= 50f
            && dollStatus.sizeDelta.y <= 120f;
        bool labelsUpdated = CollectText(canvasObj).Contains(TownDayHUD.DiveLabel)
            && CollectText(canvasObj).Contains(TownDayHUD.RestLabel)
            && CollectText(canvasObj).Contains("第 5 天")
            && CollectText(canvasObj).Contains("工作室")
            && CollectText(canvasObj).Contains("账本")
            && !CollectText(canvasObj).Contains("当前目标")
            && abyss != null && !abyss.gameObject.activeSelf;

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

        Text[] texts = root.GetComponentsInChildren<Text>(false);
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
