using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// While a weapon waits for its target (ItemUseService), a dotted arc of light runs from the weapon's tile in the
// backpack (or from the bare-hands button) to the pointer, with a head at the pointer. It draws above the satchel and
// the stage and takes no clicks.
public class CombatTargetArrow : MonoBehaviour {
    private const int DotCount = 18;
    private const int SortingOrder = 800;
    private static readonly Color ArcColor = new Color(1f, 0.84f, 0.42f, 1f);
    private static Sprite _dot;

    private readonly Image[] _dots = new Image[DotCount];
    private Image[] _head;
    private Func<RectTransform> _fallbackOrigin;
    private RectTransform _rect;
    private string _originItemID;
    private RectTransform _originTile;

    public bool Showing { get; private set; }
    // Tests and DevTools aim it without a real mouse (screen pixels); null follows the pointer.
    public static Vector2? PointerOverride;

    public static CombatTargetArrow Ensure(Transform parent, Func<RectTransform> fallbackOrigin) {
        Transform existing = parent.Find("CombatTargetArrow");
        GameObject root = existing != null
            ? existing.gameObject
            : new GameObject("CombatTargetArrow", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        if (existing == null) {
            root.transform.SetParent(parent, false);
        }

        CombatTargetArrow arrow = root.GetComponent<CombatTargetArrow>();
        if (arrow == null) {
            arrow = root.AddComponent<CombatTargetArrow>();
        }

        arrow._fallbackOrigin = fallbackOrigin;
        arrow.Build();
        return arrow;
    }

    private void Build() {
        _rect = (RectTransform)transform;
        _rect.anchorMin = Vector2.zero;
        _rect.anchorMax = Vector2.one;
        _rect.offsetMin = Vector2.zero;
        _rect.offsetMax = Vector2.zero;
        Canvas canvas = GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = SortingOrder;
        CanvasGroup group = GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        for (int i = 0; i < DotCount; i++) {
            if (_dots[i] == null) {
                _dots[i] = NewImage($"ArcDot_{i}");
            }
        }

        if (_head == null) {
            _head = new[] { NewImage("ArcHead_L"), NewImage("ArcHead_R") };
        }

        Hide();
    }

    private Image NewImage(string objectName) {
        GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(transform, false);
        Image image = obj.GetComponent<Image>();
        image.sprite = DotSprite();
        image.raycastTarget = false;
        image.color = ArcColor;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        return image;
    }

    private void LateUpdate() {
        if (!ItemUseService.HasPendingEnemyTargetSelection) {
            Hide();
            return;
        }

        RectTransform origin = ResolveOrigin();
        if (origin == null) {
            Hide();
            return;
        }

        BaseInput input = EventSystem.current != null && EventSystem.current.currentInputModule != null
            ? EventSystem.current.currentInputModule.input
            : null;
        Vector2 pointer = PointerOverride ?? (input != null ? input.mousePosition : (Vector2)Input.mousePosition);
        Vector3[] corners = new Vector3[4];
        origin.GetWorldCorners(corners);
        Vector2 start = _rect.InverseTransformPoint((corners[0] + corners[2]) * 0.5f);
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, pointer, null, out Vector2 end)) {
            Hide();
            return;
        }

        Draw(start, end);
    }

    private RectTransform ResolveOrigin() {
        string itemID = ItemUseService.PendingTargetItemInstanceID;
        if (itemID != _originItemID || _originTile == null) {
            _originItemID = itemID;
            _originTile = null;
            if (!string.IsNullOrEmpty(itemID)) {
                foreach (DraggableItemUI tile in FindObjectsOfType<DraggableItemUI>()) {
                    if (tile.ItemData != null && tile.ItemData.InstanceID == itemID) {
                        _originTile = (RectTransform)tile.transform;
                        break;
                    }
                }
            }
        }

        return _originTile != null ? _originTile : _fallbackOrigin?.Invoke();
    }

    // A quadratic curve bowing upward, dots growing toward the end, and a chevron at the pointer.
    private void Draw(Vector2 start, Vector2 end) {
        Showing = true;
        float distance = Vector2.Distance(start, end);
        Vector2 control = (start + end) * 0.5f + Vector2.up * Mathf.Clamp(distance * 0.35f, 60f, 260f);
        float pulse = Mathf.Repeat(Time.unscaledTime * 1.6f, 1f);
        for (int i = 0; i < DotCount; i++) {
            float t = (i + 1f) / (DotCount + 1f);
            Vector2 p = Bezier(start, control, end, t);
            Image dot = _dots[i];
            dot.gameObject.SetActive(true);
            float size = Mathf.Lerp(16f, 30f, t);
            dot.rectTransform.sizeDelta = new Vector2(size, size);
            dot.rectTransform.anchoredPosition = p;
            // A brighter wave runs from the weapon toward the target.
            float wave = 1f - Mathf.Clamp01(Mathf.Abs(t - pulse) * 5f);
            dot.color = new Color(ArcColor.r, ArcColor.g, ArcColor.b, 0.8f + 0.2f * wave);
        }

        Vector2 tangent = (end - Bezier(start, control, end, 0.94f)).normalized;
        float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
        for (int side = 0; side < 2; side++) {
            Image arm = _head[side];
            arm.gameObject.SetActive(true);
            RectTransform rect = arm.rectTransform;
            rect.sizeDelta = new Vector2(60f, 22f);
            float armAngle = angle + (side == 0 ? 145f : -145f);
            Vector2 direction = new Vector2(Mathf.Cos(armAngle * Mathf.Deg2Rad), Mathf.Sin(armAngle * Mathf.Deg2Rad));
            rect.anchoredPosition = end + direction * 22f;
            rect.localRotation = Quaternion.Euler(0f, 0f, armAngle);
            arm.color = ArcColor;
        }
    }

    private static Vector2 Bezier(Vector2 a, Vector2 control, Vector2 b, float t) {
        float u = 1f - t;
        return u * u * a + 2f * u * t * control + t * t * b;
    }

    private void Hide() {
        if (!Showing && _dots[0] != null && !_dots[0].gameObject.activeSelf) {
            return;
        }

        Showing = false;
        foreach (Image dot in _dots) {
            if (dot != null) {
                dot.gameObject.SetActive(false);
            }
        }

        if (_head != null) {
            foreach (Image arm in _head) {
                arm.gameObject.SetActive(false);
            }
        }
    }

    // A soft round spot of light, made once at runtime.
    private static Sprite DotSprite() {
        if (_dot != null) {
            return _dot;
        }

        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "CombatTargetArrowDot", wrapMode = TextureWrapMode.Clamp };
        for (int x = 0; x < size; x++) {
            for (int y = 0; y < size; y++) {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size * 0.5f, size * 0.5f)) / (size * 0.5f);
                float a = Mathf.Clamp01(1f - d);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a * 1.8f) * a));
            }
        }

        texture.Apply(false, true);
        _dot = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _dot;
    }
}
