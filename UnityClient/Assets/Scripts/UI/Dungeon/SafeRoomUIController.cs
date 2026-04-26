using UnityEngine;
using UnityEngine.UI;

public class SafeRoomUIController : MonoBehaviour {
    public Button restBtn;
    public Button evacuateBtn;

    private SafeRoomNode _currentNode;

    void Start() {
        if (restBtn != null) {
            restBtn.onClick.AddListener(() => {
                if (_currentNode != null) {
                    _currentNode.Rest();
                }
            });
        }
        if (evacuateBtn != null) {
            evacuateBtn.onClick.AddListener(() => {
                if (_currentNode != null) {
                    _currentNode.Evacuate();
                }
            });
        }
    }

    public void Setup(SafeRoomNode node) {
        _currentNode = node;
    }
}