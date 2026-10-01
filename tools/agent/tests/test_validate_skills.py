import tempfile
import unittest
from pathlib import Path

from tools.agent.validate_skills import validate


SKILL = "---\nname: {name}\ndescription: {description}\n---\n\n# {name}\n\n{body}\n"
FORWARDER = (
    "---\nname: {name}\ndescription: {description}\n---\n\n"
    "本技能正文维护在 `.codex/skills/{directory}/SKILL.md`。\n"
)


class ValidateSkillsTests(unittest.TestCase):
    def setUp(self):
        self._tmp = tempfile.TemporaryDirectory()
        self.root = Path(self._tmp.name)
        self.add_skill("p3-alpha", "Use for alpha work.")

    def tearDown(self):
        self._tmp.cleanup()

    def write(self, relative, text):
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")

    def add_skill(self, name, description, body="", directory=None, forwarder=True):
        directory = directory or name
        self.write(
            f".codex/skills/{directory}/SKILL.md",
            SKILL.format(name=name, description=description, body=body),
        )
        if forwarder:
            self.write(
                f".claude/skills/{name}/SKILL.md",
                FORWARDER.format(name=name, description=description, directory=directory),
            )

    def add_mission_copies(self):
        self.add_skill("p3-mission", "Use for missions.")
        source = (self.root / ".codex/skills/p3-mission/SKILL.md").read_text(encoding="utf-8")
        self.write("tools/p3-mission/SKILL.md", source)
        self.write("tools/p3-mission/README.md", "# P3 Mission\n")
        for root in ("tools/p3-mission", ".codex/skills/p3-mission"):
            self.write(f"{root}/scripts/p3_mission.py", "ROLES = set()\n")

    def test_consistent_skills_pass(self):
        self.add_mission_copies()
        errors, warnings, stats = validate(self.root)
        self.assertEqual((errors, warnings), ([], []))
        self.assertEqual(stats, {"skills": 2, "forwarders": 2})

    def test_missing_forwarder_is_an_error(self):
        self.add_skill("p3-beta", "Use for beta work.", forwarder=False)
        errors, _warnings, _stats = validate(self.root)
        self.assertIn(
            "missing Claude Code forwarder for 'p3-beta': .claude/skills/p3-beta/SKILL.md", errors
        )

    def test_forwarder_front_matter_drift_is_an_error(self):
        self.write(
            ".claude/skills/p3-alpha/SKILL.md",
            FORWARDER.format(name="p3-alpha", description="Old text.", directory="p3-alpha"),
        )
        errors, _warnings, _stats = validate(self.root)
        self.assertEqual(
            errors,
            ["forwarder front matter differs from .codex/skills/p3-alpha/SKILL.md: .claude/skills/p3-alpha/SKILL.md"],
        )

    def test_forwarder_named_after_front_matter_not_directory(self):
        self.add_skill("generate-image", "Use for images.", directory="p3-generate-image")
        errors, _warnings, _stats = validate(self.root)
        self.assertEqual(errors, [])

    def test_orphan_forwarder_is_an_error(self):
        self.write(".claude/skills/p3-gone/SKILL.md", FORWARDER.format(name="p3-gone", description="x", directory="p3-gone"))
        errors, _warnings, _stats = validate(self.root)
        self.assertEqual(errors, ["forwarder has no source skill: .claude/skills/p3-gone"])

    def test_duplicate_skill_name_is_an_error(self):
        self.add_skill("p3-alpha", "Use for alpha work.", directory="p3-alpha-copy", forwarder=False)
        errors, _warnings, _stats = validate(self.root)
        self.assertIn(
            "duplicate skill name 'p3-alpha': .codex/skills/p3-alpha and .codex/skills/p3-alpha-copy", errors
        )

    def test_leftover_directory_needs_files_to_be_reported(self):
        (self.root / ".codex/skills/p3-empty/references").mkdir(parents=True)
        self.assertEqual(validate(self.root)[0], [])
        self.write(".codex/skills/p3-empty/references/notes.md", "# notes\n")
        self.assertEqual(
            validate(self.root)[0], ["skill directory has files but no SKILL.md: .codex/skills/p3-empty"]
        )

    def test_mission_copy_drift_is_an_error(self):
        self.add_mission_copies()
        self.write(".codex/skills/p3-mission/scripts/p3_mission.py", "ROLES = {'PM'}\n")
        self.write("tools/p3-mission/templates/mission.template.csv", "id\n")
        errors, _warnings, _stats = validate(self.root)
        self.assertEqual(
            errors,
            [
                "p3-mission file missing from .codex/skills/p3-mission: templates/mission.template.csv",
                "p3-mission copies differ: scripts/p3_mission.py",
            ],
        )

    def test_dead_skill_route_is_an_error(self):
        self.add_skill("p3-gamma", "Use for gamma.", body="运行时验收使用 `.codex/skills/p3-validation/SKILL.md`。")
        errors, _warnings, _stats = validate(self.root)
        self.assertEqual(
            errors,
            [".codex/skills/p3-gamma/SKILL.md:8 references a missing path: .codex/skills/p3-validation/SKILL.md"],
        )

    def test_dead_relative_link_is_an_error(self):
        self.add_skill("p3-gamma", "Use for gamma.", body="Read [policy](references/policy.md).")
        errors, _warnings, _stats = validate(self.root)
        self.assertEqual(
            errors, [".codex/skills/p3-gamma/SKILL.md:8 links to a missing file: references/policy.md"]
        )

    def test_missing_repo_path_is_a_warning(self):
        self.write("tools/agent/Run-Existing.ps1", "")
        body = "\n".join(
            [
                "```powershell",
                ".\\tools\\agent\\Run-Existing.ps1 -Strict",
                ".\\tools\\agent\\Run-Missing.ps1",
                "```",
            ]
        )
        self.add_skill("p3-gamma", "Use for gamma.", body=body)
        errors, warnings, _stats = validate(self.root)
        self.assertEqual(errors, [])
        self.assertEqual(
            warnings,
            [".codex/skills/p3-gamma/SKILL.md:10 references a missing path: tools/agent/Run-Missing.ps1"],
        )

    def test_placeholders_and_non_repo_paths_are_ignored(self):
        self.write("tools/agent/README.md", "")
        body = "Use `_IncomingAI/<VisualID>/raw/`, `Tools/P3 Art/Rebuild Registry`, `processed/<n>/` and `https://example.com/a/b`."
        self.add_skill("p3-gamma", "Use for gamma.", body=body)
        self.assertEqual(validate(self.root)[:2], ([], []))

    def test_unknown_skill_name_in_entry_doc_is_a_warning(self):
        self.write("AGENTS.md", "# Agents\n\n- 运行时验收：使用 `p3-validation`；资产：使用 `p3-alpha`。\n")
        _errors, warnings, _stats = validate(self.root)
        self.assertEqual(warnings, ["AGENTS.md:3 names an unknown skill: p3-validation"])


if __name__ == "__main__":
    unittest.main()
