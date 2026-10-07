# M4 出图提示词（2026-10-06 / 07）

批 5 换进 `UnityClient/Assets/Art/Approved` 的背景、序章 CG 和零号立绘就是用这几份任务出的，留着方便重出或改一张。

- 后端：AI 图片网关的 `gemini_chat_image`（图生图）；参考图是 M4 第一轮风格锚点、新工坊背景和零号母版（`美术文档/人设/AI出图/zero_v2_master_20261006/selected/zero_v2_master.png`）。`refs` 里的路径指向不进 git 的 `tmp/art_explore/`，重出时换成仓库里的对应图。
- 每个任务 1 张，选定后按同 VisualID 覆盖 Approved 里的原文件（`.meta` 和 GUID 不变）；背景按 1920×1080 居中裁切，零号立绘用 rembg 抠掉白底后底部对齐放进原画布。
- `backgrounds_round8.json`：工坊、选层、战斗第 1 层、安全区、阶梯、结算胜 / 败。
- `prologue_cg_and_zero_poses_round9.json`：序章 15 格 CG 和零号 5 张立绘。
- `prologue_backgrounds_round10.json`：住处、工作室、序章半开工坊、醒来视角。
