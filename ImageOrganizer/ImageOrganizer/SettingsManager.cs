using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Spectre.Console;

namespace ImageOrganizer
{
    public static class SettingsManager
    {
        public static void ConfigureSettings()
        {
            bool exit = false;
            while (!exit)
            {
                AnsiConsole.Clear();
                var options = new List<string>()
                {
                    "Supported Formats",
                    "Default Export Path",
                    "Device Import Folder",
                    "Check for Updates",
                    "Back to Main Menu"
                };
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[green]Settings:[/]")
                        .AddChoices(options));

                switch (choice)
                {
                    case "Supported Formats":
                        ConfigureSupportedFormats();
                        break;
                    case "Default Export Path":
                        SetDefaultExportPath();
                        break;
                    case "Device Import Folder":
                        ConfigureDeviceImportFolder();
                        break;
                    case "Check for Updates":
                        AnsiConsole.MarkupLine("[green]Checking for updates...[/]");
                        Task.Run(async () => await Program.CheckForUpdates(manualCheck: true)).GetAwaiter().GetResult();
                        AnsiConsole.MarkupLine("Press any key to return to settings menu.");
                        Console.ReadKey();
                        break;
                    case "Back to Main Menu":
                        exit = true;
                        break;
                }
            }
        }

        public static void ConfigureSupportedFormats()
        {
            bool back = false;
            while (!back)
            {
                AnsiConsole.Clear();
                AnsiConsole.MarkupLine("[green]Current Supported Formats:[/]");
                AnsiConsole.MarkupLine(string.Join(", ", ConfigurationManager.Settings.SupportedFormats));

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

                ConfigurationManager.Settings.SupportedFormats = selectedFormats.Where(f => f != "Add Custom Format").Select(f => f.ToUpperInvariant()).Distinct().ToList();
                ConfigurationManager.SaveConfig();

                AnsiConsole.MarkupLine("[green]Supported formats updated.[/]");
                AnsiConsole.MarkupLine("Press any key to return to settings menu.");
                Console.ReadKey();
            }
        }

        public static void SetDefaultExportPath()
        {
            var choice = "";
            bool back = false;
            while (!back)
            {
                AnsiConsole.Clear();
                string currentPath = string.IsNullOrEmpty(ConfigurationManager.Settings.DefaultExportPath) ? "[[Not Set]]" : ConfigurationManager.Settings.DefaultExportPath;
                AnsiConsole.MarkupLine($"[green]Current Default Export Path:[/] {currentPath}");
                if (currentPath == "[[Not Set]]")
                {
                    choice = "Yes";
                }
                else
                { 
                   choice = AnsiConsole.Prompt(
                   new SelectionPrompt<string>()
                       .Title("Do you want to change it?")
                       .AddChoices(new[] { "Yes", "No", "Go Back" }));
                }
                
                if (choice == "Yes")
                {
                    string exportPath = Utilities.SelectFolderDialog();
                    if (!string.IsNullOrEmpty(exportPath))
                    {
                        ConfigurationManager.Settings.DefaultExportPath = exportPath;
                        ConfigurationManager.SaveConfig();
                        AnsiConsole.MarkupLine($"[green]Default export path updated to:[/] {exportPath}");
                        AnsiConsole.MarkupLine("Press any key to return to settings menu.");
                        Console.ReadKey();
                        back = true;
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

        public static void ConfigureDeviceImportFolder()
        {
            bool back = false;
            while (!back)
            {
                AnsiConsole.Clear();
                string currentPath = string.IsNullOrEmpty(ConfigurationManager.Settings.DeviceImportFolder) ? "[[Not Set]]" : ConfigurationManager.Settings.DeviceImportFolder;
                AnsiConsole.MarkupLine($"[green]Current Device Import Folder:[/] {currentPath}");

                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Do you want to change it?")
                        .AddChoices(new[] { "Yes", "No", "Go Back" }));

                if (choice == "Yes")
                {
                    string input = Utilities.SelectFolderDialog();

                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        ConfigurationManager.Settings.DeviceImportFolder = input;
                        ConfigurationManager.SaveConfig();
                        AnsiConsole.MarkupLine("[green]Device import folder updated.[/]");
                        AnsiConsole.MarkupLine("Press any key to return to settings menu.");
                        Console.ReadKey();
                    }
                    else
                    {
                        ConfigurationManager.Settings.DeviceImportFolder = "";
                        ConfigurationManager.SaveConfig();
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
    }
}
