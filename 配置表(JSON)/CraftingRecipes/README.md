# 工坊制造配方字段说明 (Crafting Recipes Config)

> 位于本目录下的 JSON 文件定义了在小镇工坊中进行制造和加工的成本清单。
> 目前主要用于制造“义体插件(Prosthetic)”，未来可扩展到武器改造。

## 字段说明表

| 字段名 (Key) | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `RecipeID` | string | 配方的全局唯一ID | 必填项，如 `craft_pros_power_arm` |
| `TargetProstheticID`| string | 制造成功后产出的义体ID | 指向 `Prosthetics` 目录中的一个合法ID |
| `Cost` | object | 制造该物品所需的总成本 | 包含金币与材料对象 |

## Cost (制造成本对象) 内部字段

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `Money` | int | 制造需支付的工坊加工费 | 必须符合基准价值模型的折算 |
| `RequiredItems` | array | 制造所需的深渊材料清单 | 数组元素包含 `ConfigID` (素材/战利品的ID) 和 `Count` (需求数量) |