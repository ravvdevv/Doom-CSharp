# EnemyAI.cs - Line by Line

Sets up the level (places enemies and pickups) and runs the enemy AI every frame.

## Lines 1-10: Imports
```csharp
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
```

## Lines 12-20: The State Machine

Every enemy has a state that determines what it does:

```
          ┌──────────────────────────────────────┐
          │              idle                     │
          │     standing still, looking around    │
          └──┬──────────┬──────────┬─────────────┘
             │          │          │
      sees player   timeout    timeout
             │          │          │
             ▼          ▼          ▼
        ┌─────────┐ ┌─────────┐ ┌─────────┐
        │  walk   │ │ attack  │ │  pain   │
        │ toward  │ │ play    │ │ got     │
        │ player  │ │ anim    │ │ hit     │
        └────┬────┘ └────┬────┘ └────┬────┘
             │           │           │
      close enough   anim done   anim done
             │           │           │
             ▼           ▼           ▼
        ┌─────────┐    idle      idle
        │ attack  │
        └────┬────┘
             │
      anim done
             │
             ▼
            idle

    pain ──→ death ──→ dead (stays forever)
```

States:
- **idle**: Standing still, plays idle animation. If it sees the player → walk. Random timeout → attack.
- **walk**: Moving toward the player. When close enough → attack.
- **attack**: Plays attack animation. Deals damage at frame 1. When done → idle.
- **pain**: Plays pain animation (got hit). When done → idle.
- **death**: Plays death animation. When done → dead.
- **dead**: Stays on last frame forever. Gets removed later.

## Lines 22-50: SetupLevel (places everything)
```csharp
        public static void SetupLevel(Game g)
        {
            g._enemies.Clear();
            g._pickups.Clear();

            Dictionary<string, EnemyType> types = Loader.LoadEnemyTypes();

            // ---- place enemies ----
            g._enemies.Add(MakeEnemy(types["soldier"], 10.5, 4.5));
            g._enemies.Add(MakeEnemy(types["soldier"], 14.5, 6.5));
            g._enemies.Add(MakeEnemy(types["soldier"], 20.5, 15.5));
            g._enemies.Add(MakeEnemy(types["caco"], 25.5, 8.5));
            g._enemies.Add(MakeEnemy(types["caco"], 30.5, 12.5));
            g._enemies.Add(MakeEnemy(types["soul"], 12.5, 20.5));
            g._enemies.Add(MakeEnemy(types["soul"], 18.5, 22.5));
            g._enemies.Add(MakeEnemy(types["soul"], 22.5, 28.5));
            g._enemies.Add(MakeEnemy(types["cyber"], 35.5, 20.5));
            g._enemies.Add(MakeEnemy(types["soldier"], 32.5, 14.5));
            g._enemies.Add(MakeEnemy(types["caco"], 38.5, 25.5));
            g._enemies.Add(MakeEnemy(types["soul"], 28.5, 30.5));

            // ---- place pickups ----
            g._pickups.Add(new PickupItem { X = 8.5, Y = 8.5, Type = "health" });
            g._pickups.Add(new PickupItem { X = 15.5, Y = 18.5, Type = "ammo" });
            g._pickups.Add(new PickupItem { X = 30.5, Y = 28.5, Type = "health" });
            g._pickups.Add(new PickupItem { X = 35.5, Y = 15.5, Type = "ammo" });
        }

        private static Enemy MakeEnemy(EnemyType type, double x, double y)
        {
            return new Enemy
            {
                Type = type,
                X = x,
                Y = y,
                Health = type.Health,
                State = "idle",
                StateTime = 0,
                Frame = 0,
                Remove = false
            };
        }
```

12 enemies + 4 pickups. `MakeEnemy` creates an enemy with full health and idle state.

## Lines 52-178: AI Update
```csharp
        public static void UpdateAll(Game g, double dt)
        {
            foreach (Enemy e in g._enemies)
            {
                if (e.Remove)
                {
                    continue;
                }

                double dx = g._playerX - e.X;
                double dy = g._playerY - e.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                bool canSee = CanSeePoint(g, e.X, e.Y, g._playerX, g._playerY);

                switch (e.State)
                {
                    case "idle":
                        UpdateIdle(g, e, dt, dist, canSee);
                        break;
                    case "walk":
                        UpdateWalk(g, e, dt, dist, canSee);
                        break;
                    case "attack":
                        UpdateAttack(g, e, dt);
                        break;
                    case "pain":
                        UpdatePain(e, dt);
                        break;
                    case "death":
                        UpdateDeath(e, dt);
                        break;
                }
                e.StateTime += dt;
            }
        }
```

For each alive enemy: calculate distance to player, check line of sight, then run the state-specific update function.

### CanSeePoint (line of sight check)
```csharp
        private static bool CanSeePoint(Game g, double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            int steps = (int)(dist * 4);
            if (steps < 1)
            {
                steps = 1;
            }
            double sx = dx / steps;
            double sy = dy / steps;
            double cx = x1;
            double cy = y1;
            for (int i = 0; i < steps; i++)
            {
                cx += sx;
                cy += sy;
                int mx = (int)cx;
                int my = (int)cy;
                if (mx < 0 || my < 0 || mx >= g._cols || my >= g._rows)
                {
                    return false;
                }
                if (g._map[my, mx] == '1')
                {
                    return false;
                }
            }
            return true;
        }
```

Shoots an invisible ray from the enemy to the player. Steps along the ray checking each grid cell. If any cell is a wall, the enemy can't see the player. Step count = distance × 4 for enough precision.

### UpdateIdle
```csharp
        private static void UpdateIdle(Game g, Enemy e, double dt, double dist, bool canSee)
        {
            if (canSee && dist < 15.0)
            {
                e.State = "walk";
                e.StateTime = 0;
                return;
            }
            if (e.StateTime > 3.0 && _rng.NextDouble() < dt * 0.5)
            {
                e.State = "attack";
                e.StateTime = 0;
                Audio.PlayWav(System.IO.Path.Combine(
                    Application.StartupPath, "resources", "sound", "npc_attack.wav"), false);
            }
        }
```

If enemy sees player within 15 tiles → walk. After 3 seconds idle → random chance to attack (even if can't see player — they get bored and shoot anyway).

### UpdateWalk
```csharp
        private static void UpdateWalk(Game g, Enemy e, double dt, double dist, bool canSee)
        {
            if (!canSee || dist > 18.0)
            {
                e.State = "idle";
                e.StateTime = 0;
                return;
            }
            double dx = g._playerX - e.X;
            double dy = g._playerY - e.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len > 0.01)
            {
                double nx = e.X + dx / len * e.Type.Speed * dt;
                double ny = e.Y + dy / len * e.Type.Speed * dt;
                if (!IsWall(g, nx, e.Y))
                {
                    e.X = nx;
                }
                if (!IsWall(g, e.X, ny))
                {
                    e.Y = ny;
                }
            }
            if (dist < e.Type.AttackRange)
            {
                e.State = "attack";
                e.StateTime = 0;
                Audio.PlayWav(System.IO.Path.Combine(
                    Application.StartupPath, "resources", "sound", "npc_attack.wav"), false);
            }
        }
```

Moves toward the player at enemy speed. If loses sight → idle. If gets close enough → attack.

### UpdateAttack
```csharp
        private static void UpdateAttack(Game g, Enemy e, double dt)
        {
            if (e.StateTime > 0.3 && e.Frame == 0)
            {
                double dx = g._playerX - e.X;
                double dy = g._playerY - e.Y;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist < e.Type.AttackRange + 1.0)
                {
                    Player.TakeDamage(g, e.Type.Damage);
                }
                e.Frame = 1;
            }
            double attackDur = e.Type.AttackRate * e.Type.Attack.Count;
            if (e.StateTime >= attackDur)
            {
                e.State = "idle";
                e.StateTime = 0;
                e.Frame = 0;
            }
        }
```

Deals damage at frame 1 (0.3s into the animation). Damage only lands if player is within range + 1.0 tile.

### UpdatePain / UpdateDeath
```csharp
        private static void UpdatePain(Enemy e, double dt)
        {
            double painDur = e.Type.PainRate * e.Type.Pain.Count;
            if (e.StateTime >= painDur)
            {
                e.State = "idle";
                e.StateTime = 0;
                e.Frame = 0;
            }
        }

        private static void UpdateDeath(Enemy e, double dt)
        {
            double deathDur = e.Type.DeathRate * e.Type.Death.Count;
            if (e.StateTime >= deathDur)
            {
                e.State = "dead";
                e.StateTime = 0;
                e.Frame = e.Type.Death.Count - 1;
            }
            else
            {
                e.Frame = Math.Min(
                    (int)(e.StateTime / e.Type.DeathRate),
                    e.Type.Death.Count - 1
                );
            }
        }
```

- Pain: waits for animation to finish → idle
- Death: plays animation → stays on last frame ("dead")

## Lines 180-225: Player Shooting
```csharp
        public static void Shoot(Game g)
        {
            double dirX = Math.Cos(g._angle);
            double dirY = Math.Sin(g._angle);
            double closestDist = double.MaxValue;
            Enemy closestEnemy = null;
            foreach (Enemy e in g._enemies)
            {
                if (e.Remove || e.State == "death" || e.State == "dead")
                {
                    continue;
                }
                double dx = e.X - g._playerX;
                double dy = e.Y - g._playerY;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                double dot = dx * dirX + dy * dirY;
                if (dot < 0)
                {
                    continue;
                }
                double perpDist = Math.Abs(dx * dirY - dy * dirX);
                double apparentSize = e.Type.SizeTiles / dist;
                if (perpDist < apparentSize && dist < closestDist)
                {
                    if (CanSeePoint(g, g._playerX, g._playerY, e.X, e.Y))
                    {
                        closestDist = dist;
                        closestEnemy = e;
                    }
                }
            }
            if (closestEnemy != null)
            {
                int damage = 25;
                if (closestDist > 3.0)
                {
                    damage = 10;
                }
                closestEnemy.Health -= damage;
                Audio.PlayWav(System.IO.Path.Combine(
                    Application.StartupPath, "resources", "sound", "npc_pain.wav"), false);
                if (closestEnemy.Health <= 0)
                {
                    closestEnemy.State = "death";
                    closestEnemy.StateTime = 0;
                    closestEnemy.Frame = 0;
                    Audio.PlayWav(System.IO.Path.Combine(
                        Application.StartupPath, "resources", "sound", "npc_death.wav"), false);
                }
                else
                {
                    closestEnemy.State = "pain";
                    closestEnemy.StateTime = 0;
                    closestEnemy.Frame = 0;
                }
            }
        }
```

**How shooting targets enemies:**
1. Cast a ray from the player in the look direction
2. For each alive enemy: check if it's roughly in the crosshair
3. `dot < 0` = enemy is behind the player
4. `perpDist < apparentSize` = enemy is within the crosshair width
5. Pick the closest valid enemy (must have line of sight)
6. Deal 25 damage if close, 10 if far
7. If health ≤ 0 → death. Otherwise → pain.

## Lines 227-260: Pickups and Win
```csharp
        public static void CheckPickups(Game g)
        {
            foreach (PickupItem p in g._pickups)
            {
                if (p.Taken)
                {
                    continue;
                }
                double dx = p.X - g._playerX;
                double dy = p.Y - g._playerY;
                if (dx * dx + dy * dy < 0.5 * 0.5)
                {
                    if (p.Type == "health" && Player._hp < 100)
                    {
                        Player._hp = Math.Min(100, Player._hp + 25);
                        p.Taken = true;
                        Audio.PlayWav(System.IO.Path.Combine(
                            Application.StartupPath, "resources", "sound", "pickup.wav"), false);
                    }
                    else if (p.Type == "ammo" && Player._ammo < 99)
                    {
                        Player._ammo = Math.Min(99, Player._ammo + 8);
                        p.Taken = true;
                        Audio.PlayWav(System.IO.Path.Combine(
                            Application.StartupPath, "resources", "sound", "pickup.wav"), false);
                    }
                }
            }
        }

        public static void CheckWinCondition(Game g)
        {
            int alive = 0;
            foreach (Enemy e in g._enemies)
            {
                if (!e.Remove && e.State != "death" && e.State != "dead")
                {
                    alive++;
                }
            }
            if (alive == 0)
            {
                g._run = false;
                g._timer.Stop();
                Audio.StopAllSounds();
                MessageBox.Show("YOU WIN! All enemies eliminated!", "Victory",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Restart();
            }
        }
```

- Health pickup: +25 HP (max 100), must have taken damage
- Ammo pickup: +8 shells (max 99), must not be full
- Win condition: all enemies dead → "YOU WIN!" dialog → restart
