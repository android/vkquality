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
3. Export an .aar library containing the runtime formatted data of the
device and gpu lists.

You can view, but not currently not edit list data imported into the project
from within the editor. The only way to change a list is to import from a
.CSV file. Importing will clear any existing contents of a list and replace
completly with the data in the .CSV file.

The editor allows you to alter the current 'version' number of the list data.
You should increment this when you make changes to the list data before
exporting a new list, as it will ensure the VkQuality plugin invalidates
its internal cache and reruns recommendations against the new list data.

The name you give your project file will dictate the name of the exported
.aar library and the .vkq file contained within. If you named it 'MyCustom'
you would export a `MyCustomFile.aar` file which includes `MyCustomFile.vkq` in its `assets/` directory. The `MyCustom.aar` file needs to go in
`Assets/plugins/Android`. You would then tell VkQuality to use it by
passing it as a parameter to `StartVkQuality`:

`vkQuality.StartVkQuality("MyCustomFile.vkq");`

## Bugs and Issues

To report a bug or issue with VkQuality, please open an Issue from the Issues
page. This is not an officially supported Google product.

## Version history

1.0 - (05/14/2024) - Initial release.