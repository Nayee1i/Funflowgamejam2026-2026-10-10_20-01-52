# Tutorial

Open `Assets/Scenes/Tutorial.unity` and press Play. All rooms, walls, gates,
targets, robots and mirrors are saved as editable scene objects.

1. Movement: walk around two alternating walls; exit through the top passage.
2. Shooting: destroy three stationary targets; the left gate opens.
3. Combat: destroy two robots firing every 2.1 seconds. Their first shots are
   staggered. Any enemy ball touching the player returns to the room checkpoint.
   The bottom gate opens after both robots are destroyed.
4. Blue mirrors: a column blocks the direct shot. The enemy on the other side
   accepts only reflected balls. Shoot via either mirror above or below the
   column. The cyan barrier to its right stops the tank but passes balls; it
   disappears when the enemy dies. Then use the left exit.
5. Red mirror: ordinary balls cannot destroy the shielded enemy. A ball reflected
   from the red mirror removes the shield without killing the enemy. A subsequent
   shot destroys it. This enemy also fires when it can see the player. The
   tutorial returns to LevelSelection after 1.5 seconds.

Controls: WASD/arrows move, mouse aims, left mouse button fires, R returns to the
current room checkpoint. Entering a new room activates its checkpoint. Respawning
resets all enemies, shields, gates and barriers in the current encounter, keeps
previous rooms cleared, and removes existing projectiles. Checkpoint Room 1–5
objects are editable transforms, referenced in TutorialLevel's Rooms list.
Ordinary walls absorb all balls. Blue and red mirrors allow one reflection per
ball. Enemy shots cannot damage other enemies, and the player's shots cannot
damage the player. Lasers are not implemented.

The tank body turns toward movement at 540 degrees/second; the turret aims
independently with the mouse. CameraDeadZone keeps the camera stationary while
the player is inside its central zone and smoothly follows outside it. Zone
width/height and smoothing time are editable on Main Camera. TutorialLevel
adjusts camera size without moving it between room centers.

Shooting robots check a clear path with the ball's width before firing. Walls,
columns, mirrors and closed gates block this check. BallPassBarrier is ignored
by both projectiles and enemy visibility; its BoxCollider2D still stops tanks.
Robots remain dormant until the player enters their sight cone with clear line
of sight. Sight Range and Sight Angle are editable on TutorialRobot. The initial
turret orientation defines the cone; all three robots initially face the room
entrance. First Shot Delay starts at discovery, not at room entry. Activated
robots track the player but still need clear visibility to fire.

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
respawn at the current checkpoint with targets, gates and the shield restored;
check that cleared earlier rooms stay cleared and no existing balls remain.
Also check body rotation while moving diagonally, stationary camera movement
inside the dead zone, smooth corridor crossings, robots withholding fire behind
walls, the barrier before/after killing the room-4 enemy, and shield-enemy fire.
