# Doom Project - C# Windows Forms Game

A clone of the classic 1993 Doom game built using C# and Windows Forms. This project demonstrates fundamental game programming concepts including raycasting rendering, game loops, collision detection, and AI systems.

## 🎮 Features

- **Raycasting Engine** - 3D rendering using classic Doom technique
- **Weapon System** - Shotgun with spread, animation, and cooldown
- **Enemy AI** - Multiple enemy types with state machines and pathfinding
- **Wave System** - Endless waves that increase in difficulty
- **Animated Title Screen** - Glowing "DOOM" text with cycling backgrounds
- **HUD System** - Health, ammo, minimap, and weapon display
- **Audio System** - Background music and sound effects
- **Persistent High Scores** - Saved between sessions

## 📁 Project Structure

```
Doom-Project/
├── Main.cs          # Title screen and menu system
├── Game.cs          # Core game loop and state management
├── Player.cs        # Input, movement, shooting mechanics
├── EnemyAI.cs       # Enemy behaviors and wave system
├── Renderer.cs      # Raycasting 3D rendering engine
├── Hud.cs           # On-screen displays and effects
├── Audio.cs         # Sound and music management
├── Loader.cs        # Resource loading system
├── Data.cs          # Game data structures
└── Program.cs       # Application entry point
```

## 🛠️ Technical Highlights

- **Architecture**: Clean separation of concerns with each system in its own class
- **Rendering**: Software rasterizer using raycasting (like original Doom)
- **Collisions**: Axis-separated collision checking with wall sliding
- **AI**: State machine-based enemy behaviors with multiple states
- **Input**: Keyboard/mouse handling with mouse look and recentering
- **Resources**: Proper loading, management, and disposal of game assets
- **Game Loop**: Fixed timestep with delta time capping for stability

## 📚 Learning Outcomes

Through this project, you'll gain experience with:
- Game loop architecture and frame timing
- 2D to 3D projection techniques (raycasting)
- Object-oriented game design patterns
- Windows Forms graphics programming
- Resource management in .NET applications
- Collision detection and response systems
- Basic AI state machines and pathfinding
- Audio playback and mixing

## 🚀 Getting Started

1. Open `Doom-Project.sln` in Visual Studio
2. Press F5 to build and run
3. Use WASD/Arrow keys to move, mouse to look, left click to shoot
4. Press M to toggle minimap
5. Survive as many waves as possible!

## 📖 Documentation

- [Presentation Guide](docs/PRESENTATION_GUIDE.md) - Detailed explanation for defense/presentations
- [Quick Reference](docs/QUICK_REFERENCE.md) - Key points and talking points
- [Architecture Diagram](docs/doom-architecture.excalidraw) - Visual system overview

---

*Note: This was built as a learning project to understand game development fundamentals without relying on external game engines.*