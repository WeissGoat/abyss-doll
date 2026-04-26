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

        // 1. 初始化 UI 网格
        // 从黑盒里读取当前玩家（也就是我们在 CoreBackend 里初始化好的原型机·零）的底盘数据
        ChassisComponent myChassis = GameRoot.Core.CurrentPlayer.ActiveDoll.Chassis;
        
        // [极其重要] 必须先在后端内存里把这块 4x4 的矩阵真正地 new 出来！
        GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid = new BackpackGrid(myChassis);

        FindObjectOfType<GridGenerator>().GenerateGrid(myChassis);

        // 2. 实例化一个可拖拽的武器 UI，并给它塞入真实的后端数据
        if (testItemPrefab != null) {
            // 在屏幕中央生成一个 UI 图片
            GameObject itemGo = Instantiate(testItemPrefab, FindObjectOfType<Canvas>().transform);
            
            // 从配置中心捞出“生锈短剑”的真实 JSON 数据，深拷贝一份
            ItemEntity swordData = ConfigManager.CreateItem("gear_tactical_blade"); // 改为 1x3 的战术长刀测试
            
            // 调整形状和位置，防止和网格重叠
            RectTransform rect = itemGo.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 300); // 1x3 格子大小
            rect.anchoredPosition = new Vector2(-400, 0); // 放在屏幕左边备用

            // 将纯数据塞给这个会响应鼠标拖拽的 UI 脚本！
            itemGo.GetComponent<DraggableItemUI>().SetupData(swordData);
            
            Debug.Log($"[MVPTester] 已在屏幕生成测试物品：{swordData.Name}，请尝试拖入网格！");
        } else {
            Debug.LogWarning("[MVPTester] 没有在 Inspector 中挂载 testItemPrefab，无法生成拖拽测试物品。");
        }

        // 3. 启动深渊与战斗状态机
        // 我们强行进入深渊第 1 层，触发刚刚写好的 DungeonManager
        GameRoot.Core.Dungeon.LoadLayer(1); 
    }
}
