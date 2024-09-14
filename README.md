# ImageOrganizer

Sort images by date and filetype/Import images from a camera

Development:

Please make sure to install the required NuGet packages:

Spectre.Console:

```mathematica

Install-Package Spectre.Console
```

MediaDevices:

```mathematica

Install-Package MediaDevices
```

Newtonsoft.Json:

```mathematica

Install-Package Newtonsoft.Json
```

MetadataExtractor:

```mathematica

    Install-Package MetadataExtractor
```

Additionally, add a reference to System.Windows.Forms:

    Right-click on References in your project.
    Select Add Reference....
    Go to Assemblies > Framework.
    Check System.Windows.Forms.
    Click OK.

How to Run:

    Install Dependencies:
        Install the required NuGet packages as mentioned above.
        Add a reference to System.Windows.Forms.

    Build and Run:
        Build the solution in Visual Studio to ensure there are no compilation errors.
        Run the application.

    Set Default Export Path:
        If the default export path is not set, the application will prompt you to select one using a folder browser dialog.

    Use the Application:
        Navigate through the menu using the console interface.
        Use the settings menu to configure supported formats and the device import folder.
        Use the options to sort images in a folder or import images from a camera.

Testing:

    Sorting Images in a Folder:
        Place some images in a test folder.
        Use the "Sort Images in a Folder" option.
        Select the folder using the folder browser dialog.
        Verify that images are sorted into folders based on the Date Taken.

    Importing from Camera:
        Connect a camera with images.
        Use the "Import Images from Camera" option.
        If no default device import folder is set, you'll be prompted to navigate the device's directories.
        Verify that images are imported to the buffer folder and then sorted.

    Configuring Settings:
        Use the settings menu to configure supported formats.
        When selecting supported formats, use the multi-selection prompt.
        Add custom formats if needed.
