using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// The drop preview on the real loot screen: a held item over an occupied cell colours its cells red, and released in
// the gap between cells (where no slot gets the drop event) it lands in the cell the preview showed.
public static class DropPreviewPlayScenario {
    public static IEnumerator Run() {
        yield return PlayScenarioSupport.BootToWorkshop();
        P3DevTools.GoTo("combat");
        yield return PlayScenarioSupport.Until(() => GameRoot.Core.Combat.CurrentState == CombatState.PlayerTurn, 10f, "combat did not start");
        foreach (FighterEntity enemy in GameRoot.Core.Combat.EnemyFaction.Fighters) {
            enemy.RuntimeHP = Mathf.Min(enemy.RuntimeHP, 1);
        }

        P3DevTools.AutoCombat();
        yield return PlayScenarioSupport.Until(() => GameRoot.Core.Combat.CurrentState == CombatState.End && VisualQueue.Count == 0, 20f, "the fight did not finish");
        yield return new WaitForSecondsRealtime(1f);
        CombatLootPickupResult offer = new CombatLootPickupResult { NodeID = "play_scenario_drop_preview" };
        offer.OfferedItems.Add(ConfigManager.CreateItem("gear_whetstone"));
        if (GameObject.Find("CombatLootPanel_Runtime") == null) {
            GameFlowController.Instance.EnterCombatLoot(offer);
        }

        yield return PlayScenarioSupport.Until(() => GameObject.Find("CombatLootPanel_Runtime") != null, 5f, "no loot screen");
        yield return new WaitForSecondsRealtime(0.5f);

        BackpackGrid grid = GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid as BackpackGrid;
        DraggableItemUI held = null;
        foreach (DraggableItemUI item in Object.FindObjectsOfType<DraggableItemUI>()) {
            // Any one-cell item, from the loot tray or the backpack.
            if (item.isActiveAndEnabled && item.ItemData != null
                && BackpackGrid.TryGetRotatedBounds(item.ItemData, item.ItemData.Grid.Rotation, out int cols, out int rows) && cols == 1 && rows == 1) {
                held = item;
            }
        }

        GridSlotUI occupied = null;
        GridSlotUI free = null;
        foreach (GridSlotUI slot in Object.FindObjectsOfType<GridSlotUI>()) {
            if (slot.IsLocked) {
                continue;
            }

            ItemEntity there = grid.GetItemAt(slot.X, slot.Y);
            if (there != null && there != held?.ItemData) {
                occupied = occupied ?? slot;
            } else if (there == null && (free == null || slot.X < free.X || (slot.X == free.X && slot.Y < free.Y))) {
                free = slot;
            }
        }

        if (held == null || occupied == null || free == null) {
            Debug.LogError($"[DropPreview] Setup missing: 1x1 item {held != null}, occupied cell {occupied != null}, free cell {free != null}.\n{P3DevTools.Describe()}");
            yield break;
        }

        RectTransform heldRect = (RectTransform)held.transform;
        Vector2 start = RectTransformUtility.WorldToScreenPoint(null, heldRect.TransformPoint(heldRect.rect.center));
        PointerEventData data = new PointerEventData(EventSystem.current) {
            button = PointerEventData.InputButton.Left,
            position = start,
            pressPosition = start,
            pointerDrag = held.gameObject
        };
        held.OnBeginDrag(data);

        data.position = Center(occupied);
        held.OnDrag(data);
        held.UpdatePlacementPreview(data.position);
        if (!occupied.IsShowingPreviewSkin(false)) {
            Debug.LogError($"[DropPreview] Over the occupied cell ({occupied.X},{occupied.Y}) the preview is not red.");
        }

        // Just past the free cell's right edge: inside the 5 px gap, nearer this cell than the next one.
        RectTransform freeRect = (RectTransform)free.transform;
        Vector3[] corners = new Vector3[4];
        freeRect.GetWorldCorners(corners);
        Vector2 rightEdge = RectTransformUtility.WorldToScreenPoint(null, (corners[2] + corners[3]) * 0.5f);
        data.position = rightEdge + new Vector2(1.5f, 0f);
        held.OnDrag(data);
        held.UpdatePlacementPreview(data.position);
        if (held.PreviewSlot != free || !free.IsShowingPreviewSkin(true)) {
            Debug.LogError($"[DropPreview] In the gap beside free cell ({free.X},{free.Y}) the preview shows {(held.PreviewSlot != null ? $"({held.PreviewSlot.X},{held.PreviewSlot.Y})" : "nothing")}, green={free.IsShowingPreviewSkin(true)}.");
        }

        held.OnEndDrag(data);
        if (grid.GetItemAt(free.X, free.Y) != held.ItemData) {
            Debug.LogError($"[DropPreview] Released in the gap, {held.ItemData.Name} did not land in the previewed cell ({free.X},{free.Y}).");
        }

        foreach (GridSlotUI slot in Object.FindObjectsOfType<GridSlotUI>()) {
            if (slot.IsShowingPreviewSkin(true) || slot.IsShowingPreviewSkin(false)) {
                Debug.LogError($"[DropPreview] Cell ({slot.X},{slot.Y}) still shows the preview after the drop.");
                break;
            }
        }

        Debug.Log($"[DropPreview] Red over ({occupied.X},{occupied.Y}); gap drop landed {held.ItemData.Name} in ({free.X},{free.Y}).");
        P3DevTools.TakeLoot();
    }

    private static Vector2 Center(GridSlotUI slot) {
        RectTransform rect = (RectTransform)slot.transform;
        return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
    }
}
