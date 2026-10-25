# 零号战斗攻击姿势（旧版，2026-10-21 起用，2026-10-25 换下）

旧的攻击姿势是挥拳横扫，武器图标从肩膀上方挥下来。制作人觉得一个看不见的人偶少女挥拳不合适，2026-10-25 换成「一手按住胸口的核心、一手张开往前推」（候选「带光 2」），武器图标改成从手掌飞出去。

换回去：把 `doll_zero_combat_attack.png` 复制回 `UnityClient/Assets/Art/Approved/Dolls/Combat/`（只覆盖图片，不动 `.meta`），再把 `CombatMotionDirector.SpawnSwing` 和 `VfxKind.Swing` 的动画按 git 历史（2026-10-25 之前）改回从肩膀挥下。
