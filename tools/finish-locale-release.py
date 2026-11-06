from pathlib import Path
import shutil
import zipfile
project = Path(__file__).resolve().parents[1]
root = project.parents[1]
# Locale is a client-only update. Preserve the deployed server's identical
# protocol/behavior rather than interrupting its running process for metadata.
shutil.copy2(root / 'SPT_Runtime/user/mods/ServiceStars/ServiceStars.Server.dll', project / 'artifacts/ServiceStars.Server.dll')
shutil.copy2(project / 'README.md', root / 'ServiceStars-README.md')
files = {
    'BepInEx/plugins/ServiceStars/ServiceStars.Client.dll': project / 'artifacts/ServiceStars.Client.dll',
    'SPT_Runtime/user/mods/ServiceStars/ServiceStars.Server.dll': project / 'artifacts/ServiceStars.Server.dll',
    'SPT_Runtime/user/mods/ServiceStars/config.json': project / 'config.json',
    'ServiceStars-README.md': project / 'README.md',
}
with zipfile.ZipFile(project / 'dist/ServiceStars-1.2.1-SPT-4.1.6.zip', 'w', zipfile.ZIP_DEFLATED) as archive:
    for name, source in files.items(): archive.write(source, name)
note = '''
## 2026-11-06 — Service Stars 1.2.1 locale update

- Deployed client 1.2.1: inspect text and promotion notifications follow EFT LocalizationManager.Culture. Chinese uses ch, English uses en; untranslated locales fall back to English. Removed the manual Chinese setting; weapon names use game localization.
- Text resources: mods-dev/service-stars/assets/locales.json, embedded in the client DLL. Existing gameplay progress and language settings were not changed.
- Server DLL was locked by a running process. Kept the installed, protocol-compatible 1.2.0 server unchanged; no server restart or termination was performed. The 1.2.1 distribution bundles that same server DLL.
- Build, 56 automated assertions and 16 assembly API checks passed. Actual in-game language switching remains to be verified. Restart the game to load the updated client.
'''
log = root / '\u672c\u5730\u66f4\u6539\u65e5\u5fd7.md'
with log.open('a', encoding='utf-8') as output: output.write(note)
print('Client 1.2.1 installed; running server 1.2.0 preserved; distribution rebuilt.')
