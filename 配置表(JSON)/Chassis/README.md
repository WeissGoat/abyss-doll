# 局外底盘配置字段说明 (Chassis Config)

> 位于本目录下的 JSON 文件定义了人偶的“底盘”数据。
> **底盘是决定玩家能带入/带出深渊的【网格大小与形状】的绝对核心。**

## 字段说明表

| 字段名 (Key) | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `ChassisID` | string | 底盘的全局唯一ID | 必填项，如 `chassis_lv1_basic` |
| `Level` | int | 底盘的阶段等级 | 用于UI展示和排序 (如 1, 2, 3...) |
| `GridWidth` | int | 基础网格矩阵的宽度 (X轴) | 决定横向最大格数 |
| `GridHeight` | int | 基础网格矩阵的高度 (Y轴) | 决定纵向最大格数 |
| `GridMask` | bool[][] | 网格可用性掩码矩阵（二维数组） | `true`代表该格子可用，`false`代表天生不可用的死格（用于制造异形背包） |
| `UpgradeCost` | object | 升级到下一级底盘所需的消耗 | 若为最高级则填 `null` |

## UpgradeCost (升级消耗对象) 内部字段

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `Money` | int | 升级所需消耗的金币数量 | 必须结合《经济循环与通缩模型》配置 |
| `RequiredItems` | array | 升级所需消耗的素材清单 | 数组元素包含 `ConfigID` (物品ID) 和 `Count` (数量) |
| `NextChassisID` | string | 升级后解锁的新底盘ID | 指向另一个合法的 `ChassisID` |