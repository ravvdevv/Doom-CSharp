# Player.cs - Line by Line

Handles all player logic: movement, mouse look, shooting, health, and damage.

## Lines 1-8: Imports
```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
```

## Lines 10-30: Fields
```csharp
namespace Doom_Project
{
    public static class Player
    {
        // ---- mouse look ----
        internal static int _lastMouseX;
        internal static int _recenterCount;
        internal static bool _firstMouse;
        internal static bool _hasRawInput;

        // ---- shooting ----
        internal static int _ammo;
        internal static int _shotFrame;
        internal static double _shotTimer;
        internal static int _fireFrame;
        internal static bool _firePressed;
        internal static double _weaponBob;

        // ---- health ----
        internal static int _hp;
        internal static double _painFlash;
        internal static double _invulnTimer;

        // ---- weapon animation ----
        internal static int _weapY;

        // ---- mouse buttons (from Main.cs) ----
        internal static bool _leftPressed;
        internal static bool _rightPressed;
```

All `static` because there's only one player. These fields are shared across all player functions.

## Lines 32-35: Initialize
```csharp
        public static void Init(Game g)
        {
            _hp = 100;
            _ammo = 50;
```

Called from `Game` constructor. Sets starting health and ammo.

## Lines 37-86: Update (called every frame)
```csharp
        public static void Update(Game g, double dt)
        {
            _recenterCount = 0;
            _firstMouse = true;
            HandleMouseLook(g);
            HandleMovement(g, dt);
            HandleShooting(g, dt);
            UpdateShooting(g, dt);
            UpdateWeaponBob(dt);
            UpdateWeaponY(dt);
            UpdatePainFlash(dt);
            UpdateInvuln(dt);
            UpdateFireFrame(dt);
        }
```

Each frame: recenter mouse → handle movement → handle shooting → update animation timers.

## Lines 88-146: Mouse Look
```csharp
        private static void HandleMouseLook(Game g)
        {
            if (_firstMouse)
            {
                _lastMouseX = Cursor.Position.X;
                _firstMouse = false;
                return;
            }
            int dx = Cursor.Position.X - _lastMouseX;
            _lastMouseX = Cursor.Position.X;
            if (dx == 0)
            {
                return;
            }

            g._angle += dx * 0.002;
            g._cos = Math.Cos(g._angle);
            g._sin = Math.Sin(g._angle);
            g._planeX = -g._sin * Game.PlaneLen;
            g._planeY = g._cos * Game.PlaneLen;

            if (_recenterCount < 3)
            {
                Cursor.Position = new Point(
                    g.Left + g.ClientSize.Width / 2,
                    g.Top + g.ClientSize.Height / 2
                );
                _recenterCount++;
            }
        }
```

The mouse cursor is hidden. Each frame, we measure how far the mouse moved from center (`dx`), rotate the view by `dx * 0.002` radians, then snap the cursor back to center.

`_recenterCount < 3` prevents a feedback loop where recentering triggers another mouse move event.

## Lines 148-230: Movement
```csharp
        private static void HandleMovement(Game g, double dt)
        {
            // ---- forward / backward ----
            if (g.IsDown(Keys.W))
            {
                double nx = g._playerX + g._cos * Game.MoveSpeed * dt;
                double ny = g._playerY + g._sin * Game.MoveSpeed * dt;
                if (!IsWall(g, nx, g._playerY))
                {
                    g._playerX = nx;
                }
                if (!IsWall(g, g._playerX, ny))
                {
                    g._playerY = ny;
                }
            }
            if (g.IsDown(Keys.S))
            {
                double nx = g._playerX - g._cos * Game.MoveSpeed * dt;
                double ny = g._playerY - g._sin * Game.MoveSpeed * dt;
                if (!IsWall(g, nx, g._playerY))
                {
                    g._playerX = nx;
                }
                if (!IsWall(g, g._playerX, ny))
                {
                    g._playerY = ny;
                }
            }

            // ---- strafe left / right ----
            if (g.IsDown(Keys.A))
            {
                double nx = g._playerX + g._sin * Game.MoveSpeed * dt;
                double ny = g._playerY - g._cos * Game.MoveSpeed * dt;
                if (!IsWall(g, nx, g._playerY))
                {
                    g._playerX = nx;
                }
                if (!IsWall(g, g._playerX, ny))
                {
                    g._playerY = ny;
                }
            }
            if (g.IsDown(Keys.D))
            {
                double nx = g._playerX - g._sin * Game.MoveSpeed * dt;
                double ny = g._playerY + g._cos * Game.MoveSpeed * dt;
                if (!IsWall(g, nx, g._playerY))
                {
                    g._playerX = nx;
                }
                if (!IsWall(g, g._playerX, ny))
                {
                    g._playerY = ny;
                }
            }
        }

        private static bool IsWall(Game g, double x, double y)
        {
            int mx = (int)x;
            int my = (int)y;
            if (mx < 0 || my < 0 || mx >= g._cols || my >= g._rows)
            {
                return true;
            }
            return g._map[my, mx] == '1';
        }
```

**WASD movement.** Each direction checks X and Y separately so you can slide along walls.

- W = forward (along camera direction)
- S = backward
- A = strafe left (perpendicular to camera)
- D = strafe right

`IsWall` converts decimal position to grid cell and checks if it's a wall.

## Lines 232-251: Shooting
```csharp
        private static void HandleShooting(Game g, double dt)
        {
            bool wantShoot = _leftPressed || g.IsDown(Keys.Space) || g.IsDown(Keys.L);

            if (wantShoot && _ammo > 0 && _shotTimer <= 0)
            {
                _shooting = true;
                _shotFrame = 0;
                _shotTimer = 0.4;
                _ammo--;
                Shoot(g);
                Audio.PlayWav(System.IO.Path.Combine(
                    Application.StartupPath, "resources", "sound", "shotgun.wav"), false);
            }

            if (!wantShoot)
            {
                _shooting = false;
            }
        }
```

Three ways to shoot: left mouse, Space, or L key. Must have ammo and timer expired.

## Lines 253-335: Damage + Invulnerability
```csharp
        public static void TakeDamage(Game g, int amount)
        {
            if (_invulnTimer > 0)
            {
                return;
            }
            _hp -= amount;
            _painFlash = 0.3;
            Audio.PlayWav(System.IO.Path.Combine(
                Application.StartupPath, "resources", "sound", "player_pain.wav"), false);
            if (_hp <= 0)
            {
                _hp = 0;
                g._run = false;
                g._timer.Stop();
                Audio.StopAllSounds();
                MessageBox.Show("YOU DIED!", "Game Over",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Restart();
            }
        }

        public static void AddInvuln(double seconds)
        {
            _invulnTimer = seconds;
        }
```

- Pain flash = red screen tint when hit
- At 0 HP: stop game, show "YOU DIED!" dialog, restart
- Invulnerability = temporary immunity (from invulnerability pickup)

## Lines 337-380: Animation Timers
```csharp
        private static void UpdatePainFlash(double dt)
        {
            if (_painFlash > 0)
            {
                _painFlash -= dt * 3;
                if (_painFlash < 0)
                {
                    _painFlash = 0;
                }
            }
        }

        private static void UpdateInvuln(double dt)
        {
            if (_invulnTimer > 0)
            {
                _invulnTimer -= dt;
                if (_invulnTimer < 0)
                {
                    _invulnTimer = 0;
                }
            }
        }

        private static void UpdateFireFrame(double dt)
        {
            if (_fireFrame > 0)
            {
                _fireFrame -= dt * 10;
                if (_fireFrame < 0)
                {
                    _fireFrame = 0;
                }
            }
        }
```

Simple countdown timers. Each one decrements by `dt * multiplier` until it reaches 0.
