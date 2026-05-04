using UnityEngine;
using UnityEngine.UI;

public class WorkshopUIController : MonoBehaviour {
    public Text moneyText;
    public Text chassisInfoText;
    public Button upgradeBtn;
    public Button departBtn;
    public Text stashHeaderText;
    public Transform stashListParent;
    public Button sellAllBtn;

    void Start() {
        EnsureSellControls();
        BindButtons();
        RefreshUI();
    }

    public void RefreshUI() {
        EnsureSellControls();

        var player = GameRoot.Core.CurrentPlayer;
        int stashCount = player.StashInventory.Count;
        int stashEstimatedValue = 0;
        foreach (var item in player.StashInventory) {
            if (item != null) {
                stashEstimatedValue += item.BaseValue;
            }
        }

        if (moneyText != null) {
            moneyText.text = $"金币 (Money): {player.Money}G\n仓库物资: {stashCount}件 / 估值 {stashEstimatedValue}G";
        }
        
        var chassis = player.ActiveDoll.Chassis;
        if (chassisInfoText != null) {
            chassisInfoText.text = $"当前底盘: {chassis.ChassisID}\n容量: {chassis.GridWidth}x{chassis.GridHeight}";
        }

        if (stashHeaderText != null) {
            stashHeaderText.text = stashCount > 0
                ? "仓库待售物资"
                : "仓库待售物资\n当前没有可卖出的回收品";
        }

        if (sellAllBtn != null) {
            sellAllBtn.interactable = stashCount > 0;
        }

        RefreshStashList();
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
                GameFlowController.Instance.DepartToDungeon();
            });
        }

        if (sellAllBtn != null) {
            sellAllBtn.onClick.RemoveAllListeners();
            sellAllBtn.onClick.AddListener(() => {
                GameRoot.Core.Workshop.SellAllStashItems(GameRoot.Core.CurrentPlayer);
                RefreshUI();
            });
        }
    }

    private void RefreshStashList() {
        if (stashListParent == null) {
            return;
        }

        for (int i = stashListParent.childCount - 1; i >= 0; i--) {
            Destroy(stashListParent.GetChild(i).gameObject);
        }

        var player = GameRoot.Core.CurrentPlayer;
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        foreach (var item in player.StashInventory) {
            if (item == null) {
                continue;
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

            GameObject labelObj = new GameObject("ItemLabel_Text");
            labelObj.transform.SetParent(row.transform, false);
            Text label = labelObj.AddComponent<Text>();
            label.font = defaultFont;
            label.fontSize = 24;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            label.raycastTarget = false;
            label.text = $"{item.Name}  [{item.BaseValue}G]";
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
            sellText.text = "卖出";
            RectTransform sellTextRect = sellTextObj.GetComponent<RectTransform>();
            sellTextRect.anchorMin = Vector2.zero;
            sellTextRect.anchorMax = Vector2.one;
            sellTextRect.sizeDelta = Vector2.zero;
        }
    }

    private void EnsureSellControls() {
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (stashHeaderText == null) {
            GameObject headerObj = new GameObject("StashHeader_Text");
            headerObj.transform.SetParent(transform, false);
            Text headerText = headerObj.AddComponent<Text>();
            headerText.font = defaultFont;
            headerText.fontSize = 28;
            headerText.color = new Color(1f, 0.92f, 0.5f);
            headerText.alignment = TextAnchor.UpperLeft;
            headerText.raycastTarget = false;
            RectTransform headerRect = headerObj.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(1f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(1f, 1f);
            headerRect.anchoredPosition = new Vector2(-40f, -40f);
            headerRect.sizeDelta = new Vector2(620f, 80f);
            stashHeaderText = headerText;
        }

        if (sellAllBtn == null) {
            sellAllBtn = CreateActionButton("SellAll_Button", "全部卖出", new Vector2(-40f, -120f), new Color(0.7f, 0.2f, 0.18f), defaultFont);
        }

        if (stashListParent == null) {
            GameObject listObj = new GameObject("StashList");
            listObj.transform.SetParent(transform, false);
            RectTransform listRect = listObj.AddComponent<RectTransform>();
            listRect.anchorMin = new Vector2(1f, 1f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.pivot = new Vector2(1f, 1f);
            listRect.anchoredPosition = new Vector2(-40f, -190f);
            listRect.sizeDelta = new Vector2(620f, 520f);

            VerticalLayoutGroup layout = listObj.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 10f;

            ContentSizeFitter fitter = listObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

            stashListParent = listObj.transform;
        }
    }

    private Button CreateActionButton(string objectName, string label, Vector2 anchoredPosition, Color color, Font font) {
        GameObject buttonObj = new GameObject(objectName);
        buttonObj.transform.SetParent(transform, false);
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = color;
        Button button = buttonObj.AddComponent<Button>();
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 1f);
        buttonRect.anchorMax = new Vector2(1f, 1f);
        buttonRect.pivot = new Vector2(1f, 1f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(180f, 56f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.font = font;
        buttonText.fontSize = 24;
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
