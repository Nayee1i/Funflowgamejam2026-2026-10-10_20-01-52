# One Bounce tank prototype

Open `Assets/Scenes/TankArena.unity` and press Play. The walls, floor, tank,
turret, muzzle and camera are saved in the scene and visible before Play.
Movement and shooting references are already connected. The ball is stored in
`Assets/Prefabs/Ball.prefab`. TankArena is the first scene in Build Settings.
The existing `SampleScene` still generates the prototype when played.

- WASD or arrow keys: move (diagonal speed is normalized).
- Mouse: aim the turret.
- Hold left mouse button: fire a ball every 0.4 seconds.
- An ordinary wall absorbs a ball immediately.
- Mirrors reflect a ball up to five times without changing its color.
- A sixth mirror contact or any ordinary wall removes it. Shots also expire after 8 seconds.

All visuals use `Assets/Sprites/Square.png` or `Assets/Sprites/Circle.png`, with
the unlit material in `Assets/Materials/ArenaUnlit.mat`. The four walls have
static BoxCollider2D components, and the tank uses
a dynamic Rigidbody2D with no gravity and a body that turns toward movement.
The turret aims independently, and CameraDeadZone follows outside its central
zone. Shots sweep a
circle across their full movement distance to avoid crossing walls at high speed.
The tank is on Ignore Raycast so returning shots pass through it. The projectile
sweep checks all other physics layers. Only colliders with `BlueMirror` on their
object or parent, or `RedMirror`, reflect balls; all other solid surfaces absorb them.

Drag `Assets/Prefabs/BlueMirror.prefab` into a scene to place a blue rectangular
mirror. It has a Square sprite, an unlit material, a solid BoxCollider2D and a
BlueMirror component. Rotate or scale it using its Transform. Two mirrors are
already saved in TankArena as editable objects. Lasers are not implemented.

Tuning:

- `TankMovement.cs`: movement speed and fire interval.
- `Bullet.cs`: projectile speed, radius, and maximum lifetime.
- `TankArena.unity`: edit wall positions, tank shapes and colors directly.
- `TankArena.cs`: instructions and the SampleScene fallback.
- `BlueMirror.cs`: identifies surfaces that can reflect a ball.
- `ArenaCameraFit.cs`: keep the full arena visible when the Game view is resized.

Manual gameplay checks:

1. Move into each wall and confirm the tank stays inside the arena.
2. Aim diagonally at a blue mirror. Confirm up to five reflections with unchanged color, then removal on a sixth mirror contact.
3. Fire at ordinary walls, beside walls and into corners. Confirm ordinary walls absorb shots.
4. Hold fire while moving. Confirm continuous shots and independent turret aiming.
5. Resize the Game view. Confirm all four walls remain visible.
