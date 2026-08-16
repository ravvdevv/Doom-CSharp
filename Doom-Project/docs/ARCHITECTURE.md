# Architecture - How the Code Is Organized

## The Big Picture

The game is a `Form` (a window). Every frame, it:

1. Updates game logic (player moves, enemies think, pickups collected)
2. Renders the 3D view into a pixel buffer
3. Copies the buffer to a bitmap
4. Draws the bitmap + HUD onto the screen

This happens ~60 times per second.

## Module Map

```
Game.cs (coordinator)
  ├── Player.cs      (input, movement, weapon, health)
  ├── EnemyAI.cs     (enemy spawning, AI, pickups)
  ├── Renderer.cs    (raycasting, walls, sprites)
  ├── Hud.cs         (minimap, crosshair, health/ammo panels)
  ├── Loader.cs      (loads files from disk)
  ├── Audio.cs       (plays sounds)
  └── Data.cs        (simple data containers)
```

## How Modules Talk to Each Other

Every module gets a reference to `Game` in its constructor. It uses this to read and write game state.

Example:
- `Player.cs` reads `_g._playerX` to know where the player is
- `Player.cs` writes `_g._health` when the player takes damage
- `Renderer.cs` reads `_g._enemies` to know what sprites to draw

This means:
- If you delete a field from `Game.cs`, the module that uses it will break
- If you delete a module, `Game.cs` will break where it calls that module

## Game Loop Flow

```
GameLoop_Tick() [Game.cs]
  │
  ├── GameUpdate() [Game.cs]
  │     ├── Player.Update()         → moves the player
  │     ├── Player.UpdateWeapon()   → animates the weapon
  │     ├── EnemyAI.UpdateEnemies() → enemies walk/attack/die
  │     ├── EnemyAI.UpdatePickups() → player picks up items
  │     └── EnemyAI.AllEnemiesDead() → checks for win
  │
  ├── Renderer.RenderFrame() [Renderer.cs]
  │     ├── DrawSky()     → top half of screen
  │     ├── DrawFloor()   → bottom half of screen
  │     ├── DrawWalls()   → raycasting for walls
  │     └── DrawSprites() → enemies and pickups
  │
  ├── Renderer.Present() → copies pixels to bitmap
  │
  └── OnPaint() [Game.cs]
        ├── Draws the bitmap to screen
        ├── DrawMinimap()
        ├── DrawCrosshair()
        ├── DrawWeapon()
        ├── DrawHud()
        ├── DrawHelp()
        └── DrawBloodFlash()
```

## Shooting Flow

```
Player.OnMouseDown()
  └── Shoot()
        └── FireShotgun()
              └── CastHit() × 7 pellets
                    ├── Checks each enemy
                    ├── Uses dot product to check distance along shot
                    ├── Uses cross product to check if enemy is to the side
                    ├── Uses CanSeePoint() to check for walls
                    └── EnemyAI.DamageEnemy() if hit
```

## Enemy Attack Flow

```
EnemyAI.UpdateEnemies()
  ├── For each enemy:
  │     ├── If "pain" state → wait for animation to finish
  │     ├── If "attack" state → play animation, deal damage at end
  │     ├── If "death" state → play animation, then "dead"
  │     └── If "idle"/"walk":
  │           ├── Check distance to player
  │           ├── Check line of sight (CanSeePoint)
  │           ├── If close + can see → start attack
  │           ├── If can see → walk toward player
  │           └── Otherwise → idle
  └── Player.Damage() is called when attack completes
```

## Data Flow

All game data lives in `Game.cs` as `internal` fields. Every module reads/writes these directly:

| Field | Used By | What It Stores |
|-------|---------|---------------|
| `_playerX`, `_playerY` | Player, EnemyAI, Renderer | Player position on the map |
| `_angle` | Player, Renderer | Which direction the player is facing |
| `_health`, `_ammo` | Player, EnemyAI, Hud | Player stats |
| `_enemies` | EnemyAI, Renderer, Player | List of all enemies |
| `_pickups` | EnemyAI, Renderer | List of all pickups |
| `_map` | Player, EnemyAI, Renderer | The 2D grid of wall tiles |
| `_textures` | Renderer | Wall texture images |
| `_frameBuf` | Renderer | Raw pixel buffer for the 3D view |
| `_wallDist` | Renderer | Distance to wall per screen column |
| `_gameState` | Game, Player, Hud | "playing", "gameover", or "win" |
