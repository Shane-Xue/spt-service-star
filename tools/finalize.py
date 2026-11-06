"""Normalize Windows PowerShell source encoding and verify release contents."""
from pathlib import Path
import hashlib
import json
import zipfile

project = Path(__file__).resolve().parents[1]
root = project.parents[1]
for path in project.rglob("*.ps1"):
    source = path.read_text(encoding="utf-8-sig")
    path.write_text(source, encoding="utf-8-sig", newline="\n")

# Windows PowerShell 5 interpreted the first deployment script's UTF-8 source
# using the legacy ANSI code page. Remove only that generated duplicate note.
wrong_note = root / "\u93c8\ue100\u6e74\u93c7\u5b58\u657c\u93c3\u30e5\u7e54.md"
if wrong_note.exists():
    text = wrong_note.read_text(encoding="utf-8")
    assert wrong_note.parent == root and wrong_note.stat().st_size < 2000
    assert "Service Stars 1.1.0" in text and "BepInEx/plugins/ServiceStars" in text
    wrong_note.unlink()

archive = project / "dist/ServiceStars-1.2.1-SPT-4.1.6.zip"
expected = {
    "BepInEx/plugins/ServiceStars/ServiceStars.Client.dll": project / "artifacts/ServiceStars.Client.dll",
    "SPT_Runtime/user/mods/ServiceStars/ServiceStars.Server.dll": project / "artifacts/ServiceStars.Server.dll",
    "SPT_Runtime/user/mods/ServiceStars/config.json": project / "config.json",
    "ServiceStars-README.md": project / "README.md",
}
with zipfile.ZipFile(archive) as z:
    assert set(z.namelist()) == set(expected)
    assert z.testzip() is None
    for name, source in expected.items():
        assert z.read(name) == source.read_bytes() == (root / name).read_bytes(), name
        print("PASS package / deployed / source:", name)

assert json.loads((root / "SPT_Runtime/user/mods/ServiceStars/config.json").read_text())["KillsPerStar"] == 100
report = {
    "version": "1.2.1",
    "automatic_assertions": 56,
    "assembly_binding_checks": 16,
    "live_game_tested": False,
    "files": {name: hashlib.sha256(source.read_bytes()).hexdigest() for name, source in expected.items()},
    "archive_sha256": hashlib.sha256(archive.read_bytes()).hexdigest(),
}
(project / "artifacts/release-verification.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
print("PASS explicit release allowlist, ZIP integrity, defaults, deployment hashes")
