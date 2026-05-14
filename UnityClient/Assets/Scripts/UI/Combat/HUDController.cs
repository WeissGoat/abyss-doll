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
    public Image backgroundImage;

    private Font _defaultFont;
    private Image _turnBannerImage;
    private Image _playerStatusPanel;
    private Image _playerDollImage;
    private Image _hpBarTrack;
    private Image _shieldBarTrack;
    private Transform _apPipParent;

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

        EnsureCombatBackground();
        EnsureCombatMainSkin();

        if (shieldLabel == null) {
            shieldLabel = CreateRuntimeLabel("Shield_Text", new Vector2(20f, -120f), new Vector2(300f, 50f), new Color(0.95f, 0.82f, 0.3f), 28);
        }

        if (targetHintLabel == null) {
            targetHintLabel = CreateRuntimeLabel("TargetHint_Text", new Vector2(0f, -36f), new Vector2(760f, 70f), Color.white, 26, new Vector2(0.5f, 1f), TextAnchor.MiddleCenter);
        }

        if (enemyListParent == null) {
            GameObject enemyList = new GameObject("EnemyCardsRoot");
            enemyList.transform.SetParent(transform, false);
            RectTransform enemyRect = enemyList.AddComponent<RectTransform>();
            enemyRect.anchorMin = new Vector2(1f, 1f);
            enemyRect.anchorMax = new Vector2(1f, 1f);
            enemyRect.pivot = new Vector2(1f, 1f);
            enemyRect.anchoredPosition = new Vector2(-110f, -128f);
            enemyRect.sizeDelta = new Vector2(780f, 360f);

            HorizontalLayoutGroup layout = enemyList.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperRight;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 16f;

            enemyListParent = enemyList.transform;
        }

        PositionCombatTexts();
        ApplyCombatButtonSkin();
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

        DollFighter activeFighter = GetActivePlayerFighter();
        if (activeFighter != null) {
            HandleHPChanged(doll.Name, activeFighter.RuntimeHP, activeFighter.RuntimeMaxHP);
        } else {
            HandleHPChanged(doll.Name, doll.Status.HP_Current, doll.Status.HP_Max);
        }

        HandleSANChanged(doll.Name, doll.Status.SAN_Current, doll.Status.SAN_Max);

        if (activeFighter != null) {
            HandleAPChanged(doll.Name, activeFighter.CurrentAP, activeFighter.MaxAP);
            HandleShieldChanged(doll.Name, activeFighter.RuntimeShield);
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
        string cardVisualID = ItemUseService.HasPendingEnemyTargetSelection && isAlive
            ? VisualAssetService.UICombatEnemyCardSelectedID
            : VisualAssetService.UICombatEnemyCardID;
        VisualUIHelper.ApplySimpleSprite(image, cardVisualID, Color.white, ResolveEnemyCardColor(isAlive), true, false);

        Button button = buttonObj.AddComponent<Button>();
        button.interactable = isAlive && ItemUseService.HasPendingEnemyTargetSelection;

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(240f, 320f);
        LayoutElement layoutElement = buttonObj.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 240f;
        layoutElement.preferredHeight = 320f;

        CreateEnemyPortrait(buttonObj.transform, fighter, isAlive);
        CreateEnemyBars(buttonObj.transform, fighter);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        Text label = textObj.AddComponent<Text>();
        label.font = _defaultFont;
        label.fontSize = 18;
        label.color = Color.white;
        label.alignment = TextAnchor.UpperLeft;
        label.raycastTarget = false;
        label.text = summary;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 0f);
        textRect.pivot = new Vector2(0.5f, 0f);
        textRect.offsetMin = new Vector2(18f, 26f);
        textRect.offsetMax = new Vector2(-18f, 112f);

        if (fighter != null) {
            button.onClick.AddListener(() => OnEnemyTargetClicked(fighter));
        }
    }

    private void CreateEnemyPortrait(Transform parent, FighterEntity fighter, bool isAlive) {
        GameObject portraitObj = new GameObject("Portrait_Image");
        portraitObj.transform.SetParent(parent, false);
        Image portrait = portraitObj.AddComponent<Image>();

        string portraitID = string.Empty;
        if (fighter is MonsterFighter monsterFighter) {
            portraitID = VisualAssetService.ResolveMonsterPortraitID(monsterFighter.DataRef);
        }

        Color missingTint = isAlive
            ? new Color(0.75f, 0.42f, 0.36f, 1f)
            : new Color(0.34f, 0.34f, 0.34f, 1f);
        VisualUIHelper.ApplyContainSprite(portrait, portraitID, new Vector2(176f, 176f), Color.white, missingTint, false);

        RectTransform portraitRect = portraitObj.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0.5f, 1f);
        portraitRect.anchorMax = new Vector2(0.5f, 1f);
        portraitRect.pivot = new Vector2(0.5f, 1f);
        portraitRect.anchoredPosition = new Vector2(0f, -26f);
    }

    private void CreateEnemyBars(Transform parent, FighterEntity fighter) {
        CreateStatusTrack(parent, "HpBar", VisualAssetService.UICombatStatusBarHpID, new Vector2(0f, -214f), new Vector2(170f, 22f));
        if (fighter != null && fighter.RuntimeShield > 0) {
            CreateStatusTrack(parent, "ShieldBar", VisualAssetService.UICombatStatusBarShieldID, new Vector2(0f, -242f), new Vector2(170f, 18f));
        }
    }

    private Image CreateStatusTrack(Transform parent, string objectName, string visualID, Vector2 anchoredPosition, Vector2 size) {
        GameObject trackObj = new GameObject(objectName);
        trackObj.transform.SetParent(parent, false);
        Image image = trackObj.AddComponent<Image>();
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        VisualUIHelper.ApplySlicedSprite(image, visualID, Color.white, new Color(0.22f, 0.12f, 0.1f, 0.9f), false);
        return image;
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
            RefreshApPips(current, max);
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

    private void EnsureCombatBackground() {
        backgroundImage = VisualUIHelper.EnsurePanelBackground(transform, backgroundImage, "CombatBackground_Image");
        string visualID = VisualAssetService.ResolveCombatBackgroundID(GameRoot.Core?.Dungeon?.CurrentLayer);
        VisualUIHelper.ApplyCoverSprite(backgroundImage, visualID, Color.white, new Color(0.16f, 0.08f, 0.08f, 0.92f));
    }

    private void EnsureCombatMainSkin() {
        _turnBannerImage = EnsureSkinImage(
            _turnBannerImage,
            "TurnBanner",
            new Vector2(0.5f, 1f),
            new Vector2(0f, -36f),
            new Vector2(600f, 92f),
            VisualAssetService.UICombatTurnBannerID,
            false);

        _playerStatusPanel = EnsureSkinImage(
            _playerStatusPanel,
            "PlayerStatusPanel",
            new Vector2(0f, 0f),
            new Vector2(560f, 190f),
            new Vector2(520f, 170f),
            VisualAssetService.UIPanelInfoID,
            false);

        _hpBarTrack = EnsureChildSkinImage(
            _playerStatusPanel != null ? _playerStatusPanel.transform : transform,
            _hpBarTrack,
            "HpBar",
            new Vector2(0f, 1f),
            new Vector2(172f, -28f),
            new Vector2(250f, 26f),
            VisualAssetService.UICombatStatusBarHpID);

        _shieldBarTrack = EnsureChildSkinImage(
            _playerStatusPanel != null ? _playerStatusPanel.transform : transform,
            _shieldBarTrack,
            "ShieldBar",
            new Vector2(0f, 1f),
            new Vector2(172f, -70f),
            new Vector2(250f, 22f),
            VisualAssetService.UICombatStatusBarShieldID);

        EnsurePlayerDoll();
        EnsureApPipParent();
    }

    private Image EnsureSkinImage(Image current, string objectName, Vector2 anchor, Vector2 position, Vector2 size, string visualID, bool raycastTarget) {
        Image image = current;
        if (image == null) {
            Transform existing = transform.Find(objectName);
            image = existing != null ? existing.GetComponent<Image>() : null;
        }

        if (image == null) {
            GameObject obj = new GameObject(objectName);
            obj.transform.SetParent(transform, false);
            image = obj.AddComponent<Image>();
        }

        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        VisualUIHelper.ApplySlicedSprite(image, visualID, Color.white, new Color(0.08f, 0.07f, 0.065f, 0.9f), raycastTarget);
        image.transform.SetAsLastSibling();
        return image;
    }

    private Image EnsureChildSkinImage(Transform parent, Image current, string objectName, Vector2 anchor, Vector2 position, Vector2 size, string visualID) {
        if (parent == null) {
            return current;
        }

        Image image = current;
        if (image == null) {
            Transform existing = parent.Find(objectName);
            image = existing != null ? existing.GetComponent<Image>() : null;
        }

        if (image == null) {
            GameObject obj = new GameObject(objectName);
            obj.transform.SetParent(parent, false);
            image = obj.AddComponent<Image>();
        }

        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        VisualUIHelper.ApplySlicedSprite(image, visualID, Color.white, new Color(0.15f, 0.1f, 0.08f, 0.9f), false);
        return image;
    }

    private void EnsurePlayerDoll() {
        if (_playerDollImage == null) {
            Transform existing = transform.Find("PlayerDoll/DollImage");
            _playerDollImage = existing != null ? existing.GetComponent<Image>() : null;
        }

        if (_playerDollImage == null) {
            GameObject dollRoot = new GameObject("PlayerDoll");
            dollRoot.transform.SetParent(transform, false);
            RectTransform rootRect = dollRoot.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 0f);
            rootRect.anchorMax = new Vector2(0f, 0f);
            rootRect.pivot = new Vector2(0f, 0f);
            rootRect.anchoredPosition = new Vector2(120f, 110f);
            rootRect.sizeDelta = new Vector2(420f, 720f);

            GameObject imageObj = new GameObject("DollImage");
            imageObj.transform.SetParent(dollRoot.transform, false);
            _playerDollImage = imageObj.AddComponent<Image>();
        }

        RectTransform rect = _playerDollImage.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = Vector2.zero;
        VisualUIHelper.ApplyContainSprite(
            _playerDollImage,
            "doll_proto_0_stand",
            VisualDisplaySpecs.DollStand,
            Color.white,
            new Color(0.42f, 0.32f, 0.24f, 0.92f),
            false);
    }

    private void EnsureApPipParent() {
        if (_apPipParent != null) {
            return;
        }

        Transform parent = _playerStatusPanel != null ? _playerStatusPanel.transform : transform;
        Transform existing = parent.Find("ApPips");
        if (existing != null) {
            _apPipParent = existing;
            return;
        }

        GameObject pipRoot = new GameObject("ApPips");
        pipRoot.transform.SetParent(parent, false);
        RectTransform rect = pipRoot.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(172f, -110f);
        rect.sizeDelta = new Vector2(250f, 36f);
        HorizontalLayoutGroup layout = pipRoot.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 8f;
        _apPipParent = pipRoot.transform;
    }

    private void PositionCombatTexts() {
        MoveTextToPanel(hpLabel, new Vector2(24f, -22f), new Vector2(140f, 34f), 24, new Color(1f, 0.56f, 0.48f));
        MoveTextToPanel(shieldLabel, new Vector2(24f, -64f), new Vector2(140f, 32f), 22, new Color(0.95f, 0.82f, 0.3f));
        MoveTextToPanel(sanLabel, new Vector2(24f, -108f), new Vector2(140f, 34f), 22, new Color(0.74f, 0.56f, 1f));
        MoveTextToPanel(apLabel, new Vector2(24f, -132f), new Vector2(140f, 34f), 22, new Color(0.72f, 0.95f, 1f));

        if (targetHintLabel != null && _turnBannerImage != null) {
            targetHintLabel.transform.SetParent(_turnBannerImage.transform, false);
            targetHintLabel.fontSize = 24;
            targetHintLabel.alignment = TextAnchor.MiddleCenter;
            RectTransform rect = targetHintLabel.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(-48f, -18f);
        }

        if (endTurnBtn != null) {
            RectTransform rect = endTurnBtn.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(160f, 62f);
            rect.sizeDelta = new Vector2(220f, 64f);
        }
    }

    private void MoveTextToPanel(Text text, Vector2 position, Vector2 size, int fontSize, Color color) {
        if (text == null || _playerStatusPanel == null) {
            return;
        }

        text.transform.SetParent(_playerStatusPanel.transform, false);
        text.fontSize = fontSize;
        text.color = color;
        text.raycastTarget = false;
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private void ApplyCombatButtonSkin() {
        VisualUIHelper.ApplyButtonSkin(endTurnBtn, VisualAssetService.UIButtonPrimaryID, new Color(0.8f, 0.2f, 0.2f));
    }

    private void RefreshApPips(int current, int max) {
        if (_apPipParent == null) {
            return;
        }

        for (int i = _apPipParent.childCount - 1; i >= 0; i--) {
            Destroy(_apPipParent.GetChild(i).gameObject);
        }

        for (int i = 0; i < Mathf.Max(0, max); i++) {
            GameObject pipObj = new GameObject($"AP_{i + 1}");
            pipObj.transform.SetParent(_apPipParent, false);
            Image pip = pipObj.AddComponent<Image>();
            RectTransform rect = pip.rectTransform;
            rect.sizeDelta = new Vector2(28f, 28f);
            VisualUIHelper.ApplySimpleSprite(
                pip,
                VisualAssetService.UICombatApPipID,
                i < current ? Color.white : new Color(0.38f, 0.38f, 0.38f, 0.82f),
                i < current ? new Color(0.7f, 0.95f, 1f, 1f) : new Color(0.25f, 0.25f, 0.25f, 0.8f),
                false,
                true);
        }
    }

    private DollFighter GetActivePlayerFighter() {
        CombatSystem combat = GameRoot.Core?.Combat;
        if (combat == null || combat.CurrentState == CombatState.End || combat.PlayerFaction == null || combat.PlayerFaction.Fighters.Count == 0) {
            return null;
        }

        return combat.PlayerFaction.Fighters[0] as DollFighter;
    }
}
