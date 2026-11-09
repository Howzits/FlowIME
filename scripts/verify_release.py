"""Verify an existing release artifact without rebuilding it or requiring a local EXE."""
import argparse
import hashlib
import json
from pathlib import Path
import re
from verify_package import verify


def verify_release(source, artifacts, tag):
    if not re.fullmatch(r"v\d+\.\d+\.\d+", tag):
        raise ValueError("Only stable version tags such as v0.4.2 are accepted")
    metadata = json.loads((source / "package.json").read_text(encoding="utf-8"))
    if tag != "v" + metadata["version"]:
        raise ValueError("Release tag and source version differ")
    name = f"FlowIME-{metadata['version']}-win32-x64.vsix"
    checksum = (artifacts / "SHA256SUMS.txt").read_text(encoding="ascii").strip()
    match = re.fullmatch(r"([0-9a-f]{64})  " + re.escape(name), checksum)
    if not match:
        raise ValueError("Checksum must name exactly the expected release artifact")
    path = artifacts / name
    if hashlib.sha256(path.read_bytes()).hexdigest() != match.group(1):
        raise ValueError("Release checksum mismatch")
    verify(path, metadata, source_root=source, compare_helper=False)
    return path


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--artifacts", type=Path, required=True)
    parser.add_argument("--tag", required=True)
    args = parser.parse_args()
    print("Release artifact verified: " + str(verify_release(args.source, args.artifacts, args.tag)))
