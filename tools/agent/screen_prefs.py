# -*- coding: utf-8 -*-
"""Keep test runs from changing how the producer's game window opens.

Unity saves the window size and mode of the last run in the registry (HKCU\\Software\\DefaultCompany\\Project P3,
values "Screenmanager ..."), and the next normal launch reuses them, so a test run started with
-screen-width 1280 -screen-height 720 left the playtest build opening as a small 1280x720 window (found 2026-10-25).
Launchers call `backup` before starting the build and `restore` after it quits.

    python tools/agent/screen_prefs.py backup | restore | reset | show
"""

from __future__ import annotations

import json
import sys
import winreg
from pathlib import Path

KEY = r"Software\DefaultCompany\Project P3"
PREFIX = "Screenmanager"
BACKUP = Path(__file__).resolve().parents[2] / "tmp" / "screen_prefs_backup.json"


def read() -> dict:
    values = {}
    try:
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, KEY) as key:
            i = 0
            while True:
                try:
                    name, data, kind = winreg.EnumValue(key, i)
                except OSError:
                    break
                if name.startswith(PREFIX):
                    values[name] = [data.hex() if isinstance(data, bytes) else data, kind]
                i += 1
    except FileNotFoundError:
        pass
    return values


def write(values: dict) -> None:
    current = read()
    with winreg.CreateKey(winreg.HKEY_CURRENT_USER, KEY) as key:
        for name in current:
            if name not in values:
                winreg.DeleteValue(key, name)
        for name, (data, kind) in values.items():
            winreg.SetValueEx(key, name, 0, kind, bytes.fromhex(data) if kind == winreg.REG_BINARY else data)


def main(argv: list[str]) -> int:
    command = argv[0] if argv else "show"
    if command == "backup":
        # Keep the oldest backup: a second launch before a restore must not save the test's own settings.
        if not BACKUP.exists():
            BACKUP.parent.mkdir(parents=True, exist_ok=True)
            BACKUP.write_text(json.dumps(read(), indent=1), encoding="utf-8")
        return 0
    if command == "restore":
        if BACKUP.exists():
            write(json.loads(BACKUP.read_text(encoding="utf-8")))
            BACKUP.unlink()
        return 0
    if command == "reset":
        # Forget the saved window: the next launch opens with the build's defaults (fullscreen at desktop size).
        write({name: value for name, value in read().items() if "Default" in name})
        BACKUP.unlink(missing_ok=True)
        return 0
    for name, (data, _) in sorted(read().items()):
        print(f"{name} = {data}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
