using System;
using System.IO;
using System.Linq;

namespace EnterpriseLicenseDeployer.Services
{
    public class LicenseService
    {
        /// <summary>
        /// Looks inside the license root folder for a sub-folder whose name matches
        /// the given MAC address. Accepts common MAC formats regardless of separator
        /// (AA-BB-CC-DD-EE-FF, AA:BB:CC:DD:EE:FF, AABBCCDDEEFF) and is case-insensitive.
        /// </summary>
        public string? FindMatchingLicenseFolder(string licenseRootPath, string macAddress)
        {
            if (string.IsNullOrWhiteSpace(licenseRootPath) || !Directory.Exists(licenseRootPath))
                return null;

            var normalizedTarget = Normalize(macAddress);

            var match = Directory.GetDirectories(licenseRootPath)
                .FirstOrDefault(dir => Normalize(Path.GetFileName(dir)) == normalizedTarget);

            return match;
        }

        private static string Normalize(string mac) =>
            mac.Replace("-", "").Replace(":", "").Trim().ToUpperInvariant();

        /// <summary>
        /// Copies every file/sub-folder from the matched license folder into each of
        /// the configured destination folders, overwriting existing files.
        /// Returns the number of destination folders successfully updated.
        /// </summary>
        public int DeployToDestinations(string sourceLicenseFolder, System.Collections.Generic.IEnumerable<string> destinationFolders)
        {
            int successCount = 0;

            foreach (var destination in destinationFolders)
            {
                if (string.IsNullOrWhiteSpace(destination))
                    continue;

                try
                {
                    Directory.CreateDirectory(destination);
                    CopyDirectoryRecursive(sourceLicenseFolder, destination);
                    AuditLogger.Instance.Log("INFO", $"License files copied to '{destination}'.");
                    successCount++;
                }
                catch (Exception ex)
                {
                    AuditLogger.Instance.Log("ERROR", $"Failed copying license to '{destination}': {ex.Message}");
                }
            }

            return successCount;
        }

        private static void CopyDirectoryRecursive(string sourceDir, string destDir)
        {
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, overwrite: true);
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
                Directory.CreateDirectory(destSubDir);
                CopyDirectoryRecursive(subDir, destSubDir);
            }
        }
    }
}
