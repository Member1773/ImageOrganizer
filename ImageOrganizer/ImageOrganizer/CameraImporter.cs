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
            var devices = MediaDevice.GetDevices();

            var deviceList = devices.ToList();

            if (!deviceList.Any())
            {
                AnsiConsole.MarkupLine("[yellow]No media devices found. Ensure your camera is connected and turned on.[/]");
                Console.ReadKey();
                return;
            }

            var deviceNames = deviceList.Select(d => d.FriendlyName).ToList();

            var selectedDeviceName = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[green]Select a device to import from:[/]")
                    .AddChoices(deviceNames));

            var selectedDevice = deviceList.First(d => d.FriendlyName == selectedDeviceName);

            using (selectedDevice)
            {
                try
                {
                    selectedDevice.Connect();

                    string photosFolder = ConfigurationManager.Settings.DeviceImportFolder;

                    if (string.IsNullOrEmpty(photosFolder) || !selectedDevice.DirectoryExists(photosFolder))
                    {
                        // Prompt the user to select a folder on the device
                        photosFolder = SelectDeviceFolder(selectedDevice);
                        if (photosFolder == null)
                        {
                            AnsiConsole.MarkupLine("[yellow]No folder selected. Operation cancelled.[/]");
                            Console.ReadKey();
                            return;
                        }
                    }

                    // Get list of image files on the device
                    var mediaFiles = selectedDevice.EnumerateFiles(photosFolder, "*.*", SearchOption.AllDirectories)
                        .Where(s => ConfigurationManager.Settings.SupportedFormats.Contains(Path.GetExtension(s).ToUpperInvariant()))
                        .ToList();

                    if (!mediaFiles.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No supported image files found in the selected folder.[/]");
                        Console.ReadKey();
                        return;
                    }

                    // Ensure Buffer folder exists in DefaultExportPath
                    string bufferPath = Path.Combine(ConfigurationManager.Settings.DefaultExportPath, "Buffer");
                    Directory.CreateDirectory(bufferPath);

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

                                // Download the file to buffer folder
                                selectedDevice.DownloadFile(file, destinationPath);

                                task.Increment(1);
                            }
                            catch (Exception ex)
                            {
                                AnsiConsole.MarkupLine($"[red]Error importing file {file}: {ex.Message}[/]");
                                // Log the error or handle accordingly
                            }
                        }
                    });

                    AnsiConsole.MarkupLine("[green]Importing complete. Press any key to sort the imported images.[/]");
                    Console.ReadKey();

                    // Now sort the images in the buffer folder
                    ImageProcessor.SortImagesInSpecificFolder(bufferPath);

                    // Optionally, delete the buffer folder after sorting
                    // Directory.Delete(bufferPath, true);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error connecting to device: {ex.Message}[/]");
                    Console.ReadKey();
                }
                finally
                {
                    selectedDevice.Disconnect();
                }
            }
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
                        currentPath = Path.Combine(currentPath, choice);
                    }
                }
            }
        }
    }
}
