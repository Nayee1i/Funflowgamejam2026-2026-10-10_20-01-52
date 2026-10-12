"""Reproduce the current Figma node transforms using original bitmap pixels.
Run with the bundled Python runtime. No credentials are stored in this script.
"""
import json, math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFont

project=Path(__file__).resolve().parents[1]
ns=json.load(open(project/'Tools/FigmaLevel2/Geometry.json',encoding='utf-8'))
sources=project/'Tools/FigmaLevel2/Sources'
art=project/'Assets/Resources/SecondLevel'
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
 return dict(node=id,asset=name,x=float((center[0]-960)*factor),y=float((625-center[1])*factor),
             angle=angle,sx=float(w*sx*factor/(im.width/100)),sy=float(h*sy*factor/(im.height/100)*(-1 if flip else 1)),
             opaque=[float((opaque[0]+opaque[2]-im.width)/200),float((im.height-opaque[1]-opaque[3])/200)],
             radius=float(min(opaque[2]-opaque[0],opaque[3]-opaque[1])/200),w=float(w*sx*factor),h=float(h*sy*factor),outline=hull(im))

rooms=[]
for frame,obs,mirrors,enemies in [
 ('57:3427',['60:3457','60:3458'],[('60:3459',False),('61:3460',True)],[('61:3463','61:3464','61:3461',False)]),
 ('65:3495',['71:3542','71:3602','71:3601'],[('65:3509',False)],[('71:3546','71:3547',None,True),('71:3555','71:3556',None,False),('71:3604','71:3605',None,False)]),
 ('71:3558',['71:3574','71:3575','71:3576'],[('71:3577',True),('71:3578',True)],[('71:3580','71:3581',None,True),('71:3584','71:3585','71:3586',False),('71:3589','71:3590','71:3591',False)]),
 ('71:3608',['71:3643','71:3651','71:3652','71:3661'],[('71:3644',False),('71:3645',True)],[('71:3648','71:3649',None,False),('71:3659','71:3660',None,True),('71:3655','71:3656','71:3657',False)])]:
 rooms.append(dict(frame=frame,obstacles=[placement(i,frame) for i in obs],
                   mirrors=[dict(art=placement(i,frame),red=red) for i,red in mirrors],
                   enemies=[dict(body=placement(b,frame),head=placement(h,frame),shield=placement(s,frame) if s else None,promoted=p) for b,h,s,p in enemies]))
json.dump(dict(rooms=rooms),open(art/'Layout.json','w',encoding='utf-8'),indent=2)

# Hint and victory use the same shared UI renderer as the first level.
panels=[]
for name,frame,ids in [('NewRobotHint','65:3495',['71:3593','71:3596','71:3598','71:3599','71:3600']),('Victory','83:3951',['83:4011','83:4012','83:4013','83:4014'])]:
 parts=[]
 for id in ids:
  n=ns[id];b=n['absoluteBoundingBox'];f=ns[frame]['absoluteBoundingBox'];nameasset,im=asset(id)
  parts.append(dict(asset='SecondLevel/'+nameasset,x=b['x']-f['x'],y=b['y']-f['y'],w=b['width'],h=b['height'],close=id=='71:3596',action='menu' if id=='83:4013' else 'next' if id=='83:4014' else None))
 panels.append(dict(name=name,parts=parts))
json.dump(dict(panels=panels),open(art/'Panels.json','w',encoding='utf-8'),ensure_ascii=False,indent=2)

# Native crop contact sheet for visual quality inspection only.
samples=['60:3457','60:3458','71:3574','71:3546','71:3547','61:3463','61:3464']
sheet=Image.new('RGBA',(1400,440),(30,30,30,255));d=ImageDraw.Draw(sheet)
for i,id in enumerate(samples):
 im=bitmap(id);im.thumbnail((190,380));sheet.alpha_composite(im,(i*200,30));d.text((i*200,5),id)
sheet.save(project/'Tools/FigmaLevel2/Native-crops.png')
print('Extracted',len(list(art.glob('*.png'))),'native assets and four room transforms')
