# Tank sprite generation prompts

Generated with the built-in image_gen tool, transparent_background=true.
Enemy output: Assets/Sprites/EnemyTank.png.
Player output: Assets/Sprites/PlayerTank.png.
The player was edited from the enemy reference to preserve the silhouette.

## Enemy

Use case: stylized-concept. Asset type: Unity 2D top-down tank HULL sprite. Generate one isolated enemy tank hull with two integrated charcoal caterpillar tracks and red-orange angular armor chassis, facing straight UP, orthographic 90-degree overhead view, symmetric left-right, compact rectangular silhouette. Clean flat graphic game sprite with few subtle panel details, dark outline, readable at small size, matches a simple dark-blue grid tank game. NO turret, NO cannon, NO barrel: turret will be a separate rotating game object. Keep central flat circular turret mounting plate subtle. Transparent background with real alpha. Centered with narrow even padding, entire hull visible, no shadow outside hull, no perspective, no ground, no text, no labels, no UI. Intended hull proportion width 0.95 to height 1.1.

## Player

Use case: identity-preserve. Asset type: Unity 2D PLAYER tank HULL sprite. This image is the edit target and matching style reference. Create the matching player tank hull by changing only the red-orange armor to turquoise/emerald green with mint highlights. Preserve precisely the same overhead orthographic orientation, track shapes, center mounting plate, dimensions, silhouette, framing, padding, fine outlines, panel details and transparency. NO turret, no cannon, no barrel, no added objects, no text, no background. This is a chassis sprite; the existing game turret will rotate independently over it.
