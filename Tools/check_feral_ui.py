"""Structural checks independent of the Unity editor. Run from project root."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
scene_dir = root/'Assets/Scenes/UIReview'
script_dir = root/'Assets/FeralUI/Scripts'
# Only the added UI block is checked below. Avoid hydrating unrelated OneDrive
# assets or scanning every package to resolve these seven standard uGUI types.
all_meta = list((root/'Assets/FeralUI').rglob('*.meta'))
all_meta.append(root/'Assets/TextMesh Pro/Fonts/LiberationSans.ttf.meta')
for package in (root/'Library/PackageCache').glob('com.unity.ugui*'):
    core = package/'Runtime/UGUI/UI/Core'
    for name in ['Image', 'Text', 'RawImage', 'Button', 'Slider', 'GraphicRaycaster', 'Layout/CanvasScaler']:
        all_meta.append(core/(name+'.cs.meta'))
known_guids = set()
for meta in all_meta:
    match = re.search(r'^guid: (\w+)', Path('\\\\?\\'+str(meta)).read_text(encoding='utf-8-sig'), re.M)
    if match: known_guids.add(match[1])

for filename, original in [('Feral_MainMenu.unity','SampleScene.unity'),('Game.unity','Prototipo1.unity')]:
    scene = (scene_dir/filename).read_text(encoding='utf-8')
    source = (root/'Assets/Scenes'/original).read_text(encoding='utf-8')
    original_body, original_roots = source.split('--- !u!1660057539',1)
    assert scene.startswith(original_body), f'Original content modified in {filename}'
    assert scene.split('--- !u!1660057539',1)[1].startswith(original_roots), 'Original roots lost'
    new_body = scene[len(original_body):].split('--- !u!1660057539',1)[0]
    ids = re.findall(r'^--- !u!\d+ &(\d+)', scene,re.M)
    assert len(ids)==len(set(ids)), 'Duplicate file IDs'
    names = re.findall(r'^  m_Name: (.+)$',new_body,re.M)
    assert len(names)==len(set(names)), 'Duplicate UI names make binding ambiguous'
    for line in new_body.splitlines():
        if 'fileID:' in line and 'guid:' not in line:
            for ident in re.findall(r'fileID: (\d+)',line):
                assert ident=='0' or ident in ids, f'Missing object {ident}'
        for value in re.findall(r'guid: (\w+)',line):
            assert value in known_guids, f'Missing asset {value}'
    controller = 'FeralMainMenu.cs' if filename.startswith('Feral_Main') else 'FeralInventoryUI.cs'
    script = (script_dir/controller).read_text(encoding='utf-8')
    for name in re.findall(r'(?:Bind|Find<[^>]+>)\(transform, "([^"]+)"\)',script):
        assert name in names, f'Missing runtime binding {name}'
    # Bind includes a third argument, so validate its literal names separately.
    for name in re.findall(r'Bind\(transform, "([^"]+)"',script):
        assert name in names, f'Missing button {name}'
    if controller=='FeralInventoryUI.cs':
        for item in ['Madera','Metal','Desperdicios','Semillas','Goma']:
            for prefix in ['Card','Count','Icon']:
                assert prefix+item in names
    print(f'PASS {filename}: originals preserved, unique IDs/names, references and bindings valid')

build = (root/'ProjectSettings/EditorBuildSettings.asset').read_text()
assert build.index('Feral_MainMenu.unity') < build.index('Game.unity')
assert 'RodrigoScene_UIReview.unity' not in build
assert 'Assets/Scenes/UIReview/Game.unity' in (script_dir/'FeralUICommon.cs').read_text()
assert 'Assets/Scenes/RodrigoScene.unity' in build
print('PASS build: menu first, inventory copy included, original scene retained')
