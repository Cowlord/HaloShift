using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;

namespace HaloShift
{
    internal static class Win32Sound
    {
        private sealed class PlaybackSession
        {
            public CancellationTokenSource Cts { get; }
            public Task PlaybackTask { get; set; }

            public PlaybackSession(CancellationTokenSource cts)
            {
                Cts = cts;
                PlaybackTask = Task.CompletedTask;
            }
        }

        private static PlaybackSession? _activeSession;
        private static readonly object _lock = new();
        private static readonly object _logLock = new();

        public static string GetAudioLogPath()
            => Path.Combine(AppContext.BaseDirectory, "haloshift-audio.log");

        public static bool OpenAudioLog()
        {
            var logPath = GetAudioLogPath();

            try
            {
                if (!File.Exists(logPath))
                {
                    using var _ = File.Create(logPath);
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = logPath,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception ex)
            {
                Log("Failed to open audio log file.", ex);
                return false;
            }
        }

        public static void PlayWavFile(string fileName)
        {
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return;
            if (fileName.Contains(".."))
                return;

            var path = Path.Combine(AppContext.BaseDirectory, fileName);
            if (!File.Exists(path))
            {
                Log($"Audio file not found: {fileName}");
                return;
            }

            var cts = new CancellationTokenSource();
            var newSession = new PlaybackSession(cts);
            PlaybackSession? previousSession;

            lock (_lock)
            {
                previousSession = _activeSession;
                _activeSession = newSession;
            }

            try
            {
                previousSession?.Cts.Cancel();
            }
            catch (Exception ex)
            {
                Log("Failed to cancel prior audio session.", ex);
            }

            var playbackTask = Task.Run(() => PlayWavCore(path, cts.Token));
            newSession.PlaybackTask = playbackTask;

            _ = playbackTask.ContinueWith(_ =>
            {
                lock (_lock)
                {
                    if (ReferenceEquals(_activeSession, newSession))
                        _activeSession = null;
                }

                cts.Dispose();
            }, TaskScheduler.Default);

            if (previousSession != null)
            {
                _ = previousSession.PlaybackTask.ContinueWith(_ =>
                {
                    previousSession.Cts.Dispose();
                }, TaskScheduler.Default);
            }
        }

        private static void PlayWavCore(string path, CancellationToken token)
        {
            const int maxAttempts = 2;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (token.IsCancellationRequested)
                    return;

                IWavePlayer? output = null;
                AudioFileReader? reader = null;
                try
                {
                    if (!TryCreatePlayer(out var createdPlayer))
                    {
                        Log($"No audio output backend available for {Path.GetFileName(path)}.");
                        return;
                    }

                    output = createdPlayer;

                    reader = new AudioFileReader(path);
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

                    return;
                }
                catch (Exception ex)
                {
                    Log($"Audio playback attempt {attempt} failed for {Path.GetFileName(path)}.", ex);

                    if (attempt == maxAttempts || token.IsCancellationRequested)
                        return;

                    Thread.Sleep(50);
                }
                finally
                {
                    output?.Dispose();
                    reader?.Dispose();
                }
            }
        }

        private static bool TryCreatePlayer(out IWavePlayer player)
        {
            try
            {
                player = new WaveOutEvent
                {
                    DesiredLatency = 100,
                    NumberOfBuffers = 2
                };
                return true;
            }
            catch (Exception ex)
            {
                Log("WaveOutEvent initialization failed.", ex);
            }

            try
            {
                player = new DirectSoundOut();
                return true;
            }
            catch (Exception ex)
            {
                Log("DirectSoundOut initialization failed.", ex);
            }

            player = null!;
            return false;
        }

        private static void Log(string message, Exception? exception = null)
        {
            try
            {
                var logPath = GetAudioLogPath();
                string line = $"{DateTime.UtcNow:O} [Win32Sound] {message}";

                if (exception != null)
                    line += $" Exception: {exception.GetType().Name}: {exception.Message}";

                lock (_logLock)
                {
                    File.AppendAllText(logPath, line + Environment.NewLine);
                }
            }
            catch
            {
                // Never let logging affect app behavior.
            }
        }
    }
}
