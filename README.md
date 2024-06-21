# VkQuality plugin for the Unity engine

This repository is the home of the VkQuality plugin for the Unity engine.
The VkQuality plugin for the Unity engine provides launch-time recommendations
of the graphics API—Vulkan or OpenGL ES—to use for your game on specific devices.

For information on how to use the VkQuality plugin, visit the
[documentation page](https://developer.android.com/games/engines/unity/unity-vkquality).

Prebuilt versions of the plugin with the sample project are available on the
Releases page of this repository. Cloning the repository is not necessary
unless you wish to build the library from source.

## Directories

**[sample_project/](sample_project)** - A sample Unity engine project that uses
the VkQuality plugin. You should download the plugin package from the Releases
page, which includes this sample project with the required VkQuality .aar file
already installed. This directory does not contain the .aar file.

**[vkq_library/](vkq_library)** - The Gradle project file and source code to
build the VkQuality library as an .aar file. This can be opened and built
with Android Studio Iguana or later, or the equivalent command-line tools.

## Bugs and Issues

To report a bug or issue with VkQuality, please open an Issue from the Issues
page.

If you encounter a Vulkan related issue on a device recommended by VkQuality,
please open an Issue from the Issues page. Be sure to include the Unity engine
version being used, and the name of the device exhibiting the issue. This
feedback will help refine the recommendation list.

## Version history

1.0.1 - (6/21/2024) - Updated device list, no code changes.
1.0 - (05/14/2024) - Initial release.
