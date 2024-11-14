using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace ImageOrganizer
{
    public static class ConfigurationManager
    {
        public static AppSettings Settings { get; set; }
        private static string configFilePath = "config.json";

        // Added back the CurrentVersion property
        public static readonly string CurrentVersion = "1.2.0"; // Update this with each release

        public static void LoadConfig()
        {
            if (File.Exists(configFilePath))
            {
                string json = File.ReadAllText(configFilePath);
                Settings = JsonConvert.DeserializeObject<AppSettings>(json);
            }
            else
            {
                Settings = new AppSettings
                {
                    SupportedFormats = GetAllCommonFormats(),
                    DefaultExportPath = "",
                    DeviceImportFolder = "",
                    LastUpdateCheck = DateTime.MinValue,
                    SkippedVersion = null
                };
                SaveConfig();
            }
        }

        public static void SaveConfig()
        {
            string json = JsonConvert.SerializeObject(Settings, Formatting.Indented);
            File.WriteAllText(configFilePath, json);
        }

        private static List<string> GetAllCommonFormats()
        {
            var compressedFormats = new List<string>
            {
                ".JPG", ".JPEG", ".PNG", ".BMP", ".GIF", ".TIFF", ".TIF", ".WEBP", ".HEIC"
            };
            var rawFormats = new List<string>
            {
                ".RAW", ".RAF", ".CR2", ".NEF", ".DNG", ".ARW", ".SR2", ".ORF", ".PEF", ".SRW", ".RW2"
            };
            var allFormats = new List<string>();
            allFormats.AddRange(compressedFormats);
            allFormats.AddRange(rawFormats);
            return allFormats;
        }
    }

    public class AppSettings
    {
        public List<string> SupportedFormats { get; set; }
        public string DefaultExportPath { get; set; }
        public string DeviceImportFolder { get; set; }
        public DateTime LastUpdateCheck { get; set; }
        public string SkippedVersion { get; set; }
    }
}
