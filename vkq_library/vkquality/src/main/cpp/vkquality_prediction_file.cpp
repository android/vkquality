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

#include "vkquality_prediction_file.h"
#include "vkquality_matching.h"
#include <ctype.h>
#include <malloc.h>
#include <vector>

namespace vkquality {

static constexpr char kNullString = '\0';

static const std::string str_fmt(const char *const fmt_string, ...) {
  va_list va_args;
  va_start(va_args, fmt_string);
  va_list va_args_copy;
  va_copy(va_args_copy, va_args);
  const int formatted_length = std::vsnprintf(NULL, 0, fmt_string, va_args_copy);
  va_end(va_args_copy);

  std::vector<char> format_buffer(formatted_length + 1);
  std::vsnprintf(format_buffer.data(), format_buffer.size(), fmt_string, va_args);
  va_end(va_args);
  return std::string(format_buffer.data(), formatted_length);
}

static bool CheckOffsetListValidity(const uint32_t *offset_list, const uint32_t offset_count,
                                    const size_t file_size) {
  for (uint32_t i = 0; i < offset_count; ++i) {
    size_t offset = offset_list[i];
    if (offset >= file_size) {
      return false;
    }
  }
  return true;
}

VkQualityPredictionFile::VkQualityPredictionFile() {
  file_parse_error_ = "No error";
}

VkQualityPredictionFile::~VkQualityPredictionFile() {
  if (file_header_ != nullptr) {
    free(const_cast<VkQualityFileHeader *>(file_header_));
  }
}

VkQualityPredictionFile::FileParseResult VkQualityPredictionFile::ValidateFile(
    void *file_data, const size_t file_size, const uint32_t library_version) {
  const VkQualityFileHeader *header = reinterpret_cast<const VkQualityFileHeader *>(file_data);

  // File must be at least the size of the header
  if (file_size < sizeof(VkQualityFileHeader)) {
    file_parse_error_ = str_fmt("File size (%d) smaller than header size: %d",
                                (int)file_size, (int)sizeof(VkQualityFileHeader));
    return kFileParseResult_Error_TooSmall;
  }
  if (header->file_identifier != kVkQuality_File_Identifier) {
    file_parse_error_ = "File identifier invalid";
    return kFileParseResult_Error_InvalidIdentifier;
  }
  if (header->library_minimum_version > library_version) {
    file_parse_error_ = str_fmt("File minimum library version is %x, but library is %x",
                                header->library_minimum_version, library_version);
    return kFileParseResult_Error_LibraryTooOldForFile;
  }

  const size_t device_list_size = header->device_list_count * sizeof(VkQualityDeviceAllowListEntry);
  const size_t device_list_end = header->device_list_offset + device_list_size;
  if (device_list_end > file_size) {
    file_parse_error_ = "Invalid file: Device list overflows end of file";
    return kFileParseResult_Error_DeviceListOverflow;
  }

  const size_t gpu_allow_list_size = header->gpu_allow_predict_count *
                                     sizeof(VkQualityGpuPredictEntry);
  const size_t gpu_allow_list_end = header->gpu_allow_predict_offset + gpu_allow_list_size;
  if (gpu_allow_list_end > file_size) {
    file_parse_error_ = "Invalid file: GPU allow list overflows end of file";
    return kFileParseResult_Error_GpuAllowOverflow;
  }

  const size_t gpu_deny_list_size = header->gpu_deny_predict_count *
                                    sizeof(VkQualityGpuPredictEntry);
  const size_t gpu_deny_list_end = header->gpu_deny_predict_offset + gpu_deny_list_size;
  if (gpu_deny_list_end > file_size) {
    file_parse_error_ = "Invalid file: GPU deny list overflows end of file";
    return kFileParseResult_Error_GpuDenyOverflow;
  }

  // Individual string offset bounds checks are made at string retrieval time, we just make
  // sure the actual string offset list is within the file bounds here.
  const size_t string_offset_list_size = header->string_table_count * sizeof(uint32_t);
  const size_t string_offset_list_end = header->string_table_offset + string_offset_list_size;
  if (string_offset_list_end > file_size) {
    file_parse_error_ = "Invalid file: string table offset list overflows end of file";
    return kFileParseResult_Error_StringOffsetOverflow;
  }
  const uint8_t *file_start = reinterpret_cast<const uint8_t *>(file_data);
  const uint32_t *string_offsets = reinterpret_cast<const uint32_t *>(
      (file_start + header->string_table_offset));
  if (!CheckOffsetListValidity(string_offsets, header->device_list_count, file_size)) {
    file_parse_error_ = "Invalid file: String offset table entry overflows end of file";
    return kFileParseResult_Error_StringOffsetOverflow;
  }

  const size_t shortcut_offset_list_size = VkQualityPredictionFile::kShortcut_Offset_Count * sizeof(uint32_t);
  const size_t shortcut_offset_list_end = header->device_list_shortcuts_offset + shortcut_offset_list_size;
  if (shortcut_offset_list_end > file_size) {
    file_parse_error_ = "Invalid file: shortcut offset list overflows end of file";
    return kFileParseResult_Error_ShortcutOverflow;
  }

  return kFileParseResult_Success;
}

VkQualityPredictionFile::FileParseResult VkQualityPredictionFile::ParseFileData(
    void *file_data, const size_t file_size, const uint32_t library_version) {
  VkQualityPredictionFile::FileParseResult result =
      ValidateFile(file_data, file_size, library_version);

  if (result != kFileParseResult_Success) {
    return result;
  }

  total_file_size_ = file_size;
  file_header_ = reinterpret_cast<const VkQualityFileHeader *>(file_data);
  const uint8_t *file_start = reinterpret_cast<const uint8_t *>(file_data);

  string_offset_table_ = reinterpret_cast<const uint32_t *>(
      (file_start + file_header_->string_table_offset));
  device_shortcut_table_ = reinterpret_cast<const uint32_t *>(
      (file_start + file_header_->device_list_shortcuts_offset));
  device_table_ = reinterpret_cast<const VkQualityDeviceAllowListEntry *>(
      (file_start + file_header_->device_list_offset));
  gpu_allow_table_ = reinterpret_cast<const VkQualityGpuPredictEntry *>(
      (file_start + file_header_->gpu_allow_predict_offset));
  gpu_deny_table_ = reinterpret_cast<const VkQualityGpuPredictEntry *>((
      file_start + file_header_->gpu_deny_predict_offset));

  return result;
}

VkQualityPredictionFile::FileMatchResult VkQualityPredictionFile::FindDeviceMatch(
    const DeviceInfo &device_info) {

  // First search for an explicit device match in the device list
  FileMatchResult result = SearchDeviceList(device_info);
  if (result == kFileMatch_None) {
    // If there was no device match, look for a GPU allow or deny prediction match
    result = SearchGpuLists(device_info);
  }

  return result;
}

VkQualityPredictionFile::FileMatchResult VkQualityPredictionFile::SearchDeviceList(
    const DeviceInfo &device_info) {

  // Shortcut offset table is sorted Device.BRAND from A-Z and then everything else, default to
  // the 'everything else' entry after the alphabet
  uint32_t letter_index = 26;
  const char brand_first_letter = toupper(device_info.brand.c_str()[0]);
  if (brand_first_letter >= 'A' && brand_first_letter <= 'Z') {
    letter_index = brand_first_letter - 'A';
  }
  const uint32_t start_device_table_index = device_shortcut_table_[letter_index];

  for (uint32_t i = start_device_table_index; i < file_header_->device_list_count; ++i) {
    const char *brand_string = GetString(device_table_[i].brand_string_index);
    const char *device_string = GetString(device_table_[i].device_string_index);
    std::string_view brand_view(brand_string);
    std::string_view device_view(device_string);
    FileMatchResult result = VkQualityMatching::CheckDeviceMatch(device_info,
                                                                 brand_view,
                                                                 device_view,
                                                                 device_table_[i].min_api_version,
                                                                 device_table_[i].min_driver_version);
    if (result != kFileMatch_None) {
      return result;
    }
  }
  return kFileMatch_None;
}

VkQualityPredictionFile::FileMatchResult VkQualityPredictionFile::SearchGpuLists(
    const DeviceInfo &device_info) {

  FileMatchResult result = SearchGpuList(device_info, kFileMatch_GpuAllow);
  if (result == kFileMatch_None) {
    result = SearchGpuList(device_info, kFileMatch_GpuDeny);
  }
  return result;
}

VkQualityPredictionFile::FileMatchResult VkQualityPredictionFile::SearchGpuList(
    const DeviceInfo &device_info, const FileMatchResult match_result) {

  const VkQualityGpuPredictEntry *gpu_table;
  uint32_t table_count;
  if (match_result == kFileMatch_GpuAllow) {
    gpu_table = gpu_allow_table_;
    table_count = file_header_->gpu_allow_predict_count;
  } else if (match_result == kFileMatch_GpuDeny) {
    gpu_table = gpu_deny_table_;
    table_count = file_header_->gpu_deny_predict_count;
  } else {
    return kFileMatch_None;
  }

  for (uint32_t i = 0; i < table_count; ++i) {
    const char *device_string = GetString(gpu_table[i].device_name_string_index);
    std::string_view device_view(device_string);
    FileMatchResult result = VkQualityMatching::CheckGpuMatch(device_info,
                                                              device_string,
                                                              gpu_table[i].device_id,
                                                              gpu_table[i].vendor_id,
                                                              gpu_table[i].min_api_version,
                                                              gpu_table[i].min_driver_version,
                                                              match_result);
    if (result == match_result) {
      return result;
    }
  }

  return kFileMatch_None;
}

const char *VkQualityPredictionFile::GetString(const uint32_t string_index) {
  // Bounds check both the string index and the actual string data, return
  // a placeholder null string if either end up out of bounds
  if (string_index >= file_header_->string_table_count) {
    return &kNullString;
  }
  uint32_t string_offset_start = string_offset_table_[string_index];
  if (string_offset_start > total_file_size_) {
    return &kNullString;
  }
  const size_t max_possible_length = total_file_size_ - string_offset_start;
  const char *string_base = reinterpret_cast<const char *>(file_header_) + string_offset_start;
  const size_t string_length = strnlen(string_base, max_possible_length);
  if (string_length == max_possible_length) {
    return &kNullString;
  }
  return string_base;
}

}