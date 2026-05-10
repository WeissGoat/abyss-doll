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
    private bool _sellPanelOpen;
    private bool _prostheticPanelOpen;

    void Start() {
        EnsureSellControls();
        BindButtons();
        CloseSellPanel(false);
        CloseProstheticPanel(false);
        RefreshUI();
    }

    public void RefreshUI() {
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
                GameFlowController.Instance.DepartToDungeon();
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
        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.spacing = 12f;
        ContentSizeFitter rowFitter = row.AddComponent<ContentSizeFitter>();
        rowFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject iconObj = new GameObject("ItemIcon_Image");
        iconObj.transform.SetParent(row.transform, false);
        Image icon = iconObj.AddComponent<Image>();
        string iconID = VisualAssetService.ResolveItemIconID(item);
        bool hasRegisteredIcon = VisualAssetService.TryGetSprite(iconID, out Sprite iconSprite);
        icon.sprite = hasRegisteredIcon ? iconSprite : VisualAssetService.GetSprite(iconID);
        icon.type = Image.Type.Simple;
        icon.preserveAspect = true;
        icon.color = hasRegisteredIcon ? Color.white : ResolveItemTint(item);
        icon.raycastTarget = false;
        RectTransform iconRect = iconObj.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(44f, 44f);

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
        labelRect.sizeDelta = new Vector2(420f, 44f);

        GameObject sellBtnObj = new GameObject("Sell_Button");
        sellBtnObj.transform.SetParent(row.transform, false);
        Image sellBtnImg = sellBtnObj.AddComponent<Image>();
        sellBtnImg.color = new Color(0.86f, 0.45f, 0.18f);
        Button sellBtn = sellBtnObj.AddComponent<Button>();
        RectTransform sellBtnRect = sellBtnObj.GetComponent<RectTransform>();
        sellBtnRect.sizeDelta = new Vector2(140f, 44f);

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
        HorizontalLayoutGroup rowLayout = row.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = false;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;
        rowLayout.spacing = 12f;
        ContentSizeFitter rowFitter = row.AddComponent<ContentSizeFitter>();
        rowFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        rowFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject labelObj = new GameObject("ProstheticLabel_Text");
        labelObj.transform.SetParent(row.transform, false);
        Text label = labelObj.AddComponent<Text>();
        label.font = defaultFont;
        label.fontSize = 22;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleLeft;
        label.raycastTarget = false;
        bool isEquipped = GameRoot.Core.CurrentPlayer.ActiveDoll.EquippedProsthetics.Contains(prosthetic.ProstheticID);
        label.text = $"{prosthetic.Name} [{prosthetic.SlotType}]\n{BuildCostText(recipe.Cost)}{(isEquipped ? "  Equipped" : string.Empty)}";
        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(500f, 64f);

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

        if (openSellPanelBtn == null) {
            openSellPanelBtn = CreateAnchoredButton(
                "OpenSellPanel_Button",
                "Sell Items",
                transform,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(150f, 250f),
                new Vector2(250f, 70f),
                new Color(0.72f, 0.36f, 0.16f),
                defaultFont,
                28);
        }

        EnsureSellPanel(defaultFont);
        EnsureProstheticControls(defaultFont);
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
        panelBg.color = new Color(0.03f, 0.035f, 0.04f, 0.92f);

        GameObject cardObj = new GameObject("SellPanel_Card");
        cardObj.transform.SetParent(sellPanel.transform, false);
        RectTransform cardRect = cardObj.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(1160f, 780f);
        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.color = new Color(0.12f, 0.11f, 0.095f, 0.98f);

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
        scrollBg.color = new Color(0.055f, 0.055f, 0.055f, 0.92f);
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
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
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
                transform,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(150f, 170f),
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
        panelBg.color = new Color(0.025f, 0.035f, 0.05f, 0.94f);

        GameObject cardObj = new GameObject("ProstheticPanel_Card");
        cardObj.transform.SetParent(prostheticPanel.transform, false);
        RectTransform cardRect = cardObj.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(1160f, 780f);
        Image cardBg = cardObj.AddComponent<Image>();
        cardBg.color = new Color(0.07f, 0.095f, 0.12f, 0.98f);

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
        scrollBg.color = new Color(0.045f, 0.06f, 0.075f, 0.92f);
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
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
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
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = color;
        Button button = buttonObj.AddComponent<Button>();
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
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = color;
        Button button = buttonObj.AddComponent<Button>();
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
}
