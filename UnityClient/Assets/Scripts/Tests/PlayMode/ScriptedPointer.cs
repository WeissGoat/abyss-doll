using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

// A mouse the Play Mode scenarios move by script. It feeds the real StandaloneInputModule (raycasts, pointer enter,
// begin drag, drop, end drag in Unity's own order) instead of calling the handlers directly, so a scenario sees what
// a player's mouse would.
public class ScriptedPointer : BaseInput {
    private Vector2 _position;
    private bool _held;
    private bool _pressedThisFrame;
    private bool _releasedThisFrame;
    private int _changedFrame = -1;

    public override Vector2 mousePosition => _position;
    public override bool mousePresent => true;
    public override Vector2 mouseScrollDelta => Vector2.zero;
    public override bool GetMouseButton(int button) => button == 0 && _held;
    public override bool GetMouseButtonDown(int button) => button == 0 && _pressedThisFrame && Time.frameCount == _changedFrame;
    public override bool GetMouseButtonUp(int button) => button == 0 && _releasedThisFrame && Time.frameCount == _changedFrame;
    public override int touchCount => 0;
    public override bool touchSupported => false;

    public static Vector2 Position { get; private set; }

    public static ScriptedPointer Install() {
        EventSystem system = EventSystem.current;
        StandaloneInputModule module = system != null ? system.GetComponent<StandaloneInputModule>() : null;
        if (module == null) {
            Debug.LogError("[ScriptedPointer] No StandaloneInputModule on the EventSystem.");
            return null;
        }

        ScriptedPointer pointer = system.GetComponent<ScriptedPointer>();
        if (pointer == null) {
            pointer = system.gameObject.AddComponent<ScriptedPointer>();
        }

        module.inputOverride = pointer;
        return pointer;
    }

    public void Uninstall() {
        StandaloneInputModule module = GetComponent<StandaloneInputModule>();
        if (module != null && module.inputOverride == this) {
            module.inputOverride = null;
        }

        Destroy(this);
    }

    // Press at from, move there in steps (one frame each), release at to.
    public IEnumerator Drag(Vector2 from, Vector2 to, int steps = 12) {
        MoveTo(from);
        yield return null;
        Press(true);
        yield return null;
        for (int i = 1; i <= steps; i++) {
            MoveTo(Vector2.Lerp(from, to, i / (float)steps));
            yield return null;
        }

        yield return null;
        Press(false);
        yield return null;
        yield return null;
    }

    public void MoveTo(Vector2 position) {
        _position = position;
        Position = position;
    }

    public void Press(bool down) {
        _held = down;
        _pressedThisFrame = down;
        _releasedThisFrame = !down;
        // Set from a coroutine (after this frame's EventSystem update), so the module sees it next frame.
        _changedFrame = Time.frameCount + 1;
    }
}
