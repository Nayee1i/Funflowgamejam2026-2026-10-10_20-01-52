import json, urllib.request, shutil
from pathlib import Path
root=Path(__file__).parent;project=root.parents[1]
ns=json.load(open(root/'nodes.json',encoding='utf-8'))
frames=['156:135','161:193','161:250','164:180','162:7','161:212','161:231']
refs=set()
def collect(n):
    for f in n.get('fills',[]):
        if f.get('imageRef'):refs.add(f['imageRef'])
    for c in n.get('children',[]):collect(c)
for id in frames:collect(ns[id])
links=json.load(open(root/'image-links.json',encoding='utf-8-sig'))['meta']['images']
(root/'Sources').mkdir(exist_ok=True)
for ref in refs:
    dest=root/'Sources'/(ref+'.png')
    existing=next((p for p in [project/'Tools/FigmaLevel3/Sources'/(ref+'.png'),project/'Tools/FigmaLevel2/Sources'/(ref+'.png'),project.parent/'FigmaOriginals'/(ref+'.png'),project.parent/'FigmaFrontendSources'/(ref+'.png')] if p.exists()),None)
    if dest.exists():continue
    if existing:shutil.copyfile(existing,dest)
    else:urllib.request.urlretrieve(links[ref],dest)
print('Native images:',len(refs))
for id in ['161:193','161:250','164:180']:
    n=ns[id];print(id,n['name'])
    for c in n.get('children',[]):
        print(' ',c['id'],c['name'],c['type'],c.get('absoluteBoundingBox'),len(c.get('children',[])))
        for child in c.get('children',[]):print('   ',child['id'],child['name'],[(f.get('imageRef')) for f in child.get('fills',[])])
print('node keys',list(ns['159:186']))
