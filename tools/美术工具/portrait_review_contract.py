# -*- coding: utf-8 -*-
"""Shared validation rules for character portrait replacement reviews."""

from __future__ import annotations

import copy
from typing import Any

PORTRAIT_DIMENSIONS = ("Identity", "Costume", "Proportion", "Framing", "Technical", "TargetFit")
DEFAULT_PROTECTED_DIMENSIONS = PORTRAIT_DIMENSIONS[:5]
PORTRAIT_RUBRIC_VERSION = "character_portrait_v3"


def normalize_portrait_scores(scores: Any) -> dict[str, int]:
    if not isinstance(scores, dict):
        raise ValueError("portrait_scores_invalid")
    result: dict[str, int] = {}
    for name in PORTRAIT_DIMENSIONS:
        if name not in scores:
            raise ValueError(f"portrait_score_missing:{name}")
        value = scores[name]
        if not isinstance(value, int) or isinstance(value, bool) or not 0 <= value <= 100:
            raise ValueError(f"portrait_score_invalid:{name}")
        result[name] = value
    total = scores.get("Total")
    expected = round(sum(result.values()) / len(PORTRAIT_DIMENSIONS))
    if total != expected:
        raise ValueError("portrait_score_total_mismatch")
    result["Total"] = expected
    return result


def validate_dimension_evidence(evidence: Any) -> dict[str, dict[str, Any]]:
    if not isinstance(evidence, dict):
        raise ValueError("portrait_dimension_evidence_invalid")
    result: dict[str, dict[str, Any]] = {}
    findings: list[str] = []
    for name in PORTRAIT_DIMENSIONS:
        item = evidence.get(name)
        if not isinstance(item, dict):
            raise ValueError(f"portrait_dimension_evidence_missing:{name}")
        finding = str(item.get("Finding", "")).strip()
        items = item.get("Evidence")
        if not finding or not isinstance(items, list) or not any(str(value).strip() for value in items):
            raise ValueError(f"portrait_dimension_evidence_invalid:{name}")
        findings.append(" ".join(finding.lower().split()))
        result[name] = copy.deepcopy(item)
    if len(set(findings)) == 1:
        raise ValueError("portrait_dimension_evidence_not_independent")
    return result


def validate_portrait_review_item(item: Any, *, require_selection_threshold: bool = False) -> dict[str, Any]:
    if not isinstance(item, dict) or item.get("ReviewRubricVersion") != PORTRAIT_RUBRIC_VERSION:
        raise ValueError("portrait_review_rubric_invalid")
    result = copy.deepcopy(item)
    result["Scores"] = normalize_portrait_scores(item.get("Scores"))
    result["DimensionEvidence"] = validate_dimension_evidence(item.get("DimensionEvidence"))
    if require_selection_threshold:
        if result["Scores"]["TargetFit"] < 88:
            raise ValueError("portrait_target_fit_below_selection_threshold")
        if result["Scores"]["Total"] < 88:
            raise ValueError("portrait_total_below_selection_threshold")
    return result
