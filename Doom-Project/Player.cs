using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Doom_Project
{
    // ================================================================
    //  PLAYER - handles input, movement, weapon, and health
    //  by raven
    // ================================================================

    public class Player
    {
        private readonly Game _g;  // reference to the game, needed to access all the fields

        public Player(Game g)
        {
            _g = g;
        }

        // ================================================================
        //  INPUT - keyboard and mouse
        // ================================================================

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            _g._pressed.Add(e.KeyCode);   // remember this key is held down
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult result = MessageBox.Show(
                    "Are you sure you want to quit?",
                    "Quit Game",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    _g.Close();
                }
            }
            if (e.KeyCode == Keys.M)
            {
                _g._showMinimap = !_g._showMinimap;  // toggle minimap
            }
        }

        public void OnKeyUp(object sender, KeyEventArgs e)
        {
            _g._pressed.Remove(e.KeyCode);  // key released, stop moving that direction
        }

        public void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Shoot();  // left click = shoot
            }
        }

        public void OnMouseMove(object sender, MouseEventArgs e)
        {
            // dont look around if game is over
            if (_g._gameState != "playing")
            {
                return;
            }
            // dont look around if window isnt focused
            if (!_g.Focused)
            {
                return;
            }
            Point now = Cursor.Position;

            // how far mouse moved since last frame, times sensitivity
            // 0.0035 = sensitivity, higher = faster look
            double mouseDelta = (now.X - _g._lastMouse.X) * 0.0035;
            _g._angle += mouseDelta;  // rotate the view
            RecenterMouse();           // snap mouse back to center
        }

        public void OnShown(object sender, EventArgs e)
        {
            Cursor.Hide();       // hide the mouse cursor
            RecenterMouse();     // center the mouse
        }

        public void OnDeactivate(object sender, EventArgs e)
        {
            // window lost focus, save mouse position so we dont jump when coming back
            _g._lastMouse = Cursor.Position;
        }

        public void RecenterMouse()
        {
            // move mouse to center of window
            // without this, mouse drifts off screen and look stops working
            _g._lastMouse = _g.PointToScreen(new Point(_g.ClientSize.Width / 2, _g.ClientSize.Height / 2));
            Cursor.Position = _g._lastMouse;
        }

        // ================================================================
        //  MOVEMENT - update player position each frame
        // ================================================================

        public void Update()
        {
            double speed = 3.5 * _g._dt;   // 3.5 tiles per second, scaled by frame time
            double turn = 2.6 * _g._dt;    // 2.6 radians per second for arrow key turning

            // move forward (W or Up arrow)
            bool wForward = _g._pressed.Contains(Keys.W);
            bool upForward = _g._pressed.Contains(Keys.Up);
            if (wForward || upForward)
            {
                double moveX = Math.Cos(_g._angle) * speed;
                double moveY = Math.Sin(_g._angle) * speed;
                Step(moveX, moveY);  // try to move, slide along walls if blocked
            }

            // move backward (S or Down arrow)
            bool sBack = _g._pressed.Contains(Keys.S);
            bool downBack = _g._pressed.Contains(Keys.Down);
            if (sBack || downBack)
            {
                double moveX = -Math.Cos(_g._angle) * speed;
                double moveY = -Math.Sin(_g._angle) * speed;
                Step(moveX, moveY);
            }

            // strafe left (A key)
            if (_g._pressed.Contains(Keys.A))
            {
                double leftAngle = _g._angle - Math.PI / 2;  // 90 degrees left
                double moveX = Math.Cos(leftAngle) * speed;
                double moveY = Math.Sin(leftAngle) * speed;
                Step(moveX, moveY);
            }

            // strafe right (D key)
            if (_g._pressed.Contains(Keys.D))
            {
                double rightAngle = _g._angle + Math.PI / 2;  // 90 degrees right
                double moveX = Math.Cos(rightAngle) * speed;
                double moveY = Math.Sin(rightAngle) * speed;
                Step(moveX, moveY);
            }

            // turn with arrow keys
            if (_g._pressed.Contains(Keys.Left))
            {
                _g._angle -= turn;
            }
            if (_g._pressed.Contains(Keys.Right))
            {
                _g._angle += turn;
            }
        }

        // try to move by (dx, dy), slide along walls if blocked
        // this checks X and Y axes separately so you dont get stuck on corners
        private void Step(double dx, double dy)
        {
            const double radius = 0.2;  // player hitbox radius in tiles
            double nx = _g._playerX + dx;
            double ny = _g._playerY + dy;

            // try X axis first
            bool xBlockedLeft = IsWall(nx - radius, _g._playerY);
            bool xBlockedRight = IsWall(nx + radius, _g._playerY);
            bool xBlockedEnemy = IsBlockedByEnemy(nx, _g._playerY);
            bool xClear = !xBlockedLeft && !xBlockedRight && !xBlockedEnemy;
            if (xClear)
            {
                _g._playerX = nx;
            }

            // try Y axis
            bool yBlockedBottom = IsWall(_g._playerX, ny - radius);
            bool yBlockedTop = IsWall(_g._playerX, ny + radius);
            bool yBlockedEnemy = IsBlockedByEnemy(_g._playerX, ny);
            bool yClear = !yBlockedBottom && !yBlockedTop && !yBlockedEnemy;
            if (yClear)
            {
                _g._playerY = ny;
            }
        }

        // check if position (x,y) is inside a wall on the map
        private bool IsWall(double x, double y)
        {
            int c = (int)x;  // map column
            int r = (int)y;  // map row
            if (r < 0 || r >= _g._rows || c < 0 || c >= _g._cols)
            {
                return true;  // outside map = solid wall, cant walk off the edge
            }
            return _g._map[r, c] == '1';  // '1' = wall, anything else = walkable
        }

        // check if position (x,y) overlaps with any living enemy
        private bool IsBlockedByEnemy(double x, double y)
        {
            double collisionDist = 0.35;  // how close is "too close" in tiles
            foreach (Enemy e in _g._enemies)
            {
                if (e.State == "death" || e.State == "dead")
                {
                    continue;  // dead enemies dont block you
                }
                double dx = x - e.X;
                double dy = y - e.Y;
                double distanceSquared = dx * dx + dy * dy;  // distance squared is faster than actual distance
                if (distanceSquared < collisionDist * collisionDist)
                {
                    return true;
                }
            }
            return false;
        }

        // ================================================================
        //  WEAPON - shooting and damage
        // ================================================================

        public void UpdateWeapon()
        {
            // count down fire cooldown (cant shoot again until it hits 0)
            if (_g._fireCooldown > 0)
            {
                _g._fireCooldown -= _g._dt;
            }

            // animate weapon if currently shooting
            if (_g._shooting)
            {
                _g._weaponTimer += _g._dt;
                _g._weaponIndex = 1 + (int)(_g._weaponTimer / Game.WeaponFrameTime);
                // past last frame? stop shooting
                if (_g._weaponIndex >= _g._weaponFrames.Count)
                {
                    _g._weaponIndex = 0;  // back to idle frame
                    _g._shooting = false;
                }
            }
        }

        private void Shoot()
        {
            // can't shoot if game is over
            if (_g._gameState != "playing")
            {
                return;
            }
            // can't shoot if already shooting or on cooldown or no weapon loaded
            if (_g._shooting)
            {
                return;
            }
            if (_g._fireCooldown > 0)
            {
                return;
            }
            if (_g._weaponFrames.Count == 0)
            {
                return;
            }
            // no ammo check - infinite ammo
            _g._shooting = true;           // start weapon animation
            _g._fireCooldown = Game.FireCooldown;  // set cooldown (0.7 seconds)
            _g._weaponTimer = 0;
            _g._weaponIndex = 1;           // show first firing frame
            Audio.PlayEffect(Path.Combine(_g._soundDir, "shotgun.wav"));
            FireShotgun();                 // fire 7 pellets
        }

        private void FireShotgun()
        {
            const int pellets = 7;      // number of pellets per shot
            const double spread = 0.12; // how wide the spread is in radians
            Random rng = _g._rng;
            for (int i = 0; i < pellets; i++)
            {
                // each pellet gets a slightly random angle
                double pelletAngle = _g._angle + (rng.NextDouble() - 0.5) * spread;
                CastHit(pelletAngle);
            }
        }

        // check if a single pellet hits any enemy
        private void CastHit(double ang)
        {
            // direction vector for this pellet
            double dirX = Math.Cos(ang);
            double dirY = Math.Sin(ang);

            double bestDistance = Game.WeaponShootRange;  // max range 20 tiles
            Enemy hitEnemy = null;

            // check every enemy to see if this pellet hits
            foreach (Enemy e in _g._enemies)
            {
                if (e.Remove)
                {
                    continue;
                }
                if (e.State == "dead")
                {
                    continue;
                }

                // vector from player to enemy
                double toEnemyX = e.X - _g._playerX;
                double toEnemyY = e.Y - _g._playerY;

                // DOT PRODUCT: how far along the shot direction is the enemy?
                // positive = enemy is in front of us, negative = behind us
                double distanceAlongShot = toEnemyX * dirX + toEnemyY * dirY;
                if (distanceAlongShot <= 0)
                {
                    continue;  // enemy is behind us
                }
                if (distanceAlongShot >= bestDistance)
                {
                    continue;  // farther than our closest hit so far
                }

                // CROSS PRODUCT: how far to the side of the shot is the enemy?
                // if this is larger than enemy hit radius, we missed
                double distanceToSide = toEnemyX * dirY - toEnemyY * dirX;
                double absSide = Math.Abs(distanceToSide);
                if (absSide >= Game.EnemyHitRadius)
                {
                    continue;  // missed, too far to the side
                }

                // is there a wall between us and the enemy?
                // without this, you can shoot through walls
                if (!CanSeePoint(_g._playerX, _g._playerY, e.X, e.Y))
                {
                    continue;
                }

                // this enemy is the closest one hit so far, remember it
                bestDistance = distanceAlongShot;
                hitEnemy = e;
            }

            if (hitEnemy != null)
            {
                // deal 10 damage to the hit enemy
                _g._enemyAI.DamageEnemy(hitEnemy, 10);
            }
        }

        // line of sight check: is there a clear path between two points?
        // walks along the line in small steps and checks each map cell
        // without this, shooting and enemy detection ignores walls
        public bool CanSeePoint(double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            double dist = Math.Sqrt(dx * dx + dy * dy);

            // number of steps to check, more steps = more accurate
            // 10 checks per tile is enough for gameplay
            int steps = (int)(dist * 10);
            if (steps < 1)
            {
                steps = 1;
            }

            for (int i = 1; i <= steps; i++)
            {
                // walk along the line from start to end
                double t = (double)i / steps;
                int c = (int)(x1 + dx * t);
                int r = (int)(y1 + dy * t);

                // outside map = can't see through it
                if (r < 0 || r >= _g._rows || c < 0 || c >= _g._cols)
                {
                    return false;
                }

                // hit a wall = can't see through it
                if (_g._map[r, c] != '0')
                {
                    return false;
                }
            }
            return true;  // no walls found, clear line of sight
        }

        // ================================================================
        //  HEALTH - damage the player
        // ================================================================

        public void Damage(int damage)
        {
            // can't take damage if game is already over
            if (_g._gameState != "playing")
            {
                return;
            }
            _g._health -= damage;       // reduce health
            _g._bloodAlpha = 0.9;       // show blood flash
            Audio.PlayEffect(Path.Combine(_g._soundDir, "player_pain.wav"));
            if (_g._health <= 0)
            {
                _g._health = 0;
                _g._gameState = "gameover";    // game over!
                _g._stateTimer = 4.0;          // show game over screen for 4 seconds
                if (_g.IsNewHighScore())
                {
                    _g._highScore = _g._score;
                    _g._highWave = _g._wave;
                }
                _g.SaveHighScore();
            }
        }
    }
}
