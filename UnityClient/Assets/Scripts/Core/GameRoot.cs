using UnityEngine;

// Ensure this script executes before other runtime systems.
[DefaultExecutionOrder(-100)]
public class GameRoot : MonoBehaviour {
    // Global static access to the backend core.
    public static CoreBackend Core { get; set; }
    public static GameRoot Instance { get; private set; }

    void Awake() {
        EnsureRuntimeBootstrap();
    }

    void OnEnable() {
        EnsureRuntimeBootstrap();
    }

    public static bool IsCoreReady() {
        return Core?.CurrentPlayer?.ActiveDoll != null;
    }

    private void EnsureRuntimeBootstrap() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Application.runInBackground = true;

        EnsureComponent<FileLogger>();
        EnsureComponent<VisualQueueRunner>();

        if (Core != null) {
            return;
        }

        Debug.Log("[GameRoot] Bootstrapping CoreBackend...");
        Core = new CoreBackend();
        Core.InitAllSystems();
        Debug.Log("[GameRoot] Bootstrap complete!");
    }

    private void EnsureComponent<T>() where T : Component {
        if (GetComponent<T>() == null) {
            gameObject.AddComponent<T>();
        }
    }

    void Update() {
        if (Core != null) {
            Core.Tick(Time.deltaTime);
        }
    }
}
