"""Reproduce the current Figma node transforms using original bitmap pixels.
Run with the bundled Python runtime. No credentials are stored in this script.
"""
import json, math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

project=Path(__file__).resolve().parents[1]
ns=json.load(open(project/'Tools/FigmaLevel3/Geometry.json',encoding='utf-8'))
sources=project/'Tools/FigmaLevel3/Sources'
art=project/'Assets/Resources/ThirdLevel'
art.mkdir(parents=True,exist_ok=True)
parents={c['id']:n['id'] for n in ns.values() for c in n.get('children',[])}
factor=1586/1270/100

def matrix(id,frame):
 if id==frame:return np.eye(3)
 m=np.eye(3);m[:2,:]=ns[id]['relativeTransform']
 return matrix(parents[id],frame)@m

def bitmap(id):
 n=ns[id]
 if n['type']=='TEXT':
  st=n['style'];b=n['absoluteBoundingBox'];im=Image.new('RGBA',(math.ceil(b['width']*2),math.ceil(b['height']*2)))
  font=ImageFont.truetype(str(project.parent/'B52.ttf'),round(st['fontSize']*2));d=ImageDraw.Draw(im)
  lines=[];line=''
  for word in n['characters'].split():
   trial=(line+' '+word).strip()
   if line and d.textlength(trial,font=font)>im.width:lines.append(line);line=word
   else:line=trial
  lines.append(line);col=n['fills'][0]['color'];color=tuple(round(col[k]*255) for k in ('r','g','b','a'))
  for i,line in enumerate(lines):d.text((0,i*st.get('lineHeightPx',st['fontSize']*1.1)*2),line,font=font,fill=color,anchor='lt')
  return im
 fill=next(f for f in n['fills'] if f.get('imageRef'))
 im=Image.open(sources/(fill['imageRef']+'.png')).convert('RGBA');iw,ih=im.size
 if fill['scaleMode']=='STRETCH':
  m=fill['imageTransform'];im=im.crop(tuple(round(v) for v in (m[0][2]*iw,m[1][2]*ih,(m[0][2]+m[0][0])*iw,(m[1][2]+m[1][1])*ih)))
 elif fill['scaleMode']=='FILL':
  s=n.get('size',{});b=n['absoluteBoundingBox'];ratio=s.get('x',b['width'])/s.get('y',b['height'])
  if iw/ih>ratio:
   cw=ih*ratio;im=im.crop((round((iw-cw)/2),0,round((iw+cw)/2),ih))
  else:
   ch=iw/ratio;im=im.crop((0,round((ih-ch)/2),iw,round((ih+ch)/2)))
 return im

def asset(id):
 name='Art_'+id.replace(':','_');im=bitmap(id);im.save(art/(name+'.png'));return name,im

def hull(im):
 # Convex opaque silhouette ignores tiny transparent artifacts and soft shadows.
 a=np.array(im.getchannel('A'))[::4,::4]>210;largest=[]
 for y,x in zip(*np.nonzero(a)):
  if not a[y,x]:continue
  a[y,x]=False;component=[];todo=[(y,x)]
  while todo:
   cy,cx=todo.pop();component.append((cx*4,cy*4))
   for ny,nx in [(cy-1,cx),(cy+1,cx),(cy,cx-1),(cy,cx+1)]:
    if 0<=ny<a.shape[0] and 0<=nx<a.shape[1] and a[ny,nx]:a[ny,nx]=False;todo.append((ny,nx))
  if len(component)>len(largest):largest=component
 points=largest
 pts=sorted(set(points))
 def cross(o,a,b):return (a[0]-o[0])*(b[1]-o[1])-(a[1]-o[1])*(b[0]-o[0])
 lower=[];upper=[]
 for p in pts:
  while len(lower)>=2 and cross(lower[-2],lower[-1],p)<=0:lower.pop()
  lower.append(p)
 for p in reversed(pts):
  while len(upper)>=2 and cross(upper[-2],upper[-1],p)<=0:upper.pop()
  upper.append(p)
 return [dict(x=(x-im.width/2)/100,y=(im.height/2-y)/100) for x,y in lower[:-1]+upper[:-1]]

def placement(id,frame):
 n=ns[id];m=matrix(id,frame);w,h=n['size']['x'],n['size']['y'];center=m@np.array([w/2,h/2,1])
 name,im=asset(id)
 # Figma y increases down, Unity y increases up. Reflected nodes retain flipY.
 angle=math.degrees(math.atan2(-m[1,0],m[0,0]));sx=np.linalg.norm(m[:2,0]);sy=np.linalg.norm(m[:2,1])
 flip=np.linalg.det(m[:2,:2])<0
 opaque=im.getchannel('A').point(lambda p:255 if p>180 else 0).getbbox() or (0,0,im.width,im.height)
 return dict(node=id,asset=name,x=float((center[0]-960)*factor),y=float((592-center[1])*factor),
             angle=angle,sx=float(w*sx*factor/(im.width/100)),sy=float(h*sy*factor/(im.height/100)*(-1 if flip else 1)),
             opaque=[float((opaque[0]+opaque[2]-im.width)/200),float((im.height-opaque[1]-opaque[3])/200)],
             radius=float(min(opaque[2]-opaque[0],opaque[3]-opaque[1])/200),w=float(w*sx*factor),h=float(h*sy*factor),outline=hull(im))


roster=[
('73:3784','79:3821',['79:3834','82:3867'],['79:3843','79:3844','82:3861'],[('79:3846',False),('82:3868',True)],[('79:3837','79:3838','79:3839',False),('79:3848','79:3849',None,True)]),
('83:3876','83:3887',['83:3918','83:3919','83:3922'],['83:3948'],[('83:3921',False),('83:4015',False),('83:3937',True)],[('83:3930','83:3931','83:3932',False),('83:3944','83:3945',None,False),('83:3939','83:3940',None,True)]),
('83:4016','83:4027',['102:4179','102:4180'],['102:4187','102:4188'],[('102:4181',False),('102:4186',True)],[('102:4191','102:4192','102:4193',False),('102:4196','102:4197',None,False),('102:4203','102:4204',None,False),('102:4199','102:4200',None,True)]),
('98:4105','98:4116',['103:4217'],['103:4208','103:4238','103:4209','103:4239'],[('103:4222',True),('103:4227',True),('103:4243',False),('103:4244',False)],[('103:4215','103:4216',None,False),('103:4230','103:4231','103:4232',False),('103:4235','103:4236','103:4237',False),('103:4241','103:4242',None,True),('103:4246','103:4247',None,True)]),
('98:4117','98:4128',[],['105:4259','105:4262','105:4260','105:4263'],[('105:4269',True),('105:4264',False),('105:4274',True),('105:4275',False)],[('104:4255','104:4256','105:4258',False),('105:4279','105:4280',None,False),('105:4283','105:4284',None,False)])]
def glows(frame):
 result=[]
 def visit(n):
  if n['type']=='ELLIPSE':
   id=n['id'];name='Glow_'+id.replace(':','_');im=Image.open(project/'Tools/FigmaLevel3'/(name+'.png'));im.save(art/(name+'.png'))
   b=n.get('absoluteRenderBounds',n['absoluteBoundingBox']);f=ns[frame]['absoluteBoundingBox'];x=b['x']-f['x'];y=b['y']-f['y']
   result.append(dict(node=id,asset=name,x=(x+b['width']/2-960)*factor,y=(592-y-b['height']/2)*factor,angle=0,sx=b['width']*factor/(im.width/100),sy=b['height']*factor/(im.height/100)))
  for c in n.get('children',[]):visit(c)
 visit(ns[frame]);return result

rooms=[]
for frame,bg,obs,blocks,mirrors,enemies in roster:
 rooms.append(dict(frame=frame,glows=glows(frame),background=placement(bg,frame),obstacles=[placement(i,frame) for i in obs],blocks=[placement(i,frame) for i in blocks],mirrors=[dict(art=placement(i,frame),red=r) for i,r in mirrors],enemies=[dict(body=placement(b,frame),head=placement(h,frame),shield=placement(sh,frame) if sh else None,promoted=p,boss=b=='104:4255') for b,h,sh,p in enemies]))
stages=[]
for index,im in enumerate([bitmap('79:3843'),bitmap('82:3861'),Image.open(sources/'b3d51e4d0c475a7f88d9758745b5ac4ca3118a0a.png').convert('RGBA').crop((590,0,875,320)),Image.open(sources/'b3d51e4d0c475a7f88d9758745b5ac4ca3118a0a.png').convert('RGBA').crop((875,0,1240,330))]):
 name='BlockDamage_'+str(index);im.save(art/(name+'.png'));stages.append(dict(asset=name,outline=hull(im)))
json.dump(dict(rooms=rooms,damageStages=stages),open(art/'Layout.json','w',encoding='utf-8'),indent=2)
panels=[]
for name,frame,ids in [('DestructibleHint','73:3784',['79:3851','79:3858','79:3859']),('Victory','98:4129',['98:4168','98:4169','98:4170','98:4171'])]:
 parts=[]
 for id in ids:
  b=ns[id]['absoluteBoundingBox'];f=ns[frame]['absoluteBoundingBox'];a,im=asset(id)
  parts.append(dict(asset='ThirdLevel/'+a,x=b['x']-f['x'],y=b['y']-f['y'],w=b['width'],h=b['height'],close=False,action='menu' if id=='98:4170' else 'restart' if id=='98:4171' else None))
 if name=='DestructibleHint':
  old=json.load(open(project/'Assets/Resources/SecondLevel/Panels.json',encoding='utf-8'))['panels'][0]['parts'];close=next(p.copy() for p in old if p.get('close'));parts.append(close)
 panels.append(dict(name=name,parts=parts))
json.dump(dict(panels=panels),open(art/'Panels.json','w',encoding='utf-8'),ensure_ascii=False,indent=2)
print('Third level: native assets extracted, five rooms, 17 enemies and 14 destructible blocks.')
