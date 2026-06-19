using System;
using System.IO;

namespace HaloShift
{
    internal sealed class FileLogger
    {
        private readonly string _logFilePath;
        private readonly long _maxSizeBytes;

        public FileLogger(string logFilePath, long maxSizeBytes = 1024 * 1024)
        {
            _logFilePath = logFilePath;
            _maxSizeBytes = maxSizeBytes;
        }

        public void Log(string message)
        {
            try
            {
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
                File.AppendAllText(_logFilePath, logEntry);

                var fileInfo = new FileInfo(_logFilePath);
                if (fileInfo.Length > _maxSizeBytes)
                {
                    var lines = File.ReadAllLines(_logFilePath);
                    var halfLines = lines.Length / 2;
                    File.WriteAllLines(_logFilePath, lines[halfLines..]);
                }
            }
            catch
            {
            }
        }

        public string[] GetRecentEntries(int count)
        {
            try
            {
                if (!File.Exists(_logFilePath))
                    return Array.Empty<string>();

                var lines = File.ReadAllLines(_logFilePath);
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
