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
def reachable(start,goal,locked=False,barrier=False):
    obstacles=static+([r for r in rects if r[0]=='Exit Gate'] if locked else [])
    if barrier: obstacles += [r for r in rects if r[0]=='Room 4 - Ball Pass Barrier']
    valid=set()
    for ix in range(-120,121):
        x=ix*step
        for iy in range(-23,72):
            y=iy*step
            # Covers the corners of the tank's collider in any body rotation.
            if not any(a-.73<x<b+.73 and c-.73<y<d+.73 for _,a,c,b,d in obstacles): valid.add((ix,iy))
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
assert not reachable((5,0),(-5,0),barrier=True), 'Active room-4 barrier can be bypassed'
assert reachable((5,0),(-5,0)), 'Room-4 barrier removal does not restore passage'

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
barrier_rect=next(r for r in rects if r[0]=='Room 4 - Ball Pass Barrier')
assert intersect((5,0),(0,4.45),barrier_rect), 'Ricochet route does not exercise ball-pass barrier'
assert any(intersect((-5,0),(5,0),r) for r in static), 'Enemy line of sight crosses no obstacle in blocked case'
assert not any(intersect((-23,1),(-18,-3),r) for r in static), 'Shield enemy cannot see player in clear case'
robot_guid=re.search(r'guid: (\w+)',(root/'Assets/Scripts/TutorialRobot.cs.meta').read_text(encoding='utf-8-sig'))[1]
assert any(type==114 and robot_guid in body and 'roomIndex: 4' in body for type,body in docs.values()), 'Shield enemy has no firing controller'
checkpoint_ids=list(map(int,re.findall(r'    checkpoint: \{fileID: (\d+)\}',text)))
assert len(checkpoint_ids)==5, 'Missing room checkpoints'
checkpoint_positions=[]
for id in checkpoint_ids:
    pos,scale=world(id);checkpoint_positions.append(pos)
    assert not any(a-.73<pos[0]<b+.73 and c-.73<pos[1]<d+.73 for _,a,c,b,d in rects
                   if _!='Player Tank'), 'Checkpoint overlaps a tank obstacle'
for type,body in docs.values():
    if type!=114 or robot_guid not in body:continue
    room=int(re.search(r'roomIndex: (\d+)',body)[1])
    turret=int(re.search(r'turret: \{fileID: (\d+)',body)[1])
    pos,_=world(turret)
    aim=(checkpoint_positions[room][0]-pos[0],checkpoint_positions[room][1]-pos[1])
    z,w=map(float,re.search(r'm_LocalRotation: \{x: 0, y: 0, z: ([\d.e+-]+), w: ([\d.e+-]+)',docs[turret][1]).groups())
    front=(-2*z*w,1-2*z*z)
    distance=math.hypot(*aim)
    angle=math.degrees(math.acos(max(-1,min(1,(front[0]*aim[0]+front[1]*aim[1])/distance))))
    limit=float(re.search(r'sightAngle: ([\d.]+)',body)[1])/2
    assert angle<limit, 'Robot initial sight cone misses its room entrance'
    assert distance<float(re.search(r'sightRange: ([\d.]+)',body)[1]), 'Entrance is outside robot sight range'
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
print('Geometry passed: tank route, gates and barrier, ricochet routes, visibility, five safe checkpoints and all robot sight cones facing entrances.')
