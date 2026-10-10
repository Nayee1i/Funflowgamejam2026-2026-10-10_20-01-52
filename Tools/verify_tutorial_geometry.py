from pathlib import Path
import re, math
from collections import deque

root=Path(__file__).resolve().parent.parent
text=(root/'Assets/Scenes/Tutorial.unity').read_text(encoding='utf-8')
docs={int(m[2]):(int(m[1]),m[3]) for m in re.finditer(r'--- !u!(\d+) &(\d+)\n(.*?)(?=--- !u!|\Z)',text,re.S)}
names={id:re.search(r'm_Name: (.*)',body)[1].strip().strip('"') for id,(type,body) in docs.items() if type==1}
transforms={}
def vector(body,key):
    m=re.search(key+r': \{x: ([\d.e+-]+), y: ([\d.e+-]+)',body)
    return tuple(map(float,m.groups()))
for id,(type,body) in docs.items():
    if type==4:
        transforms[id]=(int(re.search(r'm_GameObject: \{fileID: (\d+)',body)[1]),vector(body,'m_LocalPosition'),
                        vector(body,'m_LocalScale'),int(re.search(r'm_Father: \{fileID: (\d+)',body)[1]))
go_to_tr={v[0]:k for k,v in transforms.items()}
def world(tr):
    go,pos,scale,parent=transforms[tr]
    if not parent:return pos,scale
    pp,ps=world(parent)
    return (pp[0]+pos[0]*ps[0],pp[1]+pos[1]*ps[1]),(scale[0]*ps[0],scale[1]*ps[1])
rects=[]
for id,(type,body) in docs.items():
    if type!=61:continue
    go=int(re.search(r'm_GameObject: \{fileID: (\d+)',body)[1])
    pos,scale=world(go_to_tr[go]); size=vector(body,'m_Size')
    rects.append((names[go],pos[0]-size[0]*scale[0]/2,pos[1]-size[1]*scale[1]/2,
                  pos[0]+size[0]*scale[0]/2,pos[1]+size[1]*scale[1]/2))
static=[r for r in rects if 'Wall' in r[0] or r[0] in ('Movement Obstacle A','Movement Obstacle B','Central Column','Blue Mirror','Red Mirror')]
step=.25
def reachable(start,goal,locked=False):
    obstacles=static+([r for r in rects if r[0]=='Exit Gate'] if locked else [])
    valid=set()
    for ix in range(-120,121):
        x=ix*step
        for iy in range(-23,72):
            y=iy*step
            if not any(a-.48<x<b+.48 and c-.56<y<d+.56 for _,a,c,b,d in obstacles): valid.add((ix,iy))
    st=tuple(round(v/step) for v in start); end=tuple(round(v/step) for v in goal)
    q=deque([st]); visited={st}
    while q:
        p=q.popleft()
        if p==end:return True
        for dx,dy in ((1,0),(-1,0),(0,1),(0,-1)):
            n=(p[0]+dx,p[1]+dy)
            if n in valid and n not in visited:visited.add(n);q.append(n)
    return False

assert reachable((20,-3),(-20,-3)), 'Tank cannot traverse the complete opened level'
assert not reachable((20,-3),(0,12),locked=True), 'Closed objective gates can be bypassed'

def intersect(p,q,rect,radius=.15):
    _,a,c,b,d=rect; a-=radius;c-=radius;b+=radius;d+=radius
    t0,t1=0,1
    for origin,delta,low,high in ((p[0],q[0]-p[0],a,b),(p[1],q[1]-p[1],c,d)):
        if abs(delta)<1e-9:
            if not low<=origin<=high:return False
        else:
            u,v=sorted(((low-origin)/delta,(high-origin)/delta));t0=max(t0,u);t1=min(t1,v)
            if t0>t1:return False
    return True
column=next(r for r in static if r[0]=='Central Column')
assert intersect((5,0),(-5,0),column), 'Column does not block the direct shot'
for y in (-4.45,4.45):
    assert not intersect((5,0),(0,y),column), 'Column blocks the outgoing mirror shot'
    assert not intersect((0,y),(-5,0),column), 'Column blocks the reflected mirror shot'
    for r in static:
        if r[0] in ('Blue Mirror',):continue
        assert not intersect((5,0),(0,y),r), f'Outgoing shot blocked by {r[0]}'
        assert not intersect((0,y),(-5,0),r), f'Reflected shot blocked by {r[0]}'
red_hit=(-26.75,-.2)
for r in static:
    if r[0]=='Red Mirror':continue
    assert not intersect((-18,-3),red_hit,r), f'Red mirror approach blocked by {r[0]}'
    assert not intersect(red_hit,(-23,1),r), f'Red mirror return blocked by {r[0]}'
print('Geometry passed: full tank route, gates prevent bypass, column blocks direct fire, both blue ricochets and red shield-breaking path are clear.')
