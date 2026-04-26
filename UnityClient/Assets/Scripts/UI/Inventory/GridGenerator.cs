using UnityEngine;
using UnityEngine.UI;

public class GridGenerator : MonoBehaviour {
    [Header("Prefabs & Parents")]
    public GameObject slotPrefab;      // 基础 1x1 格子预制体
    public Transform gridParent;       // 挂载了 GridLayoutGroup 的容器
    
    [Header("Grid Data")]
    private GameObject[,] _uiSlots;
    private int _width;
    private int _height;

    // 此方法应该在游戏进入战斗场景或工坊场景，底盘数据加载完成后调用
    public void GenerateGrid(ChassisComponent chassis) {
        _width = chassis.GridWidth;
        _height = chassis.GridHeight;
        
        // 1. 清空旧格子
        foreach (Transform child in gridParent) {
            Destroy(child.gameObject);
        }
        
        // 动态设置列数
        GridLayoutGroup layout = gridParent.GetComponent<GridLayoutGroup>();
        if (layout != null) layout.constraintCount = _width;
        
        _uiSlots = new GameObject[_width, _height];

        // 2. 循环生成新格子
        for (int y = 0; y < _height; y++) {
            for (int x = 0; x < _width; x++) {
                GameObject slotGo = Instantiate(slotPrefab, gridParent);
                slotGo.name = $"Slot_{x}_{y}";
                
                // 校验死格 (GridMask 是基于 [x][y] 还是 [y][x] 需要和底层统一，这里按底层 [x][y])
                bool isLocked = true;
                if (chassis.GridMask != null && chassis.GridMask.Length > x && chassis.GridMask[x].Length > y) {
                    isLocked = !chassis.GridMask[x][y];
                }

                // 挂载自定义脚本记录坐标
                GridSlotUI slotUI = slotGo.AddComponent<GridSlotUI>();
                slotUI.Initialize(x, y, isLocked);
                
                _uiSlots[x, y] = slotGo;
            }
        }
        
        Debug.Log($"[UI] Generated Backpack Grid: {_width}x{_height}");
    }
    
    // [新增] 提供给 DraggableItemUI 吸附的辅助方法
    public Transform GetSlot(int x, int y) {
        if (x < 0 || x >= _width || y < 0 || y >= _height) return null;
        return _uiSlots[x, y].transform;
    }
}
