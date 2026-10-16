using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 补给铺: a modal list of WorkshopShopService offers with a buy button each. Opened from the pre-dive loadout (and
// later the town screen); purchases go to the stash and the caller refreshes through onChanged. Built on UITheme.
public class WorkshopShopPanel : MonoBehaviour {
    public const string PanelName = "WorkshopShopPanel_Runtime";

    private Transform _listRoot;
    private Text _moneyText;
    private Text _messageText;
    private Action _onChanged;
    private readonly List<GameObject> _rows = new List<GameObject>();

    public static WorkshopShopPanel Create(Transform parent) {
        GameObject root = new GameObject(PanelName, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        WorkshopShopPanel panel = root.AddComponent<WorkshopShopPanel>();
        panel.Build();
        root.SetActive(false);
        return panel;
    }

    public void Show(Action onChanged) {
        _onChanged = onChanged;
        _messageText.text = "买下的东西放进仓库，整备时再带上。";
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Refresh();
    }

    public void Hide() {
        gameObject.SetActive(false);
    }

    private void Refresh() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        _moneyText.text = $"资金 {player?.Money ?? 0}G";

        foreach (GameObject row in _rows) {
            if (row != null) {
                row.SetActive(false);
                Destroy(row);
            }
        }
        _rows.Clear();

        List<WorkshopShopOffer> offers = WorkshopShopService.GetOffers(player);
        for (int i = 0; i < offers.Count; i++) {
            _rows.Add(CreateRow(offers[i], i));
        }
    }

    private GameObject CreateRow(WorkshopShopOffer offer, int index) {
        Image row = UITheme.CreatePanel(_listRoot, "Offer_" + offer.ConfigID, UITheme.PanelRaised);
        RectTransform rect = row.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -index * 84f);
        rect.sizeDelta = new Vector2(0f, 76f);

        Image icon = UITheme.CreatePanel(row.transform, "Icon_Image", Color.white);
        icon.raycastTarget = false;
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(14f, 0f);
        iconRect.sizeDelta = new Vector2(56f, 56f);
        if (ConfigManager.Items.TryGetValue(offer.ConfigID, out ItemEntity template)) {
            VisualUIHelper.ApplyContainSprite(icon, VisualAssetService.ResolveItemIconID(template), new Vector2(56f, 56f), Color.white, UITheme.Border);
            ItemTooltipTrigger.Attach(row.gameObject, template);
        }

        Text label = UITheme.CreateText(row.transform, "Name_Text", $"{offer.Name}\n{DescribeOffer(template)}", UITheme.CaptionSize + 1, UITheme.TextPrimary);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(84f, 6f);
        labelRect.offsetMax = new Vector2(-190f, -6f);

        Button buy = CreateButton(row.transform, "Buy_Button", $"购买 {offer.Price}G", offer.CanAfford ? UITheme.Accent : UITheme.Border, offer.CanAfford ? UITheme.OnAccent : UITheme.TextMuted);
        RectTransform buyRect = (RectTransform)buy.transform;
        buyRect.anchorMin = new Vector2(1f, 0.5f);
        buyRect.anchorMax = new Vector2(1f, 0.5f);
        buyRect.pivot = new Vector2(1f, 0.5f);
        buyRect.anchoredPosition = new Vector2(-14f, 0f);
        buyRect.sizeDelta = new Vector2(160f, 52f);
        buy.onClick.AddListener(() => HandleBuy(offer.ConfigID));
        return row.gameObject;
    }

    private void HandleBuy(string configID) {
        WorkshopShopPurchaseResult result = WorkshopShopService.TryBuy(GameRoot.Core?.CurrentPlayer, configID);
        _messageText.text = result.Reason;
        _messageText.color = result.Success ? UITheme.Positive : UITheme.Warning;
        Refresh();
        _onChanged?.Invoke();
    }

    private static string DescribeOffer(ItemEntity template) {
        if (template == null) {
            return string.Empty;
        }

        string size = BackpackGrid.TryGetRotatedBounds(template, 0, out int columns, out int rows) ? $"占 {columns}×{rows} 格" : string.Empty;
        string effect = ItemPresentationRules.BuildCompactSummary(template);
        return string.IsNullOrEmpty(effect) ? size : $"{size} · {effect}";
    }

    private void Build() {
        RectTransform rootRect = (RectTransform)transform;
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // Own sorting layer so the modal covers the backpack grid, which is a later sibling on the main canvas.
        Canvas overlay = gameObject.AddComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingOrder = 520;
        gameObject.AddComponent<GraphicRaycaster>();
        gameObject.AddComponent<Image>().color = new Color(0.1f, 0.06f, 0.03f, 0.62f);

        Image dialog = UITheme.CreatePanel(transform, "Dialog", UITheme.Panel);
        UITheme.ApplyBoardSkin(dialog, VisualAssetService.UIPanelMainID);
        RectTransform dialogRect = dialog.rectTransform;
        dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRect.pivot = new Vector2(0.5f, 0.5f);
        dialogRect.anchoredPosition = Vector2.zero;
        dialogRect.sizeDelta = new Vector2(760f, 720f);

        Text title = UITheme.CreateText(dialog.transform, "Title_Text", "补给铺", UITheme.HeadingSize, UITheme.Accent);
        PlaceTop(title.rectTransform, 24f, 28f, 400f, 44f);
        _moneyText = UITheme.CreateText(dialog.transform, "Money_Text", string.Empty, UITheme.BodySize, UITheme.TextPrimary, TextAnchor.MiddleRight);
        RectTransform moneyRect = _moneyText.rectTransform;
        moneyRect.anchorMin = new Vector2(1f, 1f);
        moneyRect.anchorMax = new Vector2(1f, 1f);
        moneyRect.pivot = new Vector2(1f, 1f);
        moneyRect.anchoredPosition = new Vector2(-28f, -28f);
        moneyRect.sizeDelta = new Vector2(300f, 44f);

        GameObject list = new GameObject("Offer_List", typeof(RectTransform));
        list.transform.SetParent(dialog.transform, false);
        RectTransform listRect = (RectTransform)list.transform;
        listRect.anchorMin = new Vector2(0f, 1f);
        listRect.anchorMax = new Vector2(1f, 1f);
        listRect.pivot = new Vector2(0.5f, 1f);
        listRect.offsetMin = new Vector2(24f, -520f);
        listRect.offsetMax = new Vector2(-24f, -92f);
        _listRoot = list.transform;

        _messageText = UITheme.CreateText(dialog.transform, "Message_Text", string.Empty, UITheme.CaptionSize + 1, UITheme.TextSecondary);
        PlaceTop(_messageText.rectTransform, 24f, 530f, 712f, 60f);

        Button close = CreateButton(dialog.transform, "Close_Button", "关闭", UITheme.PanelRaised, UITheme.TextPrimary);
        UITheme.AddBorder(close.targetGraphic, UITheme.Border, 1.5f);
        RectTransform closeRect = (RectTransform)close.transform;
        closeRect.anchorMin = new Vector2(0.5f, 0f);
        closeRect.anchorMax = new Vector2(0.5f, 0f);
        closeRect.pivot = new Vector2(0.5f, 0f);
        closeRect.anchoredPosition = new Vector2(0f, 28f);
        closeRect.sizeDelta = new Vector2(220f, 60f);
        close.onClick.AddListener(Hide);
    }

    private static Button CreateButton(Transform parent, string name, string label, Color background, Color textColor) {
        Image image = UITheme.CreatePanel(parent, name, background);
        UITheme.ApplyButtonSkin(image, background);
        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        Text text = UITheme.CreateText(image.transform, "Label_Text", label, UITheme.BodySize, textColor, TextAnchor.MiddleCenter);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        return button;
    }

    private static void PlaceTop(RectTransform rect, float x, float y, float width, float height) {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }
}
