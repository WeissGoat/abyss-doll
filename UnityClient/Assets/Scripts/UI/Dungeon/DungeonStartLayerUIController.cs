using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DungeonStartLayerUIController : MonoBehaviour {
    public Text titleText;
    public Text summaryText;
    public Transform listParent;
    public Button confirmBtn;
    public Button closeBtn;

    private readonly List<GameObject> _rows = new List<GameObject>();
    private int _selectedLayerID = 1;
    private Action _onClose;

    public void Present(Action onClose) {
        _onClose = onClose;

        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        if (player == null || GameRoot.Core.Dungeon == null) {
            return;
        }

        NormalizeSelection(player);
        RefreshTexts(player);
        RefreshLayerRows(player);
        BindButtons();
    }

    private void NormalizeSelection(PlayerProfile player) {
        int preferredLayer = player.LastSelectedDungeonStartLayer;
        if (!GameRoot.Core.Dungeon.CanStartAtLayer(preferredLayer)) {
            preferredLayer = FindDeepestStartableLayer(player);
        }

        _selectedLayerID = Mathf.Max(1, preferredLayer);
    }

    private int FindDeepestStartableLayer(PlayerProfile player) {
        int deepestLayer = 1;
        foreach (var kvp in ConfigManager.Dungeons) {
            int layerID = kvp.Key;
            if (layerID <= player.HighestUnlockedDungeonLayer && layerID > deepestLayer) {
                deepestLayer = layerID;
            }
        }

        return deepestLayer;
    }

    private void RefreshTexts(PlayerProfile player) {
        if (titleText != null) {
            titleText.text = "选择深渊入口";
        }

        if (summaryText != null) {
            summaryText.text =
                $"已解锁至第 {player.HighestUnlockedDungeonLayer} 层\n" +
                "从小镇出发会开启新一轮探索，并重置本轮战利品账本。";
        }
    }

    private void RefreshLayerRows(PlayerProfile player) {
        ClearRows();
        if (listParent == null) {
            return;
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        List<int> layerIDs = new List<int>(ConfigManager.Dungeons.Keys);
        layerIDs.Sort();

        foreach (int layerID in layerIDs) {
            if (!ConfigManager.Dungeons.TryGetValue(layerID, out DungeonConfig config)) {
                continue;
            }

            CreateLayerRow(layerID, config, player, defaultFont);
        }
    }

    private void CreateLayerRow(int layerID, DungeonConfig config, PlayerProfile player, Font font) {
        GameObject row = new GameObject($"DungeonStartLayer_{layerID}");
        row.transform.SetParent(listParent, false);
        _rows.Add(row);

        Image rowBg = row.AddComponent<Image>();
        bool canStart = GameRoot.Core.Dungeon.CanStartAtLayer(layerID);
        bool selected = layerID == _selectedLayerID;
        rowBg.color = selected
            ? new Color(0.92f, 0.64f, 0.22f, 0.95f)
            : canStart ? new Color(0.18f, 0.27f, 0.32f, 0.95f) : new Color(0.12f, 0.12f, 0.13f, 0.75f);

        Button rowButton = row.AddComponent<Button>();
        rowButton.interactable = canStart;
        rowButton.onClick.AddListener(() => {
            _selectedLayerID = layerID;
            RefreshLayerRows(player);
            RefreshConfirmButton();
        });

        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(760f, 72f);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(22, 22, 8, 8);
        layout.spacing = 18f;

        Text layerText = CreateText("Layer_Text", row.transform, font, 26, canStart ? Color.white : new Color(0.68f, 0.68f, 0.68f));
        layerText.text = $"第 {layerID} 层  {config.Name}";
        layerText.rectTransform.sizeDelta = new Vector2(360f, 52f);

        Text stateText = CreateText("State_Text", row.transform, font, 22, canStart ? new Color(0.78f, 1f, 0.82f) : new Color(1f, 0.68f, 0.58f));
        stateText.text = canStart ? (selected ? "已选择" : "可出发") : BuildLockedText(layerID);
        stateText.alignment = TextAnchor.MiddleRight;
        stateText.rectTransform.sizeDelta = new Vector2(310f, 52f);
    }

    private string BuildLockedText(int layerID) {
        int previousLayer = Mathf.Max(1, layerID - 1);
        return $"通过第 {previousLayer} 层后解锁";
    }

    private Text CreateText(string objectName, Transform parent, Font font, int fontSize, Color color) {
        GameObject obj = new GameObject(objectName);
        obj.transform.SetParent(parent, false);
        Text text = obj.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        return text;
    }

    private void BindButtons() {
        RefreshConfirmButton();

        if (confirmBtn != null) {
            confirmBtn.onClick.RemoveAllListeners();
            confirmBtn.onClick.AddListener(ConfirmStart);
        }

        if (closeBtn != null) {
            closeBtn.onClick.RemoveAllListeners();
            closeBtn.onClick.AddListener(() => _onClose?.Invoke());
        }
    }

    private void RefreshConfirmButton() {
        if (confirmBtn != null) {
            confirmBtn.interactable = GameRoot.Core?.Dungeon != null && GameRoot.Core.Dungeon.CanStartAtLayer(_selectedLayerID);
        }
    }

    private void ConfirmStart() {
        if (GameRoot.Core?.Dungeon == null) {
            return;
        }

        bool started = GameRoot.Core.Dungeon.StartRunAtLayer(_selectedLayerID);
        if (started) {
            gameObject.SetActive(false);
            return;
        }

        Present(_onClose);
    }

    private void ClearRows() {
        for (int i = _rows.Count - 1; i >= 0; i--) {
            DestroyRuntimeObject(_rows[i]);
        }

        _rows.Clear();
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
}
