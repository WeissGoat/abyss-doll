using System.Collections.Generic;
using UnityEngine;

// Where things are on one standing portrait (配置表(JSON)/Portraits, one file per portrait VisualID): the pivots her
// puppet turns on (UIPortraitPuppet), where she answers a touch (ZeroPresence) and where marks sit on her
// (StudioCareView). Points are (x, y) fractions of the portrait from its bottom-left, areas (x, y, width, height),
// heights y fractions. A new portrait gets its own file; the numbers come from the art tools' previews
// (美术文档/21_战斗姿势与动态素材工作流.md, 纸偶立绘).
public class PortraitProfile {
    public string VisualID;
    // The point her head turns on (under the chin), the middle of her cheeks (the blush), and a point on her
    // reaching arm (the drawn arm turns with her there).
    public float[] Neck;
    public float[] Cheeks;
    // The blush's width and height as fractions of the portrait's width: its two patches sit at a fifth of the width
    // in from either end, so the width sets how far apart they are (on the cheeks, clear of the mouth).
    public float[] BlushSize;
    public float[] HoldAnchor;
    // Her lean starts at KneeV (just under the skirt) and is whole from WaistV up.
    public float KneeV;
    public float WaistV;
    // Her hanging hair follows her head at once from HairLag[0] and late at HairLag[1] (the tips).
    public float[] HairLag;
    public float[] HeadArea;
    public float[] ShoulderArea;
    public float[] HandArea;
    // On her cape, dress, skirt and legs, never her face: as many as CareService can put on her.
    public List<float[]> GrimeSpots = new List<float[]>();
    public List<float[]> CrackSpots = new List<float[]>();
    public List<float[]> HazeSpots = new List<float[]>();

    public static PortraitProfile Find(string visualID) {
        return !string.IsNullOrEmpty(visualID) && ConfigManager.Portraits.TryGetValue(visualID, out PortraitProfile profile) ? profile : null;
    }

    public static Vector2 Point(float[] xy) {
        return xy != null && xy.Length >= 2 ? new Vector2(xy[0], xy[1]) : Vector2.zero;
    }

    public static Rect Area(float[] area) {
        return area != null && area.Length >= 4 ? new Rect(area[0], area[1], area[2], area[3]) : Rect.zero;
    }

    // The index-th spot of a list, or null past its end.
    public static Vector2? Spot(List<float[]> spots, int index) {
        return spots != null && index < spots.Count ? Point(spots[index]) : (Vector2?)null;
    }
}
