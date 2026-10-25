# 立绘上的位置 (Portraits)

一张会动、能摸的站立立绘一个文件，文件里的 `VisualID` 就是立绘的 VisualID（例如 `doll_proto_0_stand`）。纸偶（`UIPortraitPuppet`）绕哪里转、摸哪里有反应（`ZeroPresence`）、污渍画在哪里（`StudioCareView`）都从这里读。房间里用的立绘没有这个文件时，纸偶不动、摸不了，进房间时报错。

坐标都是立绘画布的比例，从左下角算：点是 `[x, y]`，区域是 `[x, y, 宽, 高]`，高度是 y。数字从美术工具的预览里读（`build_puppet_rig.py` 会打印脖子的位置），做法见 `美术文档/21_战斗姿势与动态素材工作流.md` 的「纸偶立绘」。

| 字段 | 含义 |
|---|---|
| `Neck` | 头转动的支点（下巴下面、脖子中间） |
| `Cheeks` | 两颊中间，被摸时脸红画在这里 |
| `HoldAnchor` | 牵手时伸出的手臂上的一点，画好的手臂跟着这一点转；要落在斗篷或裙子上，不在头发上 |
| `KneeV` / `WaistV` | 身体从哪里开始倾斜（裙摆下面一点）、到哪里倾斜完整（腰） |
| `HairLag` | `[上, 下]`：垂下的长发从「上」开始马上跟着头，到「下」（发梢）最晚 |
| `HeadArea` / `ShoulderArea` / `HandArea` | 摸头、拍肩、牵手的触摸区 |
| `GrimeSpots` / `CrackSpots` / `HazeSpots` | 污渍（6 个）、裂痕（3 个）、黑雾（3 个）的位置，放在斗篷、裙子、腿上，不放在脸上；个数要够 `CareService` 能加的上限 |

`ConfigValidator` 检查长度、范围、先后顺序（`KneeV` < `WaistV` < 脖子）和个数。
