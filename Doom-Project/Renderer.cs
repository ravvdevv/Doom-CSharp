using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Doom_Project
{
    // ================================================================
    //  RENDERER - raycasting, walls, sprites, billboard drawing
    //  by raven
    //
    //  this is the most complex file, it handles all the 3D rendering
    //  if you want to understand the math, search "raycasting tutorial"
    //  ambot dawg gi unsa nako ni pagsabot pero goods ra
    // ================================================================

    public class Renderer
    {
        private readonly Game _g;

        public Renderer(Game g)
        {
            _g = g;
        }

        // ================================================================
        //  MAIN RENDER - draws the 3D scene into the pixel buffer
        // ================================================================

        public void RenderFrame()
        {
            int horizon = Game.RenderH / 2;  // middle of the screen

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

        // ================================================================
        //  SKY - draw the sky background (top half of screen)
        // ================================================================

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
                // map screen Y to sky Y (stretches sky to fill top half)
                int sy = y * (_g._skyH - 1) / horizon;
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

        // ================================================================
        //  FLOOR - draw the floor gradient (bottom half of screen)
        // ================================================================

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

        // ================================================================
        //  WALLS - cast rays and draw textured walls
        //
        //  this is the core of the raycasting engine
        //  it works by shooting a ray from the player for each screen column
        //  then figuring out which wall it hit and how far away it is
        // ================================================================

        private void DrawWalls(int horizon)
        {
            // camera direction and plane
            double dirX = Math.Cos(_g._angle);
            double dirY = Math.Sin(_g._angle);
            double planeX = -dirY * Game.PlaneLen;
            double planeY = dirX * Game.PlaneLen;

            for (int x = 0; x < Game.RenderW; x++)
            {
                // STEP 1: calculate ray direction
                // cameraX: -1 at left edge, 0 at center, +1 at right edge
                double cameraX = 2.0 * x / Game.RenderW - 1.0;
                double rayDirX = dirX + planeX * cameraX;
                double rayDirY = dirY + planeY * cameraX;

                // STEP 2: start at the player's map cell
                int mapX = (int)_g._playerX;
                int mapY = (int)_g._playerY;

                // STEP 3: calculate how far the ray travels to cross one grid line
                // this is the classic DDA trick: divide 1.0 by the ray component
                double deltaDistX = Math.Abs(1 / rayDirX);
                double deltaDistY = Math.Abs(1 / rayDirY);

                // STEP 4: calculate step direction and initial side distances
                // these steer the ray through the grid cell by cell
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

                if (rayDirY < 0)
                {
                    stepY = -1;
                    sideDistY = (_g._playerY - mapY) * deltaDistY;
                }
                else
                {
                    stepY = 1;
                    sideDistY = (mapY + 1.0 - _g._playerY) * deltaDistY;
                }

                // STEP 5: step through the map grid until we hit a wall
                // classic DDA loop, just keep stepping until something stops us
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

                    // stop if we go outside the map
                    if (mapX < 0 || mapY < 0 || mapX >= _g._cols || mapY >= _g._rows)
                    {
                        hit = '1';
                        break;
                    }
                    hit = _g._map[mapY, mapX];
                }

                // STEP 6: calculate wall distance
                // perpendicular distance avoids the fisheye effect
                double perpWallDist;
                if (side == 0)
                {
                    perpWallDist = sideDistX - deltaDistX;
                }
                else
                {
                    perpWallDist = sideDistY - deltaDistY;
                }
                if (perpWallDist < 0.01)
                {
                    perpWallDist = 0.01;  // clamp so we never divide by zero
                }
                _g._wallDist[x] = perpWallDist;  // save for sprite occlusion

                // STEP 7: calculate wall strip height
                int lineHeight = (int)(Game.RenderH / perpWallDist);
                int drawStart = horizon - lineHeight / 2;
                if (drawStart < 0)
                {
                    drawStart = 0;
                }
                int drawEnd = horizon + lineHeight / 2;
                if (drawEnd >= Game.RenderH)
                {
                    drawEnd = Game.RenderH - 1;
                }

                // STEP 8: get the texture for the wall we hit
                WallTexture tex;
                if (_g._textures.ContainsKey(hit))
                {
                    tex = _g._textures[hit];
                }
                else
                {
                    tex = _g._textures['1'];  // fallback to texture 1
                }

                // STEP 9: figure out which column of the texture to draw
                // where the ray hit the wall horizontally (0.0 to 1.0)
                double wallX;
                if (side == 0)
                {
                    wallX = _g._playerY + perpWallDist * rayDirY;
                }
                else
                {
                    wallX = _g._playerX + perpWallDist * rayDirX;
                }
                wallX -= Math.Floor(wallX);  // keep only the decimal part
                int texX = (int)(wallX * tex.Width);
                // flip texture depending on which side was hit
                if (side == 0 && rayDirX > 0)
                {
                    texX = tex.Width - texX - 1;
                }
                if (side == 1 && rayDirY < 0)
                {
                    texX = tex.Width - texX - 1;
                }
                if (texX < 0)
                {
                    texX = 0;
                }
                if (texX >= tex.Width)
                {
                    texX = tex.Width - 1;
                }

                // STEP 10: draw the wall column pixel by pixel
                double texStep = (double)tex.Height / lineHeight;  // how much to step through texture per screen pixel
                double texPos = (drawStart - horizon + lineHeight / 2.0) * texStep;
                if (texPos < 0)
                {
                    texPos = 0;
                }

                // shading: walls facing sideways are darker (0.72 = 28% darker)
                double shade = 1.0;
                if (side == 1)
                {
                    shade = 0.72;
                }
                // distance fade: things far away get darker
                double distFade = 1.0 - Math.Min(0.8, perpWallDist * 0.04);
                shade = shade * distFade;

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
            }
        }

        // ================================================================
        //  SPRITE RENDERING - draw enemies and pickups as billboards
        //
        //  sprites always face the camera (billboard effect)
        //  we project each sprite onto the screen and draw it column by column
        // ================================================================

        private void DrawSprites(int horizon, double dirX, double dirY, double planeX, double planeY)
        {
            // collect all sprites that need to be drawn
            List<DrawItem> items = new List<DrawItem>();

            // add enemies
            foreach (Enemy e in _g._enemies)
            {
                if (e.Remove)
                {
                    continue;
                }
                SpriteFrame frame = FrameForEnemy(e);
                if (frame == null)
                {
                    continue;
                }
                DrawItem item = new DrawItem();
                item.X = e.X;
                item.Y = e.Y;
                item.Frame = frame;
                item.SizeTiles = e.Type.SizeTiles;
                items.Add(item);
            }

            // add pickups
            foreach (PickupItem p in _g._pickups)
            {
                if (p.Taken)
                {
                    continue;
                }
                SpriteFrame frame;
                if (p.Type == "health")
                {
                    frame = _g._pickupHealth;
                }
                else
                {
                    continue;
                }
                if (frame == null)
                {
                    continue;
                }
                DrawItem item = new DrawItem();
                item.X = p.X;
                item.Y = p.Y;
                item.Frame = frame;
                item.SizeTiles = 0.5;
                items.Add(item);
            }

            // project each sprite onto the screen
            List<DrawItem> visible = new List<DrawItem>();
            foreach (DrawItem it in items)
            {
                // vector from player to sprite
                double sx = it.X - _g._playerX;
                double sy = it.Y - _g._playerY;

                // project sprite position onto the camera plane
                // invDet inverts the camera transform matrix
                double invDet = 1.0 / (planeX * dirY - dirX * planeY);
                double tx = invDet * (dirY * sx - dirX * sy);        // horizontal position on screen
                double depth = invDet * (-planeY * sx + planeX * sy); // how far from camera

                // skip sprites behind the camera
                if (depth <= 0.01)
                {
                    continue;
                }

                // convert to screen X coordinate (center = half width)
                int screenX = (int)((Game.RenderW / 2.0) * (1.0 + tx / depth));

                // skip sprites way off screen (300 pixel buffer each side)
                if (screenX < -300 || screenX >= Game.RenderW + 300)
                {
                    continue;
                }

                it.Depth = depth;
                it.ScreenX = screenX;
                visible.Add(it);
            }

            // sort back to front (farthest first) so closer sprites draw on top
            visible.Sort(SortByDepth);

            // draw each visible sprite
            foreach (DrawItem it in visible)
            {
                DrawBillboard(it, horizon);
            }
        }

        // sort comparator: farther sprites first
        private static int SortByDepth(DrawItem a, DrawItem b)
        {
            return b.Depth.CompareTo(a.Depth);
        }

        // draw a single sprite as a billboard
        private void DrawBillboard(DrawItem it, int horizon)
        {
            SpriteFrame frame = it.Frame;

            // scale sprite height by size and depth
            int h = (int)(Game.RenderH * it.SizeTiles / it.Depth);
            if (h < 1)
            {
                h = 1;
            }
            if (h > Game.RenderH * 3)
            {
                h = Game.RenderH * 3;  // cap at 3x screen height so huge sprites dont blow up memory
            }
            int w = (int)(h * frame.Aspect);
            if (w < 1)
            {
                w = 1;
            }

            // center sprite horizontally on screen
            int x0 = it.ScreenX - w / 2;
            // anchor sprite feet to the ground so they dont float
            int floorY = horizon + (int)(h / (2.0 * it.SizeTiles));
            int y0 = floorY - h;

            // draw each pixel of the sprite
            for (int px = x0; px < x0 + w; px++)
            {
                if (px < 0 || px >= Game.RenderW)
                {
                    continue;
                }
                // skip if behind a wall
                if (it.Depth >= _g._wallDist[px])
                {
                    continue;
                }
                int texX = (px - x0) * frame.Width / w;

                for (int py = y0; py < y0 + h; py++)
                {
                    if (py < 0 || py >= Game.RenderH)
                    {
                        continue;
                    }
                    int texY = (py - y0) * frame.Height / h;
                    int si = (texY * frame.Width + texX) * 4;

                    // skip transparent pixels (alpha below 100 = see-through)
                    if (frame.Pixels[si + 3] < 100)
                    {
                        continue;
                    }

                    int di = (py * Game.RenderW + px) * 4;
                    _g._frameBuf[di] = frame.Pixels[si];
                    _g._frameBuf[di + 1] = frame.Pixels[si + 1];
                    _g._frameBuf[di + 2] = frame.Pixels[si + 2];
                    _g._frameBuf[di + 3] = 255;
                }
            }
        }

        // ================================================================
        //  ANIMATION - pick the right frame for each enemy
        // ================================================================

        // figure out which sprite frame to show based on enemy state
        public SpriteFrame FrameForEnemy(Enemy e)
        {
            EnemyType t = e.Type;

            if (e.State == "idle")
            {
                return PickFrame(t.Idle, e.StateTime, t.IdleRate);
            }
            if (e.State == "walk")
            {
                return PickFrame(t.Walk, e.StateTime, t.WalkRate);
            }
            if (e.State == "attack")
            {
                return PickFrame(t.Attack, e.StateTime, t.AttackRate);
            }
            if (e.State == "pain")
            {
                return PickFrame(t.Pain, e.StateTime, t.PainRate);
            }
            // cooldown: waiting between attacks, just stand there looking normal
            if (e.State == "cooldown")
            {
                return PickFrame(t.Idle, 0, t.IdleRate);
            }

            // death uses ClampFrame so it stays on last frame
            return ClampFrame(t.Death, e.Frame);
        }

        // pick a frame from a looping animation
        private static SpriteFrame PickFrame(List<SpriteFrame> frames, double time, double rate)
        {
            if (frames.Count == 0)
            {
                return null;
            }
            int index = (int)(time / rate) % frames.Count;  // modulo = loops back to start
            return frames[index];
        }

        // pick a frame from a non-looping animation (clamped to valid range)
        private static SpriteFrame ClampFrame(List<SpriteFrame> frames, int index)
        {
            if (frames.Count == 0)
            {
                return null;
            }
            if (index < 0)
            {
                index = 0;
            }
            if (index >= frames.Count)
            {
                index = frames.Count - 1;
            }
            return frames[index];
        }

        // ================================================================
        //  PRESENT - copy pixel buffer to bitmap
        // ================================================================

        // this copies our raw pixel array to the Bitmap so it can be drawn on screen
        // present() copies our pixel buffer to the bitmap so it shows on screen
        // try-catch needed because _frame can get disposed during game over
        // (close() disposes the bitmap, but timer might fire one more time)
        public void Present()
        {
            if (_g._frame == null || _g.IsDisposed || !_g._running)
            {
                return;
            }
            try
            {
                Rectangle rect = new Rectangle(0, 0, Game.RenderW, Game.RenderH);
                BitmapData data = _g._frame.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(_g._frameBuf, 0, data.Scan0, _g._frameBuf.Length);
                _g._frame.UnlockBits(data);
            }
            catch { }
        }
    }
}
