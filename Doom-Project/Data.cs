using System.Collections.Generic;

namespace Doom_Project
{
    // ================================================================
    //  DATA CLASSES - simple containers, one thing each
    //  by raven
    // ================================================================

    // one enemy that's alive (or dead) in the level
    public class Enemy
    {
        public EnemyType Type;      // what kind of enemy (soldier, caco, etc.)
        public double X;            // map position horizontal
        public double Y;            // map position vertical
        public int Health;          // how much damage it can take before dying
        public string State;        // what its doing right now (idle/walk/attack/pain/death/dead)
        public double StateTime;    // how many seconds its been in this state
        public int Frame;           // which animation frame its on
        public bool Remove;         // true if it should be removed from the game
    }

    // blueprint for one enemy kind, shared by all enemies of that type
    public class EnemyType
    {
        public string Name;              // "soldier", "caco", etc.

        // animation frames for each state
        public List<SpriteFrame> Idle;     // standing still frames
        public List<SpriteFrame> Walk;     // walking frames
        public List<SpriteFrame> Attack;   // attacking frames
        public List<SpriteFrame> Pain;     // getting hit frames
        public List<SpriteFrame> Death;    // dying frames

        // how long each frame lasts in seconds
        public double IdleRate;
        public double WalkRate;
        public double AttackRate;
        public double PainRate;
        public double DeathRate;

        // stats
        public double Speed;        // how fast it moves (tiles per second)
        public int Health;          // how much damage it can take
        public int Damage;          // how much damage it deals to player
        public double SizeTiles;    // how big it looks on screen (in tiles)
        public double AttackRange;  // how close it needs to be to attack
        public double AttackCooldown; // seconds between attacks (prevents rapid fire)
    }

    // a health pack or ammo pack sitting on the map
    public class PickupItem
    {
        public double X;            // map position horizontal
        public double Y;            // map position vertical
        public string Type;         // "health" or "ammo"
        public bool Taken;          // true if player already picked it up
    }

    // a sprite that needs to be drawn on screen (enemy or pickup)
    public class DrawItem
    {
        public double X;            // map position horizontal
        public double Y;            // map position vertical
        public SpriteFrame Frame;   // the image to draw
        public double SizeTiles;    // how big to draw it (in tiles)
        public double Depth;        // how far from camera (for sorting back to front)
        public int ScreenX;         // where on screen horizontally (pixels)
    }

    // one image frame of a sprite
    public class SpriteFrame
    {
        public byte[] Pixels;       // raw pixel colors BGRA format 4 bytes per pixel
        public int Width;           // image width in pixels
        public int Height;          // image height in pixels
        public double Aspect;       // width / height (keeps proportions when scaling)
    }

    // wall texture
    public class WallTexture
    {
        public byte[] Pixels;       // raw pixel colors BGRA format 4 bytes per pixel
        public int Width;           // image width in pixels
        public int Height;          // image height in pixels
    }
}
