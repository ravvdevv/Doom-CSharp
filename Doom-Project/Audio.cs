using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Doom_Project
{
    // ================================================================
    //  AUDIO - play sounds and music using Windows API
    //  by raven
    //
    //  two separate systems:
    //    mciSendString = background music (can loop without being killed)
    //    PlaySound     = sound effects (shotgun, pain, etc)
    // ================================================================

    public static class Audio
    {
        // ============================================================
        //  MUSIC - uses mciSendString (Media Control Interface)
        //  this can play music in the background while effects play
        // ============================================================

        [DllImport("winmm.dll")]
        private static extern int mciSendString(string command, string buffer, int bufferSize, IntPtr hwndCallback);

        public static void PlayMusic(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }
            // close any previous music first
            mciSendString("close music", null, 0, IntPtr.Zero);
            // open the wav file as an alias called "music"
            mciSendString("open \"" + path + "\" type waveaudio alias music", null, 0, IntPtr.Zero);
            // play it in a loop
            mciSendString("play music repeat", null, 0, IntPtr.Zero);
        }

        public static void StopMusic()
        {
            mciSendString("stop music", null, 0, IntPtr.Zero);
            mciSendString("close music", null, 0, IntPtr.Zero);
        }

        // ============================================================
        //  EFFECTS - uses PlaySound (classic, simple, one at a time)
        //  each new effect replaces the previous one, thats fine
        // ============================================================

        [DllImport("winmm.dll")]
        private static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);

        private const uint SND_ASYNC = 0x0001;
        private const uint SND_FILENAME = 0x00020000;

        public static void PlayEffect(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }
            PlaySound(path, IntPtr.Zero, SND_FILENAME | SND_ASYNC);
        }

        public static void StopAllSounds()
        {
            StopMusic();
            PlaySound(null, IntPtr.Zero, 0);
        }
    }
}
