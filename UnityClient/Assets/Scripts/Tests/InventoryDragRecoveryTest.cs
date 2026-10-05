using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Playtest fixes after M2: a failed drop onto the backpack used to leave the item without raycasts (it could never be
// picked up again), and loot dropped beside the backpack snapped back to the tray instead of staying where it landed.
public static class InventoryDragRecoveryTest {
    public static void Run() {
        Debug.Log("=== Running Inventory Drag Recovery Test ===");

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();

        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        BackpackGrid grid = new BackpackGrid(doll.Chassis);
        doll.RuntimeGrid = grid;

        List<string> failures = new List<string>();
        void Check(bool condition, string message) {
            if (!condition) {
                failures.Add(message);
            }
        }

        GameObject canvasObj = new GameObject("InventoryDragRecoveryCanvas");
        canvasObj.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<GraphicRaycaster>();
        GameFlowController previousFlow = GameFlowController.Instance;
        GameObject flowObj = new GameObject("GameFlowController");
        GameFlowController flow = flowObj.AddComponent<GameFlowController>();
        SetGameFlowInstance(flow);
        try {
            SetGameFlowScreen(flow, "CombatLoot");
            flowObj.AddComponent<InventoryPresentationController>().SetContext(InventoryPresentationMode.CombatLoot);

            ItemEntity held = ConfigManager.CreateItem("gear_whetstone");
            ItemEntity blocker = ConfigManager.CreateItem("gear_whetstone");
            Check(Place(held, 0, 0, out string reason) && Place(blocker, 1, 0, out reason), "setup: " + reason);

            // A failed drop onto an occupied slot returns the item, and it can be dragged again.
            DraggableItemUI heldUI = CreateItemUI(canvasObj.transform, held);
            GridSlotUI occupiedSlot = CreateSlot(canvasObj.transform, 1, 0);
            PointerEventData drag = new PointerEventData(null) { position = new Vector2(32f, 32f), pointerDrag = heldUI.gameObject };
            heldUI.OnBeginDrag(drag);
            occupiedSlot.OnDrop(drag);
            heldUI.OnEndDrag(drag);
            CanvasGroup group = heldUI.GetComponent<CanvasGroup>();
            Check(group.blocksRaycasts, "a failed drop left the item without raycasts");
            Check(grid.ContainedItems.Contains(held) && held.Grid.CurrentPos[0] == 0, "a failed drop did not put the item back");
            heldUI.OnBeginDrag(drag);
            Check(heldUI.IsDragging(), "the item could not be picked up again");
            heldUI.OnEndDrag(drag);

            // Loot from the tray dropped beside the backpack stays where it landed, still unclaimed.
            GameObject tray = new GameObject("LootTray", typeof(RectTransform));
            tray.transform.SetParent(canvasObj.transform, false);
            ItemEntity loot = ConfigManager.CreateItem("gear_whetstone");
            DraggableItemUI lootUI = CreateItemUI(tray.transform, loot);
            Vector3 start = lootUI.transform.position;
            PointerEventData lootDrag = new PointerEventData(null) { position = new Vector2(100f, 100f), pointerDrag = lootUI.gameObject };
            lootUI.OnBeginDrag(lootDrag);
            lootDrag.position = new Vector2(300f, 200f);
            lootUI.OnDrag(lootDrag);
            Vector3 dropped = lootUI.transform.position;
            lootUI.OnEndDrag(lootDrag);
            Check(dropped != start && lootUI.transform.position == dropped, $"loot snapped back: start {start}, dropped {dropped}, now {lootUI.transform.position}");
            Check(lootUI.transform.parent == tray.transform && !lootUI.IsPendingDiscard && lootUI.GetComponent<CanvasGroup>().blocksRaycasts, "loot left the tray or lost its raycasts");

            // An item taken out of the backpack keeps its discard mark after a failed drop back onto an occupied slot.
            heldUI.OnBeginDrag(drag);
            heldUI.OnEndDrag(drag);
            Check(heldUI.IsPendingDiscard && !grid.ContainedItems.Contains(held), "an item taken out was not staged for discard");
            heldUI.OnBeginDrag(drag);
            occupiedSlot.OnDrop(drag);
            heldUI.OnEndDrag(drag);
            Check(heldUI.IsPendingDiscard && group.blocksRaycasts, "a failed drop cleared the discard mark");
        } finally {
            SetGameFlowInstance(previousFlow);
            UnityEngine.Object.DestroyImmediate(canvasObj);
            UnityEngine.Object.DestroyImmediate(flowObj);
        }

        if (failures.Count == 0) {
            Debug.Log("Inventory Drag Recovery PASSED.");
        } else {
            Debug.LogError("Inventory Drag Recovery FAILED: " + string.Join("; ", failures));
        }

        GameRoot.Core = null;
        Debug.Log("=== Inventory Drag Recovery Test Finished ===");
    }

    private static bool Place(ItemEntity item, int x, int y, out string reason) {
        return InventoryInteractionService.RequestPlace(item, x, y, InventoryInteractionContext.FromCurrentDoll("DragRecoveryTestSetup"), out reason);
    }

    private static DraggableItemUI CreateItemUI(Transform parent, ItemEntity item) {
        GameObject itemObj = new GameObject("ItemUI_" + item.InstanceID, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        itemObj.transform.SetParent(parent, false);
        DraggableItemUI itemUI = itemObj.AddComponent<DraggableItemUI>();
        itemUI.SetupData(item);
        return itemUI;
    }

    private static GridSlotUI CreateSlot(Transform parent, int x, int y) {
        GameObject slotObj = new GameObject($"Slot_{x}_{y}", typeof(RectTransform), typeof(Image));
        slotObj.transform.SetParent(parent, false);
        GridSlotUI slot = slotObj.AddComponent<GridSlotUI>();
        slot.Initialize(x, y, false);
        return slot;
    }

    private static void SetGameFlowScreen(GameFlowController controller, string screenName) {
        Type screenType = typeof(GameFlowController).GetNestedType("GameScreenState", BindingFlags.NonPublic);
        typeof(GameFlowController)
            .GetField("_currentScreen", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, Enum.Parse(screenType, screenName));
    }

    private static void SetGameFlowInstance(GameFlowController controller) {
        typeof(GameFlowController)
            .GetProperty("Instance", BindingFlags.Static | BindingFlags.Public)
            .GetSetMethod(true)
            .Invoke(null, new object[] { controller });
    }
}
