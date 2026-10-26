using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Glowing links where items in the backpack work on each other (GridSolver.AdjacencyLinks): a warm seam of light
// along every cell edge the two share, pulsing gently. It sits on the item layer above the items, takes no clicks, and
// rebuilds when items move, turn, arrive or leave.
public class AdjacencyLinkView : MonoBehaviour {
    private const float Step = InventoryDisplaySpec.CellSize + InventoryDisplaySpec.CellSpacing;
    private static readonly Color LinkColor = new Color(1f, 0.82f, 0.38f, 1f);
    private static Sprite _glow;

    private readonly List<Image> _marks = new List<Image>();
    private int _signature;

    // Tests and DevTools: how many item pairs are linked, and how many edge marks are drawn.
    public int LinkCount { get; private set; }
    public int MarkCount => _marks.Count;

    public static AdjacencyLinkView Ensure(Transform itemLayer) {
        if (itemLayer == null) {
            return null;
        }

        Transform existing = itemLayer.Find("AdjacencyLinks");
        GameObject root = existing != null ? existing.gameObject : new GameObject("AdjacencyLinks", typeof(RectTransform), typeof(CanvasGroup));
        if (existing == null) {
            root.transform.SetParent(itemLayer, false);
            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        AdjacencyLinkView view = root.GetComponent<AdjacencyLinkView>();
        return view != null ? view : root.AddComponent<AdjacencyLinkView>();
    }

    private void LateUpdate() {
        // Items spawned later land above it; it stays on top.
        if (transform.parent != null && transform.GetSiblingIndex() != transform.parent.childCount - 1) {
            transform.SetAsLastSibling();
        }

        BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        int signature = Signature(grid);
        if (signature != _signature) {
            _signature = signature;
            Rebuild(grid);
        }

        float alpha = 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 2.4f);
        foreach (Image mark in _marks) {
            mark.color = new Color(LinkColor.r, LinkColor.g, LinkColor.b, alpha);
        }
    }

    public void Rebuild(BackpackGrid grid) {
        foreach (Image mark in _marks) {
            if (mark != null) {
                Destroy(mark.gameObject);
            }
        }

        _marks.Clear();
        LinkCount = 0;
        if (grid == null) {
            return;
        }

        foreach (KeyValuePair<ItemEntity, ItemEntity> link in GridSolver.AdjacencyLinks(grid)) {
            LinkCount++;
            for (int x = 0; x < grid.Width; x++) {
                for (int y = 0; y < grid.Height; y++) {
                    if (grid.GetItemAt(x, y) != link.Key) {
                        continue;
                    }

                    TryMark(grid, link.Value, x, y, 1, 0);
                    TryMark(grid, link.Value, x, y, -1, 0);
                    TryMark(grid, link.Value, x, y, 0, 1);
                    TryMark(grid, link.Value, x, y, 0, -1);
                }
            }
        }
    }

    private void TryMark(BackpackGrid grid, ItemEntity other, int x, int y, int dx, int dy) {
        int nx = x + dx;
        int ny = y + dy;
        if (nx < 0 || ny < 0 || nx >= grid.Width || ny >= grid.Height || grid.GetItemAt(nx, ny) != other) {
            return;
        }

        // Cell (x, y) is centred here on the item layer, which is the size of the grid with its pivot in the middle.
        float width = grid.Width * Step - InventoryDisplaySpec.CellSpacing;
        float height = grid.Height * Step - InventoryDisplaySpec.CellSpacing;
        Vector2 centre = new Vector2(-width * 0.5f + x * Step + InventoryDisplaySpec.CellSize * 0.5f,
            height * 0.5f - y * Step - InventoryDisplaySpec.CellSize * 0.5f);
        Vector2 edge = centre + new Vector2(dx, -dy) * (Step * 0.5f);

        GameObject markObj = new GameObject("AdjacencyLink", typeof(RectTransform), typeof(Image));
        markObj.transform.SetParent(transform, false);
        Image mark = markObj.GetComponent<Image>();
        mark.sprite = GlowSprite();
        mark.raycastTarget = false;
        mark.color = LinkColor;
        RectTransform rect = mark.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = edge;
        // A glowing seam along the edge the two share.
        rect.sizeDelta = new Vector2(84f, 18f);
        rect.localRotation = Quaternion.Euler(0f, 0f, dx != 0 ? 90f : 0f);
        _marks.Add(mark);
    }

    private static int Signature(BackpackGrid grid) {
        if (grid?.ContainedItems == null) {
            return 0;
        }

        unchecked {
            int hash = 17 * 31 + grid.Width * 131 + grid.Height;
            foreach (ItemEntity item in grid.ContainedItems) {
                if (item?.Grid?.CurrentPos == null || item.Grid.CurrentPos.Length < 2) {
                    continue;
                }

                hash = hash * 31 + (item.InstanceID?.GetHashCode() ?? 0);
                hash = hash * 31 + item.Grid.CurrentPos[0] * 1000 + item.Grid.CurrentPos[1] * 10 + item.Grid.Rotation / 90;
            }

            return hash == 0 ? 1 : hash;
        }
    }

    // A soft capsule of light, made once at runtime.
    private static Sprite GlowSprite() {
        if (_glow != null) {
            return _glow;
        }

        const int w = 64;
        const int h = 24;
        Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "AdjacencyLinkGlow", wrapMode = TextureWrapMode.Clamp };
        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                float u = Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                float v = Mathf.Abs((y + 0.5f) / h * 2f - 1f);
                float d = Mathf.Sqrt(Mathf.Pow(Mathf.Max(0f, u - 0.35f) / 0.65f, 2f) + v * v);
                float a = Mathf.Clamp01(1f - d);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * (3f - 2f * a)));
            }
        }

        texture.Apply(false, true);
        _glow = Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
        return _glow;
    }
}
