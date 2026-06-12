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
    private Transform _stageRoot;
    private Transform _playerStageRoot;
    private Transform _enemyStageRoot;
    private Transform _vfxLayer;
    private Image _turnBannerImage;
    private Text _turnBannerLabel;
    private Image _targetHintPanel;
    private Image _actionStrip;
    private Button _cancelSelectionBtn;
    private Image _playerStatusPanel;
    private Image _playerShadowImage;
    private Image _playerDollImage;
    private Image _hpBarTrack;
    private Image _hpBarFill;
    private Image _shieldBarTrack;
    private Image _shieldBarFill;
    private Transform _apPipParent;
    private CombatIntentReadabilitySnapshot _intentSnapshot;

    public void SetLootOverlayMode(bool active) {
        SetActiveIfPresent(_turnBannerImage, !active);
        SetActiveIfPresent(_targetHintPanel, !active);
        SetActiveIfPresent(_actionStrip, !active);
        SetActiveIfPresent(_playerStatusPanel, !active);
    }

    private void OnEnable() {
        _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureRuntimeWidgets();
        BindEndTurnButton();
        BindCancelSelectionButton();

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

    private void BindCancelSelectionButton() {
        if (_cancelSelectionBtn == null) {
            return;
        }

        _cancelSelectionBtn.onClick.RemoveAllListeners();
        _cancelSelectionBtn.onClick.AddListener(() => {
            ItemUseService.ClearPendingTargetSelection();
            RefreshCombatPresentation();
        });
    }

    private void EnsureRuntimeWidgets() {
        if (_defaultFont == null) {
            _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        EnsureCombatBackground();
        EnsureStageRoots();
        EnsureCombatMainSkin();
        EnsurePlayerStage();
        EnsureActionButtons();
        EnsureStatusTextLabels();
        EnsureTargetHintLabel();
        PositionCombatTexts();
        ApplyCombatButtonSkins();
        RefreshActionButtons();
    }

    private void EnsureStageRoots() {
        _stageRoot = EnsureTransform(transform, "StageRoot");
        if (_stageRoot == null) {
            Debug.LogWarning("[HUDController] Failed to create StageRoot. Combat stage widgets will be skipped.");
            return;
        }

        RectTransform stageRect = EnsureRect(_stageRoot);
        stageRect.anchorMin = Vector2.zero;
        stageRect.anchorMax = Vector2.one;
        stageRect.pivot = new Vector2(0.5f, 0.5f);
        stageRect.anchoredPosition = Vector2.zero;
        stageRect.sizeDelta = Vector2.zero;

        _playerStageRoot = EnsureTransform(_stageRoot, "PlayerStageRoot");
        if (_playerStageRoot == null) {
            Debug.LogWarning("[HUDController] Failed to create PlayerStageRoot. Combat stage widgets will be skipped.");
            return;
        }
        ConfigureTopLeftRect(EnsureRect(_playerStageRoot), 100f, 190f, 440f, 620f);

        _enemyStageRoot = ResolveEnemyStageRoot();
        if (_enemyStageRoot == null) {
            _enemyStageRoot = CreateTransform(_stageRoot, "EnemyStageRoot");
        }
        if (_enemyStageRoot == null) {
            Debug.LogWarning("[HUDController] Failed to create EnemyStageRoot. Combat enemy widgets will be skipped.");
            return;
        }
        ConfigureTopLeftRect(EnsureRect(_enemyStageRoot), 1260f, 150f, 560f, 500f);
        enemyListParent = _enemyStageRoot;
        RemoveLayoutComponents(_enemyStageRoot.gameObject);
        StripEnemyStageBacking(_enemyStageRoot);

        _vfxLayer = EnsureTransform(_stageRoot, "VfxLayer");
        if (_vfxLayer == null) {
            _vfxLayer = CreateTransform(_stageRoot, "VfxLayer");
        }
        if (_vfxLayer == null) {
            Debug.LogWarning("[HUDController] Failed to create VfxLayer. Combat vfx widgets will be skipped.");
            return;
        }
        ConfigureTopLeftRect(EnsureRect(_vfxLayer), 560f, 180f, 620f, 240f);
        CanvasGroup vfxGroup = _vfxLayer.GetComponent<CanvasGroup>();
        if (vfxGroup == null) {
            vfxGroup = _vfxLayer.gameObject.AddComponent<CanvasGroup>();
        }
        vfxGroup.blocksRaycasts = false;
        vfxGroup.interactable = false;

        _stageRoot.SetAsLastSibling();
    }

    private Transform ResolveEnemyStageRoot() {
        if (IsValidTransform(enemyListParent) && enemyListParent.IsChildOf(transform)) {
            enemyListParent.name = "EnemyStageRoot";
            enemyListParent.SetParent(_stageRoot, false);
            return enemyListParent;
        }

        Transform existing = FindChildRecursive(transform, "EnemyStageRoot");
        if (existing == null) {
            existing = FindChildRecursive(transform, "EnemyCardsRoot");
        }

        if (existing != null) {
            existing.name = "EnemyStageRoot";
            existing.SetParent(_stageRoot, false);
            return existing;
        }

        return EnsureTransform(_stageRoot, "EnemyStageRoot");
    }

    private void StripEnemyStageBacking(Transform root) {
        if (!IsValidTransform(root)) {
            return;
        }

        Image image = root.GetComponent<Image>();
        if (image != null) {
            image.sprite = null;
            image.color = Color.clear;
            image.raycastTarget = false;
        }
    }

    private void EnsureCombatMainSkin() {
        _turnBannerImage = EnsureSkinImage(
            transform,
            _turnBannerImage,
            "TurnBanner",
            760f,
            28f,
            400f,
            58f,
            VisualAssetService.UICombatTurnBannerID,
            new Color(0.08f, 0.07f, 0.065f, 0.68f),
            false);

        _turnBannerLabel = EnsureChildLabel(
            _turnBannerImage.transform,
            _turnBannerLabel,
            "TurnBanner_Text",
            "玩家回合",
            24,
            Color.white,
            TextAnchor.MiddleCenter);
        StretchToParent(_turnBannerLabel.rectTransform, 28f, 8f);

        _targetHintPanel = EnsureSkinImage(
            transform,
            _targetHintPanel,
            "TargetHintPanel",
            720f,
            424f,
            480f,
            54f,
            VisualAssetService.UIPanelInfoID,
            new Color(0.08f, 0.075f, 0.065f, 0.56f),
            false);

        _actionStrip = EnsureSkinImage(
            transform,
            _actionStrip,
            "ActionStrip",
            720f,
            498f,
            480f,
            68f,
            VisualAssetService.UIPanelInfoID,
            new Color(0.06f, 0.055f, 0.05f, 0.48f),
            false);

        _playerStatusPanel = EnsureSkinImage(
            transform,
            _playerStatusPanel,
            "PlayerStatusCluster",
            120f,
            774f,
            560f,
            214f,
            VisualAssetService.UIPanelInfoID,
            new Color(0.08f, 0.075f, 0.065f, 0.88f),
            false);

        _hpBarTrack = EnsureStatusTrack(
            _playerStatusPanel.transform,
            _hpBarTrack,
            ref _hpBarFill,
            "HpBar",
            new Vector2(250f, -28f),
            new Vector2(270f, 22f),
            VisualAssetService.UICombatStatusBarHpID,
            new Color(0.92f, 0.18f, 0.14f, 0.92f));

        _shieldBarTrack = EnsureStatusTrack(
            _playerStatusPanel.transform,
            _shieldBarTrack,
            ref _shieldBarFill,
            "ShieldBar",
            new Vector2(250f, -76f),
            new Vector2(270f, 20f),
            VisualAssetService.UICombatStatusBarShieldID,
            new Color(0.52f, 0.68f, 0.95f, 0.92f));

        EnsureApPipParent();
    }

    private void EnsurePlayerStage() {
        if (!IsValidTransform(_playerStageRoot)) {
            return;
        }

        _playerShadowImage = EnsureSimpleImage(_playerStageRoot, _playerShadowImage, "PlayerShadow_Image");
        if (_playerShadowImage == null) {
            return;
        }
        RectTransform shadowRect = _playerShadowImage.rectTransform;
        shadowRect.anchorMin = new Vector2(0.5f, 0f);
        shadowRect.anchorMax = new Vector2(0.5f, 0f);
        shadowRect.pivot = new Vector2(0.5f, 0.5f);
        shadowRect.anchoredPosition = new Vector2(0f, 48f);
        shadowRect.sizeDelta = VisualDisplaySpecs.CombatEntityShadow;
        VisualUIHelper.ApplySimpleSprite(
            _playerShadowImage,
            VisualAssetService.UICombatEntityShadowID,
            Color.white,
            new Color(0f, 0f, 0f, 0.24f),
            false,
            false);

        _playerDollImage = EnsureSimpleImage(_playerStageRoot, _playerDollImage, "PlayerDoll_Image");
        if (_playerDollImage == null) {
            return;
        }
        RectTransform dollRect = _playerDollImage.rectTransform;
        dollRect.anchorMin = new Vector2(0.5f, 0f);
        dollRect.anchorMax = new Vector2(0.5f, 0f);
        dollRect.pivot = new Vector2(0.5f, 0f);
        dollRect.anchoredPosition = new Vector2(0f, 54f);
        VisualUIHelper.ApplyContainSprite(
            _playerDollImage,
            "doll_proto_0_stand",
            VisualDisplaySpecs.CombatDoll,
            Color.white,
            new Color(0.42f, 0.32f, 0.24f, 0.92f),
            false);

        Transform vfxAnchor = EnsureTransform(_playerStageRoot, "PlayerVfxAnchor");
        RectTransform vfxRect = EnsureRect(vfxAnchor);
        if (vfxRect == null) {
            return;
        }
        vfxRect.anchorMin = new Vector2(0.5f, 0.55f);
        vfxRect.anchorMax = new Vector2(0.5f, 0.55f);
        vfxRect.pivot = new Vector2(0.5f, 0.5f);
        vfxRect.anchoredPosition = Vector2.zero;
        vfxRect.sizeDelta = new Vector2(80f, 80f);
    }

    private void EnsureActionButtons() {
        if (endTurnBtn == null) {
            endTurnBtn = CreateButton(_actionStrip.transform, "EndTurn_Button", "结束回合");
        } else {
            endTurnBtn.transform.SetParent(_actionStrip.transform, false);
        }

        RectTransform endRect = endTurnBtn.GetComponent<RectTransform>();
        endRect.anchorMin = new Vector2(0.5f, 0.5f);
        endRect.anchorMax = new Vector2(0.5f, 0.5f);
        endRect.pivot = new Vector2(0.5f, 0.5f);
        endRect.anchoredPosition = new Vector2(100f, 0f);
        endRect.sizeDelta = new Vector2(180f, 48f);
        ConfigureButtonText(endTurnBtn, "结束回合", 21, Color.white);

        if (_cancelSelectionBtn == null) {
            Transform existing = _actionStrip.transform.Find("CancelSelection_Button");
            _cancelSelectionBtn = existing != null ? existing.GetComponent<Button>() : null;
        }

        if (_cancelSelectionBtn == null) {
            _cancelSelectionBtn = CreateButton(_actionStrip.transform, "CancelSelection_Button", "取消选择");
        } else {
            _cancelSelectionBtn.transform.SetParent(_actionStrip.transform, false);
        }

        RectTransform cancelRect = _cancelSelectionBtn.GetComponent<RectTransform>();
        cancelRect.anchorMin = new Vector2(0.5f, 0.5f);
        cancelRect.anchorMax = new Vector2(0.5f, 0.5f);
        cancelRect.pivot = new Vector2(0.5f, 0.5f);
        cancelRect.anchoredPosition = new Vector2(-100f, 0f);
        cancelRect.sizeDelta = new Vector2(180f, 48f);
        ConfigureButtonText(_cancelSelectionBtn, "取消选择", 21, Color.white);
    }

    private void EnsureTargetHintLabel() {
        if (targetHintLabel == null) {
            targetHintLabel = EnsureChildLabel(
                _targetHintPanel.transform,
                targetHintLabel,
                "TargetHint_Text",
                "点击背包里的武器后，再点击右侧敌人进行攻击。",
                22,
                Color.white,
                TextAnchor.MiddleCenter);
        } else {
            targetHintLabel.transform.SetParent(_targetHintPanel.transform, false);
        }
    }

    private void EnsureStatusTextLabels() {
        if (_playerStatusPanel == null) {
            return;
        }

        if (hpLabel == null) {
            hpLabel = EnsureChildLabel(_playerStatusPanel.transform, hpLabel, "HP_Text", "HP: -- / --", 22, new Color(1f, 0.56f, 0.48f), TextAnchor.MiddleLeft);
        }

        if (shieldLabel == null) {
            shieldLabel = EnsureChildLabel(_playerStatusPanel.transform, shieldLabel, "Shield_Text", "Shield: 0", 20, new Color(0.72f, 0.86f, 1f), TextAnchor.MiddleLeft);
        }

        if (sanLabel == null) {
            sanLabel = EnsureChildLabel(_playerStatusPanel.transform, sanLabel, "SAN_Text", "SAN: -- / --", 20, new Color(0.74f, 0.56f, 1f), TextAnchor.MiddleLeft);
        }

        if (apLabel == null) {
            apLabel = EnsureChildLabel(_playerStatusPanel.transform, apLabel, "AP_Text", "AP: -- / --", 20, new Color(0.72f, 0.95f, 1f), TextAnchor.MiddleLeft);
        }
    }

    private void RefreshCombatPresentation() {
        RefreshStaticData();
        RefreshIntentSnapshot();
        RefreshEnemyButtons();
        RefreshTargetHint();
        RefreshActionButtons();
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
            DestroyRuntimeObject(enemyListParent.GetChild(i).gameObject);
        }

        CombatFaction enemyFaction = GameRoot.Core?.Combat?.EnemyFaction;
        if (enemyFaction == null || enemyFaction.Fighters.Count == 0) {
            CreateNoEnemyNotice();
            return;
        }

        int aliveOrVisibleCount = 0;
        foreach (FighterEntity fighter in enemyFaction.Fighters) {
            if (fighter != null) {
                aliveOrVisibleCount++;
            }
        }

        int index = 0;
        foreach (FighterEntity fighter in enemyFaction.Fighters) {
            if (fighter == null) {
                continue;
            }

            bool alive = fighter.RuntimeHP > 0;
            CreateEnemyEntitySlot(index, aliveOrVisibleCount, fighter, alive);
            index++;
        }
    }

    private void RefreshIntentSnapshot() {
        _intentSnapshot = CombatReadabilityTextService.BuildIntentSnapshot(GameRoot.Core?.Combat);
    }

    private void CreateNoEnemyNotice() {
        GameObject noticeObj = CreateRectGameObject("NoEnemy_Text");
        noticeObj.transform.SetParent(enemyListParent, false);
        Text label = noticeObj.AddComponent<Text>();
        label.font = _defaultFont;
        label.fontSize = 24;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        label.text = "当前没有敌方目标";

        RectTransform rect = label.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private void CreateEnemyEntitySlot(int index, int count, FighterEntity fighter, bool isAlive) {
        GameObject slotObj = CreateRectGameObject($"EnemySlot_{index}");
        slotObj.transform.SetParent(enemyListParent, false);
        RectTransform slotRect = slotObj.GetComponent<RectTransform>();
        Vector2 slotSize = ResolveEnemySlotSize(count);
        Vector2 slotPosition = ResolveEnemySlotPosition(index, count);
        slotRect.anchorMin = new Vector2(0f, 1f);
        slotRect.anchorMax = new Vector2(0f, 1f);
        slotRect.pivot = new Vector2(0.5f, 1f);
        slotRect.anchoredPosition = new Vector2(slotPosition.x, -slotPosition.y);
        slotRect.sizeDelta = slotSize;

        Button hotspot = CreateEnemyHotspot(slotObj.transform, fighter, isAlive);
        Image shadow = CreateEnemyImage(slotObj.transform, "EnemyShadow_Image", new Vector2(0f, 54f), VisualDisplaySpecs.CombatEntityShadow);
        VisualUIHelper.ApplySimpleSprite(
            shadow,
            VisualAssetService.UICombatEntityShadowID,
            Color.white,
            new Color(0f, 0f, 0f, 0.18f),
            false,
            false);

        Image targetRing = CreateEnemyImage(slotObj.transform, "EnemyTargetRing_Image", new Vector2(0f, 58f), VisualDisplaySpecs.CombatTargetRing);
        bool targetable = isAlive && ItemUseService.HasPendingEnemyTargetSelection;
        VisualUIHelper.ApplySimpleSprite(
            targetRing,
            VisualAssetService.UICombatTargetRingID,
            targetable ? Color.white : new Color(1f, 1f, 1f, 0.16f),
            targetable ? new Color(1f, 0.86f, 0.26f, 0.66f) : new Color(0.86f, 0.62f, 0.2f, 0.16f),
            false,
            false);
        targetRing.gameObject.SetActive(isAlive);

        Image sprite = CreateEnemySprite(slotObj.transform, fighter, isAlive, count);
        sprite.transform.SetAsLastSibling();

        float hpRatio = fighter != null && fighter.RuntimeMaxHP > 0
            ? Mathf.Clamp01((float)Mathf.Max(0, fighter.RuntimeHP) / fighter.RuntimeMaxHP)
            : 0f;
        CreateEnemyFootBar(
            slotObj.transform,
            "EnemyFootHpBar",
            VisualAssetService.UICombatStatusBarHpID,
            new Vector2(0f, 36f),
            new Vector2(200f, 16f),
            hpRatio,
            new Color(0.92f, 0.16f, 0.12f, 0.95f));

        float shieldRatio = fighter != null && fighter.RuntimeShield > 0 ? 1f : 0f;
        CreateEnemyFootBar(
            slotObj.transform,
            "EnemyFootShieldBar",
            VisualAssetService.UICombatStatusBarShieldID,
            new Vector2(0f, 14f),
            new Vector2(200f, 12f),
            shieldRatio,
            new Color(0.52f, 0.68f, 0.95f, 0.92f));

        CreateEnemyNameAndIntent(slotObj.transform, fighter, isAlive, targetable);
        hotspot.transform.SetAsFirstSibling();
    }

    private Button CreateEnemyHotspot(Transform parent, FighterEntity fighter, bool isAlive) {
        GameObject buttonObj = CreateRectGameObject("EnemyClickHotspot_Button");
        buttonObj.transform.SetParent(parent, false);
        Image image = buttonObj.AddComponent<Image>();
        VisualUIHelper.ApplySolidColor(image, Color.clear, true);

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.interactable = isAlive && ItemUseService.HasPendingEnemyTargetSelection;

        RectTransform rect = buttonObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        if (fighter != null) {
            button.onClick.AddListener(() => OnEnemyTargetClicked(fighter));
        }

        return button;
    }

    private Image CreateEnemyImage(Transform parent, string objectName, Vector2 bottomPosition, Vector2 size) {
        GameObject imageObj = CreateRectGameObject(objectName);
        imageObj.transform.SetParent(parent, false);
        Image image = imageObj.AddComponent<Image>();
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = bottomPosition;
        rect.sizeDelta = size;
        image.raycastTarget = false;
        return image;
    }

    private Image CreateEnemySprite(Transform parent, FighterEntity fighter, bool isAlive, int count) {
        GameObject spriteObj = CreateRectGameObject("EnemySprite_Image");
        spriteObj.transform.SetParent(parent, false);
        Image sprite = spriteObj.AddComponent<Image>();
        RectTransform rect = sprite.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 72f);

        string visualID = ResolveEnemyCombatVisualID(fighter);
        Vector2 size = count <= 1 ? VisualDisplaySpecs.MonsterCombatEntityLarge : VisualDisplaySpecs.MonsterCombatEntity;
        Color registeredColor = isAlive ? Color.white : new Color(0.45f, 0.45f, 0.45f, 0.9f);
        VisualUIHelper.ApplyContainSprite(
            sprite,
            visualID,
            size,
            registeredColor,
            isAlive ? new Color(0.75f, 0.42f, 0.36f, 1f) : new Color(0.34f, 0.34f, 0.34f, 1f),
            false);
        return sprite;
    }

    private void CreateEnemyFootBar(Transform parent, string objectName, string visualID, Vector2 bottomPosition, Vector2 size, float ratio, Color fillColor) {
        GameObject barObj = CreateRectGameObject(objectName);
        barObj.transform.SetParent(parent, false);
        Image track = barObj.AddComponent<Image>();
        RectTransform rect = track.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = bottomPosition;
        rect.sizeDelta = size;
        VisualUIHelper.ApplySlicedSprite(track, visualID, new Color(1f, 1f, 1f, 0.68f), new Color(0.16f, 0.1f, 0.09f, 0.36f), false);
        track.color = ratio > 0f ? track.color : new Color(track.color.r, track.color.g, track.color.b, 0.24f);

        Image fill = EnsureBarFill(track.transform, null, "Fill", fillColor);
        SetBarFill(fill, ratio);
    }

    private void CreateEnemyNameAndIntent(Transform parent, FighterEntity fighter, bool isAlive, bool targetable) {
        GameObject nameObj = CreateRectGameObject("EnemyName_Text");
        nameObj.transform.SetParent(parent, false);
        Text nameLabel = nameObj.AddComponent<Text>();
        nameLabel.font = _defaultFont;
        nameLabel.fontSize = 18;
        nameLabel.alignment = TextAnchor.MiddleCenter;
        nameLabel.color = isAlive ? Color.white : new Color(0.65f, 0.65f, 0.65f, 0.92f);
        nameLabel.raycastTarget = false;
        nameLabel.text = fighter != null ? fighter.Name : "未知敌人";
        RectTransform nameRect = nameLabel.rectTransform;
        nameRect.anchorMin = new Vector2(0.5f, 0f);
        nameRect.anchorMax = new Vector2(0.5f, 0f);
        nameRect.pivot = new Vector2(0.5f, 0.5f);
        nameRect.anchoredPosition = new Vector2(0f, 8f);
        nameRect.sizeDelta = new Vector2(260f, 24f);

        GameObject intentObj = CreateRectGameObject("EnemyIntent_Text");
        intentObj.transform.SetParent(parent, false);
        Text intentLabel = intentObj.AddComponent<Text>();
        intentLabel.font = _defaultFont;
        intentLabel.fontSize = 15;
        intentLabel.alignment = TextAnchor.MiddleCenter;
        intentLabel.color = targetable ? new Color(1f, 0.86f, 0.36f) : new Color(0.84f, 0.84f, 0.84f, 0.86f);
        intentLabel.raycastTarget = false;
        intentLabel.text = isAlive
            ? BuildEnemyIntentText(fighter, targetable)
            : "已击倒";
        RectTransform intentRect = intentLabel.rectTransform;
        intentRect.anchorMin = new Vector2(0.5f, 0f);
        intentRect.anchorMax = new Vector2(0.5f, 0f);
        intentRect.pivot = new Vector2(0.5f, 0.5f);
        intentRect.anchoredPosition = new Vector2(0f, -30f);
        intentRect.sizeDelta = new Vector2(300f, 50f);
    }

    private string ResolveEnemyCombatVisualID(FighterEntity fighter) {
        if (fighter is MonsterFighter monsterFighter) {
            return VisualAssetService.ResolveMonsterCombatVisualID(monsterFighter.DataRef);
        }

        return VisualAssetService.MissingSpriteVisualID;
    }

    private string BuildEnemyHpText(FighterEntity fighter) {
        if (fighter == null) {
            return "HP -";
        }

        string shieldText = fighter.RuntimeShield > 0 ? $" / Shield {fighter.RuntimeShield}" : string.Empty;
        return $"HP {Mathf.Max(0, fighter.RuntimeHP)}/{fighter.RuntimeMaxHP}{shieldText}";
    }

    private string BuildEnemyIntentText(FighterEntity fighter, bool targetable) {
        if (targetable) {
            return "可选中";
        }

        CombatIntentReadabilityLine line = FindIntentLineForFighter(fighter);
        if (line == null || !line.IsAlive || !line.HasSelectedIntent) {
            return BuildEnemyHpText(fighter);
        }

        string detail = string.IsNullOrEmpty(line.DetailText) ? BuildEnemyHpText(fighter) : line.DetailText;
        return $"敌方意图: {line.IntentTitle}\n{detail}";
    }

    private CombatIntentReadabilityLine FindIntentLineForFighter(FighterEntity fighter) {
        if (_intentSnapshot == null || !_intentSnapshot.Success || _intentSnapshot.EnemyLines == null || fighter == null) {
            return null;
        }

        MonsterFighter monster = fighter as MonsterFighter;
        foreach (CombatIntentReadabilityLine line in _intentSnapshot.EnemyLines) {
            if (line == null) {
                continue;
            }

            if (monster != null && !string.IsNullOrEmpty(monster.RuntimeID) && line.RuntimeID == monster.RuntimeID) {
                return line;
            }

            if (monster?.DataRef != null && !string.IsNullOrEmpty(monster.DataRef.MonsterID) && line.MonsterID == monster.DataRef.MonsterID) {
                return line;
            }
        }

        return null;
    }

    private Vector2 ResolveEnemySlotSize(int count) {
        return count <= 1
            ? new Vector2(380f, 470f)
            : new Vector2(300f, 420f);
    }

    private Vector2 ResolveEnemySlotPosition(int index, int count) {
        if (count <= 1) {
            return new Vector2(300f, 24f);
        }

        if (count == 2) {
            return index == 0
                ? new Vector2(220f, 84f)
                : new Vector2(410f, 24f);
        }

        switch (index) {
            case 0:
                return new Vector2(300f, 112f);
            case 1:
                return new Vector2(188f, 24f);
            default:
                return new Vector2(432f, 34f);
        }
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
            SetBarFill(_hpBarFill, max > 0 ? (float)current / max : 0f);
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
            SetBarFill(_shieldBarFill, current > 0 ? 1f : 0f);
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

        if (_turnBannerLabel != null) {
            _turnBannerLabel.text = type == FactionType.Player ? "玩家回合" : "敌方行动中";
        }

        RefreshEnemyButtons();
        RefreshTargetHint();
        RefreshActionButtons();
    }

    private void HandleTargetSelectionChanged(string message, bool isActive) {
        RefreshTargetHint(message, isActive);
        RefreshEnemyButtons();
        RefreshActionButtons();
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

        if (_turnBannerLabel != null && isActive) {
            _turnBannerLabel.text = "选择攻击目标";
        }
    }

    private void RefreshActionButtons() {
        if (_cancelSelectionBtn != null) {
            _cancelSelectionBtn.interactable = ItemUseService.HasPendingEnemyTargetSelection;
            CanvasGroup group = _cancelSelectionBtn.GetComponent<CanvasGroup>();
            if (group == null) {
                group = _cancelSelectionBtn.gameObject.AddComponent<CanvasGroup>();
            }
            group.alpha = ItemUseService.HasPendingEnemyTargetSelection ? 1f : 0.58f;
            group.blocksRaycasts = true;
        }
    }

    private void EnsureCombatBackground() {
        backgroundImage = VisualUIHelper.EnsurePanelBackground(transform, backgroundImage, "CombatBackground_Image");
        string visualID = VisualAssetService.ResolveCombatBackgroundID(GameRoot.Core?.Dungeon?.CurrentLayer);
        VisualUIHelper.ApplyCoverSprite(backgroundImage, visualID, Color.white, new Color(0.16f, 0.08f, 0.08f, 0.92f));
    }

    private Image EnsureSkinImage(Transform parent, Image current, string objectName, float x, float y, float width, float height, string visualID, Color missingColor, bool raycastTarget) {
        Image image = current;
        if (image == null) {
            Transform existing = parent.Find(objectName);
            image = existing != null ? existing.GetComponent<Image>() : null;
        }

        if (image == null) {
            GameObject obj = CreateRectGameObject(objectName);
            obj.transform.SetParent(parent, false);
            image = obj.AddComponent<Image>();
        } else if (image.transform.parent != parent) {
            image.transform.SetParent(parent, false);
        }

        ConfigureTopLeftRect(image.rectTransform, x, y, width, height);
        VisualUIHelper.ApplySlicedSprite(image, visualID, Color.white, missingColor, raycastTarget);
        image.transform.SetAsLastSibling();
        return image;
    }

    private Image EnsureSimpleImage(Transform parent, Image current, string objectName) {
        if (!IsValidTransform(parent)) {
            return null;
        }

        Image image = current;
        if (image == null) {
            Transform existing = parent.Find(objectName);
            image = existing != null ? existing.GetComponent<Image>() : null;
        }

        if (image == null) {
            GameObject obj = CreateRectGameObject(objectName);
            obj.transform.SetParent(parent, false);
            image = obj.AddComponent<Image>();
        } else if (image.transform.parent != parent) {
            image.transform.SetParent(parent, false);
        }

        image.raycastTarget = false;
        return image;
    }

    private Image EnsureStatusTrack(Transform parent, Image currentTrack, ref Image currentFill, string objectName, Vector2 anchoredPosition, Vector2 size, string visualID, Color fillColor) {
        Image track = currentTrack;
        if (track == null) {
            Transform existing = parent.Find(objectName);
            track = existing != null ? existing.GetComponent<Image>() : null;
        }

        if (track == null) {
            GameObject obj = CreateRectGameObject(objectName);
            obj.transform.SetParent(parent, false);
            track = obj.AddComponent<Image>();
        }

        RectTransform rect = track.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        VisualUIHelper.ApplySlicedSprite(track, visualID, Color.white, new Color(0.15f, 0.1f, 0.08f, 0.9f), false);
        currentFill = EnsureBarFill(track.transform, currentFill, "Fill", fillColor);
        return track;
    }

    private Image EnsureBarFill(Transform parent, Image currentFill, string objectName, Color fillColor) {
        Image fill = currentFill;
        if (fill == null) {
            Transform existing = parent.Find(objectName);
            fill = existing != null ? existing.GetComponent<Image>() : null;
        }

        if (fill == null) {
            GameObject fillObj = CreateRectGameObject(objectName);
            fillObj.transform.SetParent(parent, false);
            fill = fillObj.AddComponent<Image>();
        }

        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);
        VisualUIHelper.ApplySolidColor(fill, fillColor, false);
        return fill;
    }

    private void SetBarFill(Image fill, float ratio) {
        if (fill == null) {
            return;
        }

        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);
        fill.gameObject.SetActive(ratio > 0.001f);
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

        GameObject pipRoot = CreateRectGameObject("ApPips");
        pipRoot.transform.SetParent(parent, false);
        RectTransform rect = pipRoot.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(250f, -152f);
        rect.sizeDelta = new Vector2(270f, 28f);
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
        MoveTextToPanel(hpLabel, new Vector2(24f, -18f), new Vector2(210f, 28f), 19, new Color(1f, 0.56f, 0.48f));
        MoveTextToPanel(shieldLabel, new Vector2(24f, -66f), new Vector2(210f, 28f), 18, new Color(0.72f, 0.86f, 1f));
        MoveTextToPanel(sanLabel, new Vector2(24f, -112f), new Vector2(210f, 28f), 18, new Color(0.74f, 0.56f, 1f));
        MoveTextToPanel(apLabel, new Vector2(24f, -154f), new Vector2(210f, 28f), 18, new Color(0.72f, 0.95f, 1f));

        if (targetHintLabel != null && _targetHintPanel != null) {
            targetHintLabel.transform.SetParent(_targetHintPanel.transform, false);
            targetHintLabel.fontSize = 19;
            targetHintLabel.alignment = TextAnchor.MiddleCenter;
            targetHintLabel.raycastTarget = false;
            StretchToParent(targetHintLabel.rectTransform, 24f, 8f);
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

    private void ApplyCombatButtonSkins() {
        VisualUIHelper.ApplyButtonSkin(endTurnBtn, VisualAssetService.UIButtonPrimaryID, new Color(0.8f, 0.2f, 0.2f));
        VisualUIHelper.ApplyButtonSkin(_cancelSelectionBtn, VisualAssetService.UIButtonSecondaryID, new Color(0.24f, 0.22f, 0.2f));
    }

    private void RefreshApPips(int current, int max) {
        if (_apPipParent == null) {
            return;
        }

        for (int i = _apPipParent.childCount - 1; i >= 0; i--) {
            DestroyRuntimeObject(_apPipParent.GetChild(i).gameObject);
        }

        for (int i = 0; i < Mathf.Max(0, max); i++) {
            GameObject pipObj = CreateRectGameObject($"AP_{i + 1}");
            pipObj.transform.SetParent(_apPipParent, false);
            Image pip = pipObj.AddComponent<Image>();
            RectTransform rect = pip.rectTransform;
            rect.sizeDelta = new Vector2(24f, 24f);
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

    private void SetActiveIfPresent(Component component, bool active) {
        if (component != null) {
            component.gameObject.SetActive(active);
        }
    }

    private Button CreateButton(Transform parent, string objectName, string labelText) {
        GameObject buttonObj = CreateRectGameObject(objectName);
        buttonObj.transform.SetParent(parent, false);
        Image image = buttonObj.AddComponent<Image>();
        image.raycastTarget = true;
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;
        ConfigureButtonText(button, labelText, 24, Color.white);
        return button;
    }

    private void ConfigureButtonText(Button button, string labelText, int fontSize, Color color) {
        if (button == null) {
            return;
        }

        Text label = button.GetComponentInChildren<Text>();
        if (label == null) {
            GameObject textObj = CreateRectGameObject("Text");
            textObj.transform.SetParent(button.transform, false);
            label = textObj.AddComponent<Text>();
        }

        label.font = _defaultFont;
        label.fontSize = fontSize;
        label.color = color;
        label.text = labelText;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        StretchToParent(label.rectTransform, 10f, 4f);
    }

    private Text EnsureChildLabel(Transform parent, Text current, string objectName, string text, int fontSize, Color color, TextAnchor alignment) {
        Text label = current;
        if (label == null) {
            Transform existing = parent.Find(objectName);
            label = existing != null ? existing.GetComponent<Text>() : null;
        }

        if (label == null) {
            GameObject textObj = CreateRectGameObject(objectName);
            textObj.transform.SetParent(parent, false);
            label = textObj.AddComponent<Text>();
        } else if (label.transform.parent != parent) {
            label.transform.SetParent(parent, false);
        }

        label.font = _defaultFont;
        label.fontSize = fontSize;
        label.color = color;
        label.text = text;
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }

    private Transform EnsureTransform(Transform parent, string objectName) {
        if (!IsValidTransform(parent)) {
            return null;
        }

        try {
            Transform existing = parent.Find(objectName);
            if (IsValidTransform(existing)) {
                if (!(existing is RectTransform)) {
                    DestroyRuntimeObject(existing.gameObject);
                } else {
                    return existing;
                }
            }
        } catch (MissingReferenceException) {
            return null;
        }

        return CreateTransform(parent, objectName);
    }

    private Transform CreateTransform(Transform parent, string objectName) {
        if (!IsValidTransform(parent)) {
            return null;
        }

        GameObject obj = CreateRectGameObject(objectName);
        try {
            obj.transform.SetParent(parent, false);
        } catch (MissingReferenceException) {
            DestroyRuntimeObject(obj);
            return null;
        }

        return obj.transform;
    }

    private GameObject CreateRectGameObject(string objectName) {
        return new GameObject(objectName, typeof(RectTransform));
    }

    private RectTransform EnsureRect(Transform target) {
        if (!IsValidTransform(target)) {
            return null;
        }

        RectTransform rect = target as RectTransform;
        if (rect != null) {
            return rect;
        }

        return target.gameObject.AddComponent<RectTransform>();
    }

    private void ConfigureTopLeftRect(RectTransform rect, float x, float y, float width, float height) {
        if (rect == null) {
            return;
        }

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
    }

    private void StretchToParent(RectTransform rect, float horizontalPadding, float verticalPadding) {
        if (rect == null) {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(horizontalPadding, verticalPadding);
        rect.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
    }

    private Transform FindChildRecursive(Transform parent, string objectName) {
        if (!IsValidTransform(parent)) {
            return null;
        }

        try {
            for (int i = 0; i < parent.childCount; i++) {
                Transform child = parent.GetChild(i);
                if (!IsValidTransform(child)) {
                    continue;
                }

                if (child.name == objectName) {
                    return child;
                }

                Transform match = FindChildRecursive(child, objectName);
                if (match != null) {
                    return match;
                }
            }
        } catch (MissingReferenceException) {
            return null;
        }

        return null;
    }

    private bool IsValidTransform(Transform target) {
        return target != null;
    }

    private void RemoveLayoutComponents(GameObject target) {
        if (target == null) {
            return;
        }

        HorizontalLayoutGroup horizontal = target.GetComponent<HorizontalLayoutGroup>();
        if (horizontal != null) {
            DestroyRuntimeObject(horizontal);
        }

        VerticalLayoutGroup vertical = target.GetComponent<VerticalLayoutGroup>();
        if (vertical != null) {
            DestroyRuntimeObject(vertical);
        }

        GridLayoutGroup grid = target.GetComponent<GridLayoutGroup>();
        if (grid != null) {
            DestroyRuntimeObject(grid);
        }

        ContentSizeFitter fitter = target.GetComponent<ContentSizeFitter>();
        if (fitter != null) {
            DestroyRuntimeObject(fitter);
        }
    }

    private void DestroyRuntimeObject(Object target) {
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
