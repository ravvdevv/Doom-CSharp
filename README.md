# Doom Project - Project Overview

A simple Doom-style first-person shooter built in C# WinForms.

## What It Does

- You walk around a 2D map rendered as a 3D first-person view (raycasting)
- You can shoot enemies with a shotgun
- Enemies walk toward you and attack when close
- Pick up health packs and ammo
- Kill all enemies to win

## How to Run

1. Open `Doom-Project.sln` in Visual Studio
2. Press F5 or Build > Start Debugging
3. The game starts in a maximized window

## Controls

| Key | Action |
|-----|--------|
| W / Up Arrow | Move forward |
| S / Down Arrow | Move backward |
| A | Strafe left |
| D | Strafe right |
| Left Arrow | Turn left |
| Right Arrow | Turn right |
| Mouse | Look around |
| Left Click | Shoot |
| M | Toggle minimap |
| Escape | Quit |

## Project Files

| File | What It Does |
|------|-------------|
| `Game.cs` | Main game form, game loop, coordinates everything |
| `Player.cs` | Player input, movement, shooting, health |
| `EnemyAI.cs` | Enemy behavior, spawning, pickups |
| `Renderer.cs` | Draws the 3D view (raycasting, walls, sprites) |
| `Hud.cs` | Draws the HUD (minimap, crosshair, health, ammo) |
| `Loader.cs` | Loads all files from disk (map, textures, sprites) |
| `Audio.cs` | Plays sound effects and music |
| `Data.cs` | Simple data classes (Enemy, EnemyType, etc.) |
| `Main.cs` | Main menu screen |

## Resources

All game assets are in the `resources/` folder:

```
resources/
  maps/map.txt          - The level layout (42x32 grid)
  textures/             - Wall textures (1.png - 5.png), sky, digits, overlays
  sprites/
    npc/                - Enemy sprites (soldier, caco_demon, cyber_demon, lost_soul)
    weapon/shotgun/     - Weapon animation frames (0.png - 5.png)
    pickups/            - Health and ammo pickup images
  sound/                - WAV sound effects and theme music
  images/               - Menu background images
```
