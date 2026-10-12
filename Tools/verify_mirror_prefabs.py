"""Verify serialized mirror prefab assets and scene links without running Unity."""
from pathlib import Path
import re, subprocess
from migrate_mirror_prefabs import ROOT, blocks, field, ref, game_components, subtree, SURFACE

def check():
    backup = sorted((ROOT / 'Tools/Backups').glob('SharedMirrors-files-*'))[-1]
    source = blocks((backup / 'Assets/Scenes/Level1Rooms.unity').read_text())
    expected_sources = {'BlueMirror': 1300588162, 'RedMirror': 967598002}
    prefab_guids = {}
    for name, original_go in expected_sources.items():
        path = ROOT / 'Assets/Prefabs' / (name + '.prefab')
        data = blocks(path.read_text())
        ids = set(data)
        assert len(subtree(data, 1000)) == len(ids), 'Disconnected prefab object'
        colliders = [n for n, (kind, _, _) in data.items() if kind in (58, 60, 61, 64, 66, 68, 70)]
        assert colliders == [1003] and data[1003][0] == 68, 'Prefab needs exactly one edge collider'
        original_edge = next(n for n in game_components(source, original_go) if source[n][0] == 68)
        for key in ('m_Offset', 'm_EdgeRadius', 'm_IsTrigger'):
            assert field(data[1003][2], key) == field(source[original_edge][2], key)
        points = lambda body: re.search(r'^  m_Points:\n((?:  - .*\n)+)', body, re.M)[1]
        assert points(data[1003][2]) == points(source[original_edge][2]), 'Collider differs from working source'
        assert len([n for n, (kind, _, _) in data.items() if kind == 212]) == len([n for n in subtree(source, original_go) if source[n][0] == 212]), 'Missing source glow'
        prefab_guids[re.search(r'^guid: (\w+)', Path(str(path) + '.meta').read_text(), re.M)[1]] = ids
        source_sprite = 'RedMirrorGame.png' if name == 'RedMirror' else '56_3067.png'
        copied = 'Red_mirror.png' if name == 'RedMirror' else 'Blue_mirror.png'
        assert (ROOT / 'Assets/UI/Level1' / source_sprite).read_bytes() == (ROOT / 'Assets/Sprites' / copied).read_bytes()
        remove_guid = lambda s: re.sub(r'^guid: \w+', '', s, flags=re.M)
        assert remove_guid((ROOT / 'Assets/UI/Level1' / (source_sprite + '.meta')).read_text()) == remove_guid((ROOT / 'Assets/Sprites' / (copied + '.meta')).read_text())
    count = 0
    for path in (ROOT / 'Assets/Scenes').glob('*.unity'):
        text = path.read_text()
        data = blocks(text)
        assert len(data) == len(re.findall(r'^--- !u!', text, re.M)), 'Duplicate scene fileIDs'
        for n, (kind, _, body) in data.items():
            if kind == 1001:
                guid_match = re.search(r'm_SourcePrefab:.*guid: (\w+)', body)
                if not guid_match or guid_match[1] not in prefab_guids: continue
                guid = guid_match[1]
                count += 1
                for target in re.findall(r'target: \{fileID: (\d+), guid: ' + guid, body):
                    assert int(target) in prefab_guids[guid]
                # Position, rotation and parent must exactly match the saved original object.
                original = blocks((backup / path.relative_to(ROOT)).read_text())
                transform_id = next(i for i, (k, suffix, b) in data.items() if k == 4 and ref(b, 'm_PrefabInstance') == n)
                old_body = original[transform_id][2]
                assert int(re.search(r'm_TransformParent: \{fileID: (-?\d+)\}', body)[1]) == ref(old_body, 'm_Father')
                for key in ('m_LocalPosition', 'm_LocalRotation'):
                    for axis, number in re.findall(r'([xyzw]): ([-+\d.eE]+)', field(old_body, key)):
                        actual = re.search(r'propertyPath: ' + key + r'\.' + axis + r'\n      value: (.*)', body)[1]
                        assert abs(float(actual) - float(number)) < 1e-7
            if kind == 114 and 'stripped' not in data[n][1]:
                assert not re.search(r'm_Script:.*guid: ["\']?' + SURFACE, body), 'Unconverted mirror'
    assert count == 36, count
    print(f'PASSED: {count} prefab instances, source edge geometry/glows, identical copied sprites/import settings, and preserved positions/rotations/parents.')
    # Unity commonly writes empty YAML values with trailing spaces. Clean only
    # newly-added offending lines, leaving unrelated scene formatting untouched.
    warnings = subprocess.run(['git', 'diff', '--check'], cwd=ROOT, capture_output=True, text=True).stdout
    edits = {}
    for filename, line in re.findall(r'^(.*?):(\d+): trailing whitespace\.$', warnings, re.M): edits.setdefault(filename, []).append(int(line))
    for filename, lines in edits.items():
        path = ROOT / filename
        content = path.read_text().splitlines(keepends=True)
        for line in lines: content[line - 1] = content[line - 1].rstrip() + '\n'
        path.write_bytes(''.join(content).encode('utf-8'))

if __name__ == '__main__': check()
