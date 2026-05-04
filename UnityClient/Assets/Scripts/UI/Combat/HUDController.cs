using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour {
    public Text hpLabel;
    public Text sanLabel;
    public Text apLabel;
    public Text shieldLabel;
    public Text targetHintLabel;
    public Button endTurnBtn;
    public Transform enemyListParent;

    private Font _defaultFont;

    private void OnEnable() {
        _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureRuntimeWidgets();
        BindEndTurnButton();

        GameEventBus.OnHPChanged += HandleHPChanged;
        GameEventBus.OnSANChanged += HandleSANChanged;
        GameEventBus.OnAPChanged += HandleAPChanged;
        GameEventBus.OnShieldChanged += HandleShieldChanged;
        GameEventBus.OnTurnStarted += HandleTurnStarted;
        GameEventBus.OnTargetSelectionChanged += HandleTargetSelectionChanged;
        CombatEventBus.OnCombatPhase += HandleCombatPhase;

        RefreshCombatPresentation();
        Debug.Log("[HUDController] Combat HUD initialized.");
    }

    private void OnDisable() {
        GameEventBus.OnHPChanged -= HandleHPChanged;
        GameEventBus.OnSANChanged -= HandleSANChanged;
        GameEventBus.OnAPChanged -= HandleAPChanged;
        GameEventBus.OnShieldChanged -= HandleShieldChanged;
        GameEventBus.OnTurnStarted -= HandleTurnStarted;
        GameEventBus.OnTargetSelectionChanged -= HandleTargetSelectionChanged;
        CombatEventBus.OnCombatPhase -= HandleCombatPhase;
    }

    private void BindEndTurnButton() {
        if (endTurnBtn == null) {
            return;
        }

        endTurnBtn.onClick.RemoveAllListeners();
        endTurnBtn.onClick.AddListener(() => {
            if (GameRoot.Core?.Combat != null && GameRoot.Core.Combat.CurrentState == CombatState.PlayerTurn) {
                ItemUseService.ClearPendingTargetSelection();
                GameRoot.Core.Combat.EndPlayerTurn();
            } else {
                Debug.LogWarning("[HUD] 当前不在玩家回合，无法结束回合。");
            }
        });
    }

    private void EnsureRuntimeWidgets() {
        if (_defaultFont == null) {
            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        if (shieldLabel == null) {
            shieldLabel = CreateRuntimeLabel("Shield_Text", new Vector2(20f, -120f), new Vector2(300f, 50f), new Color(0.95f, 0.82f, 0.3f), 28);
        }

        if (targetHintLabel == null) {
            targetHintLabel = CreateRuntimeLabel("TargetHint_Text", new Vector2(0f, -36f), new Vector2(760f, 70f), Color.white, 26, new Vector2(0.5f, 1f), TextAnchor.MiddleCenter);
        }

        if (enemyListParent == null) {
            GameObject enemyList = new GameObject("EnemyList");
            enemyList.transform.SetParent(transform, false);
            RectTransform enemyRect = enemyList.AddComponent<RectTransform>();
            enemyRect.anchorMin = new Vector2(1f, 1f);
            enemyRect.anchorMax = new Vector2(1f, 1f);
            enemyRect.pivot = new Vector2(1f, 1f);
            enemyRect.anchoredPosition = new Vector2(-30f, -40f);
            enemyRect.sizeDelta = new Vector2(420f, 420f);

            VerticalLayoutGroup layout = enemyList.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperRight;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 12f;

            enemyListParent = enemyList.transform;
        }
    }

    private Text CreateRuntimeLabel(string name, Vector2 anchoredPosition, Vector2 size, Color color, int fontSize, Vector2? anchor = null, TextAnchor alignment = TextAnchor.MiddleLeft) {
        GameObject labelObj = new GameObject(name);
        labelObj.transform.SetParent(transform, false);

        RectTransform rect = labelObj.AddComponent<RectTransform>();
        Vector2 useAnchor = anchor ?? new Vector2(0f, 1f);
        rect.anchorMin = useAnchor;
        rect.anchorMax = useAnchor;
        rect.pivot = useAnchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text label = labelObj.AddComponent<Text>();
        label.font = _defaultFont;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }

    private void RefreshCombatPresentation() {
        RefreshStaticData();
        RefreshEnemyButtons();
        RefreshTargetHint();
    }

    private void RefreshStaticData() {
        var doll = GameRoot.Core?.CurrentPlayer?.ActiveDoll;
        if (doll == null) {
            return;
        }

        HandleHPChanged(doll.Name, doll.Status.HP_Current, doll.Status.HP_Max);
        HandleSANChanged(doll.Name, doll.Status.SAN_Current, doll.Status.SAN_Max);

        if (GameRoot.Core?.Combat?.PlayerFaction?.Fighters.Count > 0) {
            DollFighter fighter = GameRoot.Core.Combat.PlayerFaction.Fighters[0] as DollFighter;
            if (fighter != null) {
                HandleAPChanged(doll.Name, fighter.CurrentAP, fighter.MaxAP);
                HandleShieldChanged(doll.Name, fighter.RuntimeShield);
            }
        }
    }

    private void RefreshEnemyButtons() {
        if (enemyListParent == null) {
            return;
        }

        for (int i = enemyListParent.childCount - 1; i >= 0; i--) {
            Destroy(enemyListParent.GetChild(i).gameObject);
        }

        CombatFaction enemyFaction = GameRoot.Core?.Combat?.EnemyFaction;
        if (enemyFaction == null || enemyFaction.Fighters.Count == 0) {
            CreateEnemyStatusCard("NoEnemyCard", "当前没有敌方目标", false, null);
            return;
        }

        foreach (FighterEntity fighter in enemyFaction.Fighters) {
            if (fighter == null) {
                continue;
            }

            bool alive = fighter.RuntimeHP > 0;
            CreateEnemyStatusCard($"Enemy_{fighter.Name}", BuildEnemySummary(fighter), alive, fighter);
        }
    }

    private void CreateEnemyStatusCard(string objectName, string summary, bool isAlive, FighterEntity fighter) {
        GameObject buttonObj = new GameObject(objectName);
        buttonObj.transform.SetParent(enemyListParent, false);

        Image image = buttonObj.AddComponent<Image>();
        image.color = ResolveEnemyCardColor(isAlive);

        Button button = buttonObj.AddComponent<Button>();
        button.interactable = isAlive && ItemUseService.HasPendingEnemyTargetSelection;

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(360f, 84f);
        LayoutElement layoutElement = buttonObj.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 360f;
        layoutElement.preferredHeight = 84f;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        Text label = textObj.AddComponent<Text>();
        label.font = _defaultFont;
        label.fontSize = 24;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        label.text = summary;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        if (fighter != null) {
            button.onClick.AddListener(() => OnEnemyTargetClicked(fighter));
        }
    }

    private Color ResolveEnemyCardColor(bool isAlive) {
        if (!isAlive) {
            return new Color(0.2f, 0.2f, 0.2f, 0.92f);
        }

        return ItemUseService.HasPendingEnemyTargetSelection
            ? new Color(0.72f, 0.22f, 0.18f, 0.96f)
            : new Color(0.36f, 0.16f, 0.16f, 0.88f);
    }

    private string BuildEnemySummary(FighterEntity fighter) {
        string shieldText = fighter.RuntimeShield > 0 ? $" | Shield {fighter.RuntimeShield}" : string.Empty;
        string status = fighter.RuntimeHP > 0
            ? (ItemUseService.HasPendingEnemyTargetSelection ? "点击可选中" : "等待玩家选择武器")
            : "已击倒";
        return $"{fighter.Name}\nHP {Mathf.Max(0, fighter.RuntimeHP)}/{fighter.RuntimeMaxHP}{shieldText}\n{status}";
    }

    private void OnEnemyTargetClicked(FighterEntity fighter) {
        if (fighter == null) {
            return;
        }

        if (!ItemUseService.TryConfirmPendingTarget(fighter, out string failureReason)) {
            if (!string.IsNullOrEmpty(failureReason)) {
                Debug.LogWarning($"[HUD] {failureReason}");
            }
        }

        RefreshCombatPresentation();
    }

    private void HandleHPChanged(string id, int current, int max) {
        if (GameRoot.Core?.CurrentPlayer?.ActiveDoll != null && id == GameRoot.Core.CurrentPlayer.ActiveDoll.Name) {
            if (hpLabel != null) {
                hpLabel.text = $"HP: {current} / {max}";
            }
            return;
        }

        RefreshEnemyButtons();
    }

    private void HandleSANChanged(string id, int current, int max) {
        if (GameRoot.Core?.CurrentPlayer?.ActiveDoll != null && id == GameRoot.Core.CurrentPlayer.ActiveDoll.Name) {
            if (sanLabel != null) {
                sanLabel.text = $"SAN: {current} / {max}";
            }
        }
    }

    private void HandleAPChanged(string id, int current, int max) {
        if (GameRoot.Core?.CurrentPlayer?.ActiveDoll != null && id == GameRoot.Core.CurrentPlayer.ActiveDoll.Name) {
            if (apLabel != null) {
                apLabel.text = $"AP: {current} / {max}";
            }
        }
    }

    private void HandleShieldChanged(string id, int current) {
        if (GameRoot.Core?.CurrentPlayer?.ActiveDoll != null && id == GameRoot.Core.CurrentPlayer.ActiveDoll.Name) {
            if (shieldLabel != null) {
                shieldLabel.text = $"Shield: {current}";
            }
            return;
        }

        RefreshEnemyButtons();
    }

    private void HandleTurnStarted(FactionType type) {
        if (endTurnBtn != null) {
            endTurnBtn.interactable = type == FactionType.Player;
            Text btnText = endTurnBtn.GetComponentInChildren<Text>();
            if (btnText != null) {
                btnText.text = type == FactionType.Player ? "结束回合" : "敌方行动中...";
            }
        }

        RefreshEnemyButtons();
        RefreshTargetHint();
    }

    private void HandleTargetSelectionChanged(string message, bool isActive) {
        RefreshTargetHint(message, isActive);
        RefreshEnemyButtons();
    }

    private void HandleCombatPhase(CombatEventType phase, CombatFaction activeFaction) {
        if (phase == CombatEventType.OnTurnStart) {
            HandleTurnStarted(activeFaction.Type);
            RefreshStaticData();
        }
    }

    private void RefreshTargetHint() {
        string message = ItemUseService.HasPendingEnemyTargetSelection
            ? $"[{ItemUseService.PendingTargetItemName}] 已准备，点击右侧敌人完成攻击。"
            : "点击背包里的武器后，再点击右侧敌人进行攻击。";
        RefreshTargetHint(message, ItemUseService.HasPendingEnemyTargetSelection);
    }

    private void RefreshTargetHint(string message, bool isActive) {
        if (targetHintLabel == null) {
            return;
        }

        targetHintLabel.text = message;
        targetHintLabel.color = isActive ? new Color(1f, 0.86f, 0.36f) : Color.white;
    }
}
