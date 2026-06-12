using UnityEngine;

public abstract class WorkshopFormalV2PanelControllerBase : MonoBehaviour {
    private WorkshopFormalV1PanelController _panelController;

    protected abstract string ScreenID { get; }

    public void Open() {
        EnsurePanelController().Show(ScreenID);
    }

    public void Close() {
        EnsurePanelController().Hide();
    }

    protected WorkshopFormalV1PanelController EnsurePanelController() {
        if (_panelController != null) {
            return _panelController;
        }

        _panelController = GetComponent<WorkshopFormalV1PanelController>();
        if (_panelController == null) {
            _panelController = gameObject.AddComponent<WorkshopFormalV1PanelController>();
        }

        return _panelController;
    }
}

public sealed class MaintenancePanelController : WorkshopFormalV2PanelControllerBase {
    protected override string ScreenID => "maintenance_panel";
}

public sealed class DailyBillReportController : WorkshopFormalV2PanelControllerBase {
    protected override string ScreenID => "daily_bill_report";
}

public sealed class ShopStagingController : WorkshopFormalV2PanelControllerBase {
    protected override string ScreenID => "shop_staging";
}

public sealed class OrderBoardController : WorkshopFormalV2PanelControllerBase {
    protected override string ScreenID => "order_board";
}

public sealed class RumorBoardController : WorkshopFormalV2PanelControllerBase {
    protected override string ScreenID => "rumor_board";
}

public sealed class BusinessSettlementController : WorkshopFormalV2PanelControllerBase {
    protected override string ScreenID => "business_settlement";
}

public sealed class ChassisUpgradeController : WorkshopFormalV2PanelControllerBase {
    protected override string ScreenID => "chassis_upgrade_panel";
}

public sealed class DollInteractionController : WorkshopFormalV2PanelControllerBase {
    protected override string ScreenID => "doll_interaction";
}

public sealed class DollRoomController : WorkshopFormalV2PanelControllerBase {
    protected override string ScreenID => "doll_room";
}

public sealed class FactionShopController : WorkshopFormalV2PanelControllerBase {
    protected override string ScreenID => "faction_shop";
}

public sealed class ScenarioEventController : WorkshopFormalV2PanelControllerBase {
    protected override string ScreenID => "scenario_event";
}
