using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataDirectory = MetadataExtractor.Directory; // Alias to avoid conflict
using Spectre.Console;
using Newtonsoft.Json;
using MediaDevices;

namespace ImageOrganizer
{
    class Program
    {
        // Configuration class to hold settings
        public class Config
        {
            public List<string> SupportedFormats { get; set; }
            public string DefaultExportPath { get; set; }
            public string DeviceImportFolder { get; set; }
        }

        static Config config;

        [STAThread]
        static void Main(string[] args)
        {
            LoadConfig();

            // If DefaultExportPath is not set, prompt the user to set it
            if (string.IsNullOrEmpty(config.DefaultExportPath) || !System.IO.Directory.Exists(config.DefaultExportPath))
            {
                AnsiConsole.MarkupLine("[yellow]Default Export Path is not set or does not exist.[/]");
                SetDefaultExportPath();
            }

            bool exit = false;

            while (!exit)
            {
                AnsiConsole.Clear();
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[green]Select an option:[/]")
                        .AddChoices(new[] {
                            "Sort Images in a Folder",
                            "Import Images from Camera",
                            "Settings",
                            "Exit"
                        }));

                switch (choice)
                {
                    case "Sort Images in a Folder":
                        SortImagesInFolder();
                        break;
                    case "Import Images from Camera":
                        ImportImagesFromCamera();
                        break;
                    case "Settings":
                        ConfigureSettings();
                        break;
                    case "Exit":
                        exit = true;
                        break;
                }
            }
        }

        static void LoadConfig()
        {
            string configPath = "appsettings.json";
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                config = JsonConvert.DeserializeObject<Config>(json);
            }
            else
            {
                // Default configuration
                config = new Config
                {
                    SupportedFormats = new List<string> { ".JPG", ".RAF" },
                    DefaultExportPath = "",
                    DeviceImportFolder = "" // Default to empty
                };
                SaveConfig();
            }
        }

        static void SaveConfig()
        {
            string json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText("appsettings.json", json);
        }

        static void SetDefaultExportPath()
        {
            AnsiConsole.MarkupLine("[green]Please select a default export path.[/]");
            string exportPath = SelectFolderDialog();

            if (!string.IsNullOrEmpty(exportPath))
            {
                config.DefaultExportPath = exportPath;
                SaveConfig();
                AnsiConsole.MarkupLine($"[green]Default export path set to:[/] {exportPath}");
            }
            else
            {
                AnsiConsole.MarkupLine("[red]No folder selected. Exiting application.[/]");
                Environment.Exit(0);
            }
        }

        static string SelectFolderDialog()
        {
            string folderPath = null;
            using (var folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Select a Folder";

                // Show the dialog
                DialogResult result = folderDialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                {
                    folderPath = folderDialog.SelectedPath;
                }
            }
            return folderPath;
        }

        static void SortImagesInFolder()
        {
            AnsiConsole.MarkupLine("[green]Please select the folder containing the images to sort.[/]");
            string folderPath = SelectFolderDialog();

            if (string.IsNullOrWhiteSpace(folderPath) || !System.IO.Directory.Exists(folderPath))
            {
                AnsiConsole.MarkupLine("[red]Invalid folder path. Press any key to return to the menu.[/]");
                Console.ReadKey();
                return;
            }

            AnsiConsole.MarkupLine("[green]Processing files...[/]");

            string[] imageFiles = System.IO.Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                                           .Where(s => config.SupportedFormats.Contains(Path.GetExtension(s).ToUpperInvariant()))
                                           .ToArray();

            int totalFiles = imageFiles.Length;
            int processedFiles = 0;

            if (totalFiles == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No supported image files found in the specified folder.[/]");
                AnsiConsole.MarkupLine("Press any key to return to the menu.");
                Console.ReadKey();
                return;
            }

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
                        string dateTaken = GetDateTaken(file);
                        string extension = Path.GetExtension(file).TrimStart('.').ToUpperInvariant();
                        string dateFolderName = dateTaken;

                        string dateFolderPath = Path.Combine(config.DefaultExportPath, dateFolderName);
                        string targetSubFolder = Path.Combine(dateFolderPath, extension);

                        // Create directories if they do not exist
                        System.IO.Directory.CreateDirectory(targetSubFolder);

                        string fileName = Path.GetFileName(file);
                        string destinationPath = Path.Combine(targetSubFolder, fileName);

                        // Move the file
                        File.Move(file, destinationPath);

                        processedFiles++;
                        task.Increment(1);
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Error processing file {file}: {ex.Message}[/]");
                        // Log the error or handle accordingly
                    }
                }
            });

            AnsiConsole.MarkupLine("[green]Processing complete. Press any key to return to the menu.[/]");
            Console.ReadKey();
        }

        static void ImportImagesFromCamera()
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

                    string photosFolder = config.DeviceImportFolder;

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
                        .Where(s => config.SupportedFormats.Contains(Path.GetExtension(s).ToUpperInvariant()))
                        .ToList();

                    if (!mediaFiles.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]No supported image files found in the selected folder.[/]");
                        Console.ReadKey();
                        return;
                    }

                    // Ensure Buffer folder exists in DefaultExportPath
                    string bufferPath = Path.Combine(config.DefaultExportPath, "Buffer");
                    System.IO.Directory.CreateDirectory(bufferPath);

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
                    SortImagesInSpecificFolder(bufferPath);

                    // Optionally, delete the buffer folder after sorting
                    // System.IO.Directory.Delete(bufferPath, true);
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

        static string SelectDeviceFolder(MediaDevice device)
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

        static void SortImagesInSpecificFolder(string folderPath)
        {
            AnsiConsole.MarkupLine("[green]Processing files...[/]");

            string[] imageFiles = System.IO.Directory.GetFiles(folderPath, "*.*", SearchOption.TopDirectoryOnly)
                                           .Where(s => config.SupportedFormats.Contains(Path.GetExtension(s).ToUpperInvariant()))
                                           .ToArray();

            int totalFiles = imageFiles.Length;
            int processedFiles = 0;

            if (totalFiles == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No supported image files found in the buffer folder.[/]");
                AnsiConsole.MarkupLine("Press any key to return to the menu.");
                Console.ReadKey();
                return;
            }

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
                        string dateTaken = GetDateTaken(file);
                        string extension = Path.GetExtension(file).TrimStart('.').ToUpperInvariant();
                        string dateFolderName = dateTaken;

                        string dateFolderPath = Path.Combine(config.DefaultExportPath, dateFolderName);
                        string targetSubFolder = Path.Combine(dateFolderPath, extension);

                        // Create directories if they do not exist
                        System.IO.Directory.CreateDirectory(targetSubFolder);

                        string fileName = Path.GetFileName(file);
                        string destinationPath = Path.Combine(targetSubFolder, fileName);

                        // Move the file
                        File.Move(file, destinationPath);

                        processedFiles++;
                        task.Increment(1);
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Error processing file {file}: {ex.Message}[/]");
                        // Log the error or handle accordingly
                    }
                }
            });

            // Optionally, delete the buffer folder after sorting
            // System.IO.Directory.Delete(folderPath, true);

            AnsiConsole.MarkupLine("[green]Processing complete. Press any key to return to the menu.[/]");
            Console.ReadKey();
        }

        static void ConfigureSettings()
        {
            bool back = false;

            while (!back)
            {
                AnsiConsole.Clear();
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[green]Settings:[/]")
                        .AddChoices(new[] {
                            "Supported Formats",
                            "Default Export Path",
                            "Device Import Folder",
                            "Back to Main Menu"
                        }));

                switch (choice)
                {
                    case "Supported Formats":
                        ConfigureSupportedFormats();
                        break;
                    case "Default Export Path":
                        ConfigureDefaultExportPath();
                        break;
                    case "Device Import Folder":
                        ConfigureDeviceImportFolder();
                        break;
                    case "Back to Main Menu":
                        back = true;
                        break;
                }
            }
        }

        static void ConfigureSupportedFormats()
        {
            bool back = false;
            while (!back)
            {
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine("[green]Current Supported Formats:[/]");
                AnsiConsole.MarkupLine(string.Join(", ", config.SupportedFormats));

                var compressedFormats = new List<string> { ".JPG", ".JPEG", ".PNG" };
                var rawFormats = new List<string> { ".RAW", ".RAF", ".CR2", ".NEF", ".DNG" };

                var formatPrompt = new MultiSelectionPrompt<string>();
                formatPrompt.Title("Select supported formats (Use spacebar to select/deselect):");
                formatPrompt.PageSize(10);
                formatPrompt.AddChoiceGroup("Compressed Formats", compressedFormats);
                formatPrompt.AddChoiceGroup("RAW Formats", rawFormats);
                formatPrompt.AddChoice("Add Custom Format");
                formatPrompt.AddChoice("Go Back");

                var selectedFormats = AnsiConsole.Prompt(formatPrompt);

                if (selectedFormats.Contains("Go Back"))
                {
                    back = true;
                    continue;
                }

                if (selectedFormats.Contains("Add Custom Format"))
                {
                    selectedFormats.Remove("Add Custom Format");
                    string customFormat = AnsiConsole.Ask<string>("Enter custom format (e.g., .TIFF):").ToUpperInvariant();
                    if (!string.IsNullOrWhiteSpace(customFormat))
                    {
                        selectedFormats.Add(customFormat.StartsWith(".") ? customFormat : "." + customFormat);
                    }
                }

                config.SupportedFormats = selectedFormats.Where(f => f != "Add Custom Format").Select(f => f.ToUpperInvariant()).Distinct().ToList();
                SaveConfig();

                AnsiConsole.MarkupLine("[green]Supported formats updated.[/]");
                AnsiConsole.MarkupLine("Press any key to return to settings menu.");
                Console.ReadKey();
            }
        }

        static void ConfigureDefaultExportPath()
        {
            bool back = false;
            while (!back)
            {
                AnsiConsole.Clear();
                string currentPath = string.IsNullOrEmpty(config.DefaultExportPath) ? "[[Not Set]]" : config.DefaultExportPath;
                AnsiConsole.MarkupLine($"[green]Current Default Export Path:[/] {currentPath}");

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Do you want to change it?")
                        .AddChoices(new[] { "Yes", "No", "Go Back" }));

                if (choice == "Yes")
                {
                    string exportPath = SelectFolderDialog();
                    if (!string.IsNullOrEmpty(exportPath))
                    {
                        config.DefaultExportPath = exportPath;
                        SaveConfig();
                        AnsiConsole.MarkupLine($"[green]Default export path updated to:[/] {exportPath}");
                        AnsiConsole.MarkupLine("Press any key to return to settings menu.");
                        Console.ReadKey();
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("[yellow]No folder selected. Default export path not changed.[/]");
                        AnsiConsole.MarkupLine("Press any key to return to settings menu.");
                        Console.ReadKey();
                    }
                }
                else if (choice == "No" || choice == "Go Back")
                {
                    back = true;
                }
            }
        }

        static void ConfigureDeviceImportFolder()
        {
            bool back = false;
            while (!back)
            {
                AnsiConsole.Clear();
                string currentPath = string.IsNullOrEmpty(config.DeviceImportFolder) ? "[[Not Set]]" : config.DeviceImportFolder;
                AnsiConsole.MarkupLine($"[green]Current Device Import Folder:[/] {currentPath}");

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Do you want to change it?")
                        .AddChoices(new[] { "Yes", "No", "Go Back" }));

                if (choice == "Yes")
                {
                    string input = AnsiConsole.Ask<string>("Enter new device import folder path (e.g., \\Internal Storage\\DCIM):");

                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        config.DeviceImportFolder = input.Trim();
                        SaveConfig();
                        AnsiConsole.MarkupLine("[green]Device import folder updated.[/]");
                        AnsiConsole.MarkupLine("Press any key to return to settings menu.");
                        Console.ReadKey();
                    }
                    else
                    {
                        config.DeviceImportFolder = "";
                        SaveConfig();
                        AnsiConsole.MarkupLine("[green]Device import folder unset.[/]");
                        AnsiConsole.MarkupLine("Press any key to return to settings menu.");
                        Console.ReadKey();
                    }
                }
                else if (choice == "No" || choice == "Go Back")
                {
                    back = true;
                }
            }
        }

        static string GetDateTaken(string filePath)
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
