using System.IO;
using System.Text.Json;
using Komandio.Tools.Mobisync.Models;
using Serilog;

namespace Komandio.Tools.Mobisync.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly string _baseDir;
        private readonly string _settingsPath;
        private readonly string _locationsPath;
        private readonly string _customProcessorsPath;
        
        public string LogPath { get; set; } = string.Empty;
        public bool ReadFromBeginning { get; set; }
        public bool ShowReplayedLogs { get; set; } = false;
        public List<DynamicProcessorRule> CustomProcessors { get; set; } = new();
        public Dictionary<string, string> LocationMapping { get; private set; } = new();

        public SettingsService()
        {
            _baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Komandio", "Tools", "Mobisync");
            if (!Directory.Exists(_baseDir)) Directory.CreateDirectory(_baseDir);

            _settingsPath = Path.Combine(_baseDir, "settings.json");
            _locationsPath = Path.Combine(_baseDir, "locations.json");
            _customProcessorsPath = Path.Combine(_baseDir, "custom_processors.json");

            // Extract default locations from embedded resources if missing from AppData
            if (!File.Exists(_locationsPath))
            {
                ExtractEmbeddedLocations();
            }

            Load();
        }

        private void ExtractEmbeddedLocations()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceName = "Komandio.Tools.Mobisync.Data.locations.json";
                using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    using var reader = new BinaryReader(stream);
                    File.WriteAllBytes(_locationsPath, reader.ReadBytes((int)stream.Length));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Settings: Failed to extract embedded locations.json");
            }
        }

        public void Load()
        {
            try
            {
                CustomProcessors.Clear();
                
                // Ensure directory exists
                if (!Directory.Exists(_baseDir)) Directory.CreateDirectory(_baseDir);

                // 1. Load or Create Default settings.json
                if (File.Exists(_settingsPath))
                {
                    var json = File.ReadAllText(_settingsPath);
                    var data = JsonSerializer.Deserialize<SettingsData>(json);
                    if (data != null)
                    {
                        LogPath = data.LastLogPath;
                        ReadFromBeginning = data.ReadFromBeginning;
                        ShowReplayedLogs = data.ShowReplayedLogs;
                    }
                }
                else
                {
                    // Create defaults as requested
                    ReadFromBeginning = true;
                    ShowReplayedLogs = true;
                    Save(); // Trigger initial save to create file
                }

                // 2. Load or Create Default custom_processors.json
                if (File.Exists(_customProcessorsPath))
                {
                    var json = File.ReadAllText(_customProcessorsPath);
                    var processors = JsonSerializer.Deserialize<List<DynamicProcessorRule>>(json);
                    if (processors != null)
                    {
                        foreach (var rule in processors)
                        {
                            rule.IsBuiltIn = false;
                            CustomProcessors.Add(rule);
                        }
                    }
                }
                else
                {
                    // Create empty default file
                    File.WriteAllText(_customProcessorsPath, "[]");
                }

                // 3. Load Location Mappings
                if (File.Exists(_locationsPath))
                {
                    var json = File.ReadAllText(_locationsPath);
                    var data = JsonSerializer.Deserialize<LocationData>(json);
                    if (data?.STATION_MAPPING != null)
                    {
                        LocationMapping = data.STATION_MAPPING;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Settings: Error loading settings from {Path}", _baseDir);
            }
        }

        public void Save()
        {
            try
            {
                // Save settings.json
                var data = new SettingsData
                {
                    LastLogPath = LogPath,
                    ReadFromBeginning = ReadFromBeginning,
                    ShowReplayedLogs = ShowReplayedLogs
                };
                var jsonSettings = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, jsonSettings);

                // Save custom_processors.json
                var jsonProcessors = JsonSerializer.Serialize(CustomProcessors, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_customProcessorsPath, jsonProcessors);

                Log.Debug("Settings: Saved to {Path}", _baseDir);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Settings: Error saving settings");
            }
        }

        private class SettingsData
        {
            public string LastLogPath { get; set; } = "";
            public bool ReadFromBeginning { get; set; }
            public bool ShowReplayedLogs { get; set; } = false;
        }

        private class LocationData
        {
            public Dictionary<string, string>? STATION_MAPPING { get; set; }
        }
    }
}