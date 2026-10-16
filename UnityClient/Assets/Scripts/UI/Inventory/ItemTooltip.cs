using UnityEngine;
using UnityEngine.UI;

// One hover card for any item on screen (backpack, loot tray, stash, shop). The text comes from
// ItemPresentationRules.BuildTooltip; this class only draws the card beside the hovered element and keeps it on screen.
public class ItemTooltip : MonoBehaviour {
    // Above modals (540) and a held item (DraggableItemUI.HeldSortingOrder).
    public const int SortingOrder = 950;
    private const float Width = 400f;
    private const float Gap = 12f;

    private static ItemTooltip _instance;
    private static readonly Vector3[] Corners = new Vector3[4];

    private RectTransform _rect;
    private Text _title;
    private Text _body;
    private RectTransform _anchor;

    public static bool IsShowing => _instance != null && _instance.gameObject.activeSelf;
    public static string ShownTitle => IsShowing ? _instance._title.text : string.Empty;
    public static string ShownBody => IsShowing ? _instance._body.text : string.Empty;
    public static RectTransform ShownRect => IsShowing ? _instance._rect : null;

    public static void Show(ItemEntity item, RectTransform anchor) {
        if (item == null || anchor == null) {
            return;
        }

        ItemTooltip tooltip = Ensure(anchor);
        if (tooltip == null) {
            return;
        }

        ItemTooltipText text = ItemPresentationRules.BuildTooltip(item);
        tooltip._anchor = anchor;
        tooltip._title.text = text.Title;
        tooltip._body.text = text.Body;
        tooltip.gameObject.SetActive(true);
        tooltip.transform.SetAsLastSibling();
        tooltip.PlaceBeside(anchor);
    }

    // Hides the card only if it belongs to this element, so leaving one item never hides the next one's card.
    public static void HideFor(RectTransform anchor) {
        if (_instance != null && _instance._anchor == anchor) {
            Hide();
        }
    }

    public static void Hide() {
        if (_instance != null) {
            _instance._anchor = null;
            _instance.gameObject.SetActive(false);
        }
    }

    private void OnDestroy() {
        if (_instance == this) {
            _instance = null;
        }
    }

    private static ItemTooltip Ensure(RectTransform anchor) {
        Canvas anchorCanvas = anchor.GetComponentInParent<Canvas>();
        Canvas root = anchorCanvas != null ? anchorCanvas.rootCanvas : MainCanvas.Find();
        if (root == null) {
            return null;
        }

        if (_instance != null && _instance.transform.parent == root.transform) {
            return _instance;
        }

        if (_instance != null) {
            if (Application.isPlaying) {
                Destroy(_instance.gameObject);
            } else {
                DestroyImmediate(_instance.gameObject);
            }
        }

        GameObject obj = new GameObject("ItemTooltip", typeof(RectTransform));
        obj.transform.SetParent(root.transform, false);
        _instance = obj.AddComponent<ItemTooltip>();
        _instance.Build();
        return _instance;
    }

    private void Build() {
        _rect = (RectTransform)transform;
        _rect.anchorMin = new Vector2(0.5f, 0.5f);
        _rect.anchorMax = new Vector2(0.5f, 0.5f);
        _rect.pivot = new Vector2(0f, 1f);
        _rect.sizeDelta = new Vector2(Width, 0f);

        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = SortingOrder;

        Image background = gameObject.AddComponent<Image>();
        background.color = UITheme.Panel;
        background.raycastTarget = false;
        if (!UITheme.ApplyBoardSkin(background, VisualAssetService.UIPanelInfoID)) {
            UITheme.AddBorder(background, UITheme.Border, 2f);
        }

        VerticalLayoutGroup layout = gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 20, 22);
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _title = UITheme.CreateText(transform, "Title_Text", string.Empty, UITheme.BodySize + 2, UITheme.TextPrimary);
        _title.fontStyle = FontStyle.Bold;
        _body = UITheme.CreateText(transform, "Body_Text", string.Empty, UITheme.CaptionSize, UITheme.TextSecondary);
        _body.lineSpacing = 1.15f;
        gameObject.SetActive(false);
    }

    // To the right of the element, or to its left when that would leave the screen; top edges aligned and clamped.
    private void PlaceBeside(RectTransform anchor) {
        RectTransform canvasRect = (RectTransform)transform.parent;
        Canvas canvas = canvasRect.GetComponent<Canvas>();
        Camera eventCamera = canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        anchor.GetWorldCorners(Corners);
        Vector2 topLeft = ToCanvas(canvasRect, eventCamera, Corners[1]);
        Vector2 bottomRight = ToCanvas(canvasRect, eventCamera, Corners[3]);

        LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);
        Vector2 size = _rect.rect.size;
        Rect bounds = canvasRect.rect;

        float x = bottomRight.x + Gap;
        if (x + size.x > bounds.xMax) {
            x = topLeft.x - Gap - size.x;
        }

        x = Mathf.Clamp(x, bounds.xMin, Mathf.Max(bounds.xMin, bounds.xMax - size.x));
        float y = Mathf.Clamp(topLeft.y, Mathf.Min(bounds.yMax, bounds.yMin + size.y), bounds.yMax);
        _rect.localPosition = new Vector3(x, y, 0f);
    }

    private static Vector2 ToCanvas(RectTransform canvasRect, Camera eventCamera, Vector3 world) {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(eventCamera, world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, eventCamera, out Vector2 local);
        return local;
    }
}
