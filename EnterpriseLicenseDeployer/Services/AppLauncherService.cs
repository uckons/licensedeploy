using System;
using System.Diagnostics;
using System.IO;

namespace EnterpriseLicenseDeployer.Services
{
    public class AppLauncherService
    {
        /// <summary>
        /// Launches each configured application path. Skips blanks and logs
        /// missing executables instead of throwing.
        /// </summary>
        public int LaunchAll(System.Collections.Generic.IEnumerable<string> applicationPaths)
        {
            int launched = 0;

            foreach (var path in applicationPaths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                try
                {
                    if (!File.Exists(path))
                    {
                        AuditLogger.Instance.Log("WARN", $"Application not found, skipped: '{path}'");
                        continue;
                    }

                    var psi = new ProcessStartInfo
                    {
                        FileName = path,
                        WorkingDirectory = Path.GetDirectoryName(path) ?? "",
                        UseShellExecute = true
                    };

                    Process.Start(psi);
                    AuditLogger.Instance.Log("INFO", $"Application launched: '{path}'");
                    launched++;
                }
                catch (Exception ex)
                {
                    AuditLogger.Instance.Log("ERROR", $"Failed to launch '{path}': {ex.Message}");
                }
            }

            return launched;
        }

        /// <summary>
        /// Closes running instances of each configured application before the next deployment run.
        /// </summary>
        public int CloseAll(System.Collections.Generic.IEnumerable<string> applicationPaths)
        {
            int closed = 0;

            foreach (var path in applicationPaths)
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                var processName = Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrWhiteSpace(processName))
                    continue;

                foreach (var process in Process.GetProcessesByName(processName))
                {
                    using (process)
                    {
                        try
                        {
                            if (!MatchesConfiguredPath(process, path))
                            {
                                AuditLogger.Instance.Log("WARN", $"Running process '{process.ProcessName}' skipped because its path does not match '{path}'.");
                                continue;
                            }

                            if (!process.HasExited)
                            {
                                if (process.CloseMainWindow())
                                    process.WaitForExit(10000);

                                if (!process.HasExited)
                                {
                                    process.Kill(entireProcessTree: true);
                                    process.WaitForExit(5000);
                                }
                            }

                            if (process.HasExited)
                            {
                                AuditLogger.Instance.Log("INFO", $"Application closed: '{path}'");
                                closed++;
                            }
                        }
                        catch (Exception ex)
                        {
                            AuditLogger.Instance.Log("ERROR", $"Failed to close '{path}' (PID {process.Id}): {ex.Message}");
                        }
                    }
                }
            }

            return closed;
        }

        private static bool MatchesConfiguredPath(Process process, string configuredPath)
        {
            try
            {
                var runningPath = process.MainModule?.FileName;
                return string.Equals(
                    Path.GetFullPath(runningPath ?? string.Empty),
                    Path.GetFullPath(configuredPath),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }
    }
}
