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
    private Image _incomingPanel;
    private Text _incomingLabel;
    private Coroutine _autoEndTurn;
    private readonly EndTurnClickGuard _endTurnGuard = new EndTurnClickGuard();
    private Button _cancelSelectionBtn;
    private Button _unarmedStrikeBtn;
    private Image _playerStatusPanel;
    private Image _playerShadowImage;
    private Image _playerDollImage;
    private const string DollCombatPoseSet = "doll_zero_combat";
    private Image _hpBarTrack;
    private Image _hpBarFill;
    private Image _playerHpPlate;
    private Image _hpLossFill;
    private Image _shieldBadge;
    private Image _sanBarTrack;
    private Image _sanBarFill;
    private const string ShieldIconID = "ui_combat_intent_defend";
    // The turn banner shows while the enemy acts, while a target is being picked, and this long into her turn.
    private const float TurnBannerSeconds = 1.2f;
    // The right end of her HP bar (x on the reference canvas): the "-9" tag hangs under it, right-aligned.
    private const float HpBarRight = 662f;
    private float _playerTurnShownAt = -10f;
    // The fight in which she has already acted (spent AP): the how-to-attack hint is gone from then on.
    private object _actedFight;
    private Transform _apPipParent;
    private CombatIntentReadabilitySnapshot _intentSnapshot;
    private CombatMotionDirector _motion;
    // Whose turn the screen shows. The backend resolves the enemy turn at once; the screen catches up through the
    // VisualQueue, and the player can act only once it shows the player turn.
    private bool _playerTurnShown;
    public bool PlayerTurnShown => _playerTurnShown;
    private static HUDController _active;

    // True while a fight is on screen and the screen has not caught up to the player turn yet: inputs that act in
    // combat (backpack clicks, buttons) wait.
    public static bool EnemyTurnShowing => _active != null && _active.isActiveAndEnabled && !_active._playerTurnShown
        && GameRoot.Core?.Combat != null && GameRoot.Core.Combat.CurrentState != CombatState.End;

    public void SetLootOverlayMode(bool active) {
        SetActiveIfPresent(_turnBannerImage, !active);
        SetActiveIfPresent(_targetHintPanel, !active);
        SetActiveIfPresent(_actionStrip, !active);
        SetActiveIfPresent(_incomingPanel, !active);
        SetActiveIfPresent(_playerStatusPanel, !active);
        SetActiveIfPresent(_playerHpPlate, !active);
    }

    private void OnEnable() {
        _defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureRuntimeWidgets();
        BindEndTurnButton();
        BindCancelSelectionButton();
        BindUnarmedStrikeButton();

        GameEventBus.OnHPChanged += HandleHPChanged;
        GameEventBus.OnSANChanged += HandleSANChanged;
        GameEventBus.OnAPChanged += HandleAPChanged;
        GameEventBus.OnShieldChanged += HandleShieldChanged;
        GameEventBus.OnTurnStarted += HandleQueuedTurnStarted;
        GameEventBus.OnTargetSelectionChanged += HandleTargetSelectionChanged;
        CombatEventBus.OnCombatPhase += HandleCombatPhase;

        _active = this;
        CombatSystem combatAtOpen = GameRoot.Core?.Combat;
        _playerTurnShown = combatAtOpen != null && combatAtOpen.CurrentState == CombatState.PlayerTurn && VisualQueue.Count == 0;
        _playerTurnShownAt = Time.unscaledTime;
        RefreshCombatPresentation();
        PlayEnemyIntro();
        Debug.Log("[HUDController] Combat HUD initialized.");
    }

    // A boss with a registered entrance clip plays it once at the start of the fight.
    private void PlayEnemyIntro() {
        CombatFaction enemies = GameRoot.Core?.Combat?.EnemyFaction;
        if (enemies == null || enemies.Fighters.Count == 0) {
            return;
        }

        foreach (FighterEntity fighter in enemies.Fighters) {
            if (CombatIntroCutscene.TryPlay(transform, enemies.Fighters[0], ResolveEnemyCombatVisualID(fighter)) != null) {
                return;
            }
        }
    }

    private void OnDisable() {
        if (_active == this) {
            _active = null;
        }

        _autoEndTurn = null;
        GameEventBus.OnHPChanged -= HandleHPChanged;
        GameEventBus.OnSANChanged -= HandleSANChanged;
        GameEventBus.OnAPChanged -= HandleAPChanged;
        GameEventBus.OnShieldChanged -= HandleShieldChanged;
        GameEventBus.OnTurnStarted -= HandleQueuedTurnStarted;
        GameEventBus.OnTargetSelectionChanged -= HandleTargetSelectionChanged;
        CombatEventBus.OnCombatPhase -= HandleCombatPhase;
    }

    private void BindEndTurnButton() {
        if (endTurnBtn == null) {
            return;
        }

        endTurnBtn.onClick.RemoveAllListeners();
        endTurnBtn.onClick.AddListener(() => {
            if (!_playerTurnShown) {
                Debug.Log("[HUD] 敌方还在行动，忽略这次结束回合点击。");
                return;
            }

            if (!_endTurnGuard.AllowManualEnd(Time.unscaledTime)) {
                Debug.Log("[HUD] 新回合刚开始，忽略这次结束回合点击。");
                return;
            }

            if (GameRoot.Core?.Combat != null && GameRoot.Core.Combat.CurrentState == CombatState.PlayerTurn) {
                // A click during the auto-end beat takes over, so the turn ends once and the beat cannot fire later.
                CancelAutoEndTurn();
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

    private void BindUnarmedStrikeButton() {
        if (_unarmedStrikeBtn == null) {
            return;
        }

        _unarmedStrikeBtn.onClick.RemoveAllListeners();
        _unarmedStrikeBtn.onClick.AddListener(() => {
            if (!_playerTurnShown) {
                return;
            }

            if (!ItemUseService.TryBeginUnarmedStrike(out string reason)) {
                PlayerToast.Show(reason, PlayerToastKind.Warning);
            }
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
        // The stage is the upper part of the screen (her feet near y 605); the backpack tray is below it.
        ConfigureTopLeftRect(EnsureRect(_playerStageRoot), 250f, 40f, 440f, 620f);

        _enemyStageRoot = ResolveEnemyStageRoot();
        if (_enemyStageRoot == null) {
            _enemyStageRoot = CreateTransform(_stageRoot, "EnemyStageRoot");
        }
        if (_enemyStageRoot == null) {
            Debug.LogWarning("[HUDController] Failed to create EnemyStageRoot. Combat enemy widgets will be skipped.");
            return;
        }
        // From the middle of the stage to the right edge: up to three monsters side by side.
        ConfigureTopLeftRect(EnsureRect(_enemyStageRoot), 860f, 100f, 1020f, 500f);
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

        // Floating numbers sit above both sides of the stage; the stage itself is what a heavy hit shakes.
        RectTransform popupLayer = EnsureRect(EnsureTransform(_stageRoot, "CombatPopupLayer"));
        if (popupLayer != null) {
            popupLayer.anchorMin = Vector2.zero;
            popupLayer.anchorMax = Vector2.one;
            popupLayer.pivot = new Vector2(0.5f, 0.5f);
            popupLayer.anchoredPosition = Vector2.zero;
            popupLayer.sizeDelta = Vector2.zero;
            popupLayer.SetAsLastSibling();
        }

        _motion = CombatMotionDirector.Ensure(gameObject);
        _motion.SetLayers(popupLayer, stageRect);

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
        EnsureBottomShade();

        _turnBannerImage = EnsureSkinImage(
            transform,
            _turnBannerImage,
            "TurnBanner",
            760f,
            24f,
            400f,
            58f,
            VisualAssetService.UICombatTurnBannerID,
            new Color(0.08f, 0.07f, 0.065f, 0.68f),
            false);
        if (_turnBannerImage.GetComponent<CanvasGroup>() == null) {
            _turnBannerImage.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
        }

        _turnBannerLabel = EnsureChildLabel(
            _turnBannerImage.transform,
            _turnBannerLabel,
            "TurnBanner_Text",
            "玩家回合",
            24,
            UITheme.TextOnDark,
            TextAnchor.MiddleCenter);
        StretchToParent(_turnBannerLabel.rectTransform, 28f, 8f);

        // Between the stage and the tray, only while it helps (RefreshTargetHint).
        _targetHintPanel = EnsureSkinImage(
            transform,
            _targetHintPanel,
            "TargetHintPanel",
            680f,
            596f,
            560f,
            44f,
            VisualAssetService.UIPanelInfoID,
            new Color(0.08f, 0.075f, 0.065f, 0.56f),
            false);

        // Right of the tray: bare hands, cancel and the big end-turn button, with no panel behind them.
        _actionStrip = EnsureSkinImage(
            transform,
            _actionStrip,
            "ActionStrip",
            1320f,
            720f,
            560f,
            330f,
            VisualAssetService.UIPanelInfoID,
            Color.clear,
            false);
        ClearImage(_actionStrip);

        // Under her feet: HP in the bar, the shield badge on its left, and what the enemy turn will take flashing on it.
        _playerHpPlate = EnsureSkinImage(
            transform,
            _playerHpPlate,
            "PlayerHpPlate",
            270f,
            612f,
            400f,
            64f,
            VisualAssetService.UIPanelInfoID,
            Color.clear,
            false);
        ClearImage(_playerHpPlate);

        _hpBarTrack = EnsureStatusTrack(
            _playerHpPlate.transform,
            _hpBarTrack,
            ref _hpBarFill,
            "HpBar",
            new Vector2(62f, -12f),
            new Vector2(330f, 40f),
            VisualAssetService.UICombatStatusBarHpID,
            new Color(0.86f, 0.16f, 0.12f, 0.95f));
        _hpLossFill = EnsureBarFill(_hpBarTrack.transform, _hpLossFill, "IncomingLoss", new Color(1f, 0.84f, 0.42f, 0.9f));

        _shieldBadge = EnsureSimpleImage(_playerHpPlate.transform, _shieldBadge, "ShieldBadge");
        ConfigureTopLeftRect(_shieldBadge.rectTransform, 0f, -2f, 60f, 64f);
        VisualUIHelper.ApplySimpleSprite(_shieldBadge, ShieldIconID, Color.white, new Color(0.52f, 0.68f, 0.95f, 0.92f), false, true);

        _incomingPanel = EnsureSkinImage(
            transform,
            _incomingPanel,
            "IncomingDamagePanel",
            HpBarRight - 130f,
            680f,
            130f,
            40f,
            VisualAssetService.UIPanelInfoID,
            new Color(0.08f, 0.06f, 0.05f, 0.7f),
            false);
        VisualUIHelper.ApplySolidColor(_incomingPanel, new Color(0.1f, 0.06f, 0.05f, 0.82f), false);
        _incomingLabel = EnsureChildLabel(
            _incomingPanel.transform,
            _incomingLabel,
            "IncomingDamage_Text",
            string.Empty,
            24,
            UITheme.TextOnDark,
            TextAnchor.MiddleCenter);
        _incomingLabel.fontStyle = FontStyle.Bold;
        _incomingLabel.verticalOverflow = VerticalWrapMode.Overflow;
        StretchToParent(_incomingLabel.rectTransform, 8f, 2f);

        // Left of the tray: AP and SAN.
        _playerStatusPanel = EnsureSkinImage(
            transform,
            _playerStatusPanel,
            "PlayerStatusCluster",
            70f,
            770f,
            540f,
            230f,
            VisualAssetService.UIPanelInfoID,
            Color.clear,
            false);
        ClearImage(_playerStatusPanel);

        _sanBarTrack = EnsureStatusTrack(
            _playerStatusPanel.transform,
            _sanBarTrack,
            ref _sanBarFill,
            "SanBar",
            new Vector2(24f, -170f),
            new Vector2(330f, 24f),
            VisualAssetService.UICombatStatusBarShieldID,
            new Color(0.56f, 0.46f, 0.86f, 0.95f));

        EnsureApPipParent();
    }

    // A dark gradient from the middle of the stage down, so the tray and the numbers under it read on any background.
    private void EnsureBottomShade() {
        Image shade = EnsureSimpleImage(transform, null, "CombatBottomShade");
        ConfigureTopLeftRect(shade.rectTransform, 0f, 500f, 1920f, 580f);
        shade.sprite = BottomShadeSprite();
        shade.type = Image.Type.Simple;
        shade.preserveAspect = false;
        shade.color = Color.white;
        int above = backgroundImage != null ? backgroundImage.transform.GetSiblingIndex() + 1 : 0;
        shade.transform.SetSiblingIndex(above);
    }

    private static Sprite _bottomShade;

    private static Sprite BottomShadeSprite() {
        if (_bottomShade != null) {
            return _bottomShade;
        }

        const int height = 64;
        Texture2D texture = new Texture2D(1, height, TextureFormat.RGBA32, false) {
            name = "CombatBottomShade",
            wrapMode = TextureWrapMode.Clamp
        };
        for (int y = 0; y < height; y++) {
            float t = 1f - y / (float)(height - 1);
            texture.SetPixel(0, y, new Color(0.04f, 0.03f, 0.025f, 0.72f * Mathf.SmoothStep(0f, 1f, t)));
        }
        texture.Apply(false, true);
        _bottomShade = Sprite.Create(texture, new Rect(0f, 0f, 1f, height), new Vector2(0.5f, 0.5f), 1f);
        return _bottomShade;
    }

    private static void ClearImage(Image image) {
        if (image != null) {
            image.sprite = null;
            image.color = Color.clear;
            image.raycastTarget = false;
        }
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
        // Her combat stance when the pose set is registered, otherwise the standing portrait.
        string dollVisualID = CombatMotionDirector.HasPose(DollCombatPoseSet, CombatPose.Idle)
            ? CombatMotionDirector.PoseVisualID(DollCombatPoseSet, CombatPose.Idle)
            : "doll_proto_0_stand";
        VisualUIHelper.ApplyContainSprite(
            _playerDollImage,
            dollVisualID,
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

        ConfigureTopLeftRect(endTurnBtn.GetComponent<RectTransform>(), 310f, 50f, 220f, 220f);
        ConfigureButtonText(endTurnBtn, "结束回合", 30, UITheme.OnAccent);

        if (_cancelSelectionBtn == null) {
            Transform existing = _actionStrip.transform.Find("CancelSelection_Button");
            _cancelSelectionBtn = existing != null ? existing.GetComponent<Button>() : null;
        }

        if (_cancelSelectionBtn == null) {
            _cancelSelectionBtn = CreateButton(_actionStrip.transform, "CancelSelection_Button", "取消选择");
        } else {
            _cancelSelectionBtn.transform.SetParent(_actionStrip.transform, false);
        }

        ConfigureTopLeftRect(_cancelSelectionBtn.GetComponent<RectTransform>(), 30f, 170f, 240f, 56f);
        ConfigureButtonText(_cancelSelectionBtn, "取消选择", 21, UITheme.TextPrimary);

        if (_unarmedStrikeBtn == null) {
            Transform existingUnarmed = _actionStrip.transform.Find("UnarmedStrike_Button");
            _unarmedStrikeBtn = existingUnarmed != null ? existingUnarmed.GetComponent<Button>() : null;
        }

        if (_unarmedStrikeBtn == null) {
            _unarmedStrikeBtn = CreateButton(_actionStrip.transform, "UnarmedStrike_Button", "徒手攻击");
            BindUnarmedStrikeButton();
        }

        ConfigureTopLeftRect(_unarmedStrikeBtn.GetComponent<RectTransform>(), 30f, 90f, 240f, 64f);
        DollEntity doll = GameRoot.Core?.CurrentPlayer?.ActiveDoll;
        ConfigureButtonText(_unarmedStrikeBtn, $"徒手 {ItemUseService.GetUnarmedDamage(doll)}伤害", 21, UITheme.TextPrimary);
    }

    private void EnsureTargetHintLabel() {
        if (targetHintLabel == null) {
            targetHintLabel = EnsureChildLabel(
                _targetHintPanel.transform,
                targetHintLabel,
                "TargetHint_Text",
                "点击背包里的武器后，再点击右侧敌人进行攻击。",
                22,
                UITheme.TextPrimary,
                TextAnchor.MiddleCenter);
        } else {
            targetHintLabel.transform.SetParent(_targetHintPanel.transform, false);
        }
    }

    private void EnsureStatusTextLabels() {
        if (_playerStatusPanel == null || _playerHpPlate == null) {
            return;
        }

        if (hpLabel == null) {
            hpLabel = EnsureChildLabel(_hpBarTrack.transform, hpLabel, "HP_Text", "-- / --", 22, UITheme.TextOnDark, TextAnchor.MiddleCenter);
        }

        if (shieldLabel == null) {
            shieldLabel = EnsureChildLabel(_shieldBadge.transform, shieldLabel, "Shield_Text", "0", 22, UITheme.TextOnDark, TextAnchor.MiddleCenter);
        }

        if (sanLabel == null) {
            sanLabel = EnsureChildLabel(_playerStatusPanel.transform, sanLabel, "SAN_Text", "SAN -- / --", 22, UITheme.TextOnDark, TextAnchor.MiddleLeft);
        }

        if (apLabel == null) {
            apLabel = EnsureChildLabel(_playerStatusPanel.transform, apLabel, "AP_Text", "AP -- / --", 22, UITheme.TextOnDark, TextAnchor.MiddleLeft);
        }
    }

    private void RefreshCombatPresentation() {
        RefreshStaticData();
        RefreshIntentSnapshot();
        RefreshEnemyButtons();
        RefreshTargetHint();
        RefreshActionButtons();
        RefreshIncomingPreview();
    }

    // The damage the locked intents will deal when the turn ends, after the current shield.
    private void RefreshIncomingPreview() {
        if (_incomingPanel == null || _incomingLabel == null) {
            return;
        }

        CombatSystem combat = GameRoot.Core?.Combat;
        IncomingDamagePreview preview = combat != null && combat.CurrentState == CombatState.PlayerTurn
            ? CombatReadabilityTextService.BuildIncomingDamagePreview(combat)
            : null;
        bool show = preview != null && preview.Total > 0;
        _incomingPanel.gameObject.SetActive(show);
        int maxHP = GetActivePlayerFighter()?.RuntimeMaxHP ?? 0;
        if (!show || maxHP <= 0) {
            SetBarSegment(_hpLossFill, 0f, 0f);
            return;
        }

        _incomingLabel.text = preview.Lethal ? $"-{preview.HPLoss} 会倒下"
            : preview.HPLoss > 0 ? $"-{preview.HPLoss}"
            : "护盾挡住";
        _incomingLabel.color = preview.Lethal
            ? new Color(1f, 0.42f, 0.36f)
            : preview.HPLoss > 0 ? UITheme.AccentOnDark : UITheme.PositiveOnDark;
        RectTransform tag = _incomingPanel.rectTransform;
        float width = preview.Lethal ? 190f : 130f;
        tag.sizeDelta = new Vector2(width, tag.sizeDelta.y);
        tag.anchoredPosition = new Vector2(HpBarRight - width, tag.anchoredPosition.y);
        SetBarSegment(_hpLossFill, (float)preview.HPAfter / maxHP, (float)preview.HPBefore / maxHP);
    }

    // The bar between two fractions (what the enemy turn will take off her HP).
    private void SetBarSegment(Image fill, float from, float to) {
        if (fill == null) {
            return;
        }

        RectTransform rect = fill.rectTransform;
        rect.anchorMin = new Vector2(Mathf.Clamp01(from), 0f);
        rect.anchorMax = new Vector2(Mathf.Clamp01(to), 1f);
        rect.offsetMin = new Vector2(from <= 0.001f ? 4f : 0f, 4f);
        rect.offsetMax = new Vector2(-4f, -4f);
        fill.gameObject.SetActive(to - from > 0.001f);
    }

    private void Update() {
        if (_hpLossFill != null && _hpLossFill.gameObject.activeSelf) {
            Color color = _hpLossFill.color;
            color.a = 0.45f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.2f));
            _hpLossFill.color = color;
        }

        CanvasGroup banner = _turnBannerImage != null ? _turnBannerImage.GetComponent<CanvasGroup>() : null;
        if (banner != null) {
            banner.alpha = Mathf.MoveTowards(banner.alpha, TurnBannerWanted() ? 1f : 0f, Time.unscaledDeltaTime * 4f);
        }
    }

    private bool TurnBannerWanted() {
        CombatSystem combat = GameRoot.Core?.Combat;
        if (combat == null || combat.CurrentState == CombatState.End) {
            return false;
        }

        return !_playerTurnShown || ItemUseService.HasPendingEnemyTargetSelection || _autoEndTurn != null
            || Time.unscaledTime - _playerTurnShownAt < TurnBannerSeconds;
    }

    private static object CurrentFight() {
        CombatFaction enemies = GameRoot.Core?.Combat?.EnemyFaction;
        return enemies != null && enemies.Fighters.Count > 0 ? enemies.Fighters[0] : null;
    }

    // Out of AP with nothing affordable left: end the turn for the player after a short beat, so the last hit lands
    // on screen first. The rule (what still counts as an action) lives in CombatSystem.PlayerHasActionLeft.
    private void MaybeAutoEndTurn() {
        CombatSystem combat = GameRoot.Core?.Combat;
        if (_autoEndTurn != null || !isActiveAndEnabled || combat == null || combat.CurrentState != CombatState.PlayerTurn
            || ItemUseService.HasPendingEnemyTargetSelection || combat.PlayerHasActionLeft()) {
            return;
        }

        _autoEndTurn = StartCoroutine(AutoEndTurnAfterBeat());
    }

    private void CancelAutoEndTurn() {
        if (_autoEndTurn != null) {
            StopCoroutine(_autoEndTurn);
            _autoEndTurn = null;
        }
    }

    private System.Collections.IEnumerator AutoEndTurnAfterBeat() {
        if (_turnBannerLabel != null) {
            _turnBannerLabel.text = "AP 用完，自动结束回合";
        }

        yield return new WaitForSecondsRealtime(0.8f);
        _autoEndTurn = null;
        CombatSystem combat = GameRoot.Core?.Combat;
        if (combat != null && combat.CurrentState == CombatState.PlayerTurn && !combat.PlayerHasActionLeft()
            && !ItemUseService.HasPendingEnemyTargetSelection) {
            combat.EndPlayerTurn();
        } else if (_turnBannerLabel != null && combat != null && combat.CurrentState == CombatState.PlayerTurn) {
            _turnBannerLabel.text = "玩家回合";
        }
    }

    private void RefreshStaticData() {
        var doll = GameRoot.Core?.CurrentPlayer?.ActiveDoll;
        if (doll == null) {
            return;
        }

        DollFighter activeFighter = GetActivePlayerFighter();
        if (activeFighter != null && _motion != null) {
            _motion.Register(activeFighter, _playerDollImage, true, DollCombatPoseSet);
        }

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

        // Intents are locked again at every player turn start; a snapshot from OnEnable would show stale cards.
        RefreshIntentSnapshot();

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
        if (_motion != null) {
            _motion.Register(fighter, sprite, false, ResolveEnemyCombatVisualID(fighter));
        }

        float hpRatio = fighter != null && fighter.RuntimeMaxHP > 0
            ? Mathf.Clamp01((float)Mathf.Max(0, fighter.RuntimeHP) / fighter.RuntimeMaxHP)
            : 0f;
        Image hpBar = CreateEnemyFootBar(
            slotObj.transform,
            "EnemyFootHpBar",
            VisualAssetService.UICombatStatusBarHpID,
            new Vector2(0f, 40f),
            new Vector2(260f, 32f),
            hpRatio,
            new Color(0.86f, 0.16f, 0.12f, 0.95f));
        Text hpText = EnsureChildLabel(hpBar.transform, null, "EnemyHp_Text", BuildEnemyHpText(fighter), 20, UITheme.TextOnDark, TextAnchor.MiddleCenter);
        PlaceLabel(hpText, hpBar, Vector2.zero, Vector2.zero, 20, true);

        if (isAlive && fighter.RuntimeShield > 0) {
            Image shieldBadge = CreateEnemyImage(slotObj.transform, "EnemyShieldBadge", new Vector2(-150f, 40f), new Vector2(50f, 54f));
            VisualUIHelper.ApplySimpleSprite(shieldBadge, ShieldIconID, Color.white, new Color(0.52f, 0.68f, 0.95f, 0.92f), false, true);
            Text shieldText = EnsureChildLabel(shieldBadge.transform, null, "EnemyShield_Text", fighter.RuntimeShield.ToString(), 20, UITheme.TextOnDark, TextAnchor.MiddleCenter);
            PlaceLabel(shieldText, shieldBadge, Vector2.zero, Vector2.zero, 20, true);
        }

        CreateEnemyNameAndIntent(slotObj.transform, fighter, isAlive, targetable, SpriteOpaqueTop(sprite));
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

        // A monster with a pose set stands in its "_idle" pose, drawn with the other poses so they match.
        string visualID = ResolveEnemyCombatVisualID(fighter);
        if (CombatMotionDirector.HasPose(visualID, CombatPose.Idle)) {
            visualID = CombatMotionDirector.PoseVisualID(visualID, CombatPose.Idle);
        }

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

    private Image CreateEnemyFootBar(Transform parent, string objectName, string visualID, Vector2 bottomPosition, Vector2 size, float ratio, Color fillColor) {
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
        return track;
    }

    // Under its HP bar its name (and what a non-attack intent does), statuses above the bar, and over its head the
    // intent badge: the intent's icon and its number. spriteTop: the top of its sprite from the slot's bottom.
    private void CreateEnemyNameAndIntent(Transform parent, FighterEntity fighter, bool isAlive, bool targetable, float spriteTop) {
        Text nameLabel = CreateEnemyLabel(parent, "EnemyName_Text", fighter != null ? fighter.Name : "未知敌人", 18, new Vector2(0f, 8f));
        nameLabel.color = isAlive ? UITheme.TextOnDark : new Color(0.65f, 0.65f, 0.65f, 0.92f);

        // Statuses on it (中毒 3 · 破甲 1), just above the HP bar.
        string statuses = isAlive ? CombatStatusService.Describe(fighter) : string.Empty;
        if (!string.IsNullOrEmpty(statuses)) {
            Text statusLabel = CreateEnemyLabel(parent, "EnemyStatus_Text", statuses, 18, new Vector2(0f, 76f));
            statusLabel.color = new Color(0.78f, 0.95f, 0.52f);
        }

        CombatIntentReadabilityLine line = isAlive ? FindIntentLineForFighter(fighter) : null;
        if (!isAlive) {
            CreateEnemyLabel(parent, "EnemyIntent_Text", "已击倒", 18, new Vector2(0f, -18f));
            return;
        }

        if (line == null || !line.HasSelectedIntent) {
            return;
        }

        string intent = line.CanExecute ? line.IntentShort : $"{line.IntentShort}（做不到）";
        if (line.IntentType != MonsterIntentType.Attack || !line.CanExecute) {
            Text intentLabel = CreateEnemyLabel(parent, "EnemyIntent_Text", intent, 18, new Vector2(0f, -18f));
            intentLabel.color = targetable ? new Color(1f, 0.86f, 0.36f) : new Color(1f, 0.9f, 0.78f);
        }

        CreateEnemyIntentBadge(parent, line, spriteTop + 8f);
    }

    // Where the monster's drawing actually ends, from the slot's bottom: combat sprites are square with empty space
    // around the creature, and a Tight sprite mesh outlines what is painted (a Full Rect one gives the whole picture).
    private static float SpriteOpaqueTop(Image sprite) {
        RectTransform rect = sprite.rectTransform;
        Vector2 box = rect.sizeDelta;
        float drawnHeight = box.y;
        float paintedTop = 1f;
        Sprite source = sprite.sprite;
        if (source != null && source.rect.width > 0f) {
            if (sprite.preserveAspect) {
                drawnHeight = Mathf.Min(box.y, box.x * source.rect.height / source.rect.width);
            }

            Bounds bounds = source.bounds;
            float maxY = float.MinValue;
            foreach (Vector2 vertex in source.vertices) {
                maxY = Mathf.Max(maxY, vertex.y);
            }

            if (maxY > float.MinValue && bounds.size.y > 0f) {
                paintedTop = Mathf.Clamp01((maxY - bounds.min.y) / bounds.size.y);
            }
        }

        return rect.anchoredPosition.y + (box.y - drawnHeight) * 0.5f + drawnHeight * paintedTop;
    }

    private void CreateEnemyIntentBadge(Transform parent, CombatIntentReadabilityLine line, float bottom) {
        // Kept under the top edge of the screen for a tall monster.
        bottom = Mathf.Min(bottom, 470f);
        bool hasNumber = !string.IsNullOrEmpty(line.IntentBadge);
        float width = hasNumber ? 74f + 18f * Mathf.Max(1, line.IntentBadge.Length) : 76f;
        GameObject badgeObj = CreateRectGameObject("EnemyIntentBadge");
        badgeObj.transform.SetParent(parent, false);
        Image backing = badgeObj.AddComponent<Image>();
        VisualUIHelper.ApplySolidColor(backing, new Color(0.13f, 0.07f, 0.06f, 0.86f), false);
        RectTransform rect = backing.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, bottom);
        rect.sizeDelta = new Vector2(width, 64f);
        Outline frame = badgeObj.AddComponent<Outline>();
        frame.effectColor = line.IntentType == MonsterIntentType.Attack || line.IntentType == MonsterIntentType.SelfDestruct
            ? new Color(0.86f, 0.3f, 0.22f, 0.95f)
            : new Color(0.86f, 0.68f, 0.36f, 0.95f);
        frame.effectDistance = new Vector2(2f, -2f);

        Image icon = CreateRectGameObject("EnemyIntentIcon").AddComponent<Image>();
        icon.transform.SetParent(badgeObj.transform, false);
        ConfigureTopLeftRect(icon.rectTransform, 6f, 4f, 56f, 56f);
        VisualUIHelper.ApplySimpleSprite(icon, IntentIconID(line.IntentType), Color.white, new Color(0.9f, 0.5f, 0.3f, 1f), false, true);
        if (!line.CanExecute) {
            icon.color = new Color(1f, 1f, 1f, 0.4f);
        }

        if (hasNumber) {
            Text number = EnsureChildLabel(badgeObj.transform, null, "EnemyIntentBadge_Text", line.IntentBadge, 32, UITheme.TextOnDark, TextAnchor.MiddleLeft);
            PlaceLabel(number, backing, new Vector2(64f, -2f), new Vector2(width - 66f, 64f), 32, false);
            number.fontStyle = FontStyle.Bold;
        }
    }

    // Intent icons in Approved/UI, one per kind of intent.
    private static string IntentIconID(MonsterIntentType type) {
        switch (type) {
            case MonsterIntentType.Attack:
                return "ui_combat_intent_attack";
            case MonsterIntentType.WeakenWeapon:
                return "ui_combat_intent_debuff";
            case MonsterIntentType.AddJunk:
                return "ui_combat_intent_add_junk";
            case MonsterIntentType.Defend:
                return "ui_combat_intent_defend";
            case MonsterIntentType.Cleanse:
                return "ui_combat_intent_buff";
            case MonsterIntentType.SelfDestruct:
                return "ui_combat_intent_charge";
            case MonsterIntentType.Disrupt:
                return "ui_combat_intent_move_item";
            default:
                return "ui_combat_intent_unknown";
        }
    }

    private Text CreateEnemyLabel(Transform parent, string objectName, string text, int fontSize, Vector2 bottomPosition) {
        Text label = EnsureChildLabel(parent, null, objectName, text, fontSize, UITheme.TextOnDark, TextAnchor.MiddleCenter);
        Outline outline = label.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = bottomPosition;
        rect.sizeDelta = new Vector2(300f, 28f);
        return label;
    }

    private string ResolveEnemyCombatVisualID(FighterEntity fighter) {
        if (fighter is MonsterFighter monsterFighter) {
            return VisualAssetService.ResolveMonsterCombatVisualID(monsterFighter.DataRef);
        }

        return VisualAssetService.MissingSpriteVisualID;
    }

    private string BuildEnemyHpText(FighterEntity fighter) {
        return fighter != null ? $"{Mathf.Max(0, fighter.RuntimeHP)} / {fighter.RuntimeMaxHP}" : "-";
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

    // Slot centre x and top y in the enemy stage. Side by side, the middle one a step further back, so no monster hides
    // another and their bars and captions stay above the hint bar.
    private Vector2 ResolveEnemySlotPosition(int index, int count) {
        if (count <= 1) {
            return new Vector2(560f, 24f);
        }

        if (count == 2) {
            return index == 0
                ? new Vector2(380f, 40f)
                : new Vector2(740f, 30f);
        }

        switch (index) {
            case 0:
                return new Vector2(250f, 40f);
            case 1:
                return new Vector2(560f, 16f);
            default:
                return new Vector2(870f, 40f);
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
                hpLabel.text = $"{current} / {max}";
            }
            SetBarFill(_hpBarFill, max > 0 ? (float)current / max : 0f);
            RefreshIncomingPreview();
            return;
        }

        RefreshEnemyButtons();
        RefreshIncomingPreview();
    }

    private void HandleSANChanged(string id, int current, int max) {
        if (GameRoot.Core?.CurrentPlayer?.ActiveDoll != null && id == GameRoot.Core.CurrentPlayer.ActiveDoll.Name) {
            if (sanLabel != null) {
                sanLabel.text = $"SAN {current} / {max}";
            }
            SetBarFill(_sanBarFill, max > 0 ? (float)current / max : 0f);
        }
    }

    private void HandleAPChanged(string id, int current, int max) {
        if (GameRoot.Core?.CurrentPlayer?.ActiveDoll != null && id == GameRoot.Core.CurrentPlayer.ActiveDoll.Name) {
            if (apLabel != null) {
                apLabel.text = $"AP {current}/{max}";
            }

            CombatSystem combat = GameRoot.Core?.Combat;
            if (current < max && combat != null && combat.CurrentState == CombatState.PlayerTurn) {
                _actedFight = CurrentFight();
            }
            RefreshApPips(current, max);
            MaybeAutoEndTurn();
        }
    }

    private void HandleShieldChanged(string id, int current) {
        if (GameRoot.Core?.CurrentPlayer?.ActiveDoll != null && id == GameRoot.Core.CurrentPlayer.ActiveDoll.Name) {
            if (shieldLabel != null) {
                shieldLabel.text = current.ToString();
            }
            SetActiveIfPresent(_shieldBadge, current > 0);
            RefreshIncomingPreview();
            return;
        }

        RefreshEnemyButtons();
    }

    private void HandleTurnStarted(FactionType type) {
        _playerTurnShown = type == FactionType.Player;
        if (type == FactionType.Player) {
            _playerTurnShownAt = Time.unscaledTime;
            _endTurnGuard.OnPlayerTurnStarted(Time.unscaledTime);
        }

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
        RefreshIncomingPreview();
    }

    private void HandleTargetSelectionChanged(string message, bool isActive) {
        RefreshTargetHint(message, isActive);
        RefreshEnemyButtons();
        RefreshActionButtons();
        if (!isActive) {
            MaybeAutoEndTurn();
        }
    }

    // Turn starts reach the screen through GameEventBus.OnTurnStarted (queued); this only takes the turn away from the player.
    private void HandleCombatPhase(CombatEventType phase, CombatFaction activeFaction) {
        // The player's turn is over the moment it ends, even though the enemy turn reaches the screen later.
        if (phase == CombatEventType.OnCombatEnd || (phase == CombatEventType.OnTurnEnd && activeFaction?.Type == FactionType.Player)) {
            _playerTurnShown = false;
        }

        // A map node opens this screen before its fight starts, so the enemies are only known at the first turn
        // (TryPlay plays once per fight).
        if (phase == CombatEventType.OnTurnStart && activeFaction?.Type == FactionType.Player) {
            PlayEnemyIntro();
        }
    }

    private void HandleQueuedTurnStarted(FactionType type) {
        HandleTurnStarted(type);
        RefreshStaticData();
    }

    private void RefreshTargetHint() {
        string message = ItemUseService.HasPendingEnemyTargetSelection
            ? $"[{ItemUseService.PendingTargetItemName}] 已准备，点击敌人完成攻击。"
            : "点击背包里的武器，再点击敌人攻击。";
        RefreshTargetHint(message, ItemUseService.HasPendingEnemyTargetSelection);
    }

    private void RefreshTargetHint(string message, bool isActive) {
        if (targetHintLabel == null) {
            return;
        }

        targetHintLabel.text = message;
        targetHintLabel.color = isActive ? UITheme.Accent : UITheme.TextPrimary;
        // Until her first action in this fight, or while a target is being picked.
        bool acted = _actedFight != null && _actedFight == CurrentFight();
        SetActiveIfPresent(_targetHintPanel, isActive || !acted);

        if (_turnBannerLabel != null && isActive) {
            _turnBannerLabel.text = "选择攻击目标";
        }
    }

    private void RefreshActionButtons() {
        if (_unarmedStrikeBtn != null) {
            CombatSystem combat = GameRoot.Core?.Combat;
            _unarmedStrikeBtn.interactable = combat != null && combat.CurrentState == CombatState.PlayerTurn && _playerTurnShown;
        }

        SetActiveIfPresent(_cancelSelectionBtn, ItemUseService.HasPendingEnemyTargetSelection);
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
        rect.anchoredPosition = new Vector2(24f, -56f);
        rect.sizeDelta = new Vector2(330f, 52f);
        HorizontalLayoutGroup layout = pipRoot.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.spacing = 12f;
        _apPipParent = pipRoot.transform;
    }

    private void PositionCombatTexts() {
        PlaceLabel(hpLabel, _hpBarTrack, Vector2.zero, Vector2.zero, 22, true);
        PlaceLabel(shieldLabel, _shieldBadge, Vector2.zero, Vector2.zero, 22, true);
        PlaceLabel(apLabel, _playerStatusPanel, new Vector2(24f, -16f), new Vector2(160f, 36f), 22, false);
        PlaceLabel(sanLabel, _playerStatusPanel, new Vector2(24f, -128f), new Vector2(330f, 36f), 22, false);

        if (targetHintLabel != null && _targetHintPanel != null) {
            targetHintLabel.transform.SetParent(_targetHintPanel.transform, false);
            targetHintLabel.fontSize = 19;
            targetHintLabel.alignment = TextAnchor.MiddleCenter;
            targetHintLabel.raycastTarget = false;
            StretchToParent(targetHintLabel.rectTransform, 24f, 8f);
        }
    }

    // A label on dark ground with an outline: stretched over its parent (centered), or at a top-left spot in it.
    private void PlaceLabel(Text text, Component parent, Vector2 position, Vector2 size, int fontSize, bool stretch) {
        if (text == null || parent == null) {
            return;
        }

        text.transform.SetParent(parent.transform, false);
        text.fontSize = fontSize;
        text.color = UITheme.TextOnDark;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        // A line of Noto Sans SC is taller than its font size; truncating would drop the whole line in a short box.
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.alignment = stretch ? TextAnchor.MiddleCenter : TextAnchor.MiddleLeft;
        Outline outline = text.GetComponent<Outline>();
        if (outline == null) {
            outline = text.gameObject.AddComponent<Outline>();
        }
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        if (stretch) {
            StretchToParent(text.rectTransform, 4f, 0f);
            text.transform.SetAsLastSibling();
            return;
        }

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
        VisualUIHelper.ApplyButtonSkin(_unarmedStrikeBtn, VisualAssetService.UIButtonSecondaryID, new Color(0.3f, 0.26f, 0.22f));
    }

    private void RefreshApPips(int current, int max) {
        if (_apPipParent == null) {
            return;
        }

        for (int i = _apPipParent.childCount - 1; i >= 0; i--) {
            DestroyRuntimeObject(_apPipParent.GetChild(i).gameObject);
        }

        // 精神好 can lift AP one above the cap for a turn: draw the extra pip too.
        for (int i = 0; i < Mathf.Max(0, Mathf.Max(max, current)); i++) {
            GameObject pipObj = CreateRectGameObject($"AP_{i + 1}");
            pipObj.transform.SetParent(_apPipParent, false);
            Image pip = pipObj.AddComponent<Image>();
            RectTransform rect = pip.rectTransform;
            rect.sizeDelta = new Vector2(46f, 46f);
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
