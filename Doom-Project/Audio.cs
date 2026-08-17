using System;
using System.IO;
using System.Windows.Forms;
using NAudio.Wave;

namespace Doom_Project
{
    public static class Audio
    {
        private static WaveOutEvent _musicPlayer;
        private static AudioFileReader _musicReader;

        public static void PlayMusic(string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show("Theme file not found: " + path);
                return;
            }
            StopMusic();
            try
            {
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

        private static void OnMusicStopped(object sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                System.Diagnostics.Debug.WriteLine("Music stopped with error: " + e.Exception.Message);
            }
            // loop the music
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

        public static void PlayEffect(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }
            try
            {
                var reader = new AudioFileReader(path);
                reader.Volume = 1.0f;
                var player = new WaveOutEvent();
                player.Init(reader);
                player.PlaybackStopped += (s, e) =>
                {
                    player.Dispose();
                    reader.Dispose();
                };
                player.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Effect error: " + ex.Message);
            }
        }

        public static void StopAllSounds()
        {
            StopMusic();
        }
    }
}
