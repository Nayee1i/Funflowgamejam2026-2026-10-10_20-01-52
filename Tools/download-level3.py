import json,urllib.request,shutil,concurrent.futures
from pathlib import Path
root=Path('Tools/FigmaLevel3');ns=json.load(open(root/'Geometry.json',encoding='utf-8'))
frames=['73:3784','83:3876','83:4016','98:4105','98:4117','98:4129']
refs=set()
def collect(n):
 for f in n.get('fills',[]):
  if f.get('imageRef'):refs.add(f['imageRef'])
 for c in n.get('children',[]):collect(c)
for id in frames:collect(ns[id])
links=json.load(open(root/'image-links.json',encoding='utf-8'))['meta']['images'];(root/'Sources').mkdir(exist_ok=True)
def download(ref):
 dest=root/'Sources'/(ref+'.png')
 for source in [Path('Tools/FigmaLevel2/Sources')/(ref+'.png'),Path('../FigmaOriginals')/(ref+'.png'),Path('..')/(ref+'.png')]:
  if source.exists():shutil.copyfile(source,dest);return ref
 urllib.request.urlretrieve(links[ref],dest);return ref
with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:
 for r in pool.map(download,refs):print('Source',r)
for id,url in json.load(open(root/'render-links.json'))['images'].items():
 urllib.request.urlretrieve(url,root/(id.replace(':','_')+'.png'))
print('Fetched',len(refs),'original images and reference frames')
