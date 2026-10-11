"""Check baked scene references, hierarchy, enemy counts, and tank-sized routes."""
from pathlib import Path
import collections
import math
import re

ROOT = Path(__file__).resolve().parents[1]
GUIDS = {re.search(r'^guid: (\w+)', p.read_text(encoding='utf-8-sig'), re.M)[1]
         for p in (ROOT / 'Assets').rglob('*.meta') if re.search(r'^guid: (\w+)', p.read_text(encoding='utf-8-sig'), re.M)}


def vector(text, field):
    match = re.search(r'  ' + field + r': \{x: ([^,]+), y: ([^,]+)', text)
    return tuple(map(float, match.groups()))


def validate(index, expected_rooms, expected_enemies):
    text = (ROOT / f'Assets/Scenes/level{index}.unity').read_text(encoding='utf-8')
    blocks = {}
    for part in re.split(r'(?=^--- !u!)', text, flags=re.M)[1:]:
        identity = int(re.match(r'--- !u!\d+ &(\d+)', part)[1])
        assert identity not in blocks
        blocks[identity] = part
    for identity in map(int, re.findall(r'\{fileID: (\d+)\}', text)):
        assert identity == 0 or identity in blocks
    for guid in re.findall(r'guid: (\w+)', text):
        # UniversalAdditionalCameraData is supplied by the installed URP package.
        assert guid in GUIDS or guid == 'a79441f348de89743a2939f4d699eac1' or guid.startswith('0000000000000000'), guid
    transforms = {i: b for i, b in blocks.items() if b.startswith('--- !u!4 ')}
    def parent(b):
        if ' stripped\n' in b:
            instance = int(re.search(r'm_PrefabInstance: \{fileID: (\d+)\}', b)[1])
            return int(re.search(r'm_TransformParent: \{fileID: (\d+)\}', blocks[instance])[1])
        return int(re.search(r'm_Father: \{fileID: (\d+)\}', b)[1])
    parents = {i: parent(b) for i, b in transforms.items()}
    for identity, block in transforms.items():
        if ' stripped\n' in block: continue
        children = set(map(int, re.findall(r'fileID: (\d+)', block.split('  m_Children:')[1].split('  m_Father:')[0])))
        assert children == {i for i, p in parents.items() if p == identity}
    rooms = [i for i, b in transforms.items() if ' stripped\n' not in b and re.search(r'm_Name: "Room \d+"', blocks[int(re.search(r'm_GameObject: \{fileID: (\d+)\}', b)[1])])]
    assert len(rooms) == expected_rooms
    assert text.count('m_Script: {fileID: 11500000, guid: 7a802bcc963f457780e06f2426203481') == expected_enemies
    assert text.count('    checkpoint:') == expected_rooms
    assert text.count('    gate:') == expected_rooms
    prefab_guid = re.search(r'^guid: (\w+)', (ROOT / 'Assets/Prefabs/EnemyTank.prefab.meta').read_text(), re.M)[1]
    instances = [b for b in blocks.values() if b.startswith('--- !u!1001 ')]
    assert len(instances) == expected_enemies
    for instance in instances:
        assert f'm_SourcePrefab: {{fileID: 100100000, guid: {prefab_guid}' in instance
        assert 'propertyPath: shotInterval' not in instance
        assert 'propertyPath: firstShotDelay' not in instance
        assert 'propertyPath: player' in instance and 'propertyPath: level' in instance
    prefab = (ROOT / 'Assets/Prefabs/EnemyTank.prefab').read_text()
    assert 'fcf27d96eb0d402a940c50eecc6c0479' in prefab
    assert 'shotInterval: 2.1' in prefab and 'firstShotDelay: 0.8' in prefab
    assert 'maxHealth: 5' in prefab
    for room in rooms:
        solids = []
        for identity, parent in parents.items():
            if parent != room:
                continue
            block = transforms[identity]
            if ' stripped\n' in block: continue
            go = blocks[int(re.search(r'm_GameObject: \{fileID: (\d+)\}', block)[1])]
            name = re.search(r'  m_Name: (.*)', go)[1].strip('"')
            if name not in ('Wall', 'Column', 'Entry Wall', 'Final Boundary'):
                continue
            pos, size = vector(block, 'm_LocalPosition'), vector(block, 'm_LocalScale')
            solids.append((pos, size))
        # Model both body orientations and only turn where the whole swept body fits.
        def free(x, y, hx, hy):
            return all(abs(x-p[0]) > s[0]/2+hx or abs(y-p[1]) > s[1]/2+hy for p, s in solids)
        step = .1
        points = {(x, y, o) for x in range(-82, 83) for y in range(-42, 43) for o in (0, 1)
                  if free(x*step, y*step, * ((.55, .475) if o == 0 else (.475, .55)))}
        start = (-70, 0, 0)
        assert start in points
        seen = {start}
        queue = collections.deque([start])
        while queue:
            x, y, o = queue.popleft()
            neighbors = [(x-1, y, o), (x+1, y, o)] if o == 0 else [(x, y-1, o), (x, y+1, o)]
            if free(x*step, y*step, .73, .73):
                neighbors.append((x, y, 1-o))
            for other in neighbors:
                if other in points and other not in seen:
                    seen.add(other)
                    queue.append(other)
        assert (80, 0, 0) in seen, f'level{index}, room {room}: exit unreachable'
    print(f'level{index}: asset/local references, hierarchy, {expected_rooms} rooms, {expected_enemies} enemies, all exit routes PASS')


if __name__ == '__main__':
    for args in ((1, 3, 7), (2, 4, 12), (3, 5, 18)):
        validate(*args)
