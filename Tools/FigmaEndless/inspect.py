import json
from pathlib import Path
root=Path(__file__).parent
d=json.load(open(root/'current.json',encoding='utf-8-sig'))
nodes={}
def walk(n):
    nodes[n['id']]=n
    for c in n.get('children',[]):walk(c)
walk(d['document'])
json.dump(nodes,open(root/'nodes.json','w',encoding='utf-8'),ensure_ascii=False)
for id in ['156:135','161:231','161:212','162:7']:
    n=nodes[id];print(id,n['name'],n.get('absoluteBoundingBox'))
    for c in n.get('children',[]):print(' ',c['id'],c['name'],c['type'],c.get('absoluteBoundingBox'),len(c.get('children',[])))
print('Possible results:')
for n in nodes.values():
    if n['type']=='FRAME' and any(t in n['name'].lower() for t in ['побед','пораж','конеч','смер','результ','4.']):print(n['id'],n['name'])
