using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A mouse driven by dev commands for recording footage (trailer, bug clips): it glides, clicks and drags through the
// real input module (ScriptedPointer), so hover tooltips, drag previews and target arrows show as for a player, and it
// draws its own arrow cursor on top so frame recordings (P3FrameRecorder) show where it is. While it is on, the real
// mouse is ignored; P3DevTools.PointerOff() gives the mouse back.
// Targets: "x,y" (1920x1080, top-left origin), "node:N" (Nth map node, left to right), "item:名字" (an item on
// screen), "cell:x,y" (a backpack cell), "enemy:N", "pat:head|hand" (where Zero is touched), "grime:N" (a smudge on her),
// or the label of a button that can be clicked, or an object name.
public class P3DemoPointer : MonoBehaviour {
    private const float CursorSize = 36f;
    private static P3DemoPointer _instance;

    private readonly Queue<IEnumerator> _queue = new Queue<IEnumerator>();
    private ScriptedPointer _pointer;
    private RectTransform _cursor;
    private Vector2 _position;
    private bool _running;

    public static bool Busy => _instance != null && (_instance._running || _instance._queue.Count > 0);

    public static string MoveTo(string target, float seconds) {
        P3DemoPointer pointer = Ensure();
        if (pointer == null) {
            return "No EventSystem with a StandaloneInputModule.";
        }

        pointer.Enqueue(pointer.Glide(target, seconds, false));
        return $"Pointer to {target} over {seconds:0.##}s.";
    }

    public static string Click(string target, float seconds) {
        P3DemoPointer pointer = Ensure();
        if (pointer == null) {
            return "No EventSystem with a StandaloneInputModule.";
        }

        if (!string.IsNullOrEmpty(target)) {
            pointer.Enqueue(pointer.Glide(target, seconds, false));
        }

        pointer.Enqueue(pointer.ClickHere());
        return string.IsNullOrEmpty(target) ? "Pointer click." : $"Pointer to {target} and click.";
    }

    public static string Drag(string from, string to, float seconds) {
        P3DemoPointer pointer = Ensure();
        if (pointer == null) {
            return "No EventSystem with a StandaloneInputModule.";
        }

        pointer.Enqueue(pointer.Glide(from, 0.5f, false));
        pointer.Enqueue(pointer.Hold(true));
        pointer.Enqueue(pointer.Glide(to, seconds, true));
        pointer.Enqueue(pointer.Hold(false));
        return $"Pointer drags {from} to {to} over {seconds:0.##}s.";
    }

    // Press on the target and rub back and forth (stroking her head, wiping a smudge), then let go.
    public static string Scrub(string target, float seconds, float width) {
        P3DemoPointer pointer = Ensure();
        if (pointer == null) {
            return "No EventSystem with a StandaloneInputModule.";
        }

        pointer.Enqueue(pointer.Glide(target, 0.5f, false));
        pointer.Enqueue(pointer.Hold(true));
        pointer.Enqueue(pointer.Rub(seconds, width));
        pointer.Enqueue(pointer.Hold(false));
        return $"Pointer rubs {target} for {seconds:0.##}s.";
    }

    // Press on the target, keep holding, let go (holding her hand).
    public static string HoldOn(string target, float seconds) {
        P3DemoPointer pointer = Ensure();
        if (pointer == null) {
            return "No EventSystem with a StandaloneInputModule.";
        }

        pointer.Enqueue(pointer.Glide(target, 0.5f, false));
        pointer.Enqueue(pointer.Hold(true));
        pointer.Enqueue(pointer.Wait(seconds));
        pointer.Enqueue(pointer.Hold(false));
        return $"Pointer holds {target} for {seconds:0.##}s.";
    }

    // Press on the target and keep holding (a drag in steps: Down, Pointer moves, Rotate, Up).
    public static string Down(string target, float seconds) {
        P3DemoPointer pointer = Ensure();
        if (pointer == null) {
            return "No EventSystem with a StandaloneInputModule.";
        }

        pointer.Enqueue(pointer.Glide(target, seconds, false));
        pointer.Enqueue(pointer.Hold(true));
        return $"Pointer presses {target}.";
    }

    public static string Up() {
        if (_instance == null) {
            return "Pointer is off.";
        }

        _instance.Enqueue(_instance.Hold(false));
        return "Pointer lets go.";
    }

    // A right click where the pointer is: turns the item being dragged, or the backpack item under it.
    public static string Rotate() {
        P3DemoPointer pointer = Ensure();
        if (pointer == null) {
            return "No EventSystem with a StandaloneInputModule.";
        }

        pointer.Enqueue(pointer.RightClick());
        return "Pointer right-clicks.";
    }

    public static string Off() {
        if (_instance == null) {
            return "Pointer is off.";
        }

        _instance.StopAllCoroutines();
        _instance._queue.Clear();
        if (_instance._pointer != null) {
            _instance._pointer.Uninstall();
        }

        Destroy(_instance.gameObject);
        _instance = null;
        return "Pointer off; the real mouse is back.";
    }

    private static P3DemoPointer Ensure() {
        if (_instance != null) {
            return _instance;
        }

        ScriptedPointer pointer = ScriptedPointer.Install();
        if (pointer == null) {
            return null;
        }

        GameObject host = new GameObject("P3DemoPointer");
        DontDestroyOnLoad(host);
        _instance = host.AddComponent<P3DemoPointer>();
        _instance._pointer = pointer;
        _instance._position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        pointer.MoveTo(_instance._position);
        _instance.BuildCursor();
        return _instance;
    }

    private void BuildCursor() {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject cursorObj = new GameObject("DemoCursor_Image", typeof(RectTransform));
        cursorObj.transform.SetParent(transform, false);
        RawImage image = cursorObj.AddComponent<RawImage>();
        image.texture = ArrowTexture();
        image.raycastTarget = false;
        _cursor = image.rectTransform;
        _cursor.anchorMin = Vector2.zero;
        _cursor.anchorMax = Vector2.zero;
        _cursor.pivot = new Vector2(0.06f, 0.94f);
        _cursor.sizeDelta = new Vector2(CursorSize, CursorSize);
        PlaceCursor();
    }

    // A white arrow with a dark outline, drawn once.
    private static Texture2D ArrowTexture() {
        const int size = 64;
        Vector2[] outer = { new Vector2(4, 60), new Vector2(4, 8), new Vector2(17, 21), new Vector2(26, 2), new Vector2(35, 6),
            new Vector2(26, 25), new Vector2(44, 25) };
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        Color clear = new Color(0f, 0f, 0f, 0f);
        Color edge = new Color(0.12f, 0.1f, 0.1f, 1f);
        Color fill = new Color(0.98f, 0.96f, 0.92f, 1f);
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                float d = SignedDistance(p, outer);
                texture.SetPixel(x, y, d > 0.5f ? clear : d > -2.5f ? edge : fill);
            }
        }

        texture.Apply();
        return texture;
    }

    // Negative inside the polygon; the distance to its nearest edge.
    private static float SignedDistance(Vector2 p, Vector2[] poly) {
        float best = float.MaxValue;
        bool inside = false;
        for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++) {
            Vector2 a = poly[j];
            Vector2 b = poly[i];
            if ((b.y > p.y) != (a.y > p.y) && p.x < (a.x - b.x) * (p.y - b.y) / (a.y - b.y) + b.x) {
                inside = !inside;
            }

            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
        }

        return inside ? -best : best;
    }

    // A build ignores pointer events while its window is not in front (the recording runs behind other windows), so
    // the EventSystem is told it has focus while this pointer drives it.
    private void Update() {
        EventSystem system = EventSystem.current;
        if (system != null && !system.isFocused) {
            typeof(EventSystem).GetMethod("OnApplicationFocus", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(system, new object[] { true });
        }
    }

    private void Enqueue(IEnumerator step) {
        _queue.Enqueue(step);
        if (!_running) {
            StartCoroutine(Run());
        }
    }

    private IEnumerator Run() {
        _running = true;
        while (_queue.Count > 0) {
            yield return StartCoroutine(_queue.Dequeue());
        }

        _running = false;
    }

    private IEnumerator Glide(string target, float seconds, bool held) {
        if (!TryResolve(target, out Vector2 to)) {
            Debug.LogWarning($"[P3DemoPointer] No target '{target}'.");
            yield break;
        }

        Vector2 from = _position;
        // A slight arc, like a hand moving a mouse.
        Vector2 bend = Vector2.Perpendicular(to - from) * 0.08f;
        float duration = Mathf.Max(0.05f, seconds);
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime) {
            float p = Mathf.Clamp01(t / duration);
            float e = p * p * (3f - 2f * p);
            Set(Vector2.Lerp(from, to, e) + bend * Mathf.Sin(e * Mathf.PI));
            yield return null;
        }

        Set(to);
        yield return null;
        if (!held) {
            yield return null;
        }
    }

    private IEnumerator Rub(float seconds, float width) {
        Vector2 center = _position;
        float scale = Screen.height / 1080f;
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime) {
            Set(center + new Vector2(Mathf.Sin(t * Mathf.PI * 2.4f) * width * scale, Mathf.Sin(t * Mathf.PI * 4.8f) * 6f * scale));
            yield return null;
        }

        Set(center);
        yield return null;
    }

    private IEnumerator RightClick() {
        _pointer.TapRight();
        yield return null;
        yield return null;
        yield return null;
    }

    private IEnumerator Wait(float seconds) {
        yield return new WaitForSecondsRealtime(seconds);
    }

    private IEnumerator ClickHere() {
        yield return Hold(true);
        yield return new WaitForSecondsRealtime(0.08f);
        yield return Hold(false);
    }

    private IEnumerator Hold(bool down) {
        _pointer.Press(down);
        if (_cursor != null) {
            _cursor.localScale = down ? Vector3.one * 0.86f : Vector3.one;
        }

        yield return null;
        yield return null;
    }

    private void Set(Vector2 position) {
        _position = position;
        _pointer.MoveTo(position);
        PlaceCursor();
    }

    private void PlaceCursor() {
        if (_cursor == null) {
            return;
        }

        Canvas canvas = GetComponent<Canvas>();
        _cursor.anchoredPosition = _position / canvas.scaleFactor;
        _cursor.SetAsLastSibling();
    }

    private static bool TryResolve(string target, out Vector2 screen) {
        screen = Vector2.zero;
        target = (target ?? string.Empty).Trim();
        int colon = target.IndexOf(':');
        string kind = colon > 0 ? target.Substring(0, colon).ToLowerInvariant() : string.Empty;
        string arg = colon > 0 ? target.Substring(colon + 1) : target;

        if (kind.Length == 0 && TryParsePair(target, out Vector2 reference)) {
            screen = new Vector2(reference.x / 1920f * Screen.width, (1f - reference.y / 1080f) * Screen.height);
            return true;
        }

        switch (kind) {
            case "node":
                return TryCenter(MapNode(ParseInt(arg)), out screen);
            case "item":
                foreach (DraggableItemUI item in Object.FindObjectsOfType<DraggableItemUI>()) {
                    if (item.isActiveAndEnabled && item.ItemData != null && item.ItemData.Name.Contains(arg)) {
                        return TryCenter(item.transform as RectTransform, out screen);
                    }
                }

                return false;
            case "cell":
                if (!TryParsePair(arg, out Vector2 cell)) {
                    return false;
                }

                foreach (GridSlotUI slot in Object.FindObjectsOfType<GridSlotUI>()) {
                    if (slot.isActiveAndEnabled && slot.X == (int)cell.x && slot.Y == (int)cell.y) {
                        return TryCenter(slot.transform as RectTransform, out screen);
                    }
                }

                return false;
            case "enemy":
                List<Transform> enemies = new List<Transform>();
                foreach (Image image in Object.FindObjectsOfType<Image>()) {
                    if (image.name == "EnemySprite_Image" && image.isActiveAndEnabled) {
                        enemies.Add(image.transform);
                    }
                }

                enemies.Sort((a, b) => a.position.x.CompareTo(b.position.x));
                int index = ParseInt(arg);
                return index >= 0 && index < enemies.Count && TryCenter(enemies[index] as RectTransform, out screen);
            case "pat":
                foreach (PortraitPatZone zone in Object.FindObjectsOfType<PortraitPatZone>()) {
                    if (zone.isActiveAndEnabled && zone.Region.ToString().ToLowerInvariant() == arg.ToLowerInvariant()
                        && TryCenter(zone.transform as RectTransform, out screen) && OnScreen(screen)) {
                        return true;
                    }
                }

                return false;
            case "grime":
                List<GrimeSpot> spots = new List<GrimeSpot>();
                foreach (GrimeSpot spot in Object.FindObjectsOfType<GrimeSpot>()) {
                    if (spot.isActiveAndEnabled) {
                        spots.Add(spot);
                    }
                }

                spots.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));
                int spotIndex = ParseInt(arg);
                return spotIndex >= 0 && spotIndex < spots.Count && TryCenter(spots[spotIndex].transform as RectTransform, out screen);
        }

        foreach (Button button in Object.FindObjectsOfType<Button>()) {
            Text text = button.GetComponentInChildren<Text>();
            if (button.isActiveAndEnabled && button.interactable && text != null && text.text.Replace("\n", " ").Contains(target)) {
                return TryCenter(button.transform as RectTransform, out screen);
            }
        }

        return TryCenter(FindActive(target), out screen);
    }

    // The map's node buttons, left to right then top to bottom.
    private static RectTransform MapNode(int index) {
        GameObject layout = GameObject.Find("MapLayout");
        if (layout == null) {
            return null;
        }

        List<RectTransform> nodes = new List<RectTransform>();
        foreach (Transform child in layout.transform) {
            if (child.gameObject.activeInHierarchy && child.GetComponent<Button>() != null) {
                nodes.Add((RectTransform)child);
            }
        }

        nodes.Sort((a, b) => Mathf.Abs(a.position.x - b.position.x) > 1f ? a.position.x.CompareTo(b.position.x) : b.position.y.CompareTo(a.position.y));
        return index >= 0 && index < nodes.Count ? nodes[index] : null;
    }

    private static RectTransform FindActive(string name) {
        foreach (RectTransform rect in Object.FindObjectsOfType<RectTransform>()) {
            if (rect.name == name && rect.gameObject.activeInHierarchy) {
                return rect;
            }
        }

        return null;
    }

    private static bool TryCenter(RectTransform rect, out Vector2 screen) {
        screen = Vector2.zero;
        if (rect == null) {
            return false;
        }

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera camera = canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.rootCanvas.worldCamera : null;
        screen = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
        return true;
    }

    private static bool OnScreen(Vector2 screen) {
        return screen.x >= 0f && screen.y >= 0f && screen.x <= Screen.width && screen.y <= Screen.height;
    }

    private static bool TryParsePair(string text, out Vector2 pair) {
        pair = Vector2.zero;
        string[] parts = text.Split(',');
        return parts.Length == 2
            && float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out pair.x)
            && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out pair.y);
    }

    private static int ParseInt(string text) {
        return int.TryParse(text, out int value) ? value : -1;
    }
}
