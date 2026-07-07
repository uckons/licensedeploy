using System;
using System.IO;
using EnterpriseLicenseDeployer.Models;

namespace EnterpriseLicenseDeployer.Services
{
    /// <summary>
    /// Simple thread-safe audit logger. Writes to a daily rolling log file under
    /// %ProgramData%\EnterpriseLicenseDeployer\Logs and raises an event so the
    /// UI can mirror every line into the on-screen audit box.
    /// </summary>
    public sealed class AuditLogger
    {
        private static readonly Lazy<AuditLogger> _instance = new(() => new AuditLogger());
        public static AuditLogger Instance => _instance.Value;

        private readonly object _lock = new();
        private string _logDirectory;

        public event Action<string>? LineWritten;

        private AuditLogger()
        {
            _logDirectory = AuditLogDefaults.DefaultLogFolderPath;
            Directory.CreateDirectory(_logDirectory);
        }

        public void ConfigureLogDirectory(string? logDirectory)
        {
            var normalizedDirectory = string.IsNullOrWhiteSpace(logDirectory)
                ? AuditLogDefaults.DefaultLogFolderPath
                : Environment.ExpandEnvironmentVariables(logDirectory.Trim());

            lock (_lock)
            {
                Directory.CreateDirectory(normalizedDirectory);
                _logDirectory = normalizedDirectory;
            }
        }

        public void Log(string level, string message)
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\t[{level}]\t{message}";

            lock (_lock)
            {
                var file = Path.Combine(_logDirectory, $"audit_{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(file, line + Environment.NewLine);
            }

            LineWritten?.Invoke(line);
        }

        public string LogDirectory => _logDirectory;
    }
}
