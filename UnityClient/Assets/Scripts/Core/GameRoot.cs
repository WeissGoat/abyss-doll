using UnityEngine;

// Ensure this script executes before others if necessary
[DefaultExecutionOrder(-100)]
public class GameRoot : MonoBehaviour {
    // Global static access to the backend core
    public static CoreBackend Core { get; set; }
    
    void Awake() {
        // Ensure there is only one instance
        if (Core != null) {
            Destroy(gameObject);
            return;
        }
        
        // Make this object persist across scene loads
        DontDestroyOnLoad(gameObject);
        
        // [新增] 启动本地文件日志服务与表现队列 Runner
        gameObject.AddComponent<FileLogger>();
        gameObject.AddComponent<VisualQueueRunner>();
        
        Debug.Log("[GameRoot] Bootstrapping CoreBackend...");
        
        // 1. Instantiate the pure C# backend
        Core = new CoreBackend();
        
        // 2. Initialize all systems (this will load configs)
        Core.InitAllSystems();
        
        Debug.Log("[GameRoot] Bootstrap complete!");
    }
    
    void Update() {
        if (Core != null) {
            // 3. Provide the time pulse to the backend
            Core.Tick(Time.deltaTime);
        }
    }
}
