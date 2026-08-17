using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Doom_Project
{
    // ================================================================
    //  LOADER - loads all game resources from disk
    //  by raven
    //
    //  this is where we read the map, textures, sprites, etc.
    //  if you get "file not found" errors, look here first
    // ================================================================

    public static class Loader
    {
        // ================================================================
        //  MAP LOADING
        // ================================================================

        // reads map.txt and converts it to a 2D char array
        // the "out" keyword sends values back to the variable we passed in
        public static char[,] LoadMap(string path, out int rows, out int cols)
        {
            string[] lines = File.ReadAllLines(path);
            rows = lines.Length;
            cols = lines[0].Length;
            char[,] map = new char[rows, cols];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    map[r, c] = lines[r][c];
                }
            }
            return map;
        }

        // ================================================================
        //  GENERIC IMAGE LOADING
        // ================================================================

        // load an image, return null if file doesnt exist (dont crash)
        public static Image LoadImageOrNull(string path)
        {
            if (File.Exists(path))
            {
                return Image.FromFile(path);
            }
            return null;
        }

        // load a bitmap and return its raw pixel data
        // resizes to a square, locks bits, copies raw array
        // BGRA format = 4 bytes per pixel (Blue, Green, Red, Alpha)
        public static byte[] LoadPixels(Bitmap src, int size)
        {
            using (Bitmap bmp = new Bitmap(src, size, size))
            {
                Rectangle rect = new Rectangle(0, 0, size, size);
                BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                byte[] pixels = new byte[Math.Abs(data.Stride) * size];
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
                bmp.UnlockBits(data);
                return pixels;
            }
        }

        // load a bitmap at its original size and return a SpriteFrame
        public static SpriteFrame LoadSpriteFrame(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }
            using (Bitmap bmp = new Bitmap(path))
            {
                Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                byte[] pixels = new byte[bmp.Width * bmp.Height * 4];
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
                bmp.UnlockBits(data);
                SpriteFrame frame = new SpriteFrame();
                frame.Pixels = pixels;
                frame.Width = bmp.Width;
                frame.Height = bmp.Height;
                frame.Aspect = (double)bmp.Width / bmp.Height;
                return frame;
            }
        }

        // ================================================================
        //  WALL TEXTURE LOADING
        // ================================================================

        // loads wall textures 1.png through 5.png
        // if no textures found, generates a fallback brick pattern
        public static void LoadTextures(string path, Dictionary<char, WallTexture> textures)
        {
            for (char i = '1'; i <= '5'; i++)
            {
                string file = Path.Combine(path, i + ".png");
                if (!File.Exists(file))
                {
                    continue;
                }
                using (Bitmap bmp = new Bitmap(file))
                {
                    WallTexture tex = new WallTexture();
                    tex.Pixels = LoadPixels(bmp, 128);
                    tex.Width = 128;
                    tex.Height = 128;
                    textures[i] = tex;
                }
            }

            // fallback: if no textures loaded, make a brick pattern
            // this is just so the walls arent completely black
            if (textures.Count == 0)
            {
                using (Bitmap bmp = new Bitmap(64, 64))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.FromArgb(120, 90, 60));
                        using (Pen p = new Pen(Color.FromArgb(60, 40, 25)))
                        {
                            for (int i = 0; i <= 64; i += 16)
                            {
                                g.DrawLine(p, i, 0, i, 64);
                                g.DrawLine(p, 0, i, 64, i);
                            }
                        }
                    }
                    WallTexture tex = new WallTexture();
                    tex.Pixels = LoadPixels(bmp, 64);
                    tex.Width = 64;
                    tex.Height = 64;
                    textures['1'] = tex;
                }
            }
        }

        // ================================================================
        //  SKY LOADING
        // ================================================================

        // load sky image, generate gradient if file missing
        public static byte[] LoadSky(string path, out int skyW, out int skyH)
        {
            if (File.Exists(path))
            {
                using (Bitmap bmp = new Bitmap(path))
                {
                    skyW = bmp.Width;
                    skyH = bmp.Height;
                    byte[] pixels = LoadPixels(bmp, skyW);
                    return pixels;
                }
            }

            // fallback: generate a simple gradient sky
            skyW = 640;
            skyH = 200;
            byte[] skyPixels = new byte[skyW * skyH * 4];
            for (int y = 0; y < skyH; y++)
            {
                byte c = (byte)(40 + y * 30 / skyH);
                for (int x = 0; x < skyW; x++)
                {
                    int i = (y * skyW + x) * 4;
                    skyPixels[i] = c;
                    skyPixels[i + 1] = c;
                    skyPixels[i + 2] = (byte)(c + 20);
                    skyPixels[i + 3] = 255;
                }
            }
            return skyPixels;
        }

        // ================================================================
        //  ENEMY TYPE LOADING
        // ================================================================

        // loads all enemy types and their sprite frames
        // this is where the enemy stats are defined (speed, hp, damage, etc)
        public static Dictionary<string, EnemyType> LoadEnemyTypes()
        {
            string npc = Path.Combine(Application.StartupPath, "resources", "sprites", "npc");
            Dictionary<string, EnemyType> types = new Dictionary<string, EnemyType>();

            //                          idle frames        idle rate    walk frames           walk rate
            //                          attack frames       attack rate  pain frames           pain rate
            //                          death frames        death rate   speed  hp   dmg  size  range

            // soldier config - attack 0.12s per frame (was 0.08, too fast), 0.5s cooldown between attacks
            // cooldown = after swing finishes, soldier waits before attacking again
            //                           idle frames   idle spd    walk frames  walk spd   pain frames  pain spd    death frames  death spd   attack frames                                                                                         attack spd   spd   hp  dmg  size  range   cooldown
            types["soldier"] = LoadEnemyType(
                Path.Combine(npc, "soldier"),
                new string[] { "0" },                     0.15,
                new string[] { "0", "1", "2", "3" },      0.18,
                new string[] { "0", "1" },                 0.25,
                new string[] { "0" },                      0.35,
                new string[] { "POSSM0", "POSSN0", "POSSO0", "POSSP0", "POSSQ0", "POSSR0", "POSSS0", "POSST0", "POSSU0" }, 0.12,
                0.8, 100, 10, 0.7, 1.6,
                0.5);  // <-- 0.5s cooldown so soldier doesn't rapid fire
            types["soldier"].Name = "soldier";

            types["caco"] = LoadEnemyType(
                Path.Combine(npc, "caco_demon"),
                new string[] { "0" },                                           0.15,
                new string[] { "0", "1", "2" },                             0.2,
                new string[] { "0", "1", "2", "3", "4" },                   0.15,
                new string[] { "0", "1" },                                   0.2,
                new string[] { "0", "1", "2", "3", "4", "5" },              0.12,
                1.3, 150, 15, 0.9, 1.7);
            types["caco"].Name = "caco";

            types["soul"] = LoadEnemyType(
                Path.Combine(npc, "lost_soul"),
                new string[] { "0" },                  0.3,
                new string[] { "0", "1" },             0.25,
                new string[] { "0" },                  0.3,
                new string[] { "0" },                  0.3,
                new string[] { "0", "1", "2", "3" },  0.15,
                2.2, 50, 8, 0.8, 1.2);
            types["soul"].Name = "soul";

            types["cyber"] = LoadEnemyType(
                Path.Combine(npc, "cyber_demon"),
                new string[] { "0" },                  0.3,
                new string[] { "0", "1", "3", "4" },  0.22,
                new string[] { "0", "1" },             0.25,
                new string[] { "0" },                  0.3,
                new string[] { "0", "1", "2", "3", "4", "5" }, 0.12,
                0.6, 400, 25, 1.5, 2.0);
            types["cyber"].Name = "cyber";

            return types;
        }

        // helper to load one enemy type
        private static EnemyType LoadEnemyType(
            string dir,
            string[] idleFrames, double idleRate,
            string[] walkFrames, double walkRate,
            string[] attackFrames, double attackRate,
            string[] painFrames, double painRate,
            string[] deathFrames, double deathRate,
            double speed, int health, int damage, double sizeTiles, double attackRange,
            double attackCooldown = 0.0)  // optional: seconds between attacks (0 = no cooldown)
        {
            EnemyType t = new EnemyType();
            t.Idle = LoadFrames(dir, "idle", idleFrames);
            t.Walk = LoadFrames(dir, "walk", walkFrames);
            t.Attack = LoadFrames(dir, "attack", attackFrames);
            t.Pain = LoadFrames(dir, "pain", painFrames);
            t.Death = LoadFrames(dir, "death", deathFrames);
            t.IdleRate = idleRate;
            t.WalkRate = walkRate;
            t.AttackRate = attackRate;
            t.PainRate = painRate;
            t.DeathRate = deathRate;
            t.Speed = speed;
            t.Health = health;
            t.Damage = damage;
            t.SizeTiles = sizeTiles;
            t.AttackRange = attackRange;
            t.AttackCooldown = attackCooldown;
            return t;
        }

        // helper to load frames from a subfolder
        // takes the directory, subfolder (idle/walk/etc), and frame names
        private static List<SpriteFrame> LoadFrames(string dir, string sub, string[] names)
        {
            List<SpriteFrame> frames = new List<SpriteFrame>();
            foreach (string name in names)
            {
                SpriteFrame frame = LoadSpriteFrame(Path.Combine(dir, sub, name + ".png"));
                if (frame != null)
                {
                    frames.Add(frame);
                }
            }
            return frames;
        }
    }
}
