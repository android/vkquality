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

#include "vkquality_matching.h"
#include <string.h>

namespace vkquality {

VkQualityMatching::StringMatchResult VkQualityMatching::StringMatches(
    const std::string_view &a, const std::string_view &b) {
  const size_t a_length = a.length();
  const size_t b_length = b.length();
  if (a_length == 0 || b_length == 0) {
    return kStringMatch_None;
  }

  if (b[0] == '^' && b_length > 1) {
    // Substring match at start of string
    const char *compare = b.data() + 1;
    if (strstr(a.data(), compare) == a.data()) {
      return kStringMatch_Substring_Start;
    }
  } else if (b[0] == '*' && b_length > 1) {
    // Substring match anywhere in string
    const char *compare = b.data() + 1;
    if (strstr(a.data(), compare) != nullptr) {
      return kStringMatch_Substring;
    }
  } else {
    // Exact match
    if (a == b) {
      return kStringMatch_Exact;
    }
  }

  return kStringMatch_None;
}

VkQualityPredictionFile::FileMatchResult VkQualityMatching::CheckDeviceMatch(
    const DeviceInfo &device_info,
    const std::string_view &brand,
    const std::string_view &device,
    const uint32_t min_api,
    const uint32_t min_driver) {
  VkQualityPredictionFile::FileMatchResult result = VkQualityPredictionFile::kFileMatch_None;
  bool version_too_old = false;

  // Must at least have a brand string
  if (brand.empty()) {
    return VkQualityPredictionFile::kFileMatch_None;
  }

  if (min_api > 0 && device_info.api_level < min_api) {
    version_too_old = true;
  }

  if (min_driver > 0 && device_info.vk_driver_version < min_driver) {
    version_too_old = true;
  }

  if (device.empty()) {
    std::string_view brand_view(device_info.brand);
    if (brand_view == brand && !version_too_old) {
      return VkQualityPredictionFile::kFileMatch_BrandWildcard;
    }
  } else {
    std::string_view brand_view(device_info.brand);
    std::string_view device_view(device_info.device);
    if (brand_view == brand && device_view == device) {
      if (version_too_old) {
       return VkQualityPredictionFile::kFileMatch_DeviceOldVersion;
      } else {
        return VkQualityPredictionFile::kFileMatch_ExactDevice;
      }
    }
  }

  return result;
}

VkQualityPredictionFile::FileMatchResult VkQualityMatching::CheckGpuMatch(
    const DeviceInfo &device_info,
    const std::string_view &device,
    const uint32_t device_id,
    const uint32_t vendor_id,
    const uint32_t min_api,
    const uint32_t min_driver,
    const VkQualityPredictionFile::FileMatchResult match_result) {

  // Require a device name string, or an explicit device/vendor id combo
  if ((device_id == 0 || vendor_id == 0) && device.empty()) {
    return VkQualityPredictionFile::kFileMatch_None;
  }

  if (match_result == VkQualityPredictionFile::kFileMatch_GpuAllow) {
    if (min_driver > 0 && device_info.vk_driver_version < min_driver) {
      return VkQualityPredictionFile::kFileMatch_None;
    }
    if (min_api > 0 && device_info.api_level < min_api) {
      return VkQualityPredictionFile::kFileMatch_None;
    }
  } else {
    if (min_driver > 0 && device_info.vk_driver_version > min_driver) {
      return VkQualityPredictionFile::kFileMatch_None;
    }
    if (min_api > 0 && device_info.api_level > min_api) {
      return VkQualityPredictionFile::kFileMatch_None;
    }
  }

  if (device_id == device_info.vk_device_id &&
      vendor_id == device_info.vk_vendor_id) {
    return match_result;
  }

  if (VkQualityMatching::StringMatches(device_info.vk_device_name, device) !=
      VkQualityMatching::kStringMatch_None) {
    return match_result;
  }

  return VkQualityPredictionFile::kFileMatch_None;
}

}