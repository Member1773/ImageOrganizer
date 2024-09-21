using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace ImageOrganizer
{
    public static class Utilities
    {
        public static string SelectFolderDialog()
        {
            string folderPath = null;
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select a folder";
                // The property 'UseDescriptionForTitle' is deprecated and no longer needs to be set

                // Show the dialog
                DialogResult result = folderDialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                {
                    folderPath = folderDialog.SelectedPath;
                }
            }
            return folderPath;
        }

        public static string GetDateTaken(string filePath)
        {
            try
            {
                // Read metadata using MetadataExtractor
                var directories = ImageMetadataReader.ReadMetadata(filePath);
                var subIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();

                if (subIfdDirectory != null && subIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out DateTime dateTaken))
                {
                    return dateTaken.ToString("yyyy-MM-dd");
                }
            }
            catch
            {
                // Handle exceptions silently and fall back to file creation date
            }

            // Fallback to file creation date
            DateTime creationDate = File.GetCreationTime(filePath);
            return creationDate.ToString("yyyy-MM-dd");
        }
    }
}
