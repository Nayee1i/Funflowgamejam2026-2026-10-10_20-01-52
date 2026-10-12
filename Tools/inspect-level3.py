import json
ns=json.load(open('Tools/FigmaLevel3/Geometry.json',encoding='utf-8'))
def walk(n,depth=0):
 print(' '*depth+n['id']+' '+n['name']+' '+str(n.get('absoluteBoundingBox',{}))+' '+str([f.get('imageRef') for f in n.get('fills',[]) if f.get('imageRef')]))
 for c in n.get('children',[]):walk(c,depth+1)
for id in ['73:3784','83:3876','83:4016','98:4105','98:4117']:walk(ns[id])
