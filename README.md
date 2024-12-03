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

**[list_editor/](list_editor)** - A tool for generating the runtime quality
data file. This is only useful for developers that wish to create custom
list data files. Further information located in the subdirectory README.

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

* 1.2 - (12/03/2024) - Added additional quality signal source option, derived from
pairs of SoC names and driver fingerprint strings. This signal check can be disabled
using a new `INIT_FLAG_SKIP_DRIVER_FINGERPRINT_CHECK` flag.
* 1.1 - (08/02/2024) - Added mitigation code to fix issues of certain OEM devices
with certain SoCs on odler drivers crashing from VkQuality attempting to determine
driver version. Added new `StartVkQualityWithFlags` call with flags to disable/change
the startup mitigation flow. See the `VkQualityTestActivity.java` file for details.
No changes to the default device list file in this version.
* 1.0.1 - (06/21/2024) - Updated device list, no code changes.
* 1.0 - (05/14/2024) - Initial release.
