using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ImageOrganizer
{
    public static class ConfigurationManager
    {
        public class AppSettings
        {
            public List<string> SupportedFormats { get; set; }
            public string DefaultExportPath { get; set; }
            public string DeviceImportFolder { get; set; }
        }

        public static AppSettings Settings { get; private set; }

        public static void LoadConfig()
        {
            string configPath = "appsettings.json";
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                Settings = JsonConvert.DeserializeObject<AppSettings>(json);
            }
            else
            {
                // Default configuration
                Settings = new AppSettings
                {
                    SupportedFormats = new List<string> { ".JPG", ".RAF" },
                    DefaultExportPath = "",
                    DeviceImportFolder = "" // Default to empty
                };
                SaveConfig();
            }
        }

        public static void SaveConfig()
        {
            string json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
            File.WriteAllText("appsettings.json", json);
        }
    }
}
