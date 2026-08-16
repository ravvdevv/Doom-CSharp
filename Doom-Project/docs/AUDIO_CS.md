# Audio.cs - Line by Line

Smallest file. Plays WAV sounds using the Windows `PlaySound` API.

## Lines 1-5: Imports
```csharp
using System;
using System.IO;                        // File.Exists() - check if sound file exists
using System.Runtime.InteropServices;    // DllImport - call Windows API functions
```

## Lines 12-15: The Windows API function
```csharp
    public static class Audio
    {
        [DllImport("winmm.dll")]
        private static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);
```
`DllImport("winmm.dll")` means "this function lives in the Windows multimedia DLL."
`static extern` means "this is an external function, not defined here."
`PlaySound` is the Windows API for playing WAV files.

## Lines 17-19: Flag constants
```csharp
        private const uint SND_ASYNC = 0x0001;           // Play asynchronously (don't freeze the game)
        private const uint SND_FILENAME = 0x00020000;    // The path is a filename (not a resource)
        private const uint SND_LOOP = 0x0008;            // Loop the sound
```
These are bitflags that tell PlaySound how to behave.

## Lines 21-33: PlayWav method
```csharp
        public static void PlayWav(string path, bool loop)
        {
            if (!File.Exists(path))
            {
                return;                    // If file doesn't exist, just do nothing
            }
            uint flags = SND_FILENAME | SND_ASYNC;   // Always use filename + async
            if (loop)
            {
                flags = flags | SND_LOOP;             // Add loop flag if requested
            }
            PlaySound(path, IntPtr.Zero, flags);      // Play it!
        }
```
- `SND_FILENAME`: The path is a file, not a Windows resource ID
- `SND_ASYNC`: Play in background, don't block the game
- `SND_LOOP`: Keep playing until stopped
- `IntPtr.Zero`: No special module handle needed

## Lines 35-38: StopAllSounds
```csharp
        public static void StopAllSounds()
        {
            PlaySound(null, IntPtr.Zero, 0);    // Passing null stops all playing sounds
        }
```
Called when the game closes to stop the theme music.
