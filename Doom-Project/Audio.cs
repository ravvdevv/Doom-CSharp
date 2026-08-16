using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Doom_Project
{
    // ================================================================
    //  AUDIO - play sounds and music using Windows API
    //  by raven
    //
    //  this is the only way to play .wav files in C# without extra libraries
    //  i dont fully understand the Windows API part but it works so lets go
    // ================================================================

    public static class Audio
    {
        // Windows API function for playing sound
        // DllImport = call a function from a .dll file
        [DllImport("winmm.dll")]
        private static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);

        // flags for PlaySound
        private const uint SND_ASYNC = 0x0001;          // play without blocking
        private const uint SND_FILENAME = 0x00020000;   // path is a file path
        private const uint SND_LOOP = 0x0008;           // loop the sound

        public static void PlayWav(string path, bool loop)
        {
            // if file doesnt exist, just skip (dont crash)
            if (!File.Exists(path))
            {
                return;
            }
            uint flags = SND_FILENAME | SND_ASYNC;
            if (loop)
            {
                flags = flags | SND_LOOP;
            }
            PlaySound(path, IntPtr.Zero, flags);
        }

        public static void StopAllSounds()
        {
            // passing null = stop all sounds
            PlaySound(null, IntPtr.Zero, 0);
        }
    }
}
