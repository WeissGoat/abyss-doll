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

        List<string> panels = ActivePanels(canvas);
        sb.AppendLine("Screen: " + string.Join(", ", panels.ToArray()));
        HomeRoomNavigator rooms = Object.FindObjectOfType<WorkshopUIController>()?.Rooms;
        if (rooms != null && rooms.isActiveAndEnabled) {
            sb.AppendLine("Room: " + HomeRoomNavigator.RoomLabels[(int)rooms.Current] + (rooms.Current == HomeRoom.Studio ? " / " + StudioBenchView.TabLabels[(int)rooms.Bench.CurrentTab] : string.Empty)
                + (rooms.Journal.IsShowing ? (rooms.Journal.IsArrival ? ", journal page (arrival)" : ", wall journal") : string.Empty)
                + (rooms.NightTalk.IsShowing ? ", night talk" : string.Empty)
                + (rooms.Unpack.IsShowing ? ", unpacking" : string.Empty));
            PlayerProfile home = GameRoot.Core.CurrentPlayer;
            if (home.IsNight || rooms.NightTalk.IsShowing) {
                sb.AppendLine($"Tonight: next {HomeEveningStamps.Labels[(int)(rooms.NightTalk.IsShowing ? EveningStamp.Goodnight : HomeEveningService.Next(home))]}, haul {HomeEveningService.Haul(home).Count} undecided");
            }

            if (rooms.Journal.HasPortrait) {
                sb.AppendLine($"Journal: Zero up close, face {rooms.Journal.CurrentFace}");
            }

            if (rooms.NightTalk.IsShowing) {
                sb.AppendLine($"Night talk: {rooms.NightTalk.CurrentLine}{(rooms.NightTalk.HasReplies ? " (two replies waiting)" : string.Empty)}");
            }

            if (!string.IsNullOrEmpty(home.ActivePromiseID)) {
                sb.AppendLine("Promise: " + PromiseService.MapLine(home));
            }

            ZeroPresence speaking = rooms.Zero(rooms.Current);
            if (speaking != null && speaking.IsSpeaking) {
                sb.AppendLine("Zero says: " + speaking.CurrentLine + (speaking.IsCloseUp ? $" (up close, face {speaking.CurrentFace})" : string.Empty));
            }

            DollEntity zero = GameRoot.Core.CurrentPlayer.ActiveDoll;
            sb.AppendLine($"Zero: grime {CareService.Count(zero, CareMarkKind.Grime)}, cracks {CareService.Count(zero, CareMarkKind.Crack)}, haze {CareService.Count(zero, CareMarkKind.Haze)}, bond {BondService.Level(zero)} ({BondService.Points(zero)}/{BondService.Needed(zero)}), "
                + $"wish {WishService.Active(GameRoot.Core.CurrentPlayer)?.Text ?? "none"}, shelf {GameRoot.Core.CurrentPlayer.Shelf?.Count ?? 0}");
        }

        P3DialogueOverlayController overlay = FindOverlay();
        if (overlay != null && overlay.IsShowing) {
            string speaker = ReadChildText(overlay.transform, "DialoguePanel_Image/Speaker_Text");
            string body = ReadChildText(overlay.transform, "DialoguePanel_Image/Body_Text");
            sb.AppendLine($"Prologue: showing, blocking={overlay.IsInputBlocking}, line='{speaker}: {body}'");
        }

        if (DemoEndingComic.IsPlaying) {
            sb.AppendLine("Ending comic: " + overlay?.CurrentText);
        }

        if (DemoEndCard.IsShowing) {
            sb.AppendLine($"End card: {DemoEndCard.TitleText}");
        }

        if (ScenePanel.IsShowing) {
            Transform scene = ScenePanel.Active.transform;
            string face = ScenePanel.Active.CurrentFace;
            sb.AppendLine($"Scene: '{ReadChildText(scene, "Dialog/Speaker_Text")}: {ReadChildText(scene, "Dialog/Line_Text")}'"
                + (string.IsNullOrEmpty(face) ? string.Empty : $" (Zero on stage, face {face})"));
        }

        PlayerProfile player = GameRoot.Core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;
        DayCalendarInfo calendar = DayCycleService.GetCalendar(player);
        string rent = calendar.HasPendingRent ? $"rent {calendar.PendingRentAmount} PENDING" : $"rent {calendar.NextRent} in {calendar.RentCountdown}d";
        string debt = calendar.DebtAmount > 0 ? $", debt {calendar.DebtAmount}" : string.Empty;
        sb.AppendLine($"Day {player.CurrentDay} (week {calendar.Week} {calendar.WeekDayName}, {rent}{debt}), money {player.Money}, HP {doll?.Status.HP_Current}/{doll?.Status.HP_Max}, SAN {doll?.Status.SAN_Current}/{doll?.Status.SAN_Max}");

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
                    string intent = card.SelectedIntent != null ? (card.SelectedIntent.ShortText ?? card.SelectedIntent.Title) : "?";
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

    // Visible text that still shows English words or raw config IDs ("Ready to dive.", chassis_lv1_basic). M4 checks
    // each screen with it while reskinning; HP / SAN / AP and the like are allowed.
    public static string EnglishText() {
        System.Text.RegularExpressions.Regex word = new System.Text.RegularExpressions.Regex("[A-Za-z]{3,}");
        HashSet<string> allowed = new HashSet<string> { "SAN", "Boss", "MAX" };
        List<string> hits = new List<string>();
        foreach (Text text in Object.FindObjectsOfType<Text>()) {
            if (!text.enabled || string.IsNullOrEmpty(text.text)) {
                continue;
            }

            foreach (System.Text.RegularExpressions.Match match in word.Matches(text.text)) {
                if (!allowed.Contains(match.Value)) {
                    hits.Add($"{PathOf(text.transform)}: {text.text.Replace("\n", " / ")}");
                    break;
                }
            }
        }

        return hits.Count == 0 ? "No English text on screen." : $"{hits.Count} text(s) with English:\n" + string.Join("\n", hits.ToArray());
    }

    private static string PathOf(Transform transform) {
        string path = transform.name;
        for (Transform parent = transform.parent; parent != null && parent.parent != null; parent = parent.parent) {
            path = parent.name + "/" + path;
        }

        return path;
    }

    // Items and clickable buttons that something else is drawn on top of (UIOcclusion, no screenshot). Buttons behind
    // an open modal are expected; an item under the backpack or a button under a panel is a bug.
    public static string Occlusion() {
        List<string> lines = new List<string>();
        foreach (DraggableItemUI item in Object.FindObjectsOfType<DraggableItemUI>()) {
            AddCovered(lines, (RectTransform)item.transform, "item " + item.ItemData?.Name);
        }

        foreach (Button button in ClickableButtons()) {
            AddCovered(lines, (RectTransform)button.transform, $"button '{ButtonLabel(button)}'");
        }

        return lines.Count == 0 ? "Nothing covered." : $"Covered ({lines.Count}):\n" + string.Join("\n", lines.ToArray());
    }

    // Picks up an item as the player would (loot first, or the item whose name contains itemName), holds it over the
    // middle of the backpack, reports what is drawn on top of it, then puts it back where it was.
    public static string DragCheck(string itemName = null) {
        DraggableItemUI picked = null;
        BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        foreach (DraggableItemUI item in Object.FindObjectsOfType<DraggableItemUI>()) {
            if (!item.isActiveAndEnabled || item.ItemData == null || (itemName != null && !item.ItemData.Name.Contains(itemName))) {
                continue;
            }

            bool loose = grid == null || !grid.ContainedItems.Contains(item.ItemData);
            if (picked == null || (loose && grid.ContainedItems.Contains(picked.ItemData))) {
                picked = item;
            }
        }

        GridGenerator generator = Object.FindObjectOfType<GridGenerator>();
        RectTransform gridRect = generator != null ? generator.gridParent as RectTransform : null;
        if (picked == null || gridRect == null || !gridRect.gameObject.activeInHierarchy) {
            return "DragCheck: no item on screen or the backpack is hidden.\n" + Describe();
        }

        RectTransform rect = (RectTransform)picked.transform;
        Vector2 start = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        Vector2 over = RectTransformUtility.WorldToScreenPoint(null, gridRect.TransformPoint(gridRect.rect.center));
        UnityEngine.EventSystems.PointerEventData data = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) {
            button = UnityEngine.EventSystems.PointerEventData.InputButton.Left,
            position = start,
            pressPosition = start,
            pointerDrag = picked.gameObject
        };

        picked.OnBeginDrag(data);
        data.position = over;
        picked.OnDrag(data);
        List<Graphic> covering = UIOcclusion.CoveringGraphics(rect, over);
        picked.UpdatePlacementPreview(over);
        string preview = DescribeDropPreview(picked);
        picked.ReturnToOriginalPosition();
        picked.OnEndDrag(data);

        string name = picked.ItemData.Name;
        if (covering.Count == 0) {
            return $"DragCheck: held {name} over the backpack; nothing drawn on top of it. {preview}";
        }

        List<string> names = new List<string>();
        foreach (Graphic graphic in covering) {
            names.Add(UIOcclusion.PathOf(graphic.transform));
        }

        return $"DragCheck: held {name} over the backpack is COVERED by {string.Join(", ", names.ToArray())}.";
    }

    // "Drop preview: fits, 2 cell(s) green." / "does not fit, 2 cell(s) red." / "none."
    private static string DescribeDropPreview(DraggableItemUI held) {
        if (held.PreviewSlot == null) {
            return "Drop preview: none.";
        }

        int green = 0;
        int red = 0;
        foreach (GridSlotUI slot in Object.FindObjectsOfType<GridSlotUI>()) {
            if (slot.IsShowingPreviewSkin(true)) {
                green++;
            } else if (slot.IsShowingPreviewSkin(false)) {
                red++;
            }
        }

        if (green == 0 && red == 0) {
            return "Drop preview: none.";
        }

        return green > 0 ? $"Drop preview: fits, {green} cell(s) green." : $"Drop preview: does not fit, {red} cell(s) red.";
    }

    private static void AddCovered(List<string> lines, RectTransform target, string label) {
        if (target == null || !target.gameObject.activeInHierarchy) {
            return;
        }

        Graphic graphic = target.GetComponent<Graphic>();
        if (graphic == null || !UIOcclusion.IsVisible(graphic)) {
            return;
        }

        List<Graphic> covering = UIOcclusion.CoveringGraphics(target);
        if (covering.Count > 0) {
            lines.Add($"{label} ({UIOcclusion.PathOf(target)}) under {UIOcclusion.PathOf(covering[covering.Count - 1].transform)}");
        }
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

    // Editor only: whether the next Play Mode starts with the prologue. Builds always play it.
    public static string SetPrologueOnStartup(bool play) {
        P3DevSettings.PlayPrologueOnStartup = play;
        return $"Prologue on startup: {(P3DevSettings.PlayPrologueOnStartup ? "on" : "off")} (takes effect next time Play Mode starts).";
    }

    public static string TogglePrologueOnStartup() {
        return SetPrologueOnStartup(!P3DevSettings.PlayPrologueOnStartup);
    }

    // Records the game's own frames for a few seconds (returns at once; see P3FrameRecorder). Default folder: P3Rec/
    // next to the executable.
    public static string Record(string folder = null, float seconds = 10f, int fps = 24) {
        if (string.IsNullOrEmpty(folder)) {
            folder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Application.dataPath) ?? ".", "P3Rec");
        }

        return P3FrameRecorder.Begin(folder, seconds, fps);
    }

    // Debug fight against the given monsters, comma-separated (e.g. Fight("boss_gatekeeper_mk1") for the boss entrance
    // clip, Fight("mob_rust_hound,mob_rust_hound")).
    public static string Fight(string monsters) {
        string[] monsterIDs = (monsters ?? string.Empty).Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < monsterIDs.Length; i++) {
            monsterIDs[i] = monsterIDs[i].Trim();
        }

        GameFlowController flow = GameFlowController.Instance;
        if (flow == null || GameRoot.Core == null) {
            return "Game is not running.";
        }

        if (monsterIDs.Length == 0) {
            return "Fight needs at least one MonsterID.";
        }

        foreach (string id in monsterIDs) {
            if (!ConfigManager.Monsters.ContainsKey(id)) {
                return $"Unknown monster {id}.";
            }
        }

        if (GameRoot.Core.Dungeon.CurrentLayer == null && !StartRun()) {
            return "Could not start a run.\n" + Describe();
        }

        GameRoot.Core.Combat.StartCombat(new List<string>(monsterIDs));
        flow.EnterCombat();
        return Describe();
    }

    // Debug route to a screen: workshop (= hall), studio, attic, sell, prosthetic, layers, loadout, map, combat, saferoom, stairs.
    public static string GoTo(string screen) {
        GameFlowController flow = GameFlowController.Instance;
        if (flow == null || GameRoot.Core == null) {
            return "Game is not running.";
        }

        WorkshopUIController workshop = Object.FindObjectOfType<WorkshopUIController>(true);
        switch ((screen ?? string.Empty).ToLowerInvariant()) {
            case "workshop":
            case "hall":
                flow.EnterWorkshop();
                break;
            case "studio":
                flow.EnterWorkshop();
                workshop?.Rooms?.Show(HomeRoom.Studio);
                break;
            case "attic":
                flow.EnterWorkshop();
                workshop?.Rooms?.Show(HomeRoom.Attic);
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
            case "loadout":
                flow.EnterPreDiveLoadout(Mathf.Max(1, GameRoot.Core.CurrentPlayer.LastSelectedDungeonStartLayer));
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
    // stopAtCombat: stop at the start of a player turn (reports "Combat ready.") so the caller can look at it.
    public static string RunDive(int maxSteps = 40, bool stopAtCombat = false) {
        StringBuilder log = new StringBuilder();
        if (FinishArrival(log)) {
            return log.ToString() + Describe();
        }

        if (IsPanelActive("WorkshopPanel") && !IsPanelActive("DungeonMapPanel")) {
            // Today's choice is in the hall; walk there from the studio or the attic like a player.
            HomeRoomNavigator rooms = Object.FindObjectOfType<WorkshopUIController>()?.Rooms;
            if (rooms != null && rooms.Current != HomeRoom.Hall) {
                ClickByLabel(HomeRoomNavigator.RoomLabels[(int)HomeRoom.Hall], log);
            }
            PrepareDay(log);
            if (!ClickByLabel(TownDayHUD.DiveLabel, null)) {
                ClickByLabel("浅层入口", log);
            } else {
                log.AppendLine($"Clicked '{TownDayHUD.DiveLabel}'.");
            }
            ClickByLabel("开始下潜", log);
        }

        if (IsPanelActive(PreDiveLoadoutUIController.PanelName)) {
            ClickByLabel("带上上次的装备", log);
            ClickByLabel("出发", log);
            if (IsPanelActive(PreDiveLoadoutUIController.PanelName)) {
                ClickByLabel("仍然出发", log);
            }
        }

        string lastSignature = null;
        for (int step = 0; step < maxSteps; step++) {
            if (ScenePanel.IsShowing) {
                log.AppendLine("A scene is showing; PlayPrologue() plays it.");
                break;
            }

            // Some screens (stairs, settlement) only react a frame later; one call runs in a single frame, so stop
            // when nothing changed and let the caller call again.
            string signature = StateSignature();
            if (signature == lastSignature) {
                log.AppendLine("Waiting for the game to update; call RunDive() again to continue.");
                break;
            }
            lastSignature = signature;

            CombatSystem combat = GameRoot.Core?.Combat;
            if (IsPanelActive("CombatPanel") && combat != null && combat.CurrentState == CombatState.End && !IsPanelActive("CombatLootPanel_Runtime")) {
                // Combat resolves instantly; the result screen appears after the queued animations play out.
                log.AppendLine($"Combat over; {VisualQueue.Count} animation(s) still playing. Call RunDive() again in a few seconds.");
                break;
            } else if (IsPanelActive("CombatLootPanel_Runtime")) {
                log.AppendLine(PlaceLoot());
            } else if (stopAtCombat && IsPanelActive("CombatPanel") && combat != null && combat.CurrentState == CombatState.PlayerTurn) {
                log.AppendLine("Combat ready.");
                break;
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
            } else if (IsPanelActive("WorkshopPanel")) {
                // Home: the journal page is open in the hall.
                if (!FinishArrival(log)) {
                    log.AppendLine("Back in the workshop.");
                }
                break;
            } else if (DemoEndingComic.IsPlaying) {
                log.AppendLine("Ending comic: " + FindOverlay()?.CurrentText);
                DemoEndingComic.Current.Advance();
            } else if (IsPanelActive("SafeRoomPanel")) {
                if (EasyFights && ClickByLabel("休整", log)) {
                    continue;
                }

                if (!ClickByLabel("返回小镇", log)) {
                    break;
                }
            } else if (IsPanelActive("DungeonMapPanel")) {
                if (!EnterNextMapNode(log)) {
                    break;
                }
            } else if (ActivePanels(FindCanvas()).Count == 0) {
                // Between screens (a node result still queued behind animations): nothing to click yet.
                log.AppendLine("Waiting for the game to update; call RunDive() again to continue.");
                break;
            } else if (!ClickFirstContinue(log)) {
                log.AppendLine("Stopped: unrecognised screen.");
                break;
            }
        }

        return log.ToString() + Describe();
    }

    // Plays the prologue like a player from wherever it is — the title, the dialogue and its ritual buttons, the
    // half-open workshop, the first-dive permit, the shallow edge (map, scenes, the fight) and the return home — until
    // the normal town opens ("Prologue finished."). The labels pick the scene choices: listen to the voice or not,
    // what to bring back, care for her or read the notice. Call again while it reports waiting.
    public static string PlayPrologue(int maxSteps = 40, string voiceChoice = "让她先听清楚", string bringBack = "记忆噪声片", string returnChoice = "先照看她") {
        StringBuilder log = new StringBuilder();
        string lastSignature = null;
        for (int step = 0; step < maxSteps; step++) {
            if (PrologueShallowRun.Current != null && PrologueShallowRun.Current.ReturnHomeDone && !ScenePanel.IsShowing) {
                log.AppendLine("Prologue finished.");
                break;
            }

            string signature = StateSignature() + StorySignature();
            if (signature == lastSignature) {
                log.AppendLine("Waiting for the game to update; call PlayPrologue() again to continue.");
                break;
            }
            lastSignature = signature;

            if (!StoryStep(log, voiceChoice, bringBack, returnChoice)) {
                // Only the dungeon screens are RunDive's; in the workshop it would start a normal dive.
                if (IsPanelActive("WorkshopPanel") && !IsPanelActive("DungeonMapPanel")) {
                    log.AppendLine("Waiting for the game to update; call PlayPrologue() again to continue.");
                    break;
                }

                string dive = RunDive(1);
                log.AppendLine(dive.Split('\n')[0].Trim());
                if (dive.Contains("Stopped:")) {
                    break;
                }
            }
        }

        return log.ToString() + Describe();
    }

    private static string StorySignature() {
        P3DialogueOverlayController overlay = FindOverlay();
        string line = overlay != null && overlay.IsShowing ? ReadChildText(overlay.transform, "DialoguePanel_Image/Body_Text") + overlay.HasActionVisible : string.Empty;
        string scene = ScenePanel.IsShowing ? ReadChildText(ScenePanel.Active.transform, "Dialog/Line_Text") + ReadChildText(ScenePanel.Active.transform, "Dialog/Title_Text") : string.Empty;
        return $"|{TitleScreen.IsShowing}|{line}|{scene}";
    }

    // One prologue step outside the dungeon screens; false when it is the dungeon's turn (map, combat).
    private static bool StoryStep(StringBuilder log, params string[] preferredChoices) {
        if (TitleScreen.IsShowing) {
            return ClickByLabel(TitleScreen.NewGameLabel, log);
        }

        if (ScenePanel.IsShowing) {
            Transform scene = ScenePanel.Active.transform;
            foreach (string choice in preferredChoices) {
                if (!string.IsNullOrEmpty(choice) && ClickWithin(scene, choice, log)) {
                    return true;
                }
            }

            // No preferred choice on screen: the first choice, or the next line.
            return ClickWithin(scene, "SceneChoice_0", log, byName: true)
                || ClickWithin(scene, ScenePanel.ContinueLabel, log)
                || ClickWithin(scene, ScenePanel.DoneLabel, log);
        }

        P3DialogueOverlayController overlay = FindOverlay();
        if (overlay != null && overlay.IsShowing && overlay.IsInputBlocking) {
            if (overlay.HasActionVisible) {
                return ClickByLabel("启动零号", log) || ClickByLabel("擦去核心仓灰尘", log) || ClickFirstContinue(log);
            }

            return overlay.HasContinueVisible && (ClickByLabel("▶", log) || ClickFirstContinue(log));
        }

        WorkshopUIController workshop = Object.FindObjectOfType<WorkshopUIController>();
        if (workshop != null && workshop.IsPrologueHalfOpen && IsPanelActive("WorkshopPanel")) {
            if (workshop.IsDungeonStartLayerPanelOpen) {
                return ClickByLabel("出发", log);
            }

            return ClickByLabel("浅层入口", log);
        }

        return false;
    }

    private static bool ClickWithin(Transform root, string label, StringBuilder log, bool byName = false) {
        foreach (Button button in root.GetComponentsInChildren<Button>(false)) {
            if (!button.interactable) {
                continue;
            }

            string text = ButtonLabel(button);
            if (byName ? button.name == label : text.Contains(label)) {
                button.onClick.Invoke();
                log?.AppendLine($"Clicked '{text.Split('\n')[0]}'.");
                return true;
            }
        }

        return false;
    }

    // Zero is home and the dive's journal page is open: read it and put it away ("Dive finished.").
    private static bool FinishArrival(StringBuilder log) {
        HomeRoomNavigator rooms = Object.FindObjectOfType<WorkshopUIController>()?.Rooms;
        if (rooms == null || rooms.Journal == null || !rooms.Journal.IsShowing || !rooms.Journal.IsArrival) {
            return false;
        }

        ClickByLabel(HomeJournalView.CloseLabel, log);
        log.AppendLine("Dive finished.");
        return true;
    }

    // Closes the workshop's night dialogs like a player: the night card, then the rent (pay when the money covers
    // it, otherwise owe it).
    public static string ResolveDayDialogs() {
        StringBuilder log = new StringBuilder();
        ResolveDayDialogs(log);
        return log.ToString() + Describe();
    }

    private static void ResolveDayDialogs(StringBuilder log) {
        for (int i = 0; i < 8; i++) {
            if (ClickByLabel(NightBusinessPanel.OpenLabel, null)) {
                log.AppendLine("Opened the night business.");
            } else if (ClickByLabel(HomeUnpackView.BusinessLabel, null)) {
                log.AppendLine("Left the unpacking for the business.");
            } else if (ClickByLabel(TownDayHUD.GoBusinessLabel, null)) {
                log.AppendLine("Went to the business screen.");
            } else if (DemoEndCard.IsShowing) {
                log.AppendLine($"The end card is showing: {DemoEndCard.TitleText}.");
                return;
            } else if (ClickByLabel("迎接新的一天", null)) {
                log.AppendLine("Closed the night card.");
            } else if (ReplyAtNight(0, log)) {
                continue;
            } else if (ClickByLabel(HomeNightTalkView.GoodnightLabel, null)) {
                log.AppendLine("Said goodnight.");
            } else if (ClickByLabel("交房租", null)) {
                log.AppendLine("Paid the rent.");
            } else if (ClickByLabel("先欠着", null)) {
                log.AppendLine("Owed the rent.");
            } else {
                return;
            }
        }
    }

    // In the night talk, picks reply 0 or 1 when the night has replies.
    public static string Reply(int index) {
        StringBuilder log = new StringBuilder();
        if (!ReplyAtNight(index, log)) {
            log.AppendLine("No replies waiting.");
        }

        return log.ToString() + Describe();
    }

    private static bool ReplyAtNight(int index, StringBuilder log) {
        HomeNightTalkView talk = Object.FindObjectOfType<WorkshopUIController>()?.Rooms?.NightTalk;
        if (talk == null || !talk.IsShowing || !talk.HasReplies) {
            return false;
        }

        talk.Reply(index);
        log?.AppendLine("Replied at night: " + talk.CurrentLine + (string.IsNullOrEmpty(talk.CurrentFace) ? " (no portrait)" : $" (face {talk.CurrentFace})"));
        return true;
    }

    // The route to the end of the Demo, not its numbers: layer 3 open and chosen, and every fight from here on starts
    // with the enemies at 1 HP (RunDive also rests at safe rooms instead of leaving, so it reaches the boss).
    public static bool EasyFights;

    public static string PrepareDemoEnding() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        if (player == null) {
            return "Game is not running.";
        }

        player.HighestUnlockedDungeonLayer = Mathf.Max(player.HighestUnlockedDungeonLayer, 3);
        player.LastSelectedDungeonStartLayer = 3;
        EasyFights = true;
        return "Layer 3 open and chosen; fights are easy until EasyFights is cleared.\n" + Describe();
    }

    // Clicks 和她说话 in the hall or the attic (goes to the hall from the studio).
    public static string Talk() {
        HomeRoomNavigator rooms = Object.FindObjectOfType<WorkshopUIController>()?.Rooms;
        if (rooms == null) {
            return "Not at home.\n" + Describe();
        }

        if (rooms.Current == HomeRoom.Studio) {
            rooms.Show(HomeRoom.Hall);
        }

        StringBuilder log = new StringBuilder();
        ClickByLabel(HomeRoomNavigator.TalkLabel, log);
        return log.ToString() + Describe();
    }

    // Clicks Zero's head, shoulder or hand in the current room ("head", "shoulder", "hand").
    public static string Touch(string region) {
        string name = "Touch_" + char.ToUpperInvariant(region[0]) + region.Substring(1).ToLowerInvariant();
        foreach (Button button in Object.FindObjectsOfType<Button>()) {
            if (button.name == name && button.isActiveAndEnabled) {
                button.onClick.Invoke();
                return $"Touched her {region}.\n" + Describe();
            }
        }

        return $"No {name} on screen.\n" + Describe();
    }

    // At night: open the counter and sort what the dive brought home like a player would (sell what sells, keep the
    // rest), then leave for the business.
    public static string Unpack() {
        StringBuilder log = new StringBuilder();
        HomeRoomNavigator rooms = Object.FindObjectOfType<WorkshopUIController>()?.Rooms;
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        if (rooms == null || player == null || !player.IsNight) {
            return "Not at home at night.\n" + Describe();
        }

        int haul = HomeEveningService.Haul(player).Count;
        if (!rooms.Unpack.IsShowing) {
            rooms.OpenCounter();
        }

        if (!rooms.Unpack.IsShowing) {
            return $"Nothing to unpack (haul {haul}).\n" + Describe();
        }

        for (int i = 0; i < 40 && HomeEveningService.Haul(player).Count > 0; i++) {
            if (!ClickByLabel(HomeUnpackView.ChoiceLabels[(int)HaulChoice.Sell], null) && !ClickByLabel(HomeUnpackView.ChoiceLabels[(int)HaulChoice.Keep], null)) {
                break;
            }
        }

        log.AppendLine($"Unpacked {haul - HomeEveningService.Haul(player).Count} of {haul}: showcase {player.Showcase.Count}, dump box {player.DumpBox.Count}.");
        ClickByLabel(HomeUnpackView.BusinessLabel, log);
        return log.ToString() + Describe();
    }

    // At night: dump every loot item into the dump box (the business screen must be open), as a player in a hurry would.
    public static string DumpAllForBusiness() {
        if (GameRoot.Core?.CurrentPlayer?.IsNight == true) {
            ClickByLabel(TownDayHUD.GoBusinessLabel, null);
        }

        bool clicked = ClickByLabel(NightBusinessPanel.DumpAllLabel, null);
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        return $"{(clicked ? "Dumped loot" : "No business screen")}: dump box {player?.DumpBox.Count ?? 0}, showcase {player?.Showcase.Count ?? 0}.\n" + Describe();
    }

    // Before a dive: close the night dialogs, and rest first when Zero cannot dive (HP 0).
    private static void PrepareDay(StringBuilder log) {
        ResolveDayDialogs(log);
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        if (player != null && !DayCycleService.CanDive(player, out string reason) && DayCycleService.CanStartDay(player, out _)) {
            ClickByLabel(TownDayHUD.RestLabel, log);
            ClickByLabel("休息一天", log);
            log.AppendLine("Rested: " + reason);
            ResolveDayDialogs(log);
        }
    }

    // In the studio, rubs every smudge off Zero like a player would with the mouse.
    public static string Care() {
        WorkshopUIController workshop = Object.FindObjectOfType<WorkshopUIController>();
        workshop?.Rooms?.Show(HomeRoom.Studio);
        DollEntity doll = GameRoot.Core?.CurrentPlayer?.ActiveDoll;
        int before = CareService.Count(doll, CareMarkKind.Grime);
        for (int i = 0; i < CareService.MaxGrime * 2 && CareService.Count(doll, CareMarkKind.Grime) > 0; i++) {
            GrimeSpot spot = Object.FindObjectOfType<GrimeSpot>();
            if (spot == null) {
                break;
            }

            spot.RubAway();
            Object.DestroyImmediate(spot);
        }

        int wiped = before - CareService.Count(doll, CareMarkKind.Grime);

        return $"Wiped {wiped} smudge(s).\n" + Describe();
    }

    // Debug: puts smudges on Zero (0 to CareService.MaxGrime) and opens the studio to see them on her.
    public static string Soil(int grime = CareService.MaxGrime) {
        DollEntity doll = GameRoot.Core?.CurrentPlayer?.ActiveDoll;
        if (doll?.Status == null) {
            return "No Zero.\n" + Describe();
        }

        doll.Status.Grime = Mathf.Clamp(grime, 0, CareService.MaxGrime);
        HomeRoomNavigator rooms = Object.FindObjectOfType<WorkshopUIController>()?.Rooms;
        rooms?.Show(HomeRoom.Studio);
        rooms?.Care?.Refresh();
        return $"Zero has {doll.Status.Grime} smudge(s).\n" + Describe();
    }

    // Pats Zero's head in the room on screen like a hand would: press, two strokes across, let go (her puppet
    // portrait dips and leans), then it counts as a touch on her head. Returns at once; the pat takes about 2 s.
    public static string Pat() {
        return PlayTouch(TouchRegion.Head, "Patting her head.");
    }

    // Holds Zero's hand in the hall or attic for about 2 s (her hands rise); counts as a touch on her hand.
    public static string HoldHand() {
        return PlayTouch(TouchRegion.Hand, "Holding her hand.");
    }

    // On the Zero of the room on screen (the hall's lives on the workshop screen, outside the room layers).
    private static string PlayTouch(TouchRegion region, string doing) {
        HomeRoomNavigator rooms = Object.FindObjectOfType<WorkshopUIController>()?.Rooms;
        string room = rooms == null ? null : rooms.Current == HomeRoom.Studio ? "StudioRoom" : rooms.Current == HomeRoom.Attic ? "AtticRoom" : string.Empty;
        foreach (PortraitPatZone zone in Object.FindObjectsOfType<PortraitPatZone>()) {
            string zoneRoom = zone.transform.parent?.parent?.parent?.name;
            bool inRoom = room == null || (room.Length == 0 ? zoneRoom != "StudioRoom" && zoneRoom != "AtticRoom" : zoneRoom == room);
            if (zone.isActiveAndEnabled && zone.Region == region && inRoom && zone.Puppet != null) {
                zone.StartCoroutine(zone.Play());
                return doing;
            }
        }

        return $"No {region} to touch on screen.\n" + Describe();
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

    // Shows a CG (or a prologue comic page, e.g. vfx_no0_core_start) in the dialogue overlay, the way a story line
    // does; HideCG closes it. For checking art and CG motion clips on screen.
    public static string ShowCG(string visualID) {
        P3DialogueOverlayController overlay = UnityEngine.Object.FindObjectOfType<P3DialogueOverlayController>(true);
        if (overlay == null) {
            foreach (Canvas canvas in UnityEngine.Object.FindObjectsOfType<Canvas>()) {
                if (canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay) {
                    overlay = P3DialogueOverlayController.CreateUnder(canvas);
                    break;
                }
            }
        }

        if (overlay == null) {
            return "No dialogue overlay and no screen canvas.";
        }

        overlay.PresentLine(new NarrativeOverlayPayload {
            SpeakerName = "DevTools",
            Text = visualID,
            VisualRequest = new NarrativeVisualRequest { VisualID = visualID }
        });
        return $"Showing {visualID}.\n" + MotionCG();
    }

    public static string HideCG() {
        P3DialogueOverlayController overlay = UnityEngine.Object.FindObjectOfType<P3DialogueOverlayController>(true);
        if (overlay != null) {
            overlay.Hide();
        }

        return "CG hidden.";
    }

    // Every CG motion clip on screen: which clip, the frame it is on, whether it shows and has finished.
    public static string MotionCG() {
        StringBuilder text = new StringBuilder();
        foreach (UIMotionCG motion in UnityEngine.Object.FindObjectsOfType<UIMotionCG>()) {
            text.AppendLine($"{motion.name}: {(motion.Clip != null ? motion.Clip.name : "none")}, frame {motion.Frame}, shown {motion.FrameShown}, finished {motion.Finished}");
        }

        return text.Length > 0 ? text.ToString() : "No CG motion clips on screen.";
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

    // One player action at the player's pace (for recordings): use the named backpack item, or the strongest weapon,
    // on the first enemy still standing. Pair with EndTurn().
    public static string Strike(string itemName = null) {
        CombatSystem combat = GameRoot.Core?.Combat;
        if (combat == null || combat.CurrentState != CombatState.PlayerTurn) {
            return "Not the player's turn.";
        }

        BackpackGrid grid = GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid as BackpackGrid;
        ItemEntity chosen = null;
        foreach (ItemEntity item in grid.ContainedItems) {
            bool named = !string.IsNullOrEmpty(itemName) && item.Name == itemName;
            bool weapon = string.IsNullOrEmpty(itemName) && item.ItemType == nameof(ItemType.Weapon) && item.Combat != null
                && item.Combat.TriggerType == TriggerType.Manual.ToString() && (chosen == null || item.Combat.BaseValue > chosen.Combat.BaseValue);
            if (named || weapon) {
                chosen = item;
            }
        }

        string reason;
        bool started = chosen != null ? ItemUseService.TryUseItem(chosen, out reason) : ItemUseService.TryBeginUnarmedStrike(out reason);
        FighterEntity enemy = combat.EnemyFaction.Fighters.Find(f => f.RuntimeHP > 0);
        if (started && ItemUseService.HasPendingEnemyTargetSelection && enemy != null) {
            ItemUseService.TryConfirmPendingTarget(enemy, out reason);
        }

        return started ? $"Used {(chosen != null ? chosen.Name : "徒手")}.\n" + Describe() : $"Could not act: {reason}";
    }

    public static string EndTurn() {
        CombatSystem combat = GameRoot.Core?.Combat;
        if (combat == null || combat.CurrentState != CombatState.PlayerTurn) {
            return "Not the player's turn.";
        }

        combat.EndPlayerTurn();
        return "Turn ended.";
    }

    private static string PlayCombat() {
        CombatSystem combat = GameRoot.Core.Combat;
        if (EasyFights) {
            foreach (FighterEntity enemy in combat.EnemyFaction.Fighters) {
                enemy.RuntimeHP = Mathf.Min(enemy.RuntimeHP, 1);
            }
        }

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

            // No weapon: bare hands, as a player with an empty backpack would.
            int cost = weapon != null ? weapon.Combat.APCost : ItemUseService.GetUnarmedAPCost(player.DataRef);
            int guard = 0;
            while (combat.CurrentState == CombatState.PlayerTurn && player.CurrentAP >= cost && guard++ < 8) {
                FighterEntity enemy = combat.EnemyFaction.Fighters.Find(f => f.RuntimeHP > 0);
                if (enemy == null) {
                    break;
                }

                if (weapon != null) {
                    ItemUseService.TryUseItem(weapon, out reason);
                } else if (!ItemUseService.TryBeginUnarmedStrike(out reason)) {
                    break;
                }

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

    private static List<string> ActivePanels(Transform canvas) {
        List<string> panels = new List<string>();
        if (canvas == null) {
            return panels;
        }

        foreach (Transform child in canvas) {
            if (child.gameObject.activeSelf && child.name != "InventoryItemLayer" && child.name != "GridContainer" && child.name != OverlayName) {
                panels.Add(child.name);
            }
        }

        return panels;
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
