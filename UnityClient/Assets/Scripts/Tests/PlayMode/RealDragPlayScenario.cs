using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// Drags on the loot screen through Unity's own event system with a scripted mouse (ScriptedPointer), the way a
// player's mouse does it: loot dropped on a free cell lands in the backpack; a backpack item dropped outside the
// backpack stays in sight; an item from outside dropped on another item goes back to where it was. All three used to
// vanish in the 2026-10-17 builds.
public static class RealDragPlayScenario {
    public static IEnumerator Run() {
        yield return PlayScenarioSupport.BootToWorkshop();
        yield return PlayScenarioSupport.OpenWhetstoneLoot("play_scenario_real_drag", 2);

        BackpackGrid grid = GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid as BackpackGrid;
        DraggableItemUI loot = null;
        DraggableItemUI spare = null;
        DraggableItemUI packed = null;
        foreach (DraggableItemUI item in Object.FindObjectsOfType<DraggableItemUI>()) {
            if (!item.isActiveAndEnabled || item.ItemData == null) {
                continue;
            }

            if (grid.ContainedItems.Contains(item.ItemData)) {
                packed = packed ?? item;
            } else if (BackpackGrid.TryGetRotatedBounds(item.ItemData, item.ItemData.Grid.Rotation, out int cols, out int rows) && cols == 1 && rows == 1) {
                spare = loot;
                loot = item;
            }
        }

        GridSlotUI free = null;
        foreach (GridSlotUI slot in Object.FindObjectsOfType<GridSlotUI>()) {
            if (!slot.IsLocked && grid.GetItemAt(slot.X, slot.Y) == null && (free == null || slot.Y > free.Y || (slot.Y == free.Y && slot.X > free.X))) {
                free = slot;
            }
        }

        if (loot == null || spare == null || packed == null || free == null) {
            Debug.LogError($"[RealDrag] Setup missing: two 1x1 loot {loot != null && spare != null}, packed item {packed != null}, free cell {free != null}.\n{P3DevTools.Describe()}");
            yield break;
        }

        ScriptedPointer pointer = ScriptedPointer.Install();
        if (pointer == null) {
            yield break;
        }

        // 1. Loot onto a free cell.
        yield return pointer.Drag(Center(loot.transform), Center(free.transform));
        if (grid.GetItemAt(free.X, free.Y) != loot.ItemData) {
            Debug.LogError($"[RealDrag] Loot {loot.ItemData.Name} dropped on free cell ({free.X},{free.Y}) did not land there. {Describe(loot)}");
        } else {
            CheckInSight(loot, "loot dropped into the backpack");
        }

        // 2. A backpack item out past the backpack's left edge.
        RectTransform gridRect = (RectTransform)free.GetComponentInParent<GridGenerator>().transform;
        Vector3[] corners = new Vector3[4];
        gridRect.GetWorldCorners(corners);
        Vector2 outside = RectTransformUtility.WorldToScreenPoint(null, corners[0]) + new Vector2(-60f, 60f);
        string packedName = packed.ItemData.Name;
        yield return pointer.Drag(Center(packed.transform), outside);
        CheckInSight(packed, "backpack item dropped outside the backpack");

        // 3. That item, now lying outside, and 4. the other loot, each dropped on a cell another item holds: both go
        // back to where they were picked up.
        GridSlotUI occupied = null;
        foreach (GridSlotUI slot in Object.FindObjectsOfType<GridSlotUI>()) {
            if (!slot.IsLocked && grid.GetItemAt(slot.X, slot.Y) != null) {
                occupied = slot;
            }
        }

        foreach (DraggableItemUI outsider in new[] { packed, spare }) {
            Vector2 before = Center(outsider.transform);
            yield return pointer.Drag(before, Center(occupied.transform));
            if (grid.ContainedItems.Contains(outsider.ItemData) || (Center(outsider.transform) - before).magnitude > 2f) {
                Debug.LogError($"[RealDrag] {outsider.ItemData.Name} dropped on occupied cell ({occupied.X},{occupied.Y}) did not go back to {before}. {Describe(outsider)}");
            }

            CheckInSight(outsider, "item from outside dropped on another item");
        }

        pointer.Uninstall();
        Debug.Log($"[RealDrag] {loot.ItemData.Name} landed in ({free.X},{free.Y}); {packedName} outside the backpack: {Describe(packed)}.");
        P3DevTools.TakeLoot();
    }

    // In sight: drawn on top of the backpack grid and inside the screen, not left under another panel.
    private static void CheckInSight(DraggableItemUI item, string what) {
        if (item == null || !item.isActiveAndEnabled) {
            Debug.LogError($"[RealDrag] {what}: the item is gone.");
            return;
        }

        Vector2 centre = Center(item.transform);
        if (centre.x < 0f || centre.y < 0f || centre.x > Screen.width || centre.y > Screen.height) {
            Debug.LogError($"[RealDrag] {what}: off screen at {centre}. {Describe(item)}");
            return;
        }

        // What a click there would hit, the item's own picture switched to raycastable for the look.
        CanvasGroup group = item.GetComponent<CanvasGroup>();
        PointerEventData probe = new PointerEventData(EventSystem.current) { position = centre };
        var hits = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(probe, hits);
        GameObject top = hits.Count > 0 ? hits[0].gameObject : null;
        if (top == null || (top != item.gameObject && !top.transform.IsChildOf(item.transform))) {
            Debug.LogError($"[RealDrag] {what}: covered by {(top != null ? top.name : "nothing")} (alpha {(group != null ? group.alpha : 1f)}). {Describe(item)}");
        }
    }

    private static string Describe(DraggableItemUI item) {
        Transform parent = item.transform.parent;
        return $"{item.ItemData?.Name} under {(parent != null ? parent.name : "-")} at {Center(item.transform)}";
    }

    private static Vector2 Center(Transform transform) {
        RectTransform rect = (RectTransform)transform;
        return RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
    }
}
