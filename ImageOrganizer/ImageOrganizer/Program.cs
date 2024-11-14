using System;
using System.Threading.Tasks;
using Spectre.Console;

namespace ImageOrganizer
{
    static class Program
    {
        [STAThread]
        static async Task Main()
        {
            ConfigurationManager.LoadConfig();

            // Check for updates asynchronously
            await CheckForUpdates();

            // If DefaultExportPath is not set, prompt the user to set it
            if (string.IsNullOrEmpty(ConfigurationManager.Settings.DefaultExportPath) ||
                !System.IO.Directory.Exists(ConfigurationManager.Settings.DefaultExportPath))
            {
                // Inform the user about supported formats
                AnsiConsole.MarkupLine("[green]All common image formats are selected by default.[/]");
                AnsiConsole.MarkupLine("If you want to exclude or add formats, you can do so via the settings menu.\n");
                
                //Set the default export path
                AnsiConsole.MarkupLine("[yellow]Default Export Path is not set or does not exist.[/]");
                AnsiConsole.MarkupLine("[yellow]Press any key to set it now...[/]");
                Console.ReadKey();
                SettingsManager.SetDefaultExportPath();
            }



            bool exit = false;
            while (!exit)
            {
                var choice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("[green]Select an option:[/]")
                        .AddChoices(new[]
                        {
                            "Sort Images in a Folder",
                            "Import Images from Camera",
                            "Settings",
                            "Exit"
                        }));

                switch (choice)
                {
                    case "Sort Images in a Folder":
                        ImageProcessor.SortImagesInFolder();
                        break;
                    case "Import Images from Camera":
                        CameraImporter.ImportImagesFromCamera();
                        break;
                    case "Settings":
                        SettingsManager.ConfigureSettings();
                        break;
                    case "Exit":
                        exit = true;
                        break;
                }
            }
        }

        public static async Task CheckForUpdates(bool manualCheck = false)
        {
            // Only check if last check was more than 24 hours ago or if manual check
            DateTime lastChecked = ConfigurationManager.Settings.LastUpdateCheck;
            if (!manualCheck && (DateTime.Now - lastChecked).TotalHours < 24)
            {
                return;
            }

            // Update the last checked time
            ConfigurationManager.Settings.LastUpdateCheck = DateTime.Now;
            ConfigurationManager.SaveConfig();

            string latestVersion = await UpdateChecker.CheckForUpdatesAsync();

            if (!string.IsNullOrEmpty(latestVersion))
            {
                // Check if user has skipped this version
                if (ConfigurationManager.Settings.SkippedVersion == latestVersion && !manualCheck)
                {
                    // User has chosen to skip this version, so do not notify
                    return;
                }

                int comparison = VersionHelper.CompareVersions(ConfigurationManager.CurrentVersion, latestVersion);

                if (comparison < 0)
                {
                    // A newer version is available
                    AnsiConsole.MarkupLine(
                        $"[yellow]A new version ({latestVersion}) is available! You are using version {ConfigurationManager.CurrentVersion}.[/]");

                    // Ask the user what they want to do
                    var choice = AnsiConsole.Prompt(
                        new SelectionPrompt<string>()
                            .Title("What would you like to do?")
                            .AddChoices(new[] { "Download Update", "Skip this Version", "Remind Me Later" }));

                    if (choice == "Download Update")
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = $"https://github.com/Member1773/ImageOrganizer/releases/latest",
                            UseShellExecute = true
                        });
                        AnsiConsole.Clear();
                        AnsiConsole.MarkupLine("[Yellow]To update the Program, download the latest zip file and replace all files with the latest version.[/]");
                        AnsiConsole.MarkupLine("[Yellow]Console will exit after pressing any key.[/]");
                        Console.ReadKey();
                        Environment.Exit(0);
                    }
                    else if (choice == "Skip this Version")
                    {
                        ConfigurationManager.Settings.SkippedVersion = latestVersion;
                        ConfigurationManager.SaveConfig();
                    }
                    else if (choice == "Remind Me Later (24h)")
                    {
                        // Do nothing, will remind in the next check (after 24 hours)
                    }
                }
                else if (comparison > 0)
                {
                    // Current version is newer than the latest release
                    if (manualCheck)
                    {
                        AnsiConsole.MarkupLine("[green]You are using a pre-release or development version.[/]");
                    }
                }
                else
                {
                    // Up to date
                    if (manualCheck)
                    {
                        AnsiConsole.MarkupLine("[green]You are using the latest version.[/]");
                    }
                }
            }
            else
            {
                if (manualCheck)
                {
                    AnsiConsole.MarkupLine(
                        "[red]Could not check for updates. Please check your internet connection.[/]");
                }
            }
        }
    }
}