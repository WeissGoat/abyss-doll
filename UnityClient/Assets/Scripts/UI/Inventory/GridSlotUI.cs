using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// 挂载在每个 1x1 的网格预制体上
public class GridSlotUI : MonoBehaviour, IDropHandler {
    public int X { get; private set; }
    public int Y { get; private set; }
    public bool IsLocked { get; private set; }
    
    private Image _image;

    public void Initialize(int x, int y, bool isLocked) {
        X = x;
        Y = y;
        IsLocked = isLocked;
        
        _image = GetComponent<Image>();
        if (IsLocked) {
            _image.color = new Color(0.2f, 0.2f, 0.2f, 1f); // 暗色表示死格
        } else {
            _image.color = new Color(1f, 1f, 1f, 0.5f); // 半透明白色表示可用
        }
    }

    // UGUI 接口：当有物品被拖拽并在这个格子上松开时触发
    public void OnDrop(PointerEventData eventData) {
        if (IsLocked) return;

        // 获取被拖拽的物品的脚本
        DraggableItemUI draggedItem = eventData.pointerDrag.GetComponent<DraggableItemUI>();
        if (draggedItem != null) {
            // [核心修复] 根据玩家抓取的部位，反推物品真正的 [0,0] 原点坐标应该放在哪个格子上
            int originX = X - draggedItem.DragCellOffsetX;
            int originY = Y - draggedItem.DragCellOffsetY;
            
            // 呼叫后端 API 发起放置请求 (传给后端的是原点坐标)
            bool canPlace = GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid != null &&
                            ((BackpackGrid)GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid).CanPlaceItem(draggedItem.ItemData, originX, originY);

            if (canPlace) {
                // 如果后端允许，则真正执行放置
                ((BackpackGrid)GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid).PlaceItem(draggedItem.ItemData, originX, originY);
                
                // 抛出事件让全局重算
                GridSolver.RecalculateAllEffects(GameRoot.Core.CurrentPlayer.ActiveDoll);
                GameEventBus.PublishItemPlaced(draggedItem.ItemData.InstanceID, originX, originY);
                
                // 吸附UI：我们必须让物品对齐到它真正的原点格子上，而不是当前鼠标松开的格子！
                GridGenerator generator = GetComponentInParent<GridGenerator>();
                Transform targetSlot = generator != null ? generator.GetSlot(originX, originY) : null;
                
                if (targetSlot != null) {
                    draggedItem.SnapToSlot(targetSlot, originX, originY);
                } else {
                    draggedItem.SnapToSlot(this.transform, originX, originY); // fallback
                }
                
                Debug.Log($"[UI] 成功放置物品 {draggedItem.ItemData.Name} 到坐标 ({originX}, {originY})");
            } else {
                draggedItem.ReturnToOriginalPosition();
                Debug.LogWarning($"[UI] 无法在坐标 ({originX}, {originY}) 放置物品 {draggedItem.ItemData.Name}，发生重叠或越界！");
            }
        }
    }
}
