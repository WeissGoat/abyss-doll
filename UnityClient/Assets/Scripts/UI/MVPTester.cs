using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

// 临时的胶水测试代码，挂载在场景中启动深渊流程
public class MVPTester : MonoBehaviour {
    public GameObject testItemPrefab; // 在 Inspector 中拖入我们刚才生成的红色武器 Prefab

    void Start() {
        // 延迟 1 秒，确保 GameRoot.Awake 里的 JSON 全部加载并反序列化完毕
        Invoke("StartMockRun", 1.0f);
    }

    void Update() {
        // 鼠标左键点击时，强制打印射线检测结果，查出是谁挡住了 UI
        if (Input.GetMouseButtonDown(0)) {
            if (EventSystem.current == null) {
                Debug.LogError("[UI-Debug] 严重错误：场景中没有 EventSystem！请检查是否成功生成。");
                return;
            }
            var pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            
            if (results.Count == 0) {
                Debug.LogWarning("[UI-Debug] 鼠标点击，但没有击中任何 UGUI 元素。(可能是点到了空地，或者被 UI Toolkit 完全拦截了射线)");
            } else {
                string hitLog = "[UI-Debug] 鼠标点击击中了以下 UGUI 元素（从上到下）：\n";
                foreach (var res in results) {
                    hitLog += $" -> {res.gameObject.name} (Component: {res.gameObject.GetComponent<MonoBehaviour>()?.GetType().Name})\n";
                }
                Debug.Log(hitLog);
            }
        }
    }

    void StartMockRun() {
        Debug.Log("[MVPTester] 开始启动 MVP 闭环测试...");

        // 1. 初始化 UI 网格，直接使用后端已根据魔偶配置生成好的背包状态
        ChassisComponent myChassis = GameRoot.Core.CurrentPlayer.ActiveDoll.Chassis;
        FindObjectOfType<GridGenerator>().GenerateGrid(myChassis);

        // 2. 启动深渊与战斗状态机
        // 我们强行进入深渊第 1 层，触发刚刚写好的 DungeonManager
        GameRoot.Core.Dungeon.LoadLayer(1); 
    }
}
