using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Trigger chains on screen (GDD_01 §5.1A), plus the monsters' marks on the backpack (§6.2): violet crystal over sealed
// cells and a green haze over items fouled with spores. Each item tile carries a small notch on every port, amber for an out port
// and teal for an in port; they turn and travel with the tile while it is dragged (MarkPorts). On the item layer this
// view draws a bright seam wherever an out port meets an in port, and while a chain start's card is up it lights the
// cells of every module its use would fire. Takes no clicks; rebuilds when items move, turn, arrive or leave.
public class ChainPortView : MonoBehaviour {
    private const float Step = InventoryDisplaySpec.CellSize + InventoryDisplaySpec.CellSpacing;
    public static readonly Color OutColor = new Color(1f, 0.62f, 0.22f, 1f);
    public static readonly Color InColor = new Color(0.36f, 0.86f, 0.92f, 1f);
    private static readonly Color LinkColor = new Color(0.62f, 0.95f, 1f, 1f);
    private static readonly Color LitColor = new Color(0.45f, 0.9f, 1f, 0.28f);
    private static Sprite _pip;
    private static Sprite _cell;

    private readonly List<Image> _links = new List<Image>();
    private readonly List<Image> _lit = new List<Image>();
    private readonly List<Image> _marks = new List<Image>();
    private static readonly Color CrystalColor = new Color(0.72f, 0.55f, 1f, 0.55f);
    private static readonly Color SporeColor = new Color(0.45f, 0.85f, 0.3f, 0.35f);
    private int _signature;
    private ItemEntity _litStart;

    // Tests and DevTools.
    public int LinkCount => _links.Count;
    public int LitCellCount => _lit.Count;

    public static ChainPortView Ensure(Transform itemLayer) {
        if (itemLayer == null) {
            return null;
        }

        Transform existing = itemLayer.Find("ChainLinks");
        GameObject root = existing != null ? existing.gameObject : new GameObject("ChainLinks", typeof(RectTransform), typeof(CanvasGroup));
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

        ChainPortView view = root.GetComponent<ChainPortView>();
        return view != null ? view : root.AddComponent<ChainPortView>();
    }

    private void LateUpdate() {
        if (transform.parent != null && transform.GetSiblingIndex() != transform.parent.childCount - 1) {
            transform.SetAsLastSibling();
        }

        BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        ItemEntity hovered = ItemTooltip.ShownItem;
        ItemEntity start = hovered != null && grid != null && grid.ContainedItems.Contains(hovered) && TriggerChain.HasOutPort(hovered) ? hovered : null;
        int signature = Signature(grid);
        if (signature != _signature || start != _litStart) {
            _signature = signature;
            _litStart = start;
            Rebuild(grid, start);
        }

        float alpha = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 4f);
        foreach (Image link in _links) {
            link.color = new Color(LinkColor.r, LinkColor.g, LinkColor.b, alpha);
        }
    }

    public void Rebuild(BackpackGrid grid, ItemEntity litStart) {
        Clear(_links);
        Clear(_lit);
        Clear(_marks);
        if (grid == null) {
            return;
        }

        foreach (int[] cell in grid.CrystalCells()) {
            Image crystal = NewImage("CrystalCell", CellSprite(), CrystalColor);
            crystal.rectTransform.anchoredPosition = CellCentre(grid, cell[0], cell[1]);
            crystal.rectTransform.sizeDelta = new Vector2(InventoryDisplaySpec.CellSize, InventoryDisplaySpec.CellSize);
            _marks.Add(crystal);
        }

        for (int x = 0; x < grid.Width; x++) {
            for (int y = 0; y < grid.Height; y++) {
                ItemEntity item = grid.GetItemAt(x, y);
                if (item != null && CombatItemEffects.Pollution(item) > 0) {
                    Image spores = NewImage("SporeCell", CellSprite(), SporeColor);
                    spores.rectTransform.anchoredPosition = CellCentre(grid, x, y);
                    spores.rectTransform.sizeDelta = new Vector2(InventoryDisplaySpec.CellSize, InventoryDisplaySpec.CellSize);
                    _marks.Add(spores);
                }
            }
        }

        foreach (ChainLink link in TriggerChain.AllLinks(grid)) {
            Vector2 edge = CellCentre(grid, link.Out.X, link.Out.Y) + Offset(link.Out.Side) * (Step * 0.5f);
            Image seam = NewImage("ChainLink", PipSprite(), LinkColor);
            RectTransform rect = seam.rectTransform;
            rect.anchoredPosition = edge;
            rect.sizeDelta = new Vector2(58f, 20f);
            rect.localRotation = Quaternion.Euler(0f, 0f, IsSideways(link.Out.Side) ? 90f : 0f);
            _links.Add(seam);
        }

        if (litStart == null) {
            return;
        }

        foreach (ChainStep step in TriggerChain.Plan(litStart, grid)) {
            bool fires = TriggerChain.CanFire(step.Item, out _);
            for (int x = 0; x < grid.Width; x++) {
                for (int y = 0; y < grid.Height; y++) {
                    if (grid.GetItemAt(x, y) != step.Item) {
                        continue;
                    }

                    Image cell = NewImage("ChainLit", CellSprite(), fires ? LitColor : new Color(0.9f, 0.3f, 0.25f, 0.25f));
                    cell.rectTransform.anchoredPosition = CellCentre(grid, x, y);
                    cell.rectTransform.sizeDelta = new Vector2(InventoryDisplaySpec.CellSize, InventoryDisplaySpec.CellSize);
                    _lit.Add(cell);
                }
            }
        }
    }

    // The notches on one item tile, at each port's edge for the tile's current rotation (the tile's pivot is the
    // centre of its top-left cell, InventoryDisplaySpec.ResolveItemPivot).
    public static void MarkPorts(RectTransform tile, ItemEntity item) {
        if (tile == null) {
            return;
        }

        for (int i = tile.childCount - 1; i >= 0; i--) {
            Transform child = tile.GetChild(i);
            if (child.name == "Port_In" || child.name == "Port_Out") {
                Object.Destroy(child.gameObject);
            }
        }

        if (!TriggerChain.HasPorts(item)) {
            return;
        }

        int rotation = item.Grid != null ? BackpackGrid.NormalizeRotation(item.Grid.Rotation) : 0;
        foreach (PlacedPort port in TriggerChain.PortsAt(item, 0, 0, rotation)) {
            GameObject mark = new GameObject(port.IsOut ? "Port_Out" : "Port_In", typeof(RectTransform), typeof(Image));
            mark.transform.SetParent(tile, false);
            Image image = mark.GetComponent<Image>();
            image.sprite = PipSprite();
            image.color = port.IsOut ? OutColor : InColor;
            image.raycastTarget = false;
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = tile.pivot;
            rect.pivot = new Vector2(0.5f, 0.5f);
            Vector2 centre = new Vector2(port.X * InventoryDisplaySpec.CellSize, -port.Y * InventoryDisplaySpec.CellSize);
            rect.anchoredPosition = centre + Offset(port.Side) * (InventoryDisplaySpec.CellSize * 0.5f - 9f);
            rect.sizeDelta = port.IsOut ? new Vector2(40f, 16f) : new Vector2(32f, 12f);
            rect.localRotation = Quaternion.Euler(0f, 0f, IsSideways(port.Side) ? 90f : 0f);
            Outline outline = mark.AddComponent<Outline>();
            outline.effectColor = new Color(0.08f, 0.05f, 0.03f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }
    }

    private Image NewImage(string name, Sprite sprite, Color color) {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(transform, false);
        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        return image;
    }

    private static void Clear(List<Image> images) {
        foreach (Image image in images) {
            if (image != null) {
                Object.Destroy(image.gameObject);
            }
        }

        images.Clear();
    }

    // Cell (x, y) centred on the item layer, which is the size of the grid with its pivot in the middle.
    private static Vector2 CellCentre(BackpackGrid grid, int x, int y) {
        float width = grid.Width * Step - InventoryDisplaySpec.CellSpacing;
        float height = grid.Height * Step - InventoryDisplaySpec.CellSpacing;
        return new Vector2(-width * 0.5f + x * Step + InventoryDisplaySpec.CellSize * 0.5f, height * 0.5f - y * Step - InventoryDisplaySpec.CellSize * 0.5f);
    }

    // Screen direction of a side: grid y grows downward, the screen's upward.
    private static Vector2 Offset(TargetDirection side) {
        switch (side) {
            case TargetDirection.Right: return Vector2.right;
            case TargetDirection.Left: return Vector2.left;
            case TargetDirection.Up: return Vector2.up;
            default: return Vector2.down;
        }
    }

    private static bool IsSideways(TargetDirection side) {
        return side == TargetDirection.Left || side == TargetDirection.Right;
    }

    private static int Signature(BackpackGrid grid) {
        if (grid?.ContainedItems == null) {
            return 0;
        }

        unchecked {
            int hash = 23 * 31 + grid.Width * 131 + grid.Height;
            foreach (ItemEntity item in grid.ContainedItems) {
                if (item?.Grid?.CurrentPos == null || item.Grid.CurrentPos.Length < 2 || (!TriggerChain.HasPorts(item) && CombatItemEffects.Pollution(item) <= 0)) {
                    continue;
                }

                hash = hash * 31 + (item.InstanceID?.GetHashCode() ?? 0);
                hash = hash * 31 + item.Grid.CurrentPos[0] * 1000 + item.Grid.CurrentPos[1] * 10 + item.Grid.Rotation / 90;
            }

            // A spent module or a jam changes what a start would fire; crystal and spores come and go.
            hash = hash * 31 + (TriggerChain.State?.Jammed == true ? 7 : 0);
            foreach (int[] cell in grid.CrystalCells()) {
                hash = hash * 31 + cell[0] * 100 + cell[1] + 1;
            }

            foreach (ItemEntity item in grid.ContainedItems) {
                if (CombatItemEffects.Pollution(item) > 0) {
                    hash = hash * 31 + (item.InstanceID?.GetHashCode() ?? 0) + 3;
                }
            }
            return hash == 0 ? 1 : hash;
        }
    }

    // A small rounded bar, made once at runtime.
    private static Sprite PipSprite() {
        if (_pip != null) {
            return _pip;
        }

        const int w = 48;
        const int h = 16;
        Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { name = "ChainPortPip", wrapMode = TextureWrapMode.Clamp };
        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                float u = Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                float v = Mathf.Abs((y + 0.5f) / h * 2f - 1f);
                float d = Mathf.Sqrt(Mathf.Pow(Mathf.Max(0f, u - 0.6f) / 0.4f, 2f) + v * v);
                float a = Mathf.Clamp01((1f - d) * 3f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        texture.Apply(false, true);
        _pip = Sprite.Create(texture, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
        return _pip;
    }

    // A soft square for a lit cell.
    private static Sprite CellSprite() {
        if (_cell != null) {
            return _cell;
        }

        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "ChainLitCell", wrapMode = TextureWrapMode.Clamp };
        for (int x = 0; x < size; x++) {
            for (int y = 0; y < size; y++) {
                float u = Mathf.Abs((x + 0.5f) / size * 2f - 1f);
                float v = Mathf.Abs((y + 0.5f) / size * 2f - 1f);
                float edge = Mathf.Max(u, v);
                float a = Mathf.Lerp(0.55f, 1f, Mathf.Clamp01((edge - 0.7f) / 0.3f));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        texture.Apply(false, true);
        _cell = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _cell;
    }
}
