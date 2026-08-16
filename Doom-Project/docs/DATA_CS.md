# Data.cs - Line by Line

This file holds all the simple data classes. These are just bags of variables - no logic, no methods.

## Lines 1-8: Namespace
```csharp
using System.Collections.Generic;    // Needed for List<> in EnemyType

namespace Doom_Project
{
    // ================================================================
    //  DATA CLASSES - simple containers, each stores one thing
    //  by raven
    // ================================================================
```

## Lines 10-21: Enemy class
```csharp
    // One enemy that is alive (or dead) in the level
    public class Enemy
    {
        public EnemyType Type;      // What kind of enemy (soldier, caco, etc.)
        public double X;            // Map position (horizontal)
        public double Y;            // Map position (vertical)
        public int Health;          // How much damage it can take before dying
        public string State;        // What it's doing right now
        public double StateTime;    // How many seconds it has been in this state
        public int Frame;           // Which animation frame it's on
        public bool Remove;         // True if it should be removed from the game
    }
```

- **Type**: Points to an EnemyType which has the stats and sprite frames
- **X, Y**: Decimal position on the map. (4.5, 5.5) means the center of tile at column 4, row 5
- **Health**: Starts at Type.Health (e.g. 100 for soldier). Goes down when shot.
- **State**: One of "idle", "walk", "attack", "pain", "death", "dead"
- **StateTime**: Timer that counts up. When it reaches a threshold, the state changes.
- **Frame**: Which animation frame to show (used for death animation which doesn't loop)
- **Remove**: If true, the enemy is skipped everywhere. Set by code when enemy is fully dead and cleaned up.

## Lines 23-46: EnemyType class
```csharp
    // The blueprint for one enemy kind (soldier, caco, etc.)
    public class EnemyType
    {
        // Animation frames for each state
        public List<SpriteFrame> Idle;     // Standing still frames
        public List<SpriteFrame> Walk;     // Walking frames
        public List<SpriteFrame> Attack;   // Attacking frames
        public List<SpriteFrame> Pain;     // Getting hit frames
        public List<SpriteFrame> Death;    // Dying frames

        // How long each frame lasts (in seconds)
        public double IdleRate;     // e.g. 0.15 = each frame shows for 0.15 seconds
        public double WalkRate;     // e.g. 0.18 = each walk frame shows for 0.18 seconds
        public double AttackRate;   // e.g. 0.25
        public double PainRate;     // e.g. 0.35
        public double DeathRate;    // e.g. 0.08

        // Stats
        public double Speed;        // How fast it moves (tiles per second)
        public int Health;          // How much damage it can take
        public int Damage;          // How much damage it deals to the player
        public double SizeTiles;    // How big it looks on screen (in tiles)
        public double AttackRange;  // How close it needs to be to attack
    }
```

This is a **blueprint** - shared by all enemies of the same type. If there are 7 soldiers, they all share one EnemyType but each has their own Enemy instance.

How animation works:
- `StateTime` counts up each frame
- Frame index = `(int)(StateTime / Rate) % frames.Count`
- Example: walkRate = 0.18, 4 walk frames
  - 0.00s -> frame 0
  - 0.18s -> frame 1
  - 0.36s -> frame 2
  - 0.54s -> frame 3
  - 0.72s -> frame 0 (loops)

Enemy stats comparison:

| Type    | Speed | HP  | Dmg | Size | Range |
|---------|-------|-----|-----|------|-------|
| soldier | 0.8   | 100 | 10  | 0.7  | 1.6   |
| caco    | 1.3   | 150 | 15  | 0.9  | 1.7   |
| soul    | 2.2   | 50  | 8   | 0.8  | 1.2   |
| cyber   | 0.6   | 400 | 25  | 1.5  | 2.0   |

## Lines 48-55: PickupItem class
```csharp
    public class PickupItem
    {
        public double X;            // Map position (horizontal)
        public double Y;            // Map position (vertical)
        public string Type;         // "health" or "ammo"
        public bool Taken;          // True if the player already picked it up
    }
```
Simple: position, type, and whether it's been collected. Health gives +25 HP (max 100), ammo gives +8 shells (max 99).

## Lines 57-66: DrawItem class
```csharp
    public class DrawItem
    {
        public double X;            // Map position (horizontal)
        public double Y;            // Map position (vertical)
        public SpriteFrame Frame;   // The image to draw
        public double SizeTiles;    // How big to draw it (in tiles)
        public double Depth;        // How far from the camera (for sorting)
        public int ScreenX;         // Where on screen horizontally (in pixels)
    }
```
This is a temporary object used only during rendering. It bundles up everything the renderer needs to draw one sprite. Created in DrawSprites(), used for depth sorting, then thrown away.

## Lines 68-75: SpriteFrame class
```csharp
    public class SpriteFrame
    {
        public byte[] Pixels;       // Raw pixel colors (BGRA format, 4 bytes per pixel)
        public int Width;           // Image width in pixels
        public int Height;          // Image height in pixels
        public double Aspect;       // Width divided by Height (used to keep proportions)
    }
```
One frame of an animation. `Pixels` is a flat byte array: `[B, G, R, A, B, G, R, A, ...]`. To get pixel at (x, y): index = `(y * Width + x) * 4`.

## Lines 77-83: WallTexture class
```csharp
    public class WallTexture
    {
        public byte[] Pixels;       // Raw pixel colors (BGRA format, 4 bytes per pixel)
        public int Width;           // Image width in pixels
        public int Height;          // Image height in pixels
    }
```
Same as SpriteFrame but without Aspect ratio. Wall textures are always resized to 128x128 when loaded.
