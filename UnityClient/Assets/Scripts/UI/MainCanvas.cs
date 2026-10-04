using UnityEngine;

// The scene's main UI canvas (InventoryCanvas). FindObjectOfType<Canvas>() alone can return a nested canvas such as
// the dialogue overlay, whose CanvasGroup hides everything under it, or the F1 dev canvas; which one it returns
// differs between the Editor and builds (found 2026-10-04: the camp result panel ended up invisible in a build).
public static class MainCanvas {
    private const string Name = "InventoryCanvas";

    public static Canvas Find() {
        GameObject named = GameObject.Find(Name);
        Canvas canvas = named != null ? named.GetComponent<Canvas>() : null;
        if (canvas != null) {
            return canvas;
        }

        Canvas any = Object.FindObjectOfType<Canvas>();
        return any != null ? any.rootCanvas : null;
    }
}
