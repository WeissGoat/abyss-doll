using UnityEngine;
using UnityEngine.UI;

// Esc menu (M3 shell): 继续, 设置, 回到标题, 退出游戏. Esc closes the settings first, then the menu.
public class PauseMenu : MonoBehaviour {
    public const string PanelName = "PauseMenu_Runtime";
    public const string ResumeLabel = "继续";
    public const string TitleLabel = "回到标题";

    private GameObject _root;
    private GameObject _confirm;
    private Text _confirmText;

    public bool IsOpen => _root != null && _root.activeSelf;

    private void Update() {
        if (!Input.GetKeyDown(KeyCode.Escape) || TitleScreen.IsShowing) {
            return;
        }

        if (SettingsPanel.IsShowing) {
            SettingsPanel.Hide();
        } else if (IsOpen) {
            Close();
        } else {
            Open();
        }
    }

    public void Open() {
        if (_root == null) {
            Build();
        }

        if (_root == null) {
            return;
        }

        _confirm.SetActive(false);
        _root.SetActive(true);
        _root.transform.SetAsLastSibling();
    }

    public void Close() {
        if (_root != null) {
            _root.SetActive(false);
        }
    }

    private void Build() {
        Canvas canvas = MainCanvas.Find();
        if (canvas == null) {
            return;
        }

        _root = UITheme.CreateModal(canvas.transform, PanelName, new Vector2(460f, 470f), 880);
        Transform dialog = _root.transform.Find("Dialog");
        Text title = UITheme.CreateText(dialog, "Title_Text", "暂停", UITheme.TitleSize, UITheme.Accent, TextAnchor.MiddleCenter);
        UITheme.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -44f), new Vector2(400f, 50f));

        AddButton(dialog, ResumeLabel, -120f, true, Close);
        AddButton(dialog, "设置", -204f, false, () => SettingsPanel.Show(_root.transform));
        AddButton(dialog, TitleLabel, -288f, false, AskBackToTitle);
        AddButton(dialog, TitleScreen.QuitLabel, -372f, false, TitleScreen.QuitGame);

        _confirm = UITheme.CreateModal(_root.transform, "BackToTitleConfirm_Modal", new Vector2(620f, 260f), 890);
        Transform confirmDialog = _confirm.transform.Find("Dialog");
        _confirmText = UITheme.CreateText(confirmDialog, "Body_Text", string.Empty, UITheme.BodySize, UITheme.TextPrimary, TextAnchor.MiddleCenter);
        UITheme.Place(_confirmText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(560f, 110f));
        Button yes = UITheme.CreateButton(confirmDialog, "ConfirmBackToTitle_Button", TitleLabel, UITheme.Accent, UITheme.OnAccent);
        UITheme.Place((RectTransform)yes.transform, new Vector2(0.5f, 0f), new Vector2(-130f, 28f), new Vector2(220f, 56f));
        yes.onClick.AddListener(GameRoot.RestartGame);
        Button no = UITheme.CreateButton(confirmDialog, "CancelBackToTitle_Button", "取消", UITheme.PanelRaised, UITheme.TextPrimary);
        UITheme.AddBorder(no.targetGraphic, UITheme.Border, 1.5f);
        UITheme.Place((RectTransform)no.transform, new Vector2(0.5f, 0f), new Vector2(130f, 28f), new Vector2(220f, 56f));
        no.onClick.AddListener(() => _confirm.SetActive(false));
        _confirm.SetActive(false);
    }

    private void AddButton(Transform dialog, string label, float y, bool primary, UnityEngine.Events.UnityAction onClick) {
        Button button = UITheme.CreateButton(dialog, label + "_Button", label, primary ? UITheme.Accent : UITheme.PanelRaised, primary ? UITheme.OnAccent : UITheme.TextPrimary, UITheme.HeadingSize - 2);
        if (!primary) {
            UITheme.AddBorder(button.targetGraphic, UITheme.Border, 1.5f);
        }

        UITheme.Place((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(320f, 64f));
        button.onClick.AddListener(onClick);
    }

    // The town autosaves, so only a dive or the prologue in progress is lost.
    private void AskBackToTitle() {
        bool inTown = GameFlowController.Instance != null && GameFlowController.Instance.IsInTownLoop;
        _confirmText.text = inTown
            ? "回到标题画面？\n小镇的进度已经自动保存。"
            : "回到标题画面？\n这次下潜（或序章）的进度会丢失，\n回到上一次在小镇的存档。";
        _confirm.SetActive(true);
        _confirm.transform.SetAsLastSibling();
    }
}
