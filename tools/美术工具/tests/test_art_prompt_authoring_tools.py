# -*- coding: utf-8 -*-

from __future__ import annotations

import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from export_art_prompt_authoring_package import export_authoring_package  # noqa: E402
from publish_art_prompt_revision import publish_revision_file  # noqa: E402


class ArtPromptAuthoringToolsTests(unittest.TestCase):
    def setUp(self) -> None:
        self.request = {
            "RequestID": "doll_zero_cold@abc123",
            "VisualID": "doll_zero_cold",
            "ProductionProfile": "character_portrait_set",
            "RequirementFingerprint": "requirement-fingerprint",
            "RequirementStatus": "ready",
            "PromptAuthoringStatus": "prompt_authoring_required",
            "PromptAuthoringContext": {
                "HardConstraints": {
                    "Identity": [{"ID": "brief:identity:0", "Text": "white blindfold"}],
                    "RequiredChanges": [],
                    "ForbiddenChanges": [],
                    "Required": [],
                    "Technical": [],
                },
                "Evidence": {
                    "ReferenceAssets": [
                        {"asset_id": "zero_dialogue_neutral", "role": "identity_reference"}
                    ]
                },
            },
            "TechnicalRequest": {},
            "PreservationContract": {},
            "ActivePromptRevisionID": "",
            "PromptRevisions": [],
            "LegacyPromptVariants": {
                "natural_language_v1": {
                    "Status": "legacy_compiled",
                    "Positive": "legacy prompt must not be exported",
                }
            },
        }
        self.catalog = {"Version": 2, "Requests": [self.request]}
        self.revision = {
            "PromptRevisionID": "doll_zero_cold@abc123/prompt-001",
            "RequirementFingerprint": "requirement-fingerprint",
            "AuthoringMode": "agent_authored",
            "Status": "ready",
            "CommonStrategy": {"Operation": "character_difference"},
            "Variants": {
                "natural_language_v2": {
                    "Format": "natural_language_v2",
                    "Status": "ready",
                    "Positive": "Preserve the exact white blindfold.",
                    "Negative": "visible eyes",
                    "OutputContract": {},
                    "ConstraintMapping": {"brief:identity:0": ["exact white blindfold"]},
                },
                "danbooru_tags_v2": {
                    "Format": "danbooru_tags_v2",
                    "Status": "unsupported",
                    "UnsupportedReason": ["No reliable tag-only route for this edit."],
                },
            },
            "AuthorNotes": [],
            "CreatedAt": "2026-07-26T12:00:00+08:00",
        }

    def test_export_contains_context_but_no_legacy_prompt(self) -> None:
        package = export_authoring_package(self.catalog, ["doll_zero_cold"])
        item = package["Items"][0]

        self.assertIn("PromptAuthoringContext", item)
        self.assertNotIn("LegacyPromptVariants", item)
        self.assertNotIn("legacy prompt must not be exported", json.dumps(package))

    def test_export_rejects_unknown_visual_id(self) -> None:
        with self.assertRaisesRegex(ValueError, "compiled_request_missing:unknown"):
            export_authoring_package(self.catalog, ["unknown"])

    def test_publish_appends_and_activates_revision(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            catalog_path = root / "catalog.json"
            revision_path = root / "revision.json"
            catalog_path.write_text(json.dumps(self.catalog), encoding="utf-8")
            revision_path.write_text(json.dumps(self.revision), encoding="utf-8")

            result = publish_revision_file(catalog_path, revision_path, activate=True, dry_run=False)
            persisted = json.loads(catalog_path.read_text(encoding="utf-8"))
            request = persisted["Requests"][0]

        self.assertEqual(result["Published"], [self.revision["PromptRevisionID"]])
        self.assertEqual(request["PromptAuthoringStatus"], "prompt_ready")
        self.assertEqual(request["ActivePromptRevisionID"], self.revision["PromptRevisionID"])

    def test_publish_dry_run_does_not_mutate_catalog(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            catalog_path = root / "catalog.json"
            revision_path = root / "revision.json"
            original = json.dumps(self.catalog, ensure_ascii=False, indent=2) + "\n"
            catalog_path.write_text(original, encoding="utf-8")
            revision_path.write_text(json.dumps(self.revision), encoding="utf-8")

            result = publish_revision_file(catalog_path, revision_path, activate=True, dry_run=True)

            self.assertEqual(catalog_path.read_text(encoding="utf-8"), original)
            self.assertTrue(result["DryRun"])

    def test_publish_synchronizes_manifest_active_pointer(self) -> None:
        manifest = {
            "Entries": [
                {
                    "VisualID": "doll_zero_cold",
                    "Status": "selected",
                    "CompiledRequest": {
                        "RequestID": self.request["RequestID"],
                        "RequirementFingerprint": self.request["RequirementFingerprint"],
                        "RequirementStatus": "ready",
                        "PromptAuthoringStatus": "prompt_authoring_required",
                        "ActivePromptRevisionID": "",
                    },
                }
            ]
        }
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            catalog_path = root / "catalog.json"
            manifest_path = root / "manifest.json"
            revision_path = root / "revision.json"
            catalog_path.write_text(json.dumps(self.catalog), encoding="utf-8")
            manifest_path.write_text(json.dumps(manifest), encoding="utf-8")
            revision_path.write_text(json.dumps(self.revision), encoding="utf-8")

            publish_revision_file(
                catalog_path,
                revision_path,
                manifest_path=manifest_path,
                activate=True,
                dry_run=False,
            )
            persisted = json.loads(manifest_path.read_text(encoding="utf-8"))

        pointer = persisted["Entries"][0]["CompiledRequest"]
        self.assertEqual(pointer["PromptAuthoringStatus"], "prompt_ready")
        self.assertEqual(pointer["ActivePromptRevisionID"], self.revision["PromptRevisionID"])
        self.assertEqual(persisted["Entries"][0]["Status"], "selected")

    def test_powershell_export_wrapper_dry_run_is_utf8_safe(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            catalog_path = root / "catalog.json"
            output_path = root / "authoring.json"
            catalog_path.write_text(json.dumps(self.catalog, ensure_ascii=False), encoding="utf-8")
            completed = subprocess.run(
                [
                    "powershell",
                    "-NoProfile",
                    "-ExecutionPolicy",
                    "Bypass",
                    "-File",
                    str(TOOLS_DIR / "Export-ArtPromptAuthoringPackage.ps1"),
                    "-RequestCatalogPath",
                    str(catalog_path),
                    "-VisualID",
                    "doll_zero_cold",
                    "-OutputPath",
                    str(output_path),
                    "-DryRun",
                ],
                cwd=TOOLS_DIR.parents[1],
                capture_output=True,
                text=True,
                encoding="utf-8",
                errors="replace",
                timeout=30,
                check=False,
            )

        self.assertEqual(completed.returncode, 0, completed.stderr or completed.stdout)
        self.assertIn('"VisualID": "doll_zero_cold"', completed.stdout)
        self.assertFalse(output_path.exists())

    def test_powershell_publish_wrapper_dry_run_does_not_mutate_catalog(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            catalog_path = root / "catalog.json"
            manifest_path = root / "manifest.json"
            revision_path = root / "revision.json"
            original = json.dumps(self.catalog, ensure_ascii=False, indent=2) + "\n"
            catalog_path.write_text(original, encoding="utf-8")
            manifest_path.write_text(
                json.dumps(
                    {
                        "Entries": [
                            {
                                "VisualID": "doll_zero_cold",
                                "CompiledRequest": {
                                    "RequestID": self.request["RequestID"],
                                    "RequirementFingerprint": self.request["RequirementFingerprint"],
                                },
                            }
                        ]
                    }
                ),
                encoding="utf-8",
            )
            revision_path.write_text(json.dumps(self.revision, ensure_ascii=False), encoding="utf-8")
            completed = subprocess.run(
                [
                    "powershell",
                    "-NoProfile",
                    "-ExecutionPolicy",
                    "Bypass",
                    "-File",
                    str(TOOLS_DIR / "Publish-ArtPromptRevision.ps1"),
                    "-RequestCatalogPath",
                    str(catalog_path),
                    "-ManifestPath",
                    str(manifest_path),
                    "-RevisionPath",
                    str(revision_path),
                    "-DryRun",
                ],
                cwd=TOOLS_DIR.parents[1],
                capture_output=True,
                text=True,
                encoding="utf-8",
                errors="replace",
                timeout=30,
                check=False,
            )

            self.assertEqual(catalog_path.read_text(encoding="utf-8"), original)

        self.assertEqual(completed.returncode, 0, completed.stderr or completed.stdout)
        self.assertIn('"DryRun": true', completed.stdout)


if __name__ == "__main__":
    unittest.main()
