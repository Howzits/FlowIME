"""Validate the one official VSIX used by Marketplace and GitHub Releases."""
import argparse
import hashlib
import json
from pathlib import Path
from zipfile import ZipFile
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parent.parent
NS = {"v": "http://schemas.microsoft.com/developer/vsx-schema/2011"}


def verify(path, expected, source_root=ROOT, compare_helper=True):
    with ZipFile(path) as archive:
        if archive.testzip():
            raise ValueError("Corrupt ZIP member")
        package = json.loads(archive.read("extension/package.json"))
        for key in ("name", "publisher", "version", "displayName", "author"):
            if package.get(key) != expected.get(key):
                raise ValueError(f"Package {key} differs from source")
        if package["publisher"] != "FlowIME":
            raise ValueError("Unexpected publisher; expected FlowIME")
        manifest = ET.fromstring(archive.read("extension.vsixmanifest"))
        identity = manifest.find("v:Metadata/v:Identity", NS)
        for key, value in {"Id": package["name"], "Publisher": "FlowIME", "Version": package["version"], "TargetPlatform": "win32-x64"}.items():
            if identity is None or identity.get(key) != value:
                raise ValueError(f"Invalid manifest {key}")
        icon_path = "extension/" + package["icon"]
        icon = archive.read(icon_path)
        if not icon.startswith(b"\x89PNG\r\n\x1a\n") or len(icon) > 1024 * 1024:
            raise ValueError("Icon must be PNG and at most 1024 KB")
        asset_icons = manifest.findall("v:Assets/v:Asset", NS)
        if not any(a.get("Type") == "Microsoft.VisualStudio.Services.Icons.Default" and a.get("Path") == icon_path for a in asset_icons):
            raise ValueError("Missing VSIX icon asset")
        localized = ("package.nls.json", "package.nls.zh.json", "package.nls.zh-cn.json", "l10n/bundle.l10n.zh.json", "l10n/bundle.l10n.zh-cn.json") if expected.get("l10n") else ()
        for member in localized + ("extension.js", "bin/InputHelper.exe", package["icon"], "README.en.md", "README.zh-CN.md", "docs/USAGE.md", "docs/COMPATIBILITY.md"):
            packaged = archive.read("extension/" + member)
            if member == "bin/InputHelper.exe" and not compare_helper:
                if not packaged.startswith(b"MZ"):
                    raise ValueError("Missing Windows native helper")
                continue
            if packaged != (source_root / member).read_bytes():
                raise ValueError(f"Runtime file differs from source: {member}")
        # vsce normalizes names and rewrites README links for the public repository.
        readme = archive.read("extension/readme.md").decode("utf-8")
        if "Switch less. Stay in flow." not in readme or "README.zh-CN.md" not in readme:
            raise ValueError("Marketplace Details must use English with a Chinese documentation link")
        if any(name.startswith(("extension/.validation/", "extension/node_modules/", "extension/.github/", "extension/dist/", "extension/native/")) for name in archive.namelist()):
            raise ValueError("Build-only files leaked into VSIX")
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def main():
    expected = json.loads((ROOT / "package.json").read_text(encoding="utf-8"))
    parser = argparse.ArgumentParser()
    parser.add_argument("--vsix", type=Path, default=ROOT / "dist" / f"FlowIME-{expected['version']}-win32-x64.vsix")
    args = parser.parse_args()
    digest = verify(args.vsix, expected)
    print(f"Verified {expected['displayName']} {expected['version']} / win32-x64; SHA-256 {digest}")


if __name__ == "__main__":
    main()
