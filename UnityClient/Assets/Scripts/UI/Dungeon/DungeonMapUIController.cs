using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DungeonMapUIController : MonoBehaviour {
    public GameObject nodeButtonPrefab;
    public Transform contentParent;

    public void RefreshMap() {
        if (contentParent == null || nodeButtonPrefab == null) return;

        // Clear old children
        foreach (Transform child in contentParent) {
            Destroy(child.gameObject);
        }
        
        var layer = GameRoot.Core.Dungeon.CurrentLayer;
        if (layer == null || layer.RootNode == null) return;
        
        // Simple linear map UI for MVP
        List<NodeBase> path = new List<NodeBase>();
        NodeBase curr = layer.RootNode;
        while(curr != null) {
            path.Add(curr);
            curr = curr.NextNodes != null && curr.NextNodes.Count > 0 ? curr.NextNodes[0] : null;
        }

        bool foundCurrent = false;

        for (int i = 0; i < path.Count; i++) {
            NodeBase node = path[i];
            GameObject btnGo = Instantiate(nodeButtonPrefab, contentParent);
            Button btn = btnGo.GetComponent<Button>();
            Text txt = btnGo.GetComponentInChildren<Text>();
            Image img = btnGo.GetComponent<Image>();

            if (node is CombatNode) {
                txt.text = $"战斗节点\n(消耗1SAN)";
            } else if (node is SafeRoomNode) {
                txt.text = $"安全区\n(休整)";
            } else {
                txt.text = $"未知节点";
            }

            if (node.IsVisited) {
                img.color = new Color(0.3f, 0.3f, 0.3f);
                btn.interactable = false;
            } else {
                // If this is the next unvisited node, highlight it
                if (!foundCurrent) {
                    img.color = new Color(0.2f, 0.8f, 0.2f);
                    btn.interactable = true;
                    btn.onClick.AddListener(() => {
                        GameRoot.Core.Dungeon.MoveToNode(node);
                    });
                    foundCurrent = true;
                } else {
                    img.color = Color.black;
                    btn.interactable = false;
                }
            }
        }
    }
}