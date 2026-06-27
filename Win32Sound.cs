using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
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
        private static readonly object _audioCacheLock = new();
        private static int _startupResolutionLogged;

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

        public static void LogStartupSoundResolution(params string[] fileNames)
        {
            if (Interlocked.Exchange(ref _startupResolutionLogged, 1) == 1)
                return;

            if (fileNames == null || fileNames.Length == 0)
            {
                Log("Startup sound source diagnostic requested with no file names.");
                return;
            }

            foreach (var fileName in fileNames)
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    Log("Startup sound source: <empty> => invalid filename");
                    continue;
                }

                if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || fileName.Contains(".."))
                {
                    Log($"Startup sound source: {fileName} => invalid filename");
                    continue;
                }

                if (TryResolveAudioPath(fileName, out var diskPath) && diskPath != null)
                {
                    Log($"Startup sound source: {fileName} => disk ({diskPath})");
                    continue;
                }

                if (TryCreateEmbeddedAudioFactory(fileName, out _, out var resourceName) &&
                    !string.IsNullOrWhiteSpace(resourceName))
                {
                    Log($"Startup sound source: {fileName} => embedded ({resourceName})");
                    continue;
                }

                Log($"Startup sound source: {fileName} => missing");
            }
        }

        public static void PlayWavFile(string fileName)
        {
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return;
            if (fileName.Contains(".."))
                return;

            Func<Stream>? embeddedAudioFactory = null;
            if (!TryResolveAudioPath(fileName, out var path) &&
                !TryCreateEmbeddedAudioFactory(fileName, out embeddedAudioFactory, out _))
            {
                Log($"Audio file not found on disk or embedded resources: {fileName}");
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

            var playbackTask = Task.Run(() =>
            {
                if (path != null)
                {
                    PlayWavCoreFromFile(path, cts.Token);
                    return;
                }

                PlayWavCoreFromEmbedded(embeddedAudioFactory!, Path.GetFileName(fileName), cts.Token);
            });
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

        private static bool TryResolveAudioPath(string fileName, out string? path)
        {
            if (TryGetPersistentAudioPath(fileName, out var persistentPath))
            {
                path = persistentPath;
                return true;
            }

            var candidateDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddDirectory(string? directory)
            {
                if (!string.IsNullOrWhiteSpace(directory))
                    candidateDirectories.Add(directory);
            }

            AddDirectory(AppContext.BaseDirectory);
            AddDirectory(Environment.CurrentDirectory);
            AddDirectory(AppDomain.CurrentDomain.BaseDirectory);

            try
            {
                AddDirectory(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName));
            }
            catch
            {
                // Ignore process path lookup errors and continue probing known directories.
            }

            foreach (var directory in candidateDirectories)
            {
                var candidatePath = Path.Combine(directory, fileName);
                if (File.Exists(candidatePath))
                {
                    path = candidatePath;
                    return true;
                }
            }

            path = null;
            return false;
        }

        private static bool TryGetPersistentAudioPath(string fileName, out string? path)
        {
            var targetPath = Path.Combine(GetPersistentAudioDirectory(), fileName);
            if (File.Exists(targetPath))
            {
                path = targetPath;
                return true;
            }

            lock (_audioCacheLock)
            {
                if (File.Exists(targetPath))
                {
                    path = targetPath;
                    return true;
                }

                if (!TryCreateEmbeddedAudioFactory(fileName, out var embeddedAudioFactory, out var resourceName) ||
                    embeddedAudioFactory == null)
                {
                    path = null;
                    return false;
                }

                try
                {
                    Directory.CreateDirectory(GetPersistentAudioDirectory());

                    using var source = embeddedAudioFactory();
                    using var destination = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    source.CopyTo(destination);

                    path = targetPath;
                    Log($"Persisted audio asset: {fileName} => {targetPath}" +
                        (resourceName != null ? $" (from {resourceName})" : string.Empty));
                    return true;
                }
                catch (Exception ex)
                {
                    Log($"Failed to persist audio asset: {fileName} => {targetPath}", ex);
                    path = null;
                    return false;
                }
            }
        }

        private static string GetPersistentAudioDirectory()
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "HaloShift", "audio");
        }

        private static bool TryCreateEmbeddedAudioFactory(
            string fileName,
            out Func<Stream>? audioFactory,
            out string? resourceName)
        {
            var assembly = typeof(Win32Sound).Assembly;
            var assemblyName = assembly.GetName().Name ?? "HaloShift";
            var normalizedName = fileName.Replace('\\', '.').Replace('/', '.');

            bool TryCreateFactoryForResource(string resourceName, out Func<Stream>? factory)
            {
                using var probe = assembly.GetManifestResourceStream(resourceName);
                if (probe == null)
                {
                    factory = null;
                    return false;
                }

                factory = () =>
                    assembly.GetManifestResourceStream(resourceName)
                    ?? throw new InvalidOperationException($"Embedded resource stream missing: {resourceName}");
                return true;
            }

            var resourceCandidates = new[]
            {
                $"{assemblyName}.{normalizedName}",
                $"{assemblyName}.assets.{normalizedName}",
                normalizedName
            };

            foreach (var candidateResourceName in resourceCandidates)
            {
                if (TryCreateFactoryForResource(candidateResourceName, out audioFactory))
                {
                    resourceName = candidateResourceName;
                    return true;
                }
            }

            foreach (var existingResourceName in assembly.GetManifestResourceNames())
            {
                if (existingResourceName.EndsWith($".{normalizedName}", StringComparison.OrdinalIgnoreCase) &&
                    TryCreateFactoryForResource(existingResourceName, out audioFactory))
                {
                    resourceName = existingResourceName;
                    return true;
                }
            }

            audioFactory = null;
            resourceName = null;
            return false;
        }

        private static void PlayWavCoreFromFile(string path, CancellationToken token)
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

        private static void PlayWavCoreFromEmbedded(Func<Stream> audioFactory, string displayName, CancellationToken token)
        {
            const int maxAttempts = 2;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (token.IsCancellationRequested)
                    return;

                IWavePlayer? output = null;
                Stream? stream = null;
                WaveFileReader? reader = null;
                try
                {
                    if (!TryCreatePlayer(out var createdPlayer))
                    {
                        Log($"No audio output backend available for {displayName}.");
                        return;
                    }

                    output = createdPlayer;
                    stream = audioFactory();
                    reader = new WaveFileReader(stream);

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
                    Log($"Embedded audio playback attempt {attempt} failed for {displayName}.", ex);

                    if (attempt == maxAttempts || token.IsCancellationRequested)
                        return;

                    Thread.Sleep(50);
                }
                finally
                {
                    output?.Dispose();
                    reader?.Dispose();
                    stream?.Dispose();
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
