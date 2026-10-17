"""Apply a config agent's content draft to 配置表(JSON)/ after review.

A draft folder (e.g. tmp/agent_reports/content_poison/) holds:
  Items/<ConfigID>.json   new items, copied as they are (an existing ConfigID is refused)
  pools.json              additions to drop tables and the shop:
      {"File": "Rewards/<file>.json", "PoolID": "<pool>", "Entry": {...}}
      {"File": "Economy/economy_town_v1.json", "ShopStock": "<ConfigID>"}

Additions are inserted as text, so the hand-kept layout of the existing files stays as it is.

Usage: python tools/config/apply_content_draft.py <draft folder> [--dry-run]
Then run tools/config/Sync-Configs.ps1 -Clean and the Unity config validation.
"""
import json
import re
import shutil
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
CONFIG = ROOT / "配置表(JSON)"


def load(path):
    return json.loads(path.read_text(encoding="utf-8-sig"))


def closing_bracket(text, open_index):
    """Index of the bracket closing the one at open_index, skipping strings."""
    depth = 0
    in_string = False
    i = open_index
    while i < len(text):
        ch = text[i]
        if in_string:
            if ch == "\\":
                i += 1
            elif ch == '"':
                in_string = False
        elif ch == '"':
            in_string = True
        elif ch in "[{":
            depth += 1
        elif ch in "]}":
            depth -= 1
            if depth == 0:
                return i
        i += 1
    raise ValueError("unbalanced brackets")


def append_to_array(text, open_index, value):
    """Append a JSON value to the array opening at open_index, in the array's own style."""
    close = closing_bracket(text, open_index)
    body = text[open_index + 1:close]
    if "\n" not in body:
        compact = json.dumps(value, ensure_ascii=False)
        if isinstance(value, dict):
            compact = "{ " + compact[1:-1] + " }"
        separator = ", " if body.strip() else ""
        return text[:close] + separator + compact + text[close:]

    # One element per line: copy the indentation of the last element.
    last_line_start = body.rstrip().rfind("\n") + 1
    last_line = body[last_line_start:]
    indent = re.match(r"[ \t]*", last_line).group(0)
    newline = "\r\n" if "\r\n" in text else "\n"
    element = json.dumps(value, ensure_ascii=False)
    if isinstance(value, dict) and last_line.strip().startswith("{") and last_line.strip().endswith(("}", "},")):
        element = "{ " + element[1:-1] + " }"
    elif isinstance(value, dict):
        element = json.dumps(value, ensure_ascii=False, indent=2).replace("\n", newline + indent)
    end_of_content = open_index + 1 + len(body.rstrip())
    return text[:end_of_content] + "," + newline + indent + element + text[end_of_content:]


def add_pool_entry(text, pool_id, entry):
    match = re.search(r'"PoolID"\s*:\s*"' + re.escape(pool_id) + '"', text)
    if not match:
        raise ValueError(f"no pool {pool_id}")
    entries = re.compile(r'"Entries"\s*:\s*\[').search(text, match.end())
    if not entries:
        raise ValueError(f"pool {pool_id} has no Entries")
    return append_to_array(text, entries.end() - 1, entry)


def add_shop_stock(text, config_id):
    match = re.search(r'"ShopStock"\s*:\s*\[', text)
    if not match:
        raise ValueError("no ShopStock")
    return append_to_array(text, match.end() - 1, config_id)


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    dry = "--dry-run" in sys.argv
    if len(args) != 1:
        sys.exit(__doc__)

    draft = Path(args[0]).resolve()
    changes = []

    for item_file in sorted((draft / "Items").glob("*.json")):
        item = load(item_file)
        target = CONFIG / "Items" / f"{item['ConfigID']}.json"
        if target.exists():
            sys.exit(f"refused: {target.name} already exists")
        changes.append(f"new item {item['ConfigID']} ({item.get('Name')})")
        if not dry:
            shutil.copyfile(item_file, target)

    pools_file = draft / "pools.json"
    texts = {}
    for addition in load(pools_file) if pools_file.exists() else []:
        path = CONFIG / addition["File"]
        text = texts.get(path) or path.read_text(encoding="utf-8-sig")
        data = json.loads(text)
        if "ShopStock" in addition:
            if addition["ShopStock"] in data.get("ShopStock", []):
                continue
            text = add_shop_stock(text, addition["ShopStock"])
            changes.append(f"shop + {addition['ShopStock']}")
        else:
            pool = next((p for p in data.get("WeightedPools", []) if p.get("PoolID") == addition["PoolID"]), None)
            if pool is None:
                sys.exit(f"refused: {addition['File']} has no pool {addition['PoolID']}")
            entry = addition["Entry"]
            if any(e.get("ItemID") == entry.get("ItemID") for e in pool.get("Entries", [])):
                continue
            text = add_pool_entry(text, addition["PoolID"], entry)
            changes.append(f"{addition['File']}:{addition['PoolID']} + {entry.get('ItemID')} (weight {entry.get('Weight')})")
        json.loads(text)  # still valid JSON
        texts[path] = text

    if not dry:
        for path, text in texts.items():
            bom = b"\xef\xbb\xbf" if path.read_bytes().startswith(b"\xef\xbb\xbf") else b""
            path.write_bytes(bom + text.encode("utf-8"))

    print(("[dry run] " if dry else "") + f"{len(changes)} change(s):")
    for change in changes:
        print("  " + change)


if __name__ == "__main__":
    main()
