using System;
using System.IO;
using System.Windows.Forms;
using NAudio.Wave;

namespace Doom_Project
{
    // ================================================================
    //  AUDIO - handles music and sound effects using NAudio
    //  by raven
    //
    //  PlayMusic() plays background music on loop
    //  PlayEffect() plays a one-shot sound (gunshot, pain, etc)
    //  StopAllSounds() stops everything
    // ================================================================

    public static class Audio
    {
        // music player - one instance that loops forever
        private static WaveOutEvent _musicPlayer;
        private static AudioFileReader _musicReader;

        // ================================================================
        //  PLAY MUSIC - start looping background music
        // ================================================================

        public static void PlayMusic(string path)
        {
            // make sure the file exists before trying to play
            if (!File.Exists(path))
            {
                MessageBox.Show("Theme file not found: " + path);
                return;
            }

            // stop any music that's already playing
            StopMusic();

            try
            {
                // create a new music player and start playing
                _musicReader = new AudioFileReader(path);
                _musicReader.Volume = 1.0f;
                _musicPlayer = new WaveOutEvent();
                _musicPlayer.Init(_musicReader);
                _musicPlayer.PlaybackStopped += OnMusicStopped;
                _musicPlayer.Play();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Music error: " + ex.Message + "\n" + ex.InnerException?.Message);
            }
        }

        // ================================================================
        //  MUSIC LOOP - restart music when it finishes playing
        // ================================================================

        // this runs every time the music track reaches the end
        // it rewinds to the start and plays again (loop forever)
        private static void OnMusicStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                System.Diagnostics.Debug.WriteLine("Music stopped with error: " + e.Exception.Message);
            }

            // loop the music - rewind to start and play again
            if (_musicReader != null && _musicPlayer != null)
            {
                try
                {
                    _musicReader.Position = 0;
                    _musicPlayer.Play();
                }
                catch { }
            }
        }

        // ================================================================
        //  STOP MUSIC - stop and dispose the music player
        // ================================================================

        public static void StopMusic()
        {
            if (_musicPlayer != null)
            {
                _musicPlayer.PlaybackStopped -= OnMusicStopped;
                _musicPlayer.Stop();
                _musicPlayer.Dispose();
                _musicPlayer = null;
            }
            if (_musicReader != null)
            {
                _musicReader.Dispose();
                _musicReader = null;
            }
        }

        // ================================================================
        //  PLAY EFFECT - play a one-shot sound effect
        //
        //  each effect gets its own player so multiple sounds can play at once
        //  when the effect finishes, the player cleans itself up
        // ================================================================

        public static void PlayEffect(string path)
        {
            // don't play if file doesn't exist
            if (!File.Exists(path))
            {
                return;
            }

            try
            {
                // create a new player just for this one sound
                AudioFileReader reader = new AudioFileReader(path);
                reader.Volume = 1.0f;
                WaveOutEvent player = new WaveOutEvent();
                player.Init(reader);

                // when this effect finishes playing, clean up the player
                // this is a named method instead of a lambda for simplicity
                player.PlaybackStopped += OnEffectStopped;
                player.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Effect error: " + ex.Message);
            }
        }

        // ================================================================
        //  EFFECT CLEANUP - dispose player after effect finishes
        // ================================================================

        // runs when a sound effect finishes playing
        // disposes the player and reader so we don't leak memory
        private static void OnEffectStopped(object sender, StoppedEventArgs e)
        {
            WaveOutEvent player = sender as WaveOutEvent;
            if (player != null)
            {
                player.PlaybackStopped -= OnEffectStopped;
                player.Stop();
                player.Dispose();
            }
        }

        // ================================================================
        //  STOP ALL - stop music and all effects
        // ================================================================

        public static void StopAllSounds()
        {
            StopMusic();
        }
    }
}
