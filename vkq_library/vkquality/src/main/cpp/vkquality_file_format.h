/*
 * Copyright 2024 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#ifndef VKQUALITY_FILE_FORMAT_H_
#define VKQUALITY_FILE_FORMAT_H_

#include <cstdint>

namespace vkquality {

/**
 * @brief A structure that describes the header of a VkQuality
 * recommendations file.
 */
typedef struct __attribute__((packed)) VkQualityFileHeader {
  /** @brief Identifier value for the file, expected to be equal
   * to the `kVkQuality_File_Identifier` constant.
   */
  uint32_t file_identifier;
  /** @brief Semantic version number of the format of this VkQuality file
   * (i.e. 0x010200 for 1.2.0)
   */
  uint32_t file_format_version;
  /** @brief Semantic version number of the earliest version
   * of the VkQuality library that is compatible with this file.
   * (i.e. 0x010200 for 1.2.0)
   */
  uint32_t library_minimum_version;
  /** @brief Version number of the data contained in this file, treated
   * like a versionCode in an app bundle
   */
  uint32_t list_version;
  /** @brief The minimum Android API level required to be running on an
   * unrecognized device to recommend using Vulkan because it's the future
   */
  int32_t min_future_vulkan_recommendation_api;
  /** @brief The number of device allow list entries present in the file. The
   * device allow list is a sequential array of
   * `VkQualityDeviceAllowListEntry` structures starting
   * at `device_list_offset` bytes from the beginning of this header.
   */
  uint32_t device_list_count;
  /** @brief The number of gpu predict allow list entries present in the file. The
   * gpu predict allow list is a sequential array of
   * `VkQualityGpuPredictEntry` structures starting
   * at `gpu_allow_predict_offset` bytes from the beginning of this header.
   */
  uint32_t gpu_allow_predict_count;
  /** @brief The number of gpu predict deny list entries present in the file. The
   * gpu predict deny list is a sequential array of
   * `VkQualityGpuPredictEntry` structures starting
   * at `gpu_deny_predict_offset` bytes from the beginning of this header.
   */
  uint32_t gpu_deny_predict_count;
  /** @brief The number of strings in the string table located at
   * at `string_table_offset` bytes from the beginning of this header.
   * The string table starts with a `string_table_count` array of 32-bit offsets
   * from beginning of this header for each UTF-8 null-terminated C string in the string table.
   */
  uint32_t string_table_count;
  /** @brief Offset in bytes from the beginning of the header to the start of the string table data
   */
  uint32_t string_table_offset;
  /** @brief Offset in bytes from the beginning of the header to the start of the device list
   * data
   */
  uint32_t device_list_offset;
  /** @brief Offset in bytes from the beginning of the header to the start of the device list
   * shortcut data. This is a 27 entry array that specifies an index offset A-Z (+1 for
   * everything else) into the device array list for the Device.BRAND strings. This is a shortcut
   * to reduce the search space to the first letter of a set of brands.
   */
  uint32_t device_list_shortcuts_offset;
  /** @brief Offset in bytes from the beginning of the header to the start of the gpu
   * predict allow list data
   */
  uint32_t gpu_allow_predict_offset;
  /** @brief Offset in bytes from the beginning of the header to the start of the gpu
   * predict allow list data
   */
  uint32_t gpu_deny_predict_offset;
} VkQualityFileHeader;

/**
 * @brief A structure that describes the data to match a device for the Vulkan
 * allowlist recommendation
 */
typedef struct __attribute__((packed)) VkQualityDeviceAllowListEntry {
  /** @brief Index into the string table of the string containing the
   * Build.BRAND matching value for this device entry
   */
  uint32_t brand_string_index;
  /** @brief Index into the string table of the string containing the
   * Build.DEVICE matching value for this device entry. This may be a null (0 index)
   * string, which will apply to all devices for Build.BRAND that do not have
   * a Build.DEVICE entry (i.e. futureproofing against future device models)
   */
  uint32_t device_string_index;
  /** @brief Minimum API level the device matching this device level
   * must be running to recommend Vulkan. 0 = any API version
   */
  uint32_t min_api_version;
  /** @brief Minimum driver version reported by VkPhysicalDeviceProperties.driverVersion
   * required to recommend Vulkan on this device. 0 = any driver version
   */
  uint32_t min_driver_version;
} VkQualityDeviceAllowListEntry;

/**
 * @brief A structure that describes the data to match a GPU for a Vulkan
 * predict allowlist or denylist recommendation
 */
typedef struct __attribute__((packed)) VkQualityGpuPredictEntry {
  /** @brief Index into the string table of the string containing the
   * VkPhysicalDeviceProperties.deviceName match for this entry. This
   * does not need to be an exact match. If the string begins with '^' a
   * starts with substring match will qualify. If the string begins with '*' a
   * substring match anywhere in deviceName will qualify.
   * i.e. 'FooGpu 2400xl' deviceName
   * 'FooGpu 2400' - no match
   * '^FooGpu 24' - match
   * '*2400xl' - match
   * If this is set null string entry (0 index), device_id and vendor_id must be populated
   * and non-zero as they will be use to qualify a match
   */
  uint32_t device_name_string_index;
  /** @brief Minimum API level the device matching this device level
   * must be running to recommend Vulkan. 0 = any API version
   */
  uint32_t min_api_version;
  /** @brief Matching value of VkPhysicalDeviceProperties.deviceid for this entry, can
   * be 0 (ignored) if brand_string_index is a valid string.
   */
  uint32_t device_id;
  /** @brief Matching value of VkPhysicalDeviceProperties.vendorid for this entry, can
   * be 0 (ignored) if brand_string_index is a valid string.
   */
  uint32_t vendor_id;
  /** @brief Minimum or driver version reported by VkPhysicalDeviceProperties.driverVersion
   * required to recommend or not recommend Vulkan on this device.
   * If used in a gpu_deny_predict entry, driver numbers BELOW this number match for the
   * deny list.
   * If used in a gpu_allow_predict entry, driver numbers EQUAL OR GREATER this number
   * match for the allow list
   */
  uint32_t min_driver_version;
} VkQualityGpuPredictEntry;

} // namespace vkquality

#endif // VKQUALITY_FILE_FORMAT_H_
