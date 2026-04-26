using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public Text hpLabel;
    public Text sanLabel;
    public Text apLabel;
    public Button endTurnBtn;

    private void OnEnable()
    {
        // 2. 绑定交互事件：结束回合按钮
        if (endTurnBtn != null)
        {
            endTurnBtn.onClick.AddListener(() => {
                if (GameRoot.Core.Combat != null && GameRoot.Core.Combat.CurrentState == CombatState.PlayerTurn) {
                    GameRoot.Core.Combat.EndPlayerTurn();
                } else {
                    Debug.LogWarning("[HUD] 不在玩家回合，无法点击结束回合。");
                }
            });
        }

        // 3. 订阅全局事件总线
        GameEventBus.OnHPChanged += HandleHPChanged;
        GameEventBus.OnSANChanged += HandleSANChanged;
        GameEventBus.OnAPChanged += HandleAPChanged;
        GameEventBus.OnTurnStarted += HandleTurnStarted;
        CombatEventBus.OnCombatPhase += HandleCombatPhase;
        
        Debug.Log("[HUDController] UGUI HUD Initialized and subscribed to EventBus.");
    }

    private void OnDisable()
    {
        GameEventBus.OnHPChanged -= HandleHPChanged;
        GameEventBus.OnSANChanged -= HandleSANChanged;
        GameEventBus.OnAPChanged -= HandleAPChanged;
        GameEventBus.OnTurnStarted -= HandleTurnStarted;
        CombatEventBus.OnCombatPhase -= HandleCombatPhase;
    }

    // --- 事件处理逻辑 ---

    private void HandleHPChanged(string id, int current, int max)
    {
        if (GameRoot.Core.CurrentPlayer != null && id == GameRoot.Core.CurrentPlayer.ActiveDoll.Name) {
            if (hpLabel != null) hpLabel.text = $"HP: {current} / {max}";
        }
    }

    private void HandleSANChanged(string id, int current, int max)
    {
        if (GameRoot.Core.CurrentPlayer != null && id == GameRoot.Core.CurrentPlayer.ActiveDoll.Name) {
            if (sanLabel != null) sanLabel.text = $"SAN: {current} / {max}";
        }
    }

    private void HandleAPChanged(string id, int current, int max)
    {
        if (GameRoot.Core.CurrentPlayer != null && id == GameRoot.Core.CurrentPlayer.ActiveDoll.Name) {
            if (apLabel != null) apLabel.text = $"AP: {current} / {max}";
        }
    }

    private void HandleTurnStarted(FactionType type)
    {
        if (endTurnBtn != null) {
            endTurnBtn.interactable = (type == FactionType.Player);
            Text btnText = endTurnBtn.GetComponentInChildren<Text>();
            if (btnText != null) {
                btnText.text = type == FactionType.Player ? "结束回合" : "敌方行动中...";
            }
        }
    }
    
    private void HandleCombatPhase(CombatEventType phase, CombatFaction activeFaction)
    {
        if (phase == CombatEventType.OnTurnStart) {
            HandleTurnStarted(activeFaction.Type);
        }
    }
}
