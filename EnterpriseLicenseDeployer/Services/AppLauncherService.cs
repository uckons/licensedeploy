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
    }
}
