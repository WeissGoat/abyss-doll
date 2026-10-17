using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// The drop preview on the real loot screen: a held item over an occupied cell colours its cells red, and released in
// the gap between cells (where no slot gets the drop event) it lands in the cell the preview showed.
public static class DropPreviewPlayScenario {
    public static IEnumerator Run() {
        yield return PlayScenarioSupport.BootToWorkshop();
        yield return PlayScenarioSupport.OpenWhetstoneLoot("play_scenario_drop_preview");

        BackpackGrid grid = GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid as BackpackGrid;
        DraggableItemUI held = null;
        foreach (DraggableItemUI item in Object.FindObjectsOfType<DraggableItemUI>()) {
            // A one-cell item, the offered loot first (a backpack item may be the only occupied cell).
            if (item.isActiveAndEnabled && item.ItemData != null
                && BackpackGrid.TryGetRotatedBounds(item.ItemData, item.ItemData.Grid.Rotation, out int cols, out int rows) && cols == 1 && rows == 1
                && (held == null || !grid.ContainedItems.Contains(item.ItemData))) {
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
