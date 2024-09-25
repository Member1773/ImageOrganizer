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
                // Default configuration with all common image formats selected
                Settings = new AppSettings
                {
                    SupportedFormats = GetAllCommonFormats(),
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

        private static List<string> GetAllCommonFormats()
        {
            // List of common compressed and raw image formats
            var compressedFormats = new List<string> { ".JPG", ".JPEG", ".PNG", ".BMP", ".GIF", ".TIFF" };
            var rawFormats = new List<string> { ".RAW", ".RAF", ".CR2", ".NEF", ".DNG", ".ARW", ".SR2", ".ORF", ".PEF" };

            var allFormats = new List<string>();
            allFormats.AddRange(compressedFormats);
            allFormats.AddRange(rawFormats);

            return allFormats;
        }
    }
}
