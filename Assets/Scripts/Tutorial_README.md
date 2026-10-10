# Tutorial

Open `Assets/Scenes/Tutorial.unity` and press Play. All rooms, walls, gates,
targets, robots and mirrors are saved as editable scene objects.

1. Movement: walk around two alternating walls; exit through the top passage.
2. Shooting: destroy three stationary targets; the left gate opens.
3. Combat: destroy two robots firing every 2.1 seconds. Their first shots are
   staggered. Any enemy ball touching the player restarts the entire tutorial.
   The bottom gate opens after both robots are destroyed.
4. Blue mirrors: a column blocks the direct shot. The enemy on the other side
   accepts only reflected balls. Shoot via either mirror above or below the
   column, then use the left exit.
5. Red mirror: ordinary balls cannot destroy the shielded enemy. A ball reflected
   from the red mirror removes the shield without killing the enemy. A subsequent
   shot destroys it. The tutorial returns to LevelSelection after 1.5 seconds.

Controls: WASD/arrows move, mouse aims, left mouse button fires, R restarts.
Ordinary walls absorb all balls. Blue and red mirrors allow one reflection per
ball. Enemy shots cannot damage other enemies, and the player's shots cannot
damage the player. Lasers are not implemented.

Room instructions, gates and objective references are configured on the
TutorialLevel component. Robot timing is editable on TutorialRobot. Shield and
reflected-only damage are configured on TutorialTarget. Mirrors are available as
BlueMirror and RedMirror prefabs.

Tutorial, LevelSelection, TankArena and SampleScene are enabled in Build Settings.
The existing arena and the user's scene2 are kept separately.

Manual Play-mode checks: finish each room, verify gates stay closed before the
last target dies, dodge both robots, test ordinary and reflected shots against
the column enemy, test the shield with ordinary/blue/red shots, and verify that
completion opens the level selection menu. Test R and enemy hits for a clean
restart with targets, gates and the shield restored.
