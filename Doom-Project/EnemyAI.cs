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
            // spawn all enemies
            // format: SpawnEnemy("type", x, y)
            SpawnEnemy("soldier", 4.5, 5.5);
            SpawnEnemy("soldier", 20.5, 5.5);
            SpawnEnemy("soldier", 31.5, 10.5);
            SpawnEnemy("soldier", 15.5, 16.5);
            SpawnEnemy("soldier", 20.5, 25.5);
            SpawnEnemy("soldier", 8.5, 29.5);
            SpawnEnemy("soldier", 35.5, 30.5);
            SpawnEnemy("caco", 13.5, 9.5);
            SpawnEnemy("caco", 28.5, 21.5);
            SpawnEnemy("soul", 5.5, 18.5);
            SpawnEnemy("soul", 24.5, 13.5);
            SpawnEnemy("cyber", 31.5, 26.5);

            // spawn pickups
            _g._pickups.Add(new PickupItem { X = 10.5, Y = 3.5, Type = "health" });
            _g._pickups.Add(new PickupItem { X = 3.5, Y = 20.5, Type = "ammo" });
            _g._pickups.Add(new PickupItem { X = 30.5, Y = 20.5, Type = "health" });
            _g._pickups.Add(new PickupItem { X = 20.5, Y = 9.5, Type = "ammo" });
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
                        e.State = "walk";  // go back to chasing
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
                        Audio.PlayWav(Path.Combine(_g._soundDir, "npc_attack.wav"), false);
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
            Audio.PlayWav(Path.Combine(_g._soundDir, "npc_pain.wav"), false);
            e.State = "pain";    // stun the enemy briefly
            e.StateTime = 0;
            e.Frame = 0;
            if (e.Health <= 0)
            {
                // enemy died, play death sound and start death animation
                Audio.PlayWav(Path.Combine(_g._soundDir, "npc_death.wav"), false);
                e.State = "death";
                e.StateTime = 0;
                e.Frame = 0;
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
                        _g._ammo = Math.Min(99, _g._ammo + 8);       // get 8 ammo, max 99
                    }
                    p.Taken = true;  // mark as collected
                }
            }
        }

        // ================================================================
        //  WIN CHECK - are all enemies dead?
        // ================================================================

        public bool AllEnemiesDead()
        {
            foreach (Enemy e in _g._enemies)
            {
                if (e.State != "dead")
                {
                    return false;
                }
            }
            return true;
        }
    }
}
