"""Save live MuMain skill captures through its documented local test socket.

Launch the reference with tools/launch-visual-reference.cmd and log in first.
Examples:
  python tools/capture_mumain_skill.py --state
  python tools/capture_mumain_skill.py 9 Publish/SkillComparison/MuMain/9

Capture times are relative to the client's cast acknowledgement, including wind-up.
These captures are evidence to review, not an automatic visual parity verdict.
"""
import argparse
import json
from pathlib import Path
import socket
import time

SOCKET_PATH = "C:/MuOnline_Source/MuMain-main/out/visual-reference.sock"
CAPTURE_SECONDS = (0.04, 0.20, 0.48, 0.96, 1.92, 2.56)


def request(command):
    with socket.socket(socket.AF_UNIX, socket.SOCK_STREAM) as connection:
        connection.settimeout(20)
        connection.connect(SOCKET_PATH)
        connection.sendall((json.dumps(command) + "\n").encode("utf-8"))
        with connection.makefile("rb") as response:
            line = response.readline()
            if not line:
                raise RuntimeError("MuMain closed the test connection without replying")
            return json.loads(line)


def sanitized_state():
    response = request({"cmd": "state"})
    if not response.get("ok"):
        raise RuntimeError(json.dumps(response))
    state = response["result"]
    # Account identifiers and inventory are unnecessary for visual evidence.
    fields = ("scene", "class", "class_name", "map", "map_name", "position", "skills", "safe_zone")
    return {key: state[key] for key in fields if key in state}


def capture(skill, output, target=None):
    output.mkdir(parents=True, exist_ok=True)
    state = sanitized_state()
    if state.get("scene") != "world":
        raise RuntimeError("Log the reference character into the game first")
    command = {"cmd": "skill", "skill": skill}
    if target is not None:
        command["target"] = target
    records = []
    cast_result = request(command)
    if not cast_result.get("ok"):
        (output / "cast-failed.json").write_text(json.dumps(cast_result, indent=2), encoding="utf-8")
        raise RuntimeError("MuMain refused the cast; review cast-failed.json")
    started = time.monotonic()
    for seconds in CAPTURE_SECONDS:
        remaining = started + seconds - time.monotonic()
        if remaining > 0:
            time.sleep(remaining)
        path = output / f"skill-{skill}-elapsed-{round(seconds * 1000):04d}ms.jpg"
        sent = time.monotonic() - started
        result = request({"cmd": "screenshot", "out": str(path.resolve()), "quality": 100})
        records.append({"planned_seconds": seconds, "sent_seconds": sent, "response": result})
    evidence = {"skill": skill, "state": state, "cast": cast_result, "captures": records,
                "status": "captured; visual parity unverified",
                "timing": "relative to cast acknowledgement, including wind-up; not renderer frame numbers"}
    (output / "capture-state.json").write_text(json.dumps(evidence, indent=2), encoding="utf-8")
    if not cast_result.get("ok") or any(not row["response"].get("ok") for row in records):
        raise RuntimeError("Cast or screenshot failed; review capture-state.json")
    print(f"Saved {len(records)} reference captures for skill {skill} to {output}")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("skill", nargs="?", type=int)
    parser.add_argument("output", nargs="?", type=Path)
    parser.add_argument("--target", type=int)
    parser.add_argument("--state", action="store_true")
    args = parser.parse_args()
    if args.state:
        print(json.dumps(sanitized_state(), indent=2))
        return
    if args.skill is None or args.output is None:
        parser.error("provide a skill number and output folder, or --state")
    capture(args.skill, args.output, args.target)


if __name__ == "__main__":
    main()
