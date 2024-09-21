using System;
using System.IO;
using System.Linq;
using Spectre.Console;

namespace ImageOrganizer
{
    public static class ImageProcessor
    {
        public static void SortImagesInFolder()
        {
            AnsiConsole.MarkupLine("[green]Please select the folder containing the images to sort.[/]");
            string folderPath = Utilities.SelectFolderDialog();

            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
            {
                AnsiConsole.MarkupLine("[red]Invalid folder path. Press any key to return to the menu.[/]");
                Console.ReadKey();
                return;
            }

            AnsiConsole.MarkupLine("[green]Processing files...[/]");

            string[] imageFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                                           .Where(s => ConfigurationManager.Settings.SupportedFormats.Contains(Path.GetExtension(s).ToUpperInvariant()))
                                           .ToArray();

            if (imageFiles.Length == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No supported image files found in the specified folder.[/]");
                AnsiConsole.MarkupLine("Press any key to return to the menu.");
                Console.ReadKey();
                return;
            }

            ProcessImages(imageFiles);

            AnsiConsole.MarkupLine("[green]Processing complete. Press any key to return to the menu.[/]");
            Console.ReadKey();
        }

        public static void SortImagesInSpecificFolder(string folderPath)
        {
            AnsiConsole.MarkupLine("[green]Processing files...[/]");

            string[] imageFiles = Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                                           .Where(s => ConfigurationManager.Settings.SupportedFormats.Contains(Path.GetExtension(s).ToUpperInvariant()))
                                           .ToArray();

            if (imageFiles.Length == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No supported image files found in the buffer folder.[/]");
                AnsiConsole.MarkupLine("Press any key to return to the menu.");
                Console.ReadKey();
                return;
            }

            ProcessImages(imageFiles);

            // Optionally, delete the buffer folder after sorting
            // Directory.Delete(folderPath, true);

            AnsiConsole.MarkupLine("[green]Processing complete. Press any key to return to the menu.[/]");
            Console.ReadKey();
        }

        private static void ProcessImages(string[] imageFiles)
        {
            int totalFiles = imageFiles.Length;

            var progress = AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn(),
                    new SpinnerColumn(),
                });

            progress.Start(ctx =>
            {
                var task = ctx.AddTask("[green]Sorting images[/]", true, totalFiles);

                foreach (var file in imageFiles)
                {
                    try
                    {
                        string dateTaken = Utilities.GetDateTaken(file);
                        string extension = Path.GetExtension(file).TrimStart('.').ToUpperInvariant();
                        string dateFolderName = dateTaken;

                        string dateFolderPath = Path.Combine(ConfigurationManager.Settings.DefaultExportPath, dateFolderName);
                        string targetSubFolder = Path.Combine(dateFolderPath, extension);

                        // Create directories if they do not exist
                        Directory.CreateDirectory(targetSubFolder);

                        string fileName = Path.GetFileName(file);
                        string destinationPath = Path.Combine(targetSubFolder, fileName);

                        // Move the file
                        File.Move(file, destinationPath);

                        task.Increment(1);
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Error processing file {file}: {ex.Message}[/]");
                        // Log the error or handle accordingly
                    }
                }
            });
        }
    }
}
