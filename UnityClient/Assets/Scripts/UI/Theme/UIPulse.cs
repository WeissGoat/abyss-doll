using UnityEngine;

// Gently pulses an object's opacity (through a CanvasGroup) and optionally its scale, to say "you can act here".
// Unscaled time, so it keeps moving while the game is paused for dialogue.
public class UIPulse : MonoBehaviour {
    public float MinAlpha = 0.35f;
    public float MaxAlpha = 1f;
    public float ScaleAmount = 0.04f;
    public float Speed = 2.6f;

    private CanvasGroup _group;
    private Vector3 _baseScale = Vector3.one;

    public static UIPulse Attach(GameObject target, float minAlpha, float maxAlpha, float scaleAmount, float speed) {
        UIPulse pulse = target.GetComponent<UIPulse>();
        if (pulse == null) {
            pulse = target.AddComponent<UIPulse>();
        }

        pulse.MinAlpha = minAlpha;
        pulse.MaxAlpha = maxAlpha;
        pulse.ScaleAmount = scaleAmount;
        pulse.Speed = speed;
        return pulse;
    }

    private void Awake() {
        _group = GetComponent<CanvasGroup>();
        if (_group == null) {
            _group = gameObject.AddComponent<CanvasGroup>();
        }

        _group.blocksRaycasts = false;
        _baseScale = transform.localScale;
    }

    private void Update() {
        float wave = (Mathf.Sin(Time.unscaledTime * Speed) + 1f) * 0.5f;
        _group.alpha = Mathf.Lerp(MinAlpha, MaxAlpha, wave);
        transform.localScale = _baseScale * (1f + ScaleAmount * wave);
    }
}
