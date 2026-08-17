using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Doom_Project
{
    // ================================================================
    //  ENEMY AI - spawning, behavior, and pickups
    //  by raven
    // ================================================================

    public class EnemyAI
    {
        private readonly Game _g;

        public EnemyAI(Game g)
        {
            _g = g;
        }

        // ================================================================
        //  LEVEL SETUP - place enemies and pickups on the map
        // ================================================================

        public void SetupLevel()
        {
            // spawn initial pickups
            _g._pickups.Add(new PickupItem { X = 10.5, Y = 3.5, Type = "health" });
            _g._pickups.Add(new PickupItem { X = 3.5, Y = 20.5, Type = "ammo" });
            _g._pickups.Add(new PickupItem { X = 30.5, Y = 20.5, Type = "health" });
            _g._pickups.Add(new PickupItem { X = 20.5, Y = 9.5, Type = "ammo" });

            // spawn wave 1
            _g._wave = 1;
            _g._score = 0;
            _g._enemiesKilled = 0;
            SpawnWave(1);
        }

        private void SpawnEnemy(string type, double x, double y)
        {
            // create a new enemy at the given position
            Enemy e = new Enemy();
            e.Type = _g._enemyTypes[type];  // look up stats (speed, hp, damage, etc)
            e.X = x;
            e.Y = y;
            e.Health = e.Type.Health;        // start with full health
            _g._enemies.Add(e);             // add to active enemies list
        }

        // ================================================================
        //  ENEMY AI - update each enemy's behavior every frame
        // ================================================================

        // enemy state machine:
        //   idle  -> walk    (when player is visible)
        //   walk  -> attack  (when player is in attack range)
        //   walk  -> idle    (when player goes out of sight)
        //   attack -> walk   (when attack animation finishes)
        //   pain  -> idle    (when pain animation finishes)
        //   any   -> pain    (when hit by player)
        //   any   -> death   (when health <= 0)
        //   death -> dead    (when death animation finishes)
        public void UpdateEnemies()
        {
            foreach (Enemy e in _g._enemies)
            {
                // skip enemies marked for removal
                if (e.Remove)
                {
                    continue;
                }
                // skip dead enemies, they just sit there as corpses
                if (e.State == "dead")
                {
                    continue;
                }

                // distance from this enemy to the player
                double dx = _g._playerX - e.X;
                double dy = _g._playerY - e.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                // PAIN STATE: stunned after being hit
                if (e.State == "pain")
                {
                    e.StateTime += _g._dt;
                    // pain animation done? go back to idle
                    if (e.StateTime >= e.Type.PainRate * e.Type.Pain.Count)
                    {
                        e.State = "idle";
                        e.StateTime = 0;
                        e.Frame = 0;
                    }
                    continue;
                }

                // ATTACK STATE: swinging at the player
                if (e.State == "attack")
                {
                    e.StateTime += _g._dt;
                    e.Frame = (int)(e.StateTime / e.Type.AttackRate);
                    // attack animation done?
                    if (e.Frame >= e.Type.Attack.Count)
                    {
                        // deal damage if player still in range (0.6 tile buffer for fairness)
                        if (dist < e.Type.AttackRange + 0.6)
                        {
                            _g._player.Damage(e.Type.Damage);
                        }
                        // go to cooldown before next attack
                        if (e.Type.AttackCooldown > 0)
                        {
                            e.State = "cooldown";
                            e.StateTime = 0;
                            e.Frame = 0;
                        }
                        else
                        {
                            e.State = "walk";
                            e.StateTime = 0;
                            e.Frame = 0;
                        }
                    }
                    continue;
                }

                // COOLDOWN STATE: pause between attacks
                if (e.State == "cooldown")
                {
                    e.StateTime += _g._dt;
                    if (e.StateTime >= e.Type.AttackCooldown)
                    {
                        e.State = "walk";
                        e.StateTime = 0;
                        e.Frame = 0;
                    }
                    continue;
                }

                // DEATH STATE: playing death animation
                if (e.State == "death")
                {
                    e.StateTime += _g._dt;
                    e.Frame = (int)(e.StateTime / e.Type.DeathRate);
                    // death animation done? stay on last frame
                    if (e.Frame >= e.Type.Death.Count)
                    {
                        e.Frame = e.Type.Death.Count - 1;
                        e.State = "dead";  // now fully dead
                    }
                    continue;
                }

                // IDLE/WALK STATE: decide what to do
                bool inRange = dist < e.Type.AttackRange;  // close enough to attack?
                bool canSee = dist < Game.EnemyAggroRange;  // within detection range?
                if (canSee)
                {
                    // also check line of sight, cant see through walls
                    // without this, enemies detect you through walls
                    canSee = _g._player.CanSeePoint(e.X, e.Y, _g._playerX, _g._playerY);
                }

                if (inRange && canSee)
                {
                    // player is close AND visible -> attack!
                    if (e.State != "attack")
                    {
                        e.State = "attack";
                        e.StateTime = 0;
                        e.Frame = 0;
                        Audio.PlayEffect(Path.Combine(_g._soundDir, "npc_attack.wav"));
                    }
                }
                else if (canSee)
                {
                    // player visible but not in range -> chase them
                    e.State = "walk";
                    double move = e.Type.Speed * _g._dt;
                    double nx = e.X + (dx / dist) * move;  // normalize direction * speed
                    double ny = e.Y + (dy / dist) * move;
                    // check each axis independently for wall sliding
                    if (!IsWall(nx, e.Y))
                    {
                        e.X = nx;
                    }
                    if (!IsWall(e.X, ny))
                    {
                        e.Y = ny;
                    }
                    e.StateTime += _g._dt;
                }
                else
                {
                    // player not visible, stand still
                    e.State = "idle";
                    e.StateTime += _g._dt;
                }
            }
        }

        // check if position (x,y) is inside a wall
        private bool IsWall(double x, double y)
        {
            int c = (int)x;
            int r = (int)y;
            if (r < 0 || r >= _g._rows || c < 0 || c >= _g._cols)
            {
                return true;  // outside map = wall
            }
            return _g._map[r, c] == '1';
        }

        // ================================================================
        //  DAMAGE - handle enemy taking hits
        // ================================================================

        public void DamageEnemy(Enemy e, int damage)
        {
            if (e.State == "dead")
            {
                return;
            }
            e.Health -= damage;  // reduce health
            Audio.PlayEffect(Path.Combine(_g._soundDir, "npc_pain.wav"));
            e.State = "pain";    // stun the enemy briefly
            e.StateTime = 0;
            e.Frame = 0;
            if (e.Health <= 0)
            {
                // enemy died, play death sound and start death animation
                Audio.PlayEffect(Path.Combine(_g._soundDir, "npc_death.wav"));
                e.State = "death";
                e.StateTime = 0;
                e.Frame = 0;
                AddScore(e.Type.Name);
            }
        }

        // ================================================================
        //  PICKUPS - check if player picks up health or ammo
        // ================================================================

        public void UpdatePickups()
        {
            foreach (PickupItem p in _g._pickups)
            {
                if (p.Taken)
                {
                    continue;  // already picked up
                }
                double dx = _g._playerX - p.X;
                double dy = _g._playerY - p.Y;
                double pickupRadius = 0.5;  // how close to pick up in tiles
                if (dx * dx + dy * dy < pickupRadius * pickupRadius)
                {
                    if (p.Type == "health")
                    {
                        _g._health = Math.Min(100, _g._health + 25);  // heal 25 HP, max 100
                    }
                    else
                    {
                        _g._ammo = 8;          // refill ammo to 8
                    }
                    p.Taken = true;  // mark as collected
                }
            }
        }

        // ================================================================
        //  WAVE SYSTEM - endless waves, each one harder than the last
        // ================================================================

        public void CheckWave()
        {
            // check if all enemies are dead
            bool allDead = true;
            foreach (Enemy e in _g._enemies)
            {
                if (e.State != "dead")
                {
                    allDead = false;
                    break;
                }
            }

            if (allDead && !_g._waveClear)
            {
                // wave just cleared - start countdown to next wave
                _g._waveClear = true;
                _g._waveDelay = 3.0;  // 3 seconds between waves
            }

            if (_g._waveClear)
            {
                _g._waveDelay -= _g._dt;
                if (_g._waveDelay <= 0)
                {
                    // spawn next wave
                    _g._wave++;
                    _g._waveClear = false;
                    SpawnWave(_g._wave);
                }
            }
        }

        private void SpawnWave(int wave)
        {
            _g._enemies.Clear();

            // base count increases each wave
            int soldiers = 4 + wave;
            int cacos = Math.Min(wave, 8);
            int souls = Math.Min(wave / 2, 6);
            int cybers = Math.Min(wave / 3, 4);

            // spawn at random open floor tiles
            Random rng = new Random();

            for (int i = 0; i < soldiers; i++)
            {
                var pos = FindOpenTile(rng);
                SpawnEnemy("soldier", pos.X, pos.Y);
            }
            for (int i = 0; i < cacos; i++)
            {
                var pos = FindOpenTile(rng);
                SpawnEnemy("caco", pos.X, pos.Y);
            }
            for (int i = 0; i < souls; i++)
            {
                var pos = FindOpenTile(rng);
                SpawnEnemy("soul", pos.X, pos.Y);
            }
            for (int i = 0; i < cybers; i++)
            {
                var pos = FindOpenTile(rng);
                SpawnEnemy("cyber", pos.X, pos.Y);
            }

            // spawn pickups each wave (resets the old ones)
            _g._pickups.Clear();
            int healthPickups = Math.Max(2, 4 - wave / 3);  // fewer health as waves go up
            int ammoPickups = 2 + wave / 2;                  // more ammo as waves go up
            for (int i = 0; i < healthPickups; i++)
            {
                var pos = FindOpenTile(rng);
                _g._pickups.Add(new PickupItem { X = pos.X, Y = pos.Y, Type = "health" });
            }
            for (int i = 0; i < ammoPickups; i++)
            {
                var pos = FindOpenTile(rng);
                _g._pickups.Add(new PickupItem { X = pos.X, Y = pos.Y, Type = "ammo" });
            }
        }

        private PointD FindOpenTile(Random rng)
        {
            // keep trying random positions until we find a floor tile
            // far enough from the player
            for (int attempt = 0; attempt < 100; attempt++)
            {
                int c = rng.Next(2, _g._cols - 2);
                int r = rng.Next(2, _g._rows - 2);
                if (_g._map[r, c] == '0')
                {
                    double dx = c + 0.5 - _g._playerX;
                    double dy = r + 0.5 - _g._playerY;
                    if (dx * dx + dy * dy > 25)  // at least 5 tiles from player
                    {
                        return new PointD { X = c + 0.5, Y = r + 0.5 };
                    }
                }
            }
            // fallback: just return somewhere
            return new PointD { X = 5.5, Y = 5.5 };
        }

        // simple helper to return x,y without making a whole class
        private struct PointD
        {
            public double X;
            public double Y;
        }

        // ================================================================
        //  SCORE - called when an enemy dies
        // ================================================================

        public void AddScore(string enemyType)
        {
            switch (enemyType)
            {
                case "soldier": _g._score += 100; break;
                case "caco":    _g._score += 200; break;
                case "soul":    _g._score += 150; break;
                case "cyber":   _g._score += 500; break;
            }
            _g._enemiesKilled++;
        }
    }
}
