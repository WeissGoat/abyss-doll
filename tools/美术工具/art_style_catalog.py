# -*- coding: utf-8 -*-
"""Normalize and resolve the Project P3 art style catalog."""

from __future__ import annotations

import copy
import hashlib
import json
from pathlib import Path
from typing import Any


DEFAULT_CATALOG: dict[str, Any] = {
    "Version": 1,
    "GlobalStyle": {
        "ID": "p3_global_v1",
        "PositiveEN": [
            "Japanese anime-inspired 2D game art",
            "subterranean fantasy adventure",
            "whimsical yet ominous",
            "soft cel-shaded painterly rendering",
            "clean linework",
            "airy fantasy atmosphere",
        ],
        "NegativeEN": [
            "western dark fantasy",
            "photorealistic rendering",
            "oil painting",
            "cyberpunk UI",
            "dominant steampunk machinery",
            "blue-white UI wash",
        ],
    },
    "Profiles": {
        "ui_v1": {
            "PositiveEN": [
                "clean hand-painted fantasy game UI",
                "low information density",
                "readable 2D interface component",
                "ink-gray and deep graphite panel base",
                "warm moon-white structural trim",
                "restrained coral-crimson action accents",
            ],
            "NegativeEN": [
                "fake readable text",
                "dense button wall",
                "blue-white primary palette",
                "neon cyan main surface",
                "icy blue-white gradient",
            ],
        },
        "character_portrait_v1": {
            "PositiveEN": [
                "full-body anime game character portrait",
                "clear readable silhouette",
                "transparent background",
            ],
            "NegativeEN": ["cropped head", "cropped feet", "busy background"],
        },
        "environment_v1": {
            "PositiveEN": [
                "hand-painted anime environment",
                "atmospheric depth",
                "large readable negative space",
                "storybook color balance with warm life and localized bioluminescence",
            ],
            "NegativeEN": ["bright daylight", "busy foreground clutter", "blue-white UI wash"],
        },
        "icon_v1": {
            "PositiveEN": ["centered single object", "clean readable silhouette"],
            "NegativeEN": ["text", "letters", "numbers"],
        },
    },
    "Families": {
        "ui_button_core_v1": {
            "Profile": "ui_v1",
            "PositiveEN": [
                "single horizontal button skin",
                "continuous outer frame",
                "clean stretchable center",
                "ink-gray center plate",
                "warm moon-white rim",
                "label-free center area",
            ],
            "NegativeEN": [
                "disconnected border",
                "fragmented ornaments",
                "blue-white gradient",
                "icy blue highlight",
            ],
            "Roles": {
                "primary": {
                    "PositiveEN": [
                        "crimson primary action surface",
                        "coral-crimson primary action accent",
                        "warm moon-white edge highlight",
                    ],
                    "NegativeEN": [
                        "dominant blue or cyan primary surface",
                        "blue-white primary surface",
                    ],
                },
                "secondary": {
                    "PositiveEN": ["quiet secondary action surface"],
                },
                "danger": {
                    "PositiveEN": ["restrained red danger feedback surface"],
                },
            },
        },
        "ui_panel_core_v1": {
            "Profile": "ui_v1",
            "PositiveEN": ["modular fantasy interface panel", "ink-gray panel base"],
            "NegativeEN": ["realistic western console", "baked UI text"],
            "Roles": {},
        },
    },
    "ContextAccents": {
        "none": {"PositiveEN": [], "NegativeEN": []},
        "workshop": {"PositiveEN": ["subtle worn repair-tool accent"], "NegativeEN": []},
        "combat": {"PositiveEN": ["restrained danger feedback accent"], "NegativeEN": []},
        "map": {"PositiveEN": ["clear route-reading accent"], "NegativeEN": []},
    },
}


def canonical_json(value: Any) -> str:
    return json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":"))


def sha256_json(value: Any) -> str:
    return hashlib.sha256(canonical_json(value).encode("utf-8")).hexdigest()


def _error(code: str, detail: str = "") -> ValueError:
    return ValueError(f"{code}{':' + detail if detail else ''}")


def _positive(node: Any) -> list[str]:
    if not isinstance(node, dict):
        return []
    return [str(value) for value in node.get("PositiveEN", []) if str(value).strip()]


def _negative(node: Any) -> list[str]:
    if not isinstance(node, dict):
        return []
    return [str(value) for value in node.get("NegativeEN", []) if str(value).strip()]


def _source_sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest() if path.exists() else "missing"


def validate_catalog(catalog: dict[str, Any]) -> list[str]:
    errors: list[str] = []
    if not isinstance(catalog, dict):
        return ["style_catalog_invalid"]
    global_style = catalog.get("GlobalStyle")
    if not isinstance(global_style, dict) or not global_style.get("ID"):
        errors.append("style_global_missing")

    profiles = catalog.get("Profiles")
    families = catalog.get("Families")
    accents = catalog.get("ContextAccents")
    if not isinstance(profiles, dict):
        errors.append("style_profiles_missing")
        profiles = {}
    if not isinstance(families, dict):
        errors.append("style_families_missing")
        families = {}
    if not isinstance(accents, dict):
        errors.append("style_context_accents_missing")
        accents = {}

    for family_id, family in families.items():
        if not isinstance(family, dict):
            errors.append(f"style_family_invalid:{family_id}")
            continue
        profile_id = str(family.get("Profile", ""))
        if profile_id not in profiles:
            errors.append(f"style_family_profile_missing:{family_id}")
        allowed = profiles.get(profile_id, {}).get("Families") if isinstance(profiles.get(profile_id), dict) else None
        if isinstance(allowed, list) and family_id not in allowed:
            errors.append(f"style_family_profile_mismatch:{family_id}")
        roles = family.get("Roles", {})
        if not isinstance(roles, dict):
            errors.append(f"style_roles_invalid:{family_id}")

    if "none" not in accents:
        errors.append("style_context_accent_missing:none")
    return errors


def resolve_style_ref(catalog: dict[str, Any], style_ref: dict[str, Any]) -> dict[str, Any]:
    errors = validate_catalog(catalog)
    if errors:
        raise _error(errors[0])
    if not isinstance(style_ref, dict):
        raise _error("style_ref_missing")

    global_style = catalog["GlobalStyle"]
    profiles = catalog["Profiles"]
    families = catalog["Families"]
    accents = catalog["ContextAccents"]
    profile_id = str(style_ref.get("Profile", ""))
    if profile_id not in profiles:
        raise _error("style_profile_missing", profile_id)
    profile = profiles[profile_id]

    result: dict[str, Any] = {
        "Global": copy.deepcopy(global_style),
        "Profile": copy.deepcopy(profile),
        "Family": {},
        "Role": {},
        "ContextAccent": {},
        "ResolvedLayers": [f"global:{global_style['ID']}", f"profile:{profile_id}"],
    }

    family_id = str(style_ref.get("Family", ""))
    if family_id:
        family = families.get(family_id)
        if not isinstance(family, dict):
            raise _error("style_family_missing", family_id)
        if str(family.get("Profile", "")) != profile_id:
            raise _error("style_family_profile_mismatch", family_id)
        allowed = profile.get("Families")
        if isinstance(allowed, list) and family_id not in allowed:
            raise _error("style_family_profile_mismatch", family_id)
        result["Family"] = copy.deepcopy(family)
        result["ResolvedLayers"].append(f"family:{family_id}")

        roles = family.get("Roles", {})
        if isinstance(roles, dict) and roles:
            role_id = str(style_ref.get("Role", ""))
            if not role_id:
                raise _error("style_role_missing", family_id)
            role = roles.get(role_id)
            if not isinstance(role, dict):
                raise _error("style_role_missing", role_id)
            result["Role"] = copy.deepcopy(role)
            result["ResolvedLayers"].append(f"role:{role_id}")

    accent_id = str(style_ref.get("ContextAccent", "none") or "none")
    if accent_id not in accents:
        raise _error("style_context_accent_not_allowed", accent_id)
    result["ContextAccent"] = copy.deepcopy(accents[accent_id])
    result["ResolvedLayers"].append(f"accent:{accent_id}")
    return result


def resolve_entry_layers(
    entry: dict[str, Any],
    catalog: dict[str, Any],
    asset_sets: dict[str, Any],
) -> dict[str, Any]:
    profile_name = str(entry.get("ProductionProfile", "standard_asset"))
    asset_set_id = str(entry.get("AssetSetID", ""))
    asset_set = asset_sets.get(asset_set_id, {}) if isinstance(asset_sets, dict) else {}
    if profile_name == "character_portrait_set":
        if not asset_set_id or not isinstance(asset_set, dict) or not asset_set:
            raise _error("asset_set_missing", asset_set_id or str(entry.get("VisualID", "")))
        style_ref = copy.deepcopy(asset_set.get("StyleRef", {}))
        style_ref.update(entry.get("StyleRef", {}) if isinstance(entry.get("StyleRef"), dict) else {})
    else:
        style_ref = copy.deepcopy(entry.get("StyleRef", {}))
        if not style_ref:
            style_ref = {"Profile": "icon_v1" if entry.get("AssetType") == "icon" else "environment_v1"}

    resolved = resolve_style_ref(catalog, style_ref)
    if profile_name == "character_portrait_set":
        resolved["IdentityLocks"] = copy.deepcopy(asset_set.get("IdentityLocks", []))
        resolved["IdentitySources"] = copy.deepcopy(asset_set.get("IdentitySources", []))
        resolved["ConsistencyRules"] = copy.deepcopy(asset_set.get("ConsistencyRules", []))
        resolved["SetRole"] = entry.get("SetRole", "")
        resolved["SourceAssets"] = copy.deepcopy(entry.get("SourceAssets", []))
    return resolved


def build_catalog_snapshot(
    project_root: Path,
    existing: dict[str, Any] | None = None,
    *,
    refresh: bool = False,
) -> dict[str, Any]:
    existing_catalog = (
        existing.get("ArtStyleCatalog")
        if isinstance(existing, dict) and not refresh
        else None
    )
    catalog = copy.deepcopy(existing_catalog) if isinstance(existing_catalog, dict) else copy.deepcopy(DEFAULT_CATALOG)
    catalog.setdefault("Version", 1)
    source_paths = [
        "美术文档/04_美术风格基准.md",
        "美术文档/ui_design/design_tokens.json",
        "美术文档/art_requirements_seed.json",
    ]
    catalog["Sources"] = [
        {"Path": path, "SHA256": _source_sha256(project_root / path)} for path in source_paths
    ]
    payload = copy.deepcopy(catalog)
    payload.pop("CatalogFingerprint", None)
    catalog["CatalogFingerprint"] = sha256_json(payload)
    return catalog
