---
id: config_craftingrecipes_readme
title: 工坊制造配方字段说明 (Crafting Recipes Config)
type: config
role: 策划
domain: config_crafting
status: active
source_of_truth: true
related:
  - 开发文档/数据与实体定义/05_经济与社会实体.md
  - 开发文档/04_工坊与养成逻辑(WorkshopSystem).md
  - 数值模型设计/01_经济循环与通缩模型.md
  - 数值模型设计/00_基准价值与空间本位模型.md
  - 配置表(JSON)/Chassis/README.md
  - 配置表(JSON)/README.md
  - 配置表(JSON)/Prosthetics/README.md
  - 设计文档/GDD_08_人偶养成子模块全案.md
  - 设计文档/GDD_10_势力声望与订单系统.md
  - 设计文档/GDD_04_小镇循环与经济物价波浪模型.md
last_verified: 2026-05-23
update_rule: 修改配置字段、数据源规则或表间引用时同步本文件。
---

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