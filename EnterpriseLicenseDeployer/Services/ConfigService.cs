using System;
using System.IO;
using System.Text.Json;
using EnterpriseLicenseDeployer.Models;

namespace EnterpriseLicenseDeployer.Services
{
    /// <summary>
    /// Loads and saves AppConfig to a JSON file that lives next to the executable
    /// (in %ProgramData% for a machine-wide, write-protected-by-default location).
    /// </summary>
    public class ConfigService
    {
        private static readonly string ConfigDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "EnterpriseLicenseDeployer");

        private static readonly string ConfigPath = Path.Combine(ConfigDirectory, "config.json");

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public AppConfig Load()
        {
            try
            {
                if (!Directory.Exists(ConfigDirectory))
                    Directory.CreateDirectory(ConfigDirectory);

                if (!File.Exists(ConfigPath))
                {
                    // Seed from appsettings.json shipped alongside the exe, if present.
                    var seedPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                    var config = File.Exists(seedPath)
                        ? JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(seedPath)) ?? new AppConfig()
                        : new AppConfig();

                    config.EnsureListSizes();
                    Save(config);
                    return config;
                }

                var json = File.ReadAllText(ConfigPath);
                var loaded = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                loaded.EnsureListSizes();
                return loaded;
            }
            catch (Exception ex)
            {
                AuditLogger.Instance.Log("ERROR", $"Failed to load config: {ex.Message}");
                var fallback = new AppConfig();
                fallback.EnsureListSizes();
                return fallback;
            }
        }

        public void Save(AppConfig config)
        {
            try
            {
                if (!Directory.Exists(ConfigDirectory))
                    Directory.CreateDirectory(ConfigDirectory);

                config.EnsureListSizes();
                var json = JsonSerializer.Serialize(config, JsonOptions);
                File.WriteAllText(ConfigPath, json);
                AuditLogger.Instance.Log("INFO", "Configuration saved.");
            }
            catch (Exception ex)
            {
                AuditLogger.Instance.Log("ERROR", $"Failed to save config: {ex.Message}");
                throw;
            }
        }

        public string GetConfigPath() => ConfigPath;
    }
}
