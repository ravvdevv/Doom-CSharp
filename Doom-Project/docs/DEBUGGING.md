# Quick Reference for Live Debugging

## If Something Breaks, Look Here

| Problem | File to Open | Method to Check |
|---------|-------------|-----------------|
| Player won't move | `Player.cs` | `Update()` |
| Player won't shoot | `Player.cs` | `Shoot()` |
| Enemies don't appear | `EnemyAI.cs` | `SetupLevel()` |
| Enemies don't move | `EnemyAI.cs` | `UpdateEnemies()` |
| Enemies don't take damage | `EnemyAI.cs` | `DamageEnemy()` |
| Screen is black | `Renderer.cs` | `RenderFrame()` |
| Walls look wrong | `Renderer.cs` | `DrawWalls()` |
| Sprites look wrong | `Renderer.cs` | `DrawSprites()` |
| HUD missing | `Hud.cs` | `DrawHud()` |
| No sound | `Audio.cs` | `PlayWav()` |
| Game won't start | `Game.cs` | `Game()` constructor |
| Game crashes on close | `Game.cs` | `OnFormClosing()` |

## Common Debugging Tasks

### "Enemy doesn't take damage"
1. Check `EnemyAI.DamageEnemy()` — is it being called?
2. Check `e.State == "dead"` — is the enemy already dead?
3. Check `e.Health` — is it going below 0?

### "Player can't move through a gap"
1. Check `map.txt` — is the gap actually walkable (`0`)?
2. Check `Player.Step()` — is `IsWall()` returning true?
3. Check `IsBlockedByEnemy()` — is an enemy in the way?

### "Wall texture looks wrong"
1. Check `resources/textures/1.png` through `5.png` — do they exist?
2. Check `Loader.LoadTextures()` — are they loading correctly?
3. Check `Renderer.DrawWalls()` — is `texX` calculated correctly?

### "Enemy is stuck or not pathfinding"
1. Check `EnemyAI.UpdateEnemies()` — is `seesPlayer` true?
2. Check `CanSeePoint()` — is there a wall blocking line of sight?
3. Check `IsWall()` — is the enemy's next position blocked?

### "No sound plays"
1. Check `resources/sound/` — do the WAV files exist?
2. Check `Audio.PlayWav()` — is `File.Exists()` returning true?
3. Check if another sound is still playing (only one sound at a time with `PlaySound`)

## Key Constants to Know

| Constant | Value | What It Controls |
|----------|-------|-----------------|
| `RenderW` | 640 | Screen width |
| `RenderH` | 360 | Screen height |
| `PlaneLen` | 0.66 | Camera field of view |
| `EnemyAggroRange` | 8 | How far enemies see |
| `EnemyHitRadius` | 0.4 | Enemy collision size |
| `FireCooldown` | 0.7 | Seconds between shots |
| `WeaponShootRange` | 20 | Shotgun range in tiles |

## How to Add a New Enemy Type

1. Create sprite folders in `resources/sprites/npc/your_enemy/`
2. Add idle, walk, attack, pain, death subfolders with PNG frames
3. In `Loader.cs`, add a new entry in `LoadEnemyTypes()`:
```csharp
types["your_enemy"] = LoadEnemyType(
    Path.Combine(npc, "your_enemy"),
    new string[] { "0" }, 0.15,     // idle frames + rate
    new string[] { "0", "1" }, 0.18, // walk frames + rate
    new string[] { "0" }, 0.25,      // attack frames + rate
    new string[] { "0" }, 0.35,      // pain frames + rate
    new string[] { "0", "1" }, 0.1,  // death frames + rate
    1.0, 100, 10, 0.8, 1.5);        // speed, hp, dmg, size, range
```
4. In `EnemyAI.cs`, add a spawn call in `SetupLevel()`:
```csharp
SpawnEnemy("your_enemy", 10.5, 10.5);
```

## How to Change Enemy Stats

Open `Loader.cs` and find `LoadEnemyTypes()`. Each enemy's stats are at the end of the `LoadEnemyType()` call:

```
speed, hp, dmg, size, range
```

For example, to make the soldier faster and weaker:
```csharp
// Before: 0.8, 100, 10, 0.7, 1.6
// After:  1.5,  50,  5, 0.7, 1.6
```
