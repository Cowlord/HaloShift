using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace HaloShift
{
    /// <summary>
    /// Handles crash detection and automatic recovery for Steam Link scenarios
    /// </summary>
    public static class CrashHandler
    {
        private const string LOCK_FILE_NAME = "haloshift.lock";
        private const string CRASH_LOG_NAME = "crash.log";
        private static readonly string LockFilePath = Path.Combine(AppContext.BaseDirectory, LOCK_FILE_NAME);
        private static readonly string CrashLogPath = Path.Combine(AppContext.BaseDirectory, CRASH_LOG_NAME);
        private static Timer? _heartbeatTimer;
        private static readonly object _lockObject = new object();
        private static bool _isShuttingDown = false;

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(CtrlHandler handler, bool add);

        private delegate bool CtrlHandler(CtrlTypes sig);

        private enum CtrlTypes : uint
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        static CrashHandler()
        {
            // Set up console control handler for graceful shutdown
            SetConsoleCtrlHandler(Handler, true);
        }

        /// <summary>
        /// Initialize crash detection and heartbeat system
        /// </summary>
        public static void Initialize()
        {
            // Check if we're recovering from a crash
            CheckForPreviousCrash();

            // Create lock file to indicate we're running
            CreateLockFile();

            // Start heartbeat timer (updates every 5 seconds)
            _heartbeatTimer = new Timer(UpdateHeartbeat, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

            // Log successful startup
            LogMessage($"HaloShift started successfully at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        }

        /// <summary>
        /// Clean shutdown - removes lock file and stops heartbeat
        /// </summary>
        public static void Shutdown()
        {
            lock (_lockObject)
            {
                if (_isShuttingDown) return;
                _isShuttingDown = true;
            }

            LogMessage($"HaloShift shutting down gracefully at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            
            _heartbeatTimer?.Dispose();
            RemoveLockFile();
        }

        /// <summary>
        /// Check if previous instance crashed and attempt recovery
        /// </summary>
        private static void CheckForPreviousCrash()
        {
            if (File.Exists(LockFilePath))
            {
                try
                {
                    var lockTime = File.GetLastWriteTime(LockFilePath);
                    var timeSinceLock = DateTime.Now - lockTime;

                    // If lock file is older than 30 seconds, assume crash
                    if (timeSinceLock.TotalSeconds > 30)
                    {
                        LogMessage($"CRASH DETECTED: Previous instance crashed {timeSinceLock.TotalMinutes:F1} minutes ago");
                        
                        // Attempt to clean up any orphaned processes
                        CleanupOrphanedProcesses();
                        
                        // Log crash details
                        LogCrashDetails(lockTime);
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Error checking for previous crash: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Create/update lock file with current timestamp
        /// </summary>
        private static void CreateLockFile()
        {
            try
            {
                File.WriteAllText(LockFilePath, $"{Process.GetCurrentProcess().Id}|{DateTime.Now:O}");
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to create lock file: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove lock file on clean shutdown
        /// </summary>
        private static void RemoveLockFile()
        {
            try
            {
                if (File.Exists(LockFilePath))
                {
                    File.Delete(LockFilePath);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to remove lock file: {ex.Message}");
            }
        }

        /// <summary>
        /// Update heartbeat timestamp in lock file
        /// </summary>
        private static void UpdateHeartbeat(object? state)
        {
            lock (_lockObject)
            {
                if (_isShuttingDown) return;
            }

            try
            {
                CreateLockFile();
            }
            catch (Exception ex)
            {
                LogMessage($"Heartbeat update failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Look for and clean up orphaned HaloShift processes
        /// </summary>
        private static void CleanupOrphanedProcesses()
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var currentPid = currentProcess.Id;

                foreach (var process in Process.GetProcessesByName("HaloShift"))
                {
                    if (process.Id != currentPid)
                    {
                        try
                        {
                            // Check if process has been running without heartbeat updates
                            if (process.StartTime < DateTime.Now.AddMinutes(-5))
                            {
                                LogMessage($"Terminating orphaned process {process.Id} (started {process.StartTime})");
                                process.Kill();
                                process.WaitForExit(5000);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"Failed to terminate process {process.Id}: {ex.Message}");
                        }
                        finally
                        {
                            process.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error during cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// Log crash details for diagnostics
        /// </summary>
        private static void LogCrashDetails(DateTime crashTime)
        {
            try
            {
                var crashInfo = $@"
=== CRASH REPORT ===
Crash Time: {crashTime:yyyy-MM-dd HH:mm:ss}
Detection Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}
System: {Environment.OSVersion}
.NET Version: {Environment.Version}
Working Directory: {Environment.CurrentDirectory}
Process ID: {Process.GetCurrentProcess().Id}
===================";

                LogMessage(crashInfo);
            }
            catch (Exception ex)
            {
                LogMessage($"Failed to log crash details: {ex.Message}");
            }
        }

        /// <summary>
        /// Write message to crash log
        /// </summary>
        private static void LogMessage(string message)
        {
            try
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(CrashLogPath, logEntry);

                // Keep log file under 1MB
                var fileInfo = new FileInfo(CrashLogPath);
                if (fileInfo.Length > 1024 * 1024)
                {
                    var lines = File.ReadAllLines(CrashLogPath);
                    var halfLines = lines.Length / 2;
                    File.WriteAllLines(CrashLogPath, lines[halfLines..]);
                }
            }
            catch
            {
                // Silently fail if we can't write to log
            }
        }

        /// <summary>
        /// Console control handler for graceful shutdown
        /// </summary>
        private static bool Handler(CtrlTypes sig)
        {
            LogMessage($"Received shutdown signal: {sig}");
            Shutdown();
            return false; // Let default handler also run
        }

        /// <summary>
        /// Get recent crash log entries for diagnostics
        /// </summary>
        public static string[] GetRecentLogEntries(int count = 50)
        {
            try
            {
                if (!File.Exists(CrashLogPath))
                    return Array.Empty<string>();

                var lines = File.ReadAllLines(CrashLogPath);
                if (lines.Length <= count)
                    return lines;

                return lines[^count..];
            }
            catch
            {
                return Array.Empty<string>();
            }
        }
    }
}
