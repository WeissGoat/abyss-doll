using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The draw-order check behind P3DevTools.Occlusion/DragCheck, on the layout of the M4 playtest bug: a loot tray, then
// the backpack grid as a later sibling. A plain later sibling covers the item; a held item draws above it.
public static class UIOcclusionTest {
    public static void Run() {
        Debug.Log("=== Running UI Occlusion Test ===");

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        doll.RuntimeGrid = new BackpackGrid(doll.Chassis);

        List<string> failures = new List<string>();
        void Check(bool condition, string message) {
            if (!condition) {
                failures.Add(message);
            }
        }

        GameObject canvasObj = new GameObject("UIOcclusionTestCanvas");
        try {
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            RectTransform tray = CreateImage(canvasObj.transform, "LootTray", new Vector2(200f, 200f), new Vector2(300f, 300f));
            DraggableItemUI loot = CreateItem(tray, ConfigManager.CreateItem("gear_whetstone"));
            RectTransform backpack = CreateImage(canvasObj.transform, "BackpackGrid", new Vector2(600f, 200f), new Vector2(300f, 300f));
            RectTransform lootRect = (RectTransform)loot.transform;
            Vector2 overBackpack = backpack.position;

            // Not covered where it lies; a later sibling is on top once the item sits over it.
            Check(UIOcclusion.CoveringGraphics(lootRect).Count == 0, "the item in its tray is reported as covered");
            lootRect.position = overBackpack;
            List<Graphic> covering = UIOcclusion.CoveringGraphics(lootRect, overBackpack);
            Check(covering.Exists(graphic => graphic.transform == backpack), "the backpack drawn after the tray does not cover the item");
            lootRect.anchoredPosition = Vector2.zero;

            // Held, the item draws above the backpack.
            PointerEventData drag = new PointerEventData(null) { position = lootRect.position, pointerDrag = loot.gameObject };
            loot.OnBeginDrag(drag);
            drag.position = overBackpack;
            loot.OnDrag(drag);
            List<Graphic> whileHeld = UIOcclusion.CoveringGraphics(lootRect, overBackpack);
            Check(whileHeld.Count == 0, "the held item is covered by " + string.Join(", ", whileHeld.ConvertAll(graphic => graphic.name).ToArray()));
            loot.ReturnToOriginalPosition();
            loot.OnEndDrag(drag);

            // A popup on its own sorting canvas covers the item even as an earlier sibling; hidden graphics never do.
            RectTransform popup = CreateImage(canvasObj.transform, "Popup", new Vector2(200f, 200f), new Vector2(300f, 300f));
            popup.SetAsFirstSibling();
            Canvas popupCanvas = popup.gameObject.AddComponent<Canvas>();
            popupCanvas.overrideSorting = true;
            popupCanvas.sortingOrder = 540;
            Check(UIOcclusion.CoveringGraphics(lootRect).Exists(graphic => graphic.transform == popup), "a popup on a higher sorting canvas does not cover the item");
            popup.gameObject.SetActive(false);
            Check(UIOcclusion.CoveringGraphics(lootRect).Count == 0, "a hidden popup still covers the item");
        } finally {
            Object.DestroyImmediate(canvasObj);
        }

        GameRoot.Core = null;
        if (failures.Count == 0) {
            Debug.Log("UI Occlusion PASSED.");
        } else {
            Debug.LogError("UI Occlusion FAILED: " + string.Join("; ", failures));
        }

        Debug.Log("=== UI Occlusion Test Finished ===");
    }

    private static RectTransform CreateImage(Transform parent, string name, Vector2 position, Vector2 size) {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)obj.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static DraggableItemUI CreateItem(Transform parent, ItemEntity item) {
        GameObject obj = new GameObject("ItemUI_" + item.InstanceID, typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        obj.transform.SetParent(parent, false);
        DraggableItemUI itemUI = obj.AddComponent<DraggableItemUI>();
        itemUI.SetupData(item);
        ((RectTransform)obj.transform).sizeDelta = new Vector2(64f, 64f);
        return itemUI;
    }
}
