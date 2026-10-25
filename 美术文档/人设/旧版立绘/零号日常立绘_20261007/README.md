# 零号日常立绘（旧版，2026-10-07 起用，2026-10-25 换下）

制作人 2026-10-25 选了序章画风的新立绘（候选 a_002），这里留着换下来的整套，需要时可以原样换回去。

| 文件 | 原来的位置（`UnityClient/Assets/Art/Approved/Dolls/` 下） |
|---|---|
| `doll_proto_0_stand.png` | `doll_proto_0_stand.png`（大厅、标题、工作室里的零号） |
| `doll_proto_0_stand_hold.png` | `doll_proto_0_stand_hold.png`（牵手时伸出的手臂） |
| `doll_proto_0_stand_rig.png` | `Rigs/doll_proto_0_stand_rig.png`（头发和裙摆摆动权重） |
| `doll_proto_0_stand_puppet.png` | `Rigs/doll_proto_0_stand_puppet.png`（头、脸、手的纸偶权重） |
| `doll_proto_0_stand_idle.webm` | `Motion/doll_proto_0_stand_idle.webm`（大厅、标题的待机循环） |

## 换回去

把文件复制回原位置（只覆盖图片本身，不动 `.meta`，GUID 不变），再把代码里跟着这张图的点改回下面的值：

- `UIPortraitPuppet`：`Neck = (0.513, 0.792)`，`Cheeks = (0.52, 0.8275)`，`HoldAnchor = (0.43, 0.5)`。
- `ZeroPresence`：`HeadArea = (0.38, 0.8, 0.24, 0.17)`，`ShoulderArea = (0.3, 0.71, 0.4, 0.08)`，`HandArea = (0.33, 0.48, 0.2, 0.1)`。
- `StudioCareView`：`GrimeSpots` 第 4、5 个 `(0.61, 0.30)`、`(0.45, 0.14)`；`CrackSpots` 第 2、3 个 `(0.63, 0.52)`、`(0.48, 0.22)`；`HazeSpots` 第 2 个 `(0.38, 0.48)`。

当时生成权重图的参数：`build_portrait_rig.py`（不带 `--strands`）；`build_puppet_rig.py --head 530,165,150 --neck 525,320 --face 522,250,80,70 --hands 440,760,60,600`；手臂 `make_pose_patch.py --below 0.30 --cover 360,600,560,840`。

战斗姿势（`Combat/doll_zero_combat_*`）和夜谈半身像（`Bust/doll_zero_bust*`）是照旧立绘出的，这次没换。
