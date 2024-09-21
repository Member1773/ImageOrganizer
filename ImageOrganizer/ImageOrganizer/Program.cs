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
                AnsiConsole.MarkupLine("[yellow]Default Export Path is not set or does not exist.[/]");
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
