using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Plays a short scene (SceneConfig): lines one at a time with ▶, then the choices (or 继续 when there are none).
// Shows state and forwards the chosen ID; what a choice does lives in the caller's domain service. In a scene where
// Zero speaks she stands beside the box as her large portrait (ZeroBust), wearing the face of her last line and
// stepping back into the shade while someone else talks.
public class ScenePanel : MonoBehaviour {
    public const string PanelName = "ScenePanel_Runtime";
    public const string ContinueLabel = "▶";
    public const string DoneLabel = "继续";
    public const string ZeroSpeaker = "零号";
    private const int MaxChoices = 3;
    private const float WideWidth = 1180f;
    private const float NarrowWidth = 1040f;
    private const float Margin = 40f;
    private static readonly Color ListeningTint = new Color(0.62f, 0.62f, 0.68f, 1f);

    private RectTransform _dialog;
    private Image _bust;
    private bool _zeroOnStage;
    private string _face;
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

    // Zero's face on her portrait now (ZeroFace), or empty when she is not on stage.
    public string CurrentFace => _zeroOnStage && _bust.gameObject.activeSelf ? _face : string.Empty;

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
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
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

    private void Build(RectTransform dialog) {
        _dialog = dialog;
        // Zero on the right, standing on the bottom edge, clear of the narrowed box.
        _bust = UITheme.CreatePanel(transform, "ZeroBust", Color.white);
        _bust.raycastTarget = false;
        UITheme.Place(_bust.rectTransform, new Vector2(0.5f, 0f), new Vector2(590f, 0f), ZeroBust.Size);
        _bust.transform.SetSiblingIndex(dialog.GetSiblingIndex());
        _bust.gameObject.SetActive(false);

        _title = UITheme.CreateText(dialog, "Title_Text", string.Empty, UITheme.CaptionSize, UITheme.TextSecondary, TextAnchor.MiddleLeft);
        PlaceRow(_title.rectTransform, -24f, 30f);
        _speaker = UITheme.CreateText(dialog, "Speaker_Text", string.Empty, UITheme.BodySize, UITheme.Accent, TextAnchor.MiddleLeft);
        PlaceRow(_speaker.rectTransform, -66f, 32f);
        _line = UITheme.CreateText(dialog, "Line_Text", string.Empty, UITheme.HeadingSize, UITheme.TextPrimary, TextAnchor.UpperLeft);
        PlaceRow(_line.rectTransform, -100f, 80f);
        _extra = UITheme.CreateText(dialog, "Extra_Text", string.Empty, UITheme.BodySize - 2, UITheme.TextSecondary, TextAnchor.UpperLeft);
        PlaceRow(_extra.rectTransform, -100f, 110f);

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
        _zeroOnStage = ZeroBust.HasArt && _lines.Exists(line => line?.Speaker == ZeroSpeaker);
        _face = ZeroFace.Neutral;
        _bust.gameObject.SetActive(false);
        float dialogWidth = _zeroOnStage ? NarrowWidth : WideWidth;
        // Tall only when there are choices or extra text under the line; a plain scene was a big, mostly empty box.
        bool roomy = (scene.Choices != null && scene.Choices.Count > 0) || !string.IsNullOrEmpty(extraText);
        _dialog.sizeDelta = new Vector2(dialogWidth, roomy ? 470f : 270f);
        _dialog.anchoredPosition = new Vector2(_zeroOnStage ? -360f : 0f, _dialog.anchoredPosition.y);

        _choiceIDs.Clear();
        List<SceneChoice> choices = new List<SceneChoice>();
        foreach (SceneChoice choice in scene.Choices ?? new List<SceneChoice>()) {
            if (allowedChoiceIDs == null || allowedChoiceIDs.Contains(choice.ID)) {
                choices.Add(choice);
            }
        }

        // Choices sit side by side under the text, each with its hint on a second line.
        float gap = 20f;
        float width = choices.Count == 0 ? 0f : Mathf.Min(520f, (dialogWidth - 2f * Margin - (choices.Count - 1) * gap) / choices.Count);
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
        ShowZero(line);
        _extra.gameObject.SetActive(showExtra);
        _extra.rectTransform.anchoredPosition = new Vector2(0f, -66f);

        bool showChoices = atEnd && _choiceIDs.Count > 0;
        for (int i = 0; i < _choiceIDs.Count; i++) {
            _choiceButtons[i].gameObject.SetActive(showChoices);
        }

        UITheme.SetButtonLabel(_continueButton, atEnd ? DoneLabel : ContinueLabel);
        _continueButton.gameObject.SetActive(!showChoices);
    }

    private void ShowZero(SceneLine line) {
        if (!_zeroOnStage) {
            return;
        }

        bool speaking = line?.Speaker == ZeroSpeaker;
        if (speaking) {
            _face = string.IsNullOrEmpty(line.Face) ? ZeroFace.Neutral : line.Face;
        }

        _face = ZeroBust.Apply(_bust, _face, speaking ? Color.white : ListeningTint);
    }

    // A row of the box stretched to its width (which narrows when Zero is on stage), y down from the top.
    private static void PlaceRow(RectTransform rect, float y, float height) {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(Margin, y - height);
        rect.offsetMax = new Vector2(-Margin, y);
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
