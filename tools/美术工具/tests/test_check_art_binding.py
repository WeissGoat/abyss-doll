# -*- coding: utf-8 -*-

from __future__ import annotations

import io
import json
import sys
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from check_art_binding import main, parse_registry, run_check  # noqa: E402


GUID_A = "0123456789abcdef0123456789abcdef"
GUID_B = "fedcba9876543210fedcba9876543210"


def registry_text(entries: list[tuple[str, str]]) -> str:
    lines = ["MonoBehaviour:", "  m_Name: VisualAssetRegistry", "  Entries:"]
    for visual_id, guid in entries:
        lines.append(f"  - VisualID: {visual_id}")
        if guid:
            lines.append(f"    Sprite: {{fileID: 21300000, guid: {guid}, type: 3}}")
        else:
            lines.append("    Sprite: {fileID: 0}")
        lines.append("    Prefab: {fileID: 0}")
    return "\n".join(lines) + "\n"


class CheckArtBindingTests(unittest.TestCase):
    def setUp(self) -> None:
        self._temp = tempfile.TemporaryDirectory()
        self.root = Path(self._temp.name)
        self.manifest = self.root / "manifest.json"
        self.registry = self.root / "VisualAssetRegistry.asset"

    def tearDown(self) -> None:
        self._temp.cleanup()

    def add_approved(self, visual_id: str, guid: str | None) -> dict[str, str]:
        relative = f"Approved/{visual_id}.png"
        path = self.root / relative
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(b"png")
        if guid is not None:
            path.with_name(path.name + ".meta").write_text(f"fileFormatVersion: 2\nguid: {guid}\n", encoding="utf-8")
        return {"VisualID": visual_id, "OutputPath": relative, "RegistryStatus": "unregistered"}

    def write(self, entries: list[dict[str, str]], registry: list[tuple[str, str]]) -> None:
        self.manifest.write_text(json.dumps({"Entries": entries}), encoding="utf-8")
        self.registry.write_text(registry_text(registry), encoding="utf-8")

    def results(self, visual_ids: list[str] | None = None) -> dict[str, str]:
        report = run_check(self.root, self.manifest, self.registry, visual_ids)
        return {item["VisualID"]: item["Result"] for item in report["items"]}

    def test_parse_registry_reads_sprite_guids(self) -> None:
        parsed = parse_registry(registry_text([("icon_a", GUID_A.upper()), ("icon_b", "")]))
        self.assertEqual(parsed, {"icon_a": [GUID_A], "icon_b": [""]})

    def test_matching_guid_passes(self) -> None:
        self.write([self.add_approved("icon_a", GUID_A)], [("icon_a", GUID_A)])
        self.assertEqual(self.results(["icon_a"]), {"icon_a": "pass"})

    def test_each_broken_binding_reports_its_reason(self) -> None:
        entries = [
            self.add_approved("mismatch", GUID_A),
            self.add_approved("no_meta", None),
            self.add_approved("unregistered", GUID_A),
            self.add_approved("duplicate", GUID_A),
            self.add_approved("empty_sprite", GUID_A),
            {"VisualID": "no_file", "OutputPath": "Approved/no_file.png"},
        ]
        registry = [
            ("mismatch", GUID_B),
            ("no_meta", GUID_A),
            ("duplicate", GUID_A),
            ("duplicate", GUID_A),
            ("empty_sprite", ""),
            ("no_file", GUID_A),
        ]
        self.write(entries, registry)
        ids = ["mismatch", "no_meta", "unregistered", "duplicate", "empty_sprite", "no_file", "not_in_manifest"]
        self.assertEqual(
            self.results(ids),
            {
                "mismatch": "fail:guid_mismatch",
                "no_meta": "fail:meta_missing",
                "unregistered": "fail:registry_missing",
                "duplicate": "fail:registry_duplicate",
                "empty_sprite": "fail:registry_sprite_empty",
                "no_file": "fail:approved_file_missing",
                "not_in_manifest": "fail:manifest_missing",
            },
        )

    def test_default_selection_covers_bound_and_claimed_entries(self) -> None:
        bound = self.add_approved("bound", GUID_A)
        claimed = self.add_approved("claimed", GUID_B)
        claimed["RegistryStatus"] = "registered"
        unbound = self.add_approved("unbound", GUID_B)
        self.write([bound, claimed, unbound], [("bound", GUID_A)])
        self.assertEqual(self.results(), {"bound": "pass", "claimed": "fail:registry_missing"})

    def test_main_writes_report_and_fails_on_mismatch(self) -> None:
        self.write([self.add_approved("icon_a", GUID_A)], [("icon_a", GUID_B)])
        out = "Logs/P3ArtCheck/run_01/binding.json"
        args = ["--root", str(self.root), "--manifest", "manifest.json", "--registry", "VisualAssetRegistry.asset", "--out", out]
        with redirect_stdout(io.StringIO()) as printed:
            exit_code = main(args + ["--visual-id", "icon_a"])
        self.assertEqual(exit_code, 1)
        self.assertIn("icon_a: fail:guid_mismatch", printed.getvalue())
        report = json.loads((self.root / out).read_text(encoding="utf-8"))
        self.assertEqual(report["summary"], {"checked": 1, "passed": 0, "failed": 1})


if __name__ == "__main__":
    unittest.main()
