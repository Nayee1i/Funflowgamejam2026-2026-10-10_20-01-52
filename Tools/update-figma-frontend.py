import json,math,shutil
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
root=Path(__file__).resolve().parents[2]
project=root/'Funflowgamejam2026'
ns=json.load(open(root/'frontend-game-index.json',encoding='utf-8'))
art=project/'Assets/Resources/FirstLevelFX'; sources=root/'FigmaFrontendSources'

def bitmap(n):
 if n['type']=='GROUP':
  child=next(c for c in n['children'] if any(f.get('type')=='IMAGE' for f in c.get('fills',[])))
  im=bitmap(child);cb=child['absoluteBoundingBox'];b=n['absoluteBoundingBox'];sx=im.width/cb['width'];sy=im.height/cb['height']
  return im.crop(tuple(round(v) for v in ((b['x']-cb['x'])*sx,(b['y']-cb['y'])*sy,(b['x']+b['width']-cb['x'])*sx,(b['y']+b['height']-cb['y'])*sy)))
 b=n['absoluteBoundingBox'];w,h=b['width'],b['height']
 if n['type']=='TEXT':
  st=n['style'];font=ImageFont.truetype(str(root/'B52.ttf'),round(st['fontSize']*2));im=Image.new('RGBA',(math.ceil(w*2),math.ceil(h*2)))
  draw=ImageDraw.Draw(im);txt=n['characters'].upper() if st.get('textCase')=='UPPER' else n['characters'];lines=[]
  for paragraph in txt.split('\n'):
   line=''
   for word in paragraph.split():
    trial=(line+' '+word).strip()
    if line and draw.textlength(trial,font=font)>im.width:lines.append(line);line=word
    else:line=trial
   lines.append(line)
  col=n['fills'][0]['color'];color=tuple(round(col[k]*255) for k in ('r','g','b','a'));spacing=st.get('lineHeightPx',st['fontSize']*1.1)*2
  for i,line in enumerate(lines):
   tw=draw.textlength(line,font=font);x=(im.width-tw)/2 if st.get('textAlignHorizontal')=='CENTER' else im.width-tw if st.get('textAlignHorizontal')=='RIGHT' else 0
   draw.text((x,i*spacing),line,font=font,fill=color,anchor='lt')
  return im
 fill=next(f for f in n['fills'] if f['type']=='IMAGE');im=Image.open(sources/(fill['imageRef']+'.png')).convert('RGBA')
 if fill['scaleMode']=='STRETCH':
  m=fill['imageTransform'];iw,ih=im.size;im=im.crop(tuple(round(v) for v in (m[0][2]*iw,m[1][2]*ih,(m[0][2]+m[0][0])*iw,(m[1][2]+m[1][1])*ih)))
 elif fill['scaleMode']=='FILL':
  iw,ih=im.size;ratio=w/h
  if iw/ih>ratio:
   cw=ih*ratio;im=im.crop((round((iw-cw)/2),0,round((iw+cw)/2),ih))
  else:
   ch=iw/ratio;im=im.crop((0,round((ih-ch)/2),iw,round((ih+ch)/2)))
 return im

def part(id,frame,action=None):
 n=ns[id];b=n['absoluteBoundingBox'];f=ns[frame]['absoluteBoundingBox'];asset='UI_'+id.replace(':','_');bitmap(n).save(art/(asset+'.png'))
 p=dict(asset=asset,x=b['x']-f['x'],y=b['y']-f['y'],w=b['width'],h=b['height'],close=False,action=action)
 return p
panels={'panels':[]}
for name,frame,ids in [
 ('StartHint','56:3181',['56:3203','56:3204','56:3207','56:3208','56:3210','119:79','119:77','119:75','119:76','119:78']),
 ('BlueHint','56:3118',['56:3138','56:3139','56:3142','56:3143','56:3144','56:3145']),
 ('RedHint','56:3146',['56:3166','56:3167','56:3170','56:3171','56:3172','56:3173','56:3174']),
 ('Victory','56:3089',['56:3110','56:3111','56:3112','56:3113','115:4'])]:
 ps=[part(id,frame) for id in ids]
 for p in ps:
  if p['asset'] in ['UI_56_3204','UI_56_3139','UI_56_3167']:p['close']=True
  if p['asset']=='UI_56_3112':p['action']='menu'
  if p['asset']=='UI_56_3113':p['action']='next'
 panels['panels'].append(dict(name=name,parts=ps))
json.dump(panels,open(art/'Panels.json','w',encoding='utf-8'),ensure_ascii=False,indent=2)
menu={'panels':[]}
for name,frame,ids,actions in [
 ('Settings','56:3241',['56:3248','56:3250','56:3256','56:3249','56:3251','56:3252','56:3253','56:3254','56:3255','126:93','126:94','126:92'],{'56:3256':'close','56:3249':'soundUp','56:3253':'soundDown','56:3254':'musicUp','56:3255':'musicDown','126:94':'sensitivityDown','126:92':'sensitivityUp'}),
 ('Modes','56:3007',['56:3017','56:3018','56:3019','56:3020','56:3021','56:3022','56:3025'],{'56:3018':'level1','56:3019':'level2','56:3020':'level3','56:3021':'arena','56:3022':'close'})]:
 menu['panels'].append(dict(name=name,parts=[part(id,frame,actions.get(id)) for id in ids]))
json.dump(menu,open(art/'MenuPanels.json','w',encoding='utf-8'),ensure_ascii=False,indent=2)
for src,dst in [('Modal_48_2837','MenuBarFull'),('Modal_48_2831','MenuBarEmpty')]:shutil.copyfile(project/'Assets/UI/MainMenu'/(src+'.png'),art/(dst+'.png'))
bitmap(ns['118:36']).save(art/'ResultStar.png')
im=bitmap(ns['118:36']);grey=im.convert('LA').convert('RGBA');grey.putalpha(im.getchannel('A'));grey.save(art/'ResultStarEmpty.png')
bitmap(ns['118:41']).save(art/'ResultTimeFrame.png')
bitmap(ns['56:3019']).save(art/'LevelLocked.png')
# The unlocked play plate uses the first level's exact atlas crop.
bitmap(ns['56:3018']).save(art/'LevelUnlocked.png')
with open(art/'ArtworkSources.md','a',encoding='utf-8') as f:f.write('\nFrontend refresh 2026-10-12: native Figma sources from frames 56:3241 (sensitivity), 56:3007 (level results), 56:3181, 56:3118, 56:3146 (tutorial), 56:3089 / 115:29 (victory timer). Image atlas crops preserve native pixels; B52 text rasterized at 2x according to current alignment and geometry.\n')
print('Updated native panel assets and layouts')
# Pause menu: Frame 12.
ids=['73:3684','73:3685','73:3696','73:3697','73:3698','73:3699','73:3700','73:3701','73:3722','73:3729','73:3721']
actions={'73:3685':'close','73:3696':'soundUp','73:3699':'soundDown','73:3700':'musicUp','73:3701':'musicDown','73:3722':'continue','73:3729':'restart','73:3721':'menu'}
json.dump({'panels':[dict(name='Pause',parts=[part(id,'73:3662',actions.get(id)) for id in ids])]},open(art/'PausePanel.json','w',encoding='utf-8'),ensure_ascii=False,indent=2)
