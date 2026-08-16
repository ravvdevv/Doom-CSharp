# Game.cs - Line by Line

The boss file. Coordinates everything — creates the game, runs the loop, handles input, and connects all the modules.

## The Big Picture

```
Main.cs                    → shows the menu form
Game.cs                    → this file, the game window
│
├── Audio.cs               → plays sounds (PlayWav)
├── Data.cs                → simple classes (Enemy, EnemyType, etc.)
├── Loader.cs              → loads files from disk
├── Player.cs              → input, movement, shooting
├── EnemyAI.cs             → enemy logic, level setup, win condition
├── Renderer.cs            → 3D raycasting and sprite drawing
└── Hud.cs                 → minimap, health bar, weapon, help text
```

## Lines 1-16: Imports
```csharp
using System;
using System.Collections.Generic;   // for List<>, Dictionary<>
using System.Drawing;               // for Bitmap, Color, Graphics
using System.Drawing.Imaging;       // for BitmapData, PixelFormat
using System.Windows.Forms;         // for Form, Timer, Keys

namespace Doom_Project
{
    public class Game : Form
    {
```

We inherit from Form (WinForms window). All the fields below live on the Game instance so every module can access them.

## Lines 18-44: Important Fields
```csharp
        // ---- the window ----
        internal Timer _timer;
        internal Bitmap _frame;
        internal byte[] _frameBuf;
        internal Graphics _gr;

        // ---- player position ----
        internal double _playerX;
        internal double _playerY;
        internal double _angle;

        // ---- map ----
        internal char[,] _map;
        internal int _rows;
        internal int _cols;
        internal DateTime _lastTime;

        // ---- renderer stuff ----
        internal double[] _wallDist;
        internal double _cos, _sin;
        internal double _planeX, _planeY;

        // ---- textures ----
        internal Dictionary<char, Loader.WallTexture> _textures;
        internal byte[] _skyPixels;
        internal int _skyW, _skyH;

        // ---- enemy and item lists ----
        internal List<Enemy> _enemies;
        internal List<PickupItem> _pickups;
```

`internal` = visible to all files in this project but not outside. This is how all modules share the same data without passing it as parameters everywhere.

## Lines 46-66: Constants
```csharp
        // ---- game settings ----
        public const int RenderW = 640;
        public const int RenderH = 360;
        public const double MoveSpeed = 2.5;
        public const double RotSpeed = 2.5;
        public const double PlaneLen = 0.66;
```

- `RenderW/H` = internal render resolution (smaller = faster)
- `MoveSpeed` = tiles per second
- `RotSpeed` = radians per second
- `PlaneLen` = camera field of view (0.66 is classic Doom value)

## Lines 68-71: State Flags
```csharp
        internal bool _shooting;
        internal bool _run;

        // ---- constructor ----
        public Game()
```

## Lines 73-144: Constructor
```csharp
            // ---- window setup ----
            Text = "DOOM - raven edition";
            ClientSize = new Size(800, 600);
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = false;
            KeyPreview = true;
            Cursor.Hide();
            WindowState = FormWindowState.Normal;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            // ---- rendering bitmap ----
            _frame = new Bitmap(RenderW, RenderH, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            _gr = Graphics.FromImage(_frame);
            _frameBuf = new byte[RenderW * RenderH * 4];
            _wallDist = new double[RenderW];

            // ---- load textures ----
            string resPath = System.IO.Path.Combine(Application.StartupPath, "resources");
            _textures = new Dictionary<char, Loader.WallTexture>();
            Loader.LoadTextures(System.IO.Path.Combine(resPath, "textures"), _textures);
            _skyPixels = Loader.LoadSky(System.IO.Path.Combine(resPath, "sky", "sky.png"), out _skyW, out _skyH);

            // ---- load map ----
            _map = Loader.LoadMap(System.IO.Path.Combine(resPath, "maps", "map.txt"), out _rows, out _cols);

            // ---- player start position ----
            _playerX = 2.5;
            _playerY = 2.5;
            _angle = 0.0;

            // ---- spawn enemies and pickups ----
            _enemies = new List<Enemy>();
            _pickups = new List<PickupItem>();
            EnemyAI.SetupLevel(this);

            // ---- camera values ----
            _cos = Math.Cos(_angle);
            _sin = Math.Sin(_angle);
            _planeX = -_sin * PlaneLen;
            _planeY = _cos * PlaneLen;

            // ---- play theme music ----
            Audio.PlayWav(System.IO.Path.Combine(resPath, "sound", "theme.wav"), true);

            // ---- keyboard handlers ----
            KeyDown += Game_KeyDown;
            KeyUp += Game_KeyUp;

            // ---- 60fps timer ----
            _run = true;
            _lastTime = DateTime.Now;
            _timer = new Timer();
            _timer.Interval = 16;
            _timer.Tick += GameLoop_Tick;
            _timer.Start();
        }
```

Key things:
- `Cursor.Hide()` = no cursor in game window (mouse look mode)
- `_frameBuf` = raw byte array for fast pixel drawing
- `_wallDist` = tracks wall distance per screen column (used for sprite occlusion)
- `_map` = 2D char array from map.txt
- `Audio.PlayWav` with `true` = loop the theme music
- `_timer.Interval = 16` = roughly 60 FPS (16ms per frame)

## Lines 146-177: Game Loop (the heartbeat)
```csharp
        // ================================================================
        //  GAME LOOP - runs every 16ms (~60fps)
        //  calculate dt → move things → draw → repeat
        // ================================================================
        private void GameLoop_Tick(object sender, EventArgs e)
        {
            if (!_run)
            {
                return;
            }
            DateTime now = DateTime.Now;
            double dt = (now - _lastTime).TotalSeconds;
            _lastTime = now;
            if (dt > 0.05)
            {
                dt = 0.05;
            }
            if (_shooting)
            {
                dt *= 0.6;
            }

            GameUpdate(dt);

            Renderer.RenderFrame();

            Renderer.Present();

            Invalidate();
        }

        private void GameUpdate(double dt)
        {
            Player.Update(this, dt);
            EnemyAI.UpdateAll(this, dt);
            EnemyAI.CheckPickups(this);
            EnemyAI.CheckWinCondition(this);
        }
```

**Step by step:**
1. `_lastTime` tracks when the last frame happened. `dt` = time since last frame.
2. Cap `dt` at 0.05 so lag spikes don't cause huge jumps.
3. Slow everything down by 0.4x when shooting (makes it feel punchy).
4. `GameUpdate` = move player, move enemies, check pickups, check win.
5. `Renderer.RenderFrame` = draw the 3D view into `_frameBuf`.
6. `Renderer.Present` = copy `_frameBuf` to the Bitmap so WinForms can show it.
7. `Invalidate` = tell WinForms to call `OnPaint` on the next frame.

## Lines 179-214: Keyboard
```csharp
        private readonly HashSet<Keys> _keys = new HashSet<Keys>();

        private void Game_KeyDown(object sender, KeyEventArgs e)
        {
            _keys.Add(e.KeyCode);
            if (e.KeyCode == Keys.Escape)
            {
                Application.Exit();
            }
            if (e.KeyCode == Keys.F1)
            {
                Hud._showHelp = !Hud._showHelp;
            }
        }

        private void Game_KeyUp(object sender, KeyEventArgs e)
        {
            _keys.Remove(e.KeyCode);
        }

        internal bool IsDown(Keys k)
        {
            return _keys.Contains(k);
        }
```

Uses a `HashSet<Keys>` to track which keys are held down. This is better than checking `KeyEventArgs` directly because it handles key repeat and multi-key combos.
- Escape = quit
- F1 = toggle help screen

## Lines 216-249: OnPaint (draws the rendered frame to screen)
```csharp
        protected override void OnPaint(PaintEventArgs e)
        {
            if (_frame != null)
            {
                e.Graphics.DrawImage(_frame, 0, 0, ClientSize.Width, ClientSize.Height);
            }
            Hud.DrawHUD(this, e.Graphics);
            Hud.DrawMinimap(e.Graphics);
            Hud.DrawCrosshair(e.Graphics);
            Hud.DrawWeapon(e.Graphics);
            Hud.DrawPanels(e.Graphics);
            Hud.DrawBloodFlash(e.Graphics);
            if (Player._painFlash > 0)
            {
                Hud.DrawPain(e.Graphics);
            }
            if (Player._invulnTimer > 0)
            {
                Hud.DrawInvuln(e.Graphics);
            }
            if (Hud._showHelp)
            {
                Hud.DrawHelp(e.Graphics);
            }
            if (_shooting)
            {
                Hud.DrawGunFlash(e.Graphics);
            }
        }
```

`OnPaint` is called by WinForms whenever the window needs to redraw. We draw the rendered 3D frame (scaled to window size) then layer the HUD on top.

## Lines 251-262: OnKeyDown (for key repeat)
```csharp
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F1)
            {
                Hud._showHelp = !Hud._showHelp;
            }
            base.OnKeyDown(e);
        }
```

## Lines 264-277: Cleanup
```csharp
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _run = false;
            _timer.Stop();
            _timer.Dispose();
            Audio.StopAllSounds();
            _gr?.Dispose();
            _frame?.Dispose();
            base.OnFormClosing(e);
        }
```

Stops everything when the window closes. Releases resources so we don't leak memory.
