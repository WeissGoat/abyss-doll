import os
import subprocess
import tempfile
import unittest
from pathlib import Path

from tools.agent.agent_health_check import brief_lines, check_game_progress, status_now_lines


DAY = 86400
START = 1_780_000_000


class GameProgressTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.root = Path(self._tmp.name)
        self.git("init", "-q")

    def tearDown(self):
        self._tmp.cleanup()

    def git(self, *args, day=0):
        stamp = f"{START + day * DAY} +0000"
        env = dict(
            os.environ,
            GIT_AUTHOR_NAME="test",
            GIT_AUTHOR_EMAIL="test@example.com",
            GIT_COMMITTER_NAME="test",
            GIT_COMMITTER_EMAIL="test@example.com",
            GIT_AUTHOR_DATE=stamp,
            GIT_COMMITTER_DATE=stamp,
        )
        result = subprocess.run(
            ["git", "-c", "core.autocrlf=false", *args],
            cwd=self.root,
            env=env,
            capture_output=True,
            text=True,
            encoding="utf-8",
        )
        self.assertEqual(result.returncode, 0, result.stderr)
        return result.stdout.strip()

    def write(self, relative, text="x\n"):
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")

    def commit(self, relative, day):
        self.write(relative, f"{relative} {day}\n")
        self.git("add", "--", relative)
        self.git("commit", "-q", "-m", f"touch {relative}", day=day)
        return self.git("rev-parse", "--short", "HEAD")

    def test_only_game_paths_count(self):
        game = self.commit("UnityClient/Assets/Scripts/Core/GameRoot.cs", day=0)
        self.commit("UnityClient/Assets/Art/Approved/icon.png", day=3)
        self.commit("UnityClient/Assets/Scripts/Editor/Tool.cs", day=4)
        self.commit("UnityClient/Assets/Editor/Menu.cs", day=4)
        self.commit("UnityClient/Assets/Scripts/ArtAcceptance/Runner.cs", day=5)
        self.commit("UnityClient/Assets/StreamingAssets/Configs/items.json", day=6)
        self.commit("tools/agent/README.md", day=9)

        state, details = check_game_progress(self.root, now=START + 10 * DAY)

        self.assertEqual(state, "INFO")
        self.assertIn(f"10 天（{game} ", details[0])
        self.assertIn("0 项", details[1])
        self.assertIn("超过 7 天", details[2])

    def test_config_source_counts_and_recent_change_has_no_hint(self):
        self.commit("UnityClient/Assets/Scripts/Core/GameRoot.cs", day=0)
        config = self.commit("配置表(JSON)/Items/potion.json", day=9)

        state, details = check_game_progress(self.root, now=START + 10 * DAY)

        self.assertEqual(state, "INFO")
        self.assertIn(f"1 天（{config} ", details[0])
        self.assertEqual(len(details), 2)

    def test_uncommitted_game_change_suppresses_hint(self):
        self.commit("UnityClient/Assets/Prefabs/Town.prefab", day=0)
        self.write("UnityClient/Assets/Scripts/Town/TownService.cs")
        self.write("UnityClient/Assets/Art/_draft.png")

        _state, details = check_game_progress(self.root, now=START + 30 * DAY)

        self.assertIn("30 天", details[0])
        self.assertIn("1 项", details[1])
        self.assertEqual(len(details), 2)

    def test_no_game_history(self):
        self.commit("tools/agent/README.md", day=0)

        state, details = check_game_progress(self.root, now=START + DAY)

        self.assertEqual(state, "INFO")
        self.assertEqual(details[0], "  - 还没有游戏代码 / 配置提交。")

    def test_brief_reports_branch_progress_and_now_section(self):
        self.commit("UnityClient/Assets/Scripts/Core/GameRoot.cs", day=0)
        self.write(
            "PROJECT_STATUS.md",
            "# 项目状态\n\n## 现在\n\n- 正在做：M1\n- 下一步：基线检查\n\n## 已知问题\n\n- 不应出现\n",
        )

        lines = brief_lines(self.root, now=START + 2 * DAY)

        self.assertTrue(lines[0].startswith("P3 开工简报"))
        self.assertIn("工作区未提交改动 1 项", lines[1])
        self.assertTrue(any("2 天" in line for line in lines))
        self.assertIn("  - 正在做：M1", lines)
        self.assertIn("  - 下一步：基线检查", lines)
        self.assertFalse(any("不应出现" in line for line in lines))


class StatusNowTests(unittest.TestCase):
    def test_reads_only_the_now_section(self):
        text = "# 项目状态\r\n\r\n## 目标\r\n\r\n- 目标行\r\n\r\n## 现在\r\n\r\n- a\r\n- b\r\n\r\n## 已知问题\r\n\r\n- c\r\n"

        self.assertEqual(status_now_lines(text), ["- a", "- b"])

    def test_missing_section_and_line_limit(self):
        self.assertEqual(status_now_lines("# 项目状态\n\n## 目标\n\n- x\n"), [])
        many = "## 现在\n" + "".join(f"- {i}\n" for i in range(10))
        self.assertEqual(len(status_now_lines(many, limit=3)), 3)


if __name__ == "__main__":
    unittest.main()
