# Doom Project Presentation Guide

## Overview
This is a Windows Forms-based Doom clone implemented in C#. It recreates the classic 1993 Doom gameplay using raycasting for 3D rendering, sprite-based enemies, and authentic Doom-inspired mechanics.

## Key Components

### 1. Main Menu (`Main.cs`)
- Animated background that cycles through multiple images
- Glowing "DOOM" title with fire-like effect
- Start/Quit buttons with hover effects
- Displays last run score after game over
- Smooth crossfade transitions between background images

### 2. Core Game Loop (`Game.cs`)
- Runs at 60 frames per second using a Windows Forms Timer
- Separates concerns: Update logic → Render → Display
- Manages game states: playing, game over, win
- Handles high score persistence (saved to file)
- Coordinates all game systems: player, enemies, rendering, HUD

### 3. Player System (`Player.cs`)
- **Input Handling**: WASD/arrow keys for movement, mouse for looking
- **Movement**: Collision detection with wall sliding (prevents getting stuck)
- **Shooting**: Shotgun with 7 pellets, spread, cooldown, and animation
- **Health System**: Damage feedback with blood screen flash
- **Mouse Look**: Recentering for smooth 360° viewing

### 4. Enemy System (`EnemyAI.cs`)
- Multiple enemy types with different behaviors
- Pathfinding and AI decision making
- Attack cooldowns and damage dealing
- Wave system that increases difficulty over time
- Health pickups and ammo drops

### 5. Rendering System (`Renderer.cs`)
- **Raycasting**: Core 3D rendering technique (like original Doom)
- **Sprite Drawing**: Enemies, pickups, and other 2D objects in 3D space
- **Floor/Ceiling**: Textured rendering with distance-based shading
- **Wall Textures**: Multiple textures mapped based on map data

### 6. HUD System (`Hud.cs`)
- Health and ammo displays
- Minimap toggle (M key)
- Crosshair
- Weapon animation display
- Help screen
- Blood flash effect when damaged

## Technical Highlights

### Raycasting Engine
The game uses a classic raycasting technique similar to the original Doom:
- Casts rays for each vertical screen column (640 rays)
- Finds wall intersections to create 3D perspective
- Applies texture mapping based on ray distance
- Renders floors and ceilings with simple texture scrolling

### Game Loop Architecture
```
Game Loop (60fps):
1. Calculate delta time (frame timing)
2. Update game logic:
   - Player movement & input
   - Enemy AI & behavior
   - Weapon animation & cooldowns
   - Wave system & spawning
3. Render frame:
   - Clear buffer
   - Draw sky
   - Raycast walls
   - Draw sprites (enemies, pickups)
   - Apply lighting/shading
4. Present to screen
```

### Collision System
- Player uses axis-separated collision checking (checks X then Y)
- Prevents getting stuck on corners while allowing diagonal movement
- Enemy collision uses simple radius-based distance checking
- Line-of-sight checks for shooting (prevents shooting through walls)

### Audio System
- Background music looping
- Sound effects for shooting, hits, pain, etc.
- Simple WAV file playback

## What Makes This Implementation Notable

### For a Beginner Project:
1. **Clear Separation of Concerns**: Each system (input, rendering, AI, etc.) is in its own class
2. **Well-Documented Code**: Extensive comments explain complex sections
3. **Proper Resource Management**: Images, sounds, and timers are properly disposed
4. **Configurable Constants**: Gameplay parameters are defined at the top for easy tuning
5. **WinForms Integration**: Proper use of Windows Forms for window management and rendering

### Technical Achievements:
1. **Working Raycaster**: Implemented from scratch without external 3D libraries
2. **Sprite System**: Properly handles enemy sprites in 3D space with occlusion
3. **Animation Systems**: Weapon, enemy, and background animations all work independently
4. **Persistence**: High scores saved between sessions
5. **Polish**: Screen effects (blood flash, glowing title), smooth animations, proper cleanup

## Suggested Presentation Talking Points

### 1. **Project Scope** (1 minute)
"This is a Doom clone built in C# using Windows Forms. It implements the core gameplay mechanics of the original 1993 Doom, including raycasting-based 3D rendering, enemy AI, weapon systems, and level progression."

### 2. **Technical Architecture** (2 minutes)
"We separated the game into distinct systems:
- Main menu handles UI and animation
- Game class manages the core loop and state
- Player handles input, movement, and shooting
- EnemyAI controls all enemy behaviors
- Renderer implements the raycasting engine
- Hud displays all on-screen information
- Audio handles music and sound effects
- Loader manages resource loading"

### 3. **The Raycasting Engine** (2 minutes - most impressive part)
"The heart of the game is our raycasting renderer, which creates 3D visuals from a 2D map:
- We cast a ray for each vertical screen column (640 rays total)
- Each ray travels until it hits a wall in our map
- We calculate distance to apply proper height and texture mapping
- Sprites (enemies, pickups) are drawn after walls with distance-based sorting
- This creates the illusion of 3D space using only 2D calculations"

### 4. **Gameplay Systems** (1.5 minutes)
"Our game features:
- Weapon system with shotgun spread, recoil animation, and cooldown
- Enemy AI with multiple types, pathfinding, and attack patterns
- Wave system that progressively increases difficulty
- Health and ammo pickups placed throughout the level
- Collision prevention that lets players slide along walls"

### 5. **Challenges Overcome** (1 minute)
"Key challenges we solved:
- Understanding and implementing raycasting mathematics
- Managing game state transitions (playing → game over → menu)
- Proper resource disposal to prevent memory leaks
- Synchronizing animations with game logic
- Creating satisfying weapon feedback and enemy reactions"

### 6. **Lessons Learned** (1 minute)
"Through this project we gained experience with:
- Game loop architecture and frame timing
- 2D to 3D projection techniques
- Object-oriented game design patterns
- Windows Forms graphics programming
- Resource management in .NET applications
- Collision detection and response systems"

### 7. **Demo Highlights** (During live demo)
- Show the animated title screen with glowing text
- Demonstrate smooth mouse looking and movement
- Show weapon firing with spread and animation
- Demonstrate enemy AI chasing and attacking
- Show the wave system in action (enemies getting harder)
- Display the HUD with health/ammo and minimap
- Show death screen with last run score

## Anticipated Questions & Answers

**Q: How does the raycasting work exactly?**
A: For each screen column, we cast a ray from the player's position at a specific angle. We step through the map grid until we hit a wall, calculate the distance, then draw a vertical slice of the wall texture scaled by that distance (closer = taller).

**Q: Why Windows Forms instead of Unity or another game engine?**
A: This was chosen to learn fundamental game programming concepts without relying on engine abstractions. It gives us full control over the rendering pipeline and game loop.

**Q: How did you implement enemy AI?**
A: Each enemy has a state machine (idle, chasing, attacking). They use simple line-of-sight checks to detect the player, then move toward them using basic pathfinding around obstacles.

**Q: Can you add more levels or modify the existing one?**
A: Yes! The map is loaded from a text file where different characters represent different things (walls, open space, etc.). To add levels, you'd create new map.txt files and modify the loader.

**Q: What would you improve if you had more time?**
A: We'd like to add: more enemy types, better level variety, saved game functionality, improved audio system, and possibly multiplayer capabilities.

## Final Tips for Defense
1. **Know your code**: Be able to point to specific files/lines when discussing features
2. **Focus on the raycaster**: This is the most technically impressive part
3. **Explain trade-offs**: Why you chose certain approaches over others
4. **Show enthusiasm**: Talk about what you learned and enjoyed building
5. **Keep it accessible**: Avoid jargon when explaining to non-technical audience
6. **Have backups**: Know where your exe is and test it beforehand
7. **Expect questions**: Prepare for "how does X work?" questions about specific systems

Good luck with your defense! You've built an impressive project that demonstrates solid game programming fundamentals.