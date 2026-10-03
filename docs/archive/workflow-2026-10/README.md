# 旧工作流归档（2026-10-03）

2026-10-03 起，P3 改由制作人加 Claude Code 为主开发，工作流做了一次减法：入口改成根目录的 `AGENTS.md`，状态只记在 `PROJECT_STATUS.md`。

这里存放被替换下来的流程文档和 Skill，只作历史参考，不再维护。

| 内容 | 原位置 | 说明 |
|---|---|---|
| `AGENTS.md`、`PROJECT_STATUS.md` | 仓库根目录 | 减负前的入口和状态页（2026-10-03 快照）。 |
| `agent_status/` | 仓库根目录 | 按角色分开的状态页（导演、策划、程序、美术、PM）。美术暂停项的细节仍可在 `agent_status/art.md` 查到。 |
| `知识库/` | 仓库根目录 | 知识库入口和各角色的阅读路线（View）。 |
| `rules/` | 仓库根目录 | 文档维护规则（01）和智能体任务路由与完成协议（02：执行道、Owner 做法、回写矩阵）。 |
| `skills/` | `.codex/skills/` | 已合并或下线的 Skill。`p3-art-asset-production`、`p3-generate-image`、`p3-narrative-cg-comic` 合并为 `p3-art`，参考资料移到 `.codex/skills/p3-art/references/`；`p3-art-validation` 的轻量检查并入 `p3-playtest`，ArtRun 框架说明随旧验证框架下线。 |
| （已删除）文档校验 | `tools/docs/` | `validate_docs.py` 及其测试只为旧的角色、状态页和 `related` 关联网服务，已删除，见 git 历史。文档索引改为按需生成（`tools/docs/Generate-DocsIndex.ps1`），不再提交。 |

其他文档 front matter 里 `related:` 指向这些旧路径的链接不再维护。正文里提到 `agent_status/`、`知识库/`、`rules/0x` 时，按本归档理解。
