# Renderer.cs - Line by Line

This is the most complex file. It handles all the 3D rendering using raycasting.

## What Raycasting Is

Imagine standing in a room and shooting a laser pointer straight ahead. When it hits a wall, you measure how far away it is. Then you draw a vertical line on your screen — closer walls = taller lines, farther walls = shorter lines. Do this for every column of the screen and you get a 3D view from a 2D map. That's raycasting.

## Lines 1-21: Setup

```csharp
using System;
using System.Collections.Generic;    // List<>, Dictionary<>
using System.Drawing;                // Bitmap, Color, Rectangle
using System.Drawing.Imaging;        // BitmapData, PixelFormat
using System.Runtime.InteropServices; // Marshal.Copy

namespace Doom_Project
{
    public class Renderer
    {
        private readonly Game _g;     // reference to the game

        public Renderer(Game g)
        {
            _g = g;
        }
```

## Lines 23-49: Main Render

```csharp
        public void RenderFrame()
        {
            int horizon = Game.RenderH / 2;  // middle of the screen (180)

            // clear wall distances (set to max so sprite occlusion works)
            for (int x = 0; x < Game.RenderW; x++)
            {
                _g._wallDist[x] = double.MaxValue;
            }

            // draw sky, floor, and walls
            DrawSky(horizon);
            DrawFloor(horizon);
            DrawWalls(horizon);

            // draw sprites (enemies + pickups) on top
            double dirX = Math.Cos(_g._angle);
            double dirY = Math.Sin(_g._angle);
            double planeX = -dirY * Game.PlaneLen;  // camera plane perpendicular to direction
            double planeY = dirX * Game.PlaneLen;
            DrawSprites(horizon, dirX, dirY, planeX, planeY);
        }
```

The render order matters: sky first, then floor, then walls, then sprites on top. This way sprites appear in front of the sky/floor but behind closer walls.

`_wallDist` is set to max before drawing so that sprites behind walls get hidden (occlusion).

## Lines 51-81: Sky

```csharp
        private void DrawSky(int horizon)
        {
            // offset based on player angle so sky scrolls when you turn
            int skyOff = (int)(_g._angle * _g._skyW / (2 * Math.PI));

            // pre-calculate which sky column to draw for each screen column
            int[] sxMap = new int[Game.RenderW];
            for (int x = 0; x < Game.RenderW; x++)
            {
                sxMap[x] = ((x + skyOff) % _g._skyW + _g._skyW) % _g._skyW;
            }

            for (int y = 0; y < horizon; y++)
            {
                int sy = y * (_g._skyH - 1) / horizon;  // map screen Y to sky Y
                int syBase = sy * _g._skyW * 4;
                int db = y * Game.RenderW * 4;
                for (int x = 0; x < Game.RenderW; x++)
                {
                    int si = syBase + sxMap[x] * 4;
                    int di = db + x * 4;
                    _g._frameBuf[di] = _g._skyPixels[si];
                    _g._frameBuf[di + 1] = _g._skyPixels[si + 1];
                    _g._frameBuf[di + 2] = _g._skyPixels[si + 2];
                    _g._frameBuf[di + 3] = 255;
                }
            }
        }
```

The sky scrolls horizontally as you turn. `skyOff` shifts the sky image based on the player's angle. The double modulo `% _g._skyW` handles wrapping (so the sky loops).

## Lines 83-103: Floor

```csharp
        private void DrawFloor(int horizon)
        {
            for (int y = horizon; y < Game.RenderH; y++)
            {
                // floor gets brighter closer to the player (lower on screen)
                byte c = (byte)(18 + (y - horizon) * 28 / (Game.RenderH - horizon));
                int db = y * Game.RenderW * 4;
                for (int x = 0; x < Game.RenderW; x++)
                {
                    int di = db + x * 4;
                    _g._frameBuf[di] = c;
                    _g._frameBuf[di + 1] = c;
                    _g._frameBuf[di + 2] = (byte)(c - 6);
                    _g._frameBuf[di + 3] = 255;
                }
            }
        }
```

The floor is just a gradient. Closer to the player (lower on screen) = brighter. The slight blue offset (`c - 6`) gives it a cool tone.

## Lines 105-296: Walls (the big one)

This is the core raycasting engine. It works by shooting one ray per screen column.

### Step 1: Calculate ray direction (line 118)
```csharp
                // cameraX: -1 at left edge, 0 at center, +1 at right edge
                double cameraX = 2.0 * x / Game.RenderW - 1.0;
                double rayDirX = dirX + planeX * cameraX;
                double rayDirY = dirY + planeY * cameraX;
```

Each screen column gets a slightly different angle. Left edge = angle left of center, right edge = angle right of center.

### Step 2: Start at player's map cell (line 125)
```csharp
                int mapX = (int)_g._playerX;
                int mapY = (int)_g._playerY;
```

### Step 3: Calculate DDA distances (line 130)
```csharp
                // how far the ray travels to cross one grid line
                double deltaDistX = Math.Abs(1 / rayDirX);
                double deltaDistY = Math.Abs(1 / rayDirY);
```

This is the classic DDA trick. `1/rayDirX` tells you how far the ray must travel to cross one vertical grid line.

### Step 4: Step direction and initial distances (line 136)
```csharp
                int stepX, stepY;
                double sideDistX, sideDistY;

                if (rayDirX < 0)
                {
                    stepX = -1;
                    sideDistX = (_g._playerX - mapX) * deltaDistX;
                }
                else
                {
                    stepX = 1;
                    sideDistX = (mapX + 1.0 - _g._playerX) * deltaDistX;
                }
                // same for Y...
```

`stepX` tells us which direction to step (+1 or -1). `sideDistX` is the initial distance to the first grid line.

### Step 5: DDA loop (line 162)
```csharp
                char hit = '0';
                int side = 0;
                while (hit == '0')
                {
                    if (sideDistX < sideDistY)
                    {
                        sideDistX += deltaDistX;
                        mapX += stepX;
                        side = 0;
                    }
                    else
                    {
                        sideDistY += deltaDistY;
                        mapY += stepY;
                        side = 1;
                    }
                    // stop if outside map
                    if (mapX < 0 || mapY < 0 || mapX >= _g._cols || mapY >= _g._rows)
                    {
                        hit = '1';
                        break;
                    }
                    hit = _g._map[mapY, mapX];
                }
```

This steps through the grid cell by cell until it hits a wall. `side` tells us if we hit a vertical wall (0) or horizontal wall (1) — this is used for shading.

### Step 6: Calculate wall distance (line 188)
```csharp
                double perpWallDist;
                if (side == 0)
                {
                    perpWallDist = sideDistX - deltaDistX;
                }
                else
                {
                    perpWallDist = sideDistY - deltaDistY;
                }
```

**Perpendicular distance** (not actual distance) is used to avoid the fisheye effect. If you used actual distance, walls at the edges of the screen would appear farther away and curve inward.

### Step 7: Calculate wall strip height (line 207)
```csharp
                int lineHeight = (int)(Game.RenderH / perpWallDist);
```

Closer walls = taller lines. Simple division.

### Step 8-9: Texture mapping (lines 220-258)
 figuring out which column of the texture to draw based on where the ray hit the wall.

### Step 10: Draw the wall column (line 280)
```csharp
                for (int y = drawStart; y <= drawEnd; y++)
                {
                    int texY = (int)texPos;
                    if (texY >= tex.Height)
                    {
                        texY = tex.Height - 1;
                    }
                    texPos += texStep;
                    int si = (texY * tex.Width + texX) * 4;
                    int di = (y * Game.RenderW + x) * 4;
                    _g._frameBuf[di] = (byte)(tex.Pixels[si] * shade);
                    _g._frameBuf[di + 1] = (byte)(tex.Pixels[si + 1] * shade);
                    _g._frameBuf[di + 2] = (byte)(tex.Pixels[si + 2] * shade);
                    _g._frameBuf[di + 3] = 255;
                }
```

Pixel by pixel, sampling from the texture and multiplying by shade for the darkening effect.

### Shading
```csharp
                // walls facing sideways are darker (0.72 = 28% darker)
                double shade = 1.0;
                if (side == 1)
                {
                    shade = 0.72;
                }
                // distance fade: things far away get darker
                double distFade = 1.0 - Math.Min(0.8, perpWallDist * 0.04);
                shade = shade * distFade;
```

Two effects combined:
1. Horizontal walls are 28% darker than vertical walls (gives depth)
2. Walls fade to black with distance (0.04 = fade rate, max 80% dark)

## Lines 298-462: Sprites

### DrawSprites (line 302)
1. Collect all sprites (enemies + pickups) into a list
2. Project each sprite onto the screen using the camera matrix
3. Skip sprites behind the camera
4. Sort by distance (farthest first = painter's algorithm)
5. Draw each sprite as a billboard

### The projection math (line 362)
```csharp
                double invDet = 1.0 / (planeX * dirY - dirX * planeY);
                double tx = invDet * (dirY * sx - dirX * sy);
                double depth = invDet * (-planeY * sx + planeX * sy);
```

This is a 2D matrix inverse. `tx` = horizontal position on screen, `depth` = distance from camera. If `depth <= 0`, the sprite is behind us.

### DrawBillboard (line 400)
Draws one sprite:
1. Calculate height based on size and depth
2. Anchor feet to the ground (so sprites don't float)
3. Draw pixel by pixel, skipping transparent pixels (alpha < 100)
4. Skip pixels where a wall is closer (occlusion using `_wallDist`)

## Lines 464-517: Animation helpers

```csharp
        public SpriteFrame FrameForEnemy(Enemy e)
        {
            EnemyType t = e.Type;
            if (e.State == "idle")   return PickFrame(t.Idle, e.StateTime, t.IdleRate);
            if (e.State == "walk")   return PickFrame(t.Walk, e.StateTime, t.WalkRate);
            if (e.State == "attack") return PickFrame(t.Attack, e.StateTime, t.AttackRate);
            if (e.State == "pain")   return PickFrame(t.Pain, e.StateTime, t.PainRate);
            return ClampFrame(t.Death, e.Frame);
        }
```

- `PickFrame` = looping animation (modulo wraps around)
- `ClampFrame` = non-looping animation (stays on last frame, used for death)

## Lines 519-529: Present

```csharp
        public void Present()
        {
            Rectangle rect = new Rectangle(0, 0, Game.RenderW, Game.RenderH);
            BitmapData data = _g._frame.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            Marshal.Copy(_g._frameBuf, 0, data.Scan0, _g._frameBuf.Length);
            _g._frame.UnlockBits(data);
        }
```

This copies the raw byte array (`_frameBuf`) into the Bitmap (`_frame`) so WinForms can display it. `Marshal.Copy` is the fastest way to move bytes between managed (.NET) and unmanaged (GDI+) memory.
