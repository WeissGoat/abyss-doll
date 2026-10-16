using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Producer playtest after M4: items need a hover card. The text is read from item data (no written descriptions yet);
// the card follows the hovered element and only that element's exit hides it. Also covers the end-turn click guard
// from the same playtest (a click landing right after the auto end skipped the next turn).
public static class ItemTooltipTest {
    public static void Run() {
        Debug.Log("=== Running Item Tooltip Test ===");

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;

        List<string> failures = new List<string>();
        void Check(bool condition, string message) {
            if (!condition) {
                failures.Add(message);
            }
        }

        // A weapon: its use, its adjacency boost (direction turns with the item), size and value.
        ItemEntity hook = ConfigManager.CreateItem("gear_chain_hook");
        ItemTooltipText hookText = ItemPresentationRules.BuildTooltip(hook);
        Check(hookText.Title == hook.Name, "title: " + hookText.Title);
        Check(hookText.Body.Contains("武器") && hookText.Body.Contains("AP") && hookText.Body.Contains("价值"), "weapon body: " + hookText.Body);
        Check(hookText.Body.Contains("紧贴右侧的物品伤害 +20%"), "adjacency line: " + hookText.Body);
        Check(hookText.Body.Contains("格"), "size line: " + hookText.Body);
        hook.Grid.Rotation = 90;
        string rotatedBody = ItemPresentationRules.BuildTooltip(hook).Body;
        Check(!rotatedBody.Contains("紧贴右侧") && rotatedBody.Contains("伤害 +20%"), "rotated adjacency still says right: " + rotatedBody);

        // Passive armour and a dive-cost item.
        string armour = ItemPresentationRules.BuildTooltip(ConfigManager.CreateItem("gear_iron_armor")).Body;
        Check(armour.Contains("每回合开始获得 15 护盾"), "armour: " + armour);
        string cloak = ItemPresentationRules.BuildTooltip(ConfigManager.CreateItem("gear_mycelium_cloak")).Body;
        Check(cloak.Contains("SAN 消耗 -10%"), "cloak: " + cloak);

        // The card: shown for the hovered element, above modals, hidden only by that element.
        GameObject canvasObj = new GameObject("ItemTooltipTestCanvas");
        try {
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            RectTransform first = CreateAnchor(canvasObj.transform, "First");
            RectTransform second = CreateAnchor(canvasObj.transform, "Second");

            ItemTooltip.Show(hook, first);
            Check(ItemTooltip.IsShowing && ItemTooltip.ShownTitle == hook.Name, "tooltip not shown for the hovered item");
            Canvas tooltipCanvas = ItemTooltip.ShownRect != null ? ItemTooltip.ShownRect.GetComponent<Canvas>() : null;
            Check(tooltipCanvas != null && tooltipCanvas.sortingOrder > DraggableItemUI.HeldSortingOrder, "tooltip does not draw above held items and modals");
            Check(ItemTooltip.ShownRect != null && ItemTooltip.ShownRect.rect.height > 40f, "tooltip did not size to its text");

            ItemTooltip.Show(ConfigManager.CreateItem("gear_iron_armor"), second);
            ItemTooltip.HideFor(first);
            Check(ItemTooltip.IsShowing, "leaving the first item hid the second item's card");
            ItemTooltip.HideFor(second);
            Check(!ItemTooltip.IsShowing, "leaving the item did not hide its card");
        } finally {
            ItemTooltip.Hide();
            Object.DestroyImmediate(canvasObj);
        }

        // End-turn guard: a click right after a player turn starts is dropped; later clicks go through.
        EndTurnClickGuard guard = new EndTurnClickGuard();
        Check(guard.AllowManualEnd(0f), "first click blocked");
        guard.OnPlayerTurnStarted(10f);
        Check(!guard.AllowManualEnd(10.05f), "a click just after the new turn started was not dropped");
        Check(guard.AllowManualEnd(10f + EndTurnClickGuard.GraceSeconds), "a click after the grace was dropped");

        GameRoot.Core = null;
        if (failures.Count == 0) {
            Debug.Log("Item Tooltip PASSED.");
        } else {
            Debug.LogError("Item Tooltip FAILED: " + string.Join("; ", failures));
        }

        Debug.Log("=== Item Tooltip Test Finished ===");
    }

    private static RectTransform CreateAnchor(Transform parent, string name) {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)obj.transform;
        rect.sizeDelta = new Vector2(120f, 120f);
        return rect;
    }
}
