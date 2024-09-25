using System;
using Spectre.Console;

namespace ImageOrganizer
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ConfigurationManager.LoadConfig();

            // If DefaultExportPath is not set, prompt the user to set it
            if (string.IsNullOrEmpty(ConfigurationManager.Settings.DefaultExportPath) || !System.IO.Directory.Exists(ConfigurationManager.Settings.DefaultExportPath))
            {
                // Inform the user about supported formats
                AnsiConsole.MarkupLine("[green]All common image formats are selected by default.[/]");
                AnsiConsole.MarkupLine("If you want to exclude or add formats, you can do so via the settings menu.\n");
                //set default export path
                AnsiConsole.MarkupLine("[yellow]Default Export Path is not set or does not exist.[/]");
                AnsiConsole.MarkupLine("Press any key to set it now.");
                Console.ReadKey();
                SettingsManager.SetDefaultExportPath();
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
    }
}
