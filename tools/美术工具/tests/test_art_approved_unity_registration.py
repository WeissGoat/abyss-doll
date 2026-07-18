# -*- coding: utf-8 -*-
from __future__ import annotations

import hashlib
import json
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from art_approved_unity_registration import (  # noqa: E402
    ArtImportError,
    create_plan,
    find_approved_basename_collisions,
    parse_meta_guid,
    record_approved_sync,
    sha256_file,
    verify_sync_authorization,
)


class ArtApprovedUnityRegistrationTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        self.root = Path(self.temp_dir.name)
        self.project_root = self.root / "project"
        self.incoming_root = self.project_root / "UnityClient" / "Assets" / "Art" / "_IncomingAI"
        self.approved_root = self.project_root / "UnityClient" / "Assets" / "Art" / "Approved"
        self.evidence_root = self.project_root / "UnityClient" / "Logs" / "P3ArtImport"
        self.manifest_path = self.project_root / "美术文档" / "_generated" / "art_manifest.json"
        self.workspace = self.incoming_root / "character_portraits" / "doll_zero_dialogue_neutral"
        self.selected_path = self.workspace / "selected" / "001.png"
        self.output_path = self.approved_root / "Dolls" / "doll_zero_dialogue_neutral.png"
        self.write_image(self.selected_path, (1024, 1536), (255, 255, 255, 255))
        self.manifest_path.parent.mkdir(parents=True, exist_ok=True)
        self.manifest_path.write_text(
            json.dumps(
                {
                    "Version": 1,
                    "Entries": [
                        {
                            "VisualID": "doll_zero_dialogue_neutral",
                            "ProductionProfile": "character_portrait_set",
                            "Status": "selected",
                            "OutputPath": "UnityClient/Assets/Art/Approved/Dolls/doll_zero_dialogue_neutral.png",
                            "SelectedPath": "UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_zero_dialogue_neutral/selected/001.png",
                            "Spec": {
                                "SourceSpec": {
                                    "Format": "png",
                                    "Width": 1024,
                                    "Height": 1536,
                                    "AlphaRequired": True,
                                }
                            },
                        }
                    ],
                }
            ),
            encoding="utf-8",
        )

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    @staticmethod
    def write_image(path: Path, size: tuple[int, int], color: tuple[int, int, int, int]) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        Image.new("RGBA", size, color).save(path)

    def test_parse_meta_guid_requires_valid_guid(self) -> None:
        meta = self.root / "asset.png.meta"
        meta.write_text("fileFormatVersion: 2\nguid: 0123456789abcdef0123456789abcdef\n", encoding="utf-8")
        self.assertEqual(parse_meta_guid(meta), "0123456789abcdef0123456789abcdef")

    def test_create_plan_records_target_and_expected_importer(self) -> None:
        plan = create_plan(
            manifest_path=self.manifest_path,
            incoming_root=self.incoming_root,
            approved_root=self.approved_root,
            evidence_root=self.evidence_root,
            art_import_run_id="art_import_test_01",
            visual_ids=["doll_zero_dialogue_neutral"],
            mode="interactive",
            unity_instance="UnityClient@test1234",
            permissions={
                "allow_approved_sync": False,
                "allow_existing_target_overwrite": False,
                "allow_new_approved_target": False,
            },
        )
        item = plan["items"][0]
        self.assertEqual(item["selected_source_kind"], "manifest_selected")
        self.assertEqual(item["selected_sha256"], sha256_file(self.selected_path))
        self.assertEqual(item["unity_asset_path"], "Assets/Art/Approved/Dolls/doll_zero_dialogue_neutral.png")
        self.assertEqual(item["expected_importer"]["max_texture_size"], 2048)
        self.assertTrue(item["authorization_required"])
        self.assertTrue((self.evidence_root / "art_import_test_01" / "request.json").exists())
        self.assertTrue((self.evidence_root / "art_import_test_01" / "approved-plan.json").exists())

    def test_create_plan_rejects_duplicate_visual_id_entries(self) -> None:
        data = json.loads(self.manifest_path.read_text(encoding="utf-8"))
        data["Entries"].append(dict(data["Entries"][0]))
        self.manifest_path.write_text(json.dumps(data), encoding="utf-8")
        with self.assertRaisesRegex(ArtImportError, "duplicate Manifest VisualID"):
            create_plan(
                manifest_path=self.manifest_path,
                incoming_root=self.incoming_root,
                approved_root=self.approved_root,
                evidence_root=self.evidence_root,
                art_import_run_id="art_import_test_02",
                visual_ids=["doll_zero_dialogue_neutral"],
                mode="interactive",
                unity_instance="UnityClient@test1234",
                permissions={},
            )

    def test_find_approved_basename_collisions(self) -> None:
        first = self.approved_root / "Dolls" / "doll_zero_dialogue_neutral.png"
        second = self.approved_root / "Legacy" / "doll_zero_dialogue_neutral.png"
        self.write_image(first, (2, 2), (1, 2, 3, 255))
        self.write_image(second, (2, 2), (1, 2, 3, 255))
        collisions = find_approved_basename_collisions(self.approved_root)
        self.assertEqual(collisions["doll_zero_dialogue_neutral"], sorted([first.as_posix(), second.as_posix()]))

    def test_create_plan_is_idempotent_for_same_request(self) -> None:
        kwargs = {
            "manifest_path": self.manifest_path,
            "incoming_root": self.incoming_root,
            "approved_root": self.approved_root,
            "evidence_root": self.evidence_root,
            "art_import_run_id": "art_import_test_03",
            "visual_ids": ["doll_zero_dialogue_neutral"],
            "mode": "interactive",
            "unity_instance": "UnityClient@test1234",
            "permissions": {"allow_approved_sync": False},
        }
        first = create_plan(**kwargs)
        second = create_plan(**kwargs)
        self.assertEqual(first["request_fingerprint"], second["request_fingerprint"])
        self.assertEqual(first["items"], second["items"])

    def test_verify_sync_requires_explicit_gate_and_new_target_permission(self) -> None:
        create_plan(
            manifest_path=self.manifest_path,
            incoming_root=self.incoming_root,
            approved_root=self.approved_root,
            evidence_root=self.evidence_root,
            art_import_run_id="art_import_test_04",
            visual_ids=["doll_zero_dialogue_neutral"],
            mode="interactive",
            unity_instance="UnityClient@test1234",
            permissions={"allow_approved_sync": True},
        )
        run_path = self.evidence_root / "art_import_test_04"
        with self.assertRaisesRegex(ArtImportError, "approved_authorization_required"):
            verify_sync_authorization(run_path, authorize_approved_sync=False)
        with self.assertRaisesRegex(ArtImportError, "new Approved target"):
            verify_sync_authorization(run_path, authorize_approved_sync=True)

    def test_record_approved_sync_accepts_new_target_without_meta(self) -> None:
        create_plan(
            manifest_path=self.manifest_path,
            incoming_root=self.incoming_root,
            approved_root=self.approved_root,
            evidence_root=self.evidence_root,
            art_import_run_id="art_import_test_05",
            visual_ids=["doll_zero_dialogue_neutral"],
            mode="interactive",
            unity_instance="UnityClient@test1234",
            permissions={
                "allow_approved_sync": True,
                "allow_new_approved_target": True,
            },
        )
        run_path = self.evidence_root / "art_import_test_05"
        verify_sync_authorization(run_path, authorize_approved_sync=True)
        self.output_path.parent.mkdir(parents=True, exist_ok=True)
        self.output_path.write_bytes(self.selected_path.read_bytes())
        result = record_approved_sync(run_path)
        self.assertEqual(result["items"][0]["meta_status"], "awaiting_unity_import")
        self.assertEqual(result["items"][0]["approved_sha256"], sha256_file(self.selected_path))

    def test_record_approved_sync_preserves_existing_meta(self) -> None:
        self.output_path.parent.mkdir(parents=True, exist_ok=True)
        self.write_image(self.output_path, (1024, 1536), (1, 2, 3, 255))
        meta_path = self.output_path.with_name(self.output_path.name + ".meta")
        meta_path.write_text("fileFormatVersion: 2\nguid: 0123456789abcdef0123456789abcdef\n", encoding="utf-8")
        create_plan(
            manifest_path=self.manifest_path,
            incoming_root=self.incoming_root,
            approved_root=self.approved_root,
            evidence_root=self.evidence_root,
            art_import_run_id="art_import_test_06",
            visual_ids=["doll_zero_dialogue_neutral"],
            mode="interactive",
            unity_instance="UnityClient@test1234",
            permissions={
                "allow_approved_sync": True,
                "allow_existing_target_overwrite": True,
            },
        )
        run_path = self.evidence_root / "art_import_test_06"
        verify_sync_authorization(run_path, authorize_approved_sync=True)
        self.output_path.write_bytes(self.selected_path.read_bytes())
        result = record_approved_sync(run_path)
        self.assertEqual(result["items"][0]["meta_status"], "preserved")
        self.assertEqual(result["items"][0]["meta_guid"], "0123456789abcdef0123456789abcdef")


if __name__ == "__main__":
    unittest.main()
