import json, math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
project=Path(__file__).resolve().parents[2]
root=project/'Tools/FigmaEndless'
ns={}
def collect(n):
    ns[n['id']]=n
    for c in n.get('children',[]):collect(c)
response=json.load(open(root/'geometry-response.json',encoding='utf-8-sig'))
for v in response['nodes'].values():collect(v['document'])
json.dump(ns,open(root/'geometry.json','w',encoding='utf-8'),ensure_ascii=False)
# Same original bitmap crops and geometry conversion used for previous levels.
base=(project/'Tools/extract-level3.py').read_text(encoding='utf-8')
prefix=base[base.index('def matrix'):base.index('\nroster=')]
sources=root/'Sources';art=project/'Assets/Resources/Endless';art.mkdir(parents=True,exist_ok=True)
parents={c['id']:n['id'] for n in ns.values() for c in n.get('children',[])}
factor=1586/1270/100
exec(prefix)
def flatten(n):
    yield n
    for c in n.get('children',[]):yield from flatten(c)
def compose(frame,destination):
    out=Image.new('RGBA',(1920,1080),(25,24,23,255))
    for n in flatten(ns[frame]):
        if n['type']=='GROUP' or n['type']=='FRAME':continue
        if not any(f.get('imageRef') for f in n.get('fills',[])):
            if n['name']=='Rectangle 14':
                layer=Image.new('RGBA',out.size,(0,0,0,round(255*n.get('opacity',.5))));out=Image.alpha_composite(out,layer)
            continue
        im=bitmap(n['id']);m=matrix(n['id'],frame);w,h=n['size']['x'],n['size']['y']
        m=m@np.diag([w/im.width,h/im.height,1]);inv=np.linalg.inv(m)
        layer=im.transform(out.size,Image.Transform.AFFINE,tuple(inv[:2].ravel()),Image.Resampling.BICUBIC)
        if n.get('opacity',1)<1:layer.putalpha(layer.getchannel('A').point(lambda p:round(p*n['opacity'])))
        out=Image.alpha_composite(out,layer)
    out.save(root/destination)
for frame in ['156:135','161:193','161:250','164:180','162:7','161:212','161:231']:
    compose(frame,frame.replace(':','_')+'-native.png')
for frame in ['156:135','161:193','161:250']:
    print('\nFRAME',frame)
    for n in flatten(ns[frame]):
        if any(f.get('imageRef') for f in n.get('fills',[])):
            print(n['id'],n['name'],[f.get('imageRef','')[:8] for f in n.get('fills',[])],n.get('size'),n.get('opacity',1))
sheet=Image.new('RGB',(1440,900),(30,30,30))
for i,id in enumerate(['156:135','161:193','161:250','164:180','162:7','161:212']):
    im=Image.open(root/(id.replace(':','_')+'-native.png'));im.thumbnail((720,405));sheet.paste(im,(i%2*720,i//2*300))
sheet.save(root/'overview.jpg')

rooms=[]
rosters=[
 ('156:135','157:181','157:184',['159:186','159:187','159:188','159:189'],['162:32'],[('162:55',False),('162:56',False)],[('162:35','162:36',None,False),('162:39','162:40',None,False),('162:43','162:44',None,False)]),
 ('161:193','161:204','161:206',['161:208','161:209'],['162:74','162:75'],[('162:49',True),('162:88',True),('162:61',True),('162:93',True)],[('162:64','162:65','162:66',False),('162:68','162:69',None,True),('162:71','162:72',None,True),('162:78','162:79',None,False),('162:82','162:83',None,False)]),
 ('161:250','161:261','161:263',['162:109','162:110','162:111','162:128'],['162:127'],[('162:119',True),('162:124',True),('162:125',False),('162:126',False)],[('162:96','162:97','162:98',False),('162:101','162:102','162:103',False),('162:106','162:107','162:108',False),('162:113','162:114',None,True)])]
for frame,bg,hero,blocks,obstacles,mirrors,enemies in rosters:
    rooms.append(dict(frame=frame,background=placement(bg,frame),hero=placement(hero,frame),blocks=[placement(id,frame) for id in blocks],obstacles=[placement(id,frame) for id in obstacles],mirrors=[dict(art=placement(id,frame),red=red) for id,red in mirrors],enemies=[dict(body=placement(b,frame),head=placement(h,frame),shield=placement(sh,frame) if sh else None,promoted=red) for b,h,sh,red in enemies]))
json.dump(dict(rooms=rooms),open(art/'Layout.json','w',encoding='utf-8'),indent=2)
parts=[]
frame='164:180'
for id,action in [('164:176',None),('164:178','restart'),('164:179','menu')]:
    n=ns[id];b=n['absoluteBoundingBox'];f=ns[frame]['absoluteBoundingBox'];name,im=asset(id)
    parts.append(dict(asset='Endless/'+name,x=b['x']-f['x'],y=b['y']-f['y'],w=b['width'],h=b['height'],action=action))
json.dump(dict(panels=[dict(name='ArenaResult',parts=parts)]),open(art/'Panels.json','w',encoding='utf-8'),indent=2)
waves=[]
for frame,id in [('162:7','162:27'),('161:212','162:29'),('161:231','162:31')]:
    name,im=asset(id);n=ns[id];b=n['absoluteBoundingBox'];f=ns[frame]['absoluteBoundingBox']
    waves.append(dict(asset='Endless/'+name,x=b['x']-f['x'],y=b['y']-f['y'],w=b['width'],h=b['height']))
json.dump(dict(waves=waves),open(art/'Waves.json','w',encoding='utf-8'),indent=2)
(art/'ArtworkSources.md').write_text('Original Figma bitmap crops and geometry from Ph3GGyRnJoW5mYfM8ddWyI: 4.1 (156:135), 4.2 (161:193), 4.3 (161:250), results (164:180), wave banners 162:7 / 161:212 / 161:231.\nSame pixel-to-world scale as level2/level3. No credentials stored.\n',encoding='utf-8')
print('Exported three authored waves, 12 enemies, 10 mirrors, 10 destructible covers.')
