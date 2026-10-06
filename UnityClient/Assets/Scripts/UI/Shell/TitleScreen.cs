using System;
using UnityEngine;
using UnityEngine.UI;

// The Demo's title screen (M3 shell): 继续游戏 (only with a save), 新游戏, 设置, 退出游戏.
public class TitleScreen : MonoBehaviour {
    public const string PanelName = "TitleScreen_Runtime";
    public const string ContinueLabel = "继续游戏";
    public const string NewGameLabel = "新游戏";
    public const string SettingsLabel = "设置";
    public const string QuitLabel = "退出游戏";
    private const string BackgroundVisualID = "cg_t0_01a_p06_panel01_no0_close";

    public static bool IsShowing => _active != null && _active.gameObject.activeSelf;
    private static TitleScreen _active;

    private SaveFile _save;
    private Action _onContinue;
    private Action _onNewGame;
    private GameObject _confirm;

    public static void Show(SaveFile save, Action onContinue, Action onNewGame) {
        Canvas canvas = MainCanvas.Find();
        if (canvas == null) {
            onNewGame?.Invoke();
            return;
        }

        GameObject root = new GameObject(PanelName, typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        UITheme.Stretch((RectTransform)root.transform);
        Canvas overlay = root.AddComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingOrder = 800;
        root.AddComponent<GraphicRaycaster>();
        _active = root.AddComponent<TitleScreen>();
        _active._save = save;
        _active._onContinue = onContinue;
        _active._onNewGame = onNewGame;
        _active.Build();
    }

    private void OnDestroy() {
        if (_active == this) {
            _active = null;
        }
    }

    private void Build() {
        Image background = UITheme.CreatePanel(transform, "TitleBackground_Image", UITheme.Background);
        UITheme.Stretch(background.rectTransform);
        VisualUIHelper.ApplyCoverSprite(background, BackgroundVisualID, new Color(0.78f, 0.72f, 0.64f, 1f), UITheme.Background);
        Image veil = UITheme.CreatePanel(transform, "TitleVeil_Image", new Color(0.09f, 0.055f, 0.03f, 0.5f));
        UITheme.Stretch(veil.rectTransform);

        Text title = UITheme.CreateText(transform, "Title_Text", "Project P3", 72, UITheme.AccentOnDark, TextAnchor.MiddleLeft);
        UITheme.Place(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(160f, 250f), new Vector2(900f, 100f));
        title.rectTransform.pivot = new Vector2(0f, 0.5f);
        Text subtitle = UITheme.CreateText(transform, "Subtitle_Text", "她还能陪你下去一次。", UITheme.HeadingSize, UITheme.TextOnDark, TextAnchor.MiddleLeft);
        UITheme.Place(subtitle.rectTransform, new Vector2(0f, 0.5f), new Vector2(160f, 175f), new Vector2(900f, 44f));
        subtitle.rectTransform.pivot = new Vector2(0f, 0.5f);

        float y = 60f;
        if (_save != null) {
            Button continueButton = AddMenuButton(ContinueLabel, y, true, Continue);
            PlayerProfile player = _save.Player;
            Text summary = UITheme.CreateText(continueButton.transform, "SaveSummary_Text",
                $"第 {player.CurrentDay} 天 · 资金 {player.Money}G · 保存于 {_save.SavedAt}", UITheme.CaptionSize - 2, UITheme.TextOnDarkMuted, TextAnchor.MiddleLeft);
            UITheme.Place(summary.rectTransform, new Vector2(1f, 0.5f), new Vector2(20f, 0f), new Vector2(560f, 30f));
            summary.rectTransform.pivot = new Vector2(0f, 0.5f);
            y -= 84f;
        }

        AddMenuButton(NewGameLabel, y, _save == null, NewGame);
        AddMenuButton(SettingsLabel, y - 84f, false, () => SettingsPanel.Show(transform));
        AddMenuButton(QuitLabel, y - 168f, false, QuitGame);

        Text version = UITheme.CreateText(transform, "Version_Text", $"Demo 试玩版 · {Application.version}", UITheme.CaptionSize - 2, UITheme.TextOnDarkMuted, TextAnchor.LowerRight);
        UITheme.Place(version.rectTransform, new Vector2(1f, 0f), new Vector2(-24f, 18f), new Vector2(600f, 30f));
        version.rectTransform.pivot = new Vector2(1f, 0f);
    }

    private Button AddMenuButton(string label, float y, bool primary, Action onClick) {
        Button button = UITheme.CreateButton(transform, label + "_Button", label, primary ? UITheme.Accent : UITheme.PanelRaised, primary ? UITheme.OnAccent : UITheme.TextPrimary, UITheme.HeadingSize);
        if (!primary) {
            UITheme.AddBorder(button.targetGraphic, UITheme.Border, 1.5f);
        }

        RectTransform rect = (RectTransform)button.transform;
        UITheme.Place(rect, new Vector2(0f, 0.5f), new Vector2(160f, y), new Vector2(320f, 64f));
        rect.pivot = new Vector2(0f, 0.5f);
        button.onClick.AddListener(() => onClick());
        return button;
    }

    private void Continue() {
        Action callback = _onContinue;
        Close();
        callback?.Invoke();
    }

    // With a save on disk a new game asks first: the old save is replaced once the new prologue reaches the town.
    private void NewGame() {
        if (_save == null) {
            StartNewGame();
            return;
        }

        if (_confirm == null) {
            _confirm = UITheme.CreateModal(transform, "NewGameConfirm_Modal", new Vector2(640f, 260f), 820);
            Transform dialog = _confirm.transform.Find("Dialog");
            Text body = UITheme.CreateText(dialog, "Body_Text", "开始新游戏？\n序章结束、回到小镇后，会覆盖现在的存档。", UITheme.BodySize, UITheme.TextPrimary, TextAnchor.MiddleCenter);
            UITheme.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(580f, 110f));
            Button yes = UITheme.CreateButton(dialog, "ConfirmNewGame_Button", "开始新游戏", UITheme.Accent, UITheme.OnAccent);
            UITheme.Place((RectTransform)yes.transform, new Vector2(0.5f, 0f), new Vector2(-130f, 28f), new Vector2(220f, 56f));
            yes.onClick.AddListener(StartNewGame);
            Button no = UITheme.CreateButton(dialog, "CancelNewGame_Button", "取消", UITheme.PanelRaised, UITheme.TextPrimary);
            UITheme.AddBorder(no.targetGraphic, UITheme.Border, 1.5f);
            UITheme.Place((RectTransform)no.transform, new Vector2(0.5f, 0f), new Vector2(130f, 28f), new Vector2(220f, 56f));
            no.onClick.AddListener(() => _confirm.SetActive(false));
        }

        _confirm.SetActive(true);
    }

    private void StartNewGame() {
        Action callback = _onNewGame;
        Close();
        callback?.Invoke();
    }

    private void Close() {
        gameObject.SetActive(false);
        if (Application.isPlaying) {
            Destroy(gameObject);
        } else {
            DestroyImmediate(gameObject);
        }
    }

    public static void QuitGame() {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
