---
id: config_dolls_readme
title: 人偶基础档案配置字段说明 (Dolls Config)
type: config
role: 策划
domain: config_dolls
status: active
source_of_truth: true
related:
  - 开发文档/数据与实体定义/02_人偶与状态实体.md
  - 配置表(JSON)/README.md
  - 配置表(JSON)/Effects/README.md
  - 设计文档/GDD_08_人偶养成子模块全案.md
  - 设计文档/05_局外成长与维护规则卡.md
  - 设计文档/GDD_12_人偶交互管理器.md
  - 设计文档/GDD_03_人偶实体对象与好感双轨机制.md
  - 设计文档/GDD_11_人偶房间与视觉叙事系统.md
  - 设计文档/GDD_09_标签与特质系统.md
  - 设计文档/06_标签与特质规则卡.md
  - 设计文档/08_人偶核心状态与好感双轨规则卡.md
  - 设计文档/09_人偶交互事件与反馈规则卡.md
  - 设计文档/11_人偶房间布局与视觉叙事规则卡.md
last_verified: 2026-05-24
update_rule: 修改配置字段、数据源规则或表间引用时同步本文件。
---

# 人偶基础档案配置字段说明 (Dolls Config)

> 位于本目录下的 JSON 文件定义了游戏中可操作的人偶的基础档案。
> 注意：这里不包含局内随时间波动的临时状态，只包含其出厂的上限与三维天赋。

## 字段说明表

| 字段名 (Key) | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `DollID` | string | 人偶的全局唯一ID | 必填项，如 `doll_proto_0` |
| `Name` | string | 人偶在UI上显示的名称 | 如 "原型机·零" |
| `Stats` | object | 人偶的初始基础属性面板 | 包含血量、SAN值上限及三维等 |
| `DefaultChassisID`| string | 初始默认装配的底盘ID | 对应 `Chassis` 目录中的一个合法底盘配置 |

## Stats (基础属性对象) 内部字段

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `HP_Max` | int | 机体耐久度（血条）上限 | 局内降为0则战败并产生高额维修账单 |
| `SAN_Max` | int | 理智值（SAN值）上限 | 局外需花钱补充，局内随探索自然流失 |
| `Power` | int | 机能（三维属性） | 影响物理攻击系数与负重 |
| `Compute` | int | 算力（三维属性） | 影响能量类武器倍率及商店议价 |
| `Charm` | int | 魅力（三维属性） | 影响势力交涉、事件判定与好感度获取 |
