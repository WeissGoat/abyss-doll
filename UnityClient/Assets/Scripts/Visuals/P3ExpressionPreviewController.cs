using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class P3ExpressionPreviewController : MonoBehaviour {
    private const float ReferenceWidth = 1920f;
    private const float ReferenceHeight = 1080f;
    private const string DollStandID = "doll_proto_0_stand";
    private const string WorkshopHomeRoomID = "bg_workshop_home_room";
    private const string RustHoundCombatID = "monster_mob_rust_hound_combat";
    private const string CombatHitID = "ui_combat_feedback_hit";
    private const string ShieldBreakID = "ui_combat_feedback_shield_break";

    private readonly List<GameObject> _transientObjects = new List<GameObject>();

    private Sprite _rectSprite;
    private Sprite _circleSprite;
    private Sprite _formalDollSprite;
    private Sprite _formalBackgroundSprite;
    private Sprite _formalEnemySprite;
    private Sprite _formalHitSprite;
    private Sprite _formalShieldBreakSprite;
    private RectTransform _dollRoot;
    private RectTransform _head;
    private RectTransform _body;
    private RectTransform _coreLight;
    private RectTransform _sanVeil;
    private Image _screenFlash;
    private Image _statusLabelPanel;
    private Text _statusLabel;
    private float _startTime;

    private void Awake() {
        _rectSprite = CreateRectSprite();
        _circleSprite = CreateCircleSprite(96);
        _formalDollSprite = ResolveFormalSprite(DollStandID);
        _formalBackgroundSprite = ResolveFormalSprite(WorkshopHomeRoomID);
        _formalEnemySprite = ResolveFormalSprite(RustHoundCombatID);
        _formalHitSprite = ResolveFormalSprite(CombatHitID);
        _formalShieldBreakSprite = ResolveFormalSprite(ShieldBreakID);
    }

    private void Start() {
        _startTime = Time.time;
        BuildPreview();
        StartCoroutine(DemoLoop());
    }

    private void Update() {
        if (_dollRoot == null) {
            return;
        }

        float t = Time.time - _startTime;
        float breath = 1f + Mathf.Sin(t * 2.1f) * 0.018f;
        _body.localScale = new Vector3(1f, breath, 1f);
        if (_head != null) {
            _head.anchoredPosition = new Vector2(Mathf.Sin(t * 0.9f) * 7f, 264f + Mathf.Sin(t * 1.4f) * 4f);
        }
        _coreLight.localScale = Vector3.one * (1f + Mathf.Sin(t * 4.4f) * 0.14f);

        float sanNoise = 0.16f + Mathf.Abs(Mathf.Sin(t * 3.3f)) * 0.13f;
        Color veil = new Color(0.17f, 0.05f, 0.25f, sanNoise);
        GetImage(_sanVeil).color = veil;
    }

    private void BuildPreview() {
        Canvas canvas = CreateCanvas();
        RectTransform root = canvas.transform as RectTransform;

        Image background = CreateImage("Bg_Formal_Workshop", root, Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight), Color.white, _formalBackgroundSprite);
        if (_formalBackgroundSprite == null) {
            background.color = new Color(0.045f, 0.038f, 0.055f, 1f);
        }
        CreateImage("Bg_Readability_Veil", root, Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight), new Color(0.02f, 0.015f, 0.025f, 0.46f));
        CreateImage("Bg_Warm_Lamp", root, new Vector2(-520f, 140f), new Vector2(780f, 780f), new Color(0.55f, 0.34f, 0.12f, 0.12f), _circleSprite);
        CreateImage("Bg_Blue_Core", root, new Vector2(540f, -70f), new Vector2(760f, 760f), new Color(0.08f, 0.45f, 0.72f, 0.12f), _circleSprite);
        CreateImage("Floor_Shadow", root, new Vector2(0f, -365f), new Vector2(1280f, 80f), new Color(0f, 0f, 0f, 0.34f), _circleSprite);

        BuildDoll(root);
        BuildEnemy(root);
        if (_formalBackgroundSprite == null) {
            BuildRepairRig(root);
        }
        BuildHud(root);

        _screenFlash = CreateImage("Screen_Flash", root, Vector2.zero, new Vector2(ReferenceWidth, ReferenceHeight), new Color(1f, 0.96f, 0.88f, 0f));
        _screenFlash.raycastTarget = false;
    }

    private Canvas CreateCanvas() {
        GameObject canvasObject = new GameObject("P3_Expression_Preview_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rect = canvasObject.transform as RectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return canvas;
    }

    private void BuildDoll(RectTransform root) {
        GameObject dollObject = new GameObject("PseudoLive2D_Doll", typeof(RectTransform));
        dollObject.transform.SetParent(root, false);
        _dollRoot = dollObject.GetComponent<RectTransform>();
        _dollRoot.anchorMin = new Vector2(0.5f, 0.5f);
        _dollRoot.anchorMax = new Vector2(0.5f, 0.5f);
        _dollRoot.anchoredPosition = new Vector2(-410f, -75f);
        _dollRoot.sizeDelta = new Vector2(520f, 760f);

        CreateImage("Doll_Shadow", _dollRoot, new Vector2(0f, -342f), new Vector2(420f, 72f), new Color(0f, 0f, 0f, 0.38f), _circleSprite);
        if (_formalDollSprite != null) {
            Image formalDoll = CreateImage("Doll_Formal_Stand", _dollRoot, new Vector2(0f, -4f), new Vector2(420f, 720f), Color.white, _formalDollSprite);
            formalDoll.preserveAspect = true;
            _body = formalDoll.rectTransform;
            _head = null;
            _coreLight = CreateImage("Doll_Core_Light", _dollRoot, new Vector2(2f, 116f), new Vector2(84f, 84f), new Color(0.32f, 0.92f, 1f, 0.58f), _circleSprite).rectTransform;
            CreateImage("Doll_Core_Glow", _dollRoot, new Vector2(2f, 116f), new Vector2(190f, 190f), new Color(0.20f, 0.70f, 1f, 0.14f), _circleSprite);
            _sanVeil = CreateImage("Doll_SAN_Veil", _dollRoot, new Vector2(0f, -8f), new Vector2(370f, 650f), new Color(0.17f, 0.05f, 0.25f, 0.18f), _rectSprite).rectTransform;
            return;
        }

        _body = CreateImage("Doll_Body", _dollRoot, new Vector2(0f, -24f), new Vector2(218f, 520f), new Color(0.52f, 0.49f, 0.45f, 1f), _rectSprite).rectTransform;
        CreateImage("Doll_Coat", _dollRoot, new Vector2(0f, -54f), new Vector2(282f, 454f), new Color(0.23f, 0.19f, 0.19f, 0.94f), _rectSprite);
        CreateImage("Doll_Left_Arm", _dollRoot, new Vector2(-168f, -72f), new Vector2(78f, 390f), new Color(0.45f, 0.40f, 0.34f, 1f), _rectSprite).rectTransform.localRotation = Quaternion.Euler(0f, 0f, -8f);
        CreateImage("Doll_Right_Arm", _dollRoot, new Vector2(168f, -72f), new Vector2(78f, 390f), new Color(0.45f, 0.40f, 0.34f, 1f), _rectSprite).rectTransform.localRotation = Quaternion.Euler(0f, 0f, 8f);
        _head = CreateImage("Doll_Head", _dollRoot, new Vector2(0f, 264f), new Vector2(168f, 168f), new Color(0.72f, 0.66f, 0.58f, 1f), _circleSprite).rectTransform;
        CreateImage("Doll_Hair", _head, new Vector2(0f, 22f), new Vector2(184f, 116f), new Color(0.22f, 0.19f, 0.23f, 0.96f), _circleSprite);
        CreateImage("Doll_Eye_Left", _head, new Vector2(-34f, 0f), new Vector2(18f, 28f), new Color(0.48f, 0.86f, 1f, 1f), _circleSprite);
        CreateImage("Doll_Eye_Right", _head, new Vector2(34f, 0f), new Vector2(18f, 28f), new Color(0.48f, 0.86f, 1f, 1f), _circleSprite);
        _coreLight = CreateImage("Doll_Core_Light", _dollRoot, new Vector2(0f, 68f), new Vector2(78f, 78f), new Color(0.32f, 0.92f, 1f, 0.84f), _circleSprite).rectTransform;
        CreateImage("Doll_Core_Glow", _dollRoot, new Vector2(0f, 68f), new Vector2(168f, 168f), new Color(0.20f, 0.70f, 1f, 0.18f), _circleSprite);
        _sanVeil = CreateImage("Doll_SAN_Veil", _dollRoot, new Vector2(0f, -8f), new Vector2(370f, 650f), new Color(0.17f, 0.05f, 0.25f, 0.18f), _rectSprite).rectTransform;
    }

    private void BuildEnemy(RectTransform root) {
        if (_formalEnemySprite != null) {
            Image enemyImage = CreateImage("Enemy_Formal_Combat", root, new Vector2(500f, -58f), new Vector2(470f, 560f), Color.white, _formalEnemySprite);
            enemyImage.preserveAspect = true;
            CreateImage("Enemy_Target_Ring", root, new Vector2(500f, -72f), new Vector2(560f, 560f), new Color(0.97f, 0.72f, 0.26f, 0.18f), _circleSprite);
            return;
        }

        RectTransform enemy = CreateImage("Enemy_Silhouette", root, new Vector2(470f, -48f), new Vector2(360f, 500f), new Color(0.16f, 0.24f, 0.24f, 0.98f), _rectSprite).rectTransform;
        CreateImage("Enemy_Head", enemy, new Vector2(0f, 212f), new Vector2(190f, 136f), new Color(0.22f, 0.35f, 0.34f, 1f), _circleSprite);
        CreateImage("Enemy_Eye_Left", enemy, new Vector2(-48f, 230f), new Vector2(30f, 18f), new Color(0.8f, 1f, 0.64f, 1f), _circleSprite);
        CreateImage("Enemy_Eye_Right", enemy, new Vector2(48f, 230f), new Vector2(30f, 18f), new Color(0.8f, 1f, 0.64f, 1f), _circleSprite);
        CreateImage("Enemy_Target_Ring", root, new Vector2(470f, -72f), new Vector2(500f, 500f), new Color(0.97f, 0.72f, 0.26f, 0.12f), _circleSprite);
    }

    private void BuildRepairRig(RectTransform root) {
        CreateImage("Repair_Table", root, new Vector2(-410f, -388f), new Vector2(540f, 48f), new Color(0.45f, 0.28f, 0.12f, 0.72f), _rectSprite);
        CreateImage("Repair_Arm_Left", root, new Vector2(-656f, 142f), new Vector2(46f, 350f), new Color(0.62f, 0.43f, 0.22f, 0.88f), _rectSprite).rectTransform.localRotation = Quaternion.Euler(0f, 0f, -22f);
        CreateImage("Repair_Arm_Right", root, new Vector2(-204f, 150f), new Vector2(46f, 330f), new Color(0.62f, 0.43f, 0.22f, 0.88f), _rectSprite).rectTransform.localRotation = Quaternion.Euler(0f, 0f, 25f);
    }

    private void BuildHud(RectTransform root) {
        CreateText(root, "Title", new Vector2(0f, 455f), new Vector2(1120f, 54f), "P3 expression preview: formal assets + light animation", 30, TextAnchor.MiddleCenter, new Color(0.92f, 0.86f, 0.74f, 1f));
        CreateText(root, "Caption", new Vector2(0f, 405f), new Vector2(1240f, 42f), "Uses Approved doll, workshop background, monster combat art, and UI feedback sprites.", 20, TextAnchor.MiddleCenter, new Color(0.68f, 0.78f, 0.82f, 0.86f));
        _statusLabelPanel = CreateImage("Status_Label_Panel", root, new Vector2(0f, -475f), new Vector2(960f, 68f), new Color(0.04f, 0.035f, 0.04f, 0.82f), _rectSprite);
        _statusLabel = CreateText(_statusLabelPanel.rectTransform, "Status_Label", Vector2.zero, new Vector2(920f, 50f), "Idle: breathing, gaze, unstable SAN veil", 22, TextAnchor.MiddleCenter, new Color(0.88f, 0.86f, 0.76f, 1f));
    }

    private IEnumerator DemoLoop() {
        while (true) {
            SetStatus("Idle: breathing, gaze, unstable SAN veil");
            yield return new WaitForSeconds(1.4f);

            SetStatus("Care: repair spark and pulse sweep");
            yield return StartCoroutine(RepairPulse());
            yield return new WaitForSeconds(0.6f);

            SetStatus("Combat: slash, impact shake, shield fragments");
            yield return StartCoroutine(CombatHit());
            yield return new WaitForSeconds(1.0f);

            SetStatus("Recover: return to controllable UI");
            yield return new WaitForSeconds(1.2f);
        }
    }

    private IEnumerator RepairPulse() {
        Vector2 chest = new Vector2(-410f, -8f);
        Image pulse = CreateTransient("Repair_Pulse", chest, new Vector2(80f, 80f), new Color(0.36f, 0.86f, 1f, 0.42f), _circleSprite);
        StartCoroutine(ScaleFade(pulse.rectTransform, pulse, 0.55f, new Vector2(80f, 80f), new Vector2(430f, 430f), 0.42f, 0f));

        for (int i = 0; i < 24; i++) {
            Vector2 start = chest + new Vector2(Random.Range(-90f, 90f), Random.Range(-70f, 110f));
            Vector2 end = start + new Vector2(Random.Range(-140f, 140f), Random.Range(40f, 180f));
            Image spark = CreateTransient("Repair_Spark", start, new Vector2(Random.Range(8f, 16f), Random.Range(8f, 16f)), new Color(1f, 0.72f, 0.28f, 0.94f), _circleSprite);
            StartCoroutine(MoveFade(spark.rectTransform, spark, 0.45f + Random.Range(0f, 0.25f), start, end, 0.94f, 0f));
            yield return new WaitForSeconds(0.018f);
        }

        yield return new WaitForSeconds(0.62f);
    }

    private IEnumerator CombatHit() {
        StartCoroutine(ScreenFlash(0.20f, 0.22f));
        StartCoroutine(DollFlinch());

        if (_formalHitSprite != null) {
            Image hit = CreateTransient("VFX_Formal_Hit", new Vector2(470f, 86f), new Vector2(170f, 170f), Color.white, _formalHitSprite);
            hit.preserveAspect = true;
            StartCoroutine(ScaleFade(hit.rectTransform, hit, 0.34f, new Vector2(130f, 130f), new Vector2(430f, 430f), 0.94f, 0f));
        } else {
            Image slash = CreateTransient("VFX_Slash", new Vector2(110f, 86f), new Vector2(720f, 28f), new Color(0.88f, 0.96f, 1f, 0.94f), _rectSprite);
            slash.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -16f);
            StartCoroutine(ScaleFade(slash.rectTransform, slash, 0.34f, new Vector2(120f, 28f), new Vector2(760f, 28f), 0.94f, 0f));
        }

        if (_formalShieldBreakSprite != null) {
            Image shieldBreak = CreateTransient("VFX_Formal_ShieldBreak", new Vector2(550f, 18f), new Vector2(220f, 220f), Color.white, _formalShieldBreakSprite);
            shieldBreak.preserveAspect = true;
            StartCoroutine(ScaleFade(shieldBreak.rectTransform, shieldBreak, 0.48f, new Vector2(180f, 180f), new Vector2(520f, 520f), 0.72f, 0f));
        }

        for (int i = 0; i < 18; i++) {
            Vector2 start = new Vector2(470f, 20f) + Random.insideUnitCircle * 70f;
            Vector2 end = start + new Vector2(Random.Range(-180f, 180f), Random.Range(-130f, 150f));
            Color color = i % 2 == 0 ? new Color(0.33f, 0.80f, 1f, 0.88f) : new Color(1f, 0.67f, 0.22f, 0.88f);
            Image shard = CreateTransient("Shield_Fragment", start, new Vector2(Random.Range(16f, 36f), Random.Range(7f, 18f)), color, _rectSprite);
            shard.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(-50f, 50f));
            StartCoroutine(MoveFade(shard.rectTransform, shard, 0.55f, start, end, 0.88f, 0f));
        }

        Text damage = CreateTransientText("Damage_Number", new Vector2(492f, 210f), new Vector2(220f, 64f), "-37", 46, new Color(1f, 0.86f, 0.36f, 1f));
        StartCoroutine(MoveFadeText(damage.rectTransform, damage, 0.72f, new Vector2(492f, 210f), new Vector2(492f, 300f), 1f, 0f));

        yield return new WaitForSeconds(0.82f);
    }

    private IEnumerator DollFlinch() {
        Vector2 original = _dollRoot.anchoredPosition;
        _dollRoot.anchoredPosition = original + new Vector2(-18f, 0f);
        yield return new WaitForSeconds(0.06f);
        _dollRoot.anchoredPosition = original + new Vector2(12f, 0f);
        yield return new WaitForSeconds(0.05f);
        _dollRoot.anchoredPosition = original;
    }

    private IEnumerator ScreenFlash(float alpha, float duration) {
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            Color color = _screenFlash.color;
            color.a = Mathf.Lerp(alpha, 0f, p);
            _screenFlash.color = color;
            yield return null;
        }
    }

    private IEnumerator ScaleFade(RectTransform rect, Image image, float duration, Vector2 startSize, Vector2 endSize, float startAlpha, float endAlpha) {
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            rect.sizeDelta = Vector2.Lerp(startSize, endSize, EaseOutCubic(p));
            Color color = image.color;
            color.a = Mathf.Lerp(startAlpha, endAlpha, p);
            image.color = color;
            yield return null;
        }
        Destroy(image.gameObject);
    }

    private IEnumerator MoveFade(RectTransform rect, Image image, float duration, Vector2 start, Vector2 end, float startAlpha, float endAlpha) {
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            rect.anchoredPosition = Vector2.Lerp(start, end, EaseOutCubic(p));
            Color color = image.color;
            color.a = Mathf.Lerp(startAlpha, endAlpha, p);
            image.color = color;
            yield return null;
        }
        Destroy(image.gameObject);
    }

    private IEnumerator MoveFadeText(RectTransform rect, Text text, float duration, Vector2 start, Vector2 end, float startAlpha, float endAlpha) {
        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            rect.anchoredPosition = Vector2.Lerp(start, end, EaseOutCubic(p));
            Color color = text.color;
            color.a = Mathf.Lerp(startAlpha, endAlpha, p);
            text.color = color;
            yield return null;
        }
        Destroy(text.gameObject);
    }

    private Image CreateTransient(string name, Vector2 position, Vector2 size, Color color, Sprite sprite) {
        Canvas canvas = FindObjectOfType<Canvas>();
        Image image = CreateImage(name, canvas.transform as RectTransform, position, size, color, sprite);
        image.raycastTarget = false;
        _transientObjects.Add(image.gameObject);
        return image;
    }

    private Text CreateTransientText(string name, Vector2 position, Vector2 size, string text, int fontSize, Color color) {
        Canvas canvas = FindObjectOfType<Canvas>();
        Text label = CreateText(canvas.transform as RectTransform, name, position, size, text, fontSize, TextAnchor.MiddleCenter, color);
        _transientObjects.Add(label.gameObject);
        return label;
    }

    private Image CreateImage(string name, RectTransform parent, Vector2 position, Vector2 size, Color color, Sprite sprite = null) {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = obj.GetComponent<Image>();
        image.sprite = sprite == null ? _rectSprite : sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private Text CreateText(RectTransform parent, string name, Vector2 position, Vector2 size, string text, int fontSize, TextAnchor anchor, Color color) {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text label = obj.GetComponent<Text>();
        label.text = text;
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = fontSize;
        label.alignment = anchor;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private void SetStatus(string text) {
        if (_statusLabel == null) {
            return;
        }

        _statusLabel.text = text;
        StartCoroutine(StatusPulse());
    }

    private IEnumerator StatusPulse() {
        if (_statusLabelPanel == null) {
            yield break;
        }

        Color start = new Color(0.12f, 0.08f, 0.05f, 0.92f);
        Color end = new Color(0.04f, 0.035f, 0.04f, 0.82f);
        float elapsed = 0f;
        const float duration = 0.35f;
        Image panel = _statusLabelPanel;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            panel.color = Color.Lerp(start, end, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
    }

    private Image GetImage(RectTransform rect) {
        return rect.GetComponent<Image>();
    }

    private static float EaseOutCubic(float p) {
        float q = 1f - p;
        return 1f - q * q * q;
    }

    private static Sprite ResolveFormalSprite(string visualID) {
        return VisualAssetService.TryGetSprite(visualID, out Sprite sprite) ? sprite : null;
    }

    private static Sprite CreateRectSprite() {
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
    }

    private static Sprite CreateCircleSprite(int size) {
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = (size - 1) * 0.5f;
        Vector2 center = new Vector2(radius, radius);
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }
}
