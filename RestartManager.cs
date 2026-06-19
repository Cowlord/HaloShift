using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HaloShift
{
    /// <summary>
    /// Manages automatic restart functionality for HaloShift
    /// </summary>
    public static class RestartManager
    {
        private const string WATCHER_PROCESS_NAME = "HaloShiftWatcher";
        private const int RESTART_DELAY_MS = 3000; // 3 second delay before restart
        private const int MAX_RESTART_ATTEMPTS = 5;
        private const string RESTART_LOG_NAME = "restart.log";

        private static readonly string DataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HaloShift");
        private static readonly string RestartLogPath = Path.Combine(DataDirectory, RESTART_LOG_NAME);
        private static readonly string CurrentExecutablePath = Process.GetCurrentProcess().MainModule?.FileName ?? 
            Path.Combine(AppContext.BaseDirectory, "HaloShift.exe");

        /// <summary>
        /// Start a watcher process that will restart HaloShift if it crashes
        /// </summary>
        public static void StartWatcher()
        {
            // Only start watcher if we're the main application (not already a watcher)
            if (Process.GetCurrentProcess().ProcessName.Contains(WATCHER_PROCESS_NAME))
                return;

            Task.Run(() =>
            {
                try
                {
                    var watcherPath = Path.Combine(AppContext.BaseDirectory, $"{WATCHER_PROCESS_NAME}.exe");
                    if (!File.Exists(watcherPath))
                    {
                        // Create a simple watcher process using the current executable with a special argument
                        StartSelfWatcher();
                        return;
                    }

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = watcherPath,
                        Arguments = $"\"{CurrentExecutablePath}\" {Process.GetCurrentProcess().Id}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };

                    Process.Start(startInfo);
                    LogRestartMessage("Watcher process started");
                }
                catch (Exception ex)
                {
                    LogRestartMessage($"Failed to start watcher: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Start the current executable as a watcher for itself
        /// </summary>
        private static void StartSelfWatcher()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = CurrentExecutablePath,
                    Arguments = "--watcher",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(startInfo);
                LogRestartMessage("Self-watcher process started");
            }
            catch (Exception ex)
            {
                LogRestartMessage($"Failed to start self-watcher: {ex.Message}");
            }
        }

        /// <summary>
        /// Run as watcher process - monitor main process and restart if needed
        /// </summary>
        public static void RunWatcher(int parentProcessId)
        {
            try
            {
                var parentProcess = Process.GetProcessById(parentProcessId);
                LogRestartMessage($"Watching process {parentProcessId} ({parentProcess.ProcessName})");

                // Wait for parent process to exit
                parentProcess.WaitForExit();

                LogRestartMessage($"Parent process {parentProcessId} exited, checking for crash...");

                // Check if it was a crash (unclean exit)
                if (WasCrashExit())
                {
                    LogRestartMessage("Crash detected, attempting restart...");
                    RestartApplication();
                }
                else
                {
                    LogRestartMessage("Clean exit detected, not restarting");
                }
            }
            catch (ArgumentException)
            {
                LogRestartMessage($"Parent process {parentProcessId} not found, assuming crash...");
                RestartApplication();
            }
            catch (Exception ex)
            {
                LogRestartMessage($"Watcher error: {ex.Message}");
                RestartApplication();
            }
        }

        /// <summary>
        /// Run as self-watcher using command line argument
        /// </summary>
        public static void RunSelfWatcher()
        {
            LogRestartMessage("Self-watcher mode activated");

            // Find the main HaloShift process (not the watcher)
            Process? mainProcess = null;
            var currentId = Process.GetCurrentProcess().Id;

            foreach (var process in Process.GetProcessesByName("HaloShift"))
            {
                if (process.Id != currentId && !process.ProcessName.Contains(WATCHER_PROCESS_NAME))
                {
                    mainProcess = process;
                    break;
                }
            }

            if (mainProcess == null)
            {
                LogRestartMessage("No main process found to watch, exiting");
                return;
            }

            try
            {
                LogRestartMessage($"Watching main process {mainProcess.Id}");
                mainProcess.WaitForExit();

                LogRestartMessage("Main process exited, checking for crash...");
                Thread.Sleep(2000); // Brief delay to ensure lock file is updated

                if (WasCrashExit())
                {
                    LogRestartMessage("Crash detected, attempting restart...");
                    RestartApplication();
                }
                else
                {
                    LogRestartMessage("Clean exit detected, not restarting");
                }
            }
            catch (Exception ex)
            {
                LogRestartMessage($"Self-watcher error: {ex.Message}");
                RestartApplication();
            }
            finally
            {
                mainProcess?.Dispose();
            }
        }

        /// <summary>
        /// Determine if the exit was due to a crash
        /// </summary>
        private static bool WasCrashExit()
        {
            try
            {
                var lockFilePath = Path.Combine(DataDirectory, "haloshift.lock");
                if (!File.Exists(lockFilePath))
                    return false; // Clean shutdown removed lock file

                var lockTime = File.GetLastWriteTime(lockFilePath);
                var timeSinceLock = DateTime.Now - lockTime;

                // If lock file hasn't been updated recently, it's a crash
                return timeSinceLock.TotalSeconds > 15;
            }
            catch (Exception ex)
            {
                LogRestartMessage($"Failed to check crash exit status: {ex.Message}");
                return true;
            }
        }

        /// <summary>
        /// Restart the application with delay
        /// </summary>
        private static void RestartApplication()
        {
            var restartCount = GetRestartCount();

            if (restartCount >= MAX_RESTART_ATTEMPTS)
            {
                LogRestartMessage($"Maximum restart attempts ({MAX_RESTART_ATTEMPTS}) reached, giving up");
                return;
            }

            IncrementRestartCount();
            LogRestartMessage($"Restart attempt {restartCount + 1}/{MAX_RESTART_ATTEMPTS}");

            try
            {
                // Wait before restart to avoid rapid restart loops
                Thread.Sleep(RESTART_DELAY_MS);

                var startInfo = new ProcessStartInfo
                {
                    FileName = CurrentExecutablePath,
                    UseShellExecute = true,
                    WorkingDirectory = AppContext.BaseDirectory
                };

                Process.Start(startInfo);
                LogRestartMessage($"Restarted successfully: {CurrentExecutablePath}");
            }
            catch (Exception ex)
            {
                LogRestartMessage($"Restart failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current restart count from registry/file
        /// </summary>
        private static int GetRestartCount()
        {
            try
            {
                var countFile = Path.Combine(DataDirectory, ".restart_count");
                if (File.Exists(countFile))
                {
                    var content = File.ReadAllText(countFile);
                    if (int.TryParse(content, out var count) && 
                        File.GetLastWriteTime(countFile).Date == DateTime.Today)
                    {
                        return count;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to read restart count: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// Increment restart counter
        /// </summary>
        private static void IncrementRestartCount()
        {
            try
            {
                Directory.CreateDirectory(DataDirectory);
                var countFile = Path.Combine(DataDirectory, ".restart_count");
                var count = GetRestartCount() + 1;
                File.WriteAllText(countFile, count.ToString());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to increment restart count: {ex.Message}");
            }
        }

        /// <summary>
        /// Reset restart counter (call on successful startup)
        /// </summary>
        public static void ResetRestartCount()
        {
            try
            {
                var countFile = Path.Combine(DataDirectory, ".restart_count");
                if (File.Exists(countFile))
                {
                    File.Delete(countFile);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to reset restart count: {ex.Message}");
            }
        }

        /// <summary>
        /// Log restart-related messages
        /// </summary>
        private static void LogRestartMessage(string message)
        {
            try
            {
                Directory.CreateDirectory(DataDirectory);
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(RestartLogPath, logEntry);

                // Keep log file under 500KB
                var fileInfo = new FileInfo(RestartLogPath);
                if (fileInfo.Length > 512 * 1024)
                {
                    var lines = File.ReadAllLines(RestartLogPath);
                    var halfLines = lines.Length / 2;
                    File.WriteAllLines(RestartLogPath, lines[halfLines..]);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to write restart log: {ex.Message}");
            }
        }

        /// <summary>
        /// Get recent restart log entries
        /// </summary>
        public static string[] GetRecentLogEntries(int count = 20)
        {
            try
            {
                if (!File.Exists(RestartLogPath))
                    return Array.Empty<string>();

                var lines = File.ReadAllLines(RestartLogPath);
                if (lines.Length <= count)
                    return lines;

                return lines[^count..];
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to read restart log: {ex.Message}");
                return Array.Empty<string>();
            }
        }
    }
}
