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

        private static readonly string RestartLogPath = Path.Combine(AppContext.BaseDirectory, RESTART_LOG_NAME);
        private static readonly FileLogger _logger = new(RestartLogPath, 512 * 1024);
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
                    _logger.Log("Watcher process started");
                }
                catch (Exception ex)
                {
                    _logger.Log($"Failed to start watcher: {ex.Message}");
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
                _logger.Log("Self-watcher process started");
            }
            catch (Exception ex)
            {
                _logger.Log($"Failed to start self-watcher: {ex.Message}");
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
                _logger.Log($"Watching process {parentProcessId} ({parentProcess.ProcessName})");

                // Wait for parent process to exit
                parentProcess.WaitForExit();

                _logger.Log($"Parent process {parentProcessId} exited, checking for crash...");

                // Check if it was a crash (unclean exit)
                if (WasCrashExit())
                {
                    _logger.Log("Crash detected, attempting restart...");
                    RestartApplication();
                }
                else
                {
                    _logger.Log("Clean exit detected, not restarting");
                }
            }
            catch (ArgumentException)
            {
                _logger.Log($"Parent process {parentProcessId} not found, assuming crash...");
                RestartApplication();
            }
            catch (Exception ex)
            {
                _logger.Log($"Watcher error: {ex.Message}");
                RestartApplication();
            }
        }

        /// <summary>
        /// Run as self-watcher using command line argument
        /// </summary>
        public static void RunSelfWatcher()
        {
            _logger.Log("Self-watcher mode activated");

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
                _logger.Log("No main process found to watch, exiting");
                return;
            }

            try
            {
                _logger.Log($"Watching main process {mainProcess.Id}");
                mainProcess.WaitForExit();

                _logger.Log("Main process exited, checking for crash...");
                Thread.Sleep(2000); // Brief delay to ensure lock file is updated

                if (WasCrashExit())
                {
                    _logger.Log("Crash detected, attempting restart...");
                    RestartApplication();
                }
                else
                {
                    _logger.Log("Clean exit detected, not restarting");
                }
            }
            catch (Exception ex)
            {
                _logger.Log($"Self-watcher error: {ex.Message}");
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
                var lockFilePath = Path.Combine(AppContext.BaseDirectory, "haloshift.lock");
                if (!File.Exists(lockFilePath))
                    return false; // Clean shutdown removed lock file

                var lockTime = File.GetLastWriteTime(lockFilePath);
                var timeSinceLock = DateTime.Now - lockTime;

                // If lock file hasn't been updated recently, it's a crash
                return timeSinceLock.TotalSeconds > 15;
            }
            catch (Exception ex)
            {
                _logger.Log($"Failed to check crash exit status: {ex.Message}");
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
                _logger.Log($"Maximum restart attempts ({MAX_RESTART_ATTEMPTS}) reached, giving up");
                return;
            }

            IncrementRestartCount();
            _logger.Log($"Restart attempt {restartCount + 1}/{MAX_RESTART_ATTEMPTS}");

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
                _logger.Log($"Restarted successfully: {CurrentExecutablePath}");
            }
            catch (Exception ex)
            {
                _logger.Log($"Restart failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Get current restart count from registry/file
        /// </summary>
        private static int GetRestartCount()
        {
            try
            {
                var countFile = Path.Combine(AppContext.BaseDirectory, ".restart_count");
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
                var countFile = Path.Combine(AppContext.BaseDirectory, ".restart_count");
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
                var countFile = Path.Combine(AppContext.BaseDirectory, ".restart_count");
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

        public static string[] GetRecentLogEntries(int count = 20) =>
            _logger.GetRecentEntries(count);
    }
}
