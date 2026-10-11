"""Bake the supplied SVG room sketches into editable Unity scene objects.

Usage: python Tools/generate_sketch_levels.py <directory containing level-N(1).svg>
Uses the existing Tutorial scene's serialized object templates and gameplay scripts.
"""
from pathlib import Path
import collections
import json
import re
import sys
import uuid
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
source = (ROOT / 'Assets/Scenes/Tutorial.unity').read_text(encoding='utf-8-sig')
parts = re.split(r'(?=^--- !u!)', source, flags=re.M)
templates = {}
types = {}
for part in parts[1:]:
    kind, identity = map(int, re.match(r'--- !u!(\d+) &(\d+)', part).groups())
    templates[identity] = part
    types[identity] = kind


def field(text, key, value):
    return re.sub(r'^  ' + re.escape(key) + r':.*$', '  ' + key + ': ' + value, text, flags=re.M)


def graph(go):
    result = [go]
    components = list(map(int, re.findall(r'component: \{fileID: (\d+)\}', templates[go])))
    result += components
    transform = next(c for c in components if types[c] == 4)
    children = templates[transform].split('  m_Children:')[1].split('  m_Father:')[0]
    for child in map(int, re.findall(r'fileID: (\d+)', children)):
        child_go = int(re.search(r'm_GameObject: \{fileID: (\d+)\}', templates[child])[1])
        result += graph(child_go)
    return result


class Scene:
    def __init__(self):
        self.blocks = {i: templates[i] for i in (1, 2, 3, 4)}
        self.next_id = 1000000
        for go in (1000, 1020, 519420028):
            for i in graph(go):
                self.blocks[i] = templates[i]
        self.blocks[1001] = field(self.blocks[1001], 'm_Children', '[]')

    def clone(self, go, name, position, size=(1, 1), parent=0, empty=False):
        ids = graph(go)
        if empty:
            ids = ids[:2]
        mapping = {}
        for i in ids:
            self.next_id += 1
            mapping[i] = self.next_id
        for i in ids:
            text = re.sub(r'(?<=&)\d+', str(mapping[i]), templates[i], count=1)
            text = re.sub(r'\{fileID: (\d+)\}', lambda m: '{fileID: ' + str(mapping.get(int(m[1]), int(m[1]))) + '}', text)
            self.blocks[mapping[i]] = text
        new_go = mapping[go]
        transform = next(mapping[i] for i in ids if types[i] == 4)
        self.blocks[new_go] = field(self.blocks[new_go], 'm_Name', json.dumps(name, ensure_ascii=False))
        self.blocks[transform] = field(self.blocks[transform], 'm_Father', f'{{fileID: {parent}}}')
        self.blocks[transform] = field(self.blocks[transform], 'm_LocalPosition', f'{{x: {position[0]:.6g}, y: {position[1]:.6g}, z: 0}}')
        self.blocks[transform] = field(self.blocks[transform], 'm_LocalScale', f'{{x: {size[0]:.6g}, y: {size[1]:.6g}, z: 1}}')
        if empty:
            self.blocks[transform] = re.sub(r'  m_Children:.*?  m_Father:', '  m_Children: []\n  m_Father:', self.blocks[transform], flags=re.S)
        return new_go, transform, mapping

    def shape(self, template, name, position, size, parent, color=None):
        go, transform, mapping = self.clone(template, name, position, size, parent)
        if color:
            renderer = next(mapping[i] for i in mapping if types[i] == 212)
            rgb = [int(color[j:j+2], 16) / 255 for j in (1, 3, 5)]
            self.blocks[renderer] = field(self.blocks[renderer], 'm_Color', f'{{r: {rgb[0]:.6g}, g: {rgb[1]:.6g}, b: {rgb[2]:.6g}, a: 1}}')
        return go, transform

    def enemy(self, name, position, parent, room_index, shielded, offset):
        guid = re.search(r'^guid: (\w+)', (ROOT / 'Assets/Prefabs/EnemyTank.prefab.meta').read_text(), re.M)[1]
        self.next_id += 4
        instance, go, transform, target = range(self.next_id - 3, self.next_id + 1)
        changes = []
        def change(source, prop, value='', reference=0):
            changes.append(f'    - target: {{fileID: {source}, guid: {guid}, type: 3}}\n      propertyPath: {prop}\n      value: {value}\n      objectReference: {{fileID: {reference}}}\n')
        change(2980, 'm_Name', name)
        change(2981, 'm_LocalPosition.x', position[0])
        change(2981, 'm_LocalPosition.y', position[1])
        change(3081, 'm_LocalRotation.z', '.7071068')
        change(3081, 'm_LocalRotation.w', '.7071068')
        change(519420152, 'roomIndex', room_index)
        if offset: change(519420152, 'reactionOffset', offset)
        change(519420152, 'player', reference=1021)
        change(519420152, 'level', reference=1002)
        if shielded:
            change(2983, 'shielded', 1)
            change(3000, 'm_IsActive', 1)
        self.blocks[instance] = f'--- !u!1001 &{instance}\nPrefabInstance:\n  m_ObjectHideFlags: 0\n  serializedVersion: 2\n  m_Modification:\n    serializedVersion: 3\n    m_TransformParent: {{fileID: {parent}}}\n    m_Modifications:\n'+''.join(changes)+'    m_RemovedComponents: []\n    m_RemovedGameObjects: []\n    m_AddedGameObjects: []\n    m_AddedComponents: []\n'+f'  m_SourcePrefab: {{fileID: 100100000, guid: {guid}, type: 3}}\n'
        for identity, source, kind, label in ((go,2980,1,'GameObject'),(transform,2981,4,'Transform'),(target,2983,114,'MonoBehaviour')):
            self.blocks[identity] = f'--- !u!{kind} &{identity} stripped\n{label}:\n  m_CorrespondingSourceObject: {{fileID: {source}, guid: {guid}, type: 3}}\n  m_PrefabInstance: {{fileID: {instance}}}\n  m_PrefabAsset: {{fileID: 0}}\n'
            if kind == 114:
                self.blocks[identity] += f'  m_GameObject: {{fileID: {go}}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n  m_Script: {{fileID: 11500000, guid: 7a802bcc963f457780e06f2426203481, type: 3}}\n  m_Name: \n  m_EditorClassIdentifier: \n'
        return target

    def save(self, path):
        # Derive both hierarchy directions from each transform's actual parent.
        parents = {}
        for identity, text in self.blocks.items():
            if text.startswith('--- !u!4 '):
                if ' stripped\n' in text:
                    instance = int(re.search(r'm_PrefabInstance: \{fileID: (\d+)\}', text)[1])
                    parents[identity] = int(re.search(r'm_TransformParent: \{fileID: (\d+)\}', self.blocks[instance])[1])
                else:
                    parents[identity] = int(re.search(r'm_Father: \{fileID: (\d+)\}', text)[1])
        for identity in parents:
            if ' stripped\n' in self.blocks[identity]: continue
            children = [i for i, p in parents.items() if p == identity]
            value = '\n' + ''.join(f'  - {{fileID: {i}}}\n' for i in children) if children else ' []\n'
            self.blocks[identity] = re.sub(r'  m_Children:.*?  m_Father:', '  m_Children:' + value + '  m_Father:', self.blocks[identity], flags=re.S)
        roots = ''.join(f'  - {{fileID: {i}}}\n' for i, p in parents.items() if p == 0)
        text = parts[0] + ''.join(self.blocks.values()) + '--- !u!1660057539 &9223372036854775807\nSceneRoots:\n  m_ObjectHideFlags: 0\n  m_Roots:\n' + roots
        # Every local reference must resolve; asset references retain their GUIDs.
        for ref in re.findall(r'\{fileID: (\d+)\}', text):
            assert int(ref) == 0 or int(ref) in self.blocks, f'Dangling reference {ref}'
        path.write_text(text, encoding='utf-8')
        meta = path.with_suffix(path.suffix + '.meta')
        if not meta.exists():
            meta.write_text(f'fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\nDefaultImporter:\n  externalObjects: {{}}\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n', encoding='utf-8')


def number(node, attr):
    return float(node.get(attr, '0'))


def bake(index, directory):
    nodes = list(ET.parse(directory / f'level-{index}(1).svg').getroot())
    floors = [n for n in nodes if n.tag.endswith('rect') and n.get('fill') == '#0B0F16']
    scene = Scene()
    rooms_yaml = []
    totals = collections.Counter()
    for room_index, floor in enumerate(floors):
        ox, oy = number(floor, 'x'), number(floor, 'y')
        bottom = oy + 220
        room_x = room_index * 20
        _, room, _ = scene.clone(1180, f'Room {room_index + 1}', (room_x, 0), empty=True)
        scene.shape(1200, 'Floor', (0, 0), (18, 10), room, '#0B0F16')
        in_room = lambda n, x, y: ox <= number(n, x) <= ox + 396 and oy <= number(n, y) <= bottom
        for n in nodes:
            if not n.tag.endswith('rect') or n.get('fill') not in ('#3A4658', '#4F6682', '#1F8BFF', '#FF2E42') or not in_room(n, 'x', 'y'):
                continue
            w, h = number(n, 'width') / 22, number(n, 'height') / 22
            pos = ((number(n, 'x') - ox) / 22 + w / 2 - 9, (bottom - number(n, 'y')) / 22 - h / 2 - 5)
            fill = n.get('fill')
            template, name = {'#3A4658': (1220, 'Wall'), '#4F6682': (1220, 'Column'), '#1F8BFF': (3160, 'Blue Mirror'), '#FF2E42': (3200, 'Red Mirror')}[fill]
            scene.shape(template, name, pos, (w, h), room, fill)
        # Close the first entry and the final exit. Intermediate doors join short corridors.
        if room_index == 0:
            scene.shape(1220, 'Entry Wall', (-8.75, 0), (.5, 2), room, '#3A4658')
        gate, _ = scene.shape(3220, 'Exit Gate', (8.75, 0), (.5, 2), room)
        if room_index == len(floors) - 1:
            scene.shape(1220, 'Final Boundary', (9.25, 0), (.5, 2), room, '#3A4658')
        else:
            scene.shape(1200, 'Corridor Floor', (10, 0), (2, 2), room, '#0B0F16')
            for y in (-1.25, 1.25):
                scene.shape(1220, 'Corridor Wall', (10, y), (2, .5), room, '#3A4658')
        player = next(n for n in nodes if n.tag.endswith('circle') and n.get('fill') == '#3EE09E' and n.get('stroke') == '#B8FFE0' and in_room(n, 'cx', 'cy'))
        checkpoint_pos = ((number(player, 'cx') - ox) / 22 - 9, (bottom - number(player, 'cy')) / 22 - 5)
        _, checkpoint, _ = scene.clone(519420276, f'Checkpoint Room {room_index + 1}', checkpoint_pos, parent=room)
        if room_index == 0:
            scene.blocks[1021] = field(scene.blocks[1021], 'm_LocalPosition', f'{{x: {checkpoint_pos[0]}, y: {checkpoint_pos[1]}, z: 0}}')
            scene.blocks[519420032] = field(scene.blocks[519420032], 'm_LocalPosition', '{x: 0, y: 0, z: -10}')
        targets = []
        enemies = [n for n in nodes if n.tag.endswith('circle') and n.get('fill') == '#FF5C4C' and in_room(n, 'cx', 'cy')]
        for enemy_index, enemy in enumerate(enemies):
            rings = [n for n in nodes if n.tag.endswith('circle') and n.get('fill') == 'none' and abs(number(n, 'cx') - number(enemy, 'cx')) < .001 and abs(number(n, 'cy') - number(enemy, 'cy')) < .001 and number(n, 'r') > number(enemy, 'r')]
            shielded = any(n.get('stroke') == '#FFD23F' for n in rings)
            reflected = any(n.get('stroke') == '#fff' for n in rings)
            kind = 'Shielded' if shielded else 'Ricochet' if reflected else 'Shooter'
            totals[kind] += 1
            pos = ((number(enemy, 'cx') - ox) / 22 - 9, (bottom - number(enemy, 'cy')) / 22 - 5)
            target = scene.enemy(f'{kind} Enemy {enemy_index + 1}', pos, room, room_index, shielded, enemy_index * .28)
            targets.append(target)
        title = next(n.text for n in nodes if n.tag.endswith('text') and n.get('font-size') == '13' and abs(number(n, 'x') - ox) < .01 and number(n, 'y') == bottom + 34)
        rooms_yaml.append(f'  - instruction: {json.dumps(title, ensure_ascii=False)}\n    center: {{x: {room_x}, y: 0}}\n    size: {{x: 18, y: 10}}\n    gate: {{fileID: {gate}}}\n    targets:\n' + ''.join(f'    - {{fileID: {t}}}\n' for t in targets) + f'    checkpoint: {{fileID: {checkpoint}}}\n')
    scene.blocks[1000] = field(scene.blocks[1000], 'm_Name', f'level{index}')
    scene.blocks[1002] = scene.blocks[1002].split('  rooms:')[0] + '  rooms:\n' + ''.join(rooms_yaml) + '  levelSelectionScene: LevelSelection\n  completionMessage: "Уровень пройден!"\n'
    path = ROOT / f'Assets/Scenes/level{index}.unity'
    scene.save(path)
    print(f'level{index}: {len(floors)} rooms, {sum(totals.values())} enemies, {dict(totals)}; local references verified')
    return path


if __name__ == '__main__':
    directory = Path(sys.argv[1])
    for index in (1, 2, 3):
        bake(index, directory)
