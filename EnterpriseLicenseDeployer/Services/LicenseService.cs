using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EnterpriseLicenseDeployer.Services
{
    public class LicenseService
    {
        private const string ValidMacIdKey = "Valid MAC ID";

        /// <summary>
        /// Looks inside every .lic file under the license root folder for a line like
        /// "Valid MAC ID = AA-BB-CC-DD-EE-FF". Accepts common MAC formats regardless
        /// of separator (AA-BB-CC-DD-EE-FF, AA:BB:CC:DD:EE:FF, AABBCCDDEEFF) and is
        /// case-insensitive.
        /// </summary>
        public List<string> FindMatchingLicenseFiles(string licenseRootPath, string macAddress)
        {
            var matches = new List<string>();

            if (string.IsNullOrWhiteSpace(licenseRootPath) || !Directory.Exists(licenseRootPath))
                return matches;

            var normalizedTarget = Normalize(macAddress);

            foreach (var file in Directory.EnumerateFiles(licenseRootPath, "*.lic", SearchOption.AllDirectories))
            {
                try
                {
                    if (FileContainsMatchingMac(file, normalizedTarget))
                        matches.Add(file);
                }
                catch (Exception ex)
                {
                    AuditLogger.Instance.Log("WARN", $"Could not read license file '{file}': {ex.Message}");
                }
            }

            return matches;
        }

        private static bool FileContainsMatchingMac(string filePath, string normalizedTarget)
        {
            foreach (var line in ReadTextLines(filePath))
            {
                var value = ExtractValidMacIdValue(line);
                if (value != null && Normalize(value) == normalizedTarget)
                    return true;
            }

            return false;
        }

        private static string? ExtractValidMacIdValue(string line)
        {
            var keyIndex = line.IndexOf(ValidMacIdKey, StringComparison.OrdinalIgnoreCase);
            if (keyIndex < 0)
                return null;

            var valueStartIndex = keyIndex + ValidMacIdKey.Length;
            var separatorIndex = line.IndexOfAny(new[] { '=', ':' }, valueStartIndex);
            if (separatorIndex >= 0)
                valueStartIndex = separatorIndex + 1;

            return line[valueStartIndex..].Trim();
        }

        private static string Normalize(string mac)
        {
            var hex = new string(mac.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
            return hex.Length > 12 ? hex[..12] : hex;
        }

        private static IEnumerable<string> ReadTextLines(string filePath)
        {
            var bytes = File.ReadAllBytes(filePath);

            if (bytes.Length >= 2)
            {
                if (bytes[0] == 0xFF && bytes[1] == 0xFE)
                    return File.ReadLines(filePath, Encoding.Unicode);

                if (bytes[0] == 0xFE && bytes[1] == 0xFF)
                    return File.ReadLines(filePath, Encoding.BigEndianUnicode);
            }

            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return File.ReadLines(filePath, Encoding.UTF8);

            var looksLikeUtf16Le = bytes.Length > 1 && bytes.Where((_, index) => index % 2 == 1).Take(32).Count(value => value == 0) > 8;
            if (looksLikeUtf16Le)
                return Encoding.Unicode.GetString(bytes).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            return Encoding.UTF8.GetString(bytes).Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        }

        /// <summary>
        /// Copies every matched license file into each configured destination folder,
        /// overwriting existing files with the same name. Returns the number of
        /// destination folders successfully updated.
        /// </summary>
        public int DeployFilesToDestinations(IEnumerable<string> sourceLicenseFiles, IEnumerable<string> destinationFolders)
        {
            var files = sourceLicenseFiles.Where(file => !string.IsNullOrWhiteSpace(file)).ToList();
            int successCount = 0;

            foreach (var destination in destinationFolders)
            {
                if (string.IsNullOrWhiteSpace(destination))
                    continue;

                try
                {
                    Directory.CreateDirectory(destination);

                    foreach (var file in files)
                    {
                        var destFile = Path.Combine(destination, Path.GetFileName(file));
                        File.Copy(file, destFile, overwrite: true);
                    }

                    AuditLogger.Instance.Log("INFO", $"{files.Count} license file(s) copied to '{destination}'.");
                    successCount++;
                }
                catch (Exception ex)
                {
                    AuditLogger.Instance.Log("ERROR", $"Failed copying license files to '{destination}': {ex.Message}");
                }
            }

            return successCount;
        }
    }
}
