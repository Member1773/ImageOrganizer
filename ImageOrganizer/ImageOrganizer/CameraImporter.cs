using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaDevices;
using Spectre.Console;

namespace ImageOrganizer
{
    public static class CameraImporter
    {
        public static void ImportImagesFromCamera()
        {
            // List connected devices
            var devices = MediaDevice.GetDevices().ToList();

            if (!devices.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No media devices found. Ensure your camera is connected and turned on.[/]");
                Console.ReadKey();
                return;
            }

            var deviceNames = devices.Select(d => d.FriendlyName).ToList();

            var selectedDeviceName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Select a device to import from:[/]")
                    .AddChoices(deviceNames));

            var selectedDevice = devices.First(d => d.FriendlyName == selectedDeviceName);

            AnsiConsole.MarkupLine($"[green]Selected device:[/] [yellow]{selectedDevice.FriendlyName}[/]");

            var selectedMode = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Select import mode:[/]")
                    .AddChoices(new[] { "Automatic", "Manually", "Go back" }));

            if (selectedMode == "Go back")
            {
                return;
            }

            try
            {
                using (selectedDevice)
                {
                    selectedDevice.Connect();

                    if (selectedMode == "Automatic")
                    {
                        ImportImages(selectedDevice, automatic: true);
                    }
                    else if (selectedMode == "Manually")
                    {
                        ImportImages(selectedDevice, automatic: false);
                    }
                }
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                Console.ReadKey();
            }
        }

        private static void ImportImages(MediaDevice device, bool automatic)
        {
            List<string> mediaFiles;

            if (automatic)
            {
                mediaFiles = AutomaticSearch(device);
            }
            else
            {
                mediaFiles = ManualSearch(device);
            }

            if (mediaFiles == null || !mediaFiles.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No supported image files found to import.[/]");
                Console.ReadKey();
                return;
            }

            // Ensure Buffer folder exists in DefaultExportPath
            string bufferPath = Path.Combine(ConfigurationManager.Settings.DefaultExportPath, "Buffer");
            Directory.CreateDirectory(bufferPath);

            ImportFiles(device, mediaFiles, bufferPath);

            AnsiConsole.MarkupLine("[green]Importing complete. Press any key to sort the imported images.[/]");
            Console.ReadKey();

            // Now sort the images in the buffer folder
            ImageProcessor.SortImagesInSpecificFolder(bufferPath);
        }

        private static List<string> AutomaticSearch(MediaDevice device)
        {
            AnsiConsole.MarkupLine("[green]Scanning device for image directories... This may take a while...[/]");
            List<ImageDirectoryInfo> directoriesWithImages = new List<ImageDirectoryInfo>();

            AnsiConsole.Status()
                .Start("Scanning device for image directories...", ctx =>
                {
                    directoriesWithImages = GetDirectoriesWithImages(device);
                });

            if (!directoriesWithImages.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No directories containing images were found on the device.[/]");
                Console.ReadKey();
                return null;
            }

            // Prepare choices for the prompt
            var directoryChoices = directoriesWithImages
                .Select(d => $"{d.DirectoryPath} ({d.ImageCount} images)")
                .ToList();

            // Present directories to the user for selection
            var selectedDirectoryChoices = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("[green]Select directories to import images from:[/]")
                    .PageSize(10)
                    .AddChoices(directoryChoices));

            if (!selectedDirectoryChoices.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No directories selected. Operation cancelled.[/]");
                Console.ReadKey();
                return null;
            }

            // Map selected choices back to directory paths
            var selectedDirectories = directoriesWithImages
                .Where(d => selectedDirectoryChoices.Contains($"{d.DirectoryPath} ({d.ImageCount} images)"))
                .Select(d => d.DirectoryPath)
                .ToList();

            // Collect image files from selected directories
            var mediaFiles = new List<string>();
            foreach (var dir in selectedDirectories)
            {
                var filesInDir = device.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories)
                    .Where(s => ConfigurationManager.Settings.SupportedFormats.Contains(Path.GetExtension(s).ToUpperInvariant()))
                    .ToList();

                mediaFiles.AddRange(filesInDir);
            }

            return mediaFiles;
        }

        private static List<string> ManualSearch(MediaDevice device)
        {
            string photosFolder = ConfigurationManager.Settings.DeviceImportFolder;

            if (string.IsNullOrEmpty(photosFolder) || !device.DirectoryExists(photosFolder))
            {
                // Prompt the user to select a folder on the device
                photosFolder = SelectDeviceFolder(device);
                if (photosFolder == null)
                {
                    AnsiConsole.MarkupLine("[yellow]No folder selected. Operation cancelled.[/]");
                    Console.ReadKey();
                    return null;
                }
            }

            // Get list of image files on the device
            var mediaFiles = device.EnumerateFiles(photosFolder, "*.*", SearchOption.AllDirectories)
                .Where(s => ConfigurationManager.Settings.SupportedFormats.Contains(Path.GetExtension(s).ToUpperInvariant()))
                .ToList();

            return mediaFiles;
        }

        private static void ImportFiles(MediaDevice device, List<string> mediaFiles, string bufferPath)
        {
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
                var task = ctx.AddTask("[green]Importing images[/]", true, mediaFiles.Count);

                foreach (var file in mediaFiles)
                {
                    try
                    {
                        string fileName = Path.GetFileName(file);
                        string destinationPath = Path.Combine(bufferPath, fileName);

                        // Handle potential file name collisions
                        destinationPath = Utilities.GetUniqueFilePath(destinationPath);

                        // Download the file to buffer folder
                        device.DownloadFile(file, destinationPath);

                        task.Increment(1);
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Error importing file {file}: {ex.Message}[/]");
                        // Optionally log the error
                    }
                }
            });
        }

        private static string SelectDeviceFolder(MediaDevice device)
        {
            // Start from the root directory
            string currentPath = @"\";

            while (true)
            {
                var directories = device.EnumerateDirectories(currentPath).ToList();

                if (!directories.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]No subdirectories found in the current directory.[/]");
                    // Allow the user to go back or select current directory
                    var choices = new List<string> { "Select this folder", "Go back", "Cancel" };
                    var choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("[green]What would you like to do?[/]")
                            .AddChoices(choices));

                    if (choice == "Select this folder")
                    {
                        return currentPath;
                    }
                    else if (choice == "Go back")
                    {
                        if (currentPath != @"\")
                        {
                            currentPath = Path.GetDirectoryName(currentPath);
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    // Present the directories to the user
                    var choices = directories.Select(d => Path.GetFileName(d)).ToList();
                    choices.Add("Select this folder");
                    if (currentPath != @"\")
                    {
                        choices.Add("Go back");
                    }
                    choices.Add("Cancel");

                    var choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title($"[green]Current directory:[/] {currentPath}\n[green]Select a subdirectory:[/]")
                            .AddChoices(choices));

                    if (choice == "Select this folder")
                    {
                        return currentPath;
                    }
                    else if (choice == "Go back")
                    {
                        if (currentPath != @"\")
                        {
                            currentPath = Path.GetDirectoryName(currentPath);
                        }
                    }
                    else if (choice == "Cancel")
                    {
                        return null;
                    }
                    else
                    {
                        // Navigate into the selected directory
                        if (currentPath != null) currentPath = Path.Combine(currentPath, choice);
                    }
                }
            }
        }

        // Class to hold directory information
        private class ImageDirectoryInfo
        {
            public string DirectoryPath { get; set; }
            public int ImageCount { get; set; }
        }

        private static List<ImageDirectoryInfo> GetDirectoriesWithImages(MediaDevice device)
        {
            var directoriesWithImages = new List<ImageDirectoryInfo>();
            var dirsToProcess = new Queue<string>();
            dirsToProcess.Enqueue(@"\");

            while (dirsToProcess.Count > 0)
            {
                string currentDir = dirsToProcess.Dequeue();
                try
                {
                    // Enqueue subdirectories
                    var subDirs = device.EnumerateDirectories(currentDir).ToList();
                    foreach (var subDir in subDirs)
                    {
                        dirsToProcess.Enqueue(subDir);
                    }

                    // Check for image files in current directory
                    var filesInDir = device.EnumerateFiles(currentDir, "*.*")
                        .Where(s => ConfigurationManager.Settings.SupportedFormats.Contains(Path.GetExtension(s).ToUpperInvariant()))
                        .ToList();

                    if (filesInDir.Any())
                    {
                        directoriesWithImages.Add(new ImageDirectoryInfo
                        {
                            DirectoryPath = currentDir,
                            ImageCount = filesInDir.Count
                        });
                    }
                }
                catch
                {
                    // Handle exceptions silently (e.g., access denied)
                }
            }

            // Remove duplicates
            var distinctDirectories = directoriesWithImages
                .GroupBy(d => d.DirectoryPath)
                .Select(g => g.First())
                .ToList();

            return distinctDirectories;
        }
    }
}
