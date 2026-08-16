# Sprite System - How Enemy Sprites Work

## Folder Structure

Each enemy type has its own folder under `resources/sprites/npc/`:

```
resources/sprites/npc/
  soldier/
    idle/       - Standing still frames
    walk/       - Walking frames
    attack/     - Attacking frames
    pain/       - Getting hit frames
    death/      - Dying frames
  caco_demon/
    idle/
    walk/
    attack/
    pain/
    death/
  cyber_demon/
    idle/
    walk/
    attack/
    pain/
    death/
  lost_soul/
    idle/
    walk/
    attack/
    pain/
    death/
```

## Frame Naming

Each frame is a PNG file named by number:

```
idle/0.png
walk/0.png, 1.png, 2.png, 3.png
attack/0.png, 1.png
death/0.png, 1.png, 2.png, 3.png, 4.png, 5.png
```

The soldier uses special names for death frames: `POSSM0.png`, `POSSN0.png`, etc.

## How Frames Are Loaded

In `Loader.cs`, the `LoadFrames()` function loads all frames for one animation:

```csharp
LoadFrames(dir, "idle", new string[] { "0" })       // Loads idle/0.png
LoadFrames(dir, "walk", new string[] { "0", "1", "2", "3" })  // Loads walk/0.png through walk/3.png
```

## How Animation Works

Each frame lasts for a certain duration (the "rate"). The current frame is calculated from elapsed time:

```csharp
int index = (int)(timeElapsed / frameRate) % totalFrames;
```

Example: If `walkRate = 0.18` and there are 4 walk frames:
- 0.00s → frame 0
- 0.18s → frame 1
- 0.36s → frame 2
- 0.54s → frame 3
- 0.72s → frame 0 (loops)

Death animations do NOT loop — they clamp to the last frame.

## Enemy Stats

Each enemy type is defined in `Loader.LoadEnemyTypes()` with these stats:

| Stat | What It Means | Example (Soldier) |
|------|--------------|-------------------|
| Speed | How fast it moves (tiles/sec) | 0.8 |
| Health | How much damage it can take | 100 |
| Damage | How much it hurts the player | 10 |
| SizeTiles | How big it looks on screen | 0.7 |
| AttackRange | How close to attack from | 1.6 |

## Enemy States

Each enemy has a state that determines what it does:

```
idle → walk → attack → pain → death → dead
  ↑       ↑       ↓       ↓       ↓
  └───────┴───────┴───────┴───────┘
          (state machine)
```

| State | What Happens |
|-------|-------------|
| `idle` | Standing still, playing idle animation |
| `walk` | Moving toward the player, playing walk animation |
| `attack` | Playing attack animation, deals damage at the end |
| `pain` | Playing pain animation (got hit), then goes back to idle |
| `death` | Playing death animation, then stays as `dead` |
| `dead` | Finished dying, stays on last frame forever |

## Sprite Rendering

Sprites are drawn as billboards — flat images that always face the camera.

The rendering process:
1. Collect all sprites (enemies + pickups)
2. Project each sprite onto the screen using the camera matrix
3. Sort by distance (farthest first)
4. Draw each sprite pixel by pixel, skipping transparent pixels
5. Don't draw pixels where a wall is closer (occlusion)

## Pickup Sprites

Pickups are simpler — they don't have animations:

```
resources/sprites/pickups/
  health_pickup.png
  ammo_pickup.png
```

They are always drawn at 0.5 tiles size.
