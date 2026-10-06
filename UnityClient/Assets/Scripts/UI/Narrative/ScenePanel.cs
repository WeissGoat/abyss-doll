using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Plays a short scene (SceneConfig): lines one at a time with ▶, then the choices (or 继续 when there are none).
// Shows state and forwards the chosen ID; what a choice does lives in the caller's domain service.
public class ScenePanel : MonoBehaviour {
    public const string PanelName = "ScenePanel_Runtime";
    public const string ContinueLabel = "▶";
    public const string DoneLabel = "继续";
    private const int MaxChoices = 3;

    private Text _title;
    private Text _speaker;
    private Text _line;
    private Text _extra;
    private Button _continueButton;
    private readonly List<Button> _choiceButtons = new List<Button>();
    private readonly List<string> _choiceIDs = new List<string>();
    private SceneConfig _scene;
    private List<SceneLine> _lines;
    private int _lineIndex;
    private Action<string> _onChosen;

    public static ScenePanel Active { get; private set; }

    public static bool IsShowing => Active != null && Active.gameObject.activeSelf;

    // extraText: a block shown above the choices, for numbers the scene cannot know (the C segment recap).
    public static ScenePanel Play(string sceneID, Action<string> onChosen, IList<string> allowedChoiceIDs = null, string extraText = null) {
        SceneConfig scene = null;
        if (ConfigManager.Scenes == null || !ConfigManager.Scenes.TryGetValue(sceneID, out scene)) {
            Debug.LogWarning($"[ScenePanel] Scene {sceneID} not found.");
        }

        ScenePanel panel = Ensure();
        if (panel == null) {
            onChosen?.Invoke(null);
            return null;
        }

        panel.Present(scene ?? new SceneConfig { SceneID = sceneID }, allowedChoiceIDs, extraText, onChosen);
        return panel;
    }

    private static ScenePanel Ensure() {
        if (Active != null) {
            return Active;
        }

        Canvas canvas = MainCanvas.Find();
        if (canvas == null) {
            return null;
        }

        GameObject root = UITheme.CreateModal(canvas.transform, PanelName, new Vector2(1180f, 470f), 620);
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.42f);
        RectTransform dialogRect = (RectTransform)root.transform.Find("Dialog");
        UITheme.Place(dialogRect, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1180f, 470f));
        dialogRect.pivot = new Vector2(0.5f, 0f);
        dialogRect.anchoredPosition = new Vector2(0f, 40f);
        Active = root.AddComponent<ScenePanel>();
        Active.Build(dialogRect);
        return Active;
    }

    private void OnDestroy() {
        if (Active == this) {
            Active = null;
        }
    }

    private void Build(Transform dialog) {
        _title = UITheme.CreateText(dialog, "Title_Text", string.Empty, UITheme.CaptionSize, UITheme.TextSecondary, TextAnchor.MiddleLeft);
        UITheme.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(1100f, 30f));
        _speaker = UITheme.CreateText(dialog, "Speaker_Text", string.Empty, UITheme.BodySize, UITheme.Accent, TextAnchor.MiddleLeft);
        UITheme.Place(_speaker.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(1100f, 32f));
        _line = UITheme.CreateText(dialog, "Line_Text", string.Empty, UITheme.HeadingSize, UITheme.TextPrimary, TextAnchor.UpperLeft);
        UITheme.Place(_line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(1100f, 80f));
        _extra = UITheme.CreateText(dialog, "Extra_Text", string.Empty, UITheme.BodySize - 2, UITheme.TextSecondary, TextAnchor.UpperLeft);
        UITheme.Place(_extra.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(1100f, 110f));

        _continueButton = UITheme.CreateButton(dialog, "SceneContinue_Button", ContinueLabel, UITheme.PanelRaised, UITheme.Accent, UITheme.HeadingSize);
        UITheme.AddBorder(_continueButton.targetGraphic, UITheme.Accent, 1.5f);
        UITheme.Place((RectTransform)_continueButton.transform, new Vector2(1f, 0f), new Vector2(-110f, 30f), new Vector2(150f, 56f));
        _continueButton.onClick.AddListener(Advance);

        for (int i = 0; i < MaxChoices; i++) {
            int index = i;
            Button button = UITheme.CreateButton(dialog, $"SceneChoice_{i}", string.Empty, i == 0 ? UITheme.Accent : UITheme.PanelRaised, i == 0 ? UITheme.OnAccent : UITheme.TextPrimary, UITheme.BodySize);
            UITheme.AddBorder(button.targetGraphic, UITheme.Border, 1.5f);
            button.onClick.AddListener(() => Choose(index));
            _choiceButtons.Add(button);
        }
    }

    private void Present(SceneConfig scene, IList<string> allowedChoiceIDs, string extraText, Action<string> onChosen) {
        _scene = scene;
        _onChosen = onChosen;
        _lines = scene.Lines ?? new List<SceneLine>();
        _lineIndex = 0;
        _title.text = scene.Title ?? string.Empty;
        _extra.text = extraText ?? string.Empty;

        _choiceIDs.Clear();
        List<SceneChoice> choices = new List<SceneChoice>();
        foreach (SceneChoice choice in scene.Choices ?? new List<SceneChoice>()) {
            if (allowedChoiceIDs == null || allowedChoiceIDs.Contains(choice.ID)) {
                choices.Add(choice);
            }
        }

        // Choices sit side by side under the text, each with its hint on a second line.
        float width = choices.Count <= 1 ? 520f : choices.Count == 2 ? 520f : 350f;
        float gap = 20f;
        float total = choices.Count * width + Mathf.Max(0, choices.Count - 1) * gap;
        for (int i = 0; i < _choiceButtons.Count; i++) {
            Button button = _choiceButtons[i];
            bool used = i < choices.Count;
            button.gameObject.SetActive(false);
            if (!used) {
                continue;
            }

            _choiceIDs.Add(choices[i].ID);
            string hint = string.IsNullOrEmpty(choices[i].Hint) ? string.Empty : $"\n<size={UITheme.CaptionSize - 2}>{choices[i].Hint}</size>";
            UITheme.SetButtonLabel(button, choices[i].Label + hint);
            UITheme.Place((RectTransform)button.transform, new Vector2(0.5f, 0f), new Vector2(-total / 2f + width / 2f + i * (width + gap), 30f), new Vector2(width, 84f));
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        ShowLine();
    }

    // The recap block (extra text) is one more page after the lines.
    private int PageCount => Mathf.Max(1, _lines.Count + (string.IsNullOrEmpty(_extra.text) ? 0 : 1));

    private void ShowLine() {
        SceneLine line = _lineIndex < _lines.Count ? _lines[_lineIndex] : null;
        bool atEnd = _lineIndex >= PageCount - 1;
        bool showExtra = line == null && !string.IsNullOrEmpty(_extra.text);
        _speaker.text = line?.Speaker ?? string.Empty;
        _line.text = line?.Text ?? string.Empty;
        _extra.gameObject.SetActive(showExtra);
        _extra.rectTransform.anchoredPosition = new Vector2(0f, -66f);

        bool showChoices = atEnd && _choiceIDs.Count > 0;
        for (int i = 0; i < _choiceIDs.Count; i++) {
            _choiceButtons[i].gameObject.SetActive(showChoices);
        }

        UITheme.SetButtonLabel(_continueButton, atEnd ? DoneLabel : ContinueLabel);
        _continueButton.gameObject.SetActive(!showChoices);
    }

    private void Advance() {
        GameAudio.PlaySfx("page");
        bool atEnd = _lineIndex >= PageCount - 1;
        if (!atEnd) {
            _lineIndex++;
            ShowLine();
            return;
        }

        Finish(null);
    }

    private void Choose(int index) {
        if (index < 0 || index >= _choiceIDs.Count) {
            return;
        }

        Finish(_choiceIDs[index]);
    }

    private void Finish(string choiceID) {
        gameObject.SetActive(false);
        Action<string> callback = _onChosen;
        _onChosen = null;
        callback?.Invoke(choiceID);
    }
}
