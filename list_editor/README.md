# VkQuality List Editor

This directory contains a very basic editor for creating custom device
allow/deny list files for use with the VkQuality plugin.

The editor is written in C#/.NET 7 using the [Terminal.Gui](https://github.com/gui-cs/Terminal.Gui) package for UI. The editor should be runnable on any machine with
the .NET 7 runtime installed (Win/Mac/Linux). A prebuilt executable is not
currently available - the editor needs to be built from source. The project
files were created with Jetbrains Rider. The project should be buildable with
Microsoft Visual Studio 2022/command line tools, but this has not been tested.

## Directories

**[editor_project](editor_project)** - The root directory of the editor project,
containing the solution, project files and source code.

**[example_data](example_data)** - Example .CSV input files and a .JSON
editor project file. These file contents were used to export the data file
used as the internal VkQuality default.

## Using the editor

The current editor workflow is:

1. Create a new project file, it will be empty of any device list or gpu
recommendation data.
2. Import device list and gpu allow and gpu deny list data from .CSV files
using the Import menu commands.
3. Export an .vkq runtime data file or a Android Studio library project that
will build a .aar library file that includes the .vkq runtime data file.

You can view, but not currently not edit list data imported into the project
from within the editor. The only way to change a list is to import from a
.CSV file. Importing will clear any existing contents of a list and replace
completly with the data in the .CSV file.

The editor allows you to alter the current 'version' number of the list data.
You should increment this when you make changes to the list data before
exporting a new list, as it will ensure the VkQuality plugin invalidates
its internal cache and reruns recommendations against the new list data.

The name you give your project file will dictate the name of the exported
.vkq file contained within. If you named it 'MyCustom'
you would end up with a `MyCustomFile.vkq` file. You would then tell VkQuality
to use it by passing it as a parameter to `StartVkQuality`:

`vkQuality.StartVkQuality("MyCustomFile.vkq");`

## Building the .aar library

If you are distributing your custom .vkq file at runtime via a CDN or other
method, you can just export the .vkq and use it directly.

If you wish to include the .vkq file in your app bundle, you need to create
a .aar archive file to copy into Assets/Plugins/Android. The list editor
cannot create an .aar directly, but will create an Android Studio project
that will build an .aar that contains your custom .vkq file.

To [build from the command line](https://developer.android.com/tools#tools-sdk)
you need to have Android Studio or the
[Android SDK command-line tools](https://developer.android.com/studio#command-tools)
package installed.

When you select the `Create .AAR Library Project` menu item in the list editor,
it will create a `templateproject/` subdirectory in the directory that
contains your list editor project. Navigate to the `templateproject/` directory
in a command terminal and execute the following command:

On Linux or macOS:

`./gradlew assembleRelease`

On Microsoft Windows:

`gradlew.bat assembleRelease`

Copy the generated `customquality-release.aar` file from
`templateproject/customquality/build/outputs/aar/`
into your `Assets/Plugins/Android` directory.

## Bugs and Issues

To report a bug or issue with VkQuality, please open an Issue from the Issues
page. This is not an officially supported Google product.

## Version history

1.0.1 - (06/26/2024) - Added direct .vkq file export. Removed .aar export
                       and changed to generate a Android Studio project
                       to build from command-line due to compatibility issues
                       with direct .aar generation. Fixed bug with
                       GPU allow/deny prediction lists not being reset
                       before importing a new .csv file.

1.0.0 - (05/14/2024) - Initial release.
