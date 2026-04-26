using UnityEngine;
using UnityEngine.UI;

public class WorkshopUIController : MonoBehaviour {
    public Text moneyText;
    public Text chassisInfoText;
    public Button upgradeBtn;
    public Button departBtn;

    void Start() {
        if (upgradeBtn != null) {
            upgradeBtn.onClick.AddListener(() => {
                GameRoot.Core.Workshop.UpgradeDollChassis(GameRoot.Core.CurrentPlayer.ActiveDoll);
                RefreshUI();
                
                // 升级后网格变大了，需要重新生成 UI 格子
                var chassis = GameRoot.Core.CurrentPlayer.ActiveDoll.Chassis;
                FindObjectOfType<GridGenerator>().GenerateGrid(chassis);
            });
        }

        if (departBtn != null) {
            departBtn.onClick.AddListener(() => {
                GameFlowController.Instance.DepartToDungeon();
            });
        }
    }

    public void RefreshUI() {
        var player = GameRoot.Core.CurrentPlayer;
        if (moneyText != null) moneyText.text = $"金币 (Money): {player.Money}G\n仓库素材: {player.StashInventory.Count}件";
        
        var chassis = player.ActiveDoll.Chassis;
        if (chassisInfoText != null) chassisInfoText.text = $"当前底盘: {chassis.ChassisID}\n容量: {chassis.GridWidth}x{chassis.GridHeight}";
    }
}