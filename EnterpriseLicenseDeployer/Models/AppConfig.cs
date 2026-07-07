using System.Collections.Generic;

namespace EnterpriseLicenseDeployer.Models
{
    /// <summary>
    /// Persisted application configuration. Everything the operator can change
    /// from inside the app (Settings screen) lives here.
    /// </summary>
    public class AppConfig
    {
        public const int DestinationFolderCount = 7;
        public const int ApplicationCount = 7;

        /// <summary>The IP address that must be active on this machine for the routine to run.</summary>
        public string TargetIp { get; set; } = "192.168.1.10";

        /// <summary>Root folder containing one sub-folder per licensed MAC address.</summary>
        public string LicenseFolderPath { get; set; } = @"C:\License";

        /// <summary>The 7 destination folders that receive the matched license files.</summary>
        public List<string> DestinationFolders { get; set; } = new()
        {
            "", "", "", "", "", "", ""
        };

        /// <summary>The 7 application executables launched after a successful deployment.</summary>
        public List<string> ApplicationPaths { get; set; } = new()
        {
            "", "", "", "", "", "", ""
        };

        /// <summary>Hour (24h) of the daily scheduled recheck.</summary>
        public int ScheduledHour { get; set; } = 6;

        /// <summary>Minute of the daily scheduled recheck.</summary>
        public int ScheduledMinute { get; set; } = 50;

        public void EnsureListSizes()
        {
            NormalizeList(DestinationFolders, DestinationFolderCount);
            NormalizeList(ApplicationPaths, ApplicationCount);
        }

        private static void NormalizeList(List<string> list, int size)
        {
            while (list.Count < size) list.Add(string.Empty);
            while (list.Count > size) list.RemoveAt(list.Count - 1);
        }
    }
}
