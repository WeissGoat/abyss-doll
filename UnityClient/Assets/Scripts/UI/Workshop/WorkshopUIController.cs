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

    private bool _sellPanelOpen;
    private bool _prostheticPanelOpen;
    private bool _dungeonStartLayerPanelOpen;
    private DungeonStartLayerUIController _dungeonStartLayerController;
    private Image _topStatusPanel;
    private Image _leftActionPanel;
    private Image _bottomHintPanel;
    private Image _dollStandImage;

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
                $"Money: {player.Money}G\n" +
                $"Sellable Items: {sellableCount}  Backpack {backpackCount} / Stash {stashCount}\n" +
                $"Estimated Value: {sellableEstimatedValue}G";
        }

        var chassis = player.ActiveDoll.Chassis;
        if (chassisInfoText != null) {
            chassisInfoText.text = $"Current Chassis: {chassis.ChassisID}\nCapacity: {chassis.GridWidth}x{chassis.GridHeight}";
        }

        if (stashHeaderText != null) {
            stashHeaderText.text = "Sell Items";
        }

        if (sellSummaryText != null) {
            sellSummaryText.text = sellableCount > 0
                ? $"Backpack {backpackCount} / Stash {stashCount}\nEstimated Value: {sellableEstimatedValue}G"
                : "No items available to sell.";
        }

        if (openSellPanelBtn != null) {
            openSellPanelBtn.interactable = sellableCount > 0;
        }

        if (sellAllBtn != null) {
            sellAllBtn.interactable = sellableCount > 0;
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
            upgradeBtn.onClick.AddListener(() => {
                GameRoot.Core.Workshop.UpgradeDollChassis(GameRoot.Core.CurrentPlayer.ActiveDoll);
                RefreshUI();

                var chassis = GameRoot.Core.CurrentPlayer.ActiveDoll.Chassis;
                FindObjectOfType<GridGenerator>().GenerateGrid(chassis);
            });
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
            sellAllBtn.onClick.AddListener(() => {
                GameRoot.Core.Workshop.SellAllStashItems(GameRoot.Core.CurrentPlayer);
                RefreshUI();
            });
        }

        if (openProstheticPanelBtn != null) {
            openProstheticPanelBtn.onClick.RemoveAllListeners();
            openProstheticPanelBtn.onClick.AddListener(OpenProstheticPanel);
        }

        if (closeProstheticPanelBtn != null) {
            closeProstheticPanelBtn.onClick.RemoveAllListeners();
            closeProstheticPanelBtn.onClick.AddListener(CloseProstheticPanel);
        }
    }

    public void OpenSellPanel() {
        EnsureSellControls();
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

        if (grid != null) {
            foreach (var item in grid.ContainedItems) {
                CreateSellRow(item, defaultFont, "Backpack");
            }
        }

        foreach (var item in player.StashInventory) {
            CreateSellRow(item, defaultFont, "Stash");
        }
    }

    private void CreateSellRow(ItemEntity item, Font defaultFont, string sourceLabel) {
        if (stashListParent == null || item == null) {
            return;
        }

        GameObject row = new GameObject($"SellRow_{item.InstanceID}");
        row.transform.SetParent(stashListParent, false);
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
        ContentSizeFitter rowFitter = row.AddComponent<ContentSizeFitter>();
        rowFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

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
        label.text = $"[{sourceLabel}] {item.Name}  [{item.BaseValue}G]";
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(420f, 64f);

        GameObject sellBtnObj = new GameObject("Sell_Button");
        sellBtnObj.transform.SetParent(row.transform, false);
        sellBtnObj.AddComponent<Image>();
        Button sellBtn = sellBtnObj.AddComponent<Button>();
        VisualUIHelper.ApplyButtonSkin(sellBtn, VisualAssetService.UIButtonSecondaryID, new Color(0.86f, 0.45f, 0.18f));
        RectTransform sellBtnRect = sellBtnObj.GetComponent<RectTransform>();
        sellBtnRect.sizeDelta = new Vector2(140f, 52f);

        ItemEntity capturedItem = item;
        sellBtn.onClick.AddListener(() => {
            GameRoot.Core.Workshop.SellItem(capturedItem, GameRoot.Core.CurrentPlayer);
            RefreshUI();
        });

        GameObject sellTextObj = new GameObject("Text");
        sellTextObj.transform.SetParent(sellBtnObj.transform, false);
        Text sellText = sellTextObj.AddComponent<Text>();
        sellText.font = defaultFont;
        sellText.fontSize = 22;
        sellText.color = Color.white;
        sellText.alignment = TextAnchor.MiddleCenter;
        sellText.raycastTarget = false;
        sellText.text = "Sell";
        RectTransform sellTextRect = sellTextObj.GetComponent<RectTransform>();
        sellTextRect.anchorMin = Vector2.zero;
        sellTextRect.anchorMax = Vector2.one;
        sellTextRect.sizeDelta = Vector2.zero;
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
        sellableCount = backpackCount + stashCount;
        sellableEstimatedValue = 0;

        if (grid != null) {
            foreach (var item in grid.ContainedItems) {
                if (item != null) {
                    sellableEstimatedValue += item.BaseValue;
                }
            }
        }

        foreach (var item in player.StashInventory) {
            if (item != null) {
                sellableEstimatedValue += item.BaseValue;
            }
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
            prostheticHeaderText.text = "Prosthetic Workshop";
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        foreach (var kvp in ConfigManager.CraftingRecipes) {
            CraftingRecipeConfig recipe = kvp.Value;
            if (recipe == null || string.IsNullOrEmpty(recipe.TargetProstheticID)) {
                continue;
            }

            if (!ConfigManager.Prosthetics.TryGetValue(recipe.TargetProstheticID, out var prosthetic)) {
                continue;
            }

            CreateProstheticRow(recipe, prosthetic, defaultFont);
        }
    }

    private void CreateProstheticRow(CraftingRecipeConfig recipe, ProstheticEntity prosthetic, Font defaultFont) {
        GameObject row = new GameObject($"ProstheticRow_{prosthetic.ProstheticID}");
        row.transform.SetParent(prostheticListParent, false);
        bool isEquipped = GameRoot.Core.CurrentPlayer.ActiveDoll.EquippedProsthetics.Contains(prosthetic.ProstheticID);
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
        ContentSizeFitter rowFitter = row.AddComponent<ContentSizeFitter>();
        rowFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject iconObj = new GameObject("ProstheticIcon_Image");
        iconObj.transform.SetParent(row.transform, false);
        Image icon = iconObj.AddComponent<Image>();
        string iconID = VisualAssetService.ResolveProstheticIconID(prosthetic);
        VisualUIHelper.ApplyContainSprite(icon, iconID, VisualDisplaySpecs.ProstheticIcon, Color.white, new Color(0.34f, 0.62f, 0.76f, 1f));

        GameObject labelObj = new GameObject("ProstheticLabel_Text");
        labelObj.transform.SetParent(row.transform, false);
        Text label = labelObj.AddComponent<Text>();
        label.font = defaultFont;
        label.fontSize = 22;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
        label.text = $"{prosthetic.Name} [{prosthetic.SlotType}]\n{BuildCostText(recipe.Cost)}{(isEquipped ? "  Equipped" : string.Empty)}";
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(isEquipped ? 438f : 500f, 80f);

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

        bool canCraft = GameRoot.Core.Workshop.CanAfford(recipe.Cost, GameRoot.Core.CurrentPlayer);
        Button craftBtn = CreateInlineButton(
            "Craft_Button",
            isEquipped ? "Equipped" : "Craft",
            row.transform,
            new Vector2(150f, 54f),
            isEquipped ? new Color(0.25f, 0.35f, 0.28f) : new Color(0.25f, 0.52f, 0.7f),
            defaultFont,
            22);
        craftBtn.interactable = !isEquipped && canCraft;
        craftBtn.onClick.AddListener(() => {
            GameRoot.Core.Workshop.CraftAndEquipProsthetic(recipe.RecipeID, GameRoot.Core.CurrentPlayer.ActiveDoll);
            RefreshUI();
        });
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
        int recipeCount = 0;
        foreach (var kvp in ConfigManager.CraftingRecipes) {
            CraftingRecipeConfig recipe = kvp.Value;
            if (recipe != null && !string.IsNullOrEmpty(recipe.TargetProstheticID)) {
                recipeCount++;
            }
        }

        int equippedCount = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.EquippedProsthetics?.Count ?? 0;
        return recipeCount > 0
            ? $"Recipes: {recipeCount}   Equipped: {equippedCount}\nCrafted prosthetics are equipped immediately."
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
                "Sell Items",
                _leftActionPanel != null ? _leftActionPanel.transform : transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -190f),
                new Vector2(250f, 70f),
                new Color(0.72f, 0.36f, 0.16f),
                defaultFont,
                28);
        }

        EnsureSellPanel(defaultFont);
        EnsureProstheticControls(defaultFont);
        EnsureDungeonStartLayerPanel(defaultFont);
        ApplyMainButtonSkin();
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
        panelRect.sizeDelta = Vector2.zero;
        Image panelBg = sellPanel.AddComponent<Image>();
        ApplyModalBackdropSkin(panelBg);

        GameObject cardObj = new GameObject("SellPanel_Card");
        cardObj.transform.SetParent(sellPanel.transform, false);
        RectTransform cardRect = cardObj.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(1160f, 780f);
        Image cardBg = cardObj.AddComponent<Image>();
        ApplyRuntimePanelSkin(cardBg, VisualAssetService.UIPanelMainID, new Color(0.12f, 0.11f, 0.095f, 0.98f), false);

        GameObject titleObj = new GameObject("Title_Text");
        titleObj.transform.SetParent(cardObj.transform, false);
        Text title = titleObj.AddComponent<Text>();
        title.font = defaultFont;
        title.fontSize = 40;
        title.color = new Color(1f, 0.88f, 0.48f);
        title.alignment = TextAnchor.MiddleLeft;
        title.raycastTarget = false;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(40f, -30f);
        titleRect.sizeDelta = new Vector2(360f, 64f);
        stashHeaderText = title;

        CreateTitleDivider(cardObj.transform, new Vector2(40f, -86f), new Vector2(520f, 32f));

        GameObject summaryObj = new GameObject("Summary_Text");
        summaryObj.transform.SetParent(cardObj.transform, false);
        Text summary = summaryObj.AddComponent<Text>();
        summary.font = defaultFont;
        summary.fontSize = 24;
        summary.color = new Color(0.9f, 0.9f, 0.84f);
        summary.alignment = TextAnchor.UpperLeft;
        summary.raycastTarget = false;
        RectTransform summaryRect = summaryObj.GetComponent<RectTransform>();
        summaryRect.anchorMin = new Vector2(0f, 1f);
        summaryRect.anchorMax = new Vector2(0f, 1f);
        summaryRect.pivot = new Vector2(0f, 1f);
        summaryRect.anchoredPosition = new Vector2(40f, -96f);
        summaryRect.sizeDelta = new Vector2(560f, 80f);
        sellSummaryText = summary;

        sellAllBtn = CreateAnchoredButton(
            "SellAll_Button",
            "Sell All",
            cardObj.transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-210f, -36f),
            new Vector2(160f, 56f),
            new Color(0.7f, 0.2f, 0.18f),
            defaultFont,
            24);

        closeSellPanelBtn = CreateAnchoredButton(
            "Close_Button",
            "Close",
            cardObj.transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-40f, -36f),
            new Vector2(140f, 56f),
            new Color(0.28f, 0.3f, 0.34f),
            defaultFont,
            24);

        GameObject scrollObj = new GameObject("SellList_Scroll");
        scrollObj.transform.SetParent(cardObj.transform, false);
        RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.anchoredPosition = new Vector2(0f, -85f);
        scrollRect.sizeDelta = new Vector2(1060f, 560f);
        Image scrollBg = scrollObj.AddComponent<Image>();
        ApplyRuntimePanelSkin(scrollBg, VisualAssetService.UIPanelMainID, new Color(0.055f, 0.055f, 0.055f, 0.92f), false);
        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = new Vector2(-24f, -24f);
        viewportRect.anchoredPosition = Vector2.zero;
        Image viewportImage = viewportObj.AddComponent<Image>();
        ApplyViewportMaskSkin(viewportImage);
        Mask viewportMask = viewportObj.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
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
        contentLayout.childControlWidth = false;
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
                "Prosthetics",
                _leftActionPanel != null ? _leftActionPanel.transform : transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -280f),
                new Vector2(250f, 70f),
                new Color(0.18f, 0.42f, 0.58f),
                defaultFont,
                28);
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
        panelRect.sizeDelta = Vector2.zero;
        Image panelBg = prostheticPanel.AddComponent<Image>();
        ApplyModalBackdropSkin(panelBg);

        GameObject cardObj = new GameObject("ProstheticPanel_Card");
        cardObj.transform.SetParent(prostheticPanel.transform, false);
        RectTransform cardRect = cardObj.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(1160f, 780f);
        Image cardBg = cardObj.AddComponent<Image>();
        ApplyRuntimePanelSkin(cardBg, VisualAssetService.UIPanelMainID, new Color(0.07f, 0.095f, 0.12f, 0.98f), false);

        GameObject titleObj = new GameObject("Title_Text");
        titleObj.transform.SetParent(cardObj.transform, false);
        Text title = titleObj.AddComponent<Text>();
        title.font = defaultFont;
        title.fontSize = 40;
        title.color = new Color(0.72f, 0.9f, 1f);
        title.alignment = TextAnchor.MiddleLeft;
        title.raycastTarget = false;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(40f, -30f);
        titleRect.sizeDelta = new Vector2(460f, 64f);
        prostheticHeaderText = title;

        CreateTitleDivider(cardObj.transform, new Vector2(40f, -86f), new Vector2(560f, 32f));

        GameObject summaryObj = new GameObject("Summary_Text");
        summaryObj.transform.SetParent(cardObj.transform, false);
        Text summary = summaryObj.AddComponent<Text>();
        summary.font = defaultFont;
        summary.fontSize = 24;
        summary.color = new Color(0.86f, 0.92f, 0.96f);
        summary.alignment = TextAnchor.UpperLeft;
        summary.raycastTarget = false;
        RectTransform summaryRect = summaryObj.GetComponent<RectTransform>();
        summaryRect.anchorMin = new Vector2(0f, 1f);
        summaryRect.anchorMax = new Vector2(0f, 1f);
        summaryRect.pivot = new Vector2(0f, 1f);
        summaryRect.anchoredPosition = new Vector2(40f, -96f);
        summaryRect.sizeDelta = new Vector2(700f, 80f);
        prostheticSummaryText = summary;

        closeProstheticPanelBtn = CreateAnchoredButton(
            "Close_Button",
            "Close",
            cardObj.transform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-40f, -36f),
            new Vector2(140f, 56f),
            new Color(0.28f, 0.3f, 0.34f),
            defaultFont,
            24);

        GameObject scrollObj = new GameObject("ProstheticList_Scroll");
        scrollObj.transform.SetParent(cardObj.transform, false);
        RectTransform scrollRect = scrollObj.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.anchoredPosition = new Vector2(0f, -85f);
        scrollRect.sizeDelta = new Vector2(1060f, 560f);
        Image scrollBg = scrollObj.AddComponent<Image>();
        ApplyRuntimePanelSkin(scrollBg, VisualAssetService.UIPanelMainID, new Color(0.045f, 0.06f, 0.075f, 0.92f), false);
        ScrollRect scroll = scrollObj.AddComponent<ScrollRect>();
        scroll.horizontal = false;

        GameObject viewportObj = new GameObject("Viewport");
        viewportObj.transform.SetParent(scrollObj.transform, false);
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = new Vector2(-24f, -24f);
        viewportRect.anchoredPosition = Vector2.zero;
        Image viewportImage = viewportObj.AddComponent<Image>();
        ApplyViewportMaskSkin(viewportImage);
        Mask viewportMask = viewportObj.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
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
        contentLayout.childControlWidth = false;
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
        panelRect.sizeDelta = Vector2.zero;

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
        cardRect.sizeDelta = new Vector2(960f, 720f);
        Image cardBg = cardObj.AddComponent<Image>();
        ApplyRuntimePanelSkin(cardBg, VisualAssetService.UIPanelMainID, new Color(0.08f, 0.1f, 0.11f, 0.98f), false);

        GameObject titleObj = new GameObject("Title_Text");
        titleObj.transform.SetParent(cardObj.transform, false);
        Text title = titleObj.AddComponent<Text>();
        title.font = defaultFont;
        title.fontSize = 42;
        title.color = new Color(1f, 0.84f, 0.46f);
        title.alignment = TextAnchor.MiddleLeft;
        title.raycastTarget = false;
        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(48f, -36f);
        titleRect.sizeDelta = new Vector2(480f, 70f);
        _dungeonStartLayerController.titleText = title;

        _dungeonStartLayerController.titleDividerImage = CreateTitleDivider(cardObj.transform, new Vector2(48f, -92f), new Vector2(560f, 32f));

        GameObject summaryObj = new GameObject("Summary_Text");
        summaryObj.transform.SetParent(cardObj.transform, false);
        Text summary = summaryObj.AddComponent<Text>();
        summary.font = defaultFont;
        summary.fontSize = 24;
        summary.color = new Color(0.86f, 0.9f, 0.86f);
        summary.alignment = TextAnchor.UpperLeft;
        summary.raycastTarget = false;
        RectTransform summaryRect = summaryObj.GetComponent<RectTransform>();
        summaryRect.anchorMin = new Vector2(0f, 1f);
        summaryRect.anchorMax = new Vector2(0f, 1f);
        summaryRect.pivot = new Vector2(0f, 1f);
        summaryRect.anchoredPosition = new Vector2(48f, -110f);
        summaryRect.sizeDelta = new Vector2(760f, 82f);
        _dungeonStartLayerController.summaryText = summary;

        GameObject listObj = new GameObject("LayerList");
        listObj.transform.SetParent(cardObj.transform, false);
        RectTransform listRect = listObj.AddComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0.5f, 0.5f);
        listRect.anchorMax = new Vector2(0.5f, 0.5f);
        listRect.pivot = new Vector2(0.5f, 0.5f);
        listRect.anchoredPosition = new Vector2(0f, -34f);
        listRect.sizeDelta = new Vector2(800f, 390f);
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
        VisualUIHelper.ApplySlicedSprite(image, visualID, Color.white, tint, raycastTarget);
    }

    private void ApplyModalBackdropSkin(Image image) {
        VisualUIHelper.ApplySolidColor(image, new Color(0.012f, 0.014f, 0.013f, 1f), true);
    }

    private void ApplyViewportMaskSkin(Image image) {
        VisualUIHelper.ApplySimpleSprite(image, VisualAssetService.UIPanelInfoID, Color.clear, Color.clear, true, false);
    }

    private void EnsureWorkshopMainSkin() {
        _topStatusPanel = EnsureDecorPanel(
            _topStatusPanel,
            "TopStatusPanel",
            new Vector2(0.5f, 1f),
            new Vector2(0f, -68f),
            new Vector2(1792f, 72f));

        _leftActionPanel = EnsureDecorPanel(
            _leftActionPanel,
            "LeftActionPanel",
            new Vector2(0f, 0.5f),
            new Vector2(306f, 0f),
            new Vector2(420f, 520f));

        _bottomHintPanel = EnsureDecorPanel(
            _bottomHintPanel,
            "BottomHintArea",
            new Vector2(0f, 0f),
            new Vector2(306f, 180f),
            new Vector2(420f, 180f));

        EnsureDollStandImage();
        MoveIntoPanel(moneyText, _topStatusPanel != null ? _topStatusPanel.transform : transform, new Vector2(28f, -10f), new Vector2(820f, 58f), 26);
        MoveIntoPanel(chassisInfoText, _bottomHintPanel != null ? _bottomHintPanel.transform : transform, new Vector2(24f, -18f), new Vector2(372f, 138f), 24);
        RepositionButton(upgradeBtn, _leftActionPanel != null ? _leftActionPanel.transform : transform, new Vector2(0f, -100f), new Vector2(250f, 70f));
        RepositionButton(departBtn, _leftActionPanel != null ? _leftActionPanel.transform : transform, new Vector2(0f, -20f), new Vector2(250f, 70f));
        RepositionButton(openSellPanelBtn, _leftActionPanel != null ? _leftActionPanel.transform : transform, new Vector2(0f, -190f), new Vector2(250f, 70f));
        RepositionButton(openProstheticPanelBtn, _leftActionPanel != null ? _leftActionPanel.transform : transform, new Vector2(0f, -280f), new Vector2(250f, 70f));
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
            Color.white,
            new Color(0.72f, 0.58f, 0.32f, 0.9f),
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
            displayRect.anchorMin = new Vector2(1f, 0f);
            displayRect.anchorMax = new Vector2(1f, 0f);
            displayRect.pivot = new Vector2(1f, 0f);
            displayRect.anchoredPosition = new Vector2(-220f, 90f);
            displayRect.sizeDelta = new Vector2(520f, 820f);

            GameObject imageObj = new GameObject("DollImage");
            imageObj.transform.SetParent(displayObj.transform, false);
            _dollStandImage = imageObj.AddComponent<Image>();
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
        VisualUIHelper.ApplyButtonSkin(departBtn, VisualAssetService.UIButtonPrimaryID, new Color(0.8f, 0.4f, 0.2f));
        VisualUIHelper.ApplyButtonSkin(upgradeBtn, VisualAssetService.UIButtonSecondaryID, new Color(0.2f, 0.6f, 0.2f));
        VisualUIHelper.ApplyButtonSkin(openSellPanelBtn, VisualAssetService.UIButtonSecondaryID, new Color(0.72f, 0.36f, 0.16f));
        VisualUIHelper.ApplyButtonSkin(openProstheticPanelBtn, VisualAssetService.UIButtonSecondaryID, new Color(0.18f, 0.42f, 0.58f));
    }

    private void MoveIntoPanel(Text text, Transform parent, Vector2 topLeftOffset, Vector2 size, int fontSize) {
        if (text == null || parent == null) {
            return;
        }

        text.transform.SetParent(parent, false);
        text.fontSize = fontSize;
        text.color = new Color(1f, 0.9f, 0.62f, 1f);
        text.raycastTarget = false;

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
        backgroundImage = VisualUIHelper.EnsurePanelBackground(transform, backgroundImage, "WorkshopBackground_Image");
        string visualID = VisualAssetService.ResolveWorkshopBackgroundID();
        VisualUIHelper.ApplyCoverSprite(backgroundImage, visualID, Color.white, new Color(0.1f, 0.085f, 0.065f, 0.92f));
    }
}
