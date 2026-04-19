using System;
using System.Collections.Generic;

public class BackpackGrid {
    public int Width { get; private set; }
    public int Height { get; private set; }
    
    // The core matrix storing the InstanceID of the item occupying the cell
    private string[,] _gridMatrix;
    
    public List<ItemEntity> ContainedItems { get; private set; }
    
    public BackpackGrid(ChassisComponent chassis) {
        Width = chassis.GridWidth;
        Height = chassis.GridHeight;
        _gridMatrix = new string[Width, Height];
        ContainedItems = new List<ItemEntity>();
        
        // Initialize locked cells based on the chassis mask
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                if (chassis.GridMask != null && chassis.GridMask.Length > x && chassis.GridMask[x].Length > y) {
                    if (!chassis.GridMask[x][y]) {
                        _gridMatrix[x, y] = "LOCKED_CELL";
                    }
                }
            }
        }
    }
    
    // Calculates the actual occupied cells based on position, shape, and rotation
    public List<int[]> GetOccupiedCells(ItemEntity item, int targetX, int targetY) {
        List<int[]> cells = new List<int[]>();
        if (item.Grid == null || item.Grid.Shape == null) return cells;
        
        foreach (var point in item.Grid.Shape) {
            int px = point[0];
            int py = point[1];
            
            // Apply rotation (0, 90, 180, 270 degrees clockwise)
            int rotatedX = px;
            int rotatedY = py;
            
            switch (item.Grid.Rotation) {
                case 90:
                    rotatedX = -py;
                    rotatedY = px;
                    break;
                case 180:
                    rotatedX = -px;
                    rotatedY = -py;
                    break;
                case 270:
                    rotatedX = py;
                    rotatedY = -px;
                    break;
            }
            
            cells.Add(new int[] { targetX + rotatedX, targetY + rotatedY });
        }
        
        // Normalize coordinates to ensure no negative offsets within the shape's local bounding box
        // Find minimum X and Y
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        foreach(var cell in cells) {
            if(cell[0] < minX) minX = cell[0];
            if(cell[1] < minY) minY = cell[1];
        }
        
        // Offset so local top-left is (targetX, targetY) if needed, but standard logic 
        // assumes the shape definition's origin is what we rotate around.
        // For simplicity, we just add the rotated offset to targetX, targetY.
        
        return cells;
    }

    public bool CanPlaceItem(ItemEntity item, int targetX, int targetY) {
        List<int[]> occupiedCells = GetOccupiedCells(item, targetX, targetY);
        
        foreach (var cell in occupiedCells) {
            int x = cell[0];
            int y = cell[1];
            
            // 1. Boundary Check
            if (x < 0 || x >= Width || y < 0 || y >= Height) {
                return false;
            }
            
            // 2. Collision Check
            if (!string.IsNullOrEmpty(_gridMatrix[x, y])) {
                // If it's occupied by something other than the item itself (when moving)
                if (_gridMatrix[x, y] != item.InstanceID) {
                    return false;
                }
            }
        }
        
        return true;
    }

    public bool PlaceItem(ItemEntity item, int targetX, int targetY) {
        if (!CanPlaceItem(item, targetX, targetY)) {
            return false;
        }
        
        // Remove item from previous position if it was already in the grid
        if (ContainedItems.Contains(item)) {
            RemoveItem(item);
        }
        
        List<int[]> occupiedCells = GetOccupiedCells(item, targetX, targetY);
        foreach (var cell in occupiedCells) {
            _gridMatrix[cell[0], cell[1]] = item.InstanceID;
        }
        
        item.Grid.CurrentPos = new int[] { targetX, targetY };
        ContainedItems.Add(item);
        
        return true;
    }

    public void RemoveItem(ItemEntity item) {
        if (!ContainedItems.Contains(item)) return;
        
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                if (_gridMatrix[x, y] == item.InstanceID) {
                    _gridMatrix[x, y] = null;
                }
            }
        }
        ContainedItems.Remove(item);
    }
    
    public ItemEntity GetItemAt(int x, int y) {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return null;
        string id = _gridMatrix[x, y];
        if (string.IsNullOrEmpty(id) || id == "LOCKED_CELL") return null;
        return ContainedItems.Find(i => i.InstanceID == id);
    }
    
    public void DebugPrintGrid() {
        string output = "Backpack Grid:\n";
        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < Width; x++) {
                string cell = _gridMatrix[x, y];
                if (cell == null) output += "[ ] ";
                else if (cell == "LOCKED_CELL") output += "[X] ";
                else output += $"[*] "; // Simplified for visual
            }
            output += "\n";
        }
        UnityEngine.Debug.Log(output);
    }
}
