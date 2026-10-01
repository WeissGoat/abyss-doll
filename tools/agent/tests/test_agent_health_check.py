import os
import subprocess
import tempfile
import unittest
from pathlib import Path

from tools.agent.agent_health_check import check_game_progress


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


if __name__ == "__main__":
    unittest.main()
