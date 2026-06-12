using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WorkshopUIController : MonoBehaviour {
    public Text moneyText;
    public Text chassisInfoText;
    public Button upgradeBtn;
    public Button departBtn;
    public Text stashHeaderText;
    public Text sellSummaryText;
    public Transform stashListParent;
    public GameObject sellPanel;
    public Button openSellPanelBtn;
    public Button closeSellPanelBtn;
    public Button sellAllBtn;
    public GameObject prostheticPanel;
    public Button openProstheticPanelBtn;
    public Button closeProstheticPanelBtn;
    public Text prostheticHeaderText;
    public Text prostheticSummaryText;
    public Transform prostheticListParent;
    public Image backgroundImage;
    public GameObject dungeonStartLayerPanel;
    public Button openMaintenancePanelBtn;
    public Button openDailyBillPanelBtn;
    public Button openShopStagingPanelBtn;
    public Button openOrderBoardPanelBtn;
    public Button openRumorBoardPanelBtn;
    public Button openBusinessSettlementPanelBtn;
    public Button openChassisUpgradePanelBtn;
    public Button openDollInteractionPanelBtn;
    public Button openDollRoomPanelBtn;
    public Button openFactionShopPanelBtn;
    public Button openScenarioEventPanelBtn;

    private bool _sellPanelOpen;
    private bool _prostheticPanelOpen;
    private bool _dungeonStartLayerPanelOpen;
    private DungeonStartLayerUIController _dungeonStartLayerController;
    private Image _topStatusPanel;
    private Image _leftActionPanel;
    private Image _formalV1EntryPanel;
    private Image _bottomHintPanel;
    private Image _dollStatusPanel;
    private Image _dollStandImage;
    private Text _abyssHintText;
    private Text _studioHintText;
    private Text _ledgerHintText;
    private Text _dollCaptionText;
    private WorkshopFormalV1PanelController _formalV1PanelController;

    void Start() {
        ApplyWorkshopBackground();
        EnsureSellControls();
        BindButtons();
        CloseSellPanel(false);
        CloseProstheticPanel(false);
        CloseDungeonStartLayerPanel(false);
        RefreshUI();
    }

    public void RefreshUI() {
        ApplyWorkshopBackground();
        EnsureSellControls();

        var player = GameRoot.Core.CurrentPlayer;
        CollectSellStats(out int backpackCount, out int stashCount, out int sellableCount, out int sellableEstimatedValue);

        if (moneyText != null) {
            moneyText.text =
                $"Day {player.CurrentDay} / M{player.CurrentMonth}.{player.CurrentMonthDay}   {player.Money}G\n" +
                $"{BuildRentPressureText(player)}   Sellable {sellableCount} ({sellableEstimatedValue}G)";
        }

        var chassis = player.ActiveDoll.Chassis;
        if (chassisInfoText != null) {
            chassisInfoText.text = $"Chassis {chassis.ChassisID}  Grid {chassis.GridWidth}x{chassis.GridHeight}";
        }

        if (stashHeaderText != null) {
            stashHeaderText.text = "Town Market Preview";
        }

        if (sellSummaryText != null) {
            sellSummaryText.text = sellableCount > 0
                ? $"Backpack {backpackCount} / Stash {stashCount}\nMarket preview {sellableEstimatedValue}G. Allocate and confirm sales in shop staging."
                : "Town market preview. Shop staging handles selling and allocation.";
        }

        if (openSellPanelBtn != null) {
            openSellPanelBtn.interactable = true;
        }

        if (sellAllBtn != null) {
            sellAllBtn.interactable = true;
        }

        if (openProstheticPanelBtn != null) {
            openProstheticPanelBtn.interactable = HasProstheticRecipes();
        }

        if (prostheticSummaryText != null) {
            prostheticSummaryText.text = BuildProstheticSummary();
        }

        if (sellPanel != null && sellPanel.activeSelf) {
            RefreshSellList();
        }

        if (prostheticPanel != null && prostheticPanel.activeSelf) {
            RefreshProstheticList();
        }

        if (dungeonStartLayerPanel != null && dungeonStartLayerPanel.activeSelf) {
            _dungeonStartLayerController?.Present(CloseDungeonStartLayerPanel);
        }
    }

    private void BindButtons() {
        if (upgradeBtn != null) {
            upgradeBtn.onClick.RemoveAllListeners();
            upgradeBtn.onClick.AddListener(ExecuteChassisUpgradeFromButton);
        }

        if (departBtn != null) {
            departBtn.onClick.RemoveAllListeners();
            departBtn.onClick.AddListener(() => {
                CloseSellPanel(false);
                CloseProstheticPanel(false);
                OpenDungeonStartLayerPanel();
            });
        }

        if (openSellPanelBtn != null) {
            openSellPanelBtn.onClick.RemoveAllListeners();
            openSellPanelBtn.onClick.AddListener(OpenSellPanel);
        }

        if (closeSellPanelBtn != null) {
            closeSellPanelBtn.onClick.RemoveAllListeners();
            closeSellPanelBtn.onClick.AddListener(CloseSellPanel);
        }

        if (sellAllBtn != null) {
            sellAllBtn.onClick.RemoveAllListeners();
            sellAllBtn.onClick.AddListener(() => OpenFormalV1Panel("shop_staging"));
        }

        if (openProstheticPanelBtn != null) {
            openProstheticPanelBtn.onClick.RemoveAllListeners();
            openProstheticPanelBtn.onClick.AddListener(OpenProstheticPanel);
        }

        if (closeProstheticPanelBtn != null) {
            closeProstheticPanelBtn.onClick.RemoveAllListeners();
            closeProstheticPanelBtn.onClick.AddListener(CloseProstheticPanel);
        }

        BindFormalV1Button(openMaintenancePanelBtn, "maintenance_panel");
        BindFormalV1Button(openDailyBillPanelBtn, "daily_bill_report");
        BindFormalV1Button(openShopStagingPanelBtn, "shop_staging");
        BindFormalV1Button(openOrderBoardPanelBtn, "order_board");
        BindFormalV1Button(openRumorBoardPanelBtn, "rumor_board");
        BindFormalV1Button(openBusinessSettlementPanelBtn, "business_settlement");
        BindFormalV1Button(openChassisUpgradePanelBtn, "chassis_upgrade_panel");
        BindFormalV1Button(openDollInteractionPanelBtn, "doll_interaction");
        BindFormalV1Button(openDollRoomPanelBtn, "doll_room");
        BindFormalV1Button(openFactionShopPanelBtn, "faction_shop");
        BindFormalV1Button(openScenarioEventPanelBtn, "scenario_event");
    }

    private void ExecuteChassisUpgradeFromButton() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        DollEntity doll = player?.ActiveDoll;
        ChassisUpgradeResult result = ChassisUpgradeService.Upgrade(player, doll);
        LogChassisUpgradeResult(result, "WorkshopUI");

        RefreshUI();
        if (result != null && result.Success && result.RuntimeGridRebuilt && doll?.Chassis != null) {
            GridGenerator generator = FindObjectOfType<GridGenerator>();
            if (generator != null) {
                generator.GenerateGrid(doll.Chassis);
            }
        }

        if (chassisInfoText != null && result != null) {
            chassisInfoText.text += $"\n{result.FeedbackText}";
        }
    }

    private void ExecuteProstheticCraftFromButton(string recipeID) {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        DollEntity doll = player?.ActiveDoll;
        ProstheticCraftingResult result = ProstheticCraftingService.CraftAndEquip(player, doll, recipeID);
        LogProstheticCraftingResult(result, "WorkshopUI");

        RefreshUI();
        if (prostheticSummaryText != null && result != null) {
            prostheticSummaryText.text = $"{BuildProstheticSummary()}\n{result.FeedbackText}";
        }
    }

    private void LogChassisUpgradeResult(ChassisUpgradeResult result, string source) {
        if (result == null) {
            Debug.LogWarning($"[{source}] Chassis upgrade failed: result is null.");
            return;
        }

        if (result.Success) {
            Debug.Log($"[{source}] {result.FeedbackText}");
        } else {
            Debug.LogWarning($"[{source}] Chassis upgrade failed: {result.Reason}");
        }
    }

    private void LogProstheticCraftingResult(ProstheticCraftingResult result, string source) {
        if (result == null) {
            Debug.LogWarning($"[{source}] Prosthetic crafting failed: result is null.");
            return;
        }

        if (result.Success) {
            Debug.Log($"[{source}] {result.FeedbackText}");
        } else {
            Debug.LogWarning($"[{source}] Prosthetic crafting failed: {result.Reason}");
        }
    }

    public void OpenSellPanel() {
        EnsureSellControls();
        BindButtons();
        CloseFormalV1Panel();
        CloseProstheticPanel(false);
        _sellPanelOpen = true;

        if (sellPanel != null) {
            sellPanel.SetActive(true);
            sellPanel.transform.SetAsLastSibling();
        }

        RefreshUI();
    }

    public void CloseSellPanel() {
        CloseSellPanel(true);
    }

    private void CloseSellPanel(bool refresh) {
        _sellPanelOpen = false;

        if (sellPanel != null) {
            sellPanel.SetActive(false);
        }

        if (refresh) {
            RefreshUI();
        }
    }

    public void OpenProstheticPanel() {
        EnsureSellControls();
        BindButtons();
        CloseFormalV1Panel();
        CloseSellPanel(false);
        _prostheticPanelOpen = true;

        if (prostheticPanel != null) {
            prostheticPanel.SetActive(true);
            prostheticPanel.transform.SetAsLastSibling();
        }

        RefreshUI();
    }

    public void CloseProstheticPanel() {
        CloseProstheticPanel(true);
    }

    private void CloseProstheticPanel(bool refresh) {
        _prostheticPanelOpen = false;

        if (prostheticPanel != null) {
            prostheticPanel.SetActive(false);
        }

        if (refresh) {
            RefreshUI();
        }
    }

    public void OpenDungeonStartLayerPanel() {
        EnsureSellControls();
        CloseSellPanel(false);
        CloseProstheticPanel(false);
        CloseFormalV1Panel();
        _dungeonStartLayerPanelOpen = true;

        if (dungeonStartLayerPanel != null) {
            dungeonStartLayerPanel.SetActive(true);
            dungeonStartLayerPanel.transform.SetAsLastSibling();
        }

        _dungeonStartLayerController?.Present(CloseDungeonStartLayerPanel);
    }

    public void CloseDungeonStartLayerPanel() {
        CloseDungeonStartLayerPanel(true);
    }

    private void CloseDungeonStartLayerPanel(bool refresh) {
        _dungeonStartLayerPanelOpen = false;

        if (dungeonStartLayerPanel != null) {
            dungeonStartLayerPanel.SetActive(false);
        }

        if (refresh) {
            RefreshUI();
        }
    }

    public void OpenFormalV1Panel(string screenID) {
        EnsureSellControls();
        CloseSellPanel(false);
        CloseProstheticPanel(false);
        CloseDungeonStartLayerPanel(false);
        EnsureFormalV1PanelController();
        _formalV1PanelController?.Show(screenID);
    }

    public void CloseFormalV1Panel() {
        _formalV1PanelController?.Hide();
    }

    private void RefreshSellList() {
        if (stashListParent == null) {
            return;
        }

        for (int i = stashListParent.childCount - 1; i >= 0; i--) {
            DestroyRuntimeObject(stashListParent.GetChild(i).gameObject);
        }

        var player = GameRoot.Core.CurrentPlayer;
        BackpackGrid grid = player.ActiveDoll?.RuntimeGrid as BackpackGrid;
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        int visibleRows = 0;

        if (grid != null) {
            foreach (var item in grid.ContainedItems) {
                CreateSellRow(item, defaultFont, "Backpack");
                visibleRows++;
            }
        }

        foreach (var item in player.StashInventory) {
            CreateSellRow(item, defaultFont, "Stash");
            visibleRows++;
        }

        if (visibleRows == 0) {
            CreateMarketPreviewPlaceholderRow(defaultFont);
        }

        ForceRebuildGeneratedList(stashListParent);
    }

    private void CreateSellRow(ItemEntity item, Font defaultFont, string sourceLabel) {
        if (stashListParent == null || item == null) {
            return;
        }

        GameObject row = new GameObject($"SellRow_{item.InstanceID}");
        row.transform.SetParent(stashListParent, false);
        ConfigureGeneratedRow(row, 760f, 104f);
        Image rowBg = row.AddComponent<Image>();
        VisualUIHelper.ApplySlicedSprite(
            rowBg,
            VisualAssetService.UIListRowNormalID,
            Color.white,
            new Color(0.11f, 0.105f, 0.095f, 0.94f),
            false);
        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.padding = new RectOffset(14, 14, 8, 8);
        rowLayout.spacing = 12f;
        GameObject iconObj = new GameObject("ItemIcon_Image");
        iconObj.transform.SetParent(row.transform, false);
        Image icon = iconObj.AddComponent<Image>();
        string iconID = VisualAssetService.ResolveItemIconID(item);
        VisualUIHelper.ApplyContainSprite(icon, iconID, VisualDisplaySpecs.ItemIcon, Color.white, ResolveItemTint(item));

        GameObject labelObj = new GameObject("ItemLabel_Text");
        labelObj.transform.SetParent(row.transform, false);
        Text label = labelObj.AddComponent<Text>();
        label.font = defaultFont;
        label.fontSize = 24;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
        EconomySellLine sellLine = TownEconomyService.CalculateItemSellValue(
            GameRoot.Core.CurrentPlayer,
            item,
            EconomySellChannel.DumpBox);
        string valueText = sellLine.FinalValue == item.BaseValue
            ? $"{sellLine.FinalValue}G"
            : $"{item.BaseValue}G -> {sellLine.FinalValue}G";
        label.text = $"[{sourceLabel}] {item.Name}  [{valueText}]";
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(420f, 76f);

        GameObject routeObj = new GameObject("ShopStagingRoute_Text");
        routeObj.transform.SetParent(row.transform, false);
        Text routeText = routeObj.AddComponent<Text>();
        routeText.font = defaultFont;
        routeText.fontSize = 18;
        routeText.color = new Color(0.92f, 0.82f, 0.58f, 1f);
        routeText.alignment = TextAnchor.MiddleCenter;
        routeText.raycastTarget = false;
        routeText.text = "Shop staging";
        RectTransform routeRect = routeObj.GetComponent<RectTransform>();
        routeRect.sizeDelta = new Vector2(150f, 58f);
    }

    private void CreateMarketPreviewPlaceholderRow(Font defaultFont) {
        if (stashListParent == null) {
            return;
        }

        GameObject row = new GameObject("MarketPreviewRow_Empty");
        row.transform.SetParent(stashListParent, false);
        ConfigureGeneratedRow(row, 760f, 132f);
        Image rowBg = row.AddComponent<Image>();
        VisualUIHelper.ApplySlicedSprite(
            rowBg,
            VisualAssetService.UIListRowSelectedID,
            Color.white,
            new Color(0.14f, 0.12f, 0.09f, 0.96f),
            false);

        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.padding = new RectOffset(18, 18, 12, 12);
        rowLayout.spacing = 14f;

        GameObject iconObj = new GameObject("MarketPreviewIcon_Image");
        iconObj.transform.SetParent(row.transform, false);
        Image icon = iconObj.AddComponent<Image>();
        VisualUIHelper.ApplyContainSprite(
            icon,
            VisualAssetService.UIIconShopChannelID,
            VisualDisplaySpecs.UIIcon,
            Color.white,
            new Color(0.72f, 0.46f, 0.22f, 1f));

        GameObject labelObj = new GameObject("MarketPreviewLabel_Text");
        labelObj.transform.SetParent(row.transform, false);
        Text label = labelObj.AddComponent<Text>();
        label.font = defaultFont;
        label.fontSize = 23;
        label.color = new Color(0.96f, 0.88f, 0.7f, 1f);
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
        label.text = "No goods staged here.\nUse shop staging to allocate inventory, channels, and final sale confirmation.";
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(540f, 96f);

        GameObject routeObj = new GameObject("ShopStagingRoute_Text");
        routeObj.transform.SetParent(row.transform, false);
        Text routeText = routeObj.AddComponent<Text>();
        routeText.font = defaultFont;
        routeText.fontSize = 18;
        routeText.color = new Color(0.92f, 0.82f, 0.58f, 1f);
        routeText.alignment = TextAnchor.MiddleCenter;
        routeText.raycastTarget = false;
        routeText.text = "Shop staging";
        RectTransform routeRect = routeObj.GetComponent<RectTransform>();
        routeRect.sizeDelta = new Vector2(150f, 58f);
    }

    private Color ResolveItemTint(ItemEntity item) {
        if (item == null) {
            return new Color(0.45f, 0.45f, 0.45f, 1f);
        }

        switch (item.ItemType) {
            case nameof(ItemType.Weapon):
                return new Color(0.75f, 0.18f, 0.18f, 1f);
            case nameof(ItemType.Armor):
                return new Color(0.35f, 0.45f, 0.7f, 1f);
            case nameof(ItemType.Consumable):
                return new Color(0.22f, 0.65f, 0.3f, 1f);
            case nameof(ItemType.Loot):
                return new Color(0.82f, 0.63f, 0.18f, 1f);
            default:
                return new Color(0.45f, 0.45f, 0.45f, 1f);
        }
    }

    private void CollectSellStats(out int backpackCount, out int stashCount, out int sellableCount, out int sellableEstimatedValue) {
        var player = GameRoot.Core.CurrentPlayer;
        BackpackGrid grid = player.ActiveDoll?.RuntimeGrid as BackpackGrid;
        backpackCount = grid?.ContainedItems.Count ?? 0;
        stashCount = player.StashInventory.Count;
        sellableCount = 0;
        sellableEstimatedValue = 0;

        if (grid != null) {
            foreach (var item in grid.ContainedItems) {
                if (TryGetSellValue(player, item, out int value)) {
                    sellableCount++;
                    sellableEstimatedValue += value;
                }
            }
        }

        foreach (var item in player.StashInventory) {
            if (TryGetSellValue(player, item, out int value)) {
                sellableCount++;
                sellableEstimatedValue += value;
            }
        }
    }

    private string BuildRentPressureText(PlayerProfile player) {
        if (player == null) {
            return "Rent unknown";
        }

        if (player.HasPendingMonthlyRent) {
            return $"Rent due {player.PendingMonthlyBillAmount}G";
        }

        int rentCountdown = Mathf.Max(0, 28 - Mathf.Max(1, player.CurrentMonthDay));
        string debtText = player.EconomyDebtAmount > 0
            ? $"Debt {player.EconomyDebtAmount}G"
            : "No debt";
        return $"Rent {rentCountdown}d / {debtText}";
    }

    private void SellSingleItem(ItemEntity item) {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        EconomySellReport report = TownEconomyService.SellItems(
            player,
            new[] { item },
            EconomySellChannel.DumpBox);
        LogSellReport(report);
        RefreshUI();
    }

    private void SellAllVisibleItems() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        EconomySellReport report = TownEconomyService.SellItems(
            player,
            CollectOwnedItems(player),
            EconomySellChannel.DumpBox);
        LogSellReport(report);
        RefreshUI();
    }

    private List<ItemEntity> CollectOwnedItems(PlayerProfile player) {
        List<ItemEntity> items = new List<ItemEntity>();
        BackpackGrid grid = player?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        if (grid != null) {
            foreach (ItemEntity item in grid.ContainedItems) {
                if (item != null) {
                    items.Add(item);
                }
            }
        }

        if (player?.StashInventory != null) {
            foreach (ItemEntity item in player.StashInventory) {
                if (item != null) {
                    items.Add(item);
                }
            }
        }

        return items;
    }

    private bool TryGetSellValue(PlayerProfile player, ItemEntity item, out int value) {
        value = 0;
        if (player == null || item == null) {
            return false;
        }

        EconomySellLine line = TownEconomyService.CalculateItemSellValue(player, item, EconomySellChannel.DumpBox);
        value = Mathf.Max(0, line.FinalValue);
        return value > 0;
    }

    private void LogSellReport(EconomySellReport report) {
        if (report == null) {
            Debug.LogWarning("[WorkshopUI] Sell failed: report is null.");
            return;
        }

        string summary = $"[WorkshopUI] Sell report. Channel={report.Channel}, Success={report.Success}, Sold={report.SoldItems.Count}, Income={report.TotalIncome}, Money={report.StartingMoney}->{report.EndingMoney}";
        if (report.Success) {
            Debug.Log(summary);
        } else {
            Debug.LogWarning($"{summary}, Reason={report.Reason}, Failed={string.Join("; ", report.FailedItems)}");
        }
    }

    private void RefreshProstheticList() {
        if (prostheticListParent == null) {
            return;
        }

        for (int i = prostheticListParent.childCount - 1; i >= 0; i--) {
            DestroyRuntimeObject(prostheticListParent.GetChild(i).gameObject);
        }

        if (prostheticHeaderText != null) {
            prostheticHeaderText.text = "Workshop Studio";
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        int visibleRows = 0;
        foreach (var kvp in ConfigManager.CraftingRecipes) {
            CraftingRecipeConfig recipe = kvp.Value;
            if (recipe == null || string.IsNullOrEmpty(recipe.TargetProstheticID)) {
                continue;
            }

            if (!ConfigManager.Prosthetics.TryGetValue(recipe.TargetProstheticID, out var prosthetic)) {
                continue;
            }

            CreateProstheticRow(recipe, prosthetic, defaultFont);
            visibleRows++;
        }

        if (visibleRows == 0) {
            CreateStudioPlaceholderRow(defaultFont);
        }

        ForceRebuildGeneratedList(prostheticListParent);
    }

    private void CreateProstheticRow(CraftingRecipeConfig recipe, ProstheticEntity prosthetic, Font defaultFont) {
        GameObject row = new GameObject($"ProstheticRow_{prosthetic.ProstheticID}");
        row.transform.SetParent(prostheticListParent, false);
        ConfigureGeneratedRow(row, 860f, 132f);
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        DollEntity doll = player?.ActiveDoll;
        bool isEquipped = doll?.EquippedProsthetics?.Contains(prosthetic.ProstheticID) == true;
        bool canCraft = ProstheticCraftingService.CanCraftAndEquip(player, doll, recipe.RecipeID, out string craftReason);
        Image rowBg = row.AddComponent<Image>();
        VisualUIHelper.ApplySlicedSprite(
            rowBg,
            isEquipped ? VisualAssetService.UIListRowSelectedID : VisualAssetService.UIListRowNormalID,
            Color.white,
            isEquipped ? new Color(0.14f, 0.18f, 0.13f, 0.96f) : new Color(0.065f, 0.085f, 0.1f, 0.94f),
            false);
        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.padding = new RectOffset(14, 14, 8, 8);
        rowLayout.spacing = 12f;
        GameObject iconObj = new GameObject("ProstheticIcon_Image");
        iconObj.transform.SetParent(row.transform, false);
        Image icon = iconObj.AddComponent<Image>();
        string iconID = VisualAssetService.ResolveProstheticIconID(prosthetic);
        VisualUIHelper.ApplyContainSprite(icon, iconID, VisualDisplaySpecs.ProstheticIcon, Color.white, new Color(0.34f, 0.62f, 0.76f, 1f));

        GameObject materialIconObj = new GameObject("MaterialNeedIcon_Image");
        materialIconObj.transform.SetParent(row.transform, false);
        Image materialIcon = materialIconObj.AddComponent<Image>();
        VisualUIHelper.ApplyContainSprite(
            materialIcon,
            VisualAssetService.UIIconMaterialNeedID,
            new Vector2(42f, 42f),
            Color.white,
            new Color(0.72f, 0.58f, 0.32f, 1f));

        GameObject labelObj = new GameObject("ProstheticLabel_Text");
        labelObj.transform.SetParent(row.transform, false);
        Text label = labelObj.AddComponent<Text>();
        label.font = defaultFont;
        label.fontSize = 22;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
        string craftStateText = isEquipped
            ? "  Equipped"
            : canCraft
                ? string.Empty
                : $"  {craftReason}";
        label.text = $"{prosthetic.Name} [{prosthetic.SlotType}]\nMaterials: {BuildCostText(recipe.Cost)}{craftStateText}";
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(isEquipped ? 330f : 390f, 94f);

        if (isEquipped) {
            GameObject equippedObj = new GameObject("EquippedIcon_Image");
            equippedObj.transform.SetParent(row.transform, false);
            Image equippedIcon = equippedObj.AddComponent<Image>();
            VisualUIHelper.ApplyContainSprite(
                equippedIcon,
                VisualAssetService.UIIconEquippedID,
                new Vector2(48f, 48f),
                Color.white,
                new Color(0.46f, 0.72f, 0.46f, 1f));
        }

        if (!isEquipped && !canCraft) {
            GameObject lockedObj = new GameObject("LockedIcon_Image");
            lockedObj.transform.SetParent(row.transform, false);
            Image lockedIcon = lockedObj.AddComponent<Image>();
            VisualUIHelper.ApplyContainSprite(
                lockedIcon,
                VisualAssetService.UIIconLockedID,
                new Vector2(48f, 48f),
                Color.white,
                new Color(0.52f, 0.56f, 0.62f, 1f));
        }

        Button craftBtn = CreateInlineButton(
            "Craft_Button",
            isEquipped ? "Equipped" : canCraft ? "Craft" : "Locked",
            row.transform,
            new Vector2(132f, 54f),
            isEquipped ? new Color(0.25f, 0.35f, 0.28f) : canCraft ? new Color(0.25f, 0.52f, 0.7f) : new Color(0.28f, 0.3f, 0.34f),
            defaultFont,
            20);
        craftBtn.interactable = !isEquipped && canCraft;
        craftBtn.onClick.AddListener(() => {
            ExecuteProstheticCraftFromButton(recipe.RecipeID);
        });
    }

    private void CreateStudioPlaceholderRow(Font defaultFont) {
        if (prostheticListParent == null) {
            return;
        }

        GameObject row = new GameObject("StudioRecipeRow_Empty");
        row.transform.SetParent(prostheticListParent, false);
        ConfigureGeneratedRow(row, 860f, 132f);
        Image rowBg = row.AddComponent<Image>();
        VisualUIHelper.ApplySlicedSprite(
            rowBg,
            VisualAssetService.UIListRowNormalID,
            Color.white,
            new Color(0.065f, 0.085f, 0.1f, 0.94f),
            false);

        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.padding = new RectOffset(18, 18, 12, 12);
        rowLayout.spacing = 14f;

        GameObject iconObj = new GameObject("MaterialNeedIcon_Image");
        iconObj.transform.SetParent(row.transform, false);
        Image icon = iconObj.AddComponent<Image>();
        VisualUIHelper.ApplyContainSprite(
            icon,
            VisualAssetService.UIIconMaterialNeedID,
            VisualDisplaySpecs.UIIcon,
            Color.white,
            new Color(0.72f, 0.58f, 0.32f, 1f));

        GameObject labelObj = new GameObject("StudioPlaceholder_Text");
        labelObj.transform.SetParent(row.transform, false);
        Text label = labelObj.AddComponent<Text>();
        label.font = defaultFont;
        label.fontSize = 23;
        label.color = new Color(0.9f, 0.95f, 1f, 1f);
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
        label.text = "No prosthetic recipes are unlocked.\nMaintenance and chassis controls remain available from the studio tabs.";
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(660f, 96f);
    }

    private string BuildCostText(CraftingCost cost) {
        if (cost == null) {
            return "No cost";
        }

        string text = $"{cost.Money}G";
        if (cost.RequiredItems != null) {
            foreach (var item in cost.RequiredItems) {
                if (item != null) {
                    text += $" + {item.ConfigID} x{item.Count}";
                }
            }
        }

        return text;
    }

    private bool HasProstheticRecipes() {
        foreach (var kvp in ConfigManager.CraftingRecipes) {
            CraftingRecipeConfig recipe = kvp.Value;
            if (recipe != null && !string.IsNullOrEmpty(recipe.TargetProstheticID)) {
                return true;
            }
        }

        return false;
    }

    private string BuildProstheticSummary() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        DollEntity doll = player?.ActiveDoll;
        int recipeCount = 0;
        int craftableCount = 0;
        foreach (var kvp in ConfigManager.CraftingRecipes) {
            CraftingRecipeConfig recipe = kvp.Value;
            if (recipe != null && !string.IsNullOrEmpty(recipe.TargetProstheticID)) {
                recipeCount++;
                if (ProstheticCraftingService.CanCraftAndEquip(player, doll, recipe.RecipeID, out _)) {
                    craftableCount++;
                }
            }
        }

        int equippedCount = doll?.EquippedProsthetics?.Count ?? 0;
        return recipeCount > 0
            ? $"Studio recipes: {recipeCount}   Craftable: {craftableCount}   Equipped: {equippedCount}\nMaintenance and chassis are available as studio subpanels."
            : "No prosthetic recipes are available.";
    }

    private void DestroyRuntimeObject(GameObject target) {
        if (target == null) {
            return;
        }

        if (Application.isPlaying) {
            Destroy(target);
        } else {
            DestroyImmediate(target);
        }
    }

    private void EnsureSellControls() {
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        EnsureWorkshopMainSkin();
        ApplyMainButtonSkin();

        if (openSellPanelBtn == null) {
            openSellPanelBtn = CreateAnchoredButton(
                "OpenSellPanel_Button",
                "Market",
                _bottomHintPanel != null ? _bottomHintPanel.transform : transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -118f),
                new Vector2(220f, 54f),
                new Color(0.72f, 0.36f, 0.16f),
                defaultFont,
                22);
        }

        EnsureSellPanel(defaultFont);
        EnsureProstheticControls(defaultFont);
        EnsureDungeonStartLayerPanel(defaultFont);
        EnsureFormalV1PanelController();
        EnsureFormalV1EntryButtons(defaultFont);
        EnsureWorkshopMainSkin();
        ApplyMainButtonSkin();
    }

    private void EnsureFormalV1EntryButtons(Font defaultFont) {
        Transform parent = _formalV1EntryPanel != null ? _formalV1EntryPanel.transform : transform;
        openMaintenancePanelBtn = EnsureFormalV1EntryButton(openMaintenancePanelBtn, "OpenMaintenancePanel_Button", "Maintenance", parent, 0, 0, defaultFont);
        openDailyBillPanelBtn = EnsureFormalV1EntryButton(openDailyBillPanelBtn, "OpenDailyBillPanel_Button", "Daily Bill", parent, 1, 0, defaultFont);
        openShopStagingPanelBtn = EnsureFormalV1EntryButton(openShopStagingPanelBtn, "OpenShopStagingPanel_Button", "Shop", parent, 0, 1, defaultFont);
        openOrderBoardPanelBtn = EnsureFormalV1EntryButton(openOrderBoardPanelBtn, "OpenOrderBoardPanel_Button", "Orders", parent, 1, 1, defaultFont);
        openRumorBoardPanelBtn = EnsureFormalV1EntryButton(openRumorBoardPanelBtn, "OpenRumorBoardPanel_Button", "Rumors", parent, 0, 2, defaultFont);
        openBusinessSettlementPanelBtn = EnsureFormalV1EntryButton(openBusinessSettlementPanelBtn, "OpenBusinessSettlementPanel_Button", "Business", parent, 1, 2, defaultFont);
        openChassisUpgradePanelBtn = EnsureFormalV1EntryButton(openChassisUpgradePanelBtn, "OpenChassisUpgradePanel_Button", "Chassis", parent, 0, 3, defaultFont);
        openDollInteractionPanelBtn = EnsureFormalV1EntryButton(openDollInteractionPanelBtn, "OpenDollInteractionPanel_Button", "Doll Talk", parent, 1, 3, defaultFont);
        openDollRoomPanelBtn = EnsureFormalV1EntryButton(openDollRoomPanelBtn, "OpenDollRoomPanel_Button", "Doll Room", parent, 0, 4, defaultFont);
        openFactionShopPanelBtn = EnsureFormalV1EntryButton(openFactionShopPanelBtn, "OpenFactionShopPanel_Button", "Factions", parent, 1, 4, defaultFont);
        openScenarioEventPanelBtn = EnsureFormalV1EntryButton(openScenarioEventPanelBtn, "OpenScenarioEventPanel_Button", "Scenario", parent, 0, 5, defaultFont);
    }

    private Button EnsureFormalV1EntryButton(Button button, string objectName, string label, Transform parent, int column, int row, Font font) {
        if (button == null) {
            button = CreateAnchoredButton(
                objectName,
                label,
                parent,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                ResolveFormalV1EntryPosition(column, row),
                new Vector2(190f, 48f),
                new Color(0.28f, 0.34f, 0.34f),
                font,
                20);
        }

        RepositionButton(button, parent, ResolveFormalV1EntryPosition(column, row), new Vector2(190f, 48f));
        return button;
    }

    private Vector2 ResolveFormalV1EntryPosition(int column, int row) {
        float x = column == 0 ? -105f : 105f;
        return new Vector2(x, -48f - row * 62f);
    }

    private void BindFormalV1Button(Button button, string screenID) {
        if (button == null || string.IsNullOrEmpty(screenID)) {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OpenFormalV1Panel(screenID));
    }

    private void EnsureFormalV1PanelController() {
        if (_formalV1PanelController != null) {
            return;
        }

        _formalV1PanelController = GetComponent<WorkshopFormalV1PanelController>();
        if (_formalV1PanelController == null) {
            _formalV1PanelController = gameObject.AddComponent<WorkshopFormalV1PanelController>();
        }
    }

    private void EnsureSellPanel(Font defaultFont) {
        if (sellPanel != null &&
            stashHeaderText != null &&
            sellSummaryText != null &&
            stashListParent != null &&
            sellAllBtn != null &&
            closeSellPanelBtn != null) {
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        Transform panelParent = canvas != null ? canvas.transform : transform.parent ?? transform;

        sellPanel = new GameObject("WorkshopSellPanel_Runtime");
        sellPanel.transform.SetParent(panelParent, false);
        RectTransform panelRect = sellPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelBg = sellPanel.AddComponent<Image>();
        ApplyModalBackdropSkin(panelBg);

        GameObject cardObj = new GameObject("SellPanel_Card");
        cardObj.transform.SetParent(sellPanel.transform, false);
        RectTransform cardRect = cardObj.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(1180f, 720f);
        Image cardBg = cardObj.AddComponent<Image>();
        ApplyRuntimeSolidPanelSkin(cardBg, new Color(0.045f, 0.06f, 0.055f, 0.98f), new Color(0.35f, 0.42f, 0.35f, 0.36f), false);

        GameObject titleObj = new GameObject("Title_Text");
        titleObj.transform.SetParent(cardObj.transform, false);
        Text title = titleObj.AddComponent<Text>();
        title.font = defaultFont;
        title.fontSize = 34;
        title.color = new Color(0.92f, 0.88f, 0.7f);
        title.alignment = TextAnchor.MiddleLeft;
        title.raycastTarget = false;
        ConfigureReadableText(title, 22, 34);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(40f, -30f);
        titleRect.sizeDelta = new Vector2(440f, 56f);
        stashHeaderText = title;

        CreateTitleDivider(cardObj.transform, new Vector2(40f, -86f), new Vector2(520f, 32f));

        GameObject summaryObj = new GameObject("Summary_Text");
        summaryObj.transform.SetParent(cardObj.transform, false);
        Text summary = summaryObj.AddComponent<Text>();
        summary.font = defaultFont;
        summary.fontSize = 20;
        summary.color = new Color(0.88f, 0.9f, 0.84f);
        summary.alignment = TextAnchor.UpperLeft;
        summary.raycastTarget = false;
        ConfigureReadableText(summary, 14, 20);
        RectTransform summaryRect = summaryObj.GetComponent<RectTransform>();
        summaryRect.anchorMin = new Vector2(0f, 1f);
        summaryRect.anchorMax = new Vector2(0f, 1f);
        summaryRect.pivot = new Vector2(0f, 1f);
        summaryRect.anchoredPosition = new Vector2(40f, -92f);
        summaryRect.sizeDelta = new Vector2(760f, 76f);
        sellSummaryText = summary;

        sellAllBtn = CreateAnchoredButton(
            "OpenShopStaging_Button",
            "Shop Staging",
            cardObj.transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-238f, -36f),
            new Vector2(210f, 54f),
            new Color(0.72f, 0.36f, 0.16f),
            defaultFont,
            20);

        closeSellPanelBtn = CreateAnchoredButton(
            "Close_Button",
            "Close",
            cardObj.transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-56f, -36f),
            new Vector2(112f, 54f),
            new Color(0.28f, 0.3f, 0.34f),
            defaultFont,
            24);

        GameObject scrollObj = new GameObject("SellList_Scroll");
        scrollObj.transform.SetParent(cardObj.transform, false);
        RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.anchoredPosition = new Vector2(-120f, -92f);
        scrollRect.sizeDelta = new Vector2(820f, 430f);
        Image scrollBg = scrollObj.AddComponent<Image>();
        ApplyRuntimeSolidPanelSkin(scrollBg, new Color(0.02f, 0.028f, 0.026f, 0.98f), new Color(0.24f, 0.34f, 0.3f, 0.26f), false);
        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = new Vector2(-24f, -24f);
        viewportRect.anchoredPosition = Vector2.zero;
        viewportObj.AddComponent<RectMask2D>();
        scroll.viewport = viewportRect;

        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = false;
        contentLayout.childForceExpandHeight = false;
        contentLayout.spacing = 10f;
        ContentSizeFitter contentFitter = contentObj.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRect;
        stashListParent = contentObj.transform;

        sellPanel.SetActive(_sellPanelOpen);
    }

    private void EnsureProstheticControls(Font defaultFont) {
        if (openProstheticPanelBtn == null) {
            openProstheticPanelBtn = CreateAnchoredButton(
                "OpenProstheticPanel_Button",
                "Studio",
                _formalV1EntryPanel != null ? _formalV1EntryPanel.transform : transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -118f),
                new Vector2(220f, 52f),
                new Color(0.18f, 0.42f, 0.58f),
                defaultFont,
                23);
        }

        EnsureProstheticPanel(defaultFont);
    }

    private void EnsureProstheticPanel(Font defaultFont) {
        if (prostheticPanel != null &&
            prostheticHeaderText != null &&
            prostheticSummaryText != null &&
            prostheticListParent != null &&
            closeProstheticPanelBtn != null) {
            prostheticPanel.SetActive(_prostheticPanelOpen);
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        Transform panelParent = canvas != null ? canvas.transform : transform.parent ?? transform;

        prostheticPanel = new GameObject("WorkshopProstheticPanel_Runtime");
        prostheticPanel.transform.SetParent(panelParent, false);
        RectTransform panelRect = prostheticPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelBg = prostheticPanel.AddComponent<Image>();
        ApplyModalBackdropSkin(panelBg);

        GameObject cardObj = new GameObject("ProstheticPanel_Card");
        cardObj.transform.SetParent(prostheticPanel.transform, false);
        RectTransform cardRect = cardObj.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(1180f, 720f);
        Image cardBg = cardObj.AddComponent<Image>();
        ApplyRuntimeSolidPanelSkin(cardBg, new Color(0.035f, 0.055f, 0.065f, 0.98f), new Color(0.28f, 0.42f, 0.48f, 0.36f), false);

        GameObject titleObj = new GameObject("Title_Text");
        titleObj.transform.SetParent(cardObj.transform, false);
        Text title = titleObj.AddComponent<Text>();
        title.font = defaultFont;
        title.fontSize = 34;
        title.color = new Color(0.74f, 0.9f, 0.92f);
        title.alignment = TextAnchor.MiddleLeft;
        title.raycastTarget = false;
        ConfigureReadableText(title, 22, 34);
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(40f, -30f);
        titleRect.sizeDelta = new Vector2(500f, 56f);
        prostheticHeaderText = title;

        CreateTitleDivider(cardObj.transform, new Vector2(40f, -86f), new Vector2(560f, 32f));

        GameObject summaryObj = new GameObject("Summary_Text");
        summaryObj.transform.SetParent(cardObj.transform, false);
        Text summary = summaryObj.AddComponent<Text>();
        summary.font = defaultFont;
        summary.fontSize = 20;
        summary.color = new Color(0.84f, 0.92f, 0.92f);
        summary.alignment = TextAnchor.UpperLeft;
        summary.raycastTarget = false;
        ConfigureReadableText(summary, 14, 20);
        RectTransform summaryRect = summaryObj.GetComponent<RectTransform>();
        summaryRect.anchorMin = new Vector2(0f, 1f);
        summaryRect.anchorMax = new Vector2(0f, 1f);
        summaryRect.pivot = new Vector2(0f, 1f);
        summaryRect.anchoredPosition = new Vector2(40f, -92f);
        summaryRect.sizeDelta = new Vector2(760f, 76f);
        prostheticSummaryText = summary;

        closeProstheticPanelBtn = CreateAnchoredButton(
            "Close_Button",
            "Close",
            cardObj.transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-56f, -36f),
            new Vector2(112f, 54f),
            new Color(0.28f, 0.3f, 0.34f),
            defaultFont,
            22);

        Button maintenanceStudioBtn = CreateAnchoredButton(
            "StudioMaintenance_Button",
            "Maintenance",
            cardObj.transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-428f, -36f),
            new Vector2(172f, 54f),
            new Color(0.22f, 0.44f, 0.42f),
            defaultFont,
            19);
        maintenanceStudioBtn.onClick.AddListener(() => OpenFormalV1Panel("maintenance_panel"));

        Button chassisStudioBtn = CreateAnchoredButton(
            "StudioChassis_Button",
            "Chassis",
            cardObj.transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-242f, -36f),
            new Vector2(154f, 54f),
            new Color(0.2f, 0.36f, 0.52f),
            defaultFont,
            19);
        chassisStudioBtn.onClick.AddListener(() => OpenFormalV1Panel("chassis_upgrade_panel"));

        GameObject scrollObj = new GameObject("ProstheticList_Scroll");
        scrollObj.transform.SetParent(cardObj.transform, false);
        RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.anchoredPosition = new Vector2(-80f, -92f);
        scrollRect.sizeDelta = new Vector2(920f, 430f);
        Image scrollBg = scrollObj.AddComponent<Image>();
        ApplyRuntimeSolidPanelSkin(scrollBg, new Color(0.018f, 0.03f, 0.036f, 0.98f), new Color(0.22f, 0.36f, 0.42f, 0.26f), false);
        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = new Vector2(-24f, -24f);
        viewportRect.anchoredPosition = Vector2.zero;
        viewportObj.AddComponent<RectMask2D>();
        scroll.viewport = viewportRect;

        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = false;
        contentLayout.childForceExpandWidth = false;
        contentLayout.childForceExpandHeight = false;
        contentLayout.spacing = 10f;
        ContentSizeFitter contentFitter = contentObj.AddComponent<ContentSizeFitter>();
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRect;
        prostheticListParent = contentObj.transform;

        prostheticPanel.SetActive(_prostheticPanelOpen);
    }

    private void EnsureDungeonStartLayerPanel(Font defaultFont) {
        if (dungeonStartLayerPanel != null && _dungeonStartLayerController != null) {
            dungeonStartLayerPanel.SetActive(_dungeonStartLayerPanelOpen);
            return;
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        Transform panelParent = canvas != null ? canvas.transform : transform.parent ?? transform;

        dungeonStartLayerPanel = new GameObject("DungeonStartLayerPanel_Runtime");
        dungeonStartLayerPanel.transform.SetParent(panelParent, false);
        RectTransform panelRect = dungeonStartLayerPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image backdropImage = dungeonStartLayerPanel.AddComponent<Image>();
        VisualUIHelper.ApplySolidColor(backdropImage, new Color(0.012f, 0.014f, 0.013f, 1f), true);

        GameObject bgObj = new GameObject("LayerSelectBackground_Image");
        bgObj.transform.SetParent(dungeonStartLayerPanel.transform, false);
        Image panelBg = bgObj.AddComponent<Image>();
        RectTransform bgRect = panelBg.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        VisualUIHelper.ApplyCoverSprite(
            panelBg,
            VisualAssetService.ResolveLayerSelectBackgroundID(),
            Color.white,
            new Color(0.025f, 0.035f, 0.04f, 0.94f));
        panelBg.raycastTarget = false;

        _dungeonStartLayerController = dungeonStartLayerPanel.AddComponent<DungeonStartLayerUIController>();

        GameObject cardObj = new GameObject("DungeonStartLayer_Card");
        cardObj.transform.SetParent(dungeonStartLayerPanel.transform, false);
        RectTransform cardRect = cardObj.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(1040f, 760f);
        Image cardBg = cardObj.AddComponent<Image>();
        ApplyRuntimePanelSkin(cardBg, VisualAssetService.UIPanelMainID, new Color(0.08f, 0.1f, 0.11f, 0.98f), false);

        GameObject titleObj = new GameObject("Title_Text");
        titleObj.transform.SetParent(cardObj.transform, false);
        Text title = titleObj.AddComponent<Text>();
        title.font = defaultFont;
        title.fontSize = 36;
        title.color = new Color(1f, 0.84f, 0.46f);
        title.alignment = TextAnchor.MiddleLeft;
        title.raycastTarget = false;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(48f, -34f);
        titleRect.sizeDelta = new Vector2(520f, 58f);
        _dungeonStartLayerController.titleText = title;

        _dungeonStartLayerController.titleDividerImage = CreateTitleDivider(cardObj.transform, new Vector2(48f, -86f), new Vector2(620f, 30f));

        GameObject summaryObj = new GameObject("Summary_Text");
        summaryObj.transform.SetParent(cardObj.transform, false);
        Text summary = summaryObj.AddComponent<Text>();
        summary.font = defaultFont;
        summary.fontSize = 21;
        summary.color = new Color(0.86f, 0.9f, 0.86f);
        summary.alignment = TextAnchor.UpperLeft;
        summary.raycastTarget = false;
        RectTransform summaryRect = summaryObj.GetComponent<RectTransform>();
        summaryRect.anchorMin = new Vector2(0f, 1f);
        summaryRect.anchorMax = new Vector2(0f, 1f);
        summaryRect.pivot = new Vector2(0f, 1f);
        summaryRect.anchoredPosition = new Vector2(48f, -104f);
        summaryRect.sizeDelta = new Vector2(820f, 76f);
        _dungeonStartLayerController.summaryText = summary;

        GameObject listObj = new GameObject("LayerList");
        listObj.transform.SetParent(cardObj.transform, false);
        RectTransform listRect = listObj.AddComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0.5f, 0.5f);
        listRect.anchorMax = new Vector2(0.5f, 0.5f);
        listRect.pivot = new Vector2(0.5f, 0.5f);
        listRect.anchoredPosition = new Vector2(0f, -26f);
        listRect.sizeDelta = new Vector2(840f, 410f);
        VerticalLayoutGroup listLayout = listObj.AddComponent<VerticalLayoutGroup>();
        listLayout.childAlignment = TextAnchor.UpperCenter;
        listLayout.childControlWidth = false;
        listLayout.childControlHeight = false;
        listLayout.childForceExpandWidth = false;
        listLayout.childForceExpandHeight = false;
        listLayout.spacing = 14f;
        _dungeonStartLayerController.listParent = listObj.transform;

        Button confirmBtn = CreateAnchoredButton(
            "Confirm_Button",
            "开始下潜",
            cardObj.transform,
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(-48f, 42f),
            new Vector2(210f, 64f),
            new Color(0.82f, 0.48f, 0.18f),
            defaultFont,
            26);
        _dungeonStartLayerController.confirmBtn = confirmBtn;

        Button closeBtn = CreateAnchoredButton(
            "Close_Button",
            "返回",
            cardObj.transform,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(48f, 42f),
            new Vector2(170f, 64f),
            new Color(0.28f, 0.31f, 0.34f),
            defaultFont,
            26);
        _dungeonStartLayerController.closeBtn = closeBtn;

        dungeonStartLayerPanel.SetActive(_dungeonStartLayerPanelOpen);
    }

    private Button CreateActionButton(string objectName, string label, Vector2 anchoredPosition, Color color, Font font) {
        return CreateAnchoredButton(
            objectName,
            label,
            transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            anchoredPosition,
            new Vector2(180f, 56f),
            color,
            font,
            24);
    }

    private Button CreateInlineButton(string objectName, string label, Transform parent, Vector2 size, Color color, Font font, int fontSize) {
        GameObject buttonObj = new GameObject(objectName);
        buttonObj.transform.SetParent(parent, false);
        buttonObj.AddComponent<Image>();
        Button button = buttonObj.AddComponent<Button>();
        VisualUIHelper.ApplyButtonSkin(button, VisualAssetService.UIButtonSecondaryID, color);
        ApplyButtonTint(button, color);
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.sizeDelta = size;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.font = font;
        buttonText.fontSize = fontSize;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.text = label;
        buttonText.raycastTarget = false;
        ConfigureReadableText(buttonText, Mathf.Max(12, fontSize - 6), fontSize);
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        return button;
    }

    private Button CreateAnchoredButton(
        string objectName,
        string label,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 size,
        Color color,
        Font font,
        int fontSize) {
        GameObject buttonObj = new GameObject(objectName);
        buttonObj.transform.SetParent(parent, false);
        buttonObj.AddComponent<Image>();
        Button button = buttonObj.AddComponent<Button>();
        VisualUIHelper.ApplyButtonSkin(button, VisualAssetService.UIButtonSecondaryID, color);
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.pivot = pivot;
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = size;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.font = font;
        buttonText.fontSize = fontSize;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.text = label;
        buttonText.raycastTarget = false;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        return button;
    }

    private void ApplyRuntimePanelSkin(Image image, Color tint, bool raycastTarget) {
        ApplyRuntimePanelSkin(image, VisualAssetService.UIPanelInfoID, tint, raycastTarget);
    }

    private void ApplyRuntimePanelSkin(Image image, string visualID, Color tint, bool raycastTarget) {
        VisualUIHelper.ApplySlicedSprite(image, visualID, tint, tint, raycastTarget);
    }

    private void ApplyRuntimeSolidPanelSkin(Image image, Color fillColor, Color outlineColor, bool raycastTarget) {
        VisualUIHelper.ApplySolidColor(image, fillColor, raycastTarget);
        Outline outline = image.GetComponent<Outline>();
        if (outline == null) {
            outline = image.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(2f, -2f);
        outline.useGraphicAlpha = true;
    }

    private void ApplyModalBackdropSkin(Image image) {
        VisualUIHelper.ApplySolidColor(image, new Color(0.004f, 0.006f, 0.008f, 1f));
        image.raycastTarget = true;
    }

    private void ConfigureReadableText(Text text, int minSize, int maxSize) {
        if (text == null) {
            return;
        }

        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = minSize;
        text.resizeTextMaxSize = maxSize;
        text.lineSpacing = 0.94f;
    }

    private void ConfigureGeneratedRow(GameObject row, float preferredWidth, float preferredHeight) {
        if (row == null) {
            return;
        }

        RectTransform rect = row.GetComponent<RectTransform>();
        if (rect == null) {
            rect = row.AddComponent<RectTransform>();
        }

        rect.sizeDelta = new Vector2(preferredWidth, preferredHeight);
        LayoutElement layoutElement = row.GetComponent<LayoutElement>();
        if (layoutElement == null) {
            layoutElement = row.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = preferredWidth;
        layoutElement.preferredWidth = preferredWidth;
        layoutElement.minHeight = preferredHeight;
        layoutElement.preferredHeight = preferredHeight;
    }

    private void ForceRebuildGeneratedList(Transform listParent) {
        RectTransform rect = listParent as RectTransform;
        if (rect != null) {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }

        Canvas.ForceUpdateCanvases();
    }

    private void ApplyViewportMaskSkin(Image image) {
        VisualUIHelper.ApplySimpleSprite(image, VisualAssetService.UIPanelInfoID, Color.clear, Color.clear, true, false);
    }

    private void EnsureWorkshopMainSkin() {
        _topStatusPanel = EnsureDecorPanel(
            _topStatusPanel,
            "LightStatusStrip",
            new Vector2(0f, 1f),
            new Vector2(48f, -28f),
            new Vector2(620f, 108f));

        _leftActionPanel = EnsureDecorPanel(
            _leftActionPanel,
            "AbyssDoorPanel",
            new Vector2(1f, 0.5f),
            new Vector2(-170f, 40f),
            new Vector2(440f, 360f));

        _formalV1EntryPanel = EnsureDecorPanel(
            _formalV1EntryPanel,
            "WorkshopEntryPanel",
            new Vector2(0f, 0.5f),
            new Vector2(44f, -12f),
            new Vector2(310f, 190f));

        _bottomHintPanel = EnsureDecorPanel(
            _bottomHintPanel,
            "LedgerCornerPanel",
            new Vector2(1f, 0f),
            new Vector2(-170f, 170f),
            new Vector2(360f, 210f));

        _dollStatusPanel = EnsureDecorPanel(
            _dollStatusPanel,
            "DollStatusPlate",
            new Vector2(0.5f, 0f),
            new Vector2(0f, 42f),
            new Vector2(520f, 112f));

        _abyssHintText = EnsureDecorText(
            _abyssHintText,
            "AbyssHint_Text",
            _leftActionPanel != null ? _leftActionPanel.transform : transform,
            "Abyss lift\nRoute and readiness check",
            new Vector2(0.5f, 1f),
            new Vector2(0f, -44f),
            new Vector2(360f, 112f),
            24,
            new Color(0.96f, 0.86f, 0.64f, 1f),
            TextAnchor.UpperCenter);

        _studioHintText = EnsureDecorText(
            _studioHintText,
            "StudioHint_Text",
            _formalV1EntryPanel != null ? _formalV1EntryPanel.transform : transform,
            "Studio\nMaintenance, chassis, prosthetics",
            new Vector2(0.5f, 1f),
            new Vector2(0f, -26f),
            new Vector2(260f, 72f),
            20,
            new Color(0.9f, 0.88f, 0.78f, 1f),
            TextAnchor.UpperCenter);

        _ledgerHintText = EnsureDecorText(
            _ledgerHintText,
            "LedgerHint_Text",
            _bottomHintPanel != null ? _bottomHintPanel.transform : transform,
            "Ledger\nMarket, orders, bills",
            new Vector2(0.5f, 1f),
            new Vector2(0f, -32f),
            new Vector2(300f, 82f),
            22,
            new Color(0.92f, 0.86f, 0.72f, 1f),
            TextAnchor.UpperCenter);

        _dollCaptionText = EnsureDecorText(
            _dollCaptionText,
            "DollCaption_Text",
            _dollStatusPanel != null ? _dollStatusPanel.transform : transform,
            "Doll status",
            new Vector2(0.5f, 1f),
            new Vector2(0f, -14f),
            new Vector2(430f, 34f),
            18,
            new Color(0.95f, 0.88f, 0.72f, 1f),
            TextAnchor.UpperCenter);

        EnsureDollStandImage();
        MoveIntoPanel(moneyText, _topStatusPanel != null ? _topStatusPanel.transform : transform, new Vector2(24f, -14f), new Vector2(560f, 78f), 20);
        MoveIntoPanel(chassisInfoText, _dollStatusPanel != null ? _dollStatusPanel.transform : transform, new Vector2(34f, -54f), new Vector2(452f, 34f), 15);
        SetButtonVisible(upgradeBtn, false);
        RepositionButton(departBtn, _leftActionPanel != null ? _leftActionPanel.transform : transform, new Vector2(0f, -230f), new Vector2(300f, 72f));
        RepositionButton(openSellPanelBtn, _bottomHintPanel != null ? _bottomHintPanel.transform : transform, new Vector2(0f, -132f), new Vector2(220f, 54f));
        RepositionButton(openProstheticPanelBtn, _formalV1EntryPanel != null ? _formalV1EntryPanel.transform : transform, new Vector2(0f, -118f), new Vector2(220f, 52f));
        SetButtonLabel(departBtn, "Descend", 28);
        SetButtonLabel(openSellPanelBtn, "Market", 22);
        SetButtonLabel(openProstheticPanelBtn, "Studio", 23);
        Transform formalParent = _formalV1EntryPanel != null ? _formalV1EntryPanel.transform : transform;
        RepositionButton(openMaintenancePanelBtn, formalParent, ResolveFormalV1EntryPosition(0, 0), new Vector2(190f, 48f));
        RepositionButton(openDailyBillPanelBtn, formalParent, ResolveFormalV1EntryPosition(1, 0), new Vector2(190f, 48f));
        RepositionButton(openShopStagingPanelBtn, formalParent, ResolveFormalV1EntryPosition(0, 1), new Vector2(190f, 48f));
        RepositionButton(openOrderBoardPanelBtn, formalParent, ResolveFormalV1EntryPosition(1, 1), new Vector2(190f, 48f));
        RepositionButton(openRumorBoardPanelBtn, formalParent, ResolveFormalV1EntryPosition(0, 2), new Vector2(190f, 48f));
        RepositionButton(openBusinessSettlementPanelBtn, formalParent, ResolveFormalV1EntryPosition(1, 2), new Vector2(190f, 48f));
        RepositionButton(openChassisUpgradePanelBtn, formalParent, ResolveFormalV1EntryPosition(0, 3), new Vector2(190f, 48f));
        RepositionButton(openDollInteractionPanelBtn, formalParent, ResolveFormalV1EntryPosition(1, 3), new Vector2(190f, 48f));
        RepositionButton(openDollRoomPanelBtn, formalParent, ResolveFormalV1EntryPosition(0, 4), new Vector2(190f, 48f));
        RepositionButton(openFactionShopPanelBtn, formalParent, ResolveFormalV1EntryPosition(1, 4), new Vector2(190f, 48f));
        RepositionButton(openScenarioEventPanelBtn, formalParent, ResolveFormalV1EntryPosition(0, 5), new Vector2(190f, 48f));
        SetFormalEntryButtonsVisible(false);
    }

    private Image EnsureDecorPanel(Image current, string objectName, Vector2 anchor, Vector2 position, Vector2 size) {
        Image image = current;
        if (image == null) {
            Transform existing = transform.Find(objectName);
            image = existing != null ? existing.GetComponent<Image>() : null;
        }

        if (image == null) {
            GameObject panelObj = new GameObject(objectName);
            panelObj.transform.SetParent(transform, false);
            image = panelObj.AddComponent<Image>();
        }

        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        VisualUIHelper.ApplySlicedSprite(image, VisualAssetService.UIPanelInfoID, Color.white, new Color(0.08f, 0.08f, 0.07f, 0.92f), false);
        image.transform.SetAsLastSibling();
        return image;
    }

    private Text EnsureDecorText(
        Text current,
        string objectName,
        Transform parent,
        string value,
        Vector2 anchor,
        Vector2 position,
        Vector2 size,
        int fontSize,
        Color color,
        TextAnchor alignment) {
        if (parent == null) {
            parent = transform;
        }

        Text text = current;
        if (text == null) {
            Transform existing = parent.Find(objectName);
            text = existing != null ? existing.GetComponent<Text>() : null;
        }

        if (text == null) {
            GameObject textObj = new GameObject(objectName);
            textObj.transform.SetParent(parent, false);
            text = textObj.AddComponent<Text>();
        } else if (text.transform.parent != parent) {
            text.transform.SetParent(parent, false);
        }

        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.text = value;
        ConfigureReadableText(text, Mathf.Max(12, fontSize - 6), fontSize);

        RectTransform rect = text.rectTransform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return text;
    }

    private void SetFormalEntryButtonsVisible(bool visible) {
        SetButtonVisible(openMaintenancePanelBtn, visible);
        SetButtonVisible(openDailyBillPanelBtn, visible);
        SetButtonVisible(openShopStagingPanelBtn, visible);
        SetButtonVisible(openOrderBoardPanelBtn, visible);
        SetButtonVisible(openRumorBoardPanelBtn, visible);
        SetButtonVisible(openBusinessSettlementPanelBtn, visible);
        SetButtonVisible(openChassisUpgradePanelBtn, visible);
        SetButtonVisible(openDollInteractionPanelBtn, visible);
        SetButtonVisible(openDollRoomPanelBtn, visible);
        SetButtonVisible(openFactionShopPanelBtn, visible);
        SetButtonVisible(openScenarioEventPanelBtn, visible);
    }

    private void SetButtonVisible(Button button, bool visible) {
        if (button == null) {
            return;
        }

        button.gameObject.SetActive(visible);
    }

    private void SetButtonLabel(Button button, string label, int fontSize) {
        if (button == null) {
            return;
        }

        Text text = button.GetComponentInChildren<Text>(true);
        if (text == null) {
            return;
        }

        text.text = label;
        text.fontSize = fontSize;
    }

    private Image CreateTitleDivider(Transform parent, Vector2 anchoredPosition, Vector2 size) {
        if (parent == null) {
            return null;
        }

        GameObject dividerObj = new GameObject("TitleDivider_Image");
        dividerObj.transform.SetParent(parent, false);
        Image divider = dividerObj.AddComponent<Image>();
        RectTransform rect = divider.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        VisualUIHelper.ApplySimpleSprite(
            divider,
            VisualAssetService.UITitleDividerID,
            new Color(0.62f, 0.76f, 0.72f, 0.32f),
            new Color(0.32f, 0.42f, 0.4f, 0.36f),
            false,
            false);
        return divider;
    }

    private void EnsureDollStandImage() {
        if (_dollStandImage == null) {
            Transform existing = transform.Find("DollDisplay/DollImage");
            _dollStandImage = existing != null ? existing.GetComponent<Image>() : null;
        }

        if (_dollStandImage == null) {
            GameObject displayObj = new GameObject("DollDisplay");
            displayObj.transform.SetParent(transform, false);
            RectTransform displayRect = displayObj.AddComponent<RectTransform>();

            GameObject imageObj = new GameObject("DollImage");
            imageObj.transform.SetParent(displayObj.transform, false);
            _dollStandImage = imageObj.AddComponent<Image>();
        }

        RectTransform display = _dollStandImage.transform.parent as RectTransform;
        if (display != null) {
            display.anchorMin = new Vector2(0.5f, 0.5f);
            display.anchorMax = new Vector2(0.5f, 0.5f);
            display.pivot = new Vector2(0.5f, 0.5f);
            display.anchoredPosition = new Vector2(0f, -40f);
            display.sizeDelta = new Vector2(620f, 760f);
        }

        RectTransform rect = _dollStandImage.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = Vector2.zero;
        VisualUIHelper.ApplyContainSprite(
            _dollStandImage,
            "doll_proto_0_stand",
            VisualDisplaySpecs.DollStand,
            Color.white,
            new Color(0.42f, 0.32f, 0.24f, 0.92f),
            false);
        _dollStandImage.transform.parent.SetAsLastSibling();
    }

    private void ApplyMainButtonSkin() {
        ApplyTintedButtonSkin(departBtn, VisualAssetService.UIButtonPrimaryID, new Color(0.42f, 0.58f, 0.5f, 0.86f));
        ApplyTintedButtonSkin(upgradeBtn, VisualAssetService.UIButtonSecondaryID, new Color(0.26f, 0.42f, 0.36f, 0.82f));
        ApplyTintedButtonSkin(openSellPanelBtn, VisualAssetService.UIButtonSecondaryID, new Color(0.38f, 0.34f, 0.25f, 0.82f));
        ApplyTintedButtonSkin(openProstheticPanelBtn, VisualAssetService.UIButtonSecondaryID, new Color(0.22f, 0.38f, 0.44f, 0.82f));
        ApplyFormalV1EntryButtonSkin(openMaintenancePanelBtn);
        ApplyFormalV1EntryButtonSkin(openDailyBillPanelBtn);
        ApplyFormalV1EntryButtonSkin(openShopStagingPanelBtn);
        ApplyFormalV1EntryButtonSkin(openOrderBoardPanelBtn);
        ApplyFormalV1EntryButtonSkin(openRumorBoardPanelBtn);
        ApplyFormalV1EntryButtonSkin(openBusinessSettlementPanelBtn);
        ApplyFormalV1EntryButtonSkin(openChassisUpgradePanelBtn);
        ApplyFormalV1EntryButtonSkin(openDollInteractionPanelBtn);
        ApplyFormalV1EntryButtonSkin(openDollRoomPanelBtn);
        ApplyFormalV1EntryButtonSkin(openFactionShopPanelBtn);
        ApplyFormalV1EntryButtonSkin(openScenarioEventPanelBtn);
    }

    private void ApplyFormalV1EntryButtonSkin(Button button) {
        ApplyTintedButtonSkin(button, VisualAssetService.UIButtonSecondaryID, new Color(0.24f, 0.32f, 0.32f, 0.78f));
    }

    private void ApplyTintedButtonSkin(Button button, string visualID, Color tint) {
        VisualUIHelper.ApplyButtonSkin(button, visualID, tint);
        ApplyButtonTint(button, tint);
    }

    private void ApplyButtonTint(Button button, Color tint) {
        if (button == null) {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null) {
            image.color = tint;
        }
    }

    private void MoveIntoPanel(Text text, Transform parent, Vector2 topLeftOffset, Vector2 size, int fontSize) {
        if (text == null || parent == null) {
            return;
        }

        text.transform.SetParent(parent, false);
        text.fontSize = fontSize;
        text.color = new Color(1f, 0.9f, 0.62f, 1f);
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = Mathf.Max(12, fontSize - 6);
        text.resizeTextMaxSize = fontSize;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = topLeftOffset;
        rect.sizeDelta = size;
    }

    private void RepositionButton(Button button, Transform parent, Vector2 anchoredPosition, Vector2 size) {
        if (button == null || parent == null) {
            return;
        }

        button.transform.SetParent(parent, false);
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private void ApplyWorkshopBackground() {
        RectTransform rootRect = transform as RectTransform;
        if (rootRect != null) {
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
        }

        backgroundImage = VisualUIHelper.EnsurePanelBackground(transform, backgroundImage, "WorkshopBackground_Image");
        string visualID = VisualAssetService.ResolveWorkshopBackgroundID();
        VisualUIHelper.ApplyCoverSprite(backgroundImage, visualID, Color.white, new Color(0.1f, 0.085f, 0.065f, 0.92f));
    }
}
