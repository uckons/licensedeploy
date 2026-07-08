using System;
using EnterpriseLicenseDeployer.Models;

namespace EnterpriseLicenseDeployer.Services
{
    public class RoutineResult
    {
        public bool Success { get; set; }
        public string? ActiveIp { get; set; }
        public string? ActiveMac { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Runs the full check -> match -> deploy -> launch routine.
    /// Used both by the manual "Run Now" button and by the daily scheduler.
    /// </summary>
    public class DeploymentOrchestrator
    {
        private readonly NetworkService _networkService = new();
        private readonly LicenseService _licenseService = new();
        private readonly AppLauncherService _appLauncherService = new();

        public RoutineResult Execute(AppConfig config)
        {
            AuditLogger.Instance.Log("INFO", "=== Routine started ===");

            var netInfo = _networkService.GetActiveNetworkInfo();
            if (netInfo == null)
            {
                var msg = "No active network adapter detected.";
                AuditLogger.Instance.Log("WARN", msg);
                return new RoutineResult { Success = false, Message = msg };
            }

            AuditLogger.Instance.Log("INFO", $"Active adapter: {netInfo.AdapterName}, IP: {netInfo.IpAddress}, MAC: {netInfo.MacAddress}");

            if (!string.Equals(netInfo.IpAddress, config.TargetIp, StringComparison.OrdinalIgnoreCase))
            {
                var msg = $"Active IP ({netInfo.IpAddress}) does not match configured IP ({config.TargetIp}). Routine aborted.";
                AuditLogger.Instance.Log("WARN", msg);
                return new RoutineResult
                {
                    Success = false,
                    ActiveIp = netInfo.IpAddress,
                    ActiveMac = netInfo.MacAddress,
                    Message = msg
                };
            }

            var matchedFiles = _licenseService.FindMatchingLicenseFiles(config.LicenseFolderPath, netInfo.MacAddress);
            if (matchedFiles.Count == 0)
            {
                var msg = $"No license file found with Valid MAC ID matching '{netInfo.MacAddress}' under '{config.LicenseFolderPath}'.";
                AuditLogger.Instance.Log("WARN", msg);
                return new RoutineResult
                {
                    Success = false,
                    ActiveIp = netInfo.IpAddress,
                    ActiveMac = netInfo.MacAddress,
                    Message = msg
                };
            }

            AuditLogger.Instance.Log("INFO", $"{matchedFiles.Count} license file match(es) found for MAC '{netInfo.MacAddress}'.");

            var copiedCount = _licenseService.DeployFilesToDestinations(matchedFiles, config.DestinationFolders);
            var launchedCount = _appLauncherService.LaunchAll(config.ApplicationPaths);

            var successMsg = $"Routine complete. Copied {matchedFiles.Count} matching license file(s) to {copiedCount}/{config.DestinationFolders.Count} destinations, launched {launchedCount}/{config.ApplicationPaths.Count} applications.";
            AuditLogger.Instance.Log("INFO", successMsg);
            AuditLogger.Instance.Log("INFO", "=== Routine finished ===");

            return new RoutineResult
            {
                Success = true,
                ActiveIp = netInfo.IpAddress,
                ActiveMac = netInfo.MacAddress,
                Message = successMsg
            };
        }
    }
}
