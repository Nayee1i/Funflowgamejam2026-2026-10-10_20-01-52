from pathlib import Path
import re
source=Path('Assets/Editor/BuildEmptySecondLevel.cs').read_text(encoding='utf-8-sig')
a=source.index('internal static readonly Vector2[] BoundaryPixels=')
b=source.index('    static Vector2 Pixel',a)
points=[((int(x)-793)/100,(496-int(y))/100) for x,y in re.findall(r'new Vector2\((\d+),(\d+)\)',source[a:b])]
p=Path('Assets/Scenes/level2.unity'); s=p.read_text(encoding='utf-8-sig')
m=re.search(r'  m_Points:\n(?:  - \{x: [^\n]+\}\n)+',s); assert m
old=[(float(x),float(y)) for x,y in re.findall(r'x: ([^,]+), y: ([^}]+)',m.group())]
assert len(points)==len(old)
assert all(n==o for n,o in zip(points,old) if o[1]>=-.59),'Upper contour changed'
block='  m_Points:\n'+''.join(f'  - {{x: {x:g}, y: {y:g}}}\n' for x,y in points)
p.write_text(s[:m.start()]+block+s[m.end():],encoding='utf-8')
print('Lower boundary moved closer to artwork; upper points identical.')
