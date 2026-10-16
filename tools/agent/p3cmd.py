"""Drive a running P3 build through its command port (P3CommandPort, started with -p3cmd).

  python tools/agent/p3cmd.py launch [build_dir_or_exe] [--port N] [--prologue]
      Starts the build windowed at 1280x720 with the port open and no save, waits until it answers, prints the pid.
      Without a path it takes the newest folder in Builds/Windows. Skips the prologue unless --prologue.
  python tools/agent/p3cmd.py [--port N] <command ...>
      Sends one command and prints the reply text: a P3DevTools method with its arguments (Describe, Click 继续,
      RunDive 40 true, GoTo combat, Occlusion, DragCheck) or help, ping, wait <s>, screenshot [path] [maxWidth], quit.
      Exit code 1 when the game reports a failure or cannot be reached.
"""
import json
import os
import socket
import subprocess
import sys
import time
from pathlib import Path

DEFAULT_PORT = 47823
REPO = Path(__file__).resolve().parents[2]


def send(command, port=DEFAULT_PORT, timeout=200):
    with socket.create_connection(("127.0.0.1", port), timeout=timeout) as conn:
        conn.sendall((command + "\n").encode("utf-8"))
        data = b""
        while not data.endswith(b"\n"):
            chunk = conn.recv(65536)
            if not chunk:
                break
            data += chunk
    return json.loads(data.decode("utf-8"))


def resolve_exe(target):
    if target:
        path = Path(target)
        return path if path.suffix.lower() == ".exe" else path / "P3.exe"
    builds = [p for p in (REPO / "Builds" / "Windows").iterdir() if p.is_dir() and (p / "P3.exe").exists()]
    if not builds:
        sys.exit("No build in Builds/Windows; build one first.")
    return max(builds, key=lambda p: p.stat().st_mtime) / "P3.exe"


def launch(args):
    port = DEFAULT_PORT
    prologue = "--prologue" in args
    rest = [a for a in args if a != "--prologue"]
    if "--port" in rest:
        i = rest.index("--port")
        port = int(rest[i + 1])
        del rest[i:i + 2]
    exe = resolve_exe(rest[0] if rest else None)
    flags = ["-p3cmd", str(port), "-p3nosave", "-screen-fullscreen", "0", "-screen-width", "1280", "-screen-height", "720"]
    if not prologue:
        flags.append("-p3skipprologue")
    proc = subprocess.Popen([str(exe)] + flags, cwd=str(exe.parent))
    deadline = time.time() + 90
    while time.time() < deadline:
        try:
            if send("ping", port, timeout=5).get("result") == "pong":
                print(f"Running {exe} (pid {proc.pid}) on port {port}.")
                return 0
        except OSError:
            pass
        if proc.poll() is not None:
            sys.exit(f"The build exited with code {proc.returncode} before answering.")
        time.sleep(1)
    sys.exit("The build did not answer within 90 s.")


def main(argv):
    if not argv or argv[0] in ("-h", "--help"):
        print(__doc__)
        return 0
    if argv[0] == "launch":
        return launch(argv[1:])
    port = DEFAULT_PORT
    if argv[0] == "--port":
        port, argv = int(argv[1]), argv[2:]
    if argv[0].lower() == "screenshot" and len(argv) > 1 and not argv[1].isdigit():
        argv[1] = str(Path(argv[1]).resolve())  # the game's working directory is the build folder, not ours
    command = " ".join(f'"{a}"' if " " in a else a for a in argv)
    try:
        reply = send(command, port)
    except OSError as e:
        print(f"Cannot reach the game on port {port}: {e}")
        return 1
    print(reply.get("result", ""))
    return 0 if reply.get("ok") else 1


if __name__ == "__main__":
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    sys.exit(main(sys.argv[1:]))
