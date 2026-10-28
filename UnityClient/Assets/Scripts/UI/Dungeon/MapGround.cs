using System.Collections.Generic;
using UnityEngine;

// Where on a layer's painted terrain a route node may stand: a black-and-white mask painted over the terrain
// (map_terrain_layer_N_ground, white = open ground, already shrunk by about a node's half size so a node on white sits
// on the ground). The map lays its columns over the ground's horizontal span, centres each column on the ground under
// it, and moves any node still off the ground to the nearest free ground spot near its column. Positions are in map
// world space: centred, y up; the mask covers the whole world (the terrain is stretched to it).
public class MapGround {
    private readonly bool[] _cells;
    private readonly int _width;
    private readonly int _height;
    private readonly float[] _centerV;
    private readonly bool[] _columnHasGround;

    public float MinU { get; }
    public float MaxU { get; }

    public MapGround(bool[] cells, int width, int height) {
        _cells = cells;
        _width = width;
        _height = height;
        _centerV = new float[width];
        _columnHasGround = new bool[width];
        int first = -1;
        int last = -1;
        for (int x = 0; x < width; x++) {
            float sum = 0f;
            int count = 0;
            for (int y = 0; y < height; y++) {
                if (cells[y * width + x]) {
                    sum += y;
                    count++;
                }
            }

            _columnHasGround[x] = count > 0;
            _centerV[x] = count > 0 ? (sum / count + 0.5f) / height : 0.5f;
            // The span counts only columns with real ground in them, not a sliver of walkway at the edge.
            if (count >= height / 20) {
                first = first < 0 ? x : first;
                last = x;
            }
        }

        MinU = first < 0 ? 0f : (first + 0.5f) / width;
        MaxU = last < 0 ? 1f : (last + 0.5f) / width;
    }

    public bool HasGround => MaxU > MinU;

    // The mask's red channel above half is ground; null when the texture cannot be read.
    public static MapGround FromTexture(Texture2D texture) {
        if (texture == null || !texture.isReadable) {
            return null;
        }

        Color32[] pixels = texture.GetPixels32();
        bool[] cells = new bool[pixels.Length];
        for (int i = 0; i < pixels.Length; i++) {
            cells[i] = pixels[i].r > 127;
        }

        return new MapGround(cells, texture.width, texture.height);
    }

    public bool IsGround(float u, float v) {
        int x = Mathf.FloorToInt(u * _width);
        int y = Mathf.FloorToInt(v * _height);
        return x >= 0 && y >= 0 && x < _width && y < _height && _cells[y * _width + x];
    }

    // The middle of the ground under u (averaged over a little band), or 0.5 when there is none near.
    public float CenterV(float u, float bandU = 0.03f) {
        int from = Mathf.Clamp(Mathf.FloorToInt((u - bandU) * _width), 0, _width - 1);
        int to = Mathf.Clamp(Mathf.FloorToInt((u + bandU) * _width), 0, _width - 1);
        float sum = 0f;
        int count = 0;
        for (int x = from; x <= to; x++) {
            if (_columnHasGround[x]) {
                sum += _centerV[x];
                count++;
            }
        }

        return count > 0 ? sum / count : 0.5f;
    }

    // Moves each position off the ground to the nearest ground spot at most maxShiftX away sideways (any distance up or
    // down), at least minGap from the nodes already settled; sideways moves cost twice as much, so columns stay
    // columns. Nodes are settled left to right; one that finds no spot keeps its place.
    public void Settle<T>(Dictionary<T, Vector2> positions, Vector2 worldSize, float maxShiftX, float minGap, float marginY) {
        List<KeyValuePair<T, Vector2>> order = new List<KeyValuePair<T, Vector2>>(positions);
        order.Sort((a, b) => Mathf.Abs(a.Value.x - b.Value.x) > 1f ? a.Value.x.CompareTo(b.Value.x) : b.Value.y.CompareTo(a.Value.y));
        List<Vector2> settled = new List<Vector2>();
        float cellW = worldSize.x / _width;
        float cellH = worldSize.y / _height;
        int reachX = Mathf.CeilToInt(maxShiftX / cellW);
        int minY = Mathf.Clamp(Mathf.CeilToInt(marginY / cellH), 0, _height - 1);
        int maxY = Mathf.Clamp(_height - 1 - minY, minY, _height - 1);
        foreach (KeyValuePair<T, Vector2> entry in order) {
            Vector2 p = entry.Value;
            int cx = Mathf.FloorToInt((p.x / worldSize.x + 0.5f) * _width);
            int cy = Mathf.FloorToInt((p.y / worldSize.y + 0.5f) * _height);
            if (InGround(cx, cy) && cy >= minY && cy <= maxY && Clear(p, settled, minGap)) {
                settled.Add(p);
                continue;
            }

            // On free ground if there is any near; otherwise at least clear of the others.
            if (!TryNearest(p, cx, reachX, minY, maxY, cellW, cellH, worldSize, settled, minGap, true, out Vector2 bestPoint)
                && !TryNearest(p, cx, reachX, minY, maxY, cellW, cellH, worldSize, settled, minGap, false, out bestPoint)) {
                bestPoint = p;
            }

            positions[entry.Key] = bestPoint;
            settled.Add(bestPoint);
        }
    }

    private bool TryNearest(Vector2 p, int cx, int reachX, int minY, int maxY, float cellW, float cellH, Vector2 worldSize,
        List<Vector2> settled, float minGap, bool onGround, out Vector2 bestPoint) {
        float best = float.MaxValue;
        bestPoint = p;
        for (int x = Mathf.Max(0, cx - reachX); x <= Mathf.Min(_width - 1, cx + reachX); x++) {
            for (int y = minY; y <= maxY; y++) {
                if (onGround && !_cells[y * _width + x]) {
                    continue;
                }

                Vector2 candidate = new Vector2((x + 0.5f) * cellW - worldSize.x * 0.5f, (y + 0.5f) * cellH - worldSize.y * 0.5f);
                float dx = (candidate.x - p.x) * 2f;
                float dy = candidate.y - p.y;
                float score = dx * dx + dy * dy;
                if (score < best && Clear(candidate, settled, minGap)) {
                    best = score;
                    bestPoint = candidate;
                }
            }
        }

        return best < float.MaxValue;
    }

    private bool InGround(int x, int y) {
        return x >= 0 && y >= 0 && x < _width && y < _height && _cells[y * _width + x];
    }

    private static bool Clear(Vector2 point, List<Vector2> others, float minGap) {
        float gap2 = minGap * minGap;
        foreach (Vector2 other in others) {
            if ((other - point).sqrMagnitude < gap2) {
                return false;
            }
        }

        return true;
    }
}
