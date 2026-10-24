using UnityEngine;
using UnityEngine.UI;

// Zero's large portrait (doll_zero_bust, one image per face: doll_zero_bust_<face>), shown wherever she talks to you
// up close: the night talk, 和她说话, the arrival page and the story scenes. A face without its image falls back to the
// plain one.
public static class ZeroBust {
    public const string VisualID = "doll_zero_bust";
    // Her size up close, standing on the bottom edge.
    public static readonly Vector2 Size = new Vector2(720f, 920f);

    public static bool HasArt => VisualAssetService.TryGetSprite(VisualID, out _);

    public static string VisualIDFor(string face) {
        if (string.IsNullOrEmpty(face) || face == ZeroFace.Neutral || !VisualAssetService.TryGetSprite(VisualID + "_" + face, out _)) {
            return VisualID;
        }

        return VisualID + "_" + face;
    }

    // Puts her with this face on the image and returns the face shown (ZeroFace), or empty when there is no portrait
    // art; the image is hidden then.
    public static string Apply(Image image, string face, Color tint) {
        string visualID = VisualIDFor(face);
        bool shown = VisualUIHelper.ApplySprite(image, visualID, tint, Color.clear);
        image.gameObject.SetActive(shown);
        if (!shown) {
            return string.Empty;
        }

        return visualID == VisualID ? ZeroFace.Neutral : visualID.Substring(VisualID.Length + 1);
    }
}
