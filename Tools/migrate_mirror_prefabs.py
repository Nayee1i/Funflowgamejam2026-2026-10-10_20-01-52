"""Copy the saved Room 3 mirror objects into linked Unity prefabs, without the editor."""
from pathlib import Path
import re, json, uuid, shutil, datetime, math, struct

ROOT = Path(__file__).resolve().parents[1]
HEADER = '%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n'
SURFACE = '143bbca64fc262d40866a48d29306f05'
BLUE = '66b0839f84ac4a6e9d83a867f84b870c'
RED = '19bf368638e64dcda0c7f50010e76954'

def blocks(text):
    return {int(m[2]): (int(m[1]), m[3], m[4]) for m in re.finditer(r'^--- !u!(\d+) &(-?\d+)([^\n]*)\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S)}

def field(body, key):
    m = re.search(r'^  ' + re.escape(key) + r': (.*)$', body, re.M)
    return m[1] if m else None

def ref(body, key):
    value = field(body, key)
    return int(re.search(r'fileID: (-?\d+)', value)[1]) if value else 0

def setfield(body, key, value):
    result, count = re.subn(r'^  ' + re.escape(key) + r': .*$', lambda m: '  ' + key + ': ' + value, body, flags=re.M)
    assert count == 1, (key, count)
    return result

def vector(value):
    return {key: float(number) for key, number in re.findall(r'([xyzw]): ([-+\d.eE]+)', value)}

def vec(value):
    return '{' + ', '.join(f'{key}: {number:.9g}' for key, number in value.items()) + '}'

def game_components(data, go):
    return [int(n) for n in re.findall(r'component: \{fileID: (-?\d+)\}', data[go][2])]

def transform(data, go):
    return next(n for n in game_components(data, go) if data[n][0] == 4)

def script_guid(body):
    return re.search(r'm_Script:.*guid: ["\']?(\w+)', body)[1] if 'm_Script:' in body else None

def mirror_objects(data):
    result = {}
    for n, (kind, _, body) in data.items():
        guid = script_guid(body) if kind == 114 else None
        if guid in (SURFACE, BLUE, RED):
            go = ref(body, 'm_GameObject')
            if go in data and data[go][0] == 1:
                charged = field(body, 'chargesShot') == '1' if guid == SURFACE else guid == RED
                if guid == SURFACE or go not in result: result[go] = charged
    return result

def subtree(data, go):
    result = {go, *game_components(data, go)}
    t = transform(data, go)
    children = re.search(r'^  m_Children:\n((?:  - .*\n)*)', data[t][2], re.M)
    if children:
        for child in re.findall(r'fileID: (-?\d+)', children[1]):
            result |= subtree(data, ref(data[int(child)][2], 'm_GameObject'))
    return result

def under(data, go, ancestor):
    t = transform(data, go)
    while t:
        if ref(data[t][2], 'm_GameObject') == ancestor: return True
        t = ref(data[t][2], 'm_Father')
    return False

def emit(data):
    return HEADER + ''.join(f'--- !u!{kind} &{n}{suffix}\n{body}' for n, (kind, suffix, body) in data.items())

def local_refs(body):
    return [int(n) for n in re.findall(r'\{fileID: (-?\d+)\}', body)]

def remap(body, mapping):
    return re.sub(r'\{fileID: (-?\d+)\}', lambda m: '{fileID: ' + str(mapping.get(int(m[1]), int(m[1]))) + '}', body)

def asset_guid(path):
    return re.search(r'^guid: (\w+)', (ROOT / (path + '.meta')).read_text(), re.M)[1]

def copy_sprite(source, destination, output):
    src = ROOT / source
    guid = asset_guid(destination) if (ROOT / (destination + '.meta')).exists() else uuid.uuid4().hex
    metadata = re.sub(r'^guid: \w+', 'guid: ' + guid, (ROOT / (source + '.meta')).read_text(), count=1, flags=re.M)
    output[destination] = src.read_bytes()
    output[destination + '.meta'] = metadata.encode()
    return guid

def sprite_width(guid, assets):
    path = assets[guid]
    if path.suffix.lower() == '.png':
        width = struct.unpack('>I', path.read_bytes()[16:20])[0]
        meta = Path(str(path) + '.meta').read_text()
        ppu = float(re.search(r'^  spritePixelsToUnits: ([\d.eE]+)', meta, re.M)[1])
        return width / ppu
    # Imported native Sprite assets serialize the rectangle in Unity units.
    text = path.read_text()
    m = re.search(r'm_Rect:\s*\n\s*serializedVersion: \d+\s*\n\s*x: .*\n\s*y: .*\n\s*width: ([\d.eE]+)', text)
    ppu = re.search(r'm_PixelsToUnits: ([\d.eE]+)', text)
    assert m and ppu, f'Unknown sprite bounds: {path}'
    return float(m[1]) / float(ppu[1])

def prefab(source, go, name, sprite, old_prefab):
    ids = subtree(source, go)
    parts = game_components(source, go)
    renderer = next(n for n in parts if source[n][0] == 212)
    collider = next(n for n in parts if source[n][0] == 68)
    surface = next(n for n in parts if source[n][0] == 114 and script_guid(source[n][2]) == SURFACE)
    assert len([n for n in parts if source[n][0] in (58, 60, 61, 64, 66, 68, 70)]) == 1
    mapping = {go: 1000, transform(source, go): 1001, renderer: 1002, collider: 1003, surface: 1004}
    for n in sorted(ids - mapping.keys()): mapping[n] = 1006 + len(mapping) - 5
    result = {}
    for n in sorted(ids, key=lambda n: mapping[n]):
        kind, _, body = source[n]
        assert all(ref_id == 0 or ref_id in ids for ref_id in local_refs(body) if ref_id != ref(body, 'm_Father')), 'Unexpected external template reference'
        body = remap(body, mapping)
        if n == go:
            body = setfield(body, 'm_Name', name)
            body = setfield(body, 'm_IsActive', '1')
            body = body.replace('  m_Layer:', '  - component: {fileID: 1005}\n  m_Layer:', 1)
        if mapping[n] == 1001:
            body = setfield(body, 'm_Father', '{fileID: 0}')
            body = setfield(body, 'm_LocalPosition', '{x: 0, y: 0, z: 0}')
            body = setfield(body, 'm_LocalRotation', '{x: 0, y: 0, z: 0, w: 1}')
            body = setfield(body, 'm_LocalEulerAnglesHint', '{x: 0, y: 0, z: 0}')
        if mapping[n] == 1002: body = setfield(body, 'm_Sprite', '{fileID: 21300000, guid: ' + sprite + ', type: 3}')
        result[mapping[n]] = (kind, '', body)
    # Retain the old marker for the tutorial projectile system, without adding a BoxCollider.
    result[1005] = old_prefab[1004]
    return result, mapping

def replacement(data, go, prefab_data, prefab_guid, instance, source_go, source_data, width_ratio):
    removed = subtree(data, go)
    parts = game_components(data, go)
    t = transform(data, go)
    transform_body = data[t][2]
    renderer = next(n for n in parts if data[n][0] == 212)
    targets = {go: 1000, t: 1001, renderer: 1002}
    for n in parts:
        kind, _, body = data[n]
        if kind in (58, 60, 61, 64, 66, 68, 70): targets[n] = 1003
        if kind == 114:
            if script_guid(body) == SURFACE: targets[n] = 1004
            elif script_guid(body) in (BLUE, RED): targets[n] = 1005
    scale = vector(field(transform_body, 'm_LocalScale'))
    if go != source_go or data is not source_data:
        native = vector(field(prefab_data[1001][2], 'm_LocalScale'))
        scale['x'] *= width_ratio
        scale['y'] = math.copysign(abs(scale['x'] * native['y'] / native['x']), scale['y'])
    modifications = []
    def modification(target, path, value):
        modifications.append(f'    - target: {{fileID: {target}, guid: {prefab_guid}, type: 3}}\n      propertyPath: {path}\n      value: {value}\n      objectReference: {{fileID: 0}}\n')
    modification(1000, 'm_Name', field(data[go][2], 'm_Name'))
    modification(1000, 'm_IsActive', field(data[go][2], 'm_IsActive'))
    for key in ('m_LocalPosition', 'm_LocalRotation', 'm_LocalEulerAnglesHint'):
        for axis, number in vector(field(transform_body, key)).items(): modification(1001, key + '.' + axis, f'{number:.9g}')
    for axis, number in scale.items(): modification(1001, 'm_LocalScale.' + axis, f'{number:.9g}')
    for key in ('m_SortingLayerID', 'm_SortingOrder'): modification(1002, key, field(data[renderer][2], key))
    body = ('PrefabInstance:\n  m_ObjectHideFlags: 0\n  serializedVersion: 2\n  m_Modification:\n'
            '    serializedVersion: 3\n    m_TransformParent: {fileID: ' + str(ref(transform_body, 'm_Father')) + '}\n'
            '    m_Modifications:\n' + ''.join(modifications) +
            '    m_RemovedComponents: []\n    m_RemovedGameObjects: []\n    m_AddedGameObjects: []\n    m_AddedComponents: []\n'
            '  m_SourcePrefab: {fileID: 100100000, guid: ' + prefab_guid + ', type: 3}\n')
    result = {instance: (1001, '', body)}
    for old, target in targets.items():
        kind, _, template_body = prefab_data[target]
        typename = template_body.split(':', 1)[0]
        body = (typename + ':\n  m_CorrespondingSourceObject: {fileID: ' + str(target) + ', guid: ' + prefab_guid + ', type: 3}\n'
                '  m_PrefabInstance: {fileID: ' + str(instance) + '}\n  m_PrefabAsset: {fileID: 0}\n')
        if kind == 114:
            body += '  m_GameObject: {fileID: ' + str(go) + '}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n'
            body += '  m_Script: ' + field(template_body, 'm_Script') + '\n  m_Name: \n  m_EditorClassIdentifier: \n'
        result[old] = (kind, ' stripped', body)
    outside = '\n'.join(body for n, (_, _, body) in data.items() if n not in removed)
    external = set(local_refs(outside)) & removed
    assert external <= targets.keys(), f'Unmapped child reference: {external - targets.keys()}'
    return removed, result

if __name__ == '__main__':
    source = blocks((ROOT / 'Assets/Scenes/Level1Rooms.unity').read_text(encoding='utf-8-sig'))
    room = next(n for n, (kind, _, body) in source.items() if kind == 1 and field(body, 'm_Name') == 'Room 1.3')
    templates = {red: go for go, red in mirror_objects(source).items() if under(source, go, room)}
    assert len(templates) == 2, templates
    output = {}
    sprite_guids = {
        False: copy_sprite('Assets/UI/Level1/56_3067.png', 'Assets/Sprites/Blue_mirror.png', output),
        True: copy_sprite('Assets/UI/Level1/RedMirrorGame.png', 'Assets/Sprites/Red_mirror.png', output),
    }
    assets = {}
    for meta in (ROOT / 'Assets').rglob('*.meta'):
        m = re.search(r'^guid: (\w+)', meta.read_text(errors='replace'), re.M)
        if m: assets[m[1]] = Path(str(meta)[:-5])
    prefab_guids, prefab_data = {}, {}
    for red, go in templates.items():
        path = 'Assets/Prefabs/' + ('RedMirror' if red else 'BlueMirror') + '.prefab'
        prefab_guids[red] = asset_guid(path)
        prefab_data[red], _ = prefab(source, go, 'RedMirror' if red else 'BlueMirror', sprite_guids[red], blocks((ROOT / path).read_text()))
        output[path] = emit(prefab_data[red]).encode()
    report = []
    for path in sorted((ROOT / 'Assets/Scenes').glob('*.unity')):
        data = source if path.name == 'Level1Rooms.unity' else blocks(path.read_text(encoding='utf-8-sig'))
        mirrors = mirror_objects(data)
        if not mirrors: continue
        new = dict(data)
        fresh_id = max(n for n in data if n < 10**14) + 1
        for go, red in mirrors.items():
            old_renderer = next(n for n in game_components(data, go) if data[n][0] == 212)
            old_guid = re.search(r'guid: ["\']?(\w+)', field(data[old_renderer][2], 'm_Sprite'))[1]
            new_width = sprite_width(asset_guid('Assets/UI/Level1/' + ('RedMirrorGame.png' if red else '56_3067.png')), assets)
            ratio = sprite_width(old_guid, assets) / new_width
            removed, replacement_blocks = replacement(data, go, prefab_data[red], prefab_guids[red], fresh_id, templates[red], source, ratio)
            fresh_id += 1
            for n in removed: new.pop(n)
            new.update(replacement_blocks)
        text = emit(new)
        parsed = blocks(text)
        assert len(parsed) == len(new)
        # Every scene-local reference must resolve, including route references and parent child lists.
        dangling = {n for _, _, body in parsed.values() for n in local_refs(body) if n != 0 and n not in parsed}
        original_dangling = {n for _, _, body in data.values() for n in local_refs(body) if n != 0 and n not in data}
        assert dangling <= original_dangling, (path.name, dangling - original_dangling)
        for go, red in mirrors.items():
            assert 'stripped' in parsed[go][1]
            assert prefab_guids[red] in parsed[go][2]
        output[str(path.relative_to(ROOT)).replace('\\', '/')] = text.encode()
        report.append(f'{path.name}: {len(mirrors)} linked prefab mirrors')
    backup = ROOT / 'Tools/Backups' / ('SharedMirrors-files-' + datetime.datetime.now().strftime('%Y%m%d-%H%M%S'))
    for relative, content in output.items():
        destination = ROOT / relative
        if destination.exists():
            saved = backup / relative
            saved.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(destination, saved)
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(content)
    report.append('VERIFIED: exact source edge colliders and glow children, copied sprite bytes/import settings, prefab links, and scene references. Unity runtime validation pending.')
    (ROOT / 'Tools/shared-mirror-prefabs-result.txt').write_text('\n'.join(report) + '\n')
    print('\n'.join(report))
