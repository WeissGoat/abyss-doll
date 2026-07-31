# -*- coding: utf-8 -*-

from __future__ import annotations

import copy
import hashlib
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image, ImageDraw


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from art_prompt_revision import publish_prompt_revision  # noqa: E402
from compile_art_generation_requests import compile_manifest_requests  # noqa: E402
from register_art_processing_round import recalculate_candidate_review  # noqa: E402
from run_character_portrait_set import (  # noqa: E402
    build_portrait_set_plan,
    execute_portrait_set_run,
    load_asset_set,
    order_portrait_members,
    reference_cli_arguments,
)


class CharacterPortraitSetTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        self.project_root = Path(self.temp_dir.name)
        approved_path = self.project_root / "UnityClient/Assets/Art/Approved/Dolls/doll_zero_dialogue_neutral.png"
        approved_path.parent.mkdir(parents=True, exist_ok=True)
        Image.new("RGBA", (1024, 1536), (220, 220, 220, 180)).save(approved_path, format="PNG")
        self.approved_path = approved_path
        self.manifest = {
            "Version": 1,
            "Entries": [
                {
                    "VisualID": "doll_zero_dialogue_neutral",
                    "Domain": "doll",
                    "AssetType": "portrait",
                    "DisplayName": "零号中性",
                    "ProductionProfile": "character_portrait_set",
                    "AssetSetID": "zero_dialogue_portrait_v1",
                    "AssetID": "zero_dialogue_neutral",
                    "SetRole": "neutral_dialogue_master",
                    "SourceAssets": [],
                    "Status": "approved",
                    "ApprovedPath": "UnityClient/Assets/Art/Approved/Dolls/doll_zero_dialogue_neutral.png",
                    "OutputPath": "UnityClient/Assets/Art/Approved/Dolls/doll_zero_dialogue_neutral.png",
                    "Spec": {"SourceSpec": {"Format": "png", "Width": 1024, "Height": 1536, "AlphaRequired": True}},
                },
                {
                    "VisualID": "doll_zero_dialogue_confused",
                    "Domain": "doll",
                    "AssetType": "portrait",
                    "DisplayName": "零号疑惑",
                    "ProductionProfile": "character_portrait_set",
                    "AssetSetID": "zero_dialogue_portrait_v1",
                    "AssetID": "zero_dialogue_confused",
                    "SetRole": "confused_expression_difference",
                    "SourceAssets": [{"asset_id": "zero_dialogue_neutral", "role": "identity_reference"}],
                    "Status": "selected",
                    "Spec": {"SourceSpec": {"Format": "png", "Width": 1024, "Height": 1536, "AlphaRequired": True}},
                },
            ],
        }
        compiled = compile_manifest_requests(copy.deepcopy(self.manifest), project_root=Path.cwd())
        self.compiled_manifest = compiled["Manifest"]
        self.catalog = compiled["Catalog"]
        for index, request in enumerate(list(self.catalog["Requests"])):
            hard_ids = [
                item["ID"]
                for values in request["PromptAuthoringContext"]["HardConstraints"].values()
                for item in values
            ]
            mapping = {constraint_id: [f"covered:{constraint_id}"] for constraint_id in hard_ids}
            revision = {
                "PromptRevisionID": f"{request['RequestID']}/prompt-001",
                "RequirementFingerprint": request["RequirementFingerprint"],
                "AuthoringMode": "agent_authored",
                "Status": "ready",
                "CommonStrategy": {"Operation": "character_portrait_member"},
                "Variants": {
                    "natural_language_v2": {
                        "Format": "natural_language_v2",
                        "Status": "ready",
                        "Positive": f"Create {request['VisualID']} while preserving the locked identity.",
                        "Negative": "identity drift",
                        "OutputContract": {},
                        "ConstraintMapping": copy.deepcopy(mapping),
                    },
                    "danbooru_tags_v2": {
                        "Format": "danbooru_tags_v2",
                        "Status": "ready",
                        "PositiveTags": [{"Tag": "1girl", "Weight": 1.0}],
                        "NegativeTags": [{"Tag": "identity drift", "Weight": 1.0}],
                        "ReferenceControls": {},
                        "ConstraintMapping": copy.deepcopy(mapping),
                    },
                },
            }
            published = publish_prompt_revision(request, revision, activate=True)
            self.catalog["Requests"][index] = published
            pointer = next(
                entry["CompiledRequest"]
                for entry in self.compiled_manifest["Entries"]
                if entry["VisualID"] == request["VisualID"]
            )
            pointer["PromptAuthoringStatus"] = "prompt_ready"
            pointer["ActivePromptRevisionID"] = revision["PromptRevisionID"]
        self.manifest_path = self.project_root / "manifest.json"
        self.catalog_path = self.project_root / "catalog.json"
        self.incoming_root = self.project_root / "UnityClient/Assets/Art/_IncomingAI"
        self.run_dir = self.project_root / "UnityClient/Logs/P3ArtProduction/portrait_test_001"
        self.manifest_path.write_text(json.dumps(self.compiled_manifest), encoding="utf-8")
        self.catalog_path.write_text(json.dumps(self.catalog), encoding="utf-8")

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    def test_powershell_wrapper_dry_run_uses_python_utf8_defaults(self) -> None:
        wrapper = TOOLS_DIR / "Run-CharacterPortraitSet.ps1"
        with tempfile.TemporaryDirectory() as temp_dir:
            manifest_path = Path(temp_dir) / "manifest.json"
            catalog_path = Path(temp_dir) / "catalog.json"
            manifest_path.write_text(__import__("json").dumps(self.compiled_manifest), encoding="utf-8")
            catalog_path.write_text(__import__("json").dumps(self.catalog), encoding="utf-8")
            completed = subprocess.run(
                [
                    "powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", str(wrapper),
                    "-AssetSetID", "zero_dialogue_portrait_v1",
                    "-ManifestPath", str(manifest_path),
                    "-RequestCatalogPath", str(catalog_path),
                    "-VisualID", "doll_zero_dialogue_neutral",
                    "-PromptFormat", "natural_language_v2",
                    "-DryRun",
                ],
                cwd=TOOLS_DIR.parents[1], capture_output=True, text=True, encoding="utf-8",
                errors="replace", timeout=30, check=False,
            )

        self.assertEqual(completed.returncode, 0, completed.stderr or completed.stdout)
        self.assertIn('"VisualID": "doll_zero_dialogue_neutral"', completed.stdout)

    def test_missing_asset_set_is_rejected(self) -> None:
        with self.assertRaisesRegex(ValueError, "asset_set_missing"):
            load_asset_set(self.compiled_manifest, "missing")

    def test_master_is_ordered_before_difference(self) -> None:
        asset_set = load_asset_set(self.compiled_manifest, "zero_dialogue_portrait_v1")
        ordered = order_portrait_members(asset_set, list(reversed(self.compiled_manifest["Entries"])))
        self.assertEqual(ordered[0]["AssetID"], "zero_dialogue_neutral")
        self.assertEqual(ordered[1]["AssetID"], "zero_dialogue_confused")

    def test_missing_source_is_rejected(self) -> None:
        entries = copy.deepcopy(self.compiled_manifest["Entries"])
        entries[1]["SourceAssets"] = [{"asset_id": "missing", "role": "identity_reference"}]
        with self.assertRaisesRegex(ValueError, "asset_set_source_missing"):
            order_portrait_members(load_asset_set(self.compiled_manifest, "zero_dialogue_portrait_v1"), entries)

    def test_dependency_cycle_is_rejected(self) -> None:
        entries = copy.deepcopy(self.compiled_manifest["Entries"])
        entries[0]["SourceAssets"] = [{"asset_id": "zero_dialogue_confused", "role": "identity_reference"}]
        with self.assertRaisesRegex(ValueError, "asset_set_dependency_cycle"):
            order_portrait_members(load_asset_set(self.compiled_manifest, "zero_dialogue_portrait_v1"), entries)

    def test_valid_set_plan_contains_request_and_preservation_contract(self) -> None:
        plan = build_portrait_set_plan(
            self.compiled_manifest,
            self.catalog,
            "zero_dialogue_portrait_v1",
            project_root=self.project_root,
        )
        self.assertEqual(plan["State"], "ready")
        self.assertEqual(plan["Items"][0]["VisualID"], "doll_zero_dialogue_neutral")
        self.assertTrue(plan["Items"][1]["RequestID"])
        self.assertTrue(plan["Items"][1]["PromptRevisionID"])
        self.assertEqual(plan["Items"][1]["PromptFormat"], "natural_language_v2")
        self.assertEqual(
            plan["Items"][1]["PromptVariantSnapshot"]["Positive"],
            "Create doll_zero_dialogue_confused while preserving the locked identity.",
        )
        self.assertEqual(
            plan["Items"][1]["PromptRevisionSnapshot"]["PromptRevisionID"],
            plan["Items"][1]["PromptRevisionID"],
        )
        self.assertIn("Preserve", plan["Items"][1]["PreservationContract"])
        self.assertIn("ReferenceAssets", plan["Items"][1])
        self.assertEqual(len(plan["Items"][1]["ResolvedReferenceAssets"]), 1)
        reference = plan["Items"][1]["ResolvedReferenceAssets"][0]
        self.assertEqual(reference["AssetID"], "zero_dialogue_neutral")
        self.assertEqual(reference["State"], "approved")
        self.assertEqual(
            reference["SHA256"], hashlib.sha256(self.approved_path.read_bytes()).hexdigest()
        )

    def test_novelai_selects_danbooru_revision_variant(self) -> None:
        plan = build_portrait_set_plan(
            self.compiled_manifest,
            self.catalog,
            "zero_dialogue_portrait_v1",
            provider="novelai",
            project_root=self.project_root,
        )
        self.assertEqual(plan["Items"][0]["PromptFormat"], "danbooru_tags_v2")

    def test_missing_real_reference_blocks_the_plan(self) -> None:
        self.approved_path.unlink()
        plan = build_portrait_set_plan(
            self.compiled_manifest,
            self.catalog,
            "zero_dialogue_portrait_v1",
            visual_ids={"doll_zero_dialogue_confused"},
            project_root=self.project_root,
        )

        self.assertEqual(plan["State"], "decision_required")
        self.assertEqual(plan["Items"], [])
        self.assertTrue(
            any(error.startswith("portrait_reference_file_missing:zero_dialogue_neutral") for error in plan["Errors"]),
            plan["Errors"],
        )

    def test_member_without_sources_has_empty_resolved_references(self) -> None:
        plan = build_portrait_set_plan(
            self.compiled_manifest,
            self.catalog,
            "zero_dialogue_portrait_v1",
            visual_ids={"doll_zero_dialogue_neutral"},
            project_root=self.project_root,
        )

        self.assertEqual(plan["State"], "ready")
        self.assertEqual(plan["Items"][0]["ResolvedReferenceAssets"], [])

    def test_resolved_references_are_serialized_for_the_child_generator(self) -> None:
        plan = build_portrait_set_plan(
            self.compiled_manifest,
            self.catalog,
            "zero_dialogue_portrait_v1",
            visual_ids={"doll_zero_dialogue_confused"},
            project_root=self.project_root,
        )
        reference = plan["Items"][0]["ResolvedReferenceAssets"][0]

        arguments = reference_cli_arguments(plan["Items"][0]["ResolvedReferenceAssets"])

        self.assertEqual(
            arguments,
            [
                "--reference-image",
                reference["Path"],
                "--reference-image-sha256",
                reference["SHA256"],
                "--reference-image-role",
                "identity_reference",
            ],
        )

    def test_missing_active_revision_is_rejected(self) -> None:
        catalog = copy.deepcopy(self.catalog)
        request = catalog["Requests"][0]
        request["PromptAuthoringStatus"] = "prompt_authoring_required"
        request["ActivePromptRevisionID"] = ""
        request["PromptRevisions"] = []
        manifest = copy.deepcopy(self.compiled_manifest)
        pointer = next(
            entry["CompiledRequest"]
            for entry in manifest["Entries"]
            if entry["VisualID"] == request["VisualID"]
        )
        pointer["PromptAuthoringStatus"] = "prompt_authoring_required"
        pointer["ActivePromptRevisionID"] = ""

        plan = build_portrait_set_plan(
            manifest,
            catalog,
            "zero_dialogue_portrait_v1",
            project_root=self.project_root,
        )
        self.assertIn(f"prompt_authoring_required:{request['VisualID']}", plan["Errors"])

    def make_runner(self, plan: dict, calls: list[str], *, failures: set[str] | None = None):
        items = {item["VisualID"]: item for item in plan["Items"]}
        failures = failures or set()

        def runner(command, cwd=None):
            visual_id = command[command.index("--visual-id") + 1]
            calls.append(visual_id)
            if visual_id in failures:
                return subprocess.CompletedProcess(command, 1)
            item = items[visual_id]
            workspace = self.project_root / item["Workspace"]
            raw = workspace / "raw" / "fake.png"
            raw.parent.mkdir(parents=True, exist_ok=True)
            image = Image.new("RGBA", (1024, 1536), (0, 0, 0, 0))
            ImageDraw.Draw(image).rectangle((40, 40, 984, 1496), fill=(80, 90, 100, 255))
            image.save(raw)
            generation = {
                "EvidenceMode": "formal_v2",
                "VisualID": visual_id,
                "BatchID": "portrait_test_001",
                "RequirementFingerprint": item["RequirementFingerprint"],
                "PromptRevisionID": item["PromptRevisionID"],
                "PromptRevisionFingerprint": item["PromptRevisionFingerprint"],
                "PromptFormat": item["PromptFormat"],
                "ReferenceImages": copy.deepcopy(item["ResolvedReferenceAssets"]),
                "Outputs": [{"RepoPath": raw.relative_to(self.project_root).as_posix()}],
                "Errors": [],
            }
            (workspace / "generation.json").write_text(json.dumps(generation), encoding="utf-8")
            return subprocess.CompletedProcess(command, 0)

        return runner

    def execute_run(
        self,
        plan: dict,
        *,
        resume: bool = False,
        provider: str = "fake",
        runner=None,
        processing_decisions_path: Path | None = None,
        visual_review_path: Path | None = None,
        allowed_technical_overrides: set[str] | None = None,
    ) -> dict:
        return execute_portrait_set_run(
            plan=plan,
            manifest_path=self.manifest_path,
            catalog_path=self.catalog_path,
            incoming_root=self.incoming_root,
            run_dir=self.run_dir,
            production_run_id="portrait_test_001",
            execution_mode="interactive",
            resume=resume,
            provider=provider,
            config="",
            variants=1,
            execute_limit=10,
            processing_decisions_path=processing_decisions_path,
            visual_review_path=visual_review_path,
            allow_selected_overwrite=False,
            allowed_technical_overrides=allowed_technical_overrides or set(),
            project_root=self.project_root,
            runner=runner or self.make_runner(plan, []),
        )

    def test_resume_skips_generation_when_snapshot_is_current(self) -> None:
        plan = build_portrait_set_plan(
            self.compiled_manifest,
            self.catalog,
            "zero_dialogue_portrait_v1",
            visual_ids={"doll_zero_dialogue_neutral"},
            project_root=self.project_root,
        )
        first_calls: list[str] = []
        first = self.execute_run(plan, runner=self.make_runner(plan, first_calls))
        self.assertEqual(first_calls, ["doll_zero_dialogue_neutral"])
        self.assertEqual(first["Items"][0]["Stage"], "processing_decision")

        resume_calls: list[str] = []
        resumed = self.execute_run(plan, resume=True, runner=self.make_runner(plan, resume_calls))

        self.assertEqual(resume_calls, [])
        self.assertEqual(resumed["Items"][0]["Stage"], "processing_decision")

    def test_existing_run_requires_resume(self) -> None:
        plan = build_portrait_set_plan(
            self.compiled_manifest,
            self.catalog,
            "zero_dialogue_portrait_v1",
            visual_ids={"doll_zero_dialogue_neutral"},
            project_root=self.project_root,
        )
        self.run_dir.mkdir(parents=True)
        with self.assertRaisesRegex(FileExistsError, "resume_required"):
            self.execute_run(plan)

    def test_resume_rejects_stale_prompt_snapshot_without_provider_call(self) -> None:
        plan = build_portrait_set_plan(
            self.compiled_manifest,
            self.catalog,
            "zero_dialogue_portrait_v1",
            visual_ids={"doll_zero_dialogue_neutral"},
            project_root=self.project_root,
        )
        self.execute_run(plan)
        changed = copy.deepcopy(plan)
        changed["Items"][0]["PromptRevisionFingerprint"] = "changed"
        calls: list[str] = []

        resumed = self.execute_run(changed, resume=True, provider="", runner=self.make_runner(changed, calls))

        self.assertEqual(calls, [])
        self.assertEqual(resumed["Items"][0]["Result"], "prompt_stale")

    def test_resume_rejects_changed_raw_output_sha(self) -> None:
        plan = build_portrait_set_plan(
            self.compiled_manifest,
            self.catalog,
            "zero_dialogue_portrait_v1",
            visual_ids={"doll_zero_dialogue_neutral"},
            project_root=self.project_root,
        )
        self.execute_run(plan)
        raw = self.project_root / plan["Items"][0]["Workspace"] / "raw/fake.png"
        Image.new("RGBA", (1024, 1536), (0, 0, 0, 0)).save(raw)
        calls: list[str] = []

        resumed = self.execute_run(plan, resume=True, provider="", runner=self.make_runner(plan, calls))

        self.assertEqual(calls, [])
        self.assertEqual(resumed["Items"][0]["Result"], "generation_stale")

    def test_resume_does_not_trust_tampered_selection_checkpoint(self) -> None:
        plan = build_portrait_set_plan(
            self.compiled_manifest,
            self.catalog,
            "zero_dialogue_portrait_v1",
            visual_ids={"doll_zero_dialogue_neutral"},
            project_root=self.project_root,
        )
        state = self.execute_run(plan)
        selected = self.project_root / plan["Items"][0]["Workspace"] / "selected" / "tampered.png"
        selected.parent.mkdir(parents=True, exist_ok=True)
        Image.new("RGBA", (1024, 1536), (1, 2, 3, 255)).save(selected)
        digest = hashlib.sha256(selected.read_bytes()).hexdigest()
        state["Items"][0]["Selection"] = {
            "State": "selected",
            "SelectedPath": selected.relative_to(self.project_root).as_posix(),
            "SHA256": digest,
        }
        state["Items"][0]["Stage"] = "complete"
        state["Items"][0]["Result"] = "selected"
        (self.run_dir / "portrait-set-run.json").write_text(json.dumps(state), encoding="utf-8")

        resumed = self.execute_run(plan, resume=True, provider="")

        self.assertNotEqual(resumed["Items"][0]["Result"], "selected")
        self.assertEqual(resumed["Items"][0]["PendingDecision"], "processing_decision")

    def test_resume_does_not_fallback_to_old_processed_round_after_latest_failure(self) -> None:
        plan = build_portrait_set_plan(
            self.compiled_manifest,
            self.catalog,
            "zero_dialogue_portrait_v1",
            visual_ids={"doll_zero_dialogue_neutral"},
            project_root=self.project_root,
        )
        state = self.execute_run(plan)
        workspace = self.project_root / plan["Items"][0]["Workspace"]
        old_candidate = workspace / "processed" / "1" / "candidate.png"
        old_candidate.parent.mkdir(parents=True, exist_ok=True)
        Image.new("RGBA", (1024, 1536), (80, 90, 100, 255)).save(old_candidate)
        old_hash = hashlib.sha256(old_candidate.read_bytes()).hexdigest()
        (old_candidate.parent / "decision.json").write_text(
            json.dumps({"State": "passed", "Candidates": [{"File": "candidate.png", "Status": "passed", "SHA256": old_hash}]}),
            encoding="utf-8",
        )
        latest_candidate = workspace / "processed" / "2" / "candidate.png"
        latest_candidate.parent.mkdir(parents=True, exist_ok=True)
        Image.new("RGBA", (1024, 1536), (0, 0, 0, 0)).save(latest_candidate)
        (latest_candidate.parent / "decision.json").write_text(
            json.dumps({"State": "failed", "Candidates": [{"File": "candidate.png", "Status": "failed"}]}),
            encoding="utf-8",
        )
        selected = workspace / "selected" / "portrait.png"
        selected.parent.mkdir(parents=True, exist_ok=True)
        selected.write_bytes(old_candidate.read_bytes())
        state["Items"][0]["Processed"] = {
            "RoundNumber": 1,
            "CandidatePath": old_candidate.relative_to(self.project_root).as_posix(),
            "CandidateSHA256": old_hash,
        }
        state["Items"][0]["Selection"] = {
            "State": "selected",
            "SelectedPath": selected.relative_to(self.project_root).as_posix(),
            "SHA256": old_hash,
            "ProcessedRound": 1,
        }
        state["Items"][0]["Stage"] = "complete"
        state["Items"][0]["Result"] = "selected"
        (self.run_dir / "portrait-set-run.json").write_text(json.dumps(state), encoding="utf-8")

        resumed = self.execute_run(plan, resume=True, provider="")

        self.assertNotEqual(resumed["Items"][0]["Result"], "selected")

    def test_visual_review_requires_candidate_sha(self) -> None:
        plan = build_portrait_set_plan(
            self.compiled_manifest,
            self.catalog,
            "zero_dialogue_portrait_v1",
            visual_ids={"doll_zero_dialogue_neutral"},
            project_root=self.project_root,
        )
        self.execute_run(plan)
        raw = self.project_root / plan["Items"][0]["Workspace"] / "raw/fake.png"
        raw_hash = hashlib.sha256(raw.read_bytes()).hexdigest()
        decisions_path = self.project_root / "processing-decisions.json"
        decisions_path.write_text(
            json.dumps(
                {
                    "ProductionRunID": "portrait_test_001",
                    "Items": [
                        {
                            "VisualID": "doll_zero_dialogue_neutral",
                            "Action": "already_usable",
                            "InputPath": raw.relative_to(self.project_root).as_posix(),
                            "InputSHA256": raw_hash,
                        }
                    ],
                }
            ),
            encoding="utf-8",
        )
        review_path = self.project_root / "visual-review.json"
        review_path.write_text(
            json.dumps(
                {
                    "ProductionRunID": "portrait_test_001",
                    "Items": [
                        {
                            "VisualID": "doll_zero_dialogue_neutral",
                            "HardGate": "passed",
                            "Scores": {"Total": 92},
                            "RecommendedAction": "select",
                        }
                    ],
                }
            ),
            encoding="utf-8",
        )

        with self.assertRaisesRegex(ValueError, "visual_review_candidate_sha_missing"):
            self.execute_run(
                plan,
                resume=True,
                runner=self.make_runner(plan, []),
                processing_decisions_path=decisions_path,
                visual_review_path=review_path,
            )

    def test_authorized_portrait_technical_override_reaches_selection(self) -> None:
        entry = next(item for item in self.compiled_manifest["Entries"] if item["VisualID"] == "doll_zero_dialogue_neutral")
        entry.setdefault("Spec", {})["CompositionSpec"] = {"SafePaddingPercent": 25}
        self.manifest_path.write_text(json.dumps(self.compiled_manifest), encoding="utf-8")
        plan = build_portrait_set_plan(
            self.compiled_manifest,
            self.catalog,
            "zero_dialogue_portrait_v1",
            visual_ids={"doll_zero_dialogue_neutral"},
            project_root=self.project_root,
        )
        calls: list[str] = []
        self.execute_run(plan, runner=self.make_runner(plan, calls))
        raw = self.project_root / plan["Items"][0]["Workspace"] / "raw/fake.png"
        raw_hash = hashlib.sha256(raw.read_bytes()).hexdigest()
        candidate_preview = self.project_root / "candidate.png"
        candidate_preview.write_bytes(raw.read_bytes())
        automatic = recalculate_candidate_review(entry, candidate_preview)
        override = {
            "SchemaVersion": "technical_override_v1",
            "VisualID": "doll_zero_dialogue_neutral",
            "Candidate": "candidate.png",
            "CandidateSHA256": raw_hash,
            "BaseReviewFingerprint": automatic["ReviewFingerprint"],
            "Overrides": [
                {
                    "RuleID": "subject_outside_safe_canvas",
                    "Action": "accept_as_warning",
                    "Reason": "Intentional full-canvas portrait crop.",
                    "Evidence": ["visual_review.json"],
                }
            ],
        }
        decisions_path = self.project_root / "processing-decisions.json"
        decisions_path.write_text(
            json.dumps(
                {
                    "ProductionRunID": "portrait_test_001",
                    "Items": [
                        {
                            "VisualID": "doll_zero_dialogue_neutral",
                            "Action": "already_usable",
                            "InputPath": raw.relative_to(self.project_root).as_posix(),
                            "InputSHA256": raw_hash,
                            "TechnicalOverride": override,
                        }
                    ],
                }
            ),
            encoding="utf-8",
        )
        review_path = self.project_root / "visual-review.json"
        review_path.write_text(
            json.dumps(
                {
                    "ProductionRunID": "portrait_test_001",
                    "Items": [
                        {
                            "VisualID": "doll_zero_dialogue_neutral",
                            "CandidateSHA256": raw_hash,
                            "HardGate": "passed",
                            "Scores": {"Total": 92},
                            "RecommendedAction": "select",
                        }
                    ],
                }
            ),
            encoding="utf-8",
        )

        completed = self.execute_run(
            plan,
            resume=True,
            runner=self.make_runner(plan, calls),
            processing_decisions_path=decisions_path,
            visual_review_path=review_path,
            allowed_technical_overrides={"subject_outside_safe_canvas"},
        )

        self.assertEqual(completed["Items"][0]["Result"], "selected", completed)

    def test_resume_marks_changed_reference_stale(self) -> None:
        plan = build_portrait_set_plan(
            self.compiled_manifest,
            self.catalog,
            "zero_dialogue_portrait_v1",
            visual_ids={"doll_zero_dialogue_confused"},
            project_root=self.project_root,
        )
        self.execute_run(plan)
        Image.new("RGBA", (1024, 1536), (1, 2, 3, 0)).save(self.approved_path)
        changed_plan = build_portrait_set_plan(
            self.compiled_manifest,
            self.catalog,
            "zero_dialogue_portrait_v1",
            visual_ids={"doll_zero_dialogue_confused"},
            project_root=self.project_root,
        )
        calls: list[str] = []

        resumed = self.execute_run(changed_plan, resume=True, provider="", runner=self.make_runner(changed_plan, calls))

        self.assertEqual(calls, [])
        self.assertEqual(resumed["Items"][0]["Result"], "reference_stale")

    def test_generation_failure_blocks_explicit_dependency(self) -> None:
        plan = build_portrait_set_plan(
            self.compiled_manifest,
            self.catalog,
            "zero_dialogue_portrait_v1",
            project_root=self.project_root,
        )
        calls: list[str] = []

        state = self.execute_run(
            plan,
            runner=self.make_runner(plan, calls, failures={"doll_zero_dialogue_neutral"}),
        )

        by_id = {item["VisualID"]: item for item in state["Items"]}
        self.assertEqual(by_id["doll_zero_dialogue_neutral"]["Result"], "generation_failed")
        self.assertEqual(by_id["doll_zero_dialogue_confused"]["Result"], "blocked_by_dependency")
        self.assertEqual(calls, ["doll_zero_dialogue_neutral"])
        self.assertEqual(state["FinalState"], "selection_complete_with_failures")

    def test_resume_skips_registered_round_and_existing_selection(self) -> None:
        plan = build_portrait_set_plan(
            self.compiled_manifest,
            self.catalog,
            "zero_dialogue_portrait_v1",
            visual_ids={"doll_zero_dialogue_neutral"},
            project_root=self.project_root,
        )
        state = self.execute_run(plan)
        item = state["Items"][0]
        candidate = self.incoming_root / "character_portraits/doll_zero_dialogue_neutral/processed/1/candidate.png"
        candidate.parent.mkdir(parents=True, exist_ok=True)
        Image.new("RGBA", (1024, 1536), (80, 90, 100, 255)).save(candidate)
        digest = hashlib.sha256(candidate.read_bytes()).hexdigest()
        (candidate.parent / "decision.json").write_text(
            json.dumps({"State": "passed", "Candidates": [{"File": candidate.name, "SHA256": digest, "Status": "passed"}]}),
            encoding="utf-8",
        )
        selected = candidate.parents[2] / "selected" / "doll_zero_dialogue_neutral.png"
        selected.parent.mkdir(parents=True, exist_ok=True)
        selected.write_bytes(candidate.read_bytes())
        item["Processed"] = {"RoundNumber": 1, "CandidatePath": candidate.relative_to(self.project_root).as_posix(), "CandidateSHA256": digest}
        item["Selection"] = {"State": "selected", "SelectedPath": selected.relative_to(self.project_root).as_posix(), "SHA256": digest}
        item["Stage"] = "complete"
        item["Result"] = "selected"
        (self.run_dir / "portrait-set-run.json").write_text(json.dumps(state), encoding="utf-8")
        calls: list[str] = []

        resumed = self.execute_run(plan, resume=True, runner=self.make_runner(plan, calls))

        self.assertEqual(calls, [])
        self.assertEqual(resumed["Items"][0]["Result"], "selected")
        self.assertEqual(resumed["FinalState"], "selection_complete")

    def test_processing_decision_and_visual_review_complete_selected_route(self) -> None:
        plan = build_portrait_set_plan(
            self.compiled_manifest,
            self.catalog,
            "zero_dialogue_portrait_v1",
            visual_ids={"doll_zero_dialogue_neutral"},
            project_root=self.project_root,
        )
        calls: list[str] = []
        first = self.execute_run(plan, runner=self.make_runner(plan, calls))
        raw = self.project_root / plan["Items"][0]["Workspace"] / "raw/fake.png"
        raw_hash = hashlib.sha256(raw.read_bytes()).hexdigest()
        decisions_path = self.project_root / "processing-decisions.json"
        decisions_path.write_text(
            json.dumps(
                {
                    "ProductionRunID": "portrait_test_001",
                    "Items": [
                        {
                            "VisualID": "doll_zero_dialogue_neutral",
                            "Action": "already_usable",
                            "InputPath": raw.relative_to(self.project_root).as_posix(),
                            "InputSHA256": raw_hash,
                        }
                    ],
                }
            ),
            encoding="utf-8",
        )
        review_path = self.project_root / "visual-review.json"
        review_path.write_text(
            json.dumps(
                {
                    "ProductionRunID": "portrait_test_001",
                    "Items": [
                        {
                            "VisualID": "doll_zero_dialogue_neutral",
                            "CandidateSHA256": raw_hash,
                            "HardGate": "passed",
                            "Scores": {"Total": 92},
                            "RecommendedAction": "select",
                        }
                    ],
                }
            ),
            encoding="utf-8",
        )

        completed = self.execute_run(
            plan,
            resume=True,
            runner=self.make_runner(plan, calls),
            processing_decisions_path=decisions_path,
            visual_review_path=review_path,
        )

        self.assertEqual(calls, ["doll_zero_dialogue_neutral"])
        self.assertEqual(completed["Items"][0]["Result"], "selected")
        self.assertEqual(completed["FinalState"], "selection_complete")
        self.assertTrue((self.incoming_root / "character_portraits/doll_zero_dialogue_neutral/processed/1/candidate.png").exists())
        self.assertTrue((self.incoming_root / "character_portraits/doll_zero_dialogue_neutral/selected/doll_zero_dialogue_neutral.png").exists())


if __name__ == "__main__":
    unittest.main()
