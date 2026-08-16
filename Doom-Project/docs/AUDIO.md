# Sound System

## How It Works

The game uses the Windows `PlaySound` API from `winmm.dll` to play WAV files. No external libraries needed.

## Files

All sounds are in `resources/sound/`:

| File | When It Plays |
|------|--------------|
| `theme.wav` | Background music, loops forever from start |
| `shotgun.wav` | When the player shoots |
| `player_pain.wav` | When the player takes damage |
| `npc_attack.wav` | When an enemy starts attacking |
| `npc_pain.wav` | When an enemy takes damage |
| `npc_death.wav` | When an enemy dies |

## Code

```csharp
// Play a sound once
Audio.PlayWav("resources/sound/shotgun.wav", false);

// Play a sound in a loop
Audio.PlayWav("resources/sound/theme.wav", true);

// Stop all sounds
Audio.StopAllSounds();
```

## Notes

- Sounds are played asynchronously (non-blocking)
- The `loop` parameter makes the sound repeat until `StopAllSounds()` is called
- If the WAV file doesn't exist, nothing happens (no error)
