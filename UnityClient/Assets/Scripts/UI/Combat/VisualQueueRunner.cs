using System.Collections;
using UnityEngine;

// 挂载在前端的常驻物体上，负责循环消化队列
public class VisualQueueRunner : MonoBehaviour {
    void Start() {
        // 告知底层队列：现在不是纯后端测试环境，是有 UI 的真实运行环境
        VisualQueue.IsHeadless = false;
        
        // 启动后台表现协程
        StartCoroutine(ProcessQueueCoroutine());
        Debug.Log("[VisualQueueRunner] Started. Listening for visual commands...");
    }

    private IEnumerator ProcessQueueCoroutine() {
        while (true) {
            if (VisualQueue.TryDequeue(out IVisualCommand cmd)) {
                // 等待当前指令的动画表现全部播完，再抓取下一条
                yield return StartCoroutine(cmd.Execute());
            } else {
                // 如果队列空了，休眠一帧再查，避免死循环卡死主线程
                yield return null;
            }
        }
    }
    
    void OnDestroy() {
        // 防止对象销毁后还有残留指令，或者切场景引发错误
        VisualQueue.Clear();
        VisualQueue.IsHeadless = true;
    }
}