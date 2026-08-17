using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Doom_Project
{
    // ================================================================
    //  DEBUGGING MAP - asa tan-awa kung mo break ang code atay ra HAHAHHAHA 
    // ================================================================
    //
    //  Game loop runs 60x per second, each tick does this:
    //
    //    Game.cs: GameLoop_Tick()
    //      -> GameUpdate()
    //           -> Player.Update()         [Player.cs]   - WASD/arrows movement
    //           -> Player.UpdateWeapon()   [Player.cs]   - weapon animation + cooldown
    //           -> EnemyAI.UpdateEnemies() [EnemyAI.cs]  - enemy AI + attacks
    //           -> EnemyAI.UpdatePickups() [EnemyAI.cs]  - health/ammo pickup
    //           -> EnemyAI.AllEnemiesDead()[EnemyAI.cs]  - win check
    //      -> Renderer.RenderFrame()      [Renderer.cs]  - draw sky/floor/walls/sprites
    //      -> Renderer.Present()          [Renderer.cs]  - copy pixels to bitmap
    //      -> Invalidate()                - tells WinForms to repaint
    //      -> OnPaint()                   [Game.cs]      - draws bitmap + HUD
    //           -> Hud.DrawMinimap()      [Hud.cs]
    //           -> Hud.DrawCrosshair()    [Hud.cs]
    //           -> Hud.DrawWeapon()       [Hud.cs]
    //           -> Hud.DrawHud()          [Hud.cs]
    //           -> Hud.DrawHelp()         [Hud.cs]
    //           -> Hud.DrawBloodFlash()   [Hud.cs]
    //
    //  Shooting:
    //    Player.OnMouseDown() -> Shoot() -> FireShotgun() -> CastHit()
    //      -> EnemyAI.DamageEnemy()
    //
    //  Enemy attacks:
    //    EnemyAI.UpdateEnemies() -> Player.Damage()
    //
    //  Data classes (Enemy, EnemyType, etc.): Data.cs
    //  File loading (textures, sprites, map): Loader.cs
    //  Sound (PlayWav, StopAllSounds):        Audio.cs
    //  version 0.1 - raven
    //
    // ================================================================

    public class Game : Form
    {
        // ================================================================
        //  CONSTANTS - numbers that never change
        // ================================================================

        internal const int RenderW = 640;               // screen width in pixels
        internal const int RenderH = 360;               // screen height in pixels
        internal const double PlaneLen = 0.66;          // camera FOV, higher = wider view
        internal const double WeaponFrameTime = 0.05;   // seconds between weapon anim frames
        internal const double WeaponShootRange = 20;    // how far shotgun pellets can hit (tiles)
        internal const double FireCooldown = 0.7;       // seconds between shots
        internal const double EnemyAggroRange = 50;     // how far enemies can see you (tiles)
        internal const double EnemyHitRadius = 0.4;     // enemy hitbox width (tiles)

        // ================================================================
        //  FIELDS - stuff the game keeps track of
        // ================================================================

        // map data (loaded from map.txt)
        internal readonly char[,] _map;             // the grid of walls and floors
        internal readonly int _rows;                // map height
        internal readonly int _cols;                // map width

        // player position and direction
        internal double _playerX = 1.5;             // horizontal position (tiles)
        internal double _playerY = 1.5;             // vertical position (tiles)
        internal double _angle;                     // facing direction in radians

        // wall textures stored by their map character ('1' through '5')
        internal readonly Dictionary<char, WallTexture> _textures = new Dictionary<char, WallTexture>();

        // sky image
        internal byte[] _skyPixels;                 // raw pixel data of sky.png
        internal int _skyW = 1;                     // sky width
        internal int _skyH = 1;                     // sky height

        // pixel buffer - we draw into this each frame then copy to bitmap
        internal readonly byte[] _frameBuf = new byte[RenderW * RenderH * 4];
        internal readonly Bitmap _frame = new Bitmap(RenderW, RenderH, PixelFormat.Format32bppArgb);

        // nearest wall distance for each screen column
        // important ni for sprite occlusion (enemies behind walls get hidden)
        internal readonly double[] _wallDist = new double[RenderW];

        // weapon animation
        internal readonly List<Image> _weaponFrames = new List<Image>();  // shotgun frames 0-5
        internal int _weaponIndex;                   // current frame
        internal bool _shooting;                     // true while shooting
        internal double _weaponTimer;                // seconds since started shooting
        internal double _fireCooldown;               // seconds before you can shoot again

        // random number generator (for shotgun spread)
        internal readonly Random _rng = new Random();

        // sound folder path
        internal readonly string _soundDir = Path.Combine(Application.StartupPath, "resources", "sound");

        // keyboard and mouse input
        internal readonly HashSet<Keys> _pressed = new HashSet<Keys>();  // keys currently held down
        internal readonly Timer _loop = new Timer();        // fires GameLoop_Tick 60x/sec
        internal readonly Stopwatch _clock = new Stopwatch(); // measures time between frames
        internal double _dt = 1.0 / 60.0;  // time since last frame in seconds, very important
        internal bool _showMinimap;                  // toggle with M key
        internal Point _lastMouse;                   // last mouse position for recentering

        // player health and ammo
        internal int _health = 100;                  // 0 = game over
        internal int _ammo = 32;                     // 0 = can't shoot

        // blood flash overlay
        internal double _bloodAlpha;           // red overlay opacity (0=invisible, 1=full red)

        // wave system - endless, gets harder each wave
        internal int _wave = 1;               // current wave number
        internal int _score = 0;              // total score
        internal int _enemiesKilled = 0;      // enemies killed this wave
        internal double _waveDelay = 0;       // seconds before next wave spawns
        internal bool _waveClear = false;     // true when all enemies dead, waiting for next wave

        // game state - controls whether we're still playing
        internal string _gameState = "playing";  // "playing", "gameover", or "win"
        internal double _stateTimer = 4.0;       // seconds before closing after game over/win
        internal volatile bool _running = true;  // false = shutting down, skip all rendering

        // all enemies currently in the level
        internal readonly List<Enemy> _enemies = new List<Enemy>();

        // enemy type definitions (soldier, caco, soul, cyber)
        internal readonly Dictionary<string, EnemyType> _enemyTypes = new Dictionary<string, EnemyType>();

        // pickups on the map (health pack and ammo)
        internal readonly List<PickupItem> _pickups = new List<PickupItem>();
        internal SpriteFrame _pickupHealth;       // health pack sprite
        internal SpriteFrame _pickupAmmo;         // ammo pack sprite

        // HUD images (digits 0-9 for health/ammo display)
        internal readonly List<Image> _digitImages = new List<Image>();
        internal Image _bloodScreen;              // red overlay for damage flash
        internal Image _gameOverImg;              // game over screen
        internal Image _winImg;                   // you win screen

        // ================================================================
        //  MODULES - the main parts of the game
        // ================================================================

        internal Player _player;          // handles input, movement, shooting
        internal EnemyAI _enemyAI;        // handles enemy AI and pickups
        internal Renderer _renderer;      // handles raycasting and drawing
        internal Hud _hud;               // handles HUD (minimap, crosshair, etc)

        // ================================================================
        //  CONSTRUCTOR - sets everything up when the game starts
        // ================================================================

        public Game()
        {
            Text = "DOOM - raven edition";
            Icon = new Icon(Path.Combine(Application.StartupPath, "resources", "images", "logo.ico"));
            WindowState = FormWindowState.Maximized;
            DoubleBuffered = true;
            BackColor = Color.Black;
            KeyPreview = true;

            // where all game resources are
            string res = Path.Combine(Application.StartupPath, "resources");

            // load the map layout from text file
            _map = Loader.LoadMap(Path.Combine(res, "maps", "map.txt"), out _rows, out _cols);

            // load wall textures (1.png through 5.png)
            Loader.LoadTextures(Path.Combine(res, "textures"), _textures);

            // load sky background
            _skyPixels = Loader.LoadSky(Path.Combine(res, "textures", "sky.png"), out _skyW, out _skyH);

            // load weapon frames (shotgun animation: 0.png through 5.png)
            string weaponDir = Path.Combine(res, "sprites", "weapon", "shotgun");
            for (int i = 0; i <= 5; i++)
            {
                string weaponPath = Path.Combine(weaponDir, i + ".png");
                if (File.Exists(weaponPath))
                {
                    _weaponFrames.Add(Image.FromFile(weaponPath));
                }
            }

            // load enemy types and their sprites
            _enemyTypes = Loader.LoadEnemyTypes();

            // load pickup sprites
            _pickupHealth = Loader.LoadSpriteFrame(Path.Combine(res, "sprites", "pickups", "health_pickup.png"));
            _pickupAmmo = Loader.LoadSpriteFrame(Path.Combine(res, "sprites", "pickups", "ammo_pickup.png"));

            // load digit images for HUD (0.png through 9.png)
            string digitsDir = Path.Combine(res, "textures", "digits");
            for (int i = 0; i <= 9; i++)
            {
                string digitPath = Path.Combine(digitsDir, i + ".png");
                if (File.Exists(digitPath))
                {
                    _digitImages.Add(Image.FromFile(digitPath));
                }
            }

            // load overlay images
            _bloodScreen = Loader.LoadImageOrNull(Path.Combine(res, "textures", "blood_screen.png"));
            _gameOverImg = Loader.LoadImageOrNull(Path.Combine(res, "textures", "game_over.png"));
            _winImg = Loader.LoadImageOrNull(Path.Combine(res, "textures", "win.png"));

            // start theme music (loops forever, mciSendString keeps it alive even when effects play)
            Audio.PlayMusic(Path.Combine(_soundDir, "theme.wav"));

            // create all the modules
            _player = new Player(this);
            _enemyAI = new EnemyAI(this);
            _renderer = new Renderer(this);
            _hud = new Hud(this);

            // setup the level - spawn enemies and pickups
            _enemyAI.SetupLevel();

            // wire up input handlers - without these, you can't move or shoot
            KeyDown += _player.OnKeyDown;
            KeyUp += _player.OnKeyUp;
            MouseDown += _player.OnMouseDown;
            MouseMove += _player.OnMouseMove;
            Shown += _player.OnShown;
            Deactivate += _player.OnDeactivate;

            // start game loop (runs about 60 times per second)
            _clock.Start();
            _loop.Interval = 16;
            _loop.Tick += GameLoop_Tick;
            _loop.Start();
        }

        // ================================================================
        //  GAME LOOP - runs every frame (60fps)
        // ================================================================

        private void GameLoop_Tick(object sender, EventArgs e)
        {
            if (!_running)
            {
                return;
            }
            // calculate time since last frame
            _dt = _clock.Elapsed.TotalSeconds;
            _clock.Restart();

            // cap delta time so physics dont explode if we lag
            // without this, player teleports if game lags for a sec
            if (_dt > 0.05)
            {
                _dt = 0.05;
            }

            // update game logic
            GameUpdate();

            // render frame (draw sky/floor/walls/sprites into _frameBuf)
            _renderer.RenderFrame();

            // copy pixel buffer to bitmap
            _renderer.Present();

            // redraw screen (triggers OnPaint)
            Invalidate();
        }

        // ================================================================
        //  GAME UPDATE - updates game state every frame
        // ================================================================

        private void GameUpdate()
        {
            // if game over or won, countdown then close
            if (_gameState != "playing")
            {
                _stateTimer -= _dt;
                if (_stateTimer <= 0)
                {
                    Close();
                }
                return;
            }

            // update player movement and weapon
            _player.Update();
            _player.UpdateWeapon();

            // blood flash fades over time (1.3 = fade speed)
            // without this, screen stays red forever after getting hit
            if (_bloodAlpha > 0)
            {
                _bloodAlpha -= _dt * 1.3;
                if (_bloodAlpha < 0)
                {
                    _bloodAlpha = 0;
                }
            }

            // update all enemies (AI, movement, attacks)
            _enemyAI.UpdateEnemies();
            // update pickups
            _enemyAI.UpdatePickups();
            // check wave status (spawn next wave when all dead)
            _enemyAI.CheckWave();
        }

        // ================================================================
        //  PAINT - draws everything to the screen
        // ================================================================

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            // NearestNeighbor = crispy pixels, no blur when scaling up
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            // draw the 3D scene (the bitmap we rendered into)
            g.DrawImage(_frame, new Rectangle(0, 0, ClientSize.Width, ClientSize.Height));

            // draw minimap if toggled on with M key
            if (_showMinimap)
            {
                _hud.DrawMinimap(g);
            }

            // draw HUD elements only while playing
            if (_gameState == "playing")
            {
                _hud.DrawCrosshair(g);
                if (_weaponFrames.Count > 0)
                {
                    _hud.DrawWeapon(g);
                }
                _hud.DrawHud(g);
                _hud.DrawHelp(g);
            }

            // blood flash (red overlay when damaged)
            _hud.DrawBloodFlash(g);

            // game over / win screens (drawn on top of everything)
            if (_gameState == "gameover" && _gameOverImg != null)
            {
                g.DrawImage(_gameOverImg, new Rectangle(0, 0, ClientSize.Width, ClientSize.Height));
            }
            else if (_gameState == "win" && _winImg != null)
            {
                g.DrawImage(_winImg, new Rectangle(0, 0, ClientSize.Width, ClientSize.Height));
            }
        }

        // ================================================================
        //  CLEANUP - dispose everything when game closes
        // ================================================================

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _running = false;            // stop game loop immediately
            _loop.Stop();
            _loop.Dispose();
            _clock.Stop();
            Cursor.Show();              // show cursor again
            Audio.StopAllSounds();      // stop music

            _frame.Dispose();
            foreach (Image frame in _weaponFrames)
            {
                frame.Dispose();
            }
            _weaponFrames.Clear();
            foreach (Image digit in _digitImages)
            {
                digit.Dispose();
            }
            _digitImages.Clear();
            if (_bloodScreen != null)
            {
                _bloodScreen.Dispose();
            }
            if (_gameOverImg != null)
            {
                _gameOverImg.Dispose();
            }
            if (_winImg != null)
            {
                _winImg.Dispose();
            }
            base.OnFormClosing(e);
        }
    }
}

// Tribute to rene batterbonia