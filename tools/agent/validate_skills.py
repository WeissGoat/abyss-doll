import argparse
import re
import subprocess
import sys
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]

SOURCE_DIR = Path(".codex/skills")
FORWARDER_DIR = Path(".claude/skills")
MISSION_SOURCE = Path("tools/p3-mission")
MISSION_COPY = SOURCE_DIR / "p3-mission"
MISSION_SOURCE_ONLY = {"README.md", ".gitignore"}
ENTRY_DOC_GLOBS = ("AGENTS.md", "rules/*.md", "知识库/README.md", "知识库/views/*.md")

FRONT_MATTER = re.compile(r"\A---\n(.*?)\n---\n", re.S)
FENCE = re.compile(r"^\s*(```|~~~)")
CODE_SPAN = re.compile(r"`([^`\n]+)`")
LINK_TARGET = re.compile(r"\]\(([^)\s]+)\)")
PLACEHOLDER = re.compile(r"[<>*{}$?|]|\.\.\.")
SKILL_NAME_TOKEN = re.compile(r"`(p3-[a-z0-9-]+)`")
TOKEN_STRIP = "\"'`,;:()[]。，；：、"


def read_text(path):
    return path.read_text(encoding="utf-8-sig").replace("\r\n", "\n")


def front_matter(text):
    match = FRONT_MATTER.match(text)
    if not match:
        return None, {}
    block = match.group(1)
    fields = {}
    for line in block.splitlines():
        key, separator, value = line.partition(":")
        if separator and key and not key.startswith((" ", "-")):
            fields[key.strip()] = value.strip()
    return block, fields


def files_under(directory):
    return sorted(
        path
        for path in directory.rglob("*")
        if path.is_file() and "__pycache__" not in path.parts
    )


def load_skills(root, errors):
    skills = {}
    source_root = root / SOURCE_DIR
    if not source_root.is_dir():
        errors.append(f"missing skill source directory: {SOURCE_DIR.as_posix()}")
        return skills
    for directory in sorted(path for path in source_root.iterdir() if path.is_dir()):
        skill_file = directory / "SKILL.md"
        relative = directory.relative_to(root).as_posix()
        if not skill_file.exists():
            if files_under(directory):
                errors.append(f"skill directory has files but no SKILL.md: {relative}")
            continue
        block, fields = front_matter(read_text(skill_file))
        name = fields.get("name", "")
        if not name or not fields.get("description"):
            errors.append(f"SKILL.md front matter needs name and description: {relative}/SKILL.md")
            continue
        if name in skills:
            errors.append(f"duplicate skill name '{name}': {skills[name]['dir']} and {relative}")
            continue
        skills[name] = {"dir": relative, "front_matter": block}
    return skills


def check_forwarders(root, skills, errors):
    forwarder_root = root / FORWARDER_DIR
    seen = set()
    for name, skill in sorted(skills.items()):
        forwarder = forwarder_root / name / "SKILL.md"
        relative = (FORWARDER_DIR / name / "SKILL.md").as_posix()
        if not forwarder.exists():
            errors.append(f"missing Claude Code forwarder for '{name}': {relative}")
            continue
        seen.add(name)
        text = read_text(forwarder)
        block, _fields = front_matter(text)
        if block != skill["front_matter"]:
            errors.append(f"forwarder front matter differs from {skill['dir']}/SKILL.md: {relative}")
        if f"`{skill['dir']}/SKILL.md`" not in text:
            errors.append(f"forwarder does not point at {skill['dir']}/SKILL.md: {relative}")
    if forwarder_root.is_dir():
        for directory in sorted(path for path in forwarder_root.iterdir() if path.is_dir()):
            if directory.name not in seen and directory.name not in skills:
                errors.append(f"forwarder has no source skill: {(FORWARDER_DIR / directory.name).as_posix()}")
    return len(seen)


def check_mission_copy(root, errors):
    source, copy = root / MISSION_SOURCE, root / MISSION_COPY
    if not source.is_dir() and not copy.is_dir():
        return
    if not source.is_dir() or not copy.is_dir():
        errors.append(f"p3-mission needs both {MISSION_SOURCE.as_posix()} and {MISSION_COPY.as_posix()}")
        return
    source_files = {
        path.relative_to(source).as_posix(): path
        for path in files_under(source)
        if path.relative_to(source).as_posix() not in MISSION_SOURCE_ONLY
    }
    copy_files = {path.relative_to(copy).as_posix(): path for path in files_under(copy)}
    for relative in sorted(source_files.keys() - copy_files.keys()):
        errors.append(f"p3-mission file missing from {MISSION_COPY.as_posix()}: {relative}")
    for relative in sorted(copy_files.keys() - source_files.keys()):
        errors.append(f"p3-mission file missing from {MISSION_SOURCE.as_posix()}: {relative}")
    for relative in sorted(source_files.keys() & copy_files.keys()):
        if read_text(source_files[relative]) != read_text(copy_files[relative]):
            errors.append(f"p3-mission copies differ: {relative}")


def reference_tokens(text):
    in_fence = False
    for number, line in enumerate(text.splitlines(), start=1):
        if FENCE.match(line):
            in_fence = not in_fence
            continue
        chunks = [line] if in_fence else CODE_SPAN.findall(line)
        for chunk in chunks:
            for token in chunk.split():
                yield number, token


def normalize_path_token(token):
    token = token.strip(TOKEN_STRIP).replace("\\", "/")
    while token.startswith("./"):
        token = token[2:]
    if "/" not in token or "://" in token or PLACEHOLDER.search(token):
        return None
    return token.rstrip("/")


def ignored_or_external(root, paths):
    def git(*args):
        return subprocess.run(
            ["git", *args], cwd=root, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL
        ).returncode

    if not paths or git("rev-parse", "--is-inside-work-tree") != 0:
        return set()
    # check-ignore exits 0 for ignored paths, 1 for paths git would track, 128 inside a submodule.
    return {path for path in paths if git("check-ignore", "-q", path) != 1}


def entry_names(directory):
    return {path.name for path in directory.iterdir()}


def check_references(root, skills, errors, warnings):
    missing = []
    root_entries = entry_names(root)
    for skill in sorted(skills.values(), key=lambda item: item["dir"]):
        skill_dir = root / skill["dir"]
        skill_entries = entry_names(skill_dir)
        for document in files_under(skill_dir):
            if document.suffix != ".md":
                continue
            text = read_text(document)
            relative_doc = document.relative_to(root).as_posix()
            for number, line in enumerate(text.splitlines(), start=1):
                for target in LINK_TARGET.findall(line):
                    if "://" in target or target.startswith(("#", "mailto:")):
                        continue
                    if not (document.parent / target.split("#", 1)[0]).exists():
                        errors.append(f"{relative_doc}:{number} links to a missing file: {target}")
            for number, token in reference_tokens(text):
                path = normalize_path_token(token)
                if path is None:
                    continue
                first = path.split("/", 1)[0]
                if first in skill_entries:
                    if not (skill_dir / path).exists():
                        missing.append((relative_doc, number, (Path(skill["dir"]) / path).as_posix()))
                elif first in root_entries and not (root / path).exists():
                    missing.append((relative_doc, number, path))
    skipped = ignored_or_external(root, sorted({path for _doc, _line, path in missing}))
    for relative_doc, number, path in missing:
        if path in skipped:
            continue
        message = f"{relative_doc}:{number} references a missing path: {path}"
        if path.startswith(SOURCE_DIR.as_posix() + "/"):
            errors.append(message)
        else:
            warnings.append(message)


def check_skill_names(root, skills, warnings):
    documents = set()
    for pattern in ENTRY_DOC_GLOBS:
        documents.update(path for path in root.glob(pattern) if path.is_file())
    for skill in skills.values():
        documents.update(path for path in (root / skill["dir"]).rglob("*.md"))
    for document in sorted(documents):
        relative_doc = document.relative_to(root).as_posix()
        for number, line in enumerate(read_text(document).splitlines(), start=1):
            for name in SKILL_NAME_TOKEN.findall(line):
                if name not in skills:
                    warnings.append(f"{relative_doc}:{number} names an unknown skill: {name}")


def validate(root):
    errors, warnings = [], []
    skills = load_skills(root, errors)
    forwarders = check_forwarders(root, skills, errors)
    check_mission_copy(root, errors)
    check_references(root, skills, errors, warnings)
    check_skill_names(root, skills, warnings)
    return errors, warnings, {"skills": len(skills), "forwarders": forwarders}


def main():
    parser = argparse.ArgumentParser(description="Validate project skills, Claude Code forwarders and skill references.")
    parser.add_argument("--root", default=str(REPO_ROOT))
    args = parser.parse_args()

    errors, warnings, stats = validate(Path(args.root).resolve())
    for warning in warnings:
        print(f"[skills] WARN: {warning}")
    if errors:
        for error in errors:
            print(f"[skills] ERROR: {error}", file=sys.stderr)
        return 1
    print(
        "[skills] validation passed. "
        f"skills={stats['skills']}, forwarders={stats['forwarders']}, warnings={len(warnings)}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
