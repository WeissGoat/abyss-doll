using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

// Developer shortcuts for playtesting, shared by the in-game dev panel (F1) and by Unity MCP
// (`execute_code`: `return P3DevTools.Describe();`). Every method returns a short text report, so a caller can
// follow the game without screenshots. Clicks go through the real buttons; GoTo/AddMoney/AddItem/Heal are debug
// routes that skip the player path. Nothing here is reachable from the normal player UI.
public static class P3DevTools {
    private const string CanvasName = "InventoryCanvas";
    private const string OverlayName = "P3DialogueOverlay_Runtime";
    private const string MapLayoutName = "MapLayout";
    private static readonly string[] ContinueLabels = { "确认拾取并继续", "返回工坊", "继续", "确定" };

    public static string Describe() {
        StringBuilder sb = new StringBuilder();
        Transform canvas = FindCanvas();
        if (canvas == null || GameRoot.Core?.CurrentPlayer == null) {
            return "Game is not running (no InventoryCanvas or GameRoot.Core).";
        }

        List<string> panels = new List<string>();
        foreach (Transform child in canvas) {
            if (child.gameObject.activeSelf && child.name != "InventoryItemLayer" && child.name != "GridContainer" && child.name != OverlayName) {
                panels.Add(child.name);
            }
        }
        sb.AppendLine("Screen: " + string.Join(", ", panels.ToArray()));

        P3DialogueOverlayController overlay = FindOverlay();
        if (overlay != null && overlay.IsShowing) {
            string speaker = ReadChildText(overlay.transform, "DialoguePanel_Image/Speaker_Text");
            string body = ReadChildText(overlay.transform, "DialoguePanel_Image/Body_Text");
            sb.AppendLine($"Prologue: showing, blocking={overlay.IsInputBlocking}, line='{speaker}: {body}'");
        }

        PlayerProfile player = GameRoot.Core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;
        sb.AppendLine($"Day {player.CurrentDay} (month {player.CurrentMonth}), money {player.Money}, HP {doll?.Status.HP_Current}/{doll?.Status.HP_Max}, SAN {doll?.Status.SAN_Current}/{doll?.Status.SAN_Max}");

        BackpackGrid grid = doll?.RuntimeGrid as BackpackGrid;
        List<string> items = new List<string>();
        if (grid != null) {
            foreach (ItemEntity item in grid.ContainedItems) {
                int x = item.Grid?.CurrentPos != null && item.Grid.CurrentPos.Length > 1 ? item.Grid.CurrentPos[0] : -1;
                int y = item.Grid?.CurrentPos != null && item.Grid.CurrentPos.Length > 1 ? item.Grid.CurrentPos[1] : -1;
                items.Add($"{item.Name}@{x},{y}");
            }
        }
        sb.AppendLine($"Backpack ({items.Count}): {string.Join("; ", items.ToArray())} | Stash: {player.StashInventory.Count}");

        CombatSystem combat = GameRoot.Core.Combat;
        if (combat != null && panels.Contains("CombatPanel") && combat.PlayerFaction?.Fighters != null && combat.PlayerFaction.Fighters.Count > 0) {
            DollFighter fighter = combat.PlayerFaction.Fighters[0] as DollFighter;
            List<string> enemies = new List<string>();
            MonsterIntentReport report = combat.CurrentState == CombatState.End ? null : MonsterIntentPreviewService.BuildReport(combat);
            if (report?.Monsters != null) {
                foreach (MonsterIntentCard card in report.Monsters) {
                    string intent = card.SelectedIntent != null ? card.SelectedIntent.Title : "?";
                    enemies.Add($"{card.MonsterName} {card.RuntimeHP}/{card.RuntimeMaxHP} intent={intent}");
                }
            }
            sb.AppendLine($"Combat: {combat.CurrentState}, HP {fighter?.RuntimeHP}, AP {fighter?.CurrentAP}, shield {fighter?.RuntimeShield}; enemies: {string.Join("; ", enemies.ToArray())}");
        }

        List<string> buttons = new List<string>();
        foreach (Button button in ClickableButtons()) {
            buttons.Add($"'{ButtonLabel(button)}'");
        }
        sb.AppendLine("Buttons: " + string.Join(", ", buttons.ToArray()));
        return sb.ToString();
    }

    // Clicks the first button the player could click whose label contains the text.
    public static string Click(string label) {
        foreach (Button button in ClickableButtons()) {
            if (ButtonLabel(button).Contains(label)) {
                button.onClick.Invoke();
                return $"Clicked '{ButtonLabel(button)}'.\n" + Describe();
            }
        }

        return $"No clickable button contains '{label}'.\n" + Describe();
    }

    public static string SkipPrologue() {
        PrologueFirstDiveController prologue = Object.FindObjectOfType<PrologueFirstDiveController>();
        if (prologue == null) {
            return "No prologue controller.\n" + Describe();
        }

        prologue.SkipForDebug();
        return "Prologue skipped.\n" + Describe();
    }

    // Debug route to a screen: workshop, sell, prosthetic, layers, map, combat, saferoom, stairs.
    public static string GoTo(string screen) {
        GameFlowController flow = GameFlowController.Instance;
        if (flow == null || GameRoot.Core == null) {
            return "Game is not running.";
        }

        WorkshopUIController workshop = Object.FindObjectOfType<WorkshopUIController>(true);
        switch ((screen ?? string.Empty).ToLowerInvariant()) {
            case "workshop":
                flow.EnterWorkshop();
                break;
            case "sell":
                flow.EnterWorkshop();
                workshop?.OpenSellPanel();
                break;
            case "prosthetic":
                flow.EnterWorkshop();
                workshop?.OpenProstheticPanel();
                break;
            case "layers":
                flow.EnterWorkshop();
                workshop?.OpenDungeonStartLayerPanel();
                break;
            case "map":
                if (!StartRun()) {
                    return "Could not start a run.\n" + Describe();
                }
                flow.EnterDungeonMap();
                break;
            case "combat":
                if (GameRoot.Core.Dungeon.CurrentLayer == null && !StartRun()) {
                    return "Could not start a run.\n" + Describe();
                }
                CombatNode node = FindFirst<CombatNode>(GameRoot.Core.Dungeon.CurrentLayer.RootNode);
                if (node == null) {
                    return "No combat node in the current layer.\n" + Describe();
                }
                GameRoot.Core.Dungeon.CurrentLayer.CurrentNode = node;
                node.IsVisited = true;
                GameRoot.Core.Combat.StartCombat(new List<string>(node.MonsterIDs));
                flow.EnterCombat();
                break;
            case "saferoom":
                flow.EnterSafeRoom(new SafeRoomNode { NodeID = "dev_safe_room" });
                break;
            case "stairs":
                flow.EnterStairs(new StairsNode { NodeID = "dev_stairs", LayerID = 1 });
                break;
            default:
                return $"Unknown screen '{screen}'. Use workshop, sell, prosthetic, layers, map, combat, saferoom or stairs.";
        }

        return $"Went to {screen}.\n" + Describe();
    }

    // Plays the current combat to the end: attacks with the strongest weapon, raises shields, ends turns.
    public static string AutoCombat() {
        string result = PlayCombat();
        return result + "\n" + Describe();
    }

    // Puts every pending loot item into the first free backpack slot that fits, then confirms the pickup.
    public static string TakeLoot() {
        string result = PlaceLoot();
        return result + "\n" + Describe();
    }

    // From the workshop, plays one dive the way a player would (map nodes, combat, loot) until the first safe room
    // or stairs, evacuates, and returns to the workshop. Stops early on anything it does not recognise.
    public static string RunDive(int maxSteps = 40) {
        StringBuilder log = new StringBuilder();
        if (IsPanelActive("WorkshopPanel") && !IsPanelActive("DungeonMapPanel")) {
            ClickByLabel("浅层入口", log);
            ClickByLabel("开始下潜", log);
        }

        string lastSignature = null;
        for (int step = 0; step < maxSteps; step++) {
            // Some screens (stairs, settlement) only react a frame later; one call runs in a single frame, so stop
            // when nothing changed and let the caller call again.
            string signature = StateSignature();
            if (signature == lastSignature) {
                log.AppendLine("Waiting for the game to update; call RunDive() again to continue.");
                break;
            }
            lastSignature = signature;

            CombatSystem combat = GameRoot.Core?.Combat;
            if (IsPanelActive("CombatLootPanel_Runtime")) {
                log.AppendLine(PlaceLoot());
            } else if (IsPanelActive("CombatPanel") && combat != null && combat.CurrentState != CombatState.End) {
                string combatResult = PlayCombat();
                log.AppendLine(combatResult);
                if (combat.CurrentState != CombatState.End) {
                    break;
                }
            } else if (IsPanelActive("SettlementPanel_Runtime")) {
                ClickByLabel("返回工坊", log);
                log.AppendLine("Dive finished.");
                break;
            } else if (IsPanelActive("SafeRoomPanel")) {
                if (!ClickByLabel("返回小镇", log)) {
                    break;
                }
            } else if (IsPanelActive("DungeonMapPanel")) {
                if (!EnterNextMapNode(log)) {
                    break;
                }
            } else if (!ClickFirstContinue(log)) {
                log.AppendLine("Stopped: unrecognised screen.");
                break;
            }
        }

        return log.ToString() + Describe();
    }

    public static string AddMoney(int amount) {
        GameRoot.Core.CurrentPlayer.Money += amount;
        return $"Money +{amount}.\n" + Describe();
    }

    // Adds an item to the stash (GroundInventory), where returning loot ends up.
    public static string AddItem(string configID) {
        ItemEntity item = ConfigManager.CreateItem(configID);
        if (item == null) {
            return $"Unknown item '{configID}'.";
        }

        ItemLifecycleService.MarkGroundInventoryItem(GameRoot.Core.CurrentPlayer, item, "DevTools");
        return $"Added {item.Name} to the stash.\n" + Describe();
    }

    public static string Heal() {
        DollEntity doll = GameRoot.Core.CurrentPlayer.ActiveDoll;
        doll.Status.HP_Current = doll.Status.HP_Max;
        doll.Status.SAN_Current = doll.Status.SAN_Max;
        CombatSystem combat = GameRoot.Core.Combat;
        if (combat?.PlayerFaction?.Fighters != null && combat.CurrentState != CombatState.End) {
            foreach (FighterEntity fighter in combat.PlayerFaction.Fighters) {
                fighter.RuntimeHP = doll.Status.HP_Max;
            }
        }

        return "Healed.\n" + Describe();
    }

    private static string PlayCombat() {
        CombatSystem combat = GameRoot.Core.Combat;
        BackpackGrid grid = GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid as BackpackGrid;
        int turns = 0;
        string reason;
        while (combat.CurrentState == CombatState.PlayerTurn && turns++ < 30) {
            DollFighter player = combat.PlayerFaction.Fighters[0] as DollFighter;
            ItemEntity weapon = null;
            foreach (ItemEntity item in grid.ContainedItems) {
                if (item.ItemType == nameof(ItemType.Weapon) && item.Combat != null && item.Combat.TriggerType == TriggerType.Manual.ToString()
                    && (weapon == null || item.Combat.BaseValue > weapon.Combat.BaseValue)) {
                    weapon = item;
                }
            }

            if (weapon == null) {
                return "AutoCombat: no weapon in the backpack.";
            }

            int guard = 0;
            while (combat.CurrentState == CombatState.PlayerTurn && player.CurrentAP >= weapon.Combat.APCost && guard++ < 8) {
                FighterEntity enemy = combat.EnemyFaction.Fighters.Find(f => f.RuntimeHP > 0);
                if (enemy == null) {
                    break;
                }
                ItemUseService.TryUseItem(weapon, out reason);
                ItemUseService.TryConfirmPendingTarget(enemy, out reason);
            }

            if (combat.CurrentState != CombatState.PlayerTurn) {
                break;
            }

            foreach (ItemEntity item in new List<ItemEntity>(grid.ContainedItems)) {
                if (item.ItemType == nameof(ItemType.Armor) && player.CurrentAP >= 1) {
                    ItemUseService.TryUseItem(item, out reason);
                }
            }

            combat.EndPlayerTurn();
        }

        FighterEntity doll = combat.PlayerFaction?.Fighters != null && combat.PlayerFaction.Fighters.Count > 0 ? combat.PlayerFaction.Fighters[0] : null;
        return $"AutoCombat: {turns} turn(s), state {combat.CurrentState}, HP {doll?.RuntimeHP}.";
    }

    private static string PlaceLoot() {
        BackpackGrid grid = GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid as BackpackGrid;
        InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("DevTools");
        StringBuilder log = new StringBuilder();
        foreach (DraggableItemUI ui in Object.FindObjectsOfType<DraggableItemUI>()) {
            ItemEntity item = ui.ItemData;
            if (item == null || grid.ContainedItems.Contains(item)) {
                continue;
            }

            bool placed = false;
            string reason;
            for (int y = 0; y < grid.Height && !placed; y++) {
                for (int x = 0; x < grid.Width && !placed; x++) {
                    foreach (int rotation in new[] { 0, 90 }) {
                        if (!placed && InventoryInteractionService.RequestPlace(item, x, y, rotation, context, out reason)) {
                            placed = true;
                            log.Append($"Loot {item.Name} -> ({x},{y}). ");
                        }
                    }
                }
            }

            if (!placed) {
                log.Append($"Loot {item.Name} does not fit, left behind. ");
            }
        }

        ClickByLabel("确认拾取并继续", log);
        return log.ToString().TrimEnd();
    }

    private static bool EnterNextMapNode(StringBuilder log) {
        Button fallback = null;
        foreach (Button button in ClickableButtons()) {
            if (button.transform.parent == null || button.transform.parent.name != MapLayoutName) {
                continue;
            }

            if (!ButtonLabel(button).Contains("首领")) {
                log.AppendLine($"Enter node '{ButtonLabel(button)}'.");
                button.onClick.Invoke();
                return true;
            }

            fallback = button;
        }

        if (fallback != null) {
            log.AppendLine($"Enter node '{ButtonLabel(fallback)}'.");
            fallback.onClick.Invoke();
            return true;
        }

        log.AppendLine("Stopped: no enterable map node.");
        return false;
    }

    private static bool ClickFirstContinue(StringBuilder log) {
        foreach (string label in ContinueLabels) {
            if (ClickByLabel(label, null)) {
                log.AppendLine($"Clicked '{label}'.");
                return true;
            }
        }

        return false;
    }

    private static bool ClickByLabel(string label, StringBuilder log) {
        foreach (Button button in ClickableButtons()) {
            if (ButtonLabel(button).Contains(label)) {
                button.onClick.Invoke();
                log?.AppendLine($"Clicked '{ButtonLabel(button)}'.");
                return true;
            }
        }

        log?.AppendLine($"No clickable button contains '{label}'.");
        return false;
    }

    private static string StateSignature() {
        StringBuilder sb = new StringBuilder();
        Transform canvas = FindCanvas();
        if (canvas != null) {
            foreach (Transform child in canvas) {
                if (child.gameObject.activeSelf) {
                    sb.Append(child.name).Append('|');
                }
            }
        }

        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        BackpackGrid grid = player?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        sb.Append(grid != null ? grid.ContainedItems.Count : -1).Append('|');
        sb.Append(player != null ? player.StashInventory.Count : -1).Append('|');
        sb.Append(GameRoot.Core?.Dungeon?.CurrentLayer?.CurrentNode?.NodeID).Append('|');
        sb.Append(GameRoot.Core?.Combat?.CurrentState);
        return sb.ToString();
    }

    private static bool StartRun() {
        PlayerProfile player = GameRoot.Core.CurrentPlayer;
        int layerID = Mathf.Max(1, player.LastSelectedDungeonStartLayer);
        return GameRoot.Core.Dungeon.StartRunAtLayer(layerID);
    }

    private static T FindFirst<T>(NodeBase root) where T : NodeBase {
        Queue<NodeBase> queue = new Queue<NodeBase>();
        HashSet<NodeBase> seen = new HashSet<NodeBase>();
        if (root != null) {
            queue.Enqueue(root);
        }

        while (queue.Count > 0) {
            NodeBase node = queue.Dequeue();
            if (!seen.Add(node)) {
                continue;
            }

            if (node is T match) {
                return match;
            }

            foreach (NodeBase next in node.NextNodes) {
                queue.Enqueue(next);
            }
        }

        return null;
    }

    // Buttons a player could click right now: while the prologue overlay blocks input only its buttons count,
    // otherwise the overlay's buttons are ignored.
    private static List<Button> ClickableButtons() {
        List<Button> result = new List<Button>();
        P3DialogueOverlayController overlay = FindOverlay();
        bool overlayBlocks = overlay != null && overlay.IsShowing && overlay.IsInputBlocking;
        foreach (Button button in Object.FindObjectsOfType<Button>()) {
            if (!button.gameObject.activeInHierarchy || !button.interactable || P3DevPanel.IsDevPanelObject(button.transform)) {
                continue;
            }

            bool inOverlay = overlay != null && button.transform.IsChildOf(overlay.transform);
            if (overlayBlocks != inOverlay || string.IsNullOrEmpty(ButtonLabel(button))) {
                continue;
            }

            result.Add(button);
        }

        return result;
    }

    private static string ButtonLabel(Button button) {
        Text text = button.GetComponentInChildren<Text>();
        return text != null ? text.text.Replace("\n", " ") : string.Empty;
    }

    private static bool IsPanelActive(string panelName) {
        Transform canvas = FindCanvas();
        Transform panel = canvas != null ? canvas.Find(panelName) : null;
        return panel != null && panel.gameObject.activeSelf;
    }

    private static Transform FindCanvas() {
        GameObject canvas = GameObject.Find(CanvasName);
        return canvas != null ? canvas.transform : null;
    }

    private static P3DialogueOverlayController FindOverlay() {
        Transform canvas = FindCanvas();
        Transform overlay = canvas != null ? canvas.Find(OverlayName) : null;
        return overlay != null ? overlay.GetComponent<P3DialogueOverlayController>() : null;
    }

    private static string ReadChildText(Transform root, string path) {
        Transform child = root.Find(path);
        Text text = child != null ? child.GetComponent<Text>() : null;
        return text != null ? text.text.Replace("\n", " ") : string.Empty;
    }
}
