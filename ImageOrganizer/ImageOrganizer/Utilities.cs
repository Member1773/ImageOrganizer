using System;
using System.IO;
using System.Linq;
using System.Threading;
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
            var thread = new Thread(() =>
            {
                using (var folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Select a folder";

                    // Show the dialog
                    DialogResult result = folderDialog.ShowDialog();

                    if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                    {
                        folderPath = folderDialog.SelectedPath;
                    }
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

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
        
        public static string GetUniqueFilePath(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            int count = 1;
            string newFullPath = filePath;

            while (File.Exists(newFullPath))
            {
                string tempFileName = $"{fileName}({count++})";
                if (directory != null) newFullPath = Path.Combine(directory, tempFileName + extension);
            }

            return newFullPath;
        }

    }
}
