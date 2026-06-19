using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace HaloShift
{
    internal static class Win32Sound
    {
        private static CancellationTokenSource? _playbackCts;
        private static readonly object _lock = new();

        public static void PlayWavFile(string fileName)
        {
            var path = Path.Combine(AppContext.BaseDirectory, fileName);
            if (!File.Exists(path))
                return;

            var cts = new CancellationTokenSource();

            lock (_lock)
            {
                _playbackCts?.Cancel();
                _playbackCts = cts;
            }

            _ = Task.Run(() => PlayWavCore(path, cts));
        }

        private static void PlayWavCore(string path, CancellationTokenSource cts)
        {
            WasapiOut? output = null;
            AudioFileReader? reader = null;
            try
            {
                var token = cts.Token;
                reader = new AudioFileReader(path);
                output = new WasapiOut(AudioClientShareMode.Shared, 100);
                output.Init(reader);
                output.Play();

                while (output.PlaybackState == PlaybackState.Playing)
                {
                    if (token.IsCancellationRequested)
                    {
                        output.Stop();
                        return;
                    }
                    Thread.Sleep(20);
                }
            }
            catch
            {
                // Best-effort sound playback
            }
            finally
            {
                output?.Dispose();
                reader?.Dispose();
                cts.Dispose();
            }
        }
    }
}
