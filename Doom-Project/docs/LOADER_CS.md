# Loader.cs - Line by Line

Loads all game resources from disk: map, textures, sprites, sounds.

## Lines 1-7: Imports
```csharp
using System;
using System.Collections.Generic;    // Dictionary<>, List<>
using System.Drawing;                // Bitmap, Color, Graphics
using System.Drawing.Imaging;        // BitmapData, PixelFormat, ImageLockMode
using System.IO;                     // File, Path
using System.Runtime.InteropServices; // Marshal (copy bytes between memory)
using System.Windows.Forms;          // Application.StartupPath
```

## Lines 16-36: LoadMap
```csharp
    public static class Loader     // Static = no instances needed, call methods directly
    {
        public static char[,] LoadMap(string path, out int rows, out int cols)
        {
            string[] lines = File.ReadAllLines(path);  // Read all lines from map.txt
            rows = lines.Length;                        // Number of rows (32)
            cols = lines[0].Length;                     // Number of columns (42)
            char[,] map = new char[rows, cols];         // Create 2D array
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    map[r, c] = lines[r][c];           // Copy each character
                }
            }
            return map;
        }
```
Reads the map text file and converts it to a 2D character array. Each character is one tile: '0' = floor, '1' = wall.

## Lines 42-64: Image loading helpers

### LoadImageOrNull (line 42)
```csharp
        public static Image LoadImageOrNull(string path)
        {
            if (File.Exists(path))
            {
                return Image.FromFile(path);    // Load and return the image
            }
            return null;                        // File doesn't exist, return null
        }
```
Safe loading - returns null instead of crashing if file is missing.

### LoadPixels (line 53)
```csharp
        public static byte[] LoadPixels(Bitmap src, int size)
        {
            using (Bitmap bmp = new Bitmap(src, size, size))  // Resize to square
            {
                Rectangle rect = new Rectangle(0, 0, size, size);
                // Lock the bitmap's memory so we can read it fast
                BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                byte[] pixels = new byte[Math.Abs(data.Stride) * size];  // Allocate byte array
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);       // Copy pixels to array
                bmp.UnlockBits(data);                                     // Unlock memory
                return pixels;
            }
        }
```
This converts a Bitmap (GDI+ object) into a raw byte array we can use for fast pixel-by-pixel rendering. `data.Stride` is the number of bytes per row (may include padding).

### LoadSpriteFrame (line 67)
```csharp
        public static SpriteFrame LoadSpriteFrame(string path)
        {
            if (!File.Exists(path))
            {
                return null;                    // File doesn't exist
            }
            using (Bitmap bmp = new Bitmap(path))
            {
                Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                byte[] pixels = new byte[bmp.Width * bmp.Height * 4];   // 4 bytes per pixel
                Marshal.Copy(data.Scan0, pixels, 0, pixels.Length);
                bmp.UnlockBits(data);
                SpriteFrame frame = new SpriteFrame();
                frame.Pixels = pixels;          // Raw pixel data
                frame.Width = bmp.Width;        // Image width
                frame.Height = bmp.Height;      // Image height
                frame.Aspect = (double)bmp.Width / bmp.Height;  // For proportional scaling
                return frame;
            }
        }
```
Like LoadPixels but keeps original size and returns a SpriteFrame instead of raw bytes.

## Lines 93-136: LoadTextures
```csharp
        public static void LoadTextures(string path, Dictionary<char, WallTexture> textures)
        {
            // Load textures 1.png through 5.png
            for (char i = '1'; i <= '5'; i++)
            {
                string file = Path.Combine(path, i + ".png");
                if (!File.Exists(file))
                {
                    continue;                    // Skip if file doesn't exist
                }
                using (Bitmap bmp = new Bitmap(file))
                {
                    WallTexture tex = new WallTexture();
                    tex.Pixels = LoadPixels(bmp, 128);   // Resize to 128x128
                    tex.Width = 128;
                    tex.Height = 128;
                    textures[i] = tex;                    // Map character '1' to this texture
                }
            }

            // If no textures loaded, generate a fallback brick pattern
            if (textures.Count == 0)
            {
                using (Bitmap bmp = new Bitmap(64, 64))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        // Fill with brown color
                        g.Clear(Color.FromArgb(120, 90, 60));
                        using (Pen p = new Pen(Color.FromArgb(60, 40, 25)))
                        {
                            // Draw grid lines to look like bricks
                            for (int i = 0; i <= 64; i += 16)
                            {
                                g.DrawLine(p, i, 0, i, 64);    // Vertical lines
                                g.DrawLine(p, 0, i, 64, i);    // Horizontal lines
                            }
                        }
                    }
                    WallTexture tex = new WallTexture();
                    tex.Pixels = LoadPixels(bmp, 64);
                    tex.Width = 64;
                    tex.Height = 64;
                    textures['1'] = tex;            // Assign as wall type '1'
                }
            }
        }
```
If texture files are missing, the game generates a simple brick pattern so it doesn't crash.

## Lines 142-171: LoadSky
```csharp
        public static byte[] LoadSky(string path, out int skyW, out int skyH)
        {
            if (File.Exists(path))
            {
                using (Bitmap bmp = new Bitmap(path))
                {
                    skyW = bmp.Width;
                    skyH = bmp.Height;
                    byte[] pixels = LoadPixels(bmp, skyW);  // Keep original width
                    return pixels;
                }
            }

            // No sky file? Generate a simple gradient
            skyW = 640;
            skyH = 200;
            byte[] skyPixels = new byte[skyW * skyH * 4];
            for (int y = 0; y < skyH; y++)
            {
                // Gradient: darker at top, brighter at bottom
                byte c = (byte)(40 + y * 30 / skyH);
                for (int x = 0; x < skyW; x++)
                {
                    int i = (y * skyW + x) * 4;
                    skyPixels[i] = c;                 // Blue
                    skyPixels[i + 1] = c;             // Green
                    skyPixels[i + 2] = (byte)(c + 20); // Red (slightly more = blue tint)
                    skyPixels[i + 3] = 255;           // Alpha (fully opaque)
                }
            }
            return skyPixels;
        }
```

## Lines 177-290: Enemy type loading

### LoadEnemyTypes (line 177)
```csharp
        public static Dictionary<string, EnemyType> LoadEnemyTypes()
        {
            string npc = Path.Combine(Application.StartupPath, "resources", "sprites", "npc");
            Dictionary<string, EnemyType> types = new Dictionary<string, EnemyType>();
```
Builds the path to the NPC sprites folder and creates an empty dictionary.

Each enemy type is defined with a big visual comment showing which parameter is which:
```
types["soldier"] = LoadEnemyType(
    Path.Combine(npc, "soldier"),     // Folder containing soldier sprites
    new string[] { "0" },             // Idle frames: just 1 frame
    0.15,                             // Idle rate: 0.15 sec per frame
    new string[] { "0", "1", "2", "3" },  // Walk frames: 4 frames
    0.18,                             // Walk rate: 0.18 sec per frame
    new string[] { "0", "1" },        // Attack frames: 2 frames
    0.25,                             // Attack rate: 0.25 sec per frame
    new string[] { "0" },             // Pain frames: 1 frame
    0.35,                             // Pain rate: 0.35 sec per frame
    new string[] { "POSSM0", ... },   // Death frames: 9 frames
    0.08,                             // Death rate: 0.08 sec per frame
    0.8,    // Speed: 0.8 tiles/sec
    100,    // Health: 100 HP
    10,     // Damage: 10 per hit
    0.7,    // Size: 0.7 tiles
    1.6);   // Attack range: 1.6 tiles
```

### LoadEnemyType (line 249)
```csharp
        private static EnemyType LoadEnemyType(
            string dir,
            string[] idleFrames, double idleRate,
            string[] walkFrames, double walkRate,
            string[] attackFrames, double attackRate,
            string[] painFrames, double painRate,
            string[] deathFrames, double deathRate,
            double speed, int health, int damage, double sizeTiles, double attackRange)
        {
            EnemyType t = new EnemyType();
            t.Idle = LoadFrames(dir, "idle", idleFrames);    // Load idle PNGs
            t.Walk = LoadFrames(dir, "walk", walkFrames);    // Load walk PNGs
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
            return t;
        }
```
Creates an EnemyType and fills in all its fields from the parameters.

### LoadFrames (line 277)
```csharp
        private static List<SpriteFrame> LoadFrames(string dir, string sub, string[] names)
        {
            List<SpriteFrame> frames = new List<SpriteFrame>();
            foreach (string name in names)
            {
                // Load "dir/sub/name.png" (e.g. "soldier/walk/0.png")
                SpriteFrame frame = LoadSpriteFrame(Path.Combine(dir, sub, name + ".png"));
                if (frame != null)
                {
                    frames.Add(frame);          // Only add if file existed
                }
            }
            return frames;
        }
```
Loads all frames for one animation from a subfolder.
