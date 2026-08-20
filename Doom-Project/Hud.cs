using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Doom_Project
{
    // ================================================================
    //  HUD - draws the minimap, crosshair, weapon, health, ammo, help
    //  by raven
    // ================================================================

    public class Hud
    {
        private readonly Game _g;

        public Hud(Game g)
        {
            _g = g;
        }

        // ================================================================
        //  MINIMAP - small overhead view of the map
        // ================================================================

        public void DrawMinimap(Graphics g)
        {
            const int cell = 4;   // each map cell is 4x4 pixels on minimap
            const int pad = 8;    // padding from screen edge
            int mw = _g._cols * cell;
            int mh = _g._rows * cell;

            // draw dark background panel with rounded corners
            Rectangle panel = new Rectangle(pad - 3, pad - 3, mw + 6, mh + 6);
            using (GraphicsPath path = RoundedRect(panel, 6))
            {
                using (SolidBrush backdrop = new SolidBrush(Color.FromArgb(170, 0, 0, 0)))
                {
                    g.FillPath(backdrop, path);
                }
            }

            // draw walls on the minimap
            for (int r = 0; r < _g._rows; r++)
            {
                for (int c = 0; c < _g._cols; c++)
                {
                    if (_g._map[r, c] == '1')
                    {
                        using (SolidBrush wall = new SolidBrush(Color.FromArgb(150, 200, 150, 90)))
                        {
                            g.FillRectangle(wall, pad + c * cell, pad + r * cell, cell, cell);
                        }
                    }
                }
            }

            // draw enemies (red dots)
            foreach (Enemy e in _g._enemies)
            {
                if (e.State == "dead")
                {
                    continue;
                }
                int ex = pad + (int)(e.X * cell);
                int ey = pad + (int)(e.Y * cell);
                using (SolidBrush enemyDot = new SolidBrush(Color.FromArgb(200, 255, 50, 50)))
                {
                    g.FillEllipse(enemyDot, ex - 2, ey - 2, 4, 4);
                }
            }

            // draw pickups (yellow dots for health, blue for ammo)
            foreach (PickupItem p in _g._pickups)
            {
                if (p.Taken)
                {
                    continue;
                }
                int ppx = pad + (int)(p.X * cell);
                int ppy = pad + (int)(p.Y * cell);
                Color dotColor;
                if (p.Type == "health")
                {
                    dotColor = Color.FromArgb(180, 255, 255, 0); // yellow
                }
                else
                {
                    dotColor = Color.FromArgb(180, 80, 180, 255); // blue
                }
                using (SolidBrush pickupDot = new SolidBrush(dotColor))
                {
                    g.FillRectangle(pickupDot, ppx - 1, ppy - 1, 3, 3);
                }
            }

            // draw border
            using (GraphicsPath path = RoundedRect(panel, 6))
            {
                using (Pen border = new Pen(Color.FromArgb(130, 255, 214, 84)))
                {
                    g.DrawPath(border, path);
                }
            }

            // draw player dot (green circle at your position)
            int px = pad + (int)(_g._playerX * cell);
            int py = pad + (int)(_g._playerY * cell);
            using (SolidBrush playerDot = new SolidBrush(Color.FromArgb(220, 0, 255, 0)))
            {
                g.FillEllipse(playerDot, px - 2, py - 2, 5, 5);
            }

            // draw direction line (shows where youre looking)
            double dirLen = 8;
            int dx = (int)(Math.Cos(_g._angle) * dirLen);
            int dy = (int)(Math.Sin(_g._angle) * dirLen);
            using (Pen dirPen = new Pen(Color.FromArgb(180, 0, 255, 0), 1.5f))
            {
                g.DrawLine(dirPen, px, py, px + dx, py + dy);
            }
        }

        // helper to make a rounded rectangle path
        public static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ================================================================
        //  CROSSHAIR - center of screen aiming reticle
        // ================================================================

        public void DrawCrosshair(Graphics g)
        {
            int cx = _g.ClientSize.Width / 2;
            int cy = _g.ClientSize.Height / 2;
            using (Pen pen = new Pen(Color.Lime, 2))
            {
                g.DrawLine(pen, cx - 10, cy, cx + 10, cy);  // horizontal line
                g.DrawLine(pen, cx, cy - 10, cx, cy + 10);  // vertical line
            }
        }

        // ================================================================
        //  WEAPON - draw the shotgun sprite at the bottom
        // ================================================================

        public void DrawWeapon(Graphics g)
        {
            Image frame = _g._weaponFrames[_g._weaponIndex];

            // calculate animation progress (0 to 1)
            double progress = _g._weaponTimer / (Game.WeaponFrameTime * (_g._weaponFrames.Count - 1));
            double kick = 0;
            if (_g._shooting && progress < 1.0)
            {
                // weapon kicks up 26 pixels when shooting, more at start less at end
                kick = (1.0 - progress) * 26;
            }

            // draw weapon centered at bottom of screen
            int h = (int)(_g.ClientSize.Height * 0.42);  // 42% of screen height
            int w = (int)(h * (float)frame.Width / frame.Height);
            int x = _g.ClientSize.Width / 2 - w / 2;
            int y = _g.ClientSize.Height - h + (int)kick;
            g.DrawImage(frame, new Rectangle(x, y, w, h));
        }

        // ================================================================
        //  HEALTH & AMMO - bottom panels with digit images
        // ================================================================

        public void DrawHud(Graphics g)
        {
            int panelW = 150;
            int panelH = 56;
            int margin = 16;

            // health panel on the left, ammo panel on the right
            Rectangle hpRect = new Rectangle(margin, _g.ClientSize.Height - panelH - 14, panelW, panelH);
            Rectangle ammoRect = new Rectangle(_g.ClientSize.Width - panelW - margin, _g.ClientSize.Height - panelH - 14, panelW, panelH);

            // draw panel backgrounds
            DrawHudPanel(g, hpRect);
            DrawHudPanel(g, ammoRect);

            // draw labels "HP" and "AMMO"
            using (Font font = new Font(FontFamily.GenericSansSerif, 13, FontStyle.Bold))
            {
                using (SolidBrush gold = new SolidBrush(Color.FromArgb(255, 214, 84)))
                {
                    g.DrawString("HP", font, gold, hpRect.X + 10, hpRect.Y + 8);
                    g.DrawString("AMMO", font, gold, ammoRect.X + 10, ammoRect.Y + 8);
                }
            }

            // draw the actual numbers using digit images
            DrawNumber(g, _g._health.ToString(), hpRect.X + 10, hpRect.Y + 28, 20);
            DrawNumber(g, _g._ammo.ToString(), ammoRect.X + 10, ammoRect.Y + 28, 20);

            // show a message while reloading (inside the ammo panel)
            if (_g._reloading)
            {
                using (Font font = new Font(FontFamily.GenericSansSerif, 11, FontStyle.Bold))
                {
                    using (SolidBrush red = new SolidBrush(Color.FromArgb(255, 120, 80)))
                    {
                        string reloadText = "RELOADING...";
                        SizeF reloadSize = g.MeasureString(reloadText, font);
                        float rX = ammoRect.Right - reloadSize.Width - 10;  // right side of panel
                        g.DrawString(reloadText, font, red, rX, ammoRect.Y + 28);
                    }
                }
            }

            // draw wave number at top center
            using (Font font = new Font(FontFamily.GenericSansSerif, 18, FontStyle.Bold))
            {
                using (SolidBrush gold = new SolidBrush(Color.FromArgb(255, 214, 84)))
                {
                    string waveText = "WAVE " + _g._wave;
                    SizeF waveSize = g.MeasureString(waveText, font);
                    float waveX = (_g.ClientSize.Width - waveSize.Width) / 2;
                    g.DrawString(waveText, font, gold, waveX, 10);
                }
            }

            // draw score at top right
            using (Font font = new Font(FontFamily.GenericSansSerif, 14, FontStyle.Bold))
            {
                using (SolidBrush gold = new SolidBrush(Color.FromArgb(255, 214, 84)))
                {
                    string scoreText = "SCORE: " + _g._score;
                    SizeF scoreSize = g.MeasureString(scoreText, font);
                    g.DrawString(scoreText, font, gold, _g.ClientSize.Width - scoreSize.Width - 16, 14);

                    // high score below current score
                    if (_g._highScore > 0)
                    {
                        string hiText = "BEST: " + _g._highScore + " (W" + _g._highWave + ")";
                        SizeF hiSize = g.MeasureString(hiText, font);
                        g.DrawString(hiText, font, gold, _g.ClientSize.Width - hiSize.Width - 16, 34);
                    }
                }
            }

            // new high score flash
            if (_g._gameState == "gameover" && _g.IsNewHighScore())
            {
                using (Font font = new Font(FontFamily.GenericSansSerif, 28, FontStyle.Bold))
                {
                    using (SolidBrush flash = new SolidBrush(Color.FromArgb(200, 255, 215, 0)))
                    {
                        string newHi = "NEW HIGH SCORE!";
                        SizeF sz = g.MeasureString(newHi, font);
                        float nx = (_g.ClientSize.Width - sz.Width) / 2;
                        g.DrawString(newHi, font, flash, nx, _g.ClientSize.Height / 2 + 30);
                    }
                }
            }

            // draw wave clear message
            if (_g._waveClear)
            {
                using (Font font = new Font(FontFamily.GenericSansSerif, 24, FontStyle.Bold))
                {
                    using (SolidBrush flash = new SolidBrush(Color.FromArgb(200, 255, 100, 100)))
                    {
                        string clearText = "WAVE " + _g._wave + " CLEAR!";
                        SizeF clearSize = g.MeasureString(clearText, font);
                        float cx = (_g.ClientSize.Width - clearSize.Width) / 2;
                        g.DrawString(clearText, font, flash, cx, _g.ClientSize.Height / 2 - 40);
                    }
                }
            }
        }

        // draw a dark panel with gold border
        private void DrawHudPanel(Graphics g, Rectangle rect)
        {
            using (GraphicsPath path = RoundedRect(rect, 8))
            {
                using (SolidBrush backdrop = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
                {
                    g.FillPath(backdrop, path);
                }
                using (Pen border = new Pen(Color.FromArgb(160, 255, 214, 84)))
                {
                    g.DrawPath(border, path);
                }
            }
        }

        // draw a number using digit images (0.png through 9.png)
        private void DrawNumber(Graphics g, string text, int x, int y, int h)
        {
            if (_g._digitImages.Count == 0)
            {
                return;
            }
            foreach (char c in text)
            {
                int d = c - '0';  // convert char to digit ('5' -> 5)
                if (d < 0 || d > 9 || d >= _g._digitImages.Count)
                {
                    continue;
                }
                // scale digit width to maintain aspect ratio
                int w = (int)(h * (double)_g._digitImages[d].Width / _g._digitImages[d].Height);
                g.DrawImage(_g._digitImages[d], x, y, w, h);
                x += w + 4;  // move right for next digit
            }
        }

        // ================================================================
        //  BLOOD FLASH - red overlay when player takes damage
        // ================================================================

        public void DrawBloodFlash(Graphics g)
        {
            if (_g._bloodScreen == null || _g._bloodAlpha <= 0)
            {
                return;
            }
            float a = (float)_g._bloodAlpha;
            // ColorMatrix with Matrix33 controls opacity
            // this is the standard way to draw a semi-transparent image in GDI+
            using (ImageAttributes attrs = new ImageAttributes())
            {
                ColorMatrix matrix = new ColorMatrix();
                matrix.Matrix33 = a;  // set alpha/opacity
                attrs.SetColorMatrix(matrix);
                Rectangle dst = new Rectangle(0, 0, _g.ClientSize.Width, _g.ClientSize.Height);
                g.DrawImage(_g._bloodScreen, dst, 0, 0, _g._bloodScreen.Width, _g._bloodScreen.Height, GraphicsUnit.Pixel, attrs);
            }
        }

        // ================================================================
        //  HELP TEXT - controls shown at bottom of screen
        // ================================================================

        public void DrawHelp(Graphics g)
        {
            using (Font font = new Font(FontFamily.GenericMonospace, 11))
            {
                using (SolidBrush white = new SolidBrush(Color.White))
                {
                    string line1 = "WASD/Arrows move  |  LMB shoot  |  Mouse look  |  Esc: exit";
                    SizeF s1 = g.MeasureString(line1, font);
                    g.DrawString(line1, font, white, (_g.ClientSize.Width - s1.Width) / 2, _g.ClientSize.Height - 46);
                }
                using (SolidBrush dim = new SolidBrush(Color.FromArgb(180, 255, 255, 255)))
                {
                    string line2 = "Press M to show/hide minimap";
                    SizeF s2 = g.MeasureString(line2, font);
                    g.DrawString(line2, font, dim, (_g.ClientSize.Width - s2.Width) / 2, _g.ClientSize.Height - 28);
                }
            }
        }
    }
}
