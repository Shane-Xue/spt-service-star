"""Package the compiled publication build without touching the installation."""
from pathlib import Path
import hashlib
import zipfile

root = Path(__file__).resolve().parents[1]
archive = root / 'releases/ServiceStars-1.2.1-SPT-4.1.6.zip'
files = {
    'BepInEx/plugins/ServiceStars/ServiceStars.Client.dll': root / 'artifacts/ServiceStars.Client.dll',
    'SPT_Runtime/user/mods/ServiceStars/ServiceStars.Server.dll': root / 'artifacts/ServiceStars.Server.dll',
    'SPT_Runtime/user/mods/ServiceStars/config.json': root / 'config.json',
    'ServiceStars-README.md': root / 'README.md',
    'LICENSE': root / 'LICENSE',
}
archive.parent.mkdir(exist_ok=True)
with zipfile.ZipFile(archive, 'w', zipfile.ZIP_DEFLATED) as z:
    for name, path in files.items(): z.write(path, name)
with zipfile.ZipFile(archive) as z:
    assert z.testzip() is None and set(z.namelist()) == set(files)
    for name, path in files.items(): assert z.read(name) == path.read_bytes()
print('Publication ZIP verified:', archive)
print('SHA256:', hashlib.sha256(archive.read_bytes()).hexdigest())
