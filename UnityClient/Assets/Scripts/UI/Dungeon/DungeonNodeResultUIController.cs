using System;
using UnityEngine;
using UnityEngine.UI;

public class DungeonNodeResultUIController : MonoBehaviour {
    public Text titleText;
    public Text summaryText;
    public Button continueBtn;

    public void Present(DungeonNodeResolutionResult result, Action onContinue) {
        if (result == null) {
            return;
        }

        if (titleText != null) {
            titleText.text = string.IsNullOrEmpty(result.Title) ? "节点结算" : result.Title;
            titleText.raycastTarget = false;
        }

        if (summaryText != null) {
            summaryText.text = string.IsNullOrEmpty(result.Summary) ? "节点事件已经完成。" : result.Summary;
            summaryText.raycastTarget = false;
        }

        if (continueBtn != null) {
            Text buttonText = continueBtn.GetComponentInChildren<Text>();
            if (buttonText != null) {
                buttonText.text = string.IsNullOrEmpty(result.PrimaryActionLabel) ? "继续探索" : result.PrimaryActionLabel;
            }

            continueBtn.onClick.RemoveAllListeners();
            continueBtn.onClick.AddListener(() => onContinue?.Invoke());
        }
    }
}
